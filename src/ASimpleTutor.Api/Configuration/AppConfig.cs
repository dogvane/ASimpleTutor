using ASimpleTutor.Core.Configuration;
using ASimpleTutor.Core.Models;

namespace ASimpleTutor.Api.Configuration;

/// <summary>
/// 应用配置
/// </summary>
public class AppConfig
{
    public List<BookHubConfig> BookHubs { get; set; } = new();
    public string? ActiveBookHubId { get; set; }
    public LlmConfig Llm { get; set; } = new();
    public TtsConfig Tts { get; set; } = new();
    public string? StoragePath { get; set; }
    public SectioningOptions Sectioning { get; set; } = new();
}

/// <summary>
/// 书籍中心配置
/// </summary>
public class BookHubConfig : BookHub
{
}

/// <summary>
/// LLM 配置
/// </summary>
public class LlmConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string Model { get; set; } = "gpt-4";
    public int Concurrency { get; set; } = 1;
}

/// <summary>
/// TTS 配置
/// </summary>
public class TtsConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string Voice { get; set; } = "alloy";
    public float Speed { get; set; } = 1.0f;
}
