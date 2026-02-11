using ASimpleTutor.Api.Configuration;
using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models.Dto;
using ASimpleTutor.Core.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

namespace ASimpleTutor.Api.Services;

/// <summary>
/// 设置服务实现
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly AppConfig _config;
    private readonly ILogger<SettingsService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILLMService _llmService;
    private readonly string _userConfigPath;

    public SettingsService(
        AppConfig config,
        ILogger<SettingsService> logger,
        ILoggerFactory loggerFactory,
        ILLMService llmService,
        IWebHostEnvironment env)
    {
        _config = config;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _llmService = llmService;
        _userConfigPath = Path.Combine(env.ContentRootPath, "appsettings.user.json");
    }

    /// <summary>
    /// 获取当前 LLM 配置（API Key 脱敏）
    /// </summary>
    public Task<LlmSettingsResponse> GetLlmSettingsAsync()
    {
        return Task.FromResult(new LlmSettingsResponse
        {
            ApiKeyMasked = MaskApiKey(_config.Llm.ApiKey),
            BaseUrl = _config.Llm.BaseUrl,
            Model = _config.Llm.Model,
            IsValid = !string.IsNullOrEmpty(_config.Llm.ApiKey),
            LastTested = null
        });
    }

    /// <summary>
    /// 更新 LLM 配置
    /// </summary>
    public async Task<LlmSettingsResponse> UpdateLlmSettingsAsync(LlmSettingsRequest request)
    {
        // 1. 验证输入
        ValidateRequest(request);

        // 2. 更新内存配置
        _config.Llm.ApiKey = request.ApiKey;
        _config.Llm.BaseUrl = request.BaseUrl;
        _config.Llm.Model = request.Model;

        // 3. 实时更新 LLM 服务的配置
        _llmService.UpdateConfig(request.ApiKey, request.BaseUrl, request.Model);

        // 4. 持久化到 appsettings.user.json
        await SaveUserConfigAsync();

        _logger.LogInformation("LLM 配置已更新并实时生效: {BaseUrl}, {Model}",
            request.BaseUrl, request.Model);

        return await GetLlmSettingsAsync();
    }

    /// <summary>
    /// 测试 LLM 连接
    /// </summary>
    public async Task<TestLlmConnectionResponse> TestLlmConnectionAsync(TestLlmConnectionRequest request)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 创建临时 LLM 服务实例测试连接
            var llmLogger = _loggerFactory.CreateLogger<LLMService>();
            var tempLlmService = new LLMService(
                request.ApiKey,
                request.BaseUrl,
                request.Model,
                llmLogger);

            // 发送简单测试请求
            const string systemPrompt = "你是一个AI助手。";
            const string testMessage = "你好";

            var response = await tempLlmService.ChatAsync(systemPrompt, testMessage);

            stopwatch.Stop();

            return new TestLlmConnectionResponse
            {
                Success = true,
                Message = "连接成功",
                ModelInfo = response?.Length > 0 ? response.Substring(0, Math.Min(100, response.Length)) : null,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "LLM 连接测试失败");

            return new TestLlmConnectionResponse
            {
                Success = false,
                Message = $"连接失败: {ex.Message}",
                ResponseTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    public async Task<bool> ValidateLlmSettingsAsync(string apiKey, string baseUrl, string model)
    {
        try
        {
            var result = await TestLlmConnectionAsync(new TestLlmConnectionRequest
            {
                ApiKey = apiKey,
                BaseUrl = baseUrl,
                Model = model
            });
            return result.Success;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 验证请求参数
    /// </summary>
    private void ValidateRequest(LlmSettingsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ApiKey))
            throw new ArgumentException("API Key 不能为空", nameof(request.ApiKey));

        if (string.IsNullOrWhiteSpace(request.BaseUrl))
            throw new ArgumentException("Base URL 不能为空", nameof(request.BaseUrl));

        if (!Uri.TryCreate(request.BaseUrl, UriKind.Absolute, out _))
            throw new ArgumentException("Base URL 格式无效", nameof(request.BaseUrl));

        if (string.IsNullOrWhiteSpace(request.Model))
            throw new ArgumentException("Model 不能为空", nameof(request.Model));
    }

    /// <summary>
    /// 验证 TTS 请求参数
    /// </summary>
    private void ValidateRequest(TtsSettingsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ApiKey))
            throw new ArgumentException("API Key 不能为空", nameof(request.ApiKey));

        if (string.IsNullOrWhiteSpace(request.BaseUrl))
            throw new ArgumentException("Base URL 不能为空", nameof(request.BaseUrl));

        if (!Uri.TryCreate(request.BaseUrl, UriKind.Absolute, out _))
            throw new ArgumentException("Base URL 格式无效", nameof(request.BaseUrl));

        if (string.IsNullOrWhiteSpace(request.Voice))
            throw new ArgumentException("Voice 不能为空", nameof(request.Voice));
    }

    /// <summary>
    /// 脱敏 API Key
    /// </summary>
    private string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey) || apiKey.Length < 10)
            return "***";

        return $"{apiKey.Substring(0, 7)}...{apiKey.Substring(Math.Max(0, apiKey.Length - 4))}";
    }

    /// <summary>
    /// 保存用户配置到 appsettings.user.json
    /// </summary>
    private async Task SaveUserConfigAsync()
    {
        var userConfig = new
        {
            App = new
            {
                Llm = new
                {
                    ApiKey = _config.Llm.ApiKey,
                    BaseUrl = _config.Llm.BaseUrl,
                    Model = _config.Llm.Model
                },
                Tts = new
                {
                    ApiKey = _config.Tts.ApiKey,
                    BaseUrl = _config.Tts.BaseUrl,
                    Voice = _config.Tts.Voice,
                    Speed = _config.Tts.Speed
                }
            }
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(userConfig, options);
        await File.WriteAllTextAsync(_userConfigPath, json);
        _logger.LogInformation("用户配置已保存到: {Path}", _userConfigPath);
    }

    /// <summary>
    /// 获取当前 TTS 配置（API Key 脱敏）
    /// </summary>
    public Task<TtsSettingsResponse> GetTtsSettingsAsync()
    {
        return Task.FromResult(new TtsSettingsResponse
        {
            ApiKeyMasked = MaskApiKey(_config.Tts.ApiKey),
            BaseUrl = _config.Tts.BaseUrl,
            Voice = _config.Tts.Voice,
            Speed = _config.Tts.Speed,
            IsValid = !string.IsNullOrEmpty(_config.Tts.ApiKey)
        });
    }

    /// <summary>
    /// 更新 TTS 配置
    /// </summary>
    public async Task<TtsSettingsResponse> UpdateTtsSettingsAsync(TtsSettingsRequest request)
    {
        // 1. 验证输入
        ValidateRequest(request);

        // 2. 更新内存配置
        _config.Tts.ApiKey = request.ApiKey;
        _config.Tts.BaseUrl = request.BaseUrl;
        _config.Tts.Voice = request.Voice;
        _config.Tts.Speed = request.Speed;

        // 3. 持久化到 appsettings.user.json
        await SaveUserConfigAsync();

        _logger.LogInformation("TTS 配置已更新并实时生效: {BaseUrl}, {Voice}, {Speed}",
            request.BaseUrl, request.Voice, request.Speed);

        return await GetTtsSettingsAsync();
    }

    /// <summary>
    /// 验证 TTS 配置是否有效
    /// </summary>
    public async Task<bool> ValidateTtsSettingsAsync(string apiKey, string baseUrl, string voice)
    {
        try
        {
            // 简单验证配置参数是否有效
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(voice))
                return false;

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out _))
                return false;

            return await Task.FromResult(true);
        }
        catch
        {
            return false;
        }
    }
}
