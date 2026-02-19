using ASimpleTutor.Api.Configuration;
using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ASimpleTutor.Api.Controllers;

/// <summary>
/// 章节控制�?
/// </summary>
[ApiController]
[Route("api/v1/chapters")]
public class ChaptersController : ControllerBase
{
    private static KnowledgeSystem? _knowledgeSystem;
    private static readonly object _lock = new();
    private readonly ILearningContentGenerator _learningContentGenerator;

    public ChaptersController(ILearningContentGenerator learningContentGenerator)
    {
        _learningContentGenerator = learningContentGenerator;
    }

    public static void SetKnowledgeSystem(KnowledgeSystem? ks)
    {
        lock (_lock)
        {
            _knowledgeSystem = ks;
        }
    }

    /// <summary>
    /// 判断是否为排除章节（习题、小结、参考文献等）
    /// </summary>
    private static bool IsExcludedChapter(string title)
    {
        // 使用与 SectioningOptions 中相同的逻辑判断是否为排除章节
        if (string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        var normalizedTitle = title.Trim().ToLowerInvariant();

        // 与 SectioningOptions.ExcludedSectionTitles 保持一致
        var excludedKeywords = new[] { "习题", "练习", "本章小结", "本章总结", "章节小结", "章节总结", "参考文献", "exercises", "practice", "chapter summary", "chapter conclusion", "references" };
        return excludedKeywords.Any(keyword => normalizedTitle.Contains(keyword));
    }

    private static List<KnowledgePoint> CollectKnowledgePoints(KnowledgeTreeNode? node, string chapterId)
    {
        var result = new List<KnowledgePoint>();

        if (node == null)
            return result;

        // 过滤掉排除章节（习题、小结、参考文献等�?
        if (IsExcludedChapter(node.Title))
            return result;

        // 检查当前节点是否匹配（精确匹配 ID �?ID 是当前节点的路径前缀�?
        bool isMatch = node.Id == chapterId || node.Id.StartsWith(chapterId + "_");

        if (isMatch)
        {
            // 当前节点匹配，收集它的知识点和所有子节点的知识点
            if (node.KnowledgePoint != null)
            {
                result.Add(node.KnowledgePoint);
            }
            foreach (var child in node.Children)
            {
                result.AddRange(CollectKnowledgePoints(child, chapterId));
            }
        }
        else
        {
            // 当前节点不匹配，继续在子节点中查�?
            foreach (var child in node.Children)
            {
                result.AddRange(CollectKnowledgePoints(child, chapterId));
            }
        }

        return result;
    }
}

public class ChapterTreeNode
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Level { get; set; }
    public bool Expanded { get; set; }
    public List<ChapterTreeNode> Children { get; set; } = new();
}

