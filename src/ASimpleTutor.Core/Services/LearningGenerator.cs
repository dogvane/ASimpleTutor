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

        List<SourceSnippet> snippets = new();

        try
        {
            // 1. 从 KnowledgeSystemStore 获取原文片段
            var bookRootId = kp.BookRootId;
            if (!string.IsNullOrEmpty(bookRootId))
            {
                var loadResult = await _knowledgeSystemStore.LoadAsync(bookRootId, cancellationToken);
                if (loadResult.KnowledgeSystem != null)
                {
                    snippets = kp.SnippetIds
                        .Select(id => loadResult.KnowledgeSystem.Snippets.TryGetValue(id, out var snippet) ? snippet : null)
                        .Where(s => s != null)
                        .Cast<SourceSnippet>()
                        .ToList();
                }
            }

            var snippetTexts = string.Join("\n\n", snippets.Select(s => s.Content));

            // 检查 snippetTexts 是否为空或长度小于 100
            if (string.IsNullOrEmpty(snippetTexts) || snippetTexts.Length < 100)
            {
                _logger.LogError("原文片段为空或长度不足: {KpId}, 长度: {Length}", kp.KpId, snippetTexts?.Length ?? 0);
                return CreateFallbackLearningPack(kp, snippets);
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
                return CreateFallbackLearningPack(kp, snippets);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "学习内容生成失败: {KpId}", kp.KpId);
            return CreateFallbackLearningPack(kp, snippets);
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
            },
            SnippetIds = snippets.Select(s => s.SnippetId).ToList()
        };
    }
}
