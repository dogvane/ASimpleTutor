using ASimpleTutor.Api.Configuration;
using ASimpleTutor.Api.Interfaces;
using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using ASimpleTutor.Core.Models.Dto;
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
    private readonly ITtsService _ttsService;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<KnowledgePointsController> _logger;

    public KnowledgePointsController(
        ILearningGenerator learningGenerator,
        ITtsService ttsService,
        ISettingsService settingsService,
        ILogger<KnowledgePointsController> logger)
    {
        _learningGenerator = learningGenerator;
        _ttsService = ttsService;
        _settingsService = settingsService;
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

        // 详细日志：记录每个 SlideCard 的 SpeechScript 状态
        _logger.LogInformation("[TTS] 加载知识点 SlideCards，KpId={KpId}, Count={Count}", kpId, slideCards.Count);
        foreach (var sc in slideCards)
        {
            var hasValue = !string.IsNullOrEmpty(sc.SpeechScript);
            var length = sc.SpeechScript?.Length ?? 0;
            _logger.LogDebug("[TTS] 加载时 SlideCard 状态，SlideId={SlideId}, HasSpeechScript={Has}, Length={Length}, IsNullOrEmpty={IsNullOrEmpty}",
                sc.SlideId, hasValue, length, string.IsNullOrEmpty(sc.SpeechScript));
        }

        // 检查是否有幻灯片卡片缺少 SpeechScript（使用 null 检查而不是 IsNullOrEmpty）
        var needsRegeneration = slideCards.Any(sc => sc.SpeechScript == null);
        _logger.LogDebug("[TTS] 检查结果，NeedsRegeneration={Needs}, TotalCount={Total}",
            needsRegeneration, slideCards.Count);
        if (needsRegeneration)
        {
            _logger.LogInformation("[TTS] 检测到幻灯片卡片缺少 SpeechScript，开始重新生成: {KpId}", kpId);
            try
            {
                var learningPack = await _learningGenerator.GenerateAsync(kp, cancellationToken);
                if (learningPack?.SlideCards != null && learningPack.SlideCards.Count > 0)
                {
                    // 更新知识点的 SlideCards
                    kp.SlideCards = learningPack.SlideCards;
                    slideCards = learningPack.SlideCards;

                    // 详细日志：记录生成后的状态
                    _logger.LogInformation("[TTS] SpeechScript 重新生成完成，KpId={KpId}, Count={Count}", kpId, learningPack.SlideCards.Count);
                    foreach (var sc in learningPack.SlideCards)
                    {
                        _logger.LogDebug("[TTS] 重新生成后状态，SlideId={SlideId}, HasSpeechScript={Has}, Length={Length}",
                            sc.SlideId,
                            !string.IsNullOrEmpty(sc.SpeechScript),
                            sc.SpeechScript?.Length ?? 0);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成 SpeechScript 失败: {KpId}", kpId);
                // 继续返回已有的 SlideCards，即使 SpeechScript 为空
            }
        }

        // 获取 TTS 语速配置
        var ttsSettings = await _settingsService.GetTtsSettingsAsync();
        var ttsSpeed = ttsSettings.Speed;

        // 为每个幻灯片卡片生成音频 URL
        var slideCardResponses = new List<object>();
        var generatedCount = 0;
        var cachedCount = 0;
        var errorCount = 0;
        var emptyScriptCount = 0;

        foreach (var sc in slideCards)
        {
            string? audioUrl = null;

            // 详细日志：显示每个卡片的 SpeechScript 状态
            var scriptValue = sc.SpeechScript;
            var isNull = scriptValue == null;
            var isEmpty = !isNull && string.IsNullOrEmpty(scriptValue);
            _logger.LogDebug("[TTS] 处理卡片，SlideId={SlideId}, SpeechScript.IsNull={IsNull}, IsEmpty={IsEmpty}, Length={Length}, Preview={Preview}",
                sc.SlideId, isNull, isEmpty, scriptValue?.Length ?? 0,
                scriptValue != null ? (scriptValue.Length > 100 ? scriptValue.Substring(0, 100) + "..." : scriptValue) : "null");

            if (isNull || isEmpty)
            {
                emptyScriptCount++;
                _logger.LogDebug("[TTS] SlideCard 没有 SpeechScript（空或null），跳过音频生成，SlideId={SlideId}", sc.SlideId);
            }
            else
            {
                try
                {
                    audioUrl = await _ttsService.GetAudioUrlAsync(sc.SpeechScript, cancellationToken);
                    if (!string.IsNullOrEmpty(audioUrl))
                    {
                        generatedCount++;
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(ex, "[TTS] 生成音频 URL 失败: {SlideId}", sc.SlideId);
                }
            }

            slideCardResponses.Add(new
            {
                slideId = sc.SlideId,
                type = sc.Type.ToString(),
                order = sc.Order,
                title = sc.Title,
                htmlContent = sc.HtmlContent,
                speechScript = sc.SpeechScript,
                audioUrl = audioUrl,
                speed = ttsSpeed,
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
            });
        }

        _logger.LogInformation("[TTS] 音频 URL 生成完成，总幻灯片数={Total}, 已生成={Generated}, 已缓存={Cached}, 无脚本={Empty}, 错误={Errors}",
            slideCards.Count, generatedCount, cachedCount, emptyScriptCount, errorCount);

        return Ok(new
        {
            id = kp.KpId,
            title = kp.Title,
            slideCards = slideCardResponses
        });
    }
}
