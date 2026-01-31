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

    public KnowledgeSystemStore(ILogger<KnowledgeSystemStore> logger, string? storePath = null)
    {
        _logger = logger;
        _storePath = storePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");

        if (!Directory.Exists(_storePath))
        {
            Directory.CreateDirectory(_storePath);
        }
    }

    /// <summary>
    /// 保存知识系统到文件
    /// </summary>
    public void Save(KnowledgeSystem knowledgeSystem)
    {
        if (knowledgeSystem == null)
        {
            _logger.LogWarning("尝试保存空知识系统");
            return;
        }

        var filePath = GetFilePath(knowledgeSystem.BookRootId);

        try
        {
            var json = SerializeKnowledgeSystem(knowledgeSystem);
            File.WriteAllText(filePath, json);
            _logger.LogInformation("知识系统已保存: {BookRootId} -> {FilePath}", knowledgeSystem.BookRootId, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存知识系统失败: {BookRootId}", knowledgeSystem.BookRootId);
            throw;
        }
    }

    /// <summary>
    /// 从文件加载知识系统
    /// </summary>
    public KnowledgeSystem? Load(string bookRootId)
    {
        var filePath = GetFilePath(bookRootId);

        if (!File.Exists(filePath))
        {
            _logger.LogInformation("未找到知识系统文件: {FilePath}", filePath);
            return null;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var knowledgeSystem = DeserializeKnowledgeSystem(json);
            _logger.LogInformation("知识系统已加载: {BookRootId}，共 {Count} 个知识点",
                bookRootId, knowledgeSystem?.KnowledgePoints.Count ?? 0);
            return knowledgeSystem;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载知识系统失败: {BookRootId}", bookRootId);
            return null;
        }
    }

    /// <summary>
    /// 检查是否存在已保存的知识系统
    /// </summary>
    public bool Exists(string bookRootId)
    {
        var filePath = GetFilePath(bookRootId);
        return File.Exists(filePath);
    }

    /// <summary>
    /// 删除保存的知识系统
    /// </summary>
    public bool Delete(string bookRootId)
    {
        var filePath = GetFilePath(bookRootId);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.LogInformation("知识系统已删除: {BookRootId}", bookRootId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 获取所有已保存的书籍目录 ID
    /// </summary>
    public List<string> GetAllSavedBookRootIds()
    {
        if (!Directory.Exists(_storePath))
        {
            return new List<string>();
        }

        return Directory.GetFiles(_storePath, "knowledge_*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Select(name => name?.Replace("knowledge_", "") ?? string.Empty)
            .Where(id => !string.IsNullOrEmpty(id))
            .ToList();
    }

    private string GetFilePath(string bookRootId)
    {
        // 清理文件名中的非法字符
        var safeId = string.Join("_", bookRootId.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_storePath, $"knowledge_{safeId}.json");
    }

    private string SerializeKnowledgeSystem(KnowledgeSystem ks)
    {
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };

        return JsonConvert.SerializeObject(ks, settings);
    }

    private KnowledgeSystem? DeserializeKnowledgeSystem(string json)
    {
        // 自定义反序列化，处理循环引用和版本兼容
        var obj = JObject.Parse(json);

        var knowledgeSystem = new KnowledgeSystem
        {
            BookRootId = obj.Value<string>("bookRootId") ?? string.Empty
        };

        // 反序列化知识点列表
        var kpArray = obj["knowledgePoints"] as JArray;
        if (kpArray != null)
        {
            foreach (var kpToken in kpArray)
            {
                var kp = ParseKnowledgePoint(kpToken);
                if (kp != null)
                {
                    knowledgeSystem.KnowledgePoints.Add(kp);
                }
            }
        }

        // 反序列化原文片段字典
        var snippetsObj = obj["snippets"] as JObject;
        if (snippetsObj != null)
        {
            foreach (var prop in snippetsObj.Properties())
            {
                var snippet = ParseSourceSnippet(prop.Value);
                if (snippet != null)
                {
                    knowledgeSystem.Snippets[prop.Name] = snippet;
                }
            }
        }

        // 构建知识树
        knowledgeSystem.Tree = BuildTreeFromKnowledgePoints(knowledgeSystem.KnowledgePoints);

        return knowledgeSystem;
    }

    private KnowledgePoint? ParseKnowledgePoint(JToken token)
    {
        try
        {
            return new KnowledgePoint
            {
                KpId = token.Value<string>("kpId") ?? string.Empty,
                BookRootId = token.Value<string>("bookRootId") ?? string.Empty,
                Title = token.Value<string>("title") ?? string.Empty,
                Aliases = token["aliases"]?.ToObject<List<string>>() ?? new List<string>(),
                ChapterPath = token["chapterPath"]?.ToObject<List<string>>() ?? new List<string>(),
                Importance = token.Value<float?>("importance") ?? 0.5f,
                SnippetIds = token["snippetIds"]?.ToObject<List<string>>() ?? new List<string>(),
                Summary = ParseSummary(token["summary"]),
                Levels = ParseContentLevels(token["levels"])
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析知识点失败");
            return null;
        }
    }

    private Summary? ParseSummary(JToken? token)
    {
        if (token == null) return null;

        try
        {
            return new Summary
            {
                Definition = token.Value<string>("definition") ?? string.Empty,
                KeyPoints = token["keyPoints"]?.ToObject<List<string>>() ?? new List<string>(),
                Pitfalls = token["pitfalls"]?.ToObject<List<string>>() ?? new List<string>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析 Summary 失败");
            return null;
        }
    }

    private List<ContentLevel> ParseContentLevels(JToken? token)
    {
        if (token == null) return new List<ContentLevel>();

        try
        {
            return token.ToObject<List<ContentLevel>>() ?? new List<ContentLevel>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析 Levels 失败");
            return new List<ContentLevel>();
        }
    }

    private SourceSnippet? ParseSourceSnippet(JToken? token)
    {
        if (token == null) return null;

        try
        {
            return new SourceSnippet
            {
                SnippetId = token.Value<string>("snippetId") ?? string.Empty,
                BookRootId = token.Value<string>("bookRootId") ?? string.Empty,
                DocId = token.Value<string>("docId") ?? string.Empty,
                Content = token.Value<string>("content") ?? string.Empty,
                FilePath = token.Value<string>("filePath") ?? string.Empty,
                HeadingPath = token["headingPath"]?.ToObject<List<string>>() ?? new List<string>(),
                StartLine = token.Value<int?>("startLine") ?? token.Value<int?>("lineStart") ?? 0,
                EndLine = token.Value<int?>("endLine") ?? token.Value<int?>("lineEnd") ?? 0,
                ChunkId = token.Value<string>("chunkId")
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析原文片段失败");
            return null;
        }
    }

    private KnowledgeTreeNode BuildTreeFromKnowledgePoints(List<KnowledgePoint> knowledgePoints)
    {
        var root = new KnowledgeTreeNode
        {
            Id = "root",
            Title = "根",
            HeadingPath = new List<string>()
        };

        foreach (var kp in knowledgePoints)
        {
            var current = root;

            foreach (var chapter in kp.ChapterPath)
            {
                var existingChild = current.Children.FirstOrDefault(c => c.Title == chapter);
                if (existingChild == null)
                {
                    var newNode = new KnowledgeTreeNode
                    {
                        Id = $"{current.Id}_{chapter}",
                        Title = chapter,
                        HeadingPath = new List<string>(current.HeadingPath) { chapter }
                    };
                    current.Children.Add(newNode);
                    current = newNode;
                }
                else
                {
                    current = existingChild;
                }
            }

            current.KnowledgePoint = kp;
        }

        return root;
    }
}
