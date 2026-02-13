using ASimpleTutor.Api.Configuration;
using ASimpleTutor.Api.Interfaces;
using ASimpleTutor.Core.Interfaces;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Audio;
using System.ClientModel;
using System.Security.Cryptography;
using System.Text;

namespace ASimpleTutor.Api.Services;

/// <summary>
/// TTS 服务实现（OpenAI 兼容 API）
/// </summary>
public class TtsService : ITtsService
{
    private string _apiKey;
    private string _baseUrl;
    private string _voice;
    private readonly ILogger<TtsService> _logger;
    private readonly string _audioDirectory;
    private readonly object _lock = new();
    ISettingsService _settingsService;

    public TtsService(AppConfig config, ILogger<TtsService> logger, IWebHostEnvironment env, ISettingsService settingsService)
    {
        _logger = logger;
        _apiKey = config.Tts.ApiKey;
        _baseUrl = config.Tts.BaseUrl;
        _voice = config.Tts.Voice;

        // 音频文件存放目录：wwwroot/audios
        _audioDirectory = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "audios");

        if (!Directory.Exists(_audioDirectory))
        {
            Directory.CreateDirectory(_audioDirectory);
        }

        _settingsService = settingsService;
    }

    /// <summary>
    /// 更新 TTS 配置并重新初始化客户端
    /// </summary>
    public void UpdateConfig(string apiKey, string baseUrl, string voice)
    {
        lock (_lock)
        {
            _apiKey = apiKey;
            _baseUrl = baseUrl;
            _voice = voice;
        }
    }

    /// <summary>
    /// 根据口语文本生成或获取音频文件 URL
    /// </summary>
    public async Task<string?> GetAudioUrlAsync(string speechScript, CancellationToken cancellationToken = default)
    {
        // 1. 检查输入是否有效
        if (string.IsNullOrWhiteSpace(speechScript))
        {
            _logger.LogWarning("[TTS] speechScript 为空，无法生成音频，KpId={KpId}", "N/A");
            return null;
        }

        // 2. 基于 speechScript 的 hash 值生成文件名
        var fileHash = ComputeSha256Hash(speechScript);
        var fileName = $"{fileHash}.wav";
        var filePath = Path.Combine(_audioDirectory, fileName);

        // 3. 检查文件是否已存在
        if (File.Exists(filePath))
        {
            return $"/audios/{fileName}";
        }

        // 4. 生成音频
        try
        {
            var setting = await _settingsService.GetTtsSettingsAsync();
            if (setting.Enabled)
            {
                await GenerateAudioAsync(speechScript, filePath, cancellationToken);
                return $"/audios/{fileName}";
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TTS] 音频生成失败，FileName={FileName}", fileName);
            return null;
        }
    }

    /// <summary>
    /// 调用 TTS API 生成音频
    /// </summary>
    private async Task GenerateAudioAsync(string text, string outputPath, CancellationToken cancellationToken)
    {
        try
        {
            var credential = new ApiKeyCredential(_apiKey);
            var options = new OpenAIClientOptions
            {
                Endpoint = new Uri(_baseUrl),
                NetworkTimeout = TimeSpan.FromMinutes(5)
            };
            var client = new OpenAIClient(credential, options);
            var audioClient = client.GetAudioClient(_voice);

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

            var audioOptions = new SpeechGenerationOptions
            {
                ResponseFormat = GeneratedSpeechFormat.Wav
            };

            var audioResult = await audioClient.GenerateSpeechAsync(text, _voice, audioOptions, linkedCts.Token);

            if (!Directory.Exists(_audioDirectory))
            {
                _logger.LogError("[TTS] 音频目录不存在，无法保存文件: {AudioDir}", _audioDirectory);
                throw new DirectoryNotFoundException($"音频目录不存在: {_audioDirectory}");
            }

            await using var fileStream = File.Create(outputPath);
            await audioResult.Value.ToStream().CopyToAsync(fileStream, linkedCts.Token);
            await fileStream.FlushAsync(linkedCts.Token);

            var fileInfo = new FileInfo(outputPath);
            if (!fileInfo.Exists || fileInfo.Length == 0)
            {
                throw new IOException($"音频文件保存验证失败: {outputPath}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TTS] TTS API 调用失败 - Voice={Voice}, BaseUrl={BaseUrl}, TextLength={TextLength}",
                _voice, _baseUrl, text?.Length ?? 0);
            throw;
        }
    }

    /// <summary>
    /// 计算字符串的 SHA256 Hash
    /// </summary>
    private string ComputeSha256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLower().Substring(0, 16); // 取前16个字符
    }
}
