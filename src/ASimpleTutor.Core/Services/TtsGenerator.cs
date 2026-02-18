using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using Microsoft.Extensions.Logging;

namespace ASimpleTutor.Core.Services;

/// <summary>
/// TTS 音频生成器
/// 负责为幻灯片卡片生成 TTS 音频
/// </summary>
public class TtsGenerator : ITtsGenerator
{
    private readonly ITtsService _ttsService;
    private readonly ILearningGenerator _learningGenerator;
    private readonly ScanProgressService _progressService;
    private readonly ILogger<TtsGenerator> _logger;

    public TtsGenerator(
        ITtsService ttsService,
        ILearningGenerator learningGenerator,
        ScanProgressService progressService,
        ILogger<TtsGenerator> logger)
    {
        _ttsService = ttsService;
        _learningGenerator = learningGenerator;
        _progressService = progressService;
        _logger = logger;
    }

    public async Task GenerateAsync(List<SlideCard> slideCards, string bookHubId, CancellationToken cancellationToken = default)
    {
        if (slideCards.Count == 0)
        {
            _logger.LogInformation("没有需要生成 TTS 的幻灯片卡片");
            return;
        }

        // 1. 检查并补全缺失的 SpeechScript
        var needsUpdateSpeechScript = slideCards.Any(sc => string.IsNullOrEmpty(sc.SpeechScript));
        if (needsUpdateSpeechScript)
        {
            _logger.LogInformation("检测到 {Count} 个幻灯片卡片缺少 SpeechScript，开始补全",
                slideCards.Count(sc => string.IsNullOrEmpty(sc.SpeechScript)));
            await _learningGenerator.UpdateSpeechScriptsAsync(slideCards, cancellationToken);
        }

        // 2. 过滤出有 SpeechScript 的卡片进行 TTS 生成
        var slideCardsWithScript = slideCards
            .Where(sc => !string.IsNullOrWhiteSpace(sc.SpeechScript))
            .ToList();

        if (slideCardsWithScript.Count == 0)
        {
            _logger.LogInformation("没有需要生成 TTS 的幻灯片卡片（无 SpeechScript）");
            return;
        }

        _logger.LogInformation("开始为 {Count} 个幻灯片卡片生成 TTS 音频", slideCardsWithScript.Count);

        var completedCount = 0;
        var failedCount = 0;
        var cachedCount = 0;
        var emptyCount = 0;
        var processedCount = 0;
        var totalCount = slideCardsWithScript.Count;

        await Parallel.ForEachAsync(slideCardsWithScript,
            new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = 3 },
            async (slideCard, cancellationToken) =>
            {
                try
                {
                    // 如果已经有 AudioUrl，检查音频文件是否存在
                    if (!string.IsNullOrEmpty(slideCard.AudioUrl))
                    {
                        var audioPath = slideCard.AudioUrl;
                        if (audioPath.StartsWith("/"))
                        {
                            audioPath = audioPath.Substring(1);
                        }

                        // 注意：这里无法直接访问 WebRootPath，跳过文件存在性检查
                        // 音频文件缺失的情况会在 API 层处理时重新生成
                        cachedCount++;
                    }
                    else
                    {
                        // 生成音频
                        var audioUrl = await _ttsService.GetAudioUrlAsync(slideCard.SpeechScript!, cancellationToken);
                        if (!string.IsNullOrEmpty(audioUrl))
                        {
                            slideCard.AudioUrl = audioUrl;
                            Interlocked.Increment(ref completedCount);
                            _logger.LogDebug("TTS 生成成功: {SlideId}", slideCard.SlideId);
                        }
                        else
                        {
                            Interlocked.Increment(ref failedCount);
                            _logger.LogWarning("TTS 生成返回空: {SlideId}", slideCard.SlideId);
                        }
                    }

                    // 更新进度
                    var current = Interlocked.Increment(ref processedCount);
                    if (current % 10 == 0 || current == totalCount) // 每10个更新一次进度
                    {
                        var percent = 80 + (current * 10 / totalCount); // 80-90% 区间
                        _progressService.UpdateProgress(bookHubId, "生成音频", percent, $"正在生成音频 ({current}/{totalCount})...", current, totalCount);
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref failedCount);
                    _logger.LogError(ex, "TTS 生成失败: {SlideId}", slideCard.SlideId);
                }
            });

        _logger.LogInformation("TTS 生成完成: 成功 {CompletedCount}, 缓存 {CachedCount}, 失败 {FailedCount}, 空脚本 {EmptyCount}",
            completedCount, cachedCount, failedCount, emptyCount);
    }
}