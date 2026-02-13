using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using ASimpleTutor.Core.Models.Dto;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace ASimpleTutor.Core.Services;

/// <summary>
/// 知识体系构建服务
/// </summary>
public class KnowledgeBuilder : IKnowledgeBuilder
{
    private readonly IScannerService _scannerService;
    private readonly ILLMService _llmService;
    private readonly ITtsService _ttsService;
    private readonly ILearningGenerator _learningGenerator;
    private readonly KnowledgeSystemStore _store;
    private readonly ILogger<KnowledgeBuilder> _logger;

    public KnowledgeBuilder(
        IScannerService scannerService,
        ILLMService llmService,
        ITtsService ttsService,
        ILearningGenerator learningGenerator,
        KnowledgeSystemStore store,
        ILogger<KnowledgeBuilder> logger)
    {
        _scannerService = scannerService;
        _llmService = llmService;
        _ttsService = ttsService;
        _learningGenerator = learningGenerator;
        _store = store;
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

            // 过程保存：扫描完成后保存文档信息
            await SaveProgressAsync(knowledgeSystem, documents, "扫描完成", cancellationToken);

            // 2. 调用 LLM 提取知识点
            _logger.LogInformation("调用 LLM 提取知识点");
            var knowledgePoints = await ExtractKnowledgePointsAsync(documents, cancellationToken);
            knowledgeSystem.KnowledgePoints = knowledgePoints;

            // 过程保存：知识点提取完成后保存
            await SaveProgressAsync(knowledgeSystem, documents, "知识点提取完成", cancellationToken);

            // 3. 为每个知识点预生成学习内容
            _logger.LogInformation("为知识点预生成学习内容");
            await GenerateLearningContentForPointsAsync(knowledgePoints, documents, cancellationToken);

            // 过程保存：学习内容生成完成后保存
            await SaveProgressAsync(knowledgeSystem, documents, "学习内容生成完成", cancellationToken);

            // 4. 为幻灯片卡片生成 TTS 音频
            _logger.LogInformation("为幻灯片卡片生成 TTS 音频");
            await GenerateTtsForSlideCardsAsync(knowledgePoints, cancellationToken);

            // 过程保存：TTS 生成完成后保存
            await SaveProgressAsync(knowledgeSystem, documents, "TTS 生成完成", cancellationToken);

            // 5. 构建知识树
            _logger.LogInformation("构建知识树");
            knowledgeSystem.Tree = BuildKnowledgeTree(knowledgePoints);

            // 最终保存：构建完成
            await SaveProgressAsync(knowledgeSystem, documents, "构建完成", cancellationToken);

            _logger.LogInformation("知识体系构建完成，共 {Count} 个知识点", knowledgePoints.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "知识体系构建失败");
            // 尝试保存已完成的进度
            await SaveProgressAsync(knowledgeSystem, documents, "构建失败-保存进度", cancellationToken);
            // 降级：返回按文件/标题的目录树
            knowledgeSystem = CreateFallbackKnowledgeSystem(bookHubId, documents);
        }

        return (knowledgeSystem, documents);
    }

    /// <summary>
    /// 保存构建进度
    /// </summary>
    private async Task SaveProgressAsync(KnowledgeSystem knowledgeSystem, List<Document> documents, string stage, CancellationToken cancellationToken)
    {
        try
        {
            await _store.SaveAsync(knowledgeSystem, documents, cancellationToken);
            _logger.LogInformation("过程保存完成 [{Stage}]: {BookHubId}, 知识点: {KpCount}", 
                stage, knowledgeSystem.BookHubId, knowledgeSystem.KnowledgePoints.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "过程保存失败 [{Stage}]: {BookHubId}", stage, knowledgeSystem.BookHubId);
            // 过程保存失败不影响主流程
        }
    }

    private async Task<List<KnowledgePoint>> ExtractKnowledgePointsAsync(
        List<Document> documents,
        CancellationToken cancellationToken)
    {
        var systemPrompt = @"你是一个知识提取专家。你的任务是从文档中提取可学习的知识点。

请以 JSON 格式输出，结构如下：
{
  ""schema_version"": ""1.0"",
  ""knowledge_points"": [
    {
      ""title"": ""知识点标题"",
      ""type"": ""concept|chapter|process|api|bestPractice"",
      ""aliases"": [""别名1"", ""别名2""] ,
      ""chapter_path"": [""章节1"", ""章节2"", ""章节3""] ,
      ""importance"": 0.0-1.0,
      ""summary"": ""一句话总结（必须填写）"",
    }
  ]
}

知识点类型说明（type 字段）：
- concept: 概念、定义、术语、理论
- chapter: 章节标题节点
- process: 流程、步骤、操作方法
- api: API、接口、方法签名
- bestPractice: 最佳实践、建议

知识点识别规则：
1. 识别文档中的概念、术语、规则、步骤、API 等
2. 每个知识点必须至少关联一个章节路径（使用完整的章节路径，如 ""第一章 概述 > 1.1 简介""）
3. importance 反映知识点的重要程度（核心概念=0.8+, 细节=0.5, 边缘=0.3）
4. 尽量使用原文中的表述作为标题
6. summary 必须填写，不能为空
7. type 字段必须填写且有效
9. chapter_path 必须反映知识点所在的完整章节层次结构

自检要求（生成后请检查）：
- knowledge_points 不能为空，如果确实没有知识点请返回空数组并说明原因
- 每个知识点的 type 必须是有效的类型之一
- 所有标题必须非空且唯一（如果重复请合并）";

        try
        {
            // 遍历所有文档的章节结构
            var allSections = new List<(Document doc, Section section)>();
            foreach (var doc in documents)
            {
                if (doc.Sections != null && doc.Sections.Count > 0)
                {
                    CollectAllSections(doc, doc.Sections, allSections);
                }
            }

            _logger.LogInformation("开始基于 Section 结构提取知识点，共 {SectionCount} 个 Section", allSections.Count);

            // 使用 ConcurrentBag 并发收集所有知识点（线程安全）
            var knowledgePointsBag = new ConcurrentBag<KnowledgePointDto>();

            // 使用 Parallel.ForEachAsync 并发处理所有 Section
            await Parallel.ForEachAsync(allSections,
                new ParallelOptions { CancellationToken = cancellationToken },
                async (item, cancellationToken) =>
                {
                    var (doc, section) = item;
                    var sectionPath = section.HeadingPath;

                    try
                    {
                        var sectionContent = await ReadSectionContentAsync(doc, section, cancellationToken);
                        _logger.LogDebug("处理 Section: {SectionPath}, 字符数: {CharCount}", string.Join(" > ", sectionPath), sectionContent.Length);

                        // 调用 LLM 提取当前 section 的知识点
                        var response = await _llmService.ChatJsonAsync<KnowledgePointsResponse>(
                            systemPrompt,
                            $"请分析以下章节内容并提取知识点：\n\n章节路径：{string.Join(" > ", sectionPath)}\n\n{sectionContent}",
                            cancellationToken);

                        if (response?.KnowledgePoints != null && response.KnowledgePoints.Count > 0)
                        {
                            _logger.LogDebug("Section {SectionPath} 提取到 {Count} 个知识点", string.Join(" > ", sectionPath), response.KnowledgePoints.Count);

                            // 为每个知识点添加正确的文档 ID 和章节路径
                            foreach (var kp in response.KnowledgePoints)
                            {
                                kp.SectionId = section.SectionId;
                                kp.DocId = doc.DocId;
                                // 如果 LLM 没有提供章节路径，使用 section 的路径
                                if (kp.ChapterPath == null || kp.ChapterPath.Count == 0)
                                {
                                    kp.ChapterPath = sectionPath;
                                }
                            }

                            // 将结果添加到并发集合
                            foreach (var kp in response.KnowledgePoints)
                            {
                                knowledgePointsBag.Add(kp);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "处理 Section 失败: {SectionPath}", string.Join(" > ", section.HeadingPath));
                    }
                });

            // 将 ConcurrentBag 转换为 List
            var knowledgePoints = knowledgePointsBag.ToList();

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

                return kpList;
            }

            return new List<KnowledgePoint>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用 LLM 提取知识点失败");
            _logger.LogError("失败上下文：文档数={DocumentCount}", documents.Count);
            _logger.LogError("可能的原因：LLM 服务不可用、API 密钥无效、网络连接问题或内容长度超过限制");
            return new List<KnowledgePoint>();
        }
    }

    private static bool IsValidKpType(string type)
    {
        var validTypes = new[] { "concept", "chapter", "process", "api", "bestPractice" };
        return validTypes.Contains(type.ToLowerInvariant());
    }

    private async Task GenerateLearningContentForPointsAsync(
        List<KnowledgePoint> knowledgePoints,
        List<Document> documents,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("开始为 {Count} 个知识点生成学习内容", knowledgePoints.Count);

        // 使用 Parallel.ForEachAsync 并发处理所有知识点
        await Parallel.ForEachAsync(knowledgePoints, new ParallelOptions { CancellationToken = cancellationToken },
            async (kp, cancellationToken) =>
            {
                try
                {
                    _logger.LogDebug("生成学习内容: {KpId}", kp.KpId);

                    // 从 Document 中获取原文片段
                    var snippetTexts = await GetSnippetTextsFromDocumentsAsync(kp, documents, cancellationToken);

                    // 检查 snippetTexts 是否为空或长度小于 100
                    if (string.IsNullOrEmpty(snippetTexts) || snippetTexts.Length < 100)
                    {
                        _logger.LogWarning("原文片段为空或长度不足: {KpId}, 长度: {Length}", kp.KpId, snippetTexts?.Length ?? 0);
                        return;
                    }

                    // 生成学习内容
                    var learningPack = await GenerateLearningContentAsync(kp, snippetTexts, cancellationToken);

                    if (learningPack != null)
                    {
                        kp.Summary = learningPack.Summary;
                        kp.Levels = learningPack.Levels;
                        kp.SlideCards = learningPack.SlideCards;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "知识点 {KpId} 学习内容生成失败，使用降级内容", kp.KpId);
                }
            });

        _logger.LogInformation("已完成 {CompletedCount}/{TotalCount} 个知识点的学习内容生成", knowledgePoints.Count(kp => kp.Summary != null), knowledgePoints.Count);
        _logger.LogInformation("学习内容生成完成");
    }

    /// <summary>
    /// 从 Document 中获取原文片段内容
    /// </summary>
    private async Task<string> GetSnippetTextsFromDocumentsAsync(KnowledgePoint kp, List<Document> documents, CancellationToken cancellationToken)
    {
        var snippetContents = new List<string>();

        // 遍历所有文档，根据章节路径查找对应的章节
        foreach (var doc in documents)
        {
            if (doc.DocId != kp.DocId)
            {
                continue;
            }

            if (doc != null && File.Exists(doc.Path))
            {
                try
                {
                    // 读取文档文件的所有行
                    var lines = await File.ReadAllLinesAsync(doc.Path, cancellationToken);

                    // 根据章节路径查找对应的章节

                    var section = doc.FindSectionById(kp.SectionId);
                    if (section != null && section.StartLine >= 0 && section.EndLine <= lines.Length)
                    {
                        // 提取章节内容
                        var contentLines = lines.Skip(section.StartLine).Take(section.EndLine - section.StartLine);
                        var content = string.Join("\n", contentLines);
                        if (!string.IsNullOrEmpty(content))
                        {
                            snippetContents.Add(content);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "读取原文片段失败: {KpId}", kp.KpId);
                }
            }
        }

        return string.Join("\n\n", snippetContents);
    }


    /// <summary>
    /// 收集所有章节（递归）
    /// </summary>
    private void CollectAllSections(Document doc, List<Section> sections, List<(Document doc, Section section)> allSections)
    {
        foreach (var section in sections)
        {
            if(section.SubSections?.Count == 0)
                allSections.Add((doc, section));

            if (section.SubSections != null && section.SubSections.Count > 0)
            {
                CollectAllSections(doc, section.SubSections, allSections);
            }
        }
    }

    /// <summary>
    /// 读取章节内容
    /// </summary>
    private async Task<string> ReadSectionContentAsync(Document doc, Section section, CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(doc.Path))
            {
                return string.Empty;
            }

            var lines = await File.ReadAllLinesAsync(doc.Path, cancellationToken);
            if (lines.Length == 0)
            {
                return string.Empty;
            }

            if (section.StartLine < 0 || section.EndLine > lines.Length)
            {
                return string.Empty;
            }

            var contentLines = lines.Skip(section.StartLine).Take(section.EndLine - section.StartLine);
            return string.Join("\n", contentLines);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取章节内容失败: {DocId}, {SectionPath}", doc.DocId, string.Join(" > ", section.HeadingPath));
            return string.Empty;
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
                SectionId = "",
                DocId = doc.DocId
            };
            knowledgeSystem.KnowledgePoints.Add(kp);
        }

        knowledgeSystem.Tree = BuildKnowledgeTree(knowledgeSystem.KnowledgePoints);

        return knowledgeSystem;
    }

    /// <summary>
    /// 为所有知识点的幻灯片卡片生成 TTS 音频
    /// </summary>
    private async Task GenerateTtsForSlideCardsAsync(
        List<KnowledgePoint> knowledgePoints,
        CancellationToken cancellationToken)
    {
        var allSlideCards = knowledgePoints
            .SelectMany(kp => kp.SlideCards)
            .ToList();

        if (allSlideCards.Count == 0)
        {
            _logger.LogInformation("没有需要生成 TTS 的幻灯片卡片");
            return;
        }

        // 1. 检查并补全缺失的 SpeechScript
        var needsUpdateSpeechScript = allSlideCards.Any(sc => string.IsNullOrEmpty(sc.SpeechScript));
        if (needsUpdateSpeechScript)
        {
            _logger.LogInformation("检测到 {Count} 个幻灯片卡片缺少 SpeechScript，开始补全", 
                allSlideCards.Count(sc => string.IsNullOrEmpty(sc.SpeechScript)));
            await _learningGenerator.UpdateSpeechScriptsAsync(allSlideCards, cancellationToken);
        }

        // 2. 过滤出有 SpeechScript 的卡片进行 TTS 生成
        var slideCardsWithScript = allSlideCards
            .Where(sc => !string.IsNullOrWhiteSpace(sc.SpeechScript))
            .ToList();

        if (slideCardsWithScript.Count == 0)
        {
            _logger.LogInformation("没有需要生成 TTS 的幻灯片卡片（无 SpeechScript）");
            return;
        }

        _logger.LogInformation("开始为 {Count} 个幻灯片卡片生成 TTS 音频", slideCardsWithScript.Count);

        var completedCount = 0;
        var failedCount = 0;
        var cachedCount = 0;
        var emptyCount = 0;

        await Parallel.ForEachAsync(slideCardsWithScript,
            new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = 3 },
            async (slideCard, cancellationToken) =>
            {
                try
                {
                    // 如果已经有 AudioUrl，检查音频文件是否存在
                    if (!string.IsNullOrEmpty(slideCard.AudioUrl))
                    {
                        var audioPath = slideCard.AudioUrl;
                        if (audioPath.StartsWith("/"))
                        {
                            audioPath = audioPath.Substring(1);
                        }
                        
                        // 注意：这里无法直接访问 WebRootPath，跳过文件存在性检查
                        // 音频文件缺失的情况会在 API 层处理时重新生成
                        cachedCount++;
                        return;
                    }

                    // 生成音频
                    var audioUrl = await _ttsService.GetAudioUrlAsync(slideCard.SpeechScript!, cancellationToken);
                    if (!string.IsNullOrEmpty(audioUrl))
                    {
                        slideCard.AudioUrl = audioUrl;
                        Interlocked.Increment(ref completedCount);
                        _logger.LogDebug("TTS 生成成功: {SlideId}", slideCard.SlideId);
                    }
                    else
                    {
                        Interlocked.Increment(ref failedCount);
                        _logger.LogWarning("TTS 生成返回空: {SlideId}", slideCard.SlideId);
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref failedCount);
                    _logger.LogError(ex, "TTS 生成失败: {SlideId}", slideCard.SlideId);
                }
            });

        _logger.LogInformation("TTS 生成完成: 成功 {CompletedCount}, 缓存 {CachedCount}, 失败 {FailedCount}, 空脚本 {EmptyCount}", 
            completedCount, cachedCount, failedCount, emptyCount);
    }
}
