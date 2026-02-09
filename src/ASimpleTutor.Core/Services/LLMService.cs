using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using ASimpleTutor.Core.Models.Dto;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using Newtonsoft.Json;
using System.ClientModel;
using System.Security.Cryptography;
using System.Text;

namespace ASimpleTutor.Core.Services;

/// <summary>
/// 简单的 LLM 服务实现（OpenAI 兼容 API）
/// </summary>
public class LLMService : ILLMService
{
    private readonly ChatClient _client;
    private readonly string _model;
    private readonly ILogger<LLMService> _logger;
    private readonly string _cacheDirectory;

    private readonly bool _isOllama;

    public LLMService(string apiKey, string baseUrl, string model, ILogger<LLMService> logger)
    {
        _model = model;
        _logger = logger;
        _cacheDirectory = Path.Combine(AppContext.BaseDirectory, "llm_cache");

        // 创建缓存目录
        if (!Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
            _logger.LogInformation("创建 LLM 缓存目录: {CacheDir}", _cacheDirectory);
        }

        // 判断是否是 Ollama 模型（根据 model 名称）
        _isOllama = "ollama".Equals(model, StringComparison.OrdinalIgnoreCase) || 
                    "ollama".Equals(apiKey, StringComparison.OrdinalIgnoreCase);

        // 支持自定义 baseUrl（如 Ollama）
        if (!string.IsNullOrEmpty(baseUrl) && !baseUrl.StartsWith("https://api.openai.com/v1"))
        {
            var credential = new ApiKeyCredential(apiKey);
            var options = new OpenAIClientOptions { Endpoint = new Uri(baseUrl) };
            var openAIClient = new OpenAIClient(credential, options);
            _client = openAIClient.GetChatClient(model);
            _logger.LogInformation("使用自定义 LLM 端点: {BaseUrl}, 模型: {Model}", baseUrl, model);
        }
        else
        {
            // 默认使用 OpenAI 官方 API
            _client = new ChatClient(model, apiKey);
            _logger.LogInformation("使用 OpenAI 官方 API，模型: {Model}", model);
        }
    }

    /// <summary>
    /// 生成缓存键
    /// </summary>
    private string GenerateCacheKey(string systemPrompt, string userMessage, float? temperature)
    {
        var key = $"{systemPrompt}|{userMessage}|{temperature ?? 0.7f}";
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(bytes).ToLower();
    }

    /// <summary>
    /// 从缓存中读取响应
    /// </summary>
    private async Task<string?> ReadFromCacheAsync(string cacheKey)
    {
        var filePath = Path.Combine(_cacheDirectory, $"{cacheKey}.json");
        if (File.Exists(filePath))
        {
            try
            {
                var content = await File.ReadAllTextAsync(filePath);
                var cacheEntry = JsonConvert.DeserializeObject<CacheEntry>(content);
                if (cacheEntry != null)
                {
                    _logger.LogInformation("从缓存中读取 LLM 响应");
                    return cacheEntry.Response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "读取缓存失败: {FilePath}", filePath);
            }
        }
        return null;
    }

    /// <summary>
    /// 将响应写入缓存
    /// </summary>
    private async Task WriteToCacheAsync(string cacheKey, string response)
    {
        var filePath = Path.Combine(_cacheDirectory, $"{cacheKey}.json");
        try
        {
            var cacheEntry = new CacheEntry
            {
                Response = response,
                Model = _model,
                Timestamp = DateTime.UtcNow
            };
            var content = JsonConvert.SerializeObject(cacheEntry, Formatting.Indented);
            await File.WriteAllTextAsync(filePath, content);
            _logger.LogInformation("将 LLM 响应写入缓存");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "写入缓存失败: {FilePath}", filePath);
        }
    }

    /// <summary>
    /// 缓存条目
    /// </summary>
    private class CacheEntry
    {
        public string Response { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public async Task<string> ChatAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
    {
        return await ChatWithOptionsAsync(systemPrompt, userMessage, temperature: null, cancellationToken);
    }

    public async Task<string> ChatWithOptionsAsync(
        string systemPrompt,
        string userMessage,
        float? temperature,
        CancellationToken cancellationToken = default)
    {
        // 生成缓存键
        var cacheKey = GenerateCacheKey(systemPrompt, userMessage, temperature);

        // 尝试从缓存读取
        var cachedResponse = await ReadFromCacheAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedResponse))
        {
            return cachedResponse;
        }

        try
        {
            _logger.LogDebug("调用 LLM，模型: {Model}, 温度: {Temp}", _model, temperature ?? 0.7f);

            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(systemPrompt),
                ChatMessage.CreateUserMessage(userMessage)
            };

            var options = new ChatCompletionOptions();

            if (temperature.HasValue)
            {
                options.Temperature = temperature.Value;
            }

            // 处理超时设置
            using var cts = new CancellationTokenSource();
            if (!_isOllama)
            {
                // 非 Ollama 模式，设置 30 秒超时
                cts.CancelAfter(TimeSpan.FromSeconds(300));
                _logger.LogDebug("非 Ollama 模式，设置 300秒(5分钟) 超时");
            }
            else
            {
                // Ollama 模式，设置 600 秒超时
                cts.CancelAfter(TimeSpan.FromSeconds(600));
                _logger.LogDebug("Ollama 模式，设置 600秒(10分钟)超时");
            }
            
            // 组合取消令牌
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

            var response = await _client.CompleteChatAsync(messages, options, linkedCts.Token);

            var content = response.Value.Content[0].Text ?? string.Empty;
            _logger.LogDebug("LLM 响应长度: {Length}", content.Length);
            _logger.LogDebug("LLM 响应内容（前500字符）: {Content}", 
                content.Length > 500 ? content.Substring(0, 500) + "..." : content);

            // 写入缓存
            await WriteToCacheAsync(cacheKey, content);

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM 调用失败");
            throw;
        }
    }

    public async Task<T> ChatJsonAsync<T>(string systemPrompt, string userMessage, CancellationToken cancellationToken = default) where T : class
    {
        const int maxRetries = 2;
        Exception? lastException = null;

        // 重试时使用的温度配置：每次重试降低温度，提高确定性
        var retryTemperatures = new float?[] { null, 0.5f, 0.3f };

        var jsonPrompt = $@"{systemPrompt}

 重要：你的响应必须是有效的 JSON 格式，不要包含任何其他文本或解释。";

        for (int retry = 0; retry <= maxRetries; retry++)
        {
            string? response = null;
            try
            {
                float? temperature = retryTemperatures[retry];
                response = await ChatWithOptionsAsync(jsonPrompt, userMessage, temperature, cancellationToken);

                // 清理可能的 markdown 代码块标记
                var cleanedResponse = response.Trim();
                if (cleanedResponse.StartsWith("```json"))
                {
                    cleanedResponse = cleanedResponse.Substring(7);
                }
                if (cleanedResponse.StartsWith("```"))
                {
                    cleanedResponse = cleanedResponse.Substring(3);
                }
                if (cleanedResponse.EndsWith("```"))
                {
                    cleanedResponse = cleanedResponse.Substring(0, cleanedResponse.Length - 3);
                }

                cleanedResponse = cleanedResponse.Trim();

                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(cleanedResponse)
                           ?? throw new InvalidOperationException("JSON 解析结果为空");

                _logger.LogInformation("JSON 解析成功，类型: {Type}", typeof(T).Name);
                return result;
            }
            catch (Exception ex) when (retry < maxRetries)
            {
                lastException = ex;
                float newTemp = retryTemperatures[retry + 1] ?? 0.5f;
                _logger.LogWarning(ex, "JSON 解析失败，第 {Retry} 次重试 (共 {MaxRetries} 次)，下次重试将使用温度: {Temp}",
                    retry + 1, maxRetries, newTemp);
                _logger.LogWarning("解析类型: {Type}", typeof(T).Name);
                _logger.LogWarning("提示词: {Prompt}", jsonPrompt);
                _logger.LogWarning("用户消息: {UserMessage}", userMessage);
                _logger.LogWarning("原始响应: {Response}", response);
            }
        }

        _logger.LogError(lastException, "JSON 解析重试失败，不再重试");

        var fallbackResponse = CreateFallbackResponse<T>();
        return fallbackResponse;
    }

    public async Task<T> ChatJsonAsync<T>(string systemPrompt, string userMessage, float temperature, CancellationToken cancellationToken = default) where T : class
    {
        const int maxRetries = 1;
        Exception? lastException = null;

        var jsonPrompt = $@"{systemPrompt}

 重要：你的响应必须是有效的 JSON 格式，不要包含任何其他文本或解释。";

        for (int retry = 0; retry <= maxRetries; retry++)
        {
            string? response = null;
            try
            {
                response = await ChatWithOptionsAsync(jsonPrompt, userMessage, temperature, cancellationToken);

                var cleanedResponse = response.Trim();
                if (cleanedResponse.StartsWith("```json"))
                {
                    cleanedResponse = cleanedResponse.Substring(7);
                }
                if (cleanedResponse.StartsWith("```"))
                {
                    cleanedResponse = cleanedResponse.Substring(3);
                }
                if (cleanedResponse.EndsWith("```"))
                {
                    cleanedResponse = cleanedResponse.Substring(0, cleanedResponse.Length - 3);
                }

                cleanedResponse = cleanedResponse.Trim();

                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(cleanedResponse)
                           ?? throw new InvalidOperationException("JSON 解析结果为空");

                _logger.LogInformation("JSON 解析成功，类型: {Type}", typeof(T).Name);
                return result;
            }
            catch (Exception ex) when (retry < maxRetries)
            {
                lastException = ex;
                _logger.LogWarning(ex, "JSON 解析失败，第 {Retry} 次重试 (共 {MaxRetries} 次)",
                    retry + 1, maxRetries);
                _logger.LogWarning("解析类型: {Type}", typeof(T).Name);
                _logger.LogWarning("提示词: {Prompt}", jsonPrompt);
                _logger.LogWarning("用户消息: {UserMessage}", userMessage);
                _logger.LogWarning("原始响应: {Response}", response);
            }
        }

        _logger.LogError(lastException, "JSON 解析重试失败，不再重试");

        var fallbackResponse = CreateFallbackResponse<T>();
        return fallbackResponse;
    }

    private static T CreateFallbackResponse<T>() where T : class
    {
        if (typeof(T) == typeof(LearningPack))
        {
            return new LearningPack
            {
                Summary = new Summary
                {
                    Definition = "无法生成精要速览",
                    KeyPoints = new List<string> { "内容生成失败" },
                    Pitfalls = new List<string>()
                },
                Levels = new List<ContentLevel>
                {
                    new ContentLevel { Level = 1, Title = "概览", Content = "无法生成层次化内容" }
                }
            } as T ?? throw new InvalidOperationException();
        }

        if (typeof(T) == typeof(List<KnowledgePoint>))
        {
            return new List<KnowledgePoint>() as T ?? throw new InvalidOperationException();
        }

        if (typeof(T) == typeof(List<Exercise>))
        {
            return new List<Exercise>() as T ?? throw new InvalidOperationException();
        }

        if (typeof(T).Name == "KnowledgePointsResponse")
        {
            var fallback = new KnowledgePointsResponse
            {
                SchemaVersion = "1.0",
                KnowledgePoints = new List<KnowledgePointDto>()
            };
            return fallback as T ?? throw new InvalidOperationException();
        }

        throw new NotSupportedException($"不支持的类型: {typeof(T).Name}");
    }
}
