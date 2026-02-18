using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;

namespace ASimpleTutor.Core.Services;

/// <summary>
/// 知识图谱构建服务实现
/// </summary>
public class KnowledgeGraphBuilder : IKnowledgeGraphBuilder
{
    private readonly KnowledgeSystemStore _knowledgeSystemStore;

    public KnowledgeGraphBuilder(KnowledgeSystemStore knowledgeSystemStore)
    {
        _knowledgeSystemStore = knowledgeSystemStore;
    }

    public KnowledgeGraph Build(List<KnowledgePoint> knowledgePoints, KnowledgeGraphBuildOptions? options = null)
    {
        options ??= new KnowledgeGraphBuildOptions();

        var graph = new KnowledgeGraph
        {
            GraphId = Guid.NewGuid().ToString(),
            BookHubId = knowledgePoints.FirstOrDefault()?.BookHubId ?? string.Empty,
            Nodes = new List<KnowledgeGraphNode>(),
            Edges = new List<KnowledgeGraphEdge>()
        };

        // 过滤知识点
        var filteredPoints = FilterKnowledgePoints(knowledgePoints, options);

        // 构建节点
        BuildNodes(graph, filteredPoints, options);

        // 构建边（关系）
        BuildEdges(graph, options);

        return graph;
    }

    public async Task<KnowledgeGraph> BuildAsync(string bookHubId, KnowledgeGraphBuildOptions? options = null)
    {
        var result = await _knowledgeSystemStore.LoadAsync(bookHubId);
        if (result.KnowledgeSystem == null || result.KnowledgeSystem.KnowledgePoints == null)
        {
            throw new InvalidOperationException($"Knowledge system not found for book hub id: {bookHubId}");
        }

        return Build(result.KnowledgeSystem.KnowledgePoints, options);
    }

    public KnowledgeGraph GetSubgraph(KnowledgeGraph graph, string rootNodeId, int depth = 2)
    {
        var subgraph = new KnowledgeGraph
        {
            GraphId = Guid.NewGuid().ToString(),
            BookHubId = graph.BookHubId,
            Nodes = new List<KnowledgeGraphNode>(),
            Edges = new List<KnowledgeGraphEdge>()
        };

        var visitedNodes = new HashSet<string>();
        var queue = new Queue<(string nodeId, int currentDepth)>();
        queue.Enqueue((rootNodeId, 0));

        while (queue.Count > 0)
        {
            var (nodeId, currentDepth) = queue.Dequeue();

            if (visitedNodes.Contains(nodeId) || currentDepth > depth)
                continue;

            visitedNodes.Add(nodeId);

            var node = graph.Nodes.FirstOrDefault(n => n.NodeId == nodeId);
            if (node != null)
            {
                subgraph.Nodes.Add(node);
            }

            if (currentDepth < depth)
            {
                var outgoingEdges = graph.Edges.Where(e => e.SourceNodeId == nodeId);
                var incomingEdges = graph.Edges.Where(e => e.TargetNodeId == nodeId);

                foreach (var edge in outgoingEdges)
                {
                    subgraph.Edges.Add(edge);
                    queue.Enqueue((edge.TargetNodeId, currentDepth + 1));
                }

                foreach (var edge in incomingEdges)
                {
                    subgraph.Edges.Add(edge);
                    queue.Enqueue((edge.SourceNodeId, currentDepth + 1));
                }
            }
        }

        // 确保所有边引用的节点都包含在子图中
        var nodeIdsInSubgraph = subgraph.Nodes.Select(n => n.NodeId).ToHashSet();
        subgraph.Edges = subgraph.Edges.Where(e =>
            nodeIdsInSubgraph.Contains(e.SourceNodeId) &&
            nodeIdsInSubgraph.Contains(e.TargetNodeId)).ToList();

        return subgraph;
    }

    public KnowledgeGraphQueryResult SearchNodes(KnowledgeGraph graph, string query, int maxResults = 10)
    {
        var lowerQuery = query.ToLower();
        var matchedNodes = graph.Nodes
            .Where(n => n.Title.ToLower().Contains(lowerQuery) ||
                       n.ChapterPath.Any(c => c.ToLower().Contains(lowerQuery)))
            .OrderByDescending(n => n.Importance)
            .Take(maxResults)
            .ToList();

        var matchedNodeIds = matchedNodes.Select(n => n.NodeId).ToHashSet();
        var relatedEdges = graph.Edges.Where(e =>
            matchedNodeIds.Contains(e.SourceNodeId) || matchedNodeIds.Contains(e.TargetNodeId)).ToList();

        return new KnowledgeGraphQueryResult
        {
            Nodes = matchedNodes,
            Edges = relatedEdges,
            TotalNodes = matchedNodes.Count,
            TotalEdges = relatedEdges.Count
        };
    }

    public KnowledgeGraphQueryResult GetNeighbors(KnowledgeGraph graph, string nodeId, int maxNeighbors = 10)
    {
        var neighbors = new List<KnowledgeGraphNode>();
        var edges = new List<KnowledgeGraphEdge>();

        var connectedEdges = graph.Edges.Where(e => e.SourceNodeId == nodeId || e.TargetNodeId == nodeId)
            .OrderByDescending(e => e.Weight)
            .Take(maxNeighbors)
            .ToList();

        edges.AddRange(connectedEdges);

        foreach (var edge in connectedEdges)
        {
            var neighborId = edge.SourceNodeId == nodeId ? edge.TargetNodeId : edge.SourceNodeId;
            var neighbor = graph.Nodes.FirstOrDefault(n => n.NodeId == neighborId);
            if (neighbor != null && !neighbors.Contains(neighbor))
            {
                neighbors.Add(neighbor);
            }
        }

        return new KnowledgeGraphQueryResult
        {
            Nodes = neighbors,
            Edges = edges,
            TotalNodes = neighbors.Count,
            TotalEdges = edges.Count
        };
    }

    public float CalculateSimilarity(KnowledgeGraphNode node1, KnowledgeGraphNode node2)
    {
        if (node1.NodeId == node2.NodeId)
            return 1.0f;

        float similarity = 0.0f;

        // 类型相似度
        if (node1.Type == node2.Type)
            similarity += 0.3f;

        // 章节路径相似度
        var commonChapters = node1.ChapterPath.Intersect(node2.ChapterPath).Count();
        var maxChapters = Math.Max(node1.ChapterPath.Count, node2.ChapterPath.Count);
        if (maxChapters > 0)
            similarity += 0.4f * (commonChapters / (float)maxChapters);

        // 重要性相似度
        similarity += 0.3f * (1 - Math.Abs(node1.Importance - node2.Importance));

        return similarity;
    }

    private List<KnowledgePoint> FilterKnowledgePoints(List<KnowledgePoint> knowledgePoints, KnowledgeGraphBuildOptions options)
    {
        var filtered = knowledgePoints.Where(p => p.Importance >= options.MinImportance);

        if (!options.IncludeChapterNodes)
        {
            filtered = filtered.Where(p => p.Type != KpType.Chapter);
        }

        if (options.MaxNodes > 0 && filtered.Count() > options.MaxNodes)
        {
            filtered = filtered.OrderByDescending(p => p.Importance).Take(options.MaxNodes);
        }

        return filtered.ToList();
    }

    private void BuildNodes(KnowledgeGraph graph, List<KnowledgePoint> knowledgePoints, KnowledgeGraphBuildOptions options)
    {
        foreach (var point in knowledgePoints)
        {
            var node = new KnowledgeGraphNode
            {
                NodeId = point.KpId,
                Title = point.Title,
                Type = point.Type,
                Importance = point.Importance,
                ChapterPath = point.ChapterPath,
                KnowledgePoint = point,
                Metadata = new KnowledgeGraphNodeMetadata
                {
                    Size = CalculateNodeSize(point.Importance),
                    Color = GetNodeColor(point.Type),
                    Position = options.CalculateNodePositions ? CalculateNodePosition(graph, point) : null
                }
            };

            graph.Nodes.Add(node);
        }
    }

    private void BuildEdges(KnowledgeGraph graph, KnowledgeGraphBuildOptions options)
    {
        if (!options.AddDefaultRelations)
            return;

        var nodes = graph.Nodes;

        // 构建不同类型的关系
        foreach (var sourceNode in nodes)
        {
            foreach (var targetNode in nodes)
            {
                if (sourceNode.NodeId == targetNode.NodeId)
                    continue;

                var edge = CreateEdge(sourceNode, targetNode);
                if (edge != null)
                {
                    graph.Edges.Add(edge);
                }
            }
        }
    }

    private KnowledgeGraphEdge? CreateEdge(KnowledgeGraphNode sourceNode, KnowledgeGraphNode targetNode)
    {
        // 根据节点关系创建不同类型的边
        var edgeType = DetermineEdgeType(sourceNode, targetNode);
        var weight = CalculateEdgeWeight(sourceNode, targetNode, edgeType);

        // 过滤弱关系
        if (weight < 0.1f)
            return null;

        return new KnowledgeGraphEdge
        {
            EdgeId = Guid.NewGuid().ToString(),
            SourceNodeId = sourceNode.NodeId,
            TargetNodeId = targetNode.NodeId,
            Type = edgeType,
            Weight = weight,
            Description = GetEdgeDescription(edgeType)
        };
    }

    private KnowledgeGraphEdgeType DetermineEdgeType(KnowledgeGraphNode sourceNode, KnowledgeGraphNode targetNode)
    {
        // 如果是章节节点，可能包含其他节点
        if (sourceNode.Type == KpType.Chapter && targetNode.Type != KpType.Chapter &&
            sourceNode.ChapterPath.SequenceEqual(targetNode.ChapterPath.Take(sourceNode.ChapterPath.Count)))
        {
            return KnowledgeGraphEdgeType.Contains;
        }

        // 相同章节的节点通常是相关关系
        if (sourceNode.ChapterPath.SequenceEqual(targetNode.ChapterPath))
        {
            return KnowledgeGraphEdgeType.Related;
        }

        // 共享相同的父章节的节点也是相关关系
        var minDepth = Math.Min(sourceNode.ChapterPath.Count, targetNode.ChapterPath.Count);
        if (sourceNode.ChapterPath.Take(minDepth - 1).SequenceEqual(targetNode.ChapterPath.Take(minDepth - 1)))
        {
            return KnowledgeGraphEdgeType.Related;
        }

        // 默认关系
        return KnowledgeGraphEdgeType.Related;
    }

    private float CalculateEdgeWeight(KnowledgeGraphNode sourceNode, KnowledgeGraphNode targetNode, KnowledgeGraphEdgeType edgeType)
    {
        float weight = 0.0f;

        // 基于关系类型的权重
        switch (edgeType)
        {
            case KnowledgeGraphEdgeType.Contains:
                weight = 0.8f;
                break;
            case KnowledgeGraphEdgeType.DependsOn:
                weight = 0.7f;
                break;
            case KnowledgeGraphEdgeType.Related:
                weight = 0.5f;
                break;
            default:
                weight = 0.3f;
                break;
        }

        // 基于相似度调整权重
        var similarity = CalculateSimilarity(sourceNode, targetNode);
        weight *= (0.5f + similarity * 0.5f);

        return Math.Min(1.0f, weight);
    }

    private float CalculateNodeSize(float importance)
    {
        return 0.5f + importance * 1.5f;
    }

    private string GetNodeColor(KpType type)
    {
        return type switch
        {
            KpType.Concept => "#667eea",
            KpType.Chapter => "#f56565",
            KpType.Process => "#48bb78",
            KpType.Api => "#ed8936",
            KpType.BestPractice => "#9f7aea",
            _ => "#667eea"
        };
    }

    private KnowledgeGraphNodePosition? CalculateNodePosition(KnowledgeGraph graph, KnowledgePoint point)
    {
        // 基于章节路径计算位置（简单的布局算法）
        var x = point.ChapterPath.Count * 200;
        var y = graph.Nodes.Count % 10 * 100 + point.ChapterPath.Count * 50;
        return new KnowledgeGraphNodePosition { X = x, Y = y };
    }

    private string? GetEdgeDescription(KnowledgeGraphEdgeType type)
    {
        return type switch
        {
            KnowledgeGraphEdgeType.Contains => "包含",
            KnowledgeGraphEdgeType.DependsOn => "依赖",
            KnowledgeGraphEdgeType.Related => "相关",
            KnowledgeGraphEdgeType.Contrast => "对比",
            KnowledgeGraphEdgeType.ExampleOf => "示例",
            KnowledgeGraphEdgeType.Extends => "扩展",
            KnowledgeGraphEdgeType.Implements => "实现",
            _ => null
        };
    }
}
