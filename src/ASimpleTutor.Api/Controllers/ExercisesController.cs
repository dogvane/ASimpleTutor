using ASimpleTutor.Api.Configuration;
using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ASimpleTutor.Api.Controllers;

/// <summary>
/// 习题控制器
/// </summary>
[ApiController]
[Route("api/v1")]
public class ExercisesController : ControllerBase
{
    private static KnowledgeSystem? _knowledgeSystem;
    private static readonly Dictionary<string, Exercise> _exerciseCache = new();
    private static readonly object _lock = new();

    public static void SetKnowledgeSystem(KnowledgeSystem? ks)
    {
        lock (_lock)
        {
            _knowledgeSystem = ks;
        }
    }

    private bool ValidateRequest(string kpId, out KnowledgePoint? kp, out IActionResult? errorResult)
    {
        kp = null;
        errorResult = null;

        if (_knowledgeSystem == null)
        {
            errorResult = NotFound(new { error = new { code = "NOT_FOUND", message = "请先激活书籍目录并构建知识体系" } });
            return false;
        }

        if (string.IsNullOrEmpty(kpId))
        {
            errorResult = BadRequest(new { error = new { code = "BAD_REQUEST", message = "kpId 不能为空" } });
            return false;
        }

        kp = _knowledgeSystem.KnowledgePoints.FirstOrDefault(p => p.KpId == kpId);
        if (kp == null)
        {
            errorResult = NotFound(new { error = new { code = "KP_NOT_FOUND", message = $"知识点不存在: {kpId}" } });
            return false;
        }

        return true;
    }

    private bool ValidateRequest(string kpId, out IActionResult? errorResult)
    {
        return ValidateRequest(kpId, out _, out errorResult);
    }

    /// <summary>
    /// 检查习题状态
    /// </summary>
    [HttpGet("knowledge-points/exercises/status")]
    public async Task<IActionResult> GetExerciseStatus(
        [FromQuery] string kpId,
        [FromServices] IServiceProvider serviceProvider,
        [FromServices] ILogger<ExercisesController> logger)
    {
        if (!ValidateRequest(kpId, out var kp, out var errorResult)) return errorResult!;

        lock (_lock)
        {
            var exercisesForKp = _exerciseCache.Values.Where(e => e.KpId == kpId).ToList();

            if (exercisesForKp.Count > 0)
            {
                return Ok(new { kpId, hasExercises = true, exerciseCount = exercisesForKp.Count, status = "ready", generatedAt = DateTime.UtcNow });
            }
        }

        try
        {
            var generator = serviceProvider.GetRequiredService<IExerciseGenerator>();
            var exercises = await generator.GenerateAsync(kp!, 3);

            lock (_lock)
            {
                foreach (var ex in exercises)
                {
                    _exerciseCache[ex.ExerciseId] = ex;
                }
            }

            return Ok(new { kpId, hasExercises = true, exerciseCount = exercises.Count, status = "ready", generatedAt = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "生成习题失败: {KpId}", kpId);
            return Ok(new { kpId, hasExercises = false, status = "generating", message = "习题生成中..." });
        }
    }

    /// <summary>
    /// 获取习题列表
    /// </summary>
    [HttpGet("knowledge-points/exercises")]
    public IActionResult GetExercises([FromQuery] string kpId)
    {
        if (!ValidateRequest(kpId, out var kp, out var errorResult)) return errorResult!;

        List<Exercise> exercises;
        lock (_lock)
        {
            exercises = _exerciseCache.Values.Where(e => e.KpId == kpId).ToList();
        }

        if (exercises.Count == 0)
        {
            return Ok(new { kpId, items = new List<object>() });
        }

        var items = exercises.Select(e => new { id = e.ExerciseId, type = e.Type.ToString().ToLower(), question = e.Question, options = e.Options, answer = string.Empty }).ToList();
        return Ok(new { kpId, items });
    }

    /// <summary>
    /// 提交答案
    /// </summary>
    [HttpPost("exercises/submit")]
    public async Task<IActionResult> SubmitAnswer(
        [FromBody] SubmitAnswerRequest request,
        [FromServices] IServiceProvider serviceProvider,
        [FromServices] ILogger<ExercisesController> logger)
    {
        if (string.IsNullOrEmpty(request.ExerciseId))
        {
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = "exerciseId 不能为空" } });
        }

        Exercise? exercise;
        lock (_lock)
        {
            if (!_exerciseCache.TryGetValue(request.ExerciseId, out exercise))
            {
                return NotFound(new { error = new { code = "EXERCISE_NOT_FOUND", message = $"习题不存在: {request.ExerciseId}" } });
            }
        }

        try
        {
            var feedback = serviceProvider.GetRequiredService<IExerciseFeedback>();
            var result = await feedback.JudgeAsync(exercise, request.Answer);
            return Ok(new { exerciseId = exercise.ExerciseId, correct = result.IsCorrect, explanation = result.Explanation, referenceAnswer = result.ReferenceAnswer });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "评判答案失败: {ExerciseId}", request.ExerciseId);
            return Problem(new { error = new { code = "GENERATION_FAILED", message = "评判答案失败" } }.ToString());
        }
    }

    /// <summary>
    /// 刷新所有习题（清空缓存并重新生成）
    /// </summary>
    [HttpPost("exercises/refresh")]
    public async Task<IActionResult> RefreshExercises(
        [FromServices] IServiceProvider serviceProvider,
        [FromServices] ILogger<ExercisesController> logger)
    {
        if (_knowledgeSystem == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "请先激活书籍目录并构建知识体系" } });
        }

        try
        {
            // 清空所有习题缓存
            lock (_lock)
            {
                _exerciseCache.Clear();
            }

            // 为所有知识点重新生成习题（并行异步）
            var generator = serviceProvider.GetRequiredService<IExerciseGenerator>();
            var totalExercises = 0;
            var knowledgePointCount = 0;

            await Parallel.ForEachAsync(_knowledgeSystem.KnowledgePoints, async (kp, ct) =>
            {
                try
                {
                    var exercises = await generator.GenerateAsync(kp, 3, ct);

                    lock (_lock)
                    {
                        foreach (var ex in exercises)
                        {
                            _exerciseCache[ex.ExerciseId] = ex;
                        }
                    }

                    Interlocked.Add(ref totalExercises, exercises.Count);
                    Interlocked.Increment(ref knowledgePointCount);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "为知识点生成习题失败: {KpId}", kp.KpId);
                }
            });

            return Ok(new { message = "习题刷新完成", knowledgePointCount, totalExerciseCount = totalExercises, status = "ready", generatedAt = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "刷新习题失败");
            return StatusCode(500, new { error = new { code = "GENERATION_FAILED", message = "刷新习题失败" } });
        }
    }

    /// <summary>
    /// 批量提交并获取反馈
    /// </summary>
    [HttpPost("exercises/feedback")]
    public async Task<IActionResult> SubmitFeedback(
        [FromBody] SubmitFeedbackRequest request,
        [FromServices] IServiceProvider serviceProvider,
        [FromServices] ILogger<ExercisesController> logger)
    {
        if (_knowledgeSystem == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "请先激活书籍目录并构建知识体系" } });
        }

        if (string.IsNullOrEmpty(request.KpId))
        {
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = "kpId 不能为空" } });
        }

        if (request.Answers == null || request.Answers.Count == 0)
        {
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = "answers 不能为空" } });
        }

        var feedbackService = serviceProvider.GetRequiredService<IExerciseFeedback>();
        var results = new List<object>();
        int correct = 0;
        int incorrect = 0;

        foreach (var answer in request.Answers)
        {
            Exercise? exercise;
            lock (_lock)
            {
                _exerciseCache.TryGetValue(answer.ExerciseId, out exercise);
            }

            if (exercise == null)
            {
                results.Add(new { exerciseId = answer.ExerciseId, correct = false, explanation = "习题不存在", referenceAnswer = "" });
                incorrect++;
                continue;
            }

            try
            {
                var result = await feedbackService.JudgeAsync(exercise, answer.Answer);
                var isCorrect = result.IsCorrect ?? false;
                results.Add(new { exerciseId = exercise.ExerciseId, correct = isCorrect, explanation = result.Explanation, referenceAnswer = result.ReferenceAnswer });

                if (isCorrect) correct++; else incorrect++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "评判答案失败: {ExerciseId}", answer.ExerciseId);
                results.Add(new { exerciseId = answer.ExerciseId, correct = false, explanation = "评判失败", referenceAnswer = "" });
                incorrect++;
            }
        }

        return Ok(new { kpId = request.KpId, summary = new { total = request.Answers.Count, correct, incorrect }, items = results });
    }
}

public class SubmitAnswerRequest
{
    public string ExerciseId { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}

public class SubmitFeedbackRequest
{
    public string KpId { get; set; } = string.Empty;
    public List<AnswerItem> Answers { get; set; } = new();
}

public class AnswerItem
{
    public string ExerciseId { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}
