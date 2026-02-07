using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using ASimpleTutor.Core.Models.Dto;
using Microsoft.Extensions.Logging;

namespace ASimpleTutor.Core.Services;

/// <summary>
/// 知识体系构建服务
/// </summary>
public class KnowledgeBuilder : IKnowledgeBuilder
{
    private readonly IScannerService _scannerService;
    private readonly ISimpleRagService _ragService;
    private readonly ISourceTracker _sourceTracker;
    private readonly ILLMService _llmService;
    private readonly ILogger<KnowledgeBuilder> _logger;

    public KnowledgeBuilder(
        IScannerService scannerService,
        ISimpleRagService ragService,
        ISourceTracker sourceTracker,
        ILLMService llmService,
        ILogger<KnowledgeBuilder> logger)
    {
        _scannerService = scannerService;
        _ragService = ragService;
        _sourceTracker = sourceTracker;
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<KnowledgeSystem> BuildAsync(string bookRootId, string rootPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始构建知识体系: {BookRootId}", bookRootId);

        var knowledgeSystem = new KnowledgeSystem
        {
            BookRootId = bookRootId
        };

        List<Document> documents = new();

        try
        {
            // 1. 扫描文档
            _logger.LogInformation("扫描文档目录: {RootPath}", rootPath);
            documents = await _scannerService.ScanAsync(rootPath, cancellationToken);

            if (documents.Count == 0)
            {
                _logger.LogWarning("未找到任何 Markdown 文档");
                return knowledgeSystem;
            }

            // 2. 插入 RAG 并跟踪原文
            _logger.LogInformation("插入文档到 RAG");
            foreach (var doc in documents)
            {
                doc.BookRootId = bookRootId;
                var content = ReconstructDocumentContent(doc);
                await _ragService.InsertAsync(doc.DocId, content, new Dictionary<string, object>
                {
                    ["filePath"] = doc.Path,
                    ["title"] = doc.Title
                });

                // 同时跟踪原文片段
                await _sourceTracker.TrackAsync(doc.DocId, content, new Dictionary<string, object>
                {
                    ["filePath"] = doc.Path,
                    ["title"] = doc.Title
                });
            }

            // 3. 调用 LLM 提取知识点
            _logger.LogInformation("调用 LLM 提取知识点");
            var knowledgePoints = await ExtractKnowledgePointsAsync(documents, cancellationToken);
            knowledgeSystem.KnowledgePoints = knowledgePoints;

            // 4. 为每个知识点预生成学习内容
            _logger.LogInformation("为知识点预生成学习内容");
            await GenerateLearningContentForPointsAsync(knowledgePoints, cancellationToken);

            // 5. 构建知识点关联关系
            _logger.LogInformation("构建知识点关联关系");
            await BuildKnowledgeRelationsAsync(knowledgePoints, cancellationToken);

            // 6. 构建知识树
            _logger.LogInformation("构建知识树");
            knowledgeSystem.Tree = BuildKnowledgeTree(knowledgePoints);

            // 7. 收集所有原文片段
            _logger.LogInformation("收集原文片段");
            foreach (var kp in knowledgePoints)
            {
                foreach (var snippetId in kp.SnippetIds)
                {
                    var snippet = _sourceTracker.GetSource(snippetId);
                    if (snippet != null && !knowledgeSystem.Snippets.ContainsKey(snippetId))
                    {
                        knowledgeSystem.Snippets[snippetId] = snippet;
                    }
                }
            }

            _logger.LogInformation("知识体系构建完成，共 {Count} 个知识点", knowledgePoints.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "知识体系构建失败");
            // 降级：返回按文件/标题的目录树
            knowledgeSystem = CreateFallbackKnowledgeSystem(bookRootId, documents);
        }

        return knowledgeSystem;
    }

    private async Task<List<KnowledgePoint>> ExtractKnowledgePointsAsync(
        List<Document> documents,
        CancellationToken cancellationToken)
    {
        // 获取文档的 chunk ID 列表
        var documentChunkIds = documents.ToDictionary(
            d => d.DocId,
            d => d.DocId + "_chunk_0"
        );

        // 构建 chunk ID 列表说明
        var chunkIdList = new System.Text.StringBuilder();
        chunkIdList.AppendLine("可用原文片段 ID（chunk_id）：");
        if (documentChunkIds.Count > 0)
        {
            foreach (var kv in documentChunkIds)
            {
                chunkIdList.AppendLine("  - " + kv.Key + " -> " + kv.Value);
            }
        }
        else
        {
            chunkIdList.AppendLine("  - {docId}_chunk_0 (默认)");
        }

        var systemPrompt = @"你是一个知识提取专家。你的任务是从文档中提取可学习的知识点。

请以 JSON 格式输出，结构如下：
{
  ""schema_version"": ""1.0"",
  ""knowledge_points"": [
    {
      ""kp_id"": ""唯一的知识点ID"",
      ""title"": ""知识点标题"",
      ""type"": ""concept|chapter|process|api|bestPractice"",
      ""aliases"": [""别名1"", ""别名2""],
      ""chapter_path"": [""章节1"", ""章节2""],
      ""importance"": 0.0-1.0,
      ""snippet_ids"": [""chunk_id1"", ""chunk_id2""],
      ""summary"": ""一句话总结（必须填写）"",
      ""doc_id"": ""来源文档ID""
    }
  ]
}

" + chunkIdList.ToString() + @"
知识点类型说明（type 字段）：
- concept: 概念、定义、术语、理论
- chapter: 章节标题节点
- process: 流程、步骤、操作方法
- api: API、接口、方法签名
- bestPractice: 最佳实践、建议

知识点识别规则：
1. 识别文档中的概念、术语、规则、步骤、API 等
2. 每个知识点必须至少关联一个原文片段（使用上面的 chunk_id）
3. importance 反映知识点的重要程度（核心概念=0.8+, 细节=0.5, 边缘=0.3）
4. 尽量使用原文中的表述作为标题
5. snippet_ids 必须使用上述可用的 chunk_id 格式
6. summary 必须填写，不能为空
7. type 字段必须填写且有效
8. doc_id 必须填写，标识知识点来源的文档

自检要求（生成后请检查）：
- knowledge_points 不能为空，如果确实没有知识点请返回空数组并说明原因
- 每个知识点的 snippet_ids 至少包含 1 个 ID
- 每个知识点的 type 必须是有效的类型之一
- 所有标题必须非空且唯一（如果重复请合并）";

        var documentContent = string.Join("\n\n", documents.Select(d =>
            $"# {d.Title}\n{ReconstructDocumentContent(d)}"));

        _logger.LogInformation("文档总字符数: {CharCount}", documentContent.Length);
        _logger.LogInformation("开始调用 LLM 提取知识点...");

        try
        {
            // 处理长文档：基于文档结构拆分成多个部分
            const int maxContentLength = 10000; // 设置最大内容长度
            var knowledgePoints = new List<KnowledgePointDto>();
            
            if (documentContent.Length > maxContentLength)
            {
                _logger.LogInformation("文档过长，开始基于结构拆分处理...");
                
                // 基于文档结构拆分成多个部分
                var parts = SplitDocumentByStructure(documents, maxContentLength);
                
                for (int i = 0; i < parts.Count; i++)
                {
                    _logger.LogInformation("处理文档部分 {PartIndex}/{TotalParts}, 字符数: {CharCount}", 
                        i + 1, parts.Count, parts[i].Length);
                    
                    var partResponse = await _llmService.ChatJsonAsync<KnowledgePointsResponse>(
                        systemPrompt,
                        $"请分析以下文档部分并提取知识点：\n\n{parts[i]}",
                        cancellationToken);
                    
                    if (partResponse?.KnowledgePoints != null && partResponse.KnowledgePoints.Count > 0)
                    {
                        _logger.LogInformation("部分 {PartIndex} 提取到 {Count} 个知识点", 
                            i + 1, partResponse.KnowledgePoints.Count);
                        knowledgePoints.AddRange(partResponse.KnowledgePoints);
                    }
                }
                
                _logger.LogInformation("基于结构拆分处理完成，共提取到 {Count} 个知识点", knowledgePoints.Count);
            }
            else
            {
                // 文档长度适中，直接处理
                var response = await _llmService.ChatJsonAsync<KnowledgePointsResponse>(
                    systemPrompt,
                    $"请分析以下文档并提取知识点：\n\n{documentContent}",
                    cancellationToken);

                _logger.LogInformation("LLM 调用完成，提取到 {Count} 个知识点", response?.KnowledgePoints?.Count ?? 0);
                
                if (response?.KnowledgePoints != null)
                {
                    knowledgePoints = response.KnowledgePoints;
                }
            }

            // 自检：验证响应数据
            if (knowledgePoints == null || knowledgePoints.Count == 0)
            {
                _logger.LogWarning("LLM 返回的知识点数为0，可能需要调整 prompt 或提供更完整的文档");
            }
            else
            {
                // 校验并清理知识点
                var validPoints = new List<KnowledgePointDto>();
                var seenTitles = new HashSet<string>();

                foreach (var kp in knowledgePoints)
                {
                    // 校验 snippet_ids
                    if (kp.SnippetIds == null || kp.SnippetIds.Count == 0)
                    {
                        _logger.LogWarning("知识点 '{Title}' 缺少 snippet_ids，已跳过", kp.Title);
                        continue;
                    }

                    // 校验知识点类型
                    if (string.IsNullOrEmpty(kp.Type) || !IsValidKpType(kp.Type))
                    {
                        _logger.LogWarning("知识点 '{Title}' 类型无效 '{Type}'，已跳过", kp.Title, kp.Type);
                        continue;
                    }

                    // 校验并去重标题
                    var normalizedTitle = kp.Title?.Trim() ?? "";
                    if (string.IsNullOrEmpty(normalizedTitle))
                    {
                        _logger.LogWarning("知识点缺少标题，已跳过");
                        continue;
                    }

                    if (seenTitles.Contains(normalizedTitle))
                    {
                        _logger.LogWarning("知识点标题重复 '{Title}'，已跳过", normalizedTitle);
                        continue;
                    }

                    seenTitles.Add(normalizedTitle);
                    validPoints.Add(kp);
                }

                _logger.LogInformation("自检完成，有效知识点: {ValidCount}/{TotalCount}", validPoints.Count, knowledgePoints.Count);

                // 将 DTO 转换为标准的 KnowledgePoint，截取章节路径到前两层
                var kpList = validPoints
                    .Select((kp, index) =>
                    {
                        var kpModel = kp.ToKnowledgePoint();
                        kpModel.KpId = $"kp_{index:D4}";
                        kpModel.BookRootId = documents.FirstOrDefault()?.BookRootId ?? string.Empty;
                        // 截取章节路径到前两层
                        if (kpModel.ChapterPath.Count > 2)
                        {
                            kpModel.ChapterPath = kpModel.ChapterPath.Take(2).ToList();
                        }
                        return kpModel;
                    })
                    .ToList();

                return kpList;
            }

            return new List<KnowledgePoint>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用 LLM 提取知识点失败");
            return new List<KnowledgePoint>();
        }
    }

    /// <summary>
    /// 基于文档结构拆分内容为多个部分
    /// </summary>
    private List<string> SplitDocumentByStructure(List<Document> documents, int maxLength)
    {
        var parts = new List<string>();
        var currentPart = new System.Text.StringBuilder();

        foreach (var doc in documents)
        {
            // 添加文档标题
            currentPart.AppendLine($"# {doc.Title}");
            currentPart.AppendLine();

            // 遍历文档的所有章节
            foreach (var section in doc.Sections)
            {
                // 构建章节标题路径
                var headingPath = string.Join(" > ", section.HeadingPath);
                var headingLevel = section.HeadingPath.Count;
                var headingPrefix = new string('#', headingLevel + 1); // +1 因为文档标题是 H1
                
                // 添加章节标题
                var sectionContent = new System.Text.StringBuilder();
                sectionContent.AppendLine($"{headingPrefix} {section.HeadingPath.Last()}");
                sectionContent.AppendLine();

                // 章节内容已不再提取，Paragraphs 为空
                // foreach (var paragraph in section.Paragraphs)
                // {
                //     sectionContent.AppendLine(paragraph.Content);
                // }
                sectionContent.AppendLine();

                // 检查当前部分加上新章节是否会超过最大长度
                if (currentPart.Length + sectionContent.Length > maxLength)
                {
                    // 如果当前部分不为空，添加到结果中
                    if (currentPart.Length > 0)
                    {
                        parts.Add(currentPart.ToString());
                        currentPart.Clear();
                        // 重新添加文档标题
                        currentPart.AppendLine($"# {doc.Title}");
                        currentPart.AppendLine();
                    }
                }

                // 检查章节本身是否超过最大长度
                if (sectionContent.Length > maxLength)
                {
                    // 如果章节本身超过最大长度，将其拆分为多个部分
                    var sectionParts = SplitLongSection(sectionContent.ToString(), maxLength);
                    foreach (var part in sectionParts)
                    {
                        // 为每个部分添加文档标题和章节标题
                        var partWithHeading = new System.Text.StringBuilder();
                        partWithHeading.AppendLine($"# {doc.Title}");
                        partWithHeading.AppendLine($"{headingPrefix} {section.HeadingPath.Last()}");
                        partWithHeading.AppendLine();
                        partWithHeading.Append(part);
                        parts.Add(partWithHeading.ToString());
                    }
                }
                else
                {
                    // 将章节添加到当前部分
                    currentPart.Append(sectionContent);
                }
            }
        }

        // 添加最后一个部分
        if (currentPart.Length > 0)
        {
            parts.Add(currentPart.ToString());
        }

        return parts;
    }

    /// <summary>
    /// 拆分长章节为多个部分
    /// </summary>
    private List<string> SplitLongSection(string content, int maxLength)
    {
        var parts = new List<string>();
        var currentPosition = 0;

        while (currentPosition < content.Length)
        {
            // 计算当前部分的结束位置
            var endPosition = Math.Min(currentPosition + maxLength, content.Length);
            
            // 尝试在段落边界处拆分
            if (endPosition < content.Length)
            {
                // 查找最近的换行符
                var lastNewline = content.LastIndexOf("\n\n", endPosition);
                if (lastNewline > currentPosition + maxLength / 2) // 确保拆分点不是太靠近当前位置
                {
                    endPosition = lastNewline;
                }
                else
                {
                    // 查找单个换行符
                    var lastSingleNewline = content.LastIndexOf("\n", endPosition);
                    if (lastSingleNewline > currentPosition + maxLength / 2)
                    {
                        endPosition = lastSingleNewline;
                    }
                }
            }
            
            // 添加当前部分
            parts.Add(content.Substring(currentPosition, endPosition - currentPosition));
            currentPosition = endPosition;
        }

        return parts;
    }

    /// <summary>
    /// 拆分文档内容为多个部分（备用方法）
    /// </summary>
    private List<string> SplitDocumentContent(string content, int maxLength)
    {
        var parts = new List<string>();
        var currentPosition = 0;

        while (currentPosition < content.Length)
        {
            // 计算当前部分的结束位置
            var endPosition = Math.Min(currentPosition + maxLength, content.Length);
            
            // 尝试在段落边界处拆分
            if (endPosition < content.Length)
            {
                // 查找最近的换行符
                var lastNewline = content.LastIndexOf("\n\n", endPosition);
                if (lastNewline > currentPosition + maxLength / 2) // 确保拆分点不是太靠近当前位置
                {
                    endPosition = lastNewline;
                }
                else
                {
                    // 查找单个换行符
                    var lastSingleNewline = content.LastIndexOf("\n", endPosition);
                    if (lastSingleNewline > currentPosition + maxLength / 2)
                    {
                        endPosition = lastSingleNewline;
                    }
                }
            }
            
            // 添加当前部分
            parts.Add(content.Substring(currentPosition, endPosition - currentPosition));
            currentPosition = endPosition;
        }

        return parts;
    }

    private static bool IsValidKpType(string type)
    {
        var validTypes = new[] { "concept", "chapter", "process", "api", "bestPractice" };
        return validTypes.Contains(type.ToLowerInvariant());
    }

    private async Task BuildKnowledgeRelationsAsync(
        List<KnowledgePoint> knowledgePoints,
        CancellationToken cancellationToken)
    {
        if (knowledgePoints.Count < 2)
        {
            _logger.LogInformation("知识点数量不足，跳过关联关系构建");
            return;
        }

        _logger.LogInformation("开始构建 {Count} 个知识点的关联关系", knowledgePoints.Count);

        try
        {
            // 构建知识点索引（标题 -> 知识点）
            var kpIndex = knowledgePoints.ToDictionary(kp => kp.Title.ToLowerInvariant(), kp => kp);

            // 为每个知识点分析关联关系
            foreach (var kp in knowledgePoints)
            {
                if (kp.Summary == null || string.IsNullOrEmpty(kp.Summary.Definition))
                {
                    continue;
                }

                // 使用 LLM 分析可能的关联关系
                var relations = await FindRelationsForKnowledgePointAsync(kp, knowledgePoints, cancellationToken);

                foreach (var relation in relations)
                {
                    if (kpIndex.TryGetValue(relation.TargetTitle.ToLowerInvariant(), out var targetKp))
                    {
                        // 解析关系类型
                        if (!Enum.TryParse<RelationType>(relation.Type, ignoreCase: true, out var relationType))
                        {
                            continue; // 无效的关系类型
                        }

                        // 检查是否已存在相同关系
                        if (!kp.Relations.Any(r => r.ToKpId == targetKp.KpId && r.Type == relationType))
                        {
                            kp.Relations.Add(new KnowledgeRelation
                            {
                                ToKpId = targetKp.KpId,
                                Type = relationType,
                                Description = relation.Description
                            });
                        }
                    }
                }
            }

            _logger.LogInformation("关联关系构建完成");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "构建关联关系时发生错误");
        }
    }

    private async Task<List<KnowledgeRelationDto>> FindRelationsForKnowledgePointAsync(
        KnowledgePoint kp,
        List<KnowledgePoint> allPoints,
        CancellationToken cancellationToken)
    {
        var systemPrompt = @"你是一个知识图谱专家。请分析当前知识点与其他知识点之间的关联关系。

请以 JSON 格式输出：
{
  ""relations"": [
    {
      ""target_title"": ""关联目标的知识点的完整标题"",
      ""relation_type"": ""prerequisite|contrast|contains|related|similar"",
      ""description"": ""关系描述""
    }
  ]
}

关系类型说明：
- prerequisite: 前置依赖（学习当前知识点前应先掌握）
- contrast: 对比关系（与关联知识点进行对比学习）
- contains: 包含/组成（关联知识点是当前知识点的组成部分）
- related: 一般关联（存在某种关联关系）
- similar: 相似关系（两者相似但有区别）

注意：
1. 只返回确实存在关联关系的知识点
2. target_title 必须与提供的知识点标题完全匹配
3. 最多返回 5 个最重要的关联关系
4. 如果没有明显关联，返回空数组";

        var allTitles = string.Join("\n", allPoints.Select(p => $"- {p.Title}"));
        var userMessage = $"当前知识点：{kp.Title}\n" +
                          $"定义：{kp.Summary?.Definition ?? "无"}\n" +
                          $"要点：{string.Join(", ", kp.Summary?.KeyPoints ?? new())}\n\n" +
                          $"所有知识点：\n{allTitles}";

        try
        {
            var response = await _llmService.ChatJsonAsync<RelationsResponse>(
                systemPrompt,
                userMessage,
                cancellationToken);

            return response?.Relations ?? new List<KnowledgeRelationDto>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "查找关联关系失败: {KpId}", kp.KpId);
            return new List<KnowledgeRelationDto>();
        }
    }

    private async Task GenerateLearningContentForPointsAsync(List<KnowledgePoint> knowledgePoints, CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始为 {Count} 个知识点生成学习内容", knowledgePoints.Count);

        foreach (var kp in knowledgePoints)
        {
            try
            {
                _logger.LogInformation("生成学习内容: {KpId}", kp.KpId);

                // 获取原文片段
                var snippets = _sourceTracker.GetSources(kp.SnippetIds);
                var snippetTexts = string.Join("\n\n", snippets.Select(s => s.Content));

                // 生成学习内容
                var content = await GenerateLearningContentAsync(kp, snippetTexts, cancellationToken);

                if (content != null)
                {
                    kp.Summary = content.Summary;
                    kp.Levels = content.Levels;
                }
                else
                {
                    // 降级：设置基本内容
                    kp.Summary = new Summary
                    {
                        Definition = $"这是关于 {kp.Title} 的知识点，位于 {string.Join(" > ", kp.ChapterPath)} 章节。",
                        KeyPoints = new List<string> { "内容生成失败，请查看原文" },
                        Pitfalls = new List<string>()
                    };
                    kp.Levels = new List<ContentLevel>
                    {
                        new ContentLevel { Level = 1, Title = "概览", Content = "无法生成层次化内容，请查看原文片段" }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "知识点 {KpId} 学习内容生成失败，使用降级内容", kp.KpId);
                // 降级：设置基本内容
                kp.Summary = new Summary
                {
                    Definition = $"这是关于 {kp.Title} 的知识点，位于 {string.Join(" > ", kp.ChapterPath)} 章节。",
                    KeyPoints = new List<string> { "内容生成失败，请查看原文" },
                    Pitfalls = new List<string>()
                };
                kp.Levels = new List<ContentLevel>
                {
                    new ContentLevel { Level = 1, Title = "概览", Content = "无法生成层次化内容，请查看原文片段" }
                };
            }
        }

        _logger.LogInformation("学习内容生成完成");
    }

    private async Task<LearningContentDto?> GenerateLearningContentAsync(
        KnowledgePoint kp,
        string snippetTexts,
        CancellationToken cancellationToken)
    {
        var systemPrompt = @"你是一个专业的学习内容生成专家。你的任务是为用户生成结构化的学习内容。

请以 JSON 格式输出，结构如下：
{
  ""summary"": {
    ""definition"": ""知识点的精确定义（1-3句，必须填写）"",
    ""key_points"": [""核心要点1"", ""核心要点2"", ""核心要点3""],
    ""pitfalls"": [""常见误区1"", ""常见误区2""]
  },
  ""levels"": [
    {
      ""level"": 1,
      ""title"": ""概览"",
      ""content"": ""面向第一次接触的简要介绍""
    },
    {
      ""level"": 2,
      ""title"": ""详细"",
      ""content"": ""解释关键机制、步骤、例子""
    },
    {
      ""level"": 3,
      ""title"": ""深入"",
      ""content"": ""边界条件、对比、推导""
    }
  ]
}

生成原则：
1. 只基于提供的原文片段，不引入外部知识
2. 定义要简洁准确，要点要清晰实用
3. 常见误区要具体且有针对性
4. 层次化内容要循序渐进
5. summary.definition 必须填写，不能为空

自检要求：
- summary.definition 不能为空
- levels 至少包含 level=1 的内容
- 如果无法生成有效内容，请返回空对象 {} 而非报错";

        var userMessage = $"知识点标题：{kp.Title}\n" +
                          $"知识点类型：{kp.Type}\n" +
                          $"所属章节：{string.Join(" > ", kp.ChapterPath)}\n" +
                          $"相关原文片段：\n{snippetTexts}";

        return await _llmService.ChatJsonAsync<LearningContentDto>(
            systemPrompt,
            userMessage,
            cancellationToken);
    }

    private string ReconstructDocumentContent(Document doc)
    {
        var sb = new System.Text.StringBuilder();

        foreach (var section in doc.Sections)
        {
            if (section.HeadingPath.Count > 0)
            {
                sb.AppendLine($"# {string.Join(" > ", section.HeadingPath)}");
            }

            // 章节内容已不再提取，Paragraphs 为空
            // foreach (var para in section.Paragraphs)
            // {
            //     sb.AppendLine(para.Content);
            // }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static KnowledgeTreeNode BuildKnowledgeTree(List<KnowledgePoint> knowledgePoints)
    {
        var root = new KnowledgeTreeNode
        {
            Id = "root",
            Title = "根",
            HeadingPath = new List<string>()
        };

        foreach (var kp in knowledgePoints)
        {
            var current = root;

            // 沿着章节路径导航
            foreach (var chapter in kp.ChapterPath)
            {
                var existingChild = current.Children.FirstOrDefault(c => c.Title == chapter);
                if (existingChild == null)
                {
                    var newNode = new KnowledgeTreeNode
                    {
                        Id = $"{current.Id}_{chapter}",
                        Title = chapter,
                        HeadingPath = new List<string>(current.HeadingPath) { chapter }
                    };
                    current.Children.Add(newNode);
                    current = newNode;
                }
                else
                {
                    current = existingChild;
                }
            }

            // 在章节节点下添加知识点
            current.KnowledgePoint = kp;
        }

        return root;
    }

    private KnowledgeSystem CreateFallbackKnowledgeSystem(string bookRootId, List<Document> documents)
    {
        var knowledgeSystem = new KnowledgeSystem
        {
            BookRootId = bookRootId
        };

        // 从文档标题创建临时知识点
        var id = 0;
        foreach (var doc in documents)
        {
            var kp = new KnowledgePoint
            {
                KpId = $"kp_{id++:D4}",
                BookRootId = bookRootId,
                Title = doc.Title,
                Type = KpType.Chapter,
                ChapterPath = new List<string> { doc.Title },
                Importance = 0.5f,
                SnippetIds = new List<string> { $"{doc.DocId}_chunk_0" },
                DocId = doc.DocId
            };
            knowledgeSystem.KnowledgePoints.Add(kp);
        }

        knowledgeSystem.Tree = BuildKnowledgeTree(knowledgeSystem.KnowledgePoints);

        return knowledgeSystem;
    }
}
