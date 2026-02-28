using ASimpleTutor.Api.Configuration;
using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using ASimpleTutor.Core.Models.Dto;
using ASimpleTutor.Core.Services;
using Microsoft.AspNetCore.Hosting;
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

    private bool ValidateRequest(string kpId, out KnowledgePoint? kp, out IActionResult? errorResult)
    {
        kp = null;
        errorResult = null;

        if (_knowledgeSystem == null)
        {
            errorResult = NotFound(new { error = new { code = "NOT_FOUND", message = "请先激活书籍目录并构建知识体系" } });
            return false;
        }

        if (string.IsNullOrEmpty(kpId))
        {
            errorResult = BadRequest(new { error = new { code = "BAD_REQUEST", message = "kpId 不能为空" } });
            return false;
        }

        kp = _knowledgeSystem.KnowledgePoints.FirstOrDefault(p => p.KpId == kpId);
        if (kp == null)
        {
            errorResult = NotFound(new { error = new { code = "KP_NOT_FOUND", message = $"知识点不存在: {kpId}" } });
            return false;
        }

        return true;
    }

    private bool ValidateRequest(string kpId, out IActionResult? errorResult)
    {
        return ValidateRequest(kpId, out _, out errorResult);
    }

    /// <summary>
    /// 获取精要速览
    /// </summary>
    [HttpGet("overview")]
    public IActionResult GetOverview([FromQuery] string kpId)
    {
        if (!ValidateRequest(kpId, out var kp, out var errorResult)) return errorResult!;

        return Ok(new
        {
            id = kp!.KpId,
            title = kp.Title,
            overview = kp.Summary,
            generatedAt = DateTime.UtcNow
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
        if (!ValidateRequest(kpId, out var kp, out var errorResult)) return errorResult!;

        // 从 KnowledgeSystemStore 加载文档
        var loadResult = await store.LoadAsync(_knowledgeSystem!.BookHubId, cancellationToken);
        var documents = loadResult.Documents ?? new List<Document>();
        if (documents.Count == 0)
        {
            return Ok(new
            {
                id = kp!.KpId,
                title = kp.Title,
                sourceItems = new List<object>()
            });
        }

        // 从 Document 中获取原文片段
        var sourceItems = new List<object>();
        foreach (var doc in documents)
        {
            if (doc.DocId != kp!.DocId)
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
                    var section = doc.FindSectionById(kp!.SectionId);
                    if (section != null && section.StartLine >= 0 && section.EndLine <= lines.Length)
                    {
                        var content = string.Join("\n", lines.Skip(section.StartLine).Take(section.EndLine - section.StartLine));
                    sourceItems.Add(CreateSourceItem(doc, section, content));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "读取原文片段失败: {KpId}", kp.KpId);
                    // 忽略错误，继续处理其他文档
                }
            }
        }

        return Ok(new
        {
            id = kp!.KpId,
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
        if (!ValidateRequest(kpId, out var kp, out var errorResult)) return errorResult!;

        var levels = kp!.Levels ?? new List<ContentLevel>();
        var responseLevels = new Dictionary<string, object>();

        // 如果 level 为空，返回所有层次
        if (string.IsNullOrEmpty(level))
        {
            var briefLevel = levels.FirstOrDefault(l => l.Level == 1);
            var detailedLevel = levels.FirstOrDefault(l => l.Level == 2);
            var deepLevel = levels.FirstOrDefault(l => l.Level == 3);

            responseLevels["brief"] = new { content = briefLevel?.Content, keyPoints = kp.Summary?.KeyPoints };
            responseLevels["detailed"] = new { content = detailedLevel?.Content, examples = new List<string>() };
            responseLevels["deep"] = new { content = deepLevel?.Content, relatedPatterns = new List<string>(), bestPractices = new List<string>() };
        }
        // 只返回概览层次
        else if (level == "brief")
        {
            var briefLevel = levels.FirstOrDefault(l => l.Level == 1);
            responseLevels["brief"] = new { content = briefLevel?.Content, keyPoints = kp.Summary?.KeyPoints };
        }
        // 只返回详细层次
        else if (level == "detailed")
        {
            var detailedLevel = levels.FirstOrDefault(l => l.Level == 2);
            responseLevels["detailed"] = new { content = detailedLevel?.Content, examples = new List<string>() };
        }
        // 只返回深度层次
        else if (level == "deep")
        {
            var deepLevel = levels.FirstOrDefault(l => l.Level == 3);
            responseLevels["deep"] = new { content = deepLevel?.Content, relatedPatterns = new List<string>(), bestPractices = new List<string>() };
        }
        // 无效的 level 参数
        else
        {
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = $"无效的 level 参数: {level}。有效值为: brief, detailed, deep" } });
        }

        return Ok(new { id = kp.KpId, title = kp.Title, levels = responseLevels });
    }

    /// <summary>
    /// 获取幻灯片卡片
    /// </summary>
    [HttpGet("slide-cards")]
    public async Task<IActionResult> GetSlideCards(
        [FromQuery] string kpId,
        [FromServices] IWebHostEnvironment env,
        [FromServices] KnowledgeSystemStore store,
        CancellationToken cancellationToken)
    {
        if (!ValidateRequest(kpId, out var kp, out var errorResult)) return errorResult!;

        var slideCards = kp!.SlideCards ?? new List<SlideCard>();
        var hasChanges = false;

        var needsUpdateSpeechScript = slideCards.Any(sc => sc.SpeechScript == null);
        if (needsUpdateSpeechScript)
        {
            await _learningGenerator.UpdateSpeechScriptsAsync(slideCards, cancellationToken);
            hasChanges = true;
        }

        var ttsSettings = await _settingsService.GetTtsSettingsAsync();
        var ttsSpeed = ttsSettings.Speed;

        var slideCardResponses = new List<object>();
        var stats = new { Generated = 0, Cached = 0, Error = 0, Empty = 0 };

        foreach (var sc in slideCards)
        {
            var audioUrl = await ProcessSlideCardAudio(sc, env, stats, cancellationToken);
            if (!string.IsNullOrEmpty(audioUrl) && string.IsNullOrEmpty(sc.AudioUrl))
            {
                hasChanges = true;
            }
            slideCardResponses.Add(CreateSlideCardResponse(sc, audioUrl, ttsSpeed));
        }

        _logger.LogInformation("[TTS] 音频 URL 生成完成，总幻灯片数={Total}, 已生成={Generated}, 已缓存={Cached}, 无脚本={Empty}, 错误={Errors}",
            slideCards.Count, stats.Generated, stats.Cached, stats.Empty, stats.Error);

        if (hasChanges && _knowledgeSystem != null)
        {
            await store.SaveAsync(_knowledgeSystem, cancellationToken: cancellationToken);
            _logger.LogInformation("幻灯片卡片数据已保存: {KpId}", kpId);
        }

        return Ok(new { id = kp.KpId, title = kp.Title, slideCards = slideCardResponses });
     }

     private async Task<string?> ProcessSlideCardAudio(SlideCard sc, IWebHostEnvironment env, dynamic stats, CancellationToken cancellationToken)
     {
         string? audioUrl = sc.AudioUrl;

         if (!string.IsNullOrEmpty(audioUrl))
          {
              var filePath = Path.Combine(env.WebRootPath, audioUrl.TrimStart('/', '\\'));
              if (!System.IO.File.Exists(filePath))
              {
                  audioUrl = string.Empty;
                  sc.AudioUrl = string.Empty;
              }
          }

         var scriptValue = sc.SpeechScript;
         var isNull = scriptValue == null;
         var isEmpty = !isNull && string.IsNullOrEmpty(scriptValue);

         if (isNull || isEmpty)
         {
             stats.Empty++;
             _logger.LogDebug("[TTS] SlideCard 没有 SpeechScript（空或null），跳过音频生成，SlideId={SlideId}", sc.SlideId);
             return audioUrl;
         }

         try
         {
             if (string.IsNullOrEmpty(audioUrl) && sc.SpeechScript != null)
             {
                 audioUrl = await _ttsService.GetAudioUrlAsync(sc.SpeechScript, cancellationToken);
                 if (!string.IsNullOrEmpty(audioUrl))
                 {
                     stats.Generated++;
                     sc.AudioUrl = audioUrl;
                 }
             }
             else
             {
                 stats.Cached++;
             }
         }
         catch (Exception ex)
         {
             stats.Error++;
             _logger.LogError(ex, "[TTS] 生成音频 URL 失败: {SlideId}", sc.SlideId);
         }

         return audioUrl;
     }

     private object CreateSlideCardResponse(SlideCard sc, string? audioUrl, float ttsSpeed)
     {
         return new
         {
             slideId = sc.SlideId,
             type = sc.Type.ToString(),
             order = sc.Order,
             title = sc.Title,
             htmlContent = sc.HtmlContent,
             speechScript = sc.SpeechScript,
             audioUrl = audioUrl,
             speed = ttsSpeed
         };
     }

     private object CreateSourceItem(Document doc, Section section, string content)
     {
         return new
         {
             filePath = doc.Path,
             fileName = System.IO.Path.GetFileName(doc.Path),
             headingPath = section.HeadingPath,
             lineStart = section.StartLine,
             lineEnd = section.EndLine,
             content = content
         };
     }
}
