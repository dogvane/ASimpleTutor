namespace ASimpleTutor.Core.Models;

/// <summary>
/// 文档（主书内容文档）
/// </summary>
public class Document
{
    /// <summary>
    /// 文档唯一标识符
    /// </summary>
    public string DocId { get; set; } = string.Empty;

    /// <summary>
    /// 所属书籍目录 ID
    /// </summary>
    public string BookRootId { get; set; } = string.Empty;

    /// <summary>
    /// 文档的本地文件系统完整路径
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 文档标题（通常从 H1 标题提取）
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 文档章节列表
    /// </summary>
    public List<Section> Sections { get; set; } = new();

    /// <summary>
    /// 文档内容的 SHA256 哈希值，用于检测变更
    /// </summary>
    public string? ContentHash { get; set; }
}

/// <summary>
/// 文档章节
/// </summary>
public class Section
{
    /// <summary>
    /// 章节唯一标识符
    /// </summary>
    public string SectionId { get; set; } = string.Empty;

    /// <summary>
    /// 标题路径（从根标题到当前章节的层级路径）
    /// </summary>
    public List<string> HeadingPath { get; set; } = new();

    /// <summary>
    /// 子章节列表（层级结构）
    /// </summary>
    public List<Section> SubSections { get; set; } = new();

    /// <summary>
    /// 章节开始的行号（包含标题行）
    /// </summary>
    public int StartLine { get; set; }

    /// <summary>
    /// 章节结束的行号（不包含）
    /// </summary>
    public int EndLine { get; set; }

    /// <summary>
    /// 原始字符数（包含所有内容，包括代码块和HTML标签）
    /// </summary>
    public int OriginalLength { get; set; }

    /// <summary>
    /// 有效字符数（过滤后，排除代码块和HTML标签）
    /// </summary>
    public int EffectiveLength { get; set; }

    /// <summary>
    /// 过滤掉的字符数（原始字符数 - 有效字符数）
    /// </summary>
    public int FilteredLength { get; set; }

    /// <summary>
    /// 是否被排除（不参与后续处理）
    /// </summary>
    public bool IsExcluded { get; set; } = false;
}
