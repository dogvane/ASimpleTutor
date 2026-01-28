using ASimpleTutor.Api.Configuration;
using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ASimpleTutor.Api.Endpoints;

/// <summary>
/// 习题端点
/// </summary>
public static class ExerciseEndpoints
{
    private static KnowledgeSystem? _knowledgeSystem;
    private static readonly Dictionary<string, Exercise> _exerciseCache = new();
    private static readonly object _lock = new();

    public static void MapExerciseEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/exercises");

        group.MapPost("/generate", GenerateExercises);
        group.MapGet("/{id}", GetExercise);
        group.MapPost("/{id}/submit", SubmitAnswer);
    }

    public static void SetKnowledgeSystem(KnowledgeSystem ks)
    {
        lock (_lock)
        {
            _knowledgeSystem = ks;
        }
    }

    private static async Task<IResult> GenerateExercises(
        [FromBody] GenerateExerciseRequest request,
        [FromServices] IServiceProvider serviceProvider,
        [FromServices] ILogger logger)
    {
        if (_knowledgeSystem == null)
        {
            return Results.NotFound("请先激活书籍目录并构建知识体系");
        }

        var kp = _knowledgeSystem.KnowledgePoints.FirstOrDefault(p => p.KpId == request.KpId);
        if (kp == null)
        {
            return Results.NotFound($"知识点不存在: {request.KpId}");
        }

        try
        {
            var generator = serviceProvider.GetRequiredService<IExerciseGenerator>();
            var exercises = await generator.GenerateAsync(kp, request.Count ?? 1);

            // 缓存习题
            lock (_lock)
            {
                foreach (var ex in exercises)
                {
                    _exerciseCache[ex.ExerciseId] = ex;
                }
            }

            return Results.Ok(exercises);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "生成习题失败: {KpId}", request.KpId);
            return Results.Problem("生成习题失败");
        }
    }

    private static IResult GetExercise(string id)
    {
        lock (_lock)
        {
            if (!_exerciseCache.TryGetValue(id, out var exercise))
            {
                return Results.NotFound($"习题不存在: {id}");
            }

            return Results.Ok(new
            {
                exercise.ExerciseId,
                exercise.KpId,
                exercise.Type,
                exercise.Question,
                exercise.Options,
                exercise.KeyPoints
            });
        }
    }

    private static async Task<IResult> SubmitAnswer(
        string id,
        [FromBody] SubmitAnswerRequest request,
        [FromServices] IServiceProvider serviceProvider,
        [FromServices] ILogger logger)
    {
        Exercise? exercise;
        lock (_lock)
        {
            if (!_exerciseCache.TryGetValue(id, out exercise))
            {
                return Results.NotFound($"习题不存在: {id}");
            }
        }

        try
        {
            var feedback = serviceProvider.GetRequiredService<IExerciseFeedback>();
            var result = await feedback.JudgeAsync(exercise, request.Answer);

            return Results.Ok(new
            {
                IsCorrect = result.IsCorrect,
                Explanation = result.Explanation,
                ReferenceAnswer = result.ReferenceAnswer,
                CoveredPoints = result.CoveredPoints,
                MissingPoints = result.MissingPoints
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "评判答案失败: {ExerciseId}", id);
            return Results.Problem("评判答案失败");
        }
    }
}

public class GenerateExerciseRequest
{
    public string KpId { get; set; } = string.Empty;
    public int? Count { get; set; }
}

public class SubmitAnswerRequest
{
    public string Answer { get; set; } = string.Empty;
}
