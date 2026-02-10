namespace ASimpleTutor.Core.Models;

/// <summary>
/// 知识系统（包含知识体系和原文片段）
/// </summary>
public class KnowledgeSystem
{
    /// <summary>
    /// 书籍中心 ID
    /// </summary>
    public string BookHubId { get; set; } = string.Empty;

    /// <summary>
    /// 知识点列表
    /// </summary>
    public List<KnowledgePoint> KnowledgePoints { get; set; } = new();

    /// <summary>
    /// 按章节路径组织的知识点树
    /// </summary>
    public KnowledgeTreeNode? Tree { get; set; }
}

/// <summary>
/// 知识树节点
/// </summary>
public class KnowledgeTreeNode
{
    /// <summary>
    /// 节点唯一标识符
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 节点标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 标题路径（完整章节路径）
    /// </summary>
    public List<string> HeadingPath { get; set; } = new();

    /// <summary>
    /// 关联的知识点（可能为 null，仅叶子节点有知识点）
    /// </summary>
    public KnowledgePoint? KnowledgePoint { get; set; }

    /// <summary>
    /// 子节点列表
    /// </summary>
    public List<KnowledgeTreeNode> Children { get; set; } = new();
}
