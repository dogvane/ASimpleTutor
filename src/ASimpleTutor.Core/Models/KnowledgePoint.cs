namespace ASimpleTutor.Core.Models;

/// <summary>
/// 知识点
/// </summary>
public class KnowledgePoint
{
    /// <summary>
    /// 知识点唯一标识符
    /// </summary>
    public string KpId { get; set; } = string.Empty;

    /// <summary>
    /// 所属书籍目录 ID
    /// </summary>
    public string BookRootId { get; set; } = string.Empty;

    /// <summary>
    /// 知识点标题（通常是概念名称或术语）
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 知识点别名列表（用于搜索匹配）
    /// </summary>
    public List<string> Aliases { get; set; } = new();

    /// <summary>
    /// 所属章节路径（从根章节到当前章节的层级路径）
    /// </summary>
    public List<string> ChapterPath { get; set; } = new();

    /// <summary>
    /// 重要性评分（0.0~1.0，值越大越重要）
    /// </summary>
    public float Importance { get; set; }

    /// <summary>
    /// 关联的原文片段 ID 列表
    /// </summary>
    public List<string> SnippetIds { get; set; } = new();

    /// <summary>
    /// 与其他知识点的关联关系
    /// </summary>
    public List<KnowledgeRelation> Relations { get; set; } = new();
}

/// <summary>
/// 知识点关联关系
/// </summary>
public class KnowledgeRelation
{
    /// <summary>
    /// 关联的目标知识点 ID
    /// </summary>
    public string ToKpId { get; set; } = string.Empty;

    /// <summary>
    /// 关联类型
    /// </summary>
    public RelationType Type { get; set; }
}

/// <summary>
/// 知识点关联类型
/// </summary>
public enum RelationType
{
    /// <summary>
    /// 前置依赖（学习当前知识点前应先掌握）
    /// </summary>
    Prerequisite,

    /// <summary>
    /// 对比关系（与关联知识点进行对比学习）
    /// </summary>
    Comparison,

    /// <summary>
    /// 包含/组成（关联知识点是当前知识点的组成部分）
    /// </summary>
    Contains,

    /// <summary>
    /// 一般关联（存在某种关联关系）
    /// </summary>
    Related
}
