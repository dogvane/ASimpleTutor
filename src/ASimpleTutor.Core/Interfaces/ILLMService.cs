using ASimpleTutor.Core.Models;

namespace ASimpleTutor.Core.Interfaces;

/// <summary>
/// LLM 服务接口
/// </summary>
public interface ILLMService
{
    /// <summary>
    /// 发送聊天请求
    /// </summary>
    Task<string> ChatAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送聊天请求并期望 JSON 响应
    /// </summary>
    Task<T> ChatJsonAsync<T>(string systemPrompt, string userMessage, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 发送聊天请求并期望 JSON 响应（可配置温度）
    /// </summary>
    Task<T> ChatJsonAsync<T>(string systemPrompt, string userMessage, float temperature, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// 更新 LLM 配置并重新初始化客户端
    /// </summary>
    /// <param name="apiKey">API 密钥</param>
    /// <param name="baseUrl">API 基础 URL</param>
    /// <param name="model">模型名称</param>
    void UpdateConfig(string apiKey, string baseUrl, string model);
}
