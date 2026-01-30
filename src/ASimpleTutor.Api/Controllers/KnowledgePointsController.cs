using ASimpleTutor.Api.Configuration;
using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ASimpleTutor.Api.Controllers;

/// <summary>
/// 知识点控制器
/// </summary>
[ApiController]
[Route("api/v1/knowledge-points")]
public class KnowledgePointsController : ControllerBase
{
    private static KnowledgeSystem? _knowledgeSystem;
    private static readonly object _lock = new();

    public static void SetKnowledgeSystem(KnowledgeSystem ks)
    {
        lock (_lock)
        {
            _knowledgeSystem = ks;
        }
    }

    /// <summary>
    /// 获取精要速览
    /// </summary>
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(
        [FromQuery] string kpId,
        [FromServices] IServiceProvider serviceProvider,
        [FromServices] ILogger<KnowledgePointsController> logger)
    {
        if (_knowledgeSystem == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "请先激活书籍目录并构建知识体系" } });
        }

        if (string.IsNullOrEmpty(kpId))
        {
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = "kpId 不能为空" } });
        }

        var kp = _knowledgeSystem.KnowledgePoints.FirstOrDefault(p => p.KpId == kpId);
        if (kp == null)
        {
            return NotFound(new { error = new { code = "KP_NOT_FOUND", message = $"知识点不存在: {kpId}" } });
        }

        try
        {
            var generator = serviceProvider.GetRequiredService<ILearningGenerator>();
            var learningPack = await generator.GenerateAsync(kp);

            return Ok(new
            {
                id = kp.KpId,
                title = kp.Title,
                overview = learningPack.Summary,
                generatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取精要速览失败: {KpId}", kpId);
            return Problem(new { error = new { code = "GENERATION_FAILED", message = "获取精要速览失败" } }.ToString());
        }
    }

    /// <summary>
    /// 获取原文对照
    /// </summary>
    [HttpGet("source-content")]
    public IActionResult GetSourceContent(
        [FromQuery] string kpId,
        [FromServices] IServiceProvider serviceProvider)
    {
        if (_knowledgeSystem == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "请先激活书籍目录并构建知识体系" } });
        }

        if (string.IsNullOrEmpty(kpId))
        {
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = "kpId 不能为空" } });
        }

        var kp = _knowledgeSystem.KnowledgePoints.FirstOrDefault(p => p.KpId == kpId);
        if (kp == null)
        {
            return NotFound(new { error = new { code = "KP_NOT_FOUND", message = $"知识点不存在: {kpId}" } });
        }

        var sourceTracker = serviceProvider.GetRequiredService<ISourceTracker>();
        var snippets = sourceTracker.GetSources(kp.SnippetIds);

        var sourceItems = snippets.Select(s => new
        {
            filePath = s.FilePath,
            fileName = Path.GetFileName(s.FilePath),
            headingPath = s.HeadingPath,
            lineStart = s.StartLine,
            lineEnd = s.EndLine,
            content = s.Content
        }).ToList();

        return Ok(new
        {
            id = kp.KpId,
            title = kp.Title,
            sourceItems
        });
    }

    /// <summary>
    /// 获取层次展开内容
    /// </summary>
    [HttpGet("detailed-content")]
    public async Task<IActionResult> GetDetailedContent(
        [FromQuery] string kpId,
        [FromQuery] string? level,
        [FromServices] IServiceProvider serviceProvider,
        [FromServices] ILogger<KnowledgePointsController> logger)
    {
        if (_knowledgeSystem == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "请先激活书籍目录并构建知识体系" } });
        }

        if (string.IsNullOrEmpty(kpId))
        {
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = "kpId 不能为空" } });
        }

        var kp = _knowledgeSystem.KnowledgePoints.FirstOrDefault(p => p.KpId == kpId);
        if (kp == null)
        {
            return NotFound(new { error = new { code = "KP_NOT_FOUND", message = $"知识点不存在: {kpId}" } });
        }

        try
        {
            var generator = serviceProvider.GetRequiredService<ILearningGenerator>();
            var learningPack = await generator.GenerateAsync(kp);

            var response = new
            {
                id = kp.KpId,
                title = kp.Title,
                levels = new
                {
                    brief = new
                    {
                        content = learningPack.Levels.FirstOrDefault(l => l.Level == 1)?.Content ?? "",
                        keyPoints = learningPack.Summary.KeyPoints
                    },
                    detailed = new
                    {
                        content = learningPack.Levels.FirstOrDefault(l => l.Level == 2)?.Content ?? "",
                        examples = new List<string>()
                    },
                    deep = new
                    {
                        content = learningPack.Levels.FirstOrDefault(l => l.Level == 3)?.Content ?? "",
                        relatedPatterns = new List<string>(),
                        bestPractices = new List<string>()
                    }
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取层次展开内容失败: {KpId}", kpId);
            return Problem(new { error = new { code = "GENERATION_FAILED", message = "获取层次展开内容失败" } }.ToString());
        }
    }
}
