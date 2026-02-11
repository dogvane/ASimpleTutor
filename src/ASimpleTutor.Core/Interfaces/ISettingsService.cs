using ASimpleTutor.Core.Models.Dto;

namespace ASimpleTutor.Core.Interfaces;

/// <summary>
/// 设置服务接口
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// 获取当前 LLM 配置（API Key 脱敏）
    /// </summary>
    /// <returns>LLM 配置响应</returns>
    Task<LlmSettingsResponse> GetLlmSettingsAsync();

    /// <summary>
    /// 更新 LLM 配置
    /// </summary>
    /// <param name="request">配置更新请求</param>
    /// <returns>更新后的 LLM 配置响应</returns>
    Task<LlmSettingsResponse> UpdateLlmSettingsAsync(LlmSettingsRequest request);

    /// <summary>
    /// 测试 LLM 连接
    /// </summary>
    /// <param name="request">测试连接请求</param>
    /// <returns>测试连接响应</returns>
    Task<TestLlmConnectionResponse> TestLlmConnectionAsync(TestLlmConnectionRequest request);

    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    /// <param name="apiKey">API 密钥</param>
    /// <param name="baseUrl">API 基础 URL</param>
    /// <param name="model">模型名称</param>
    /// <returns>配置是否有效</returns>
    Task<bool> ValidateLlmSettingsAsync(string apiKey, string baseUrl, string model);
}
