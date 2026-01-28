using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using Newtonsoft.Json;

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

        // OpenAI SDK v2 使用 API 密钥直接构造
        _client = new ChatClient(model, apiKey);
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
        var jsonPrompt = $@"{systemPrompt}

重要：你的响应必须是有效的 JSON 格式，不要包含任何其他文本或解释。";

        var response = await ChatAsync(jsonPrompt, userMessage, cancellationToken);

        // 尝试解析 JSON
        try
        {
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

            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(cleanedResponse)
                   ?? throw new InvalidOperationException("JSON 解析结果为空");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "JSON 解析失败，尝试使用降级策略");

            // 降级：尝试从文本中提取 JSON
            var fallbackResponse = CreateFallbackResponse<T>();
            return fallbackResponse;
        }
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

        throw new NotSupportedException($"不支持的类型: {typeof(T).Name}");
    }
}
