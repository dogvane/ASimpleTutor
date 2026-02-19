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
    /// 书籍中心 ID
    /// </summary>
    public string BookHubId { get; set; } = string.Empty;

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

    /// <summary>
    /// 根据 SectionId 查找对应的章节（递归搜索所有子章节）
    /// </summary>
    /// <param name="sectionId">章节唯一标识符</param>
    /// <returns>找到的章节对象，未找到返回 null</returns>
    public Section? FindSectionById(string sectionId)
    {
        return FindSectionByIdRecursive(Sections, sectionId);
    }

    /// <summary>
    /// 递归查找章节的辅助方法
    /// </summary>
    private Section? FindSectionByIdRecursive(List<Section> sections, string sectionId)
    {
        foreach (var section in sections)
        {
            if (section.SectionId == sectionId)
            {
                return section;
            }

            var foundInSubSections = FindSectionByIdRecursive(section.SubSections, sectionId);
            if (foundInSubSections != null)
            {
                return foundInSubSections;
            }
        }

        return null;
    }
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

/// <summary>
/// 行号范围
/// </summary>
public class LineRange
{
    /// <summary>
    /// 起始行
    /// </summary>
    public int Start { get; set; }

    /// <summary>
    /// 结束行
    /// </summary>
    public int End { get; set; }
}
