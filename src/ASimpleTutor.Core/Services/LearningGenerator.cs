using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ASimpleTutor.Core.Services;

/// <summary>
/// 学习内容生成服务
/// </summary>
public class LearningGenerator : ILearningGenerator
{
    private readonly ISimpleRagService _ragService;
    private readonly ISourceTracker _sourceTracker;
    private readonly ILLMService _llmService;
    private readonly ILogger<LearningGenerator> _logger;

    public LearningGenerator(
        ISimpleRagService ragService,
        ISourceTracker sourceTracker,
        ILLMService llmService,
        ILogger<LearningGenerator> logger)
    {
        _ragService = ragService;
        _sourceTracker = sourceTracker;
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<LearningPack> GenerateAsync(KnowledgePoint kp, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("生成学习内容: {KpId} - {Title}", kp.KpId, kp.Title);

        var learningPack = new LearningPack
        {
            KpId = kp.KpId,
            SnippetIds = new List<string>(kp.SnippetIds)
        };

        try
        {
            // 1. 获取原文片段
            var snippets = _sourceTracker.GetSources(kp.SnippetIds);
            var snippetTexts = string.Join("\n\n", snippets.Select(s => s.Content));

            // 2. 调用 LLM 生成学习内容
            _logger.LogDebug("调用 LLM 生成学习内容");
            var content = await GenerateLearningContentAsync(kp, snippetTexts, cancellationToken);

            if (content != null)
            {
                learningPack.Summary = content.Summary;
                learningPack.Levels = content.Levels;
            }
            else
            {
                // 降级：使用原文片段提取要点
                learningPack = CreateFallbackLearningPack(kp, snippets);
            }

            // 3. 自检学习内容
            ValidateLearningPack(learningPack, kp);

            // 4. 收集原文片段
            learningPack.SnippetIds = snippets.Select(s => s.SnippetId).ToList();

            // 5. 关联知识点（可选）
            learningPack.RelatedKpIds = await FindRelatedKnowledgePointsAsync(kp, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "学习内容生成失败: {KpId}", kp.KpId);
            learningPack = CreateFallbackLearningPack(kp, _sourceTracker.GetSources(kp.SnippetIds));
        }

        return learningPack;
    }

    private async Task<LearningContentResponse?> GenerateLearningContentAsync(
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
                          $"所属章节：{string.Join(" > ", kp.ChapterPath)}\n" +
                          $"相关原文片段：\n{snippetTexts}";

        return await _llmService.ChatJsonAsync<LearningContentResponse>(
            systemPrompt,
            userMessage,
            cancellationToken);
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

/// <summary>
/// LLM 响应数据结构（JSON 字段使用 snake_case，与设计文档保持一致）
/// </summary>
public class LearningContentResponse
{
    [JsonProperty("summary")]
    public Summary Summary { get; set; } = new();

    [JsonProperty("levels")]
    public List<ContentLevel> Levels { get; set; } = new();
}
