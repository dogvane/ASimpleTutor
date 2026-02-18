using Newtonsoft.Json;

namespace ASimpleTutor.Core.Models.Dto;

/// <summary>
/// 知识图谱关系响应 DTO
/// </summary>
public class KnowledgeGraphRelationshipsResponse
{
    /// <summary>
    /// 关系列表
    /// </summary>
    [JsonProperty("relationships")]
    public List<KnowledgeGraphRelationshipDto>? Relationships { get; set; }
}

/// <summary>
/// 知识图谱关系 DTO
/// </summary>
public class KnowledgeGraphRelationshipDto
{
    /// <summary>
    /// 源节点 ID
    /// </summary>
    [JsonProperty("source_id")]
    public string SourceId { get; set; } = string.Empty;

    /// <summary>
    /// 目标节点 ID
    /// </summary>
    [JsonProperty("target_id")]
    public string TargetId { get; set; } = string.Empty;

    /// <summary>
    /// 关系类型
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "Related";

    /// <summary>
    /// 关系权重（0.0-1.0）
    /// </summary>
    [JsonProperty("weight")]
    public float Weight { get; set; } = 0.5f;

    /// <summary>
    /// 关系描述
    /// </summary>
    [JsonProperty("description")]
    public string? Description { get; set; }
}
