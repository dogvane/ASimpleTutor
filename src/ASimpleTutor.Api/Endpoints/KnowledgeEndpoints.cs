using ASimpleTutor.Api.Configuration;
using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ASimpleTutor.Api.Endpoints;

/// <summary>
/// 知识点浏览与搜索端点
/// </summary>
public static class KnowledgeEndpoints
{
    private static KnowledgeSystem? _knowledgeSystem;
    private static readonly object _lock = new();

    public static void MapKnowledgeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/knowledge-points");

        group.MapGet("/", GetKnowledgePoints);
        group.MapGet("/search", SearchKnowledgePoints);
        group.MapGet("/{id}", GetKnowledgePoint);
    }

    public static void SetKnowledgeSystem(KnowledgeSystem ks)
    {
        lock (_lock)
        {
            _knowledgeSystem = ks;
        }
    }

    private static IResult GetKnowledgePoints([FromServices] AppConfig config, [FromQuery] string? sort)
    {
        if (_knowledgeSystem == null)
        {
            return Results.NotFound("请先激活书籍目录并构建知识体系");
        }

        var points = _knowledgeSystem.KnowledgePoints;

        // 排序
        if (sort == "importance")
        {
            points = points.OrderByDescending(p => p.Importance).ToList();
        }
        else
        {
            points = points.OrderBy(p => p.ChapterPath.FirstOrDefault()).ToList();
        }

        return Results.Ok(points.Select(p => new
        {
            p.KpId,
            p.Title,
            p.ChapterPath,
            p.Importance
        }));
    }

    private static IResult SearchKnowledgePoints(
        [FromQuery] string q,
        [FromServices] AppConfig config,
        [FromServices] AppConfig appConfig)
    {
        if (_knowledgeSystem == null)
        {
            return Results.NotFound("请先激活书籍目录并构建知识体系");
        }

        if (string.IsNullOrWhiteSpace(q))
        {
            return Results.BadRequest("搜索关键词不能为空");
        }

        var query = q.ToLower();
        var results = _knowledgeSystem.KnowledgePoints
            .Where(p =>
                p.Title.ToLower().Contains(query) ||
                p.Aliases.Any(a => a.ToLower().Contains(query)) ||
                p.ChapterPath.Any(c => c.ToLower().Contains(query)))
            .Take(20)
            .ToList();

        return Results.Ok(results);
    }

    private static IResult GetKnowledgePoint(string id)
    {
        if (_knowledgeSystem == null)
        {
            return Results.NotFound("请先激活书籍目录并构建知识体系");
        }

        var kp = _knowledgeSystem.KnowledgePoints.FirstOrDefault(p => p.KpId == id);
        if (kp == null)
        {
            return Results.NotFound($"知识点不存在: {id}");
        }

        return Results.Ok(kp);
    }
}
