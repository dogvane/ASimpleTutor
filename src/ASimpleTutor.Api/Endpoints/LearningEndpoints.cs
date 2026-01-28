using ASimpleTutor.Api.Configuration;
using ASimpleTutor.Api.Endpoints;
using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ASimpleTutor.Api.Endpoints;

/// <summary>
/// 学习内容端点
/// </summary>
public static class LearningEndpoints
{
    private static KnowledgeSystem? _knowledgeSystem;
    private static readonly object _lock = new();

    public static void MapLearningEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/learning");

        group.MapGet("/{kpId}", GetLearningPack);
    }

    public static void SetKnowledgeSystem(KnowledgeSystem ks)
    {
        lock (_lock)
        {
            _knowledgeSystem = ks;
        }
    }

    private static async Task<IResult> GetLearningPack(
        string kpId,
        [FromServices] IServiceProvider serviceProvider,
        [FromServices] ILogger logger)
    {
        if (_knowledgeSystem == null)
        {
            return Results.NotFound("请先激活书籍目录并构建知识体系");
        }

        var kp = _knowledgeSystem.KnowledgePoints.FirstOrDefault(p => p.KpId == kpId);
        if (kp == null)
        {
            return Results.NotFound($"知识点不存在: {kpId}");
        }

        try
        {
            var generator = serviceProvider.GetRequiredService<ILearningGenerator>();
            var learningPack = await generator.GenerateAsync(kp);

            // 展开原文片段
            var sourceTracker = serviceProvider.GetRequiredService<ISourceTracker>();
            var snippets = sourceTracker.GetSources(learningPack.SnippetIds);
            var snippetList = snippets.Select(s => new
            {
                s.SnippetId,
                s.FilePath,
                s.HeadingPath,
                s.Content,
                s.StartLine,
                s.EndLine
            }).ToList();

            var response = new
            {
                learningPack.KpId,
                learningPack.Summary,
                learningPack.Levels,
                RelatedKpIds = learningPack.RelatedKpIds,
                Snippets = snippetList
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取学习内容失败: {KpId}", kpId);
            return Results.Problem("获取学习内容失败");
        }
    }
}
