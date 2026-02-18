using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace ASimpleTutor.Api.Controllers;

/// <summary>
/// 知识图谱相关接口
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class KnowledgeGraphController : ControllerBase
{
    private readonly IKnowledgeGraphBuilder _knowledgeGraphBuilder;
    private readonly ILogger<KnowledgeGraphController> _logger;

    public KnowledgeGraphController(
        IKnowledgeGraphBuilder knowledgeGraphBuilder,
        ILogger<KnowledgeGraphController> logger)
    {
        _knowledgeGraphBuilder = knowledgeGraphBuilder;
        _logger = logger;
    }

    /// <summary>
    /// 根据书籍中心 ID 构建知识图谱
    /// </summary>
    /// <param name="bookHubId">书籍中心 ID</param>
    /// <param name="options">知识图谱构建选项</param>
    /// <returns>知识图谱</returns>
    [HttpPost("build")]
    public async Task<ActionResult<KnowledgeGraph>> BuildGraphAsync(
        [FromBody] BuildKnowledgeGraphRequest request)
    {
        try
        {
            _logger.LogInformation("Building knowledge graph for book hub id: {BookHubId}", request.BookHubId);

            var graph = await _knowledgeGraphBuilder.BuildAsync(request.BookHubId, request.Options);

            _logger.LogInformation("Successfully built knowledge graph with {NodeCount} nodes and {EdgeCount} edges",
                graph.Nodes.Count, graph.Edges.Count);

            return Ok(graph);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build knowledge graph for book hub id: {BookHubId}", request.BookHubId);
            return StatusCode(500, $"Failed to build knowledge graph: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取知识图谱的子图
    /// </summary>
    /// <param name="request">子图请求参数</param>
    /// <returns>知识图谱子图</returns>
    [HttpPost("subgraph")]
    public ActionResult<KnowledgeGraph> GetSubgraphAsync(
        [FromBody] GetSubgraphRequest request)
    {
        try
        {
            _logger.LogInformation("Getting subgraph for book hub id: {BookHubId}, root node: {RootNodeId}, depth: {Depth}",
                request.BookHubId, request.RootNodeId, request.Depth);

            var graph = _knowledgeGraphBuilder.Build(request.KnowledgePoints, request.Options);
            var subgraph = _knowledgeGraphBuilder.GetSubgraph(graph, request.RootNodeId, request.Depth);

            _logger.LogInformation("Successfully retrieved subgraph with {NodeCount} nodes and {EdgeCount} edges",
                subgraph.Nodes.Count, subgraph.Edges.Count);

            return Ok(subgraph);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get subgraph for book hub id: {BookHubId}", request.BookHubId);
            return StatusCode(500, $"Failed to get subgraph: {ex.Message}");
        }
    }

    /// <summary>
    /// 搜索知识图谱节点
    /// </summary>
    /// <param name="request">搜索请求参数</param>
    /// <returns>搜索结果</returns>
    [HttpPost("search")]
    public ActionResult<KnowledgeGraphQueryResult> SearchNodesAsync(
        [FromBody] SearchNodesRequest request)
    {
        try
        {
            _logger.LogInformation("Searching knowledge graph nodes for query: {Query}", request.Query);

            var graph = _knowledgeGraphBuilder.Build(request.KnowledgePoints, request.Options);
            var result = _knowledgeGraphBuilder.SearchNodes(graph, request.Query, request.MaxResults);

            _logger.LogInformation("Search completed. Found {NodeCount} nodes and {EdgeCount} edges",
                result.TotalNodes, result.TotalEdges);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search knowledge graph for query: {Query}", request.Query);
            return StatusCode(500, $"Failed to search knowledge graph: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取节点的邻居节点
    /// </summary>
    /// <param name="request">邻居请求参数</param>
    /// <returns>邻居节点和关联边</returns>
    [HttpPost("neighbors")]
    public ActionResult<KnowledgeGraphQueryResult> GetNeighborsAsync(
        [FromBody] GetNeighborsRequest request)
    {
        try
        {
            _logger.LogInformation("Getting neighbors for node: {NodeId}", request.NodeId);

            var graph = _knowledgeGraphBuilder.Build(request.KnowledgePoints, request.Options);
            var result = _knowledgeGraphBuilder.GetNeighbors(graph, request.NodeId, request.MaxNeighbors);

            _logger.LogInformation("Found {NodeCount} neighbors and {EdgeCount} edges for node: {NodeId}",
                result.TotalNodes, result.TotalEdges, request.NodeId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get neighbors for node: {NodeId}", request.NodeId);
            return StatusCode(500, $"Failed to get neighbors: {ex.Message}");
        }
    }
}

#region Request DTOs

/// <summary>
/// 构建知识图谱请求
/// </summary>
public class BuildKnowledgeGraphRequest
{
    /// <summary>
    /// 书籍中心 ID
    /// </summary>
    public string BookHubId { get; set; } = string.Empty;

    /// <summary>
    /// 知识图谱构建选项
    /// </summary>
    public KnowledgeGraphBuildOptions? Options { get; set; }
}

/// <summary>
/// 获取子图请求
/// </summary>
public class GetSubgraphRequest
{
    /// <summary>
    /// 书籍中心 ID
    /// </summary>
    public string BookHubId { get; set; } = string.Empty;

    /// <summary>
    /// 根节点 ID
    /// </summary>
    public string RootNodeId { get; set; } = string.Empty;

    /// <summary>
    /// 子图深度
    /// </summary>
    public int Depth { get; set; } = 2;

    /// <summary>
    /// 知识点列表
    /// </summary>
    public List<KnowledgePoint> KnowledgePoints { get; set; } = new();

    /// <summary>
    /// 知识图谱构建选项
    /// </summary>
    public KnowledgeGraphBuildOptions? Options { get; set; }
}

/// <summary>
/// 搜索节点请求
/// </summary>
public class SearchNodesRequest
{
    /// <summary>
    /// 搜索关键词
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// 最大结果数
    /// </summary>
    public int MaxResults { get; set; } = 10;

    /// <summary>
    /// 知识点列表
    /// </summary>
    public List<KnowledgePoint> KnowledgePoints { get; set; } = new();

    /// <summary>
    /// 知识图谱构建选项
    /// </summary>
    public KnowledgeGraphBuildOptions? Options { get; set; }
}

/// <summary>
/// 获取邻居节点请求
/// </summary>
public class GetNeighborsRequest
{
    /// <summary>
    /// 节点 ID
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// 最大邻居数
    /// </summary>
    public int MaxNeighbors { get; set; } = 10;

    /// <summary>
    /// 知识点列表
    /// </summary>
    public List<KnowledgePoint> KnowledgePoints { get; set; } = new();

    /// <summary>
    /// 知识图谱构建选项
    /// </summary>
    public KnowledgeGraphBuildOptions? Options { get; set; }
}

#endregion
