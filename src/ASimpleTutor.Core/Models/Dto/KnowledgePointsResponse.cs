using Newtonsoft.Json;

namespace ASimpleTutor.Core.Models.Dto;

/// <summary>
/// LLM 响应数据结构（用于接收 LLM 返回的知识点 JSON）
/// </summary>
public class KnowledgePointsResponse
{
    [JsonProperty("schema_version")]
    public string? SchemaVersion { get; set; }

    [JsonProperty("knowledge_points")]
    public List<KnowledgePointDto> KnowledgePoints { get; set; } = new();
}

/// <summary>
/// 知识点 DTO（从 LLM 接收）
/// </summary>
public class KnowledgePointDto
{
    [JsonProperty("kp_id")]
    public string? KpId { get; set; }

    [JsonProperty("title")]
    public string? Title { get; set; }

    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("aliases")]
    public List<string>? Aliases { get; set; }

    [JsonProperty("chapter_path")]
    public List<string>? ChapterPath { get; set; }

    [JsonProperty("importance")]
    public float Importance { get; set; }

    [JsonProperty("snippet_ids")]
    public List<string>? SnippetIds { get; set; }

    [JsonProperty("summary")]
    public string? Summary { get; set; }

    [JsonProperty("doc_id")]
    public string? DocId { get; set; }

    public KnowledgePoint ToKnowledgePoint()
    {
        return new KnowledgePoint
        {
            KpId = KpId ?? string.Empty,
            Title = Title ?? string.Empty,
            Type = ParseKpType(Type),
            Aliases = Aliases ?? new List<string>(),
            ChapterPath = ChapterPath ?? new List<string>(),
            Importance = Importance,
            SnippetIds = SnippetIds ?? new List<string>(),
            DocId = DocId
        };
    }

    private static KpType ParseKpType(string? type)
    {
        if (string.IsNullOrEmpty(type))
        {
            return KpType.Concept;
        }

        return type.ToLowerInvariant() switch
        {
            "concept" => KpType.Concept,
            "chapter" => KpType.Chapter,
            "process" => KpType.Process,
            "api" => KpType.Api,
            "bestpractice" or "best_practice" or "bestpractice" => KpType.BestPractice,
            _ => KpType.Concept
        };
    }
}

/// <summary>
/// 关联关系响应 DTO
/// </summary>
public class RelationsResponse
{
    [JsonProperty("relations")]
    public List<KnowledgeRelationDto> Relations { get; set; } = new();
}

/// <summary>
/// 关联关系 DTO
/// </summary>
public class KnowledgeRelationDto
{
    [JsonProperty("target_title")]
    public string TargetTitle { get; set; } = string.Empty;

    [JsonProperty("relation_type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("description")]
    public string? Description { get; set; }
}
