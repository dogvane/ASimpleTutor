using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using ASimpleTutor.Core.Models.Dto;
using Microsoft.Extensions.Logging;

namespace ASimpleTutor.Core.Services;

/// <summary>
/// 学习内容生成服务
/// </summary>
public class LearningGenerator : ILearningGenerator
{
    private readonly KnowledgeSystemStore _knowledgeSystemStore;
    private readonly ILLMService _llmService;
    private readonly ILogger<LearningGenerator> _logger;

    public LearningGenerator(
        KnowledgeSystemStore knowledgeSystemStore,
        ILLMService llmService,
        ILogger<LearningGenerator> logger)
    {
        _knowledgeSystemStore = knowledgeSystemStore;
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<LearningPack> GenerateAsync(KnowledgePoint kp, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("生成学习内容: {KpId} - {Title}", kp.KpId, kp.Title);

        try
        {
            // 1. 从 KnowledgeSystemStore 获取文档信息
            var bookHubId = kp.BookHubId;
            string snippetTexts = string.Empty;

            if (!string.IsNullOrEmpty(bookHubId))
            {
                var loadResult = await _knowledgeSystemStore.LoadAsync(bookHubId, cancellationToken);
                if (loadResult.Documents != null && loadResult.Documents.Count > 0)
                {
                    // 从 Document 中获取原文片段
                    snippetTexts = await GetSnippetTextsFromDocumentsAsync(kp, loadResult.Documents, cancellationToken);
                }
            }

            // 检查 snippetTexts 是否为空或长度小于 100
            if (string.IsNullOrEmpty(snippetTexts) || snippetTexts.Length < 100)
            {
                _logger.LogError("原文片段为空或长度不足: {KpId}, 长度: {Length}", kp.KpId, snippetTexts?.Length ?? 0);
                return CreateFallbackLearningPack(kp, new List<SourceSnippet>());
            }

            // 2. 调用 LLM 生成学习内容
            _logger.LogDebug("调用 LLM 生成学习内容");
            var learningPack = await GenerateLearningContentAsync(kp, snippetTexts, cancellationToken);

            if (learningPack != null)
            {
                return learningPack;
            }
            else
            {
                // 降级：使用原文片段提取要点
                return CreateFallbackLearningPack(kp, new List<SourceSnippet>());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "学习内容生成失败: {KpId}", kp.KpId);
            return CreateFallbackLearningPack(kp, new List<SourceSnippet>());
        }
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
8. 必须过滤掉参考文献、引用列表等非核心内容

自检要求：
- summary.definition 不能为空
- levels 至少包含 level=1 的内容
- slide_cards 至少包含 1 张卡片
- 必须返回完整的 JSON 对象，不能返回空对象 {}
- 如果无法生成有效内容，请返回包含基本信息的对象，而非空对象 {}";

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
            _logger.LogWarning("LLM 返回内容为空: {KpId}", kp.KpId);
            return null;
        }

        // 检测空对象或无效内容
        if (content.Summary == null && (content.Levels == null || content.Levels.Count == 0) && (content.SlideCards == null || content.SlideCards.Count == 0))
        {
            _logger.LogWarning("LLM 返回空对象或无效内容: {KpId}", kp.KpId);
            return null;
        }

        // 过滤参考文献内容
        if (content.Summary != null)
        {
            content.Summary.Definition = FilterReferences(content.Summary.Definition);
            content.Summary.KeyPoints = content.Summary.KeyPoints?.Select(FilterReferences).ToList() ?? new List<string>();
            content.Summary.Pitfalls = content.Summary.Pitfalls?.Select(FilterReferences).ToList() ?? new List<string>();
        }

        if (content.Levels != null)
        {
            foreach (var level in content.Levels)
            {
                level.Content = FilterReferences(level.Content);
            }
        }

        if (content.SlideCards != null)
        {
            foreach (var card in content.SlideCards)
            {
                card.Content = FilterReferences(card.Content) ?? string.Empty;
                card.Title = FilterReferences(card.Title) ?? string.Empty;
                card.Subtitle = FilterReferences(card.Subtitle) ?? string.Empty;
            }
        }

        var learningPack = new LearningPack
        {
            KpId = kp.KpId,
            Summary = content.Summary ?? new Summary(),
            Levels = content.Levels ?? new List<ContentLevel>(),
            RelatedKpIds = new List<string>(),
            SlideCards = content.SlideCards?.Select((dto, index) => new SlideCard
                {
                    SlideId = dto.SlideId ?? $"{kp.KpId}_slide_{index}",
                    KpId = kp.KpId,
                    Type = ConvertSlideType(dto.Type),
                    Order = dto.Order,
                    Title = dto.Title ?? string.Empty,
                    HtmlContent = dto.Content ?? string.Empty,
                    SourceReferences = new List<SourceReference>(),
                    Config = new SlideConfig
                    {
                        AllowSkip = dto.Config?.AllowSkip ?? true,
                        RequireComplete = dto.Config?.RequireComplete ?? false
                    }
                })
                .ToList() ?? new List<SlideCard>()
        };

        // 为每个幻灯片卡片生成口语化讲解脚本
        await GenerateSpeechScriptsAsync(learningPack.SlideCards, kp, cancellationToken);

        return learningPack;
    }

    /// <summary>
    /// 为幻灯片卡片生成口语化讲解脚本
    /// </summary>
    private async Task GenerateSpeechScriptsAsync(List<SlideCard> slideCards, KnowledgePoint kp, CancellationToken cancellationToken)
    {
        foreach (var card in slideCards)
        {
            try
            {
                _logger.LogDebug("为幻灯片卡片生成语音脚本: {SlideId}", card.SlideId);
                card.SpeechScript = await GenerateSpeechScriptAsync(card, kp, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成语音脚本失败: {SlideId}", card.SlideId);
                card.SpeechScript = null;
            }
        }
    }

    /// <summary>
    /// 生成单个幻灯片卡片的口语化讲解脚本
    /// </summary>
    private async Task<string?> GenerateSpeechScriptAsync(SlideCard card, KnowledgePoint kp, CancellationToken cancellationToken)
    {
        var systemPrompt = @"你是一个专业的教学讲解专家。你的任务是为幻灯片卡片生成口语化的讲解脚本，用于文字转语音（TTS）播放。

请以自然、流畅的口语风格编写讲解脚本，遵循以下原则：
1. 使用第一人称（我、我们），营造亲切感
2. 使用简单易懂的语言，避免过于学术化的表达
3. 适当使用停顿标记（用逗号、句号表示）
4. 每句话不要太长，便于语音播放
5. 语气要自然、友好，像老师在讲解
6. 避免使用 markdown 格式，只返回纯文本
7. 讲解内容要基于幻灯片内容，不要添加额外信息
8. 不需要开场白（如“大家好”），直接进入本页内容的讲解

脚本结构：
- 核心讲解：直接解析本页内容，用3-5句话
- 结尾：用1句话总结本页要点或自然过渡

示例格式：
这个概念在实际应用中非常关键。具体来说，它包含三个层面的含义。首先是... 其次是... 理解了这几点，我们就能掌握它的核心逻辑。";

        var userMessage = $"知识点标题：{kp.Title}\n" +
                          $"幻灯片标题：{card.Title}\n" +
                          $"幻灯片内容：{card.HtmlContent}\n" +
                          $"幻灯片类型：{card.Type}\n\n" +
                          "请为这张幻灯片生成口语化的讲解脚本：";

        try
        {
            var speechScript = await _llmService.ChatAsync(systemPrompt, userMessage, cancellationToken);
            return string.IsNullOrEmpty(speechScript) ? null : speechScript.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成语音脚本失败: {SlideId}", card.SlideId);
            return null;
        }
    }

    /// <summary>
    /// 过滤参考文献内容
    /// </summary>
    private string FilterReferences(string? content)
    {
        if (string.IsNullOrEmpty(content))
            return string.Empty;

        var result = content;

        // 过滤参考文献相关内容
        var referencePatterns = new[]
        {
            @"##\s*参考文献\s*[\s\S]*?(?=\n##|\Z)",
            @"##\s*References\s*[\s\S]*?(?=\n##|\Z)",
            @"##\s*参考书目\s*[\s\S]*?(?=\n##|\Z)",
            @"\[\d+\]\s*[A-Z][^\n]*",
            @"参考文献\s*[:：][\s\S]*?(?=\n\n|\Z)",
            @"References\s*[:：][\s\S]*?(?=\n\n|\Z)"
        };

        foreach (var pattern in referencePatterns)
        {
            result = System.Text.RegularExpressions.Regex.Replace(result, pattern, "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return result.Trim();
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

    private async Task<List<string>> FindRelatedKnowledgePointsAsync(
        KnowledgePoint kp,
        CancellationToken cancellationToken)
    {
        // MVP 阶段暂不实现，返回空列表
        return new List<string>();
    }

    private void ValidateLearningPack(LearningPack pack, KnowledgePoint kp)
    {
        if (pack.Summary == null)
        {
            _logger.LogWarning("学习内容 summary 为空: {KpId}", kp.KpId);
        }
        else if (string.IsNullOrEmpty(pack.Summary.Definition))
        {
            _logger.LogWarning("学习内容 definition 为空: {KpId}", kp.KpId);
        }

        if (pack.Levels == null || !pack.Levels.Any(l => l.Level == 1))
        {
            _logger.LogWarning("学习内容缺少 level=1 的层次: {KpId}", kp.KpId);
        }
    }

    private LearningPack CreateFallbackLearningPack(KnowledgePoint kp, List<SourceSnippet>? snippets)
    {
        // 处理 null 或空列表
        snippets ??= new List<SourceSnippet>();

        // 从原文片段简单提取要点
        var allContent = string.Join("\n", snippets.Select(s => s.Content));
        var sentences = allContent.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var keyPoints = sentences.Take(3)
            .Select(s => s.Trim())
            .Where(s => s.Length > 10)
            .ToList();

        return new LearningPack
        {
            KpId = kp.KpId,
            Summary = new Summary
            {
                Definition = kp.ChapterPath.Count > 0
                    ? $"这是关于 {kp.Title} 的知识点，位于 {kp.ChapterPath.Last()} 章节。"
                    : $"这是关于 {kp.Title} 的知识点。",
                KeyPoints = keyPoints.Any() ? keyPoints : new List<string> { "内容生成失败，请查看原文" },
                Pitfalls = new List<string>()
            },
            Levels = new List<ContentLevel>
            {
                new ContentLevel { Level = 1, Title = "概览", Content = "无法生成层次化内容，请查看原文片段" }
            }
        };
    }
}
