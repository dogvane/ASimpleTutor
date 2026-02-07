namespace ASimpleTutor.Core.Models;

/// <summary>
/// 标题节点，用于构建标题树
/// </summary>
public class HeadingNode
{
    public int Level { get; set; }
    public string Text { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public int ContentLength { get; set; }
    public int OriginalLength { get; set; }
    public int EffectiveLength { get; set; }
    public List<HeadingNode> Children { get; set; } = new();
    public HeadingNode? Parent { get; set; }
}

/// <summary>
/// 层级统计信息
/// </summary>
public class LevelStatistics
{
    public int Level { get; set; }
    public int SectionCount { get; set; }
    public int AverageLength { get; set; }
    public int MinLength { get; set; }
    public int MaxLength { get; set; }
}