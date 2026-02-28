using ASimpleTutor.Api.Configuration;
using ASimpleTutor.Api.Controllers;
using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ASimpleTutor.Api.Controllers;

/// <summary>
/// 服务管理控制器
/// </summary>
[ApiController]
[Route("api/v1/service")]
public class ServiceController : ControllerBase
{
    private readonly ILogger<ServiceController> _logger;
    private readonly AppConfig _config;

    public ServiceController(ILogger<ServiceController> logger, AppConfig config)
    {
        _logger = logger;
        _config = config;
    }

    /// <summary>
    /// 重启服务
    /// </summary>
    [HttpPost("restart")]
    public async Task<IActionResult> RestartService([FromServices] IServiceProvider serviceProvider)
    {
        try
        {
            _logger.LogInformation("接收到服务重启请求");

            // 清除内存中的知识系统
            AdminController.SetKnowledgeSystem(null);
            KnowledgePointsController.SetKnowledgeSystem(null);
            ChaptersController.SetKnowledgeSystem(null);
            ExercisesController.SetKnowledgeSystem(null);

            _logger.LogInformation("服务重启完成");

            return Ok(new
            {
                success = true,
                message = "服务重启成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "服务重启失败");
            return StatusCode(500, new
            {
                error = new
                {
                    code = "SERVICE_RESTART_FAILED",
                    message = "服务重启失败: " + ex.Message
                }
            });
        }
    }

    /// <summary>
    /// 检查服务状态
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetServiceStatus()
    {
        try
        {
            _logger.LogInformation("接收到服务状态检查请求");

            return Ok(new
            {
                success = true,
                status = "running",
                message = "服务运行正常",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "服务状态检查失败");
            return StatusCode(500, new
            {
                error = new
                {
                    code = "STATUS_CHECK_FAILED",
                    message = "服务状态检查失败: " + ex.Message
                }
            });
        }
    }

    /// <summary>
    /// 检查目录文件更新
    /// </summary>
    [HttpPost("check-files")]
    public async Task<IActionResult> CheckFilesUpdate([FromServices] IServiceProvider serviceProvider)
    {
        try
        {
            _logger.LogInformation("接收到目录文件检查请求");

            if (string.IsNullOrEmpty(_config.ActiveBookHubId))
            {
                return BadRequest(new { error = new { code = "BAD_REQUEST", message = "请先激活书籍中心" } });
            }

            var bookHub = _config.BookHubs.FirstOrDefault(b => b.Id == _config.ActiveBookHubId);
            if (bookHub == null)
            {
                return NotFound(new { error = new { code = "BOOKHUB_NOT_FOUND", message = $"书籍中心不存在: {_config.ActiveBookHubId}" } });
            }

            if (!System.IO.Directory.Exists(bookHub.Path))
            {
                return BadRequest(new { error = new { code = "BAD_REQUEST", message = $"目录不存在: {bookHub.Path}" } });
            }

            // 启动后台任务检查文件更新
            var taskId = System.Guid.NewGuid().ToString();
            _logger.LogInformation("启动后台文件检查任务: {TaskId}, BookHubId: {BookHubId}", taskId, _config.ActiveBookHubId);

            // 启动后台任务
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = serviceProvider.CreateScope();
                    var scopedProvider = scope.ServiceProvider;
                    var knowledgeBuilder = scopedProvider.GetRequiredService<IKnowledgeBuilder>();
                    var store = scopedProvider.GetRequiredService<KnowledgeSystemStore>();
                    var logger = scopedProvider.GetRequiredService<ILogger<ServiceController>>();

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

                    logger.LogInformation("后台文件检查任务完成: {TaskId}, 共 {Count} 个知识点",
                        taskId, knowledgeSystem.KnowledgePoints.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "后台文件检查任务失败: {TaskId}", taskId);
                }
            });

            return Ok(new
            {
                success = true,
                taskId = taskId,
                status = "checking",
                message = "文件检查任务已启动"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件检查任务触发失败");
            return StatusCode(500, new
            {
                error = new
                {
                    code = "CHECK_FILES_FAILED",
                    message = "文件检查任务触发失败: " + ex.Message
                }
            });
        }
    }
}
