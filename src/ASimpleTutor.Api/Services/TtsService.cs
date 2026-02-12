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

        // 创建音频目录
        if (!Directory.Exists(_audioDirectory))
        {
            Directory.CreateDirectory(_audioDirectory);
            _logger.LogInformation("创建音频目录: {AudioDir}", _audioDirectory);
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
            _logger.LogInformation("更新 TTS 配置: ApiKey=***, BaseUrl={BaseUrl}, Voice={Voice}", baseUrl, voice);
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
            _logger.LogInformation("[TTS] 音频文件已存在，直接返回 URL，FileName={FileName}", fileName);
            return $"/audios/{fileName}";
        }

        // 4. 生成音频
        try
        {
            var setting = await _settingsService.GetTtsSettingsAsync();
            if (setting.Enabled)
            {
                await GenerateAudioAsync(speechScript, filePath, cancellationToken);
                _logger.LogInformation("[TTS] 音频生成成功，FileName={FileName}, FilePath={FilePath}", fileName, filePath);
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
        _logger.LogDebug("[TTS] 开始调用 TTS API，TextLength={TextLength}, OutputPath={OutputPath}", text.Length, outputPath);

        try
        {
            // 使用 OpenAIClient 创建 AudioClient
            var credential = new ApiKeyCredential(_apiKey);
            var options = new OpenAIClientOptions 
            { 
                Endpoint = new Uri(_baseUrl),
                NetworkTimeout = TimeSpan.FromMinutes(5) // 设置网络请求超时为5分钟
            };
            var client = new OpenAIClient(credential, options);
            // GetAudioClient 需要 model 参数，这里使用 voice 名称
            var audioClient = client.GetAudioClient(_voice);

            _logger.LogDebug("[TTS] AudioClient 创建成功，Voice={Voice}, Timeout={Timeout}", _voice, options.NetworkTimeout);

            // 使用异步生成（300秒/5分钟超时，与 NetworkTimeout 保持一致）
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

            // 使用重载：GenerateSpeechAsync(text, voice, options, cancellationToken)
            var audioOptions = new SpeechGenerationOptions
            {
                ResponseFormat = GeneratedSpeechFormat.Wav
            };

            _logger.LogDebug("[TTS] 调用 GenerateSpeechAsync，Text={Text}", text.Substring(0, Math.Min(50, text.Length)) + "...");
            var audioResult = await audioClient.GenerateSpeechAsync(text, _voice, audioOptions, linkedCts.Token);

            _logger.LogDebug("[TTS] 音频生成完成，ContentLength={ContentLength}", audioResult.Value.ToStream().Length);

            // 保存到文件 - BinaryData 使用 ToStream 方法
            _logger.LogDebug("[TTS] 开始保存音频文件，OutputPath={OutputPath}, DirectoryExists={DirExists}",
                outputPath, Directory.Exists(_audioDirectory));

            // 验证目录是否可写
            if (!Directory.Exists(_audioDirectory))
            {
                _logger.LogError("[TTS] 音频目录不存在，无法保存文件: {AudioDir}", _audioDirectory);
                throw new DirectoryNotFoundException($"音频目录不存在: {_audioDirectory}");
            }

            await using var fileStream = File.Create(outputPath);
            _logger.LogDebug("[TTS] 文件流已创建，Path={Path}", outputPath);

            await audioResult.Value.ToStream().CopyToAsync(fileStream, linkedCts.Token);
            _logger.LogDebug("[TTS] 流复制完成，CopyToAsync返回");

            // 验证文件是否真的被保存
            await fileStream.FlushAsync(linkedCts.Token);
            var fileExistsAfterSave = File.Exists(outputPath);
            var fileSizeAfterSave = fileExistsAfterSave ? new FileInfo(outputPath).Length : 0;

            _logger.LogInformation("[TTS] 音频文件已保存，FileName={Path.GetFileName(outputPath)}, Size={Size} bytes, FileExists={FileExists}",
                Path.GetFileName(outputPath), fileSizeAfterSave, fileExistsAfterSave);

            if (!fileExistsAfterSave || fileSizeAfterSave == 0)
            {
                _logger.LogError("[TTS] 严重错误：音频文件保存后验证失败，FileName={FileName}, Path={Path}, Exists={Exists}, Size={Size}",
                    Path.GetFileName(outputPath), outputPath, fileExistsAfterSave, fileSizeAfterSave);
                throw new IOException($"音频文件保存验证失败: {outputPath}");
            }
        }
        catch (Exception ex)
        {
            // 详细错误日志
            _logger.LogError(ex, "[TTS] TTS API 调用失败");
            _logger.LogError("[TTS] 失败详情 - Voice={Voice}, BaseUrl={BaseUrl}, TextLength={TextLength}, OutputPath={OutputPath}",
                _voice, _baseUrl, text?.Length ?? 0, outputPath);
            _logger.LogError("[TTS] 异常类型={ExceptionType}, 消息={Message}",
                ex.GetType().Name, ex.Message);

            // 如果是内部异常，也记录
            if (ex.InnerException != null)
            {
                _logger.LogError("[TTS] 内部异常 - 类型={InnerType}, 消息={InnerMessage}",
                    ex.InnerException.GetType().Name, ex.InnerException.Message);
            }

            // 记录完整的堆栈跟踪（仅在 Debug 级别）
            _logger.LogDebug("[TTS] 堆栈跟踪={StackTrace}", ex.StackTrace);
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
