using ASimpleTutor.Api.Configuration;
using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ASimpleTutor.Api.Controllers;

/// <summary>
/// 管理控制器
/// </summary>
[ApiController]
[Route("api/v1/admin")]
public class AdminController : ControllerBase
{
    private static KnowledgeSystem? _knowledgeSystem;
    private static readonly object _lock = new();

    public static KnowledgeSystem? GetKnowledgeSystem()
    {
        lock (_lock)
        {
            return _knowledgeSystem;
        }
    }

    public static void SetKnowledgeSystem(KnowledgeSystem ks)
    {
        lock (_lock)
        {
            _knowledgeSystem = ks;
        }
    }

    /// <summary>
    /// 构建知识体系
    /// </summary>
    [HttpPost("build")]
    public async Task<IActionResult> BuildKnowledgeSystem(
        [FromServices] AppConfig config,
        [FromServices] IServiceProvider serviceProvider,
        [FromServices] ILogger<AdminController> logger)
    {
        if (string.IsNullOrEmpty(config.ActiveBookRootId))
        {
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = "请先激活书籍目录" } });
        }

        var bookRoot = config.BookRoots.FirstOrDefault(b => b.Id == config.ActiveBookRootId);
        if (bookRoot == null)
        {
            return BadRequest(new { error = new { code = "BOOKROOT_NOT_FOUND", message = $"书籍目录不存在: {config.ActiveBookRootId}" } });
        }

        if (!Directory.Exists(bookRoot.Path))
        {
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = $"目录不存在: {bookRoot.Path}" } });
        }

        logger.LogInformation("开始构建知识体系: {BookRootId}", config.ActiveBookRootId);

        try
        {
            var knowledgeBuilder = serviceProvider.GetRequiredService<IKnowledgeBuilder>();
            var sourceTracker = serviceProvider.GetRequiredService<ISourceTracker>();

            sourceTracker.Clear();

            var knowledgeSystem = await knowledgeBuilder.BuildAsync(
                config.ActiveBookRootId,
                bookRoot.Path);

            lock (_lock)
            {
                _knowledgeSystem = knowledgeSystem;
            }

            KnowledgePointsController.SetKnowledgeSystem(knowledgeSystem);
            ChaptersController.SetKnowledgeSystem(knowledgeSystem);
            ExercisesController.SetKnowledgeSystem(knowledgeSystem);

            logger.LogInformation("知识体系构建完成，共 {Count} 个知识点",
                knowledgeSystem.KnowledgePoints.Count);

            return Ok(new
            {
                success = true,
                message = "知识体系构建完成",
                knowledgePointCount = knowledgeSystem.KnowledgePoints.Count,
                bookRootId = config.ActiveBookRootId
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "知识体系构建失败");
            return Problem(new { error = new { code = "SCAN_FAILED", message = "知识体系构建失败: " + ex.Message } }.ToString());
        }
    }

    /// <summary>
    /// 获取状态
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus([FromServices] AppConfig config)
    {
        var hasSystem = GetKnowledgeSystem() != null;
        return Ok(new
        {
            activeBookRootId = config.ActiveBookRootId,
            hasKnowledgeSystem = hasSystem,
            timestamp = DateTime.UtcNow
        });
    }
}
