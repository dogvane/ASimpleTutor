using ASimpleTutor.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ASimpleTutor.Core.Services;

/// <summary>
/// 知识系统持久化存储服务
/// </summary>
public class KnowledgeSystemStore
{
    private readonly string _storePath;
    private readonly ILogger<KnowledgeSystemStore> _logger;

    public KnowledgeSystemStore(ILogger<KnowledgeSystemStore> logger, string baseDataDirectory)
    {
        _logger = logger;
        // 保存目录：datas/../saves/ 即相对于 datas 目录的 saves 文件夹
        _storePath = Path.GetFullPath(Path.Combine(baseDataDirectory, "..", "saves"));

        if (!Directory.Exists(_storePath))
        {
            Directory.CreateDirectory(_storePath);
        }

        _logger.LogInformation("知识系统存储目录: {Path}", _storePath);
    }

    /// <summary>
    /// 异步保存知识系统到文件
    /// </summary>
    public async Task SaveAsync(KnowledgeSystem knowledgeSystem, List<Document>? documents = null, CancellationToken cancellationToken = default)
    {
        if (knowledgeSystem == null)
        {
            _logger.LogWarning("尝试保存空知识系统");
            return;
        }

        var directory = Path.Combine(_storePath, knowledgeSystem.BookHubId);
        Directory.CreateDirectory(directory);

        _logger.LogInformation("开始保存知识系统: {BookHubId}", knowledgeSystem.BookHubId);

        try
        {
            // 并行保存知识体系、原文片段和文档章节信息
            var saveKnowledgeTask = SaveKnowledgeSystemAsync(knowledgeSystem, directory, cancellationToken);
            var saveSnippetsTask = SaveSnippetsAsync(knowledgeSystem.Snippets, directory, cancellationToken);
            var saveDocumentsTask = SaveDocumentsAsync(documents, knowledgeSystem.BookHubId, directory, cancellationToken);

            await Task.WhenAll(saveKnowledgeTask, saveSnippetsTask, saveDocumentsTask);

            _logger.LogInformation("知识系统保存完成: {BookHubId}, 知识点: {KpCount}, 原文片段: {SnippetCount}, 文档: {DocCount}",
                knowledgeSystem.BookHubId,
                knowledgeSystem.KnowledgePoints.Count,
                knowledgeSystem.Snippets.Count,
                documents?.Count ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存知识系统失败: {BookHubId}", knowledgeSystem.BookHubId);
            throw;
        }
    }

    /// <summary>
    /// 异步从文件加载知识系统
    /// </summary>
    public async Task<(KnowledgeSystem? KnowledgeSystem, List<Document>? Documents)> LoadAsync(string bookHubId, CancellationToken cancellationToken = default)
    {
        var directory = Path.Combine(_storePath, bookHubId);

        if (!Directory.Exists(directory))
        {
            _logger.LogInformation("知识系统保存目录不存在: {Directory}", directory);
            return (null, null);
        }

        _logger.LogInformation("开始加载知识系统: {BookHubId}", bookHubId);

        try
        {
            // 并行加载知识体系、原文片段和文档章节信息
            var loadKnowledgeTask = LoadKnowledgeSystemAsync(directory, cancellationToken);
            var loadSnippetsTask = LoadSnippetsAsync(directory, cancellationToken);
            var loadDocumentsTask = LoadDocumentsAsync(directory, cancellationToken);

            await Task.WhenAll(loadKnowledgeTask, loadSnippetsTask, loadDocumentsTask);

            var knowledgeSystem = loadKnowledgeTask.Result;
            var snippets = loadSnippetsTask.Result;
            var documents = loadDocumentsTask.Result;

            if (knowledgeSystem == null)
            {
                _logger.LogWarning("知识系统文件加载失败: {BookHubId}", bookHubId);
                return (null, documents);
            }

            // 合并原文片段
            if (snippets != null)
            {
                knowledgeSystem.Snippets = snippets;
            }

            // 重建知识树
            knowledgeSystem.Tree = ASimpleTutor.Core.Services.KnowledgeBuilder.BuildKnowledgeTree(knowledgeSystem.KnowledgePoints);

            _logger.LogInformation("知识系统加载完成: {BookHubId}, 知识点: {KpCount}, 原文片段: {SnippetCount}, 文档: {DocCount}",
                bookHubId,
                knowledgeSystem.KnowledgePoints.Count,
                knowledgeSystem.Snippets.Count,
                documents?.Count ?? 0);

            return (knowledgeSystem, documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载知识系统失败: {BookHubId}", bookHubId);
            return (null, null);
        }
    }

    /// <summary>
    /// 保存知识系统到文件（同步方法，保持向后兼容）
    /// </summary>
    public void Save(KnowledgeSystem knowledgeSystem, List<Document>? documents = null)
    {
        SaveAsync(knowledgeSystem, documents).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 从文件加载知识系统（同步方法，保持向后兼容）
    /// </summary>
    public KnowledgeSystem? Load(string bookHubId)
    {
        var result = LoadAsync(bookHubId).GetAwaiter().GetResult();
        return result.KnowledgeSystem;
    }

    /// <summary>
    /// 从文件加载文档章节信息（同步方法）
    /// </summary>
    public List<Document>? LoadDocuments(string bookHubId)
    {
        var result = LoadAsync(bookHubId).GetAwaiter().GetResult();
        return result.Documents;
    }

    /// <summary>
    /// 检查是否存在已保存的知识系统
    /// </summary>
    public bool Exists(string bookHubId)
    {
        var directory = Path.Combine(_storePath, bookHubId);
        var knowledgeFile = Path.Combine(directory, "knowledge-system.json");
        var documentsFile = Path.Combine(directory, "documents.json");
        return File.Exists(knowledgeFile) && File.Exists(documentsFile);
    }

    /// <summary>
    /// 检查存储文件的完整性
    /// </summary>
    public bool CheckStorageIntegrity(string bookHubId)
    {
        var directory = Path.Combine(_storePath, bookHubId);
        
        if (!Directory.Exists(directory))
        {
            return false;
        }

        var knowledgeFile = Path.Combine(directory, "knowledge-system.json");
        var documentsFile = Path.Combine(directory, "documents.json");
        var snippetsFile = Path.Combine(directory, "snippets.json");

        // 检查核心文件是否存在
        return File.Exists(knowledgeFile) && File.Exists(documentsFile);
    }

    /// <summary>
    /// 删除保存的知识系统
    /// </summary>
    public bool Delete(string bookHubId)
    {
        var directory = Path.Combine(_storePath, bookHubId);

        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
            _logger.LogInformation("知识系统已删除: {BookHubId}", bookHubId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 获取所有已保存的书籍中心 ID
    /// </summary>
    public List<string> GetAllSavedBookHubIds()
    {
        if (!Directory.Exists(_storePath))
        {
            return new List<string>();
        }

        return Directory.GetDirectories(_storePath)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .Cast<string>()
            .ToList();
    }

    private async Task SaveKnowledgeSystemAsync(KnowledgeSystem knowledgeSystem, string directory, CancellationToken cancellationToken)
    {
        // 创建保存模型（排除 Snippets，因为它们单独保存）
        var saveModel = new KnowledgeSystemSaveModel
        {
            BookHubId = knowledgeSystem.BookHubId,
            KnowledgePoints = knowledgeSystem.KnowledgePoints
        };

        var filePath = Path.Combine(directory, "knowledge-system.json");
        var json = JsonConvert.SerializeObject(saveModel, Formatting.Indented);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }

    private async Task SaveSnippetsAsync(Dictionary<string, SourceSnippet> snippets, string directory, CancellationToken cancellationToken)
    {
        if (snippets.Count == 0)
        {
            return;
        }

        var saveModel = new SnippetsSaveModel
        {
            Snippets = snippets
        };

        var filePath = Path.Combine(directory, "snippets.json");
        var json = JsonConvert.SerializeObject(saveModel, Formatting.Indented);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }

    private async Task<KnowledgeSystem?> LoadKnowledgeSystemAsync(string directory, CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(directory, "knowledge-system.json");

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("知识系统文件不存在: {Path}", filePath);
            return null;
        }

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        var saveModel = JsonConvert.DeserializeObject<KnowledgeSystemSaveModel>(json);

        if (saveModel == null)
        {
            return null;
        }

        return new KnowledgeSystem
        {
            BookHubId = saveModel.BookHubId,
            KnowledgePoints = saveModel.KnowledgePoints ?? new List<KnowledgePoint>(),
            Snippets = new Dictionary<string, SourceSnippet>(),
            Tree = null // 重建
        };
    }

    private async Task<Dictionary<string, SourceSnippet>?> LoadSnippetsAsync(string directory, CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(directory, "snippets.json");

        if (!File.Exists(filePath))
        {
            return new Dictionary<string, SourceSnippet>();
        }

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        var saveModel = JsonConvert.DeserializeObject<SnippetsSaveModel>(json);

        return saveModel?.Snippets ?? new Dictionary<string, SourceSnippet>();
    }

    private async Task SaveDocumentsAsync(List<Document>? documents, string bookHubId, string directory, CancellationToken cancellationToken)
    {
        if (documents == null || documents.Count == 0)
        {
            return;
        }

        var saveModel = new DocumentsSaveModel
        {
            BookHubId = bookHubId,
            Documents = documents
        };

        var filePath = Path.Combine(directory, "documents.json");
        var json = JsonConvert.SerializeObject(saveModel, Formatting.Indented);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }

    private async Task<List<Document>?> LoadDocumentsAsync(string directory, CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(directory, "documents.json");

        if (!File.Exists(filePath))
        {
            return new List<Document>();
        }

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        var saveModel = JsonConvert.DeserializeObject<DocumentsSaveModel>(json);

        return saveModel?.Documents ?? new List<Document>();
    }
}

/// <summary>
/// 知识系统保存模型
/// </summary>
internal class KnowledgeSystemSaveModel
{
    [JsonProperty("book_hub_id")]
    public string BookHubId { get; set; } = string.Empty;

    [JsonProperty("knowledge_points")]
    public List<KnowledgePoint> KnowledgePoints { get; set; } = new();

    [JsonProperty("saved_at")]
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 原文片段保存模型
/// </summary>
internal class SnippetsSaveModel
{
    [JsonProperty("snippets")]
    public Dictionary<string, SourceSnippet> Snippets { get; set; } = new();
}

/// <summary>
/// 文档章节信息保存模型
/// </summary>
internal class DocumentsSaveModel
{
    [JsonProperty("book_hub_id")]
    public string BookHubId { get; set; } = string.Empty;

    [JsonProperty("documents")]
    public List<Document> Documents { get; set; } = new();

    [JsonProperty("saved_at")]
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
}
