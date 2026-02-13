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
    /// 获取书籍中心列表
    /// </summary>
    [HttpGet("hubs")]
    public IActionResult GetBooks()
    {
        var books = _config.BookHubs
            .Where(b => b.Enabled)
            .OrderBy(b => b.Order)
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.Path,
                IsActive = b.Id == _config.ActiveBookHubId
            });

        return Ok(new { items = books, activeId = _config.ActiveBookHubId });
    }

    /// <summary>
    /// 切换书籍
    /// </summary>
    [HttpPost("activate")]
    public async Task<IActionResult> ActivateBook([FromBody] ActivateBookRequest request, [FromServices] IServiceProvider serviceProvider)
    {
        var book = _config.BookHubs.FirstOrDefault(b => b.Id == request.BookHubId);
        if (book == null)
        {
            return NotFound(new { error = new { code = "BOOKHUB_NOT_FOUND", message = $"书籍中心不存在: {request.BookHubId}" } });
        }

        if (!Directory.Exists(book.Path))
        {
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = $"目录不存在: {book.Path}" } });
        }

        _logger.LogInformation("激活书籍中心: {Id}", request.BookHubId);
        _config.ActiveBookHubId = request.BookHubId;

        return Ok(new { success = true, message = $"已激活书籍中心: {book.Name}" });
    }

    /// <summary>
    /// 触发扫描（后台异步执行）
    /// </summary>
    [HttpPost("scan")]
    public IActionResult TriggerScan([FromServices] IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(_config.ActiveBookHubId))
        {
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = "请先激活书籍中心" } });
        }

        var bookHub = _config.BookHubs.FirstOrDefault(b => b.Id == _config.ActiveBookHubId);
        if (bookHub == null)
        {
            return NotFound(new { error = new { code = "BOOKHUB_NOT_FOUND", message = $"书籍中心不存在: {_config.ActiveBookHubId}" } });
        }

        if (!Directory.Exists(bookHub.Path))
        {
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = $"目录不存在: {bookHub.Path}" } });
        }

        var progressService = serviceProvider.GetRequiredService<ScanProgressService>();

        // 检查是否已有正在进行的扫描任务
        var existingProgress = progressService.GetProgress(_config.ActiveBookHubId);
        if (existingProgress != null && existingProgress.Status == "scanning")
        {
            return Ok(new
            {
                success = true,
                taskId = existingProgress.TaskId,
                status = "scanning",
                message = "扫描任务已在进行中"
            });
        }

        var taskId = Guid.NewGuid().ToString();
        _logger.LogInformation("启动后台扫描任务: {TaskId}, BookHubId: {BookHubId}", taskId, _config.ActiveBookHubId);

        // 启动后台任务
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var scopedProvider = scope.ServiceProvider;
                var knowledgeBuilder = scopedProvider.GetRequiredService<IKnowledgeBuilder>();
                var store = scopedProvider.GetRequiredService<KnowledgeSystemStore>();
                var progress = scopedProvider.GetRequiredService<ScanProgressService>();
                var logger = scopedProvider.GetRequiredService<ILogger<BooksController>>();

                var (knowledgeSystem, documents) = await knowledgeBuilder.BuildAsync(
                    _config.ActiveBookHubId,
                    bookHub.Path);

                // 保存到持久化存储
                await store.SaveAsync(knowledgeSystem, documents);

                // 更新内存中的知识系统
                AdminController.SetKnowledgeSystem(knowledgeSystem);
                KnowledgePointsController.SetKnowledgeSystem(knowledgeSystem);
                ChaptersController.SetKnowledgeSystem(knowledgeSystem);
                ExercisesController.SetKnowledgeSystem(knowledgeSystem);

                logger.LogInformation("后台扫描任务完成: {TaskId}, 共 {Count} 个知识点",
                    taskId, knowledgeSystem.KnowledgePoints.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "后台扫描任务失败: {TaskId}", taskId);
            }
        });

        return Ok(new
        {
            success = true,
            taskId = taskId,
            status = "scanning",
            message = "扫描任务已启动"
        });
    }

    /// <summary>
    /// 清除已保存的知识系统
    /// </summary>
    [HttpDelete("cache")]
    public IActionResult ClearCache([FromServices] IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(_config.ActiveBookHubId))
        {
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = "请先激活书籍中心" } });
        }

        var store = serviceProvider.GetRequiredService<KnowledgeSystemStore>();
        var deleted = store.Delete(_config.ActiveBookHubId);

        if (deleted)
        {
            // 清除内存中的知识系统
            AdminController.SetKnowledgeSystem(null);
            KnowledgePointsController.SetKnowledgeSystem(null);
            ChaptersController.SetKnowledgeSystem(null);
            ExercisesController.SetKnowledgeSystem(null);

            _logger.LogInformation("已清除知识系统缓存: {BookHubId}", _config.ActiveBookHubId);
            return Ok(new { success = true, message = "缓存已清除" });
        }

        return Ok(new { success = false, message = "无缓存可清除" });
    }

    /// <summary>
    /// 获取扫描进度
    /// </summary>
    [HttpGet("scan-progress")]
    public IActionResult GetScanProgress([FromServices] ScanProgressService progressService)
    {
        if (string.IsNullOrEmpty(_config.ActiveBookHubId))
        {
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = "请先激活书籍中心" } });
        }

        var progress = progressService.GetProgress(_config.ActiveBookHubId);
        if (progress == null)
        {
            return Ok(new
            {
                taskId = (string?)null,
                status = "idle",
                currentStage = "",
                progressPercent = 0,
                message = "无进行中的扫描任务",
                processedKpCount = 0,
                totalKpCount = 0
            });
        }

        return Ok(new
        {
            taskId = progress.TaskId,
            status = progress.Status,
            currentStage = progress.CurrentStage,
            progressPercent = progress.ProgressPercent,
            message = progress.Message,
            processedKpCount = progress.ProcessedKpCount,
            totalKpCount = progress.TotalKpCount,
            error = progress.Error
        });
    }
}

public class ActivateBookRequest
{
    public string BookHubId { get; set; } = string.Empty;
}
