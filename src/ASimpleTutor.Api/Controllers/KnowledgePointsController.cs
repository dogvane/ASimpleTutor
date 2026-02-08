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

    public static void SetKnowledgeSystem(KnowledgeSystem? ks)
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
    public IActionResult GetOverview([FromQuery] string kpId)
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

        // 直接返回预存的学习内容，不需要调用 LLM
        return Ok(new
        {
            id = kp.KpId,
            title = kp.Title,
            overview = kp.Summary ?? new Summary { Definition = "暂无内容" },
            generatedAt = DateTime.UtcNow // 知识体系生成时间
        });
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

        var snippets = kp.SnippetIds
            .Select(id => _knowledgeSystem.Snippets.TryGetValue(id, out var snippet) ? snippet : null)
            .Where(s => s != null)
            .ToList();

        var sourceItems = snippets.Select(s => new
        {
            filePath = s!.FilePath,
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
    public IActionResult GetDetailedContent([FromQuery] string kpId, [FromQuery] string? level)
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

        // 直接返回预存的数据，不需要调用 LLM
        var levels = kp.Levels ?? new List<ContentLevel>();

        return Ok(new
        {
            id = kp.KpId,
            title = kp.Title,
            levels = new
            {
                brief = new
                {
                    content = levels.FirstOrDefault(l => l.Level == 1)?.Content ?? "",
                    keyPoints = kp.Summary?.KeyPoints ?? new List<string>()
                },
                detailed = new
                {
                    content = levels.FirstOrDefault(l => l.Level == 2)?.Content ?? "",
                    examples = new List<string>()
                },
                deep = new
                {
                    content = levels.FirstOrDefault(l => l.Level == 3)?.Content ?? "",
                    relatedPatterns = new List<string>(),
                    bestPractices = new List<string>()
                }
            }
        });
    }
}
