using ASimpleTutor.Api.Configuration;
using ASimpleTutor.Core.Models;
using ASimpleTutor.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace ASimpleTutor.Api.Controllers;

/// <summary>
/// 用户学习进度控制器
/// </summary>
[ApiController]
[Route("api/v1/progress")]
public class ProgressController : ControllerBase
{
    private static KnowledgeSystem? _knowledgeSystem;
    private static readonly Dictionary<string, UserProgress> _userProgress = new();
    private static readonly object _lock = new();

    private readonly ExerciseService _exerciseService;
    private readonly ILogger<ProgressController> _logger;

    public ProgressController(
        ExerciseService exerciseService,
        ILogger<ProgressController> logger)
    {
        _exerciseService = exerciseService;
        _logger = logger;
    }

    public static void SetKnowledgeSystem(KnowledgeSystem? ks)
    {
        lock (_lock)
        {
            _knowledgeSystem = ks;
        }
    }

    /// <summary>
    /// 获取用户进度
    /// </summary>
    [HttpGet]
    public IActionResult GetProgress([FromQuery] string kpId, [FromQuery] string userId = "default")
    {
        if (_knowledgeSystem == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "请先激活书籍目录并构建知识体系" } });
        }

        if (string.IsNullOrEmpty(kpId))
        {
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = "kpId 不能为空" } });
        }

        lock (_lock)
        {
            var key = $"{userId}_{kpId}";
            if (_userProgress.TryGetValue(key, out var progress))
            {
                return Ok(new
                {
                    userId = progress.UserId,
                    kpId = progress.KpId,
                    status = progress.Status,
                    masteryLevel = progress.MasteryLevel,
                    reviewCount = progress.ReviewCount,
                    lastReviewTime = progress.LastReviewTime,
                    completedSlideIds = progress.CompletedSlideIds
                });
            }

            // 返回默认进度
            return Ok(new
            {
                userId,
                kpId,
                status = LearningStatus.Todo,
                masteryLevel = 0f,
                reviewCount = 0,
                lastReviewTime = (DateTime?)null,
                completedSlideIds = new List<string>()
            });
        }
    }

    /// <summary>
    /// 获取所有知识点进度概览
    /// </summary>
    [HttpGet("overview")]
    public IActionResult GetProgressOverview([FromQuery] string userId = "default")
    {
        if (_knowledgeSystem == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "请先激活书籍目录并构建知识体系" } });
        }

        lock (_lock)
        {
            var overview = _knowledgeSystem.KnowledgePoints.Select(kp =>
            {
                var key = $"{userId}_{kp.KpId}";
                var progress = _userProgress.TryGetValue(key, out var p) ? p : null;

                return new
                {
                    kpId = kp.KpId,
                    title = kp.Title,
                    importance = kp.Importance,
                    status = progress?.Status ?? LearningStatus.Todo,
                    masteryLevel = progress?.MasteryLevel ?? 0f,
                    reviewCount = progress?.ReviewCount ?? 0
                };
            }).ToList();

            var total = overview.Count;
            var mastered = overview.Count(p => p.status == LearningStatus.Mastered);
            var learning = overview.Count(p => p.status == LearningStatus.Learning);
            var todo = overview.Count(p => p.status == LearningStatus.Todo);

            return Ok(new
            {
                userId,
                total,
                mastered,
                learning,
                todo,
                averageMasteryLevel = overview.Average(p => p.masteryLevel),
                items = overview
            });
        }
    }

    /// <summary>
    /// 更新学习状态
    /// </summary>
    [HttpPut]
    public IActionResult UpdateProgress([FromBody] UpdateProgressRequest request, [FromQuery] string userId = "default")
    {
        if (_knowledgeSystem == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "请先激活书籍目录并构建知识体系" } });
        }

        if (string.IsNullOrEmpty(request.KpId))
        {
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = "kpId 不能为空" } });
        }

        var kp = _knowledgeSystem.KnowledgePoints.FirstOrDefault(p => p.KpId == request.KpId);
        if (kp == null)
        {
            return NotFound(new { error = new { code = "KP_NOT_FOUND", message = $"知识点不存在: {request.KpId}" } });
        }

        lock (_lock)
        {
            var key = $"{userId}_{request.KpId}";
            if (!_userProgress.TryGetValue(key, out var progress))
            {
                progress = new UserProgress
                {
                    UserId = userId,
                    KpId = request.KpId
                };
                _userProgress[key] = progress;
            }

            // 更新字段
            if (request.Status.HasValue)
            {
                progress.Status = request.Status.Value;
            }

            if (request.MasteryLevel.HasValue)
            {
                progress.MasteryLevel = Math.Clamp(request.MasteryLevel.Value, 0f, 1f);
            }

            if (request.CompletedSlideIds != null)
            {
                progress.CompletedSlideIds = request.CompletedSlideIds;
            }

            if (request.AddCompletedSlideId != null)
            {
                if (!progress.CompletedSlideIds.Contains(request.AddCompletedSlideId))
                {
                    progress.CompletedSlideIds.Add(request.AddCompletedSlideId);
                }
            }

            progress.LastReviewTime = DateTime.UtcNow;
            progress.ReviewCount++;

            // 根据掌握度自动更新状态
            if (progress.MasteryLevel >= 0.8f)
            {
                progress.Status = LearningStatus.Mastered;
            }
            else if (progress.MasteryLevel > 0f)
            {
                progress.Status = LearningStatus.Learning;
            }

            _logger.LogInformation("用户进度已更新: {UserId}_{KpId}, Status={Status}, Mastery={Mastery}",
                userId, request.KpId, progress.Status, progress.MasteryLevel);

            return Ok(new
            {
                userId = progress.UserId,
                kpId = progress.KpId,
                status = progress.Status,
                masteryLevel = progress.MasteryLevel,
                reviewCount = progress.ReviewCount,
                lastReviewTime = progress.LastReviewTime,
                completedSlideIds = progress.CompletedSlideIds
            });
        }
    }

    /// <summary>
    /// 获取错题本
    /// </summary>
    [HttpGet("mistakes")]
    public IActionResult GetMistakeBook([FromQuery] string userId = "default")
    {
        var mistakes = _exerciseService.GetMistakeBook(userId);

        if (_knowledgeSystem == null)
        {
            return Ok(new { userId, items = mistakes, total = mistakes.Count });
        }

        // 添加知识点标题信息
        var items = mistakes.Select(m =>
        {
            var kp = _knowledgeSystem.KnowledgePoints.FirstOrDefault(k => k.KpId == m.KpId);
            return new
            {
                recordId = m.RecordId,
                exerciseId = m.ExerciseId,
                kpId = m.KpId,
                kpTitle = kp?.Title ?? "未知",
                question = "", // 题目内容需要从缓存获取
                userAnswer = m.UserAnswer,
                correctAnswer = m.CorrectAnswer,
                errorAnalysis = m.ErrorAnalysis,
                createdAt = m.CreatedAt,
                errorCount = m.ErrorCount
            };
        }).ToList();

        return Ok(new { userId, items, total = items.Count });
    }

    /// <summary>
    /// 更新错题状态
    /// </summary>
    [HttpPut("mistakes/{recordId}")]
    public IActionResult UpdateMistake(string recordId, [FromBody] UpdateMistakeRequest request)
    {
        if (request.IsResolved.HasValue && request.IsResolved.Value)
        {
            var result = _exerciseService.ResolveMistake(recordId);
            if (!result)
            {
                return NotFound(new { error = new { code = "NOT_FOUND", message = "错题记录不存在" } });
            }
            _logger.LogInformation("错题已解决: {RecordId}", recordId);
            return Ok(new { recordId, isResolved = true });
        }

        return BadRequest(new { error = new { code = "BAD_REQUEST", message = "无效的请求" } });
    }

    /// <summary>
    /// 获取关联知识点
    /// </summary>
    [HttpGet("relations")]
    public IActionResult GetRelations([FromQuery] string kpId)
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

        var relations = kp.Relations.Select(r =>
        {
            var targetKp = _knowledgeSystem.KnowledgePoints.FirstOrDefault(k => k.KpId == r.ToKpId);
            return new
            {
                targetKpId = r.ToKpId,
                targetTitle = targetKp?.Title ?? "未知",
                relationType = r.Type,
                description = r.Description
            };
        }).ToList();

        return Ok(new { kpId, relations });
    }
}

/// <summary>
/// 更新进度请求
/// </summary>
public class UpdateProgressRequest
{
    /// <summary>
    /// 知识点 ID
    /// </summary>
    public string KpId { get; set; } = string.Empty;

    /// <summary>
    /// 学习状态
    /// </summary>
    public LearningStatus? Status { get; set; }

    /// <summary>
    /// 掌握度 (0.0 ~ 1.0)
    /// </summary>
    public float? MasteryLevel { get; set; }

    /// <summary>
    /// 已完成的幻灯片 ID 列表
    /// </summary>
    public List<string>? CompletedSlideIds { get; set; }

    /// <summary>
    /// 添加已完成的幻灯片 ID
    /// </summary>
    public string? AddCompletedSlideId { get; set; }
}

/// <summary>
/// 更新错题请求
/// </summary>
public class UpdateMistakeRequest
{
    /// <summary>
    /// 是否已解决
    /// </summary>
    public bool? IsResolved { get; set; }
}
