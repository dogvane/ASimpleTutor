using ASimpleTutor.Api.Configuration;
using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using ASimpleTutor.Core.Services;
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

    public static void SetKnowledgeSystem(KnowledgeSystem? ks)
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
        if (string.IsNullOrEmpty(config.ActiveBookHubId))
        {
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = "请先激活书籍中心" } });
        }

        var bookHub = config.BookHubs.FirstOrDefault(b => b.Id == config.ActiveBookHubId);
        if (bookHub == null)
        {
            return BadRequest(new { error = new { code = "BOOKHUB_NOT_FOUND", message = $"书籍中心不存在: {config.ActiveBookHubId}" } });
        }

        if (!Directory.Exists(bookHub.Path))
        {
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = $"目录不存在: {bookHub.Path}" } });
        }

        logger.LogInformation("开始构建知识体系: {BookHubId}", config.ActiveBookHubId);

        try
        {
            var knowledgeBuilder = serviceProvider.GetRequiredService<IKnowledgeBuilder>();

            var (knowledgeSystem, documents) = await knowledgeBuilder.BuildAsync(
                config.ActiveBookHubId,
                bookHub.Path);

            lock (_lock)
            {
                _knowledgeSystem = knowledgeSystem;
            }

            KnowledgePointsController.SetKnowledgeSystem(knowledgeSystem);
            ChaptersController.SetKnowledgeSystem(knowledgeSystem);
            ExercisesController.SetKnowledgeSystem(knowledgeSystem);

            logger.LogInformation("知识体系构建完成，共 {Count} 个知识点，{DocCount} 个文档",
                knowledgeSystem.KnowledgePoints.Count,
                documents?.Count ?? 0);

            return Ok(new
            {
                success = true,
                message = "知识体系构建完成",
                knowledgePointCount = knowledgeSystem.KnowledgePoints.Count,
                documentCount = documents?.Count ?? 0,
                bookHubId = config.ActiveBookHubId
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
    public IActionResult GetStatus(
        [FromServices] AppConfig config,
        [FromServices] KnowledgeSystemStore store)
    {
        var hasSystem = GetKnowledgeSystem() != null;
        var knowledgeSystem = GetKnowledgeSystem();
        var documents = new List<Document>();
        var bookHubName = string.Empty;
        var knowledgePointCount = 0;
        var snippetCount = 0;
        var documentCount = 0;
        dynamic documentsWithSections = new List<object>();

        if (!string.IsNullOrEmpty(config.ActiveBookHubId))
        {
            // 获取书籍中心名称
            var bookHub = config.BookHubs.FirstOrDefault(b => b.Id == config.ActiveBookHubId);
            if (bookHub != null)
            {
                bookHubName = bookHub.Name;
            }

            // 加载知识系统和文档信息
            var loadResult = store.LoadAsync(config.ActiveBookHubId).GetAwaiter().GetResult();
            if (loadResult.KnowledgeSystem != null)
            {
                knowledgeSystem = loadResult.KnowledgeSystem;
                knowledgePointCount = knowledgeSystem.KnowledgePoints.Count;
            }

            if (loadResult.Documents != null)
            {
                documents = loadResult.Documents;
                documentCount = documents.Count;

                // 构建包含章节信息的文档列表
                documentsWithSections = documents.Select(doc => new
                {
                    docId = doc.DocId,
                    title = doc.Title,
                    sections = doc.Sections.Select(section => new
                    {
                        sectionId = section.SectionId,
                        headingPath = section.HeadingPath,
                        subSections = section.SubSections.Select(subSection => new
                        {
                            sectionId = subSection.SectionId,
                            headingPath = subSection.HeadingPath
                        }).ToList()
                    }).ToList()
                }).ToList();
            }
        }

        var status = "完整";
        if (!hasSystem || knowledgePointCount == 0)
        {
            status = "未就绪";
        }

        return Ok(new
        {
            bookHubName = bookHubName,
            knowledgePointCount = knowledgePointCount,
            snippetCount = snippetCount,
            documentCount = documentCount,
            documents = documentsWithSections,
            status = status,
            timestamp = DateTime.UtcNow
        });
    }
}
