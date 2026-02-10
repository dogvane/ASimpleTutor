using ASimpleTutor.Api.Configuration;
using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using ASimpleTutor.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
    private readonly ILearningGenerator _learningGenerator;
    private readonly ILogger<KnowledgePointsController> _logger;

    public KnowledgePointsController(
        ILearningGenerator learningGenerator,
        ILogger<KnowledgePointsController> logger)
    {
        _learningGenerator = learningGenerator;
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
    public async Task<IActionResult> GetSourceContent(
        [FromQuery] string kpId,
        [FromServices] IServiceProvider serviceProvider,
        [FromServices] KnowledgeSystemStore store,
        CancellationToken cancellationToken)
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

        // 从 KnowledgeSystemStore 加载文档
        var loadResult = await store.LoadAsync(_knowledgeSystem.BookHubId, cancellationToken);
        if (loadResult.Documents == null || loadResult.Documents.Count == 0)
        {
            return Ok(new
            {
                id = kp.KpId,
                title = kp.Title,
                sourceItems = new List<object>()
            });
        }

        // 从 Document 中获取原文片段
        var sourceItems = new List<object>();
        foreach (var doc in loadResult.Documents)
        {
            if (doc.DocId != kp.DocId)
            {
                continue;
            }

            if (doc != null && System.IO.File.Exists(doc.Path))
            {
                try
                {
                    // 读取文档文件的所有行
                    var lines = await System.IO.File.ReadAllLinesAsync(doc.Path, cancellationToken);

                    // 根据章节路径查找对应的章节
                    var section = FindSectionByPath(doc.Sections, kp.ChapterPath);
                    if (section != null && section.StartLine >= 0 && section.EndLine <= lines.Length)
                    {
                        // 提取章节内容
                        var contentLines = lines.Skip(section.StartLine).Take(section.EndLine - section.StartLine);
                        var content = string.Join("\n", contentLines);

                        sourceItems.Add(new
                        {
                            filePath = doc.Path,
                            fileName = System.IO.Path.GetFileName(doc.Path),
                            headingPath = section.HeadingPath,
                            lineStart = section.StartLine,
                            lineEnd = section.EndLine,
                            content = content
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "读取原文片段失败: {KpId}", kp.KpId);
                }
            }
        }

        return Ok(new
        {
            id = kp.KpId,
            title = kp.Title,
            sourceItems
        });
    }

    /// <summary>
    /// 根据章节路径查找对应的章节
    /// </summary>
    private Section? FindSectionByPath(List<Section> sections, List<string> path)
    {
        if (sections == null || sections.Count == 0 || path == null || path.Count == 0)
        {
            return null;
        }

        foreach (var section in sections)
        {
            // 检查路径是否匹配（完全匹配或部分匹配）
            if (section.HeadingPath.Count > 0)
            {
                // 完全匹配
                if (section.HeadingPath.Count == path.Count)
                {
                    var isMatch = true;
                    for (int i = 0; i < path.Count; i++)
                    {
                        if (section.HeadingPath[i] != path[i])
                        {
                            isMatch = false;
                            break;
                        }
                    }
                    if (isMatch)
                    {
                        return section;
                    }
                }

                // 检查子章节
                if (section.SubSections != null && section.SubSections.Count > 0)
                {
                    var found = FindSectionByPath(section.SubSections, path);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }
            else if (section.SubSections != null && section.SubSections.Count > 0)
            {
                var found = FindSectionByPath(section.SubSections, path);
                if (found != null)
                {
                    return found;
                }
            }
        }

        return null;
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

    /// <summary>
    /// 获取幻灯片卡片
    /// </summary>
    [HttpGet("slide-cards")]
    public async Task<IActionResult> GetSlideCards([FromQuery] string kpId, CancellationToken cancellationToken)
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

        var slideCards = kp.SlideCards ?? new List<SlideCard>();

        // 检查是否有幻灯片卡片缺少 SpeechScript
        var needsRegeneration = slideCards.Any(sc => string.IsNullOrEmpty(sc.SpeechScript));
        if (needsRegeneration)
        {
            _logger.LogInformation("检测到幻灯片卡片缺少 SpeechScript，开始生成: {KpId}", kpId);
            try
            {
                var learningPack = await _learningGenerator.GenerateAsync(kp, cancellationToken);
                if (learningPack?.SlideCards != null && learningPack.SlideCards.Count > 0)
                {
                    // 更新知识点的 SlideCards
                    kp.SlideCards = learningPack.SlideCards;
                    slideCards = learningPack.SlideCards;
                    _logger.LogInformation("SpeechScript 生成完成: {KpId}", kpId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成 SpeechScript 失败: {KpId}", kpId);
                // 继续返回已有的 SlideCards，即使 SpeechScript 为空
            }
        }

        return Ok(new
        {
            id = kp.KpId,
            title = kp.Title,
            slideCards = slideCards.Select(sc => new
            {
                slideId = sc.SlideId,
                type = sc.Type.ToString(),
                order = sc.Order,
                title = sc.Title,
                htmlContent = sc.HtmlContent,
                speechScript = sc.SpeechScript,
                sourceReferences = sc.SourceReferences.Select(sr => new
                {
                    snippetId = sr.SnippetId,
                    filePath = sr.FilePath,
                    headingPath = sr.HeadingPath,
                    startLine = sr.StartLine,
                    endLine = sr.EndLine,
                    content = sr.Content
                }),
                config = sc.Config
            })
        });
    }
}
