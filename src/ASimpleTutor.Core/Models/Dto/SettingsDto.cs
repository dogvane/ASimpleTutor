namespace ASimpleTutor.Core.Models.Dto;

/// <summary>
/// LLM 配置更新请求
/// </summary>
public class LlmSettingsRequest
{
    /// <summary>
    /// API 密钥
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// API 基础 URL
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// 模型名称
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// 并发数，默认为 1
    /// </summary>
    public int Concurrency { get; set; } = 1;
}

/// <summary>
/// LLM 配置响应（API Key 脱敏）
/// </summary>
public class LlmSettingsResponse
{
    /// <summary>
    /// 脱敏后的 API 密钥（sk-***...xyz 格式）
    /// </summary>
    public string ApiKeyMasked { get; set; } = string.Empty;

    /// <summary>
    /// API 基础 URL
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// 模型名称
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// 配置是否有效（非空判断）
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 最后测试时间（ISO 8601 格式）
    /// </summary>
    public string? LastTested { get; set; }

    /// <summary>
    /// 模型信息（响应片段）
    /// </summary>
    public string? ModelInfo { get; set; }

    /// <summary>
    /// 响应时间（毫秒）
    /// </summary>
    public long ResponseTimeMs { get; set; }

    /// <summary>
    /// 并发数
    /// </summary>
    public int Concurrency { get; set; }
}

/// <summary>
/// LLM 连接测试请求
/// </summary>
public class TestLlmConnectionRequest
{
    /// <summary>
    /// API 密钥
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// API 基础 URL
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// 模型名称
    /// </summary>
    public string Model { get; set; } = string.Empty;
}

/// <summary>
/// LLM 连接测试响应
/// </summary>
public class TestLlmConnectionResponse
{
    /// <summary>
    /// 测试是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 结果消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 模型信息（响应片段）
    /// </summary>
    public string? ModelInfo { get; set; }

    /// <summary>
    /// 响应时间（毫秒）
    /// </summary>
    public long ResponseTimeMs { get; set; }
}

/// <summary>
/// TTS 配置请求
/// </summary>
public class TtsSettingsRequest
{
    /// <summary>
    /// API 密钥
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// API 基础 URL
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// 语音模型
    /// </summary>
    public string Voice { get; set; } = string.Empty;

    /// <summary>
    /// 语速（0.25-4.0，默认 1.0，用于播放时控制速度）
    /// </summary>
    public float Speed { get; set; } = 1.0f;
}

/// <summary>
/// TTS 配置响应（API Key 脱敏）
/// </summary>
public class TtsSettingsResponse
{
    /// <summary>
    /// 脱敏后的 API 密钥（sk-***...xyz 格式）
    /// </summary>
    public string ApiKeyMasked { get; set; } = string.Empty;

    /// <summary>
    /// API 基础 URL
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// 语音模型
    /// </summary>
    public string Voice { get; set; } = string.Empty;

    /// <summary>
    /// 配置是否有效
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 语速（0.25-4.0，默认 1.0）
    /// </summary>
    public float Speed { get; set; } = 1.0f;
}
