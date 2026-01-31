using ASimpleTutor.Api.Configuration;
using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace ASimpleTutor.Api.Controllers;

/// <summary>
/// 书籍管理控制器
/// </summary>
[ApiController]
[Route("api/v1/books")]
public class BooksController : ControllerBase
{
    private readonly AppConfig _config;
    private readonly ILogger<BooksController> _logger;

    public BooksController(AppConfig config, ILogger<BooksController> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// 获取书籍目录列表
    /// </summary>
    [HttpGet("roots")]
    public IActionResult GetBooks()
    {
        var books = _config.BookRoots
            .Where(b => b.Enabled)
            .OrderBy(b => b.Order)
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.Path,
                IsActive = b.Id == _config.ActiveBookRootId
            });

        return Ok(new { items = books, activeId = _config.ActiveBookRootId });
    }

    /// <summary>
    /// 切换书籍
    /// </summary>
    [HttpPost("activate")]
    public async Task<IActionResult> ActivateBook([FromBody] ActivateBookRequest request, [FromServices] IServiceProvider serviceProvider)
    {
        var book = _config.BookRoots.FirstOrDefault(b => b.Id == request.BookRootId);
        if (book == null)
        {
            return NotFound(new { error = new { code = "BOOKROOT_NOT_FOUND", message = $"书籍目录不存在: {request.BookRootId}" } });
        }

        if (!Directory.Exists(book.Path))
        {
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = $"目录不存在: {book.Path}" } });
        }

        _logger.LogInformation("激活书籍目录: {Id}", request.BookRootId);
        _config.ActiveBookRootId = request.BookRootId;

        return Ok(new { success = true, message = $"已激活书籍目录: {book.Name}" });
    }

    /// <summary>
    /// 触发扫描
    /// </summary>
    [HttpPost("scan")]
    public async Task<IActionResult> TriggerScan([FromServices] IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(_config.ActiveBookRootId))
        {
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = "请先激活书籍目录" } });
        }

        var bookRoot = _config.BookRoots.FirstOrDefault(b => b.Id == _config.ActiveBookRootId);
        if (bookRoot == null)
        {
            return NotFound(new { error = new { code = "BOOKROOT_NOT_FOUND", message = $"书籍目录不存在: {_config.ActiveBookRootId}" } });
        }

        if (!Directory.Exists(bookRoot.Path))
        {
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = $"目录不存在: {bookRoot.Path}" } });
        }

        _logger.LogInformation("开始构建知识体系: {BookRootId}", _config.ActiveBookRootId);

        try
        {
            var knowledgeBuilder = serviceProvider.GetRequiredService<IKnowledgeBuilder>();
            var sourceTracker = serviceProvider.GetRequiredService<ISourceTracker>();
            var store = serviceProvider.GetRequiredService<KnowledgeSystemStore>();

            sourceTracker.Clear();

            var knowledgeSystem = await knowledgeBuilder.BuildAsync(
                _config.ActiveBookRootId,
                bookRoot.Path);

            // 保存到持久化存储
            await store.SaveAsync(knowledgeSystem);

            AdminController.SetKnowledgeSystem(knowledgeSystem);
            KnowledgePointsController.SetKnowledgeSystem(knowledgeSystem);
            ChaptersController.SetKnowledgeSystem(knowledgeSystem);
            ExercisesController.SetKnowledgeSystem(knowledgeSystem);

            _logger.LogInformation("知识体系构建完成，共 {Count} 个知识点，已保存到本地",
                knowledgeSystem.KnowledgePoints.Count);

            return Ok(new { success = true, taskId = Guid.NewGuid().ToString(), status = "completed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "知识体系构建失败");
            return Problem(new { error = new { code = "SCAN_FAILED", message = "知识体系构建失败: " + ex.Message } }.ToString());
        }
    }

    /// <summary>
    /// 清除已保存的知识系统
    /// </summary>
    [HttpDelete("cache")]
    public IActionResult ClearCache([FromServices] IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(_config.ActiveBookRootId))
        {
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = "请先激活书籍目录" } });
        }

        var store = serviceProvider.GetRequiredService<KnowledgeSystemStore>();
        var deleted = store.Delete(_config.ActiveBookRootId);

        if (deleted)
        {
            // 清除内存中的知识系统
            AdminController.SetKnowledgeSystem(null);
            KnowledgePointsController.SetKnowledgeSystem(null);
            ChaptersController.SetKnowledgeSystem(null);
            ExercisesController.SetKnowledgeSystem(null);

            _logger.LogInformation("已清除知识系统缓存: {BookRootId}", _config.ActiveBookRootId);
            return Ok(new { success = true, message = "缓存已清除" });
        }

        return Ok(new { success = false, message = "无缓存可清除" });
    }
}

public class ActivateBookRequest
{
    public string BookRootId { get; set; } = string.Empty;
}
