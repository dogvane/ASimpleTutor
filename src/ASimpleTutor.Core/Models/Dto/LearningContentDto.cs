using Newtonsoft.Json;
using ASimpleTutor.Core.Models;

namespace ASimpleTutor.Core.Models.Dto;

/// <summary>
/// LLM 响应数据结构（用于接收学习内容 JSON）
/// </summary>
public class LearningContentDto
{
    [JsonProperty("summary")]
    public Summary Summary { get; set; } = new();

    [JsonProperty("levels")]
    public List<ContentLevel> Levels { get; set; } = new();

    [JsonProperty("slide_cards")]
    public List<SlideCardDto> SlideCards { get; set; } = new();
}
