using ASimpleTutor.Api.Configuration;
using ASimpleTutor.Api.Endpoints;
using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ASimpleTutor.Api.Endpoints;

/// <summary>
/// 管理端点
/// </summary>
public static class AdminEndpoints
{
    private static KnowledgeSystem? _knowledgeSystem;
    private static readonly object _lock = new();

    public static void MapAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin");

        group.MapPost("/build", BuildKnowledgeSystem);
        group.MapGet("/status", GetStatus);
    }

    public static KnowledgeSystem? GetKnowledgeSystem()
    {
        lock (_lock)
        {
            return _knowledgeSystem;
        }
    }

    private static async Task<IResult> BuildKnowledgeSystem(
        [FromServices] AppConfig config,
        [FromServices] IServiceProvider serviceProvider,
        [FromServices] ILogger logger)
    {
        if (string.IsNullOrEmpty(config.ActiveBookRootId))
        {
            return Results.BadRequest("请先激活书籍目录");
        }

        var bookRoot = config.BookRoots.FirstOrDefault(b => b.Id == config.ActiveBookRootId);
        if (bookRoot == null)
        {
            return Results.BadRequest($"书籍目录不存在: {config.ActiveBookRootId}");
        }

        if (!Directory.Exists(bookRoot.Path))
        {
            return Results.BadRequest($"目录不存在: {bookRoot.Path}");
        }

        logger.LogInformation("开始构建知识体系: {BookRootId}", config.ActiveBookRootId);

        try
        {
            // 获取服务
            var knowledgeBuilder = serviceProvider.GetRequiredService<IKnowledgeBuilder>();
            var sourceTracker = serviceProvider.GetRequiredService<ISourceTracker>();

            // 清空现有数据
            sourceTracker.Clear();

            // 构建知识体系
            var knowledgeSystem = await knowledgeBuilder.BuildAsync(
                config.ActiveBookRootId,
                bookRoot.Path);

            // 保存到静态存储
            lock (_lock)
            {
                _knowledgeSystem = knowledgeSystem;
            }

            // 更新其他端点的知识系统引用
            KnowledgeEndpoints.SetKnowledgeSystem(knowledgeSystem);
            LearningEndpoints.SetKnowledgeSystem(knowledgeSystem);
            ExerciseEndpoints.SetKnowledgeSystem(knowledgeSystem);

            logger.LogInformation("知识体系构建完成，共 {Count} 个知识点",
                knowledgeSystem.KnowledgePoints.Count);

            return Results.Ok(new
            {
                Message = "知识体系构建完成",
                KnowledgePointCount = knowledgeSystem.KnowledgePoints.Count,
                BookRootId = config.ActiveBookRootId
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "知识体系构建失败");
            return Results.Problem("知识体系构建失败: " + ex.Message);
        }
    }

    private static IResult GetStatus([FromServices] AppConfig config)
    {
        var hasSystem = GetKnowledgeSystem() != null;
        return Results.Ok(new
        {
            ActiveBookRootId = config.ActiveBookRootId,
            HasKnowledgeSystem = hasSystem,
            Timestamp = DateTime.UtcNow
        });
    }
}
