using ASimpleTutor.Api.Configuration;
using ASimpleTutor.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ASimpleTutor.Api.Controllers;

/// <summary>
/// 章节控制器
/// </summary>
[ApiController]
[Route("api/v1/chapters")]
public class ChaptersController : ControllerBase
{
    private static KnowledgeSystem? _knowledgeSystem;
    private static readonly object _lock = new();

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
            return NotFound(new { error = new { code = "NOT_FOUND", message = "请先激活书籍目录并构建知识体系" } });
        }

        var chapters = BuildChapterTree(_knowledgeSystem.Tree);

        return Ok(new { items = chapters });
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

        var kps = CollectKnowledgePoints(_knowledgeSystem.Tree, chapterId)
            .Select(kp => new
            {
                kpId = kp.KpId,
                kp.Title,
                Summary = string.Empty
            })
            .ToList();

        return Ok(new { items = kps });
    }

    private static List<ChapterTreeNode> BuildChapterTree(KnowledgeTreeNode? node)
    {
        if (node == null)
            return new List<ChapterTreeNode>();

        var result = new List<ChapterTreeNode>();

        // 只处理第一层和第二层
        foreach (var child in node.Children)
        {
            var headingCount = child.HeadingPath.Count;

            // 第一层和第二层才显示
            if (headingCount <= 2)
            {
                result.Add(new ChapterTreeNode
                {
                    Id = child.Id,
                    Title = child.Title,
                    Level = headingCount,
                    // 只有第一层默认展开
                    Expanded = headingCount == 1,
                    // 只对第一层递归构建子节点（第二层不展开子节点）
                    Children = headingCount == 1 ? BuildSecondLevelTree(child) : new List<ChapterTreeNode>()
                });
            }
        }

        return result;
    }

    /// <summary>
    /// 构建第二层树（不展开子节点）
    /// </summary>
    private static List<ChapterTreeNode> BuildSecondLevelTree(KnowledgeTreeNode? node)
    {
        if (node == null)
            return new List<ChapterTreeNode>();

        var result = new List<ChapterTreeNode>();

        foreach (var child in node.Children)
        {
            var headingCount = child.HeadingPath.Count;

            // 只显示第二层，不递归更深
            if (headingCount <= 2)
            {
                // 第二层的 title 只显示最后一级的名称
                var lastTitle = child.HeadingPath.Count > 0
                    ? child.HeadingPath[child.HeadingPath.Count - 1]
                    : child.Title;

                result.Add(new ChapterTreeNode
                {
                    Id = child.Id,
                    Title = lastTitle,
                    Level = 2, // 第二层固定为 level 2
                    Expanded = false,
                    Children = new List<ChapterTreeNode>()
                });
            }
        }

        return result;
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

    private static List<KnowledgePoint> CollectKnowledgePoints(KnowledgeTreeNode? node, string chapterId)
    {
        var result = new List<KnowledgePoint>();

        if (node == null)
            return result;

        // 检查当前节点是否匹配（精确匹配 ID 或 ID 是当前节点的路径前缀）
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
            // 当前节点不匹配，继续在子节点中查找
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
