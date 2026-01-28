using ASimpleTutor.Core.Models;

namespace ASimpleTutor.Api.Configuration;

/// <summary>
/// 应用配置
/// </summary>
public class AppConfig
{
    public List<BookRootConfig> BookRoots { get; set; } = new();
    public string? ActiveBookRootId { get; set; }
    public LlmConfig Llm { get; set; } = new();
    public string? StoragePath { get; set; }
}

/// <summary>
/// 书籍目录配置
/// </summary>
public class BookRootConfig : BookRoot
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
}
