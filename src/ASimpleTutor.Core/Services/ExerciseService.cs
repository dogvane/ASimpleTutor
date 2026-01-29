using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using Microsoft.Extensions.Logging;

namespace ASimpleTutor.Core.Services;

/// <summary>
/// 习题生成与反馈服务
/// </summary>
public class ExerciseService : IExerciseGenerator, IExerciseFeedback
{
    private readonly ISimpleRagService _ragService;
    private readonly ISourceTracker _sourceTracker;
    private readonly ILLMService _llmService;
    private readonly ILogger<ExerciseService> _logger;

    public ExerciseService(
        ISimpleRagService ragService,
        ISourceTracker sourceTracker,
        ILLMService llmService,
        ILogger<ExerciseService> logger)
    {
        _ragService = ragService;
        _sourceTracker = sourceTracker;
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<List<Exercise>> GenerateAsync(KnowledgePoint kp, int count = 1, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("生成习题: {KpId}, 数量: {Count}", kp.KpId, count);

        try
        {
            var snippets = _sourceTracker.GetSources(kp.SnippetIds);
            var snippetTexts = string.Join("\n\n", snippets.Select(s => s.Content));

            return await GenerateExercisesAsync(kp, snippetTexts, count, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "习题生成失败: {KpId}", kp.KpId);
            return new List<Exercise>();
        }
    }

    private async Task<List<Exercise>> GenerateExercisesAsync(
        KnowledgePoint kp,
        string snippetTexts,
        int count,
        CancellationToken cancellationToken)
    {
        var systemPrompt = $@"你是一个习题生成专家。你的任务是根据提供的知识点生成练习题。

请生成 {count} 道练习题，题型包括选择题、填空题、简答题。

请以 JSON 格式输出，结构如下：
{{
  ""exercises"": [
    {{
      ""type"": ""SingleChoice|FillBlank|ShortAnswer"",
      ""question"": ""题目内容"",
      ""options"": [""选项1"", ""选项2"", ""选项3"", ""选项4""],
      ""correct_answer"": ""正确答案"",
      ""key_points"": [""考查要点1"", ""考查要点2""],
      ""explanation"": ""答案解释""
    }}
  ]
}}

类型说明：
- SingleChoice = 选择题
- FillBlank = 填空题
- ShortAnswer = 简答题

生成原则：
1. 题目难度为基础理解，不引入外部知识
2. 选择题：1个正确答案 + 2~3个干扰项
3. 填空题：关键术语或步骤
4. 简答题：可从要点角度回答的问题
5. 必须基于原文片段出题
6. type 字段必须使用英文：SingleChoice、FillBlank 或 ShortAnswer";

        var userMessage = $"知识点：{kp.Title}\n" +
                          $"章节：{string.Join(" > ", kp.ChapterPath)}\n" +
                          $"原文内容：\n{snippetTexts}";

        var response = await _llmService.ChatJsonAsync<ExercisesResponse>(
            systemPrompt,
            userMessage,
            cancellationToken);

        var exercises = response?.Exercises ?? new List<Exercise>();

        // 分配 ID
        var index = 0;
        foreach (var exercise in exercises)
        {
            exercise.ExerciseId = $"{kp.KpId}_ex_{index++}";
            exercise.KpId = kp.KpId;
            exercise.EvidenceSnippetIds = new List<string>(kp.SnippetIds);
        }

        return exercises;
    }

    public async Task<ExerciseFeedback> JudgeAsync(Exercise exercise, string userAnswer, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("评判习题: {ExerciseId}", exercise.ExerciseId);

        try
        {
            return exercise.Type switch
            {
                ExerciseType.SingleChoice => await JudgeChoiceAsync(exercise, userAnswer),
                ExerciseType.FillBlank => await JudgeFillBlankAsync(exercise, userAnswer, cancellationToken),
                ExerciseType.ShortAnswer => await JudgeShortAnswerAsync(exercise, userAnswer, cancellationToken),
                _ => new ExerciseFeedback { Explanation = "不支持的题型" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "判题失败: {ExerciseId}", exercise.ExerciseId);
            return new ExerciseFeedback
            {
                ReferenceAnswer = exercise.CorrectAnswer,
                Explanation = "判题过程出现错误"
            };
        }
    }

    private async Task<ExerciseFeedback> JudgeChoiceAsync(Exercise exercise, string userAnswer)
    {
        var isCorrect = userAnswer.Trim().Equals(exercise.CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase);

        return new ExerciseFeedback
        {
            IsCorrect = isCorrect,
            Explanation = isCorrect ? "回答正确！" : $"回答错误，正确答案是 {exercise.CorrectAnswer}",
            ReferenceAnswer = exercise.CorrectAnswer
        };
    }

    private async Task<ExerciseFeedback> JudgeFillBlankAsync(Exercise exercise, string userAnswer, CancellationToken cancellationToken)
    {
        // 使用 LLM 进行模糊匹配
        var systemPrompt = @"你是一个判题专家。请判断用户答案是否正确，并给出简要解释。

判断标准：
- 关键术语匹配正确即可
- 允许大小写、格式略有差异

请以 JSON 格式输出：
{
  ""is_correct"": true 或 false,
  ""explanation"": ""判断理由"",
  ""covered_points"": [""用户答对的要点""],
  ""missing_points"": [""用户遗漏的要点""]
}";

        var userMessage = $"题目：{exercise.Question}\n" +
                          $"正确答案：{exercise.CorrectAnswer}\n" +
                          $"用户答案：{userAnswer}";

        var response = await _llmService.ChatJsonAsync<FillBlankFeedbackResponse>(
            systemPrompt,
            userMessage,
            cancellationToken);

        return new ExerciseFeedback
        {
            IsCorrect = response?.IsCorrect,
            Explanation = response?.Explanation ?? "无法判断",
            ReferenceAnswer = exercise.CorrectAnswer,
            CoveredPoints = response?.CoveredPoints ?? new List<string>(),
            MissingPoints = response?.MissingPoints ?? new List<string>()
        };
    }

    private async Task<ExerciseFeedback> JudgeShortAnswerAsync(Exercise exercise, string userAnswer, CancellationToken cancellationToken)
    {
        // 使用 LLM 判断要点覆盖度
        var systemPrompt = @"你是一个简答题批改专家。请评估用户答案的要点击中情况。

请以 JSON 格式输出：
{
  ""is_correct"": true 或 false,
  ""explanation"": ""整体评价"",
  ""covered_points"": [""用户答对的要点""],
  ""missing_points"": [""用户遗漏的要点""]
}";

        var userMessage = $"题目：{exercise.Question}\n" +
                          $"参考要点：{string.Join(", ", exercise.KeyPoints)}\n" +
                          $"用户答案：{userAnswer}";

        var response = await _llmService.ChatJsonAsync<FillBlankFeedbackResponse>(
            systemPrompt,
            userMessage,
            cancellationToken);

        return new ExerciseFeedback
        {
            IsCorrect = response?.IsCorrect,
            Explanation = response?.Explanation ?? "请参考正确答案",
            ReferenceAnswer = exercise.CorrectAnswer,
            CoveredPoints = response?.CoveredPoints ?? new List<string>(),
            MissingPoints = response?.MissingPoints ?? new List<string>()
        };
    }
}

/// <summary>
/// LLM 响应数据结构
/// </summary>
public class ExercisesResponse
{
    public List<Exercise> Exercises { get; set; } = new();
}

public class FillBlankFeedbackResponse
{
    public bool? IsCorrect { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public List<string> CoveredPoints { get; set; } = new();
    public List<string> MissingPoints { get; set; } = new();
}
