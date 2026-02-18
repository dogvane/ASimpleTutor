namespace ASimpleTutor.Core.Models;

/// <summary>
/// 知识图谱
/// </summary>
public class KnowledgeGraph
{
    /// <summary>
    /// 知识图谱唯一标识符
    /// </summary>
    public string GraphId { get; set; } = string.Empty;

    /// <summary>
    /// 书籍中心 ID
    /// </summary>
    public string BookHubId { get; set; } = string.Empty;

    /// <summary>
    /// 知识图谱节点列表
    /// </summary>
    public List<KnowledgeGraphNode> Nodes { get; set; } = new();

    /// <summary>
    /// 知识图谱边（关系）列表
    /// </summary>
    public List<KnowledgeGraphEdge> Edges { get; set; } = new();

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 知识图谱节点
/// </summary>
public class KnowledgeGraphNode
{
    /// <summary>
    /// 节点唯一标识符（与知识点 ID 相同）
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// 节点标题（知识点标题）
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 节点类型（知识点类型）
    /// </summary>
    public KpType Type { get; set; } = KpType.Concept;

    /// <summary>
    /// 重要性评分（0.0~1.0）
    /// </summary>
    public float Importance { get; set; }

    /// <summary>
    /// 所属章节路径
    /// </summary>
    public List<string> ChapterPath { get; set; } = new();

    /// <summary>
    /// 节点元数据（可选，用于可视化）
    /// </summary>
    public KnowledgeGraphNodeMetadata? Metadata { get; set; }

    /// <summary>
    /// 关联的知识点
    /// </summary>
    public KnowledgePoint? KnowledgePoint { get; set; }
}

/// <summary>
/// 知识图谱节点元数据（用于可视化）
/// </summary>
public class KnowledgeGraphNodeMetadata
{
    /// <summary>
    /// 节点大小（根据重要性计算）
    /// </summary>
    public float Size { get; set; } = 1.0f;

    /// <summary>
    /// 节点颜色（根据类型）
    /// </summary>
    public string Color { get; set; } = "#667eea";

    /// <summary>
    /// 节点位置（用于可视化布局）
    /// </summary>
    public KnowledgeGraphNodePosition? Position { get; set; }
}

/// <summary>
/// 知识图谱节点位置
/// </summary>
public class KnowledgeGraphNodePosition
{
    /// <summary>
    /// X 坐标
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// Y 坐标
    /// </summary>
    public float Y { get; set; }
}

/// <summary>
/// 知识图谱边（关系）
/// </summary>
public class KnowledgeGraphEdge
{
    /// <summary>
    /// 边唯一标识符
    /// </summary>
    public string EdgeId { get; set; } = string.Empty;

    /// <summary>
    /// 源节点 ID
    /// </summary>
    public string SourceNodeId { get; set; } = string.Empty;

    /// <summary>
    /// 目标节点 ID
    /// </summary>
    public string TargetNodeId { get; set; } = string.Empty;

    /// <summary>
    /// 关系类型
    /// </summary>
    public KnowledgeGraphEdgeType Type { get; set; } = KnowledgeGraphEdgeType.Related;

    /// <summary>
    /// 关系权重（0.0~1.0，值越大关系越强）
    /// </summary>
    public float Weight { get; set; } = 0.5f;

    /// <summary>
    /// 关系描述
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// 知识图谱边类型
/// </summary>
public enum KnowledgeGraphEdgeType
{
    /// <summary>
    /// 相关关系
    /// </summary>
    Related,

    /// <summary>
    /// 依赖关系（源节点依赖目标节点）
    /// </summary>
    DependsOn,

    /// <summary>
    /// 包含关系（源节点包含目标节点）
    /// </summary>
    Contains,

    /// <summary>
    /// 对比关系
    /// </summary>
    Contrast,

    /// <summary>
    /// 示例关系（源节点是目标节点的示例）
    /// </summary>
    ExampleOf,

    /// <summary>
    /// 扩展关系（源节点扩展目标节点）
    /// </summary>
    Extends,

    /// <summary>
    /// 实现关系（源节点实现目标节点）
    /// </summary>
    Implements
}

/// <summary>
/// 知识图谱查询结果
/// </summary>
public class KnowledgeGraphQueryResult
{
    /// <summary>
    /// 查询到的节点列表
    /// </summary>
    public List<KnowledgeGraphNode> Nodes { get; set; } = new();

    /// <summary>
    /// 查询到的边列表
    /// </summary>
    public List<KnowledgeGraphEdge> Edges { get; set; } = new();

    /// <summary>
    /// 总节点数
    /// </summary>
    public int TotalNodes { get; set; }

    /// <summary>
    /// 总边数
    /// </summary>
    public int TotalEdges { get; set; }
}

/// <summary>
/// 知识图谱构建选项
/// </summary>
public class KnowledgeGraphBuildOptions
{
    /// <summary>
    /// 是否包含章节节点
    /// </summary>
    public bool IncludeChapterNodes { get; set; } = true;

    /// <summary>
    /// 最小重要性阈值（小于该值的节点不包含）
    /// </summary>
    public float MinImportance { get; set; } = 0.0f;

    /// <summary>
    /// 最大节点数
    /// </summary>
    public int MaxNodes { get; set; } = 1000;

    /// <summary>
    /// 是否计算节点位置（用于可视化）
    /// </summary>
    public bool CalculateNodePositions { get; set; } = true;

    /// <summary>
    /// 是否添加默认关系
    /// </summary>
    public bool AddDefaultRelations { get; set; } = true;
}
