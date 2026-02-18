using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using ASimpleTutor.Core.Models.Dto;
using Microsoft.Extensions.Logging;

namespace ASimpleTutor.Core.Services;

/// <summary>
/// 知识图谱构建服务实现
/// </summary>
public class KnowledgeGraphBuilder : IKnowledgeGraphBuilder
{
    private readonly KnowledgeSystemStore _knowledgeSystemStore;
    private readonly KnowledgeGraphStore _graphStore;
    private readonly ILLMService? _llmService;
    private readonly ILogger<KnowledgeGraphBuilder> _logger;

    public KnowledgeGraphBuilder(
        KnowledgeSystemStore knowledgeSystemStore,
        KnowledgeGraphStore graphStore,
        ILLMService? llmService = null,
        ILogger<KnowledgeGraphBuilder>? logger = null)
    {
        _knowledgeSystemStore = knowledgeSystemStore;
        _graphStore = graphStore;
        _llmService = llmService;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<KnowledgeGraphBuilder>.Instance;
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

        // 同步方法不支持 LLM 关系提取，不构建边
        // 如需构建知识图谱关系，请使用 BuildAsync 方法

        return graph;
    }

    public async Task<KnowledgeGraph> BuildAsync(List<KnowledgePoint> knowledgePoints, KnowledgeGraphBuildOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new KnowledgeGraphBuildOptions();

        _logger.LogInformation("开始异步构建知识图谱，知识点数量: {Count}", knowledgePoints.Count);

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
        _logger.LogInformation("知识图谱节点构建完成，节点数量: {Count}", graph.Nodes.Count);

        // 如果没有 LLM 服务，抛出错误
        if (_llmService == null)
        {
            throw new InvalidOperationException(
                $"知识图谱构建失败：未配置 LLM 服务。知识点数量: {knowledgePoints.Count}，" +
                $"BookHubId: {knowledgePoints.FirstOrDefault()?.BookHubId ?? "未设置"}");
        }

        _logger.LogInformation("使用 LLM 提取知识点之间的关系");
        await BuildEdgesWithLLMAsync(graph, filteredPoints, options, cancellationToken);
        _logger.LogInformation("LLM 关系提取完成，边数量: {Count}", graph.Edges.Count);

        // 保存知识图谱
        await _graphStore.SaveAsync(graph, cancellationToken);
        _logger.LogInformation("知识图谱已保存: {BookHubId}", graph.BookHubId);

        return graph;
    }

    public async Task<KnowledgeGraph> BuildAsync(string bookHubId, KnowledgeGraphBuildOptions? options = null)
    {
        var result = await _knowledgeSystemStore.LoadAsync(bookHubId);
        if (result.KnowledgeSystem == null || result.KnowledgeSystem.KnowledgePoints == null)
        {
            throw new InvalidOperationException($"Knowledge system not found for book hub id: {bookHubId}");
        }

        // 使用异步构建方法以支持 LLM 关系提取
        var graph = await BuildAsync(result.KnowledgeSystem.KnowledgePoints, options);

        // 确保设置 BookHubId
        graph.BookHubId = bookHubId;

        return graph;
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

    /// <summary>
    /// 使用 LLM 提取知识点之间的关系
    /// </summary>
    private async Task BuildEdgesWithLLMAsync(KnowledgeGraph graph, List<KnowledgePoint> knowledgePoints, KnowledgeGraphBuildOptions options, CancellationToken cancellationToken)
    {
        if (_llmService == null)
            return;

        // 将知识点分批处理，每批最多 20 个知识点（避免单次请求过大）
        var batches = knowledgePoints
            .Select((kp, index) => new { kp, index })
            .GroupBy(x => x.index / 20)
            .Select(g => g.Select(x => x.kp).ToList())
            .ToList();

        _logger.LogInformation("将 {TotalCount} 个知识点分为 {BatchCount} 批进行关系提取（并发处理）", knowledgePoints.Count, batches.Count);

        // 使用并发处理批次
        var allEdges = new List<KnowledgeGraphEdge>();
        var lockObj = new object();

        await Parallel.ForEachAsync(batches, new ParallelOptions { CancellationToken = cancellationToken }, async (batch, ct) =>
        {
            try
            {
                var edges = await ExtractRelationshipsForBatchAsync(batch, graph, ct);

                // 线程安全地添加边
                lock (lockObj)
                {
                    allEdges.AddRange(edges);
                }
            }
            catch (Exception ex)
            {
                var batchKpIds = string.Join(", ", batch.Select(kp => kp.KpId));
                _logger.LogError(ex, "批次关系提取失败，批次知识点: {KpIds}", batchKpIds);
                throw new InvalidOperationException($"关系提取失败，批次知识点: {batchKpIds}", ex);
            }
        });

        graph.Edges = allEdges;
    }

    /// <summary>
    /// 为一批知识点提取关系
    /// </summary>
    private async Task<List<KnowledgeGraphEdge>> ExtractRelationshipsForBatchAsync(List<KnowledgePoint> batch, KnowledgeGraph graph, CancellationToken cancellationToken)
    {
        var systemPrompt = @"你是一个知识图谱关系提取专家。你的任务是分析给定的知识点列表，识别它们之间的语义关系。

请分析知识点之间的以下类型关系：
1. Related（相关）：知识点内容相关或有联系
2. DependsOn（依赖）：源知识点依赖目标知识点（需要先理解目标知识点）
3. Contains（包含）：源知识点包含目标知识点
4. Contrast（对比）：源知识点与目标知识点形成对比
5. ExampleOf（示例）：源知识点是目标知识点的示例
6. Extends（扩展）：源知识点扩展了目标知识点
7. Implements（实现）：源知识点实现了目标知识点

请以 JSON 格式输出，必须严格按照以下格式：
{
  ""relationships"": [
    {
      ""source_id"": ""知识点ID（必须使用提供的ID）"",
      ""target_id"": ""知识点ID（必须使用提供的ID）"",
      ""type"": ""Related"",
      ""weight"": 0.5,
      ""description"": ""关系描述""
    }
  ]
}

重要规则：
1. source_id 和 target_id 必须使用提供的知识点 ID（如 kp_0000, kp_0001 等）
2. type 必须是以下值之一：Related, DependsOn, Contains, Contrast, ExampleOf, Extends, Implements
3. weight 是 0.3 到 1.0 之间的数字
4. 只为真正有关系的知识点创建边，不要为所有知识点对创建关系
5. 同一章或相邻章节的知识点更可能有 Related 关系
6. 依赖关系要谨慎判断，确保是真正的学习依赖
7. 每个知识点至少应该与 1-3 个其他知识点有关系
8. 避免创建过多的关系，专注于最重要的关系

示例：
如果知识点 kp_0000（智能体基础）和 kp_0001（智能体架构）相关，应该创建：
{
  ""source_id"": ""kp_0000"",
  ""target_id"": ""kp_0001"",
  ""type"": ""Related"",
  ""weight"": 0.7,
  ""description"": ""智能体架构是智能体基础的延伸""
}";

        // 构建知识点摘要
        var knowledgePointsSummary = string.Join("\n\n", batch.Select(kp =>
            $"ID: {kp.KpId}\n" +
            $"标题: {kp.Title}\n" +
            $"类型: {kp.Type}\n" +
            $"章节: {string.Join(" > ", kp.ChapterPath)}\n" +
            $"定义: {kp.Summary?.Definition ?? "无"}"
        ));

        var userMessage = $"请分析以下 {batch.Count} 个知识点之间的关系，为每个知识点创建 1-3 个关系：\n\n{knowledgePointsSummary}\n\n" +
                          $"请返回 JSON 格式的关系列表，确保 source_id 和 target_id 使用上面提供的 ID。";

        _logger.LogInformation("发送 LLM 请求，知识点数量: {Count}", batch.Count);
        _logger.LogDebug("用户消息: {UserMessage}", userMessage);

        var response = await _llmService.ChatJsonAsync<KnowledgeGraphRelationshipsResponse>(
            systemPrompt,
            userMessage,
            cancellationToken);

        // 记录响应详情
        if (response == null)
        {
            _logger.LogError("LLM 返回的响应为 null");
            throw new InvalidOperationException("LLM 返回的响应为 null");
        }

        if (response.Relationships == null)
        {
            _logger.LogError("LLM 返回的 Relationships 字段为 null");
            _logger.LogError("完整响应对象: {Response}", Newtonsoft.Json.JsonConvert.SerializeObject(response));
            throw new InvalidOperationException("LLM 返回的 Relationships 字段为 null");
        }

        _logger.LogInformation("LLM 返回了 {RelationshipCount} 条关系", response.Relationships.Count);

        var edges = new List<KnowledgeGraphEdge>();
        var graphNodeIds = graph.Nodes.Select(n => n.NodeId).ToHashSet();

        foreach (var rel in response.Relationships)
        {
            _logger.LogDebug("处理关系: SourceId=[{SourceId}], TargetId=[{TargetId}], Type=[{Type}]",
                rel.SourceId, rel.TargetId, rel.Type);

            // 验证节点是否存在
            var sourceExists = graphNodeIds.Contains(rel.SourceId);
            var targetExists = graphNodeIds.Contains(rel.TargetId);

            if (!sourceExists)
            {
                _logger.LogWarning("源节点不存在: {SourceId}", rel.SourceId);
                _logger.LogWarning("可用的节点ID（前10个）: {NodeIds}", string.Join(", ", graphNodeIds.Take(10)));
                _logger.LogWarning("完整的关系对象: {Relationship}", Newtonsoft.Json.JsonConvert.SerializeObject(rel));
                continue;
            }

            if (!targetExists)
            {
                _logger.LogWarning("目标节点不存在: {TargetId}", rel.TargetId);
                _logger.LogWarning("可用的节点ID（前10个）: {NodeIds}", string.Join(", ", graphNodeIds.Take(10)));
                _logger.LogWarning("完整的关系对象: {Relationship}", Newtonsoft.Json.JsonConvert.SerializeObject(rel));
                continue;
            }

            var edge = new KnowledgeGraphEdge
            {
                EdgeId = Guid.NewGuid().ToString(),
                SourceNodeId = rel.SourceId,
                TargetNodeId = rel.TargetId,
                Type = ParseEdgeType(rel.Type),
                Weight = Math.Clamp(rel.Weight, 0.0f, 1.0f),
                Description = rel.Description
            };

            edges.Add(edge);
            _logger.LogDebug("成功添加关系: {SourceId} -> {TargetId}, 类型: {Type}, 权重: {Weight}",
                edge.SourceNodeId, edge.TargetNodeId, edge.Type, edge.Weight);
        }

        _logger.LogInformation("批次处理完成，共 {RelationshipCount} 条关系，有效 {ValidEdgeCount} 条",
            response.Relationships.Count, edges.Count);

        // 如果没有提取到任何关系，抛出错误而不是使用降级策略
        if (edges.Count == 0)
        {
            var errorMsg = $"批次 {batch.Count} 个知识点未提取到任何有效关系。LLM 返回了 {response.Relationships.Count} 条关系，但所有关系的节点 ID 都无效。";
            _logger.LogError(errorMsg);
            _logger.LogError("批次中的知识点 ID: {KpIds}", string.Join(", ", batch.Select(kp => kp.KpId)));
            _logger.LogError("图谱中的节点 ID（前10个）: {NodeIds}", string.Join(", ", graphNodeIds.Take(10)));
            _logger.LogError("LLM 返回的关系数量: {Count}", response.Relationships.Count);

            throw new InvalidOperationException(errorMsg);
        }

        return edges;
    }

    private KnowledgeGraphEdgeType ParseEdgeType(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "related" => KnowledgeGraphEdgeType.Related,
            "dependson" => KnowledgeGraphEdgeType.DependsOn,
            "contains" => KnowledgeGraphEdgeType.Contains,
            "contrast" => KnowledgeGraphEdgeType.Contrast,
            "exampleof" => KnowledgeGraphEdgeType.ExampleOf,
            "extends" => KnowledgeGraphEdgeType.Extends,
            "implements" => KnowledgeGraphEdgeType.Implements,
            _ => KnowledgeGraphEdgeType.Related
        };
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
}
