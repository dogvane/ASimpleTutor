using ASimpleTutor.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ASimpleTutor.Core.Services;

/// <summary>
/// 知识图谱持久化存储服务
/// </summary>
public class KnowledgeGraphStore
{
    private readonly string _storePath;
    private readonly ILogger<KnowledgeGraphStore> _logger;

    public KnowledgeGraphStore(ILogger<KnowledgeGraphStore> logger, string baseDataDirectory)
    {
        _logger = logger;

        // 检查参数是否为空
        if (string.IsNullOrEmpty(baseDataDirectory))
        {
            // 默认使用项目根目录下的 datas 文件夹
            baseDataDirectory = "datas";
            _logger.LogWarning("存储目录参数为空，使用默认目录: {Directory}", baseDataDirectory);
        }

        try
        {
            // 保存目录：datas/../saves/ 即相对于 datas 目录的 saves 文件夹
            var combinedPath = Path.Combine(baseDataDirectory, "..", "saves");
            _storePath = Path.GetFullPath(combinedPath);

            if (!Directory.Exists(_storePath))
            {
                Directory.CreateDirectory(_storePath);
            }

            _logger.LogInformation("知识图谱存储目录: {Path}", _storePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "路径处理失败，baseDataDirectory: '{BaseDataDirectory}'", baseDataDirectory);
            // 即使路径处理失败，也设置一个默认路径，防止应用程序崩溃
            _storePath = "saves";
            if (!Directory.Exists(_storePath))
            {
                Directory.CreateDirectory(_storePath);
            }
        }
    }

    /// <summary>
    /// 异步保存知识图谱到文件
    /// </summary>
    public async Task SaveAsync(KnowledgeGraph graph, CancellationToken cancellationToken = default)
    {
        if (graph == null)
        {
            _logger.LogWarning("尝试保存空知识图谱");
            return;
        }

        var directory = Path.Combine(_storePath, graph.BookHubId);
        Directory.CreateDirectory(directory);

        _logger.LogInformation("开始保存知识图谱: {BookHubId}, GraphId: {GraphId}, 节点数: {NodeCount}, 边数: {EdgeCount}",
            graph.BookHubId, graph.GraphId, graph.Nodes.Count, graph.Edges.Count);

        try
        {
            var filePath = Path.Combine(directory, "knowledge-graph.json");

            // 更新时间戳
            graph.UpdatedAt = DateTime.UtcNow;

            var json = JsonConvert.SerializeObject(graph, Formatting.Indented);
            await File.WriteAllTextAsync(filePath, json, cancellationToken);

            _logger.LogInformation("知识图谱保存完成: {BookHubId}, 节点: {NodeCount}, 边: {EdgeCount}",
                graph.BookHubId, graph.Nodes.Count, graph.Edges.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存知识图谱失败: {BookHubId}", graph.BookHubId);
            throw;
        }
    }

    /// <summary>
    /// 异步从文件加载知识图谱
    /// </summary>
    public async Task<KnowledgeGraph?> LoadAsync(string bookHubId, CancellationToken cancellationToken = default)
    {
        var directory = Path.Combine(_storePath, bookHubId);
        var filePath = Path.Combine(directory, "knowledge-graph.json");

        if (!File.Exists(filePath))
        {
            _logger.LogInformation("知识图谱文件不存在: {FilePath}", filePath);
            return null;
        }

        _logger.LogInformation("开始加载知识图谱: {BookHubId}", bookHubId);

        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var graph = JsonConvert.DeserializeObject<KnowledgeGraph>(json);

            if (graph == null)
            {
                _logger.LogWarning("知识图谱反序列化失败: {BookHubId}", bookHubId);
                return null;
            }

            _logger.LogInformation("知识图谱加载完成: {BookHubId}, GraphId: {GraphId}, 节点: {NodeCount}, 边: {EdgeCount}",
                bookHubId, graph.GraphId, graph.Nodes.Count, graph.Edges.Count);

            return graph;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载知识图谱失败: {BookHubId}", bookHubId);
            return null;
        }
    }

    /// <summary>
    /// 保存知识图谱到文件（同步方法，保持向后兼容）
    /// </summary>
    public void Save(KnowledgeGraph graph)
    {
        SaveAsync(graph).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 从文件加载知识图谱（同步方法，保持向后兼容）
    /// </summary>
    public KnowledgeGraph? Load(string bookHubId)
    {
        return LoadAsync(bookHubId).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 检查是否存在已保存的知识图谱
    /// </summary>
    public bool Exists(string bookHubId)
    {
        var directory = Path.Combine(_storePath, bookHubId);
        var graphFile = Path.Combine(directory, "knowledge-graph.json");
        return File.Exists(graphFile);
    }

    /// <summary>
    /// 删除保存的知识图谱
    /// </summary>
    public bool Delete(string bookHubId)
    {
        var directory = Path.Combine(_storePath, bookHubId);
        var graphFile = Path.Combine(directory, "knowledge-graph.json");

        if (File.Exists(graphFile))
        {
            File.Delete(graphFile);
            _logger.LogInformation("知识图谱已删除: {BookHubId}", bookHubId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 获取所有已保存知识图谱的书籍中心 ID
    /// </summary>
    public List<string> GetAllSavedBookHubIds()
    {
        if (!Directory.Exists(_storePath))
        {
            return new List<string>();
        }

        return Directory.GetDirectories(_storePath)
            .Where(dir => File.Exists(Path.Combine(dir, "knowledge-graph.json")))
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .Cast<string>()
            .ToList();
    }
}
