using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using ASimpleTutor.Core.Models.Dto;
using Microsoft.Extensions.Logging;

namespace ASimpleTutor.Core.Services;

/// <summary>
/// 习题生成与反馈服务
/// </summary>
public class ExerciseService : IExerciseGenerator, IExerciseFeedback
{
    private readonly ILLMService _llmService;
    private readonly KnowledgeSystemStore _knowledgeSystemStore;
    private readonly ILogger<ExerciseService> _logger;

    // 错题记录存储（内存缓存，生产环境应使用持久化存储）
    private readonly Dictionary<string, MistakeRecord> _mistakeRecords = new();
    private readonly object _lock = new();

    public ExerciseService(
        ILLMService llmService,
        KnowledgeSystemStore knowledgeSystemStore,
        ILogger<ExerciseService> logger)
    {
        _llmService = llmService;
        _knowledgeSystemStore = knowledgeSystemStore;
        _logger = logger;
    }

    public async Task<List<Exercise>> GenerateAsync(KnowledgePoint kp, int count = 3, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("生成习题: {KpId}, 数量: {Count}", kp.KpId, count);

        try
        {
            string snippetTexts = string.Empty;

            // 从 KnowledgeSystemStore 中加载原文片段
            var bookHubId = kp.BookHubId;
            _logger.LogInformation("知识点 BookHubId: {BookHubId}", bookHubId);
            _logger.LogInformation("知识点 SnippetIds 数量: {SnippetCount}", kp.SnippetIds.Count);
            if (kp.SnippetIds.Count > 0)
            {
                _logger.LogInformation("知识点 SnippetIds: {SnippetIds}", string.Join(", ", kp.SnippetIds));
            }

            if (!string.IsNullOrEmpty(bookHubId))
            {
                // 从存储中加载知识系统
                var loadResult = await _knowledgeSystemStore.LoadAsync(bookHubId, cancellationToken);
                if (loadResult.KnowledgeSystem != null)
                {
                    _logger.LogInformation("从 KnowledgeSystemStore 加载知识系统成功: {BookHubId}, 知识点数量: {KpCount}, 原文片段数量: {SnippetCount}", 
                        bookHubId, loadResult.KnowledgeSystem.KnowledgePoints.Count, loadResult.KnowledgeSystem.Snippets.Count);
                    
                    // 尝试从知识系统的 Snippets 中获取原文片段
                    var knowledgeSystemSnippets = kp.SnippetIds
                        .Select(id => loadResult.KnowledgeSystem.Snippets.TryGetValue(id, out var snippet) ? snippet : null)
                        .Where(s => s != null)
                        .ToList();
                    
                    if (knowledgeSystemSnippets.Count > 0)
                    {
                        snippetTexts = string.Join("\n\n", knowledgeSystemSnippets.Select(s => s!.Content));
                        _logger.LogInformation("从 KnowledgeSystemStore 成功加载原文片段: {KpId}, 数量: {Count}, 长度: {Length}", 
                            kp.KpId, knowledgeSystemSnippets.Count, snippetTexts.Length);
                    }
                    else
                    {
                        // 如果找不到匹配的原文片段，尝试使用知识系统中的第一个原文片段
                        var firstSnippet = loadResult.KnowledgeSystem.Snippets.Values.FirstOrDefault();
                        if (firstSnippet != null)
                        {
                            snippetTexts = firstSnippet.Content;
                            _logger.LogInformation("使用知识系统中的第一个原文片段: {SnippetId}, 长度: {Length}", 
                                firstSnippet.SnippetId, snippetTexts.Length);
                        }
                        else
                        {
                            _logger.LogWarning("知识系统中没有原文片段: {BookHubId}", bookHubId);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("加载知识系统失败: {BookHubId}", bookHubId);
                }
            }
            else
            {
                _logger.LogWarning("知识点 BookHubId 为空: {KpId}", kp.KpId);
            }

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
        // 检查 snippetTexts 是否为空
        if (string.IsNullOrEmpty(snippetTexts))
        {
            _logger.LogError("原文片段为空: {KpId}", kp.KpId);
            
            // 降级：使用知识点本身的信息生成习题
            return await GenerateExercisesWithFallbackAsync(kp, count, cancellationToken);
        }

        // 如果原文片段长度不足，仍然尝试生成习题
        if (snippetTexts.Length < 100)
        {
            _logger.LogWarning("原文片段长度不足: {KpId}, 长度: {Length}", kp.KpId, snippetTexts.Length);
        }

        var systemPrompt = @"你是一个习题生成专家。你的任务是根据提供的知识点生成高质量的练习题。

请生成 " + count + @" 道选择题，题型应多样化，包括：
- SingleChoice（单选题）
- TrueFalse（判断题）

请以 JSON 格式输出，结构如下：
{
  ""exercises"": [
    {
      ""type"": ""SingleChoice|TrueFalse"",
      ""difficulty"": 1-5,
      ""question"": ""题目内容"",
      ""options"": [""选项A"", ""选项B"", ""选项C"", ""选项D""] ,
      ""correct_answer"": ""选项A"",
      ""key_points"": [""考查要点1"", ""考查要点2""] ,
      ""explanation"": ""答案解释（必须填写）""
    }
  ]
}

类型说明：
- SingleChoice = 单选题，需要 4 个选项（A、B、C、D）
- TrueFalse = 判断题，选项为 [""正确"", ""错误""]

生成原则：
1. 题目难度为基础理解（difficulty=1-2），不引入外部知识
2. 单选题：1个正确答案 + 3个干扰项，选项要合理且具有迷惑性
3. 判断题：正确/错误选项，判断依据要明确
4. 必须基于原文片段出题，确保答案在原文中能找到依据
5. type 字段必须使用英文
6. explanation 必须填写，解释要清晰说明为什么正确答案是对的
7. difficulty 反映题目难度（1最简单，5最难）
8. 选项内容要简洁明了，避免过长或过于相似的选项
9. 干扰项要基于常见错误理解，但要有明显的错误点

示例：
单选题示例：
{
  ""type"": ""SingleChoice"",
  ""difficulty"": 1,
  ""question"": ""智能体的核心特征是什么？"",
  ""options"": [""能够感知环境并自主行动"", ""只能执行固定程序"", ""需要人工干预"", ""只能处理简单任务""] ,
  ""correct_answer"": ""能够感知环境并自主行动"",
  ""key_points"": [""感知能力"", ""自主决策""] ,
  ""explanation"": ""智能体的核心特征是能够感知环境变化，并根据环境信息自主做出决策和采取行动。""
}

判断题示例：
{
  ""type"": ""TrueFalse"",
  ""difficulty"": 1,
  ""question"": ""智能体只能执行预设的程序，不能自主决策。"",
  ""options"": [""正确"", ""错误""] ,
  ""correct_answer"": ""错误"",
  ""key_points"": [""自主决策能力""] ,
  ""explanation"": ""错误。智能体具备自主决策能力，能够根据环境信息做出判断和选择，而不是只能执行预设的程序。""
}

自检要求：
- 生成的题目数量应与 count 基本一致
- type 必须是有效类型之一（SingleChoice 或 TrueFalse）
- correct_answer 必须是 options 中的一个选项
- explanation 必须填写，内容要清晰准确
- 选项数量必须符合题型要求（单选题4个选项，判断题2个选项）
- 如果无法生成指定数量的题目，返回实际能生成的数量";

        var userMessage = $"知识点：{kp.Title}\n" +
                          $"知识点类型：{kp.Type}\n" +
                          $"章节：{string.Join(" > ", kp.ChapterPath)}\n" +
                          $"原文内容：\n{snippetTexts}";

        var response = await _llmService.ChatJsonAsync<ExercisesResponse>(
            systemPrompt,
            userMessage,
            cancellationToken);

        var exerciseDtos = response?.Exercises ?? new List<ExerciseDto>();

        // 自检：验证题目数据
        if (exerciseDtos.Count == 0)
        {
            _logger.LogWarning("未能生成任何习题: {KpId}", kp.KpId);
        }
        else if (exerciseDtos.Count < count)
        {
            _logger.LogWarning("生成的题目数量不足: {ActualCount}/{ExpectedCount}, {KpId}",
                exerciseDtos.Count, count, kp.KpId);
        }

        // 将 DTO 转换为 Exercise 并分配 ID
        var exercises = exerciseDtos
            .Select((dto, index) =>
            {
                var exercise = dto.ToExercise();
                exercise.ExerciseId = $"{kp.KpId}_ex_{index}";
                exercise.KpId = kp.KpId;
                exercise.EvidenceSnippetIds = new List<string>(kp.SnippetIds);
                exercise.CreatedAt = DateTime.UtcNow;
                return exercise;
            })
            .ToList();

        _logger.LogInformation("成功生成 {Count} 道习题", exercises.Count);
        return exercises;
    }

    /// <summary>
    /// 降级：使用知识点本身的信息生成习题
    /// </summary>
    private async Task<List<Exercise>> GenerateExercisesWithFallbackAsync(
        KnowledgePoint kp,
        int count,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("使用降级策略生成习题: {KpId}", kp.KpId);

        var systemPrompt = @"你是一个习题生成专家。你的任务是根据提供的知识点生成高质量的练习题。

请生成 " + count + @" 道选择题，题型应多样化，包括：
- SingleChoice（单选题）
- TrueFalse（判断题）

请以 JSON 格式输出，结构如下：
{
  ""exercises"": [
    {
      ""type"": ""SingleChoice|TrueFalse"",
      ""difficulty"": 1-5,
      ""question"": ""题目内容"",
      ""options"": [""选项A"", ""选项B"", ""选项C"", ""选项D""] ,
      ""correct_answer"": ""选项A"",
      ""key_points"": [""考查要点1"", ""考查要点2""] ,
      ""explanation"": ""答案解释（必须填写）""
    }
  ]
}

类型说明：
- SingleChoice = 单选题，需要 4 个选项（A、B、C、D）
- TrueFalse = 判断题，选项为 [""正确"", ""错误""]

生成原则：
1. 题目难度为基础理解（difficulty=1-2），基于知识点的标题和章节信息
2. 单选题：1个正确答案 + 3个干扰项，选项要合理且具有迷惑性
3. 判断题：正确/错误选项，判断依据要明确
4. 基于知识点的标题和章节信息出题
5. type 字段必须使用英文
6. explanation 必须填写，解释要清晰说明为什么正确答案是对的
7. difficulty 反映题目难度（1最简单，5最难）
8. 选项内容要简洁明了，避免过长或过于相似的选项
9. 干扰项要基于常见错误理解，但要有明显的错误点

自检要求：
- 生成的题目数量应与 count 基本一致
- type 必须是有效类型之一（SingleChoice 或 TrueFalse）
- correct_answer 必须是 options 中的一个选项
- explanation 必须填写，内容要清晰准确
- 选项数量必须符合题型要求（单选题4个选项，判断题2个选项）
- 如果无法生成指定数量的题目，返回实际能生成的数量";

        var userMessage = $"知识点：{kp.Title}\n" +
                          $"知识点类型：{kp.Type}\n" +
                          $"章节：{string.Join(" > ", kp.ChapterPath)}\n" +
                          $"知识点摘要：{kp.Summary?.Definition ?? "无摘要"}";

        var response = await _llmService.ChatJsonAsync<ExercisesResponse>(
            systemPrompt,
            userMessage,
            cancellationToken);

        var exerciseDtos = response?.Exercises ?? new List<ExerciseDto>();

        // 自检：验证题目数据
        if (exerciseDtos.Count == 0)
        {
            _logger.LogWarning("降级策略未能生成任何习题: {KpId}", kp.KpId);
        }
        else if (exerciseDtos.Count < count)
        {
            _logger.LogWarning("降级策略生成的题目数量不足: {ActualCount}/{ExpectedCount}, {KpId}",
                exerciseDtos.Count, count, kp.KpId);
        }

        // 将 DTO 转换为 Exercise 并分配 ID
        var exercises = exerciseDtos
            .Select((dto, index) =>
            {
                var exercise = dto.ToExercise();
                exercise.ExerciseId = $"{kp.KpId}_ex_{index}";
                exercise.KpId = kp.KpId;
                exercise.EvidenceSnippetIds = new List<string>(kp.SnippetIds);
                exercise.CreatedAt = DateTime.UtcNow;
                return exercise;
            })
            .ToList();

        _logger.LogInformation("降级策略成功生成 {Count} 道习题", exercises.Count);
        return exercises;
    }

    public async Task<ExerciseFeedback> JudgeAsync(Exercise exercise, string userAnswer, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("评判习题: {ExerciseId}", exercise.ExerciseId);

        try
        {
            return exercise.Type switch
            {
                ExerciseType.SingleChoice
                    => await JudgeSingleChoiceAsync(exercise, userAnswer),
                ExerciseType.MultiChoice
                    => await JudgeMultiChoiceAsync(exercise, userAnswer),
                ExerciseType.TrueFalse
                    => await JudgeTrueFalseAsync(exercise, userAnswer),
                ExerciseType.ShortAnswer
                    => await JudgeShortAnswerAsync(exercise, userAnswer, cancellationToken),
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

    private Task<ExerciseFeedback> JudgeSingleChoiceAsync(Exercise exercise, string userAnswer)
    {
        var isCorrect = userAnswer.Trim().Equals(exercise.CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase);

        return Task.FromResult(new ExerciseFeedback
        {
            IsCorrect = isCorrect,
            Explanation = isCorrect ? "回答正确！" : $"回答错误，正确答案是 {exercise.CorrectAnswer}",
            ReferenceAnswer = exercise.CorrectAnswer,
            MasteryAdjustment = isCorrect ? 0.05f : -0.1f
        });
    }

    private Task<ExerciseFeedback> JudgeMultiChoiceAsync(Exercise exercise, string userAnswer)
    {
        var userOptions = userAnswer.Split(',')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var correctOptions = exercise.CorrectAnswer.Split(',')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var isCorrect = userOptions.SetEquals(correctOptions);

        return Task.FromResult(new ExerciseFeedback
        {
            IsCorrect = isCorrect,
            Explanation = isCorrect
                ? "回答正确！"
                : $"回答错误。正确答案包括：{exercise.CorrectAnswer}",
            ReferenceAnswer = exercise.CorrectAnswer,
            MasteryAdjustment = isCorrect ? 0.08f : -0.12f
        });
    }

    private Task<ExerciseFeedback> JudgeTrueFalseAsync(Exercise exercise, string userAnswer)
    {
        // 标准化用户答案
        var normalizedAnswer = userAnswer.Trim().ToLowerInvariant();
        var correctAnswer = exercise.CorrectAnswer.Trim().ToLowerInvariant();

        var isCorrect = normalizedAnswer == correctAnswer ||
                        (normalizedAnswer == "true" && correctAnswer == "正确") ||
                        (normalizedAnswer == "false" && correctAnswer == "错误") ||
                        (normalizedAnswer == "正确" && correctAnswer == "true") ||
                        (normalizedAnswer == "错误" && correctAnswer == "false");

        return Task.FromResult(new ExerciseFeedback
        {
            IsCorrect = isCorrect,
            Explanation = isCorrect ? "回答正确！" : $"回答错误，正确答案是 {exercise.CorrectAnswer}",
            ReferenceAnswer = exercise.CorrectAnswer,
            MasteryAdjustment = isCorrect ? 0.05f : -0.1f
        });
    }

    private async Task<ExerciseFeedback> JudgeShortAnswerAsync(Exercise exercise, string userAnswer, CancellationToken cancellationToken)
    {
        // 使用 LLM 判断要点覆盖度
        var systemPrompt = @"你是一个简答题批改专家。请评估用户答案的要点击中情况。

评分标准：
- 正确回答所有要点 = 优秀
- 正确回答部分要点 = 良好/一般
- 遗漏关键要点 = 错误

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

        var response = await _llmService.ChatJsonAsync<ShortAnswerFeedbackDto>(
            systemPrompt,
            userMessage,
            cancellationToken);

        var isCorrect = response?.IsCorrect ?? false;

        return new ExerciseFeedback
        {
            IsCorrect = response?.IsCorrect,
            Explanation = response?.Explanation ?? "请参考正确答案",
            ReferenceAnswer = exercise.CorrectAnswer,
            CoveredPoints = response?.CoveredPoints ?? new List<string>(),
            MissingPoints = response?.MissingPoints ?? new List<string>(),
            ErrorAnalysis = response?.MissingPoints.Count > 0
                ? $"遗漏要点：{string.Join(", ", response.MissingPoints)}"
                : null,
            MasteryAdjustment = isCorrect ? 0.08f : -0.15f
        };
    }

    /// <summary>
    /// 记录错题
    /// </summary>
    public void RecordMistake(Exercise exercise, string userAnswer, ExerciseFeedback feedback)
    {
        lock (_lock)
        {
            var recordId = Guid.NewGuid().ToString("N")[..16];

            // 检查是否已有相同习题的错题记录
            var existingRecord = _mistakeRecords.Values
                .FirstOrDefault(m => m.ExerciseId == exercise.ExerciseId && m.KpId == exercise.KpId && !m.IsResolved);

            if (existingRecord != null)
            {
                existingRecord.ErrorCount++;
                existingRecord.UserAnswer = userAnswer;
                existingRecord.ErrorAnalysis = feedback.ErrorAnalysis;
            }
            else
            {
                _mistakeRecords[recordId] = new MistakeRecord
                {
                    RecordId = recordId,
                    UserId = "default",
                    ExerciseId = exercise.ExerciseId,
                    KpId = exercise.KpId,
                    UserAnswer = userAnswer,
                    CorrectAnswer = exercise.CorrectAnswer,
                    ErrorAnalysis = feedback.ErrorAnalysis,
                    CreatedAt = DateTime.UtcNow,
                    IsResolved = false,
                    ErrorCount = 1
                };
            }
        }

        _logger.LogInformation("错题已记录: {ExerciseId}", exercise.ExerciseId);
    }

    /// <summary>
    /// 获取错题本
    /// </summary>
    public List<MistakeRecord> GetMistakeBook(string userId = "default")
    {
        lock (_lock)
        {
            return _mistakeRecords.Values
                .Where(m => m.UserId == userId && !m.IsResolved)
                .OrderByDescending(m => m.CreatedAt)
                .ToList();
        }
    }

    /// <summary>
    /// 解决错题
    /// </summary>
    public bool ResolveMistake(string recordId)
    {
        lock (_lock)
        {
            if (_mistakeRecords.TryGetValue(recordId, out var record))
            {
                record.IsResolved = true;
                record.ResolvedAt = DateTime.UtcNow;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 计算掌握度更新
    /// </summary>
    public float CalculateMasteryUpdate(float currentMastery, List<ExerciseFeedback> feedbacks)
    {
        if (feedbacks.Count == 0)
            return currentMastery;

        var totalAdjustment = feedbacks
            .Where(f => f.MasteryAdjustment.HasValue)
            .Sum(f => f.MasteryAdjustment!.Value);

        // 限制最大变化幅度
        var maxChange = 0.2f;
        var adjustment = Math.Clamp(totalAdjustment, -maxChange, maxChange);

        return Math.Clamp(currentMastery + adjustment, 0f, 1f);
    }
}
