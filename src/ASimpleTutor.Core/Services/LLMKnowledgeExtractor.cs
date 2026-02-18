using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using ASimpleTutor.Core.Models.Dto;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.IO;

namespace ASimpleTutor.Core.Services;

/// <summary>
/// 基于 LLM 的知识点提取器
/// 负责从解析后的文档中提取知识点
/// </summary>
public class LLMKnowledgeExtractor : IKnowledgeExtractor
{
    private readonly ILLMService _llmService;
    private readonly ILogger<LLMKnowledgeExtractor> _logger;

    public LLMKnowledgeExtractor(ILLMService llmService, ILogger<LLMKnowledgeExtractor> logger)
    {
        _llmService = llmService;
        _logger = logger;
    }

    public async Task<List<KnowledgePoint>> ExtractAsync(List<Document> documents, CancellationToken cancellationToken = default)
    {
        var systemPrompt = @"你是一个知识提取专家。你的任务是从文档中提取可学习的知识点。

请以 JSON 格式输出，结构如下：
{
  ""schema_version"": ""1.0"",
  ""knowledge_points"": [
    {
      ""title"": ""知识点标题"",
      ""type"": ""concept|chapter|process|api|bestPractice"",
      ""aliases"": [""别名1"", ""别名2""] ,
      ""chapter_path"": [""章节1"", ""章节2"", ""章节3""] ,
      ""importance"": 0.0-1.0,
      ""summary"": ""一句话总结（必须填写）"",
    }
  ]
}

知识点类型说明（type 字段）：
- concept: 概念、定义、术语、理论
- chapter: 章节标题节点
- process: 流程、步骤、操作方法
- api: API、接口、方法签名
- bestPractice: 最佳实践、建议

知识点识别规则：
1. 识别文档中的概念、术语、规则、步骤、API 等
2. 每个知识点必须至少关联一个章节路径（使用完整的章节路径，如 ""第一章 概述 > 1.1 简介""）
3. importance 反映知识点的重要程度（核心概念=0.8+, 细节=0.5, 边缘=0.3）
4. 尽量使用原文中的表述作为标题
6. summary 必须填写，不能为空
7. type 字段必须填写且有效
9. chapter_path 必须反映知识点所在的完整章节层次结构

自检要求（生成后请检查）：
- knowledge_points 不能为空，如果确实没有知识点请返回空数组并说明原因
- 每个知识点的 type 必须是有效的类型之一
- 所有标题必须非空且唯一（如果重复请合并）";

        try
        {
            // 遍历所有文档的章节结构
            var allSections = new List<(Document doc, Section section)>();
            foreach (var doc in documents)
            {
                if (doc.Sections != null && doc.Sections.Count > 0)
                {
                    CollectAllSections(doc, doc.Sections, allSections);
                }
            }

            _logger.LogInformation("开始基于 Section 结构提取知识点，共 {SectionCount} 个 Section", allSections.Count);

            // 使用 ConcurrentBag 并发收集所有知识点（线程安全）
            var knowledgePointsBag = new ConcurrentBag<KnowledgePointDto>();

            // 使用 Parallel.ForEachAsync 并发处理所有 Section
            await Parallel.ForEachAsync(allSections,
                new ParallelOptions { CancellationToken = cancellationToken },
                async (item, cancellationToken) =>
                {
                    var (doc, section) = item;
                    var sectionPath = section.HeadingPath;

                    try
                    {
                        var sectionContent = await ReadSectionContentAsync(doc, section, cancellationToken);
                        _logger.LogDebug("处理 Section: {SectionPath}, 字符数: {CharCount}", string.Join(" > ", sectionPath), sectionContent.Length);

                        // 调用 LLM 提取当前 section 的知识点
                        var response = await _llmService.ChatJsonAsync<KnowledgePointsResponse>(
                            systemPrompt,
                            $"请分析以下章节内容并提取知识点：\n\n章节路径：{string.Join(" > ", sectionPath)}\n\n{sectionContent}",
                            cancellationToken);

                        if (response?.KnowledgePoints != null && response.KnowledgePoints.Count > 0)
                        {
                            _logger.LogDebug("Section {SectionPath} 提取到 {Count} 个知识点", string.Join(" > ", sectionPath), response.KnowledgePoints.Count);

                            // 为每个知识点添加正确的文档 ID 和章节路径
                            foreach (var kp in response.KnowledgePoints)
                            {
                                kp.SectionId = section.SectionId;
                                kp.DocId = doc.DocId;
                                // 如果 LLM 没有提供章节路径，使用 section 的路径
                                if (kp.ChapterPath == null || kp.ChapterPath.Count == 0)
                                {
                                    kp.ChapterPath = sectionPath;
                                }
                            }

                            // 将结果添加到并发集合
                            foreach (var kp in response.KnowledgePoints)
                            {
                                knowledgePointsBag.Add(kp);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "处理 Section 失败: {SectionPath}", string.Join(" > ", section.HeadingPath));
                    }
                });

            // 将 ConcurrentBag 转换为 List
            var knowledgePoints = knowledgePointsBag.ToList();

            _logger.LogInformation("基于 Section 结构提取完成，共提取到 {Count} 个知识点", knowledgePoints.Count);

            // 自检：验证响应数据
            if (knowledgePoints == null || knowledgePoints.Count == 0)
            {
                _logger.LogWarning("LLM 返回的知识点数为0，可能需要调整 prompt 或提供更完整的文档");
                return new List<KnowledgePoint>();
            }

            // 校验并清理知识点
            var validPoints = new List<KnowledgePointDto>();
            var seenTitles = new HashSet<string>();

            foreach (var kp in knowledgePoints)
            {
                // 校验知识点类型
                if (string.IsNullOrEmpty(kp.Type) || !IsValidKpType(kp.Type))
                {
                    _logger.LogWarning("知识点 '{Title}' 类型无效 '{Type}'，已跳过", kp.Title, kp.Type);
                    continue;
                }

                // 校验并去重标题
                var normalizedTitle = kp.Title?.Trim() ?? "";
                if (string.IsNullOrEmpty(normalizedTitle))
                {
                    _logger.LogWarning("知识点缺少标题，已跳过");
                    continue;
                }

                if (seenTitles.Contains(normalizedTitle))
                {
                    _logger.LogWarning("知识点标题重复 '{Title}'，已跳过", normalizedTitle);
                    continue;
                }

                seenTitles.Add(normalizedTitle);
                validPoints.Add(kp);
            }

            _logger.LogInformation("自检完成，有效知识点: {ValidCount}/{TotalCount}", validPoints.Count, knowledgePoints.Count);

            // 将 DTO 转换为标准的 KnowledgePoint
            var kpList = validPoints
                .Select((kp, index) =>
                {
                    var kpModel = kp.ToKnowledgePoint();
                    kpModel.KpId = $"kp_{index:D4}";
                    kpModel.BookHubId = documents.FirstOrDefault()?.BookHubId ?? string.Empty;
                    return kpModel;
                })
                .ToList();

            return kpList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用 LLM 提取知识点失败");
            _logger.LogError("失败上下文：文档数={DocumentCount}", documents.Count);
            _logger.LogError("可能的原因：LLM 服务不可用、API 密钥无效、网络连接问题或内容长度超过限制");
            return new List<KnowledgePoint>();
        }
    }

    private static bool IsValidKpType(string type)
    {
        var validTypes = new[] { "concept", "chapter", "process", "api", "bestPractice" };
        return validTypes.Contains(type.ToLowerInvariant());
    }

    private static void CollectAllSections(Document doc, List<Section> sections, List<(Document doc, Section section)> allSections)
    {
        foreach (var section in sections)
        {
            if (section.SubSections?.Count == 0)
                allSections.Add((doc, section));

            if (section.SubSections != null && section.SubSections.Count > 0)
            {
                CollectAllSections(doc, section.SubSections, allSections);
            }
        }
    }

    private async Task<string> ReadSectionContentAsync(Document doc, Section section, CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(doc.Path))
            {
                return string.Empty;
            }

            var lines = await File.ReadAllLinesAsync(doc.Path, cancellationToken);
            if (lines.Length == 0)
            {
                return string.Empty;
            }

            if (section.StartLine < 0 || section.EndLine > lines.Length)
            {
                return string.Empty;
            }

            var contentLines = lines.Skip(section.StartLine).Take(section.EndLine - section.StartLine);
            return string.Join("\n", contentLines);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取章节内容失败: {DocId}, {SectionPath}", doc.DocId, string.Join(" > ", section.HeadingPath));
            return string.Empty;
        }
    }
}