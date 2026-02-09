using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using ASimpleTutor.Core.Models.Dto;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace ASimpleTutor.Core.Services;

/// <summary>
/// 知识体系构建服务
/// </summary>
public class KnowledgeBuilder : IKnowledgeBuilder
{
    private readonly IScannerService _scannerService;
    private readonly ILLMService _llmService;
    private readonly ILogger<KnowledgeBuilder> _logger;

    public KnowledgeBuilder(
        IScannerService scannerService,
        ILLMService llmService,
        ILogger<KnowledgeBuilder> logger)
    {
        _scannerService = scannerService;
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<(KnowledgeSystem KnowledgeSystem, List<Document> Documents)> BuildAsync(string bookHubId, string rootPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始构建知识体系: {BookHubId}", bookHubId);

        var knowledgeSystem = new KnowledgeSystem
        {
            BookHubId = bookHubId
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
                return (knowledgeSystem, documents);
            }

            // 2. 调用 LLM 提取知识点
            _logger.LogInformation("调用 LLM 提取知识点");
            var (knowledgePoints, snippets) = await ExtractKnowledgePointsAsync(documents, cancellationToken);
            knowledgeSystem.KnowledgePoints = knowledgePoints;
            knowledgeSystem.Snippets = snippets;

            // 4. 为每个知识点预生成学习内容
            _logger.LogInformation("为知识点预生成学习内容");
            await GenerateLearningContentForPointsAsync(knowledgePoints, snippets, cancellationToken);


            // 6. 构建知识树
            _logger.LogInformation("构建知识树");
            knowledgeSystem.Tree = BuildKnowledgeTree(knowledgePoints);

            _logger.LogInformation("知识体系构建完成，共 {Count} 个知识点", knowledgePoints.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "知识体系构建失败");
            // 降级：返回按文件/标题的目录树
            knowledgeSystem = CreateFallbackKnowledgeSystem(bookHubId, documents);
        }

        return (knowledgeSystem, documents);
    }

    private async Task<(List<KnowledgePoint> KnowledgePoints, Dictionary<string, SourceSnippet> Snippets)> ExtractKnowledgePointsAsync(
        List<Document> documents,
        CancellationToken cancellationToken)
    {
        var snippets = new Dictionary<string, SourceSnippet>();

        // 为每个文档的每个 Section 创建唯一的 chunk ID
        var sectionChunkIds = new Dictionary<string, string>();
        var sectionContentMap = new Dictionary<string, string>();
        var sectionDocMap = new Dictionary<string, string>(); // 记录 section chunk ID 对应的文档 ID
        var sectionPathMap = new Dictionary<string, List<string>>(); // 记录 section chunk ID 对应的章节路径
        var sectionLineRangeMap = new Dictionary<string, (int startLine, int endLine)>(); // 记录 section chunk ID 对应的行号范围

        // 构建 section chunk ID 和内容映射
        foreach (var doc in documents)
        {
            if (doc.Sections != null && doc.Sections.Count > 0)
            {
                // 使用深度优先搜索遍历所有层级的章节
                TraverseSectionsAsync(doc, doc.Sections, sectionChunkIds, sectionContentMap, sectionDocMap, sectionPathMap, sectionLineRangeMap);
            }
            else
            {
                // 如果文档没有 Sections，为整个文档创建一个 chunk ID
                var docId = $"{doc.DocId}_chunk_0";
                sectionChunkIds[docId] = docId;
                sectionDocMap[docId] = doc.DocId;
                sectionPathMap[docId] = new List<string> { doc.Title };
                sectionContentMap[docId] = ReconstructDocumentContent(doc);
                sectionLineRangeMap[docId] = (0, 0);
            }
        }

        // 为每个 section 创建 SourceSnippet 并添加到 snippets 字典
        foreach (var sectionId in sectionChunkIds.Keys)
        {
            var docId = sectionDocMap[sectionId];
            var sectionPath = sectionPathMap[sectionId];
            var sectionContent = sectionContentMap[sectionId];
            var (startLine, endLine) = sectionLineRangeMap[sectionId];

            // 查找对应的文档
            var doc = documents.FirstOrDefault(d => d.DocId == docId);
            if (doc != null)
            {
                // 创建 SourceSnippet
                var snippet = new SourceSnippet
                {
                    SnippetId = sectionId,
                    BookHubId = doc.BookHubId,
                    DocId = docId,
                    FilePath = doc.Path,
                    HeadingPath = sectionPath,
                    Content = sectionContent,
                    StartLine = startLine,
                    EndLine = endLine,
                    ChunkId = sectionId
                };

                // 添加到 snippets 字典
                snippets[sectionId] = snippet;
            }
        }

        // 构建 chunk ID 列表说明
        var chunkIdList = new System.Text.StringBuilder();
        chunkIdList.AppendLine("可用原文片段 ID（chunk_id）：");
        if (sectionChunkIds.Count > 0)
        {
            foreach (var kv in sectionChunkIds)
            {
                var sectionPath = sectionPathMap.TryGetValue(kv.Key, out var path) ? string.Join(" > ", path) : "";
                chunkIdList.AppendLine($"  - {kv.Key} (章节: {sectionPath})");
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
      ""aliases"": [""别名1"", ""别名2""] ,
      ""chapter_path"": [""章节1"", ""章节2"", ""章节3""] ,
      ""importance"": 0.0-1.0,
      ""snippet_ids"": [""chunk_id1"", ""chunk_id2""] ,
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
9. chapter_path 必须反映知识点所在的完整章节层次结构

自检要求（生成后请检查）：
- knowledge_points 不能为空，如果确实没有知识点请返回空数组并说明原因
- 每个知识点的 snippet_ids 至少包含 1 个 ID
- 每个知识点的 type 必须是有效的类型之一
- 所有标题必须非空且唯一（如果重复请合并）";

        var knowledgePoints = new List<KnowledgePointDto>();

        try
        {
            // 对每个 section 单独处理，提取知识点
            _logger.LogInformation("开始基于 Section 结构提取知识点，共 {SectionCount} 个 Section", sectionChunkIds.Count);

            foreach (var sectionId in sectionChunkIds.Keys)
            {
                var sectionContent = sectionContentMap[sectionId];
                var sectionPath = sectionPathMap[sectionId];
                var docId = sectionDocMap[sectionId];

                _logger.LogInformation("处理 Section: {SectionPath}, 字符数: {CharCount}", string.Join(" > ", sectionPath), sectionContent.Length);

                // 调用 LLM 提取当前 section 的知识点
                var response = await _llmService.ChatJsonAsync<KnowledgePointsResponse>(
                    systemPrompt,
                    $"请分析以下章节内容并提取知识点：\n\n{sectionContent}",
                    cancellationToken);

                if (response?.KnowledgePoints != null && response.KnowledgePoints.Count > 0)
                {
                    _logger.LogInformation("Section {SectionPath} 提取到 {Count} 个知识点", string.Join(" > ", sectionPath), response.KnowledgePoints.Count);

                    // 为每个知识点添加正确的文档 ID 和章节路径
                    foreach (var kp in response.KnowledgePoints)
                    {
                        kp.DocId = docId;
                        // 如果 LLM 没有提供章节路径，使用 section 的路径
                        if (kp.ChapterPath == null || kp.ChapterPath.Count == 0)
                        {
                            kp.ChapterPath = sectionPath;
                        }
                    }

                    knowledgePoints.AddRange(response.KnowledgePoints);
                }
            }

            _logger.LogInformation("基于 Section 结构提取完成，共提取到 {Count} 个知识点", knowledgePoints.Count);

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

                // 将 DTO 转换为标准的 KnowledgePoint
                var kpList = validPoints
                    .Select((kp, index) =>
                    {
                        var kpModel = kp.ToKnowledgePoint();
                        kpModel.KpId = $"kp_{index:D4}";
                        kpModel.BookHubId = documents.FirstOrDefault()?.BookHubId ?? string.Empty;
                        // 保留完整的章节路径
                        return kpModel;
                    })
                    .ToList();

                return (kpList, snippets);
            }

            return (new List<KnowledgePoint>(), snippets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用 LLM 提取知识点失败");
            return (new List<KnowledgePoint>(), snippets);
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
            // 读取文档文件的所有行
            string[] docLines = Array.Empty<string>();
            if (File.Exists(doc.Path))
            {
                docLines = File.ReadAllLines(doc.Path);
            }
            
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

                // 从文件中提取章节内容
                if (docLines.Length > 0 && section.StartLine >= 0 && section.EndLine <= docLines.Length)
                {
                    for (int i = section.StartLine; i < section.EndLine; i++)
                    {
                        sectionContent.AppendLine(docLines[i]);
                    }
                }
                else
                {
                    _logger.LogWarning("无法提取章节内容: DocLines={DocLines}, StartLine={StartLine}, EndLine={EndLine}", 
                        docLines.Length, section.StartLine, section.EndLine);
                }
                sectionContent.AppendLine();

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
                    endPosition = lastNewline + 2; // 跳过 "\n\n"
                }
                else
                {
                    // 查找单个换行符
                    var lastSingleNewline = content.LastIndexOf("\n", endPosition);
                    if (lastSingleNewline > currentPosition + maxLength / 2)
                    {
                        endPosition = lastSingleNewline + 1; // 跳过 "\n"
                    }
                }
            }
            
            // 添加当前部分
            var part = content.Substring(currentPosition, endPosition - currentPosition);
            parts.Add(part);
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
                    endPosition = lastNewline + 2; // 跳过 "\n\n"
                }
                else
                {
                    // 查找单个换行符
                    var lastSingleNewline = content.LastIndexOf("\n", endPosition);
                    if (lastSingleNewline > currentPosition + maxLength / 2)
                    {
                        endPosition = lastSingleNewline + 1; // 跳过 "\n"
                    }
                }
            }
            
            // 添加当前部分
            var part = content.Substring(currentPosition, endPosition - currentPosition);
            parts.Add(part);
            currentPosition = endPosition;
        }

        return parts;
    }

    private static bool IsValidKpType(string type)
    {
        var validTypes = new[] { "concept", "chapter", "process", "api", "bestPractice" };
        return validTypes.Contains(type.ToLowerInvariant());
    }

    private async Task GenerateLearningContentForPointsAsync(
        List<KnowledgePoint> knowledgePoints,
        Dictionary<string, SourceSnippet> snippets,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始为 {Count} 个知识点生成学习内容", knowledgePoints.Count);

        foreach (var kp in knowledgePoints)
        {
            try
            {
                _logger.LogInformation("生成学习内容: {KpId}", kp.KpId);

                // 获取原文片段
                var kpSnippets = kp.SnippetIds
                    .Select(id => snippets.TryGetValue(id, out var snippet) ? snippet : null)
                    .Where(s => s != null)
                    .Cast<SourceSnippet>()
                    .ToList();
                var snippetTexts = string.Join("\n\n", kpSnippets.Select(s => s.Content));

                // 检查 snippetTexts 是否为空或长度小于 100
                if (string.IsNullOrEmpty(snippetTexts) || snippetTexts.Length < 100)
                {
                    _logger.LogError("原文片段为空或长度不足: {KpId}, 长度: {Length}", kp.KpId, snippetTexts?.Length ?? 0);
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
                    continue;
                }

                // 生成学习内容
                var learningPack = await GenerateLearningContentAsync(kp, snippetTexts, cancellationToken);

                if (learningPack != null)
                {
                    kp.Summary = learningPack.Summary;
                    kp.Levels = learningPack.Levels;
                    kp.SlideCards = learningPack.SlideCards;
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

    /// <summary>
    /// 使用深度优先搜索遍历所有层级的章节，只添加叶子节点到处理列表
    /// </summary>
    private void TraverseSectionsAsync(
        Document doc,
        List<Section> sections,
        Dictionary<string, string> sectionChunkIds,
        Dictionary<string, string> sectionContentMap,
        Dictionary<string, string> sectionDocMap,
        Dictionary<string, List<string>> sectionPathMap,
        Dictionary<string, (int startLine, int endLine)> sectionLineRangeMap)
    {
        foreach (var section in sections)
        {
            // 递归处理子章节
            if (section.SubSections != null && section.SubSections.Count > 0)
            {
                TraverseSectionsAsync(doc, section.SubSections, sectionChunkIds, sectionContentMap, sectionDocMap, sectionPathMap, sectionLineRangeMap);
                // 有子章节，当前节点不单独处理
            }
            else
            {
                // 没有子章节，添加到处理列表
                var sectionId = $"{doc.DocId}_section_{section.StartLine}_{section.EndLine}";
                sectionChunkIds[sectionId] = sectionId;
                sectionDocMap[sectionId] = doc.DocId;
                sectionPathMap[sectionId] = section.HeadingPath;
                sectionLineRangeMap[sectionId] = (section.StartLine, section.EndLine);

                // 构建 section 内容
                var sectionContent = new System.Text.StringBuilder();
                var headingLevel = section.HeadingPath.Count;
                var headingPrefix = new string('#', headingLevel + 1); // +1 因为文档标题是 H1
                sectionContent.AppendLine($"{headingPrefix} {section.HeadingPath.Last()}");
                sectionContent.AppendLine();

                // 从文件中提取章节内容
                if (File.Exists(doc.Path))
                {
                    var docLines = File.ReadAllLines(doc.Path);
                    if (docLines.Length > 0 && section.StartLine >= 0 && section.EndLine <= docLines.Length)
                    {
                        for (int i = section.StartLine; i < section.EndLine; i++)
                        {
                            sectionContent.AppendLine(docLines[i]);
                        }
                    }
                }
                sectionContentMap[sectionId] = sectionContent.ToString();
            }
        }
    }

    private async Task<LearningPack?> GenerateLearningContentAsync(
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
  ],
  ""slide_cards"": [
    {
      ""type"": ""cover|explanation|detail|deepDive|source|quiz|relations|summary"",
      ""order"": 0,
      ""title"": ""卡片标题"",
      ""subtitle"": ""卡片副标题（可选）"",
      ""content"": ""幻灯片内容，可以使用 Markdown 格式"",
      ""kpLinks"": [
        {
          ""text"": ""链接文本"",
          ""targetKpId"": ""目标知识点ID"",
          ""relationship"": ""prerequisite|related|contrast|similar|contains"",
          ""targetTitle"": ""目标知识点标题""
        }
      ],
      ""config"": {
        ""allowSkip"": true,
        ""requireComplete"": false,
        ""estimatedTime"": 60
      }
    }
  ]
}

生成原则：
1. 只基于提供的原文片段，不引入外部知识
2. 定义要简洁准确，要点要清晰实用
3. 常见误区要具体且有针对性
4. 层次化内容要循序渐进
5. 幻灯片卡片要包含 3-5 张，类型要多样化
6. content 可以使用 Markdown 格式
7. summary.definition 必须填写，不能为空

自检要求：
- summary.definition 不能为空
- levels 至少包含 level=1 的内容
- slide_cards 至少包含 1 张卡片
- 如果无法生成有效内容，请返回空对象 {} 而非报错";

        var userMessage = $"知识点标题：{kp.Title}\n" +
                          $"知识点类型：{kp.Type}\n" +
                          $"所属章节：{string.Join(" > ", kp.ChapterPath)}\n" +
                          $"相关原文片段：\n{snippetTexts}";

        var content = await _llmService.ChatJsonAsync<LearningContentDto>(
            systemPrompt,
            userMessage,
            cancellationToken);

        if (content == null)
        {
            return null;
        }

        var learningPack = new LearningPack
        {
            KpId = kp.KpId,
            Summary = content.Summary,
            Levels = content.Levels,
            SnippetIds = kp.SnippetIds,
            RelatedKpIds = new List<string>(),
            SlideCards = content.SlideCards
                .Select((dto, index) => new SlideCard
                {
                    SlideId = dto.SlideId ?? $"{kp.KpId}_slide_{index}",
                    KpId = kp.KpId,
                    Type = ConvertSlideType(dto.Type),
                    Order = dto.Order,
                    Title = dto.Title,
                    HtmlContent = dto.Content,
                    SourceReferences = new List<SourceReference>(),
                    Config = new SlideConfig
                    {
                        AllowSkip = dto.Config?.AllowSkip ?? true,
                        RequireComplete = dto.Config?.RequireComplete ?? false
                    }
                })
                .ToList()
        };

        return learningPack;
    }

    private SlideType ConvertSlideType(SlideTypeDto dtoType)
    {
        return dtoType switch
        {
            SlideTypeDto.Cover => SlideType.Cover,
            SlideTypeDto.Explanation => SlideType.Explanation,
            SlideTypeDto.Detail => SlideType.Explanation,
            SlideTypeDto.DeepDive => SlideType.DeepDive,
            SlideTypeDto.Source => SlideType.Source,
            SlideTypeDto.Quiz => SlideType.Quiz,
            SlideTypeDto.Relations => SlideType.Relations,
            SlideTypeDto.Summary => SlideType.Summary,
            _ => SlideType.Explanation
        };
    }

    private string ReconstructDocumentContent(Document doc)
    {
        // 从文件路径读取原始文档内容
        if (File.Exists(doc.Path))
        {
            var content = File.ReadAllText(doc.Path);
            return content;
        }
        
        // 如果文件不存在，返回空字符串
        _logger.LogWarning("文档文件不存在: {FilePath}", doc.Path);
        return string.Empty;
    }

    public static KnowledgeTreeNode BuildKnowledgeTree(List<KnowledgePoint> knowledgePoints)
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

    private KnowledgeSystem CreateFallbackKnowledgeSystem(string bookHubId, List<Document> documents)
    {
        var knowledgeSystem = new KnowledgeSystem
        {
            BookHubId = bookHubId
        };

        // 从文档标题创建临时知识点
        var id = 0;
        foreach (var doc in documents)
        {
            var kp = new KnowledgePoint
            {
                KpId = $"kp_{id++:D4}",
                BookHubId = bookHubId,
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
