using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using Newtonsoft.Json;
using System.ClientModel;

namespace ASimpleTutor.Core.Services;

/// <summary>
/// 简单的 LLM 服务实现（OpenAI 兼容 API）
/// </summary>
public class LLMService : ILLMService
{
    private readonly ChatClient _client;
    private readonly string _model;
    private readonly ILogger<LLMService> _logger;

    public LLMService(string apiKey, string baseUrl, string model, ILogger<LLMService> logger)
    {
        _model = model;
        _logger = logger;

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

    public async Task<string> ChatAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("调用 LLM，模型: {Model}", _model);

            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(systemPrompt),
                ChatMessage.CreateUserMessage(userMessage)
            };

            var response = await _client.CompleteChatAsync(messages, cancellationToken: cancellationToken);

            var content = response.Value.Content[0].Text;
            _logger.LogDebug("LLM 响应长度: {Length}", content?.Length ?? 0);
            return content ?? string.Empty;
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

        var jsonPrompt = $@"{systemPrompt}

重要：你的响应必须是有效的 JSON 格式，不要包含任何其他文本或解释。";

        for (int retry = 0; retry <= maxRetries; retry++)
        {
            try
            {
                var response = await ChatAsync(jsonPrompt, userMessage, cancellationToken);

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

                // 记录原始响应（始终记录，用于调试）
                var displayResponse = cleanedResponse.Length > 1000 ? cleanedResponse.Substring(0, 1000) + "..." : cleanedResponse;
                _logger.LogInformation("LLM 原始响应: {Response}", displayResponse);

                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(cleanedResponse)
                           ?? throw new InvalidOperationException("JSON 解析结果为空");

                // 记录解析结果
                _logger.LogInformation("JSON 解析成功，类型: {Type}", typeof(T).Name);
                return result;
            }
            catch (Exception ex) when (retry < maxRetries)
            {
                lastException = ex;
                _logger.LogWarning(ex, "JSON 解析失败，第 {Retry} 次重试 (共 {MaxRetries} 次)", retry + 1, maxRetries);
            }
        }

        // 所有重试都失败，记录原始响应并抛出异常
        _logger.LogError(lastException, "JSON 解析重试失败，不再重试");

        // 降级：尝试从文本中提取 JSON
        var fallbackResponse = CreateFallbackResponse<T>();
        return fallbackResponse;
    }

    private static T CreateFallbackResponse<T>() where T : class
    {
        // 创建降级响应
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

        // 支持 KnowledgePointsResponse 类型（通过反射获取内部类型）
        if (typeof(T).Name == "KnowledgePointsResponse")
        {
            // 创建降级响应，使用动态类型检测
            var fallback = new
            {
                SchemaVersion = "1.0",
                KnowledgePoints = new object[0]
            };
            return fallback as T ?? throw new InvalidOperationException();
        }

        throw new NotSupportedException($"不支持的类型: {typeof(T).Name}");
    }
}
