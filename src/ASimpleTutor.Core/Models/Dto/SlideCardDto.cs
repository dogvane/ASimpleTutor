using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ASimpleTutor.Core.Models.Dto;

/// <summary>
/// 幻灯片卡片响应（用于前端展示）
/// 直接复用知识点现有内容，按幻灯片格式组织
/// </summary>
public class SlideCardResponse
{
    /// <summary>
    /// Schema 版本
    /// </summary>
    [JsonProperty("schemaVersion")]
    public string SchemaVersion { get; set; } = "1.0";

    /// <summary>
    /// 知识点 ID
    /// </summary>
    [JsonProperty("kpId")]
    public string KpId { get; set; } = string.Empty;

    /// <summary>
    /// 知识点标题
    /// </summary>
    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 知识点类型
    /// </summary>
    [JsonProperty("kpType")]
    public string KpType { get; set; } = string.Empty;

    /// <summary>
    /// 幻灯片列表
    /// </summary>
    [JsonProperty("slides")]
    public List<SlideCardDto> Slides { get; set; } = new();

    /// <summary>
    /// 元数据
    /// </summary>
    [JsonProperty("meta")]
    public SlideMetaDto Meta { get; set; } = new();
}

/// <summary>
/// 幻灯片卡片 DTO（简化版，直接复用知识点内容）
/// </summary>
public class SlideCardDto
{
    /// <summary>
    /// 幻灯片唯一标识
    /// </summary>
    [JsonProperty("slideId")]
    public string SlideId { get; set; } = string.Empty;

    /// <summary>
    /// 幻灯片类型
    /// </summary>
    [JsonProperty("type")]
    [JsonConverter(typeof(StringEnumConverter))]
    public SlideTypeDto Type { get; set; }

    /// <summary>
    /// 排序序号
    /// </summary>
    [JsonProperty("order")]
    public int Order { get; set; }

    /// <summary>
    /// 幻灯片标题
    /// </summary>
    [JsonProperty("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 幻灯片副标题（可选）
    /// </summary>
    [JsonProperty("subtitle", NullValueHandling = NullValueHandling.Ignore)]
    public string? Subtitle { get; set; }

    /// <summary>
    /// 幻灯片内容（Markdown 格式，前端渲染）
    /// 前端根据 type 字段决定如何渲染此内容
    /// </summary>
    [JsonProperty("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 知识点链接列表
    /// </summary>
    [JsonProperty("kpLinks")]
    public List<KnowledgePointLinkDto> KpLinks { get; set; } = new();

    /// <summary>
    /// 幻灯片配置
    /// </summary>
    [JsonProperty("config")]
    public SlideConfigDto Config { get; set; } = new();
}

/// <summary>
/// 幻灯片类型 DTO（与后端 SlideType 对应）
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum SlideTypeDto
{
    /// <summary>
    /// 封面/导言 - 展示 Summary 内容
    /// </summary>
    [JsonProperty("cover")]
    Cover,

    /// <summary>
    /// 概念解释 - 展示 Levels[0] 或 L1 内容
    /// </summary>
    [JsonProperty("explanation")]
    Explanation,

    /// <summary>
    /// 详细内容 - 展示 Levels[1] 或 L2 内容
    /// </summary>
    [JsonProperty("detail")]
    Detail,

    /// <summary>
    /// 深入探讨 - 展示 Levels[2] 或 L3 内容
    /// </summary>
    [JsonProperty("deepDive")]
    DeepDive,

    /// <summary>
    /// 原文对照 - 展示 Snippets 内容
    /// </summary>
    [JsonProperty("source")]
    Source,

    /// <summary>
    /// 随堂测验 - 展示 Exercises 内容
    /// </summary>
    [JsonProperty("quiz")]
    Quiz,

    /// <summary>
    /// 知识关联 - 展示 Relations 内容
    /// </summary>
    [JsonProperty("relations")]
    Relations,

    /// <summary>
    /// 总结回顾
    /// </summary>
    [JsonProperty("summary")]
    Summary
}

/// <summary>
/// 知识点链接 DTO
/// </summary>
public class KnowledgePointLinkDto
{
    [JsonProperty("text")]
    public string Text { get; set; } = string.Empty;

    [JsonProperty("targetKpId")]
    public string TargetKpId { get; set; } = string.Empty;

    [JsonProperty("relationship")]
    public string Relationship { get; set; } = string.Empty; // prerequisite, related, contrast, similar, contains

    [JsonProperty("targetTitle")]
    public string? TargetTitle { get; set; }
}

/// <summary>
/// 幻灯片配置 DTO
/// </summary>
public class SlideConfigDto
{
    [JsonProperty("allowSkip")]
    public bool AllowSkip { get; set; } = true;

    [JsonProperty("requireComplete")]
    public bool RequireComplete { get; set; } = false;

    [JsonProperty("estimatedTime")]
    public int EstimatedTime { get; set; } = 60; // 预计阅读时长（秒）
}

/// <summary>
/// 幻灯片元数据 DTO
/// </summary>
public class SlideMetaDto
{
    [JsonProperty("totalSlides")]
    public int TotalSlides { get; set; }

    [JsonProperty("estimatedTime")]
    public int EstimatedTime { get; set; } // 总预计时长（秒）

    [JsonProperty("difficulty")]
    public string Difficulty { get; set; } = "beginner"; // beginner, intermediate, advanced

    [JsonProperty("generatedAt")]
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
