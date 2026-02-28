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
        /// 获取章节树
        /// </summary>
        [HttpGet]
        public IActionResult GetChapters()
        {
            if (_knowledgeSystem == null)
            {
                System.Console.WriteLine("[DEBUG] Knowledge system is null");
                return NotFound(new { error = new { code = "NOT_FOUND", message = "请先激活书籍目录并构建知识体系" } });
            }

            System.Console.WriteLine($"[DEBUG] Knowledge system exists, Tree: {_knowledgeSystem.Tree != null}, Documents count: {_knowledgeSystem.Documents.Count}");

            // 如果有知识点树且不为空，使用知识点树
            if (_knowledgeSystem.Tree != null)
            {
                System.Console.WriteLine("[DEBUG] Using knowledge tree");
                var chapters = BuildChapterTree(_knowledgeSystem.Tree);
                System.Console.WriteLine($"[DEBUG] Built {chapters.Count} chapter nodes from tree");
                if (chapters.Count > 0)
                {
                    return Ok(new { items = chapters });
                }
                System.Console.WriteLine("[DEBUG] Knowledge tree is empty, trying to build from documents");
            }

            // 如果没有知识点树或知识点树为空，但有文档，从文档构建章节树
            if (_knowledgeSystem.Documents.Count > 0)
            {
                System.Console.WriteLine($"[DEBUG] Building chapter tree from {_knowledgeSystem.Documents.Count} documents");
                var chapters = BuildChapterTreeFromDocuments(_knowledgeSystem.Documents);
                System.Console.WriteLine($"[DEBUG] Built {chapters.Count} chapter nodes from documents");
                return Ok(new { items = chapters });
            }

            // 都没有，返回空数组
            System.Console.WriteLine("[DEBUG] No tree and no documents, returning empty array");
            return Ok(new { items = new List<ChapterTreeNode>() });
        }

    /// <summary>
    /// 搜索章节
    /// </summary>
    [HttpGet("search")]
    public IActionResult SearchChapters([FromQuery] string q, [FromQuery] int? limit)
    {
        if (_knowledgeSystem == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "请先激活书籍目录并构建知识体系" } });
        }

        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = "搜索关键词不能为空" } });
        }

        var query = q.ToLower();
        var results = new List<object>();
        int count = 0;

        CollectSearchResults(_knowledgeSystem.Tree, query, results, limit ?? 20, ref count);

        return Ok(new { items = results, total = results.Count });
    }

    /// <summary>
    /// 获取章节下的知识点列表
    /// </summary>
    [HttpGet("knowledge-points")]
    public IActionResult GetChapterKnowledgePoints([FromQuery] string chapterId)
    {
        if (_knowledgeSystem == null)
        {
            return NotFound(new { error = new { code = "NOT_FOUND", message = "请先激活书籍目录并构建知识体系" } });
        }

        if (string.IsNullOrEmpty(chapterId))
        {
            return BadRequest(new { error = new { code = "BAD_REQUEST", message = "chapterId 不能为空" } });
        }

        System.Console.WriteLine($"[DEBUG] GetChapterKnowledgePoints: chapterId = {chapterId}");

        // 首先从知识树收集知识点
        List<dynamic> kps = CollectKnowledgePoints(_knowledgeSystem.Tree, chapterId)
            .Select(kp => new
            {
                kpId = kp.KpId,
                kp.Title,
                Summary = string.Empty
            })
            .Cast<dynamic>()
            .ToList();

        System.Console.WriteLine($"[DEBUG] Collected {kps.Count} knowledge points from tree");

        // 如果从知识树收集不到知识点，尝试从文档中构建知识点
        if (kps.Count == 0 && _knowledgeSystem.Documents.Count > 0)
        {
            System.Console.WriteLine("[DEBUG] No knowledge points in tree, trying to build from documents");
            kps = BuildKnowledgePointsFromDocuments(_knowledgeSystem.Documents, chapterId);
            System.Console.WriteLine($"[DEBUG] Built {kps.Count} knowledge points from documents");
        }

        return Ok(new { items = kps });
    }

    /// <summary>
    /// 从文档构建知识点
    /// </summary>
    private static List<dynamic> BuildKnowledgePointsFromDocuments(List<Document> documents, string chapterId)
    {
        var result = new List<dynamic>();
        var kpIdCounter = 0;

        foreach (var doc in documents)
        {
            foreach (var section in doc.Sections)
            {
                if (section.SectionId == chapterId || section.SubSections.Any(s => s.SectionId == chapterId))
                {
                    // 为当前章节创建知识点
                    result.Add(new
                    {
                        kpId = $"kp_{kpIdCounter++:D4}",
                        Title = section.HeadingPath.LastOrDefault() ?? "未命名知识点",
                        Summary = string.Empty
                    });

                    // 为子章节创建知识点
                    foreach (var subSection in section.SubSections)
                    {
                        if (subSection.SectionId == chapterId || subSection.SubSections.Any(s => s.SectionId == chapterId))
                        {
                            result.Add(new
                            {
                                kpId = $"kp_{kpIdCounter++:D4}",
                                Title = subSection.HeadingPath.LastOrDefault() ?? "未命名知识点",
                                Summary = string.Empty
                            });
                        }
                    }
                }
            }
        }

        return result;
    }

    private static List<ChapterTreeNode> BuildChapterTree(KnowledgeTreeNode? node)
    {
        if (node == null)
            return new List<ChapterTreeNode>();

        var result = new List<ChapterTreeNode>();

        if (node.Children.Count == 0)
        {
            if (node.KnowledgePoint != null)
            {
                result.Add(new ChapterTreeNode
                {
                    Id = node.Id,
                    Title = node.Title,
                    Level = node.HeadingPath.Count,
                    Expanded = false,
                    Children = new List<ChapterTreeNode>()
                });
            }
        }
        else
        {
            foreach (var child in node.Children)
            {
                result.Add(new ChapterTreeNode
                {
                    Id = child.Id,
                    Title = child.Title,
                    Level = child.HeadingPath.Count,
                    Expanded = child.HeadingPath.Count <= 1,
                    Children = BuildChapterTree(child)
                });
            }
        }

        return result;
    }

    /// <summary>
    /// 从文档构建章节树
    /// </summary>
    private static List<ChapterTreeNode> BuildChapterTreeFromDocuments(List<Document> documents)
    {
        var result = new List<ChapterTreeNode>();

        foreach (var doc in documents)
        {
            // 从文档的 Sections 构建章节树
            BuildChapterTreeFromSections(doc.Sections, result, doc.Path);
        }

        return result;
    }

    /// <summary>
    /// 从章节列表递归构建章节树
    /// </summary>
    private static void BuildChapterTreeFromSections(List<Section> sections, List<ChapterTreeNode> result, string filePath)
    {
        foreach (var section in sections)
        {
            var node = new ChapterTreeNode
            {
                Id = section.SectionId,
                Title = section.HeadingPath.Count > 0 ? section.HeadingPath.Last() : section.SectionId,
                Level = section.HeadingPath.Count,
                Expanded = section.HeadingPath.Count <= 1,
                Children = new List<ChapterTreeNode>()
            };

            // 递归处理子章节
            if (section.SubSections.Count > 0)
            {
                BuildChapterTreeFromSections(section.SubSections, node.Children, filePath);
            }

            result.Add(node);
        }
    }

    private static void CollectSearchResults(KnowledgeTreeNode? node, string query, List<object> results, int limit, ref int count)
    {
        if (node == null || count >= limit)
            return;

        if (node.Title.ToLower().Contains(query))
        {
            results.Add(new
            {
                id = node.Id,
                title = node.Title,
                level = node.HeadingPath.Count,
                parentId = node.HeadingPath.Count > 1 ? node.HeadingPath[node.HeadingPath.Count - 2] : null
            });
            count++;
        }

        foreach (var child in node.Children)
        {
            CollectSearchResults(child, query, results, limit, ref count);
            if (count >= limit)
                break;
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

