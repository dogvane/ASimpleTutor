namespace ASimpleTutor.Core.Models;

/// <summary>
/// 学习内容包
/// </summary>
public class LearningPack
{
    /// <summary>
    /// 所属知识点 ID
    /// </summary>
    public string KpId { get; set; } = string.Empty;

    /// <summary>
    /// 精要速览
    /// </summary>
    public Summary Summary { get; set; } = new();

    /// <summary>
    /// 层次化内容列表（L1/L2/L3）
    /// </summary>
    public List<ContentLevel> Levels { get; set; } = new();

    /// <summary>
    /// 关联知识点 ID 列表
    /// </summary>
    public List<string> RelatedKpIds { get; set; } = new();
}

/// <summary>
/// 精要速览
/// </summary>
public class Summary
{
    /// <summary>
    /// 定义（1~3句简明定义）
    /// </summary>
    public string Definition { get; set; } = string.Empty;

    /// <summary>
    /// 核心要点列表（3~7条）
    /// </summary>
    public List<string> KeyPoints { get; set; } = new();

    /// <summary>
    /// 常见误区列表（1~3条）
    /// </summary>
    public List<string> Pitfalls { get; set; } = new();
}

/// <summary>
/// 层次化内容
/// </summary>
public class ContentLevel
{
    /// <summary>
    /// 层级：1=概览, 2=详细, 3=深入
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// 层级标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 层级内容
    /// </summary>
    public string Content { get; set; } = string.Empty;
}
