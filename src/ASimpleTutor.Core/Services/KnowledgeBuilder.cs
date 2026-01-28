using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ASimpleTutor.Core.Services;

/// <summary>
/// 知识体系构建服务
/// </summary>
public class KnowledgeBuilder : IKnowledgeBuilder
{
    private readonly IScannerService _scannerService;
    private readonly ISimpleRagService _ragService;
    private readonly ISourceTracker _sourceTracker;
    private readonly ILLMService _llmService;
    private readonly ILogger<KnowledgeBuilder> _logger;

    public KnowledgeBuilder(
        IScannerService scannerService,
        ISimpleRagService ragService,
        ISourceTracker sourceTracker,
        ILLMService llmService,
        ILogger<KnowledgeBuilder> logger)
    {
        _scannerService = scannerService;
        _ragService = ragService;
        _sourceTracker = sourceTracker;
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<KnowledgeSystem> BuildAsync(string bookRootId, string rootPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("开始构建知识体系: {BookRootId}", bookRootId);

        var knowledgeSystem = new KnowledgeSystem
        {
            BookRootId = bookRootId
        };

        List<Document> documents = new();

        try
        {
            // 1. 扫描文档
            _logger.LogInformation("扫描文档目录: {RootPath}", rootPath);
            documents = await _scannerService.ScanAsync(rootPath, cancellationToken);

            if (documents.Count == 0)
            {
                _logger.LogWarning("未找到任何 Markdown 文档");
                return knowledgeSystem;
            }

            // 2. 插入 RAG 并跟踪原文
            _logger.LogInformation("插入文档到 RAG");
            foreach (var doc in documents)
            {
                doc.BookRootId = bookRootId;
                var content = ReconstructDocumentContent(doc);
                await _ragService.InsertAsync(doc.DocId, content, new Dictionary<string, object>
                {
                    ["filePath"] = doc.Path,
                    ["title"] = doc.Title
                });

                // 同时跟踪原文片段
                await _sourceTracker.TrackAsync(doc.DocId, content, new Dictionary<string, object>
                {
                    ["filePath"] = doc.Path,
                    ["title"] = doc.Title
                });
            }

            // 3. 调用 LLM 提取知识点
            _logger.LogInformation("调用 LLM 提取知识点");
            var knowledgePoints = await ExtractKnowledgePointsAsync(documents, cancellationToken);
            knowledgeSystem.KnowledgePoints = knowledgePoints;

            // 4. 构建知识树
            _logger.LogInformation("构建知识树");
            knowledgeSystem.Tree = BuildKnowledgeTree(knowledgePoints);

            // 5. 收集所有原文片段
            _logger.LogInformation("收集原文片段");
            foreach (var kp in knowledgePoints)
            {
                foreach (var snippetId in kp.SnippetIds)
                {
                    var snippet = _sourceTracker.GetSource(snippetId);
                    if (snippet != null && !knowledgeSystem.Snippets.ContainsKey(snippetId))
                    {
                        knowledgeSystem.Snippets[snippetId] = snippet;
                    }
                }
            }

            _logger.LogInformation("知识体系构建完成，共 {Count} 个知识点", knowledgePoints.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "知识体系构建失败");
            // 降级：返回按文件/标题的目录树
            knowledgeSystem = CreateFallbackKnowledgeSystem(bookRootId, documents);
        }

        return knowledgeSystem;
    }

    private async Task<List<KnowledgePoint>> ExtractKnowledgePointsAsync(
        List<Document> documents,
        CancellationToken cancellationToken)
    {
        // 获取文档的 chunk ID 列表
        var documentChunkIds = documents.ToDictionary(
            d => d.DocId,
            d => d.DocId + "_chunk_0"
        );

        // 构建 chunk ID 列表说明
        var chunkIdList = new System.Text.StringBuilder();
        chunkIdList.AppendLine("可用原文片段 ID（chunk_id）：");
        if (documentChunkIds.Count > 0)
        {
            foreach (var kv in documentChunkIds)
            {
                chunkIdList.AppendLine("  - " + kv.Key + " -> " + kv.Value);
            }
        }
        else
        {
            chunkIdList.AppendLine("  - {docId}_chunk_0 (默认)");
        }

        var systemPrompt = @"你是一个知识提取专家。你的任务是从文档中提取可学习的知识点。

请以 JSON 格式输出，结构如下：
{
  ""schema_version"": ""1.0"",
  ""knowledge_points"": [
    {
      ""kp_id"": ""唯一的知识点ID"",
      ""title"": ""知识点标题"",
      ""aliases"": [""别名1"", ""别名2""],
      ""chapter_path"": [""章节1"", ""章节2""],
      ""importance"": 0.0-1.0,
      ""snippet_ids"": [""chunk_id1"", ""chunk_id2""],
      ""summary"": ""一句话总结""
    }
  ]
}

" + chunkIdList.ToString() + @"
知识点识别规则：
1. 识别文档中的概念、术语、规则、步骤、API 等
2. 每个知识点必须至少关联一个原文片段（使用上面的 chunk_id）
3. importance 反映知识点的重要程度（核心概念=0.8+, 细节=0.5, 边缘=0.3）
4. 尽量使用原文中的表述作为标题
5. snippet_ids 必须使用上述可用的 chunk_id 格式";

        var documentContent = string.Join("\n\n", documents.Select(d =>
            $"# {d.Title}\n{ReconstructDocumentContent(d)}"));

        _logger.LogInformation("文档总字符数: {CharCount}", documentContent.Length);
        _logger.LogInformation("开始调用 LLM 提取知识点...");

        try
        {
            var response = await _llmService.ChatJsonAsync<KnowledgePointsResponse>(
                systemPrompt,
                $"请分析以下文档并提取知识点：\n\n{documentContent}",
                cancellationToken);

            _logger.LogInformation("LLM 调用完成，提取到 {Count} 个知识点", response?.KnowledgePoints?.Count ?? 0);

            // 将 DTO 转换为标准的 KnowledgePoint
            var kpList = response?.KnowledgePoints?
                .Select((kp, index) =>
                {
                    var kpModel = kp.ToKnowledgePoint();
                    kpModel.KpId = $"kp_{index:D4}";
                    kpModel.BookRootId = documents.FirstOrDefault()?.BookRootId ?? string.Empty;
                    return kpModel;
                })
                .ToList() ?? new List<KnowledgePoint>();

            return kpList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提取知识点失败");
            return new List<KnowledgePoint>();
        }
    }

    private string ReconstructDocumentContent(Document doc)
    {
        var sb = new System.Text.StringBuilder();

        foreach (var section in doc.Sections)
        {
            if (section.HeadingPath.Count > 0)
            {
                sb.AppendLine($"# {string.Join(" > ", section.HeadingPath)}");
            }

            foreach (var para in section.Paragraphs)
            {
                sb.AppendLine(para.Content);
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static KnowledgeTreeNode BuildKnowledgeTree(List<KnowledgePoint> knowledgePoints)
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

            // 沿着章节路径导航
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

            // 在章节节点下添加知识点
            current.KnowledgePoint = kp;
        }

        return root;
    }

    private KnowledgeSystem CreateFallbackKnowledgeSystem(string bookRootId, List<Document> documents)
    {
        var knowledgeSystem = new KnowledgeSystem
        {
            BookRootId = bookRootId
        };

        // 从文档标题创建临时知识点
        var id = 0;
        foreach (var doc in documents)
        {
            var kp = new KnowledgePoint
            {
                KpId = $"kp_{id++:D4}",
                BookRootId = bookRootId,
                Title = doc.Title,
                ChapterPath = new List<string> { doc.Title },
                Importance = 0.5f,
                SnippetIds = new List<string> { $"{doc.DocId}_chunk_0" }
            };
            knowledgeSystem.KnowledgePoints.Add(kp);
        }

        knowledgeSystem.Tree = BuildKnowledgeTree(knowledgeSystem.KnowledgePoints);

        return knowledgeSystem;
    }
}

/// <summary>
/// LLM 响应数据结构（用于接收 LLM 返回的 JSON）
/// </summary>
public class KnowledgePointsResponse
{
    [JsonProperty("schema_version")]
    public string? SchemaVersion { get; set; }

    [JsonProperty("knowledge_points")]
    public List<KnowledgePointDto> KnowledgePoints { get; set; } = new();
}

/// <summary>
/// 知识点 DTO（从 LLM 接收）
/// </summary>
public class KnowledgePointDto
{
    [JsonProperty("kp_id")]
    public string? KpId { get; set; }

    [JsonProperty("title")]
    public string? Title { get; set; }

    [JsonProperty("aliases")]
    public List<string>? Aliases { get; set; }

    [JsonProperty("chapter_path")]
    public List<string>? ChapterPath { get; set; } // LLM 返回数组格式

    [JsonProperty("importance")]
    public float Importance { get; set; }

    [JsonProperty("snippet_ids")]
    public List<string>? SnippetIds { get; set; }

    [JsonProperty("summary")]
    public string? Summary { get; set; }

    /// <summary>
    /// 转换为标准的 KnowledgePoint
    /// </summary>
    public KnowledgePoint ToKnowledgePoint()
    {
        return new KnowledgePoint
        {
            KpId = KpId ?? string.Empty,
            Title = Title ?? string.Empty,
            Aliases = Aliases ?? new List<string>(),
            ChapterPath = ChapterPath ?? new List<string>(),
            Importance = Importance,
            SnippetIds = SnippetIds ?? new List<string>()
        };
    }
}
