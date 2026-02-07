using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using ASimpleTutor.Core.Models.Dto;
using Microsoft.Extensions.Logging;

namespace ASimpleTutor.Core.Services;

/// <summary>
/// 幻灯片构建服务
/// 直接从知识点的现有内容构建幻灯片，无需调用 LLM
/// </summary>
public class SlideBuilderService
{
    private readonly ISourceTracker _sourceTracker;
    private readonly ILogger<SlideBuilderService> _logger;

    public SlideBuilderService(
        ISourceTracker sourceTracker,
        ILogger<SlideBuilderService> logger)
    {
        _sourceTracker = sourceTracker;
        _logger = logger;
    }

    /// <summary>
    /// 从知识点构建幻灯片响应
    /// </summary>
    public async Task<SlideCardResponse?> BuildSlidesAsync(KnowledgePoint kp, CancellationToken cancellationToken = default)
    {
        if (kp == null) return null;

        var slides = new List<SlideCardDto>();
        int order = 1;

        // 1. Cover - 使用 Summary 内容
        if (kp.Summary != null)
        {
            slides.Add(BuildCoverSlide(kp, order++));
        }

        // 2. Explanation - 使用 Levels[0] (L1)
        if (kp.Levels.Count > 0)
        {
            slides.Add(BuildExplanationSlide(kp, kp.Levels[0], order++));
        }

        // 3. Detail - 使用 Levels[1] (L2)
        if (kp.Levels.Count > 1)
        {
            slides.Add(BuildDetailSlide(kp, kp.Levels[1], order++));
        }

        // 4. DeepDive - 使用 Levels[2] (L3)
        if (kp.Levels.Count > 2)
        {
            slides.Add(BuildDeepDiveSlide(kp, kp.Levels[2], order++));
        }

        // 5. Source - 使用 Snippets
        if (kp.SnippetIds.Count > 0)
        {
            var sourceSlide = await BuildSourceSlideAsync(kp, order++, cancellationToken);
            if (sourceSlide != null)
            {
                slides.Add(sourceSlide);
            }
        }

        // 6. Relations - 使用 Relations
        if (kp.Relations.Count > 0)
        {
            slides.Add(BuildRelationsSlide(kp, order++));
        }

        // 7. Quiz - 如果有习题（从 Exercise 获取）
        // 注：习题目前需要单独的接口获取，这里暂时预留

        return new SlideCardResponse
        {
            SchemaVersion = "1.0",
            KpId = kp.KpId,
            Title = kp.Title,
            KpType = kp.Type.ToString(),
            Slides = slides,
            Meta = BuildMeta(slides, kp)
        };
    }

    /// <summary>
    /// 构建 Cover Slide - 使用 Summary
    /// </summary>
    private SlideCardDto BuildCoverSlide(KnowledgePoint kp, int order)
    {
        var content = new System.Text.StringBuilder();

        // 副标题（章节路径）
        if (kp.ChapterPath.Count > 0)
        {
            content.AppendLine($"*{string.Join(" > ", kp.ChapterPath)}*");
            content.AppendLine();
        }

        // 定义
        if (!string.IsNullOrEmpty(kp.Summary?.Definition))
        {
            content.AppendLine("## 定义");
            content.AppendLine(kp.Summary.Definition);
            content.AppendLine();
        }

        // 关键点
        if (kp.Summary?.KeyPoints.Count > 0)
        {
            content.AppendLine("## 核心要点");
            foreach (var point in kp.Summary.KeyPoints)
            {
                content.AppendLine($"- {point}");
            }
            content.AppendLine();
        }

        // 常见误区
        if (kp.Summary?.Pitfalls.Count > 0)
        {
            content.AppendLine("## 常见误区");
            foreach (var pitfall in kp.Summary.Pitfalls)
            {
                content.AppendLine($"- ⚠️ {pitfall}");
            }
        }

        // 提取知识点链接
        var kpLinks = ExtractKpLinksFromText(content.ToString(), kp.Relations);

        return new SlideCardDto
        {
            SlideId = $"{kp.KpId}_cover",
            Type = SlideTypeDto.Cover,
            Order = order,
            Title = kp.Title,
            Subtitle = kp.Type.ToString(),
            Content = content.ToString(),
            KpLinks = kpLinks,
            Config = new SlideConfigDto
            {
                AllowSkip = true,
                RequireComplete = false,
                EstimatedTime = 45
            }
        };
    }

    /// <summary>
    /// 构建 Explanation Slide - 使用 Levels[0]
    /// </summary>
    private SlideCardDto BuildExplanationSlide(KnowledgePoint kp, ContentLevel level, int order)
    {
        var content = new System.Text.StringBuilder();

        if (!string.IsNullOrEmpty(level.Title))
        {
            content.AppendLine($"## {level.Title}");
            content.AppendLine();
        }

        content.AppendLine(level.Content);

        var kpLinks = ExtractKpLinksFromText(level.Content, kp.Relations);

        return new SlideCardDto
        {
            SlideId = $"{kp.KpId}_explanation",
            Type = SlideTypeDto.Explanation,
            Order = order,
            Title = level.Title ?? "概念解释",
            Content = content.ToString(),
            KpLinks = kpLinks,
            Config = new SlideConfigDto
            {
                AllowSkip = true,
                RequireComplete = false,
                EstimatedTime = 90
            }
        };
    }

    /// <summary>
    /// 构建 Detail Slide - 使用 Levels[1]
    /// </summary>
    private SlideCardDto BuildDetailSlide(KnowledgePoint kp, ContentLevel level, int order)
    {
        var content = new System.Text.StringBuilder();

        if (!string.IsNullOrEmpty(level.Title))
        {
            content.AppendLine($"## {level.Title}");
            content.AppendLine();
        }

        content.AppendLine(level.Content);

        var kpLinks = ExtractKpLinksFromText(level.Content, kp.Relations);

        return new SlideCardDto
        {
            SlideId = $"{kp.KpId}_detail",
            Type = SlideTypeDto.Detail,
            Order = order,
            Title = level.Title ?? "详细内容",
            Content = content.ToString(),
            KpLinks = kpLinks,
            Config = new SlideConfigDto
            {
                AllowSkip = true,
                RequireComplete = false,
                EstimatedTime = 120
            }
        };
    }

    /// <summary>
    /// 构建 DeepDive Slide - 使用 Levels[2]
    /// </summary>
    private SlideCardDto BuildDeepDiveSlide(KnowledgePoint kp, ContentLevel level, int order)
    {
        var content = new System.Text.StringBuilder();

        if (!string.IsNullOrEmpty(level.Title))
        {
            content.AppendLine($"## {level.Title}");
            content.AppendLine();
        }

        content.AppendLine(level.Content);

        var kpLinks = ExtractKpLinksFromText(level.Content, kp.Relations);

        return new SlideCardDto
        {
            SlideId = $"{kp.KpId}_deepdive",
            Type = SlideTypeDto.DeepDive,
            Order = order,
            Title = level.Title ?? "深入探讨",
            Content = content.ToString(),
            KpLinks = kpLinks,
            Config = new SlideConfigDto
            {
                AllowSkip = true,
                RequireComplete = false,
                EstimatedTime = 150
            }
        };
    }

    /// <summary>
    /// 构建 Source Slide - 使用 Snippets
    /// </summary>
    private async Task<SlideCardDto?> BuildSourceSlideAsync(KnowledgePoint kp, int order, CancellationToken cancellationToken)
    {
        try
        {
            var snippets = _sourceTracker.GetSources(kp.SnippetIds);

            if (snippets.Count == 0)
                return null;

            var content = new System.Text.StringBuilder();
            content.AppendLine("## 原文来源");
            content.AppendLine();

            foreach (var snippet in snippets)
            {
                content.AppendLine($"### 📄 {Path.GetFileName(snippet.FilePath)}");
                content.AppendLine();

                if (snippet.HeadingPath.Count > 0)
                {
                    content.AppendLine($"**位置**: {string.Join(" > ", snippet.HeadingPath)} (行 {snippet.StartLine}-{snippet.EndLine})");
                    content.AppendLine();
                }

                // 使用引用块显示原文
                content.AppendLine("> " + snippet.Content.Replace("\n", "\n> "));
                content.AppendLine();
                content.AppendLine("---");
                content.AppendLine();
            }

            return new SlideCardDto
            {
                SlideId = $"{kp.KpId}_source",
                Type = SlideTypeDto.Source,
                Order = order,
                Title = "原文对照",
                Content = content.ToString(),
                KpLinks = new List<KnowledgePointLinkDto>(),
                Config = new SlideConfigDto
                {
                    AllowSkip = true,
                    RequireComplete = false,
                    EstimatedTime = 60
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "构建原文幻灯片失败: {KpId}", kp.KpId);
            return null;
        }
    }

    /// <summary>
    /// 构建 Relations Slide - 使用 Relations
    /// </summary>
    private SlideCardDto BuildRelationsSlide(KnowledgePoint kp, int order)
    {
        var content = new System.Text.StringBuilder();
        content.AppendLine("## 知识关联");
        content.AppendLine();

        // 按关系类型分组
        var groupedRelations = kp.Relations
            .GroupBy(r => r.Type)
            .ToDictionary(g => g.Key, g => g.ToList());

        var kpLinks = new List<KnowledgePointLinkDto>();

        // 前置依赖
        if (groupedRelations.ContainsKey(RelationType.Prerequisite))
        {
            content.AppendLine("### 🔸 前置知识");
            foreach (var rel in groupedRelations[RelationType.Prerequisite])
            {
                content.AppendLine($"- [[{rel.ToKpId}]]");
                if (!string.IsNullOrEmpty(rel.Description))
                {
                    content.AppendLine($"  - {rel.Description}");
                }

                kpLinks.Add(new KnowledgePointLinkDto
                {
                    Text = rel.ToKpId,
                    TargetKpId = rel.ToKpId,
                    Relationship = "prerequisite"
                });
            }
            content.AppendLine();
        }

        // 相关知识
        if (groupedRelations.ContainsKey(RelationType.Related))
        {
            content.AppendLine("### 🔗 相关知识");
            foreach (var rel in groupedRelations[RelationType.Related])
            {
                content.AppendLine($"- [[{rel.ToKpId}]]");
                if (!string.IsNullOrEmpty(rel.Description))
                {
                    content.AppendLine($"  - {rel.Description}");
                }

                kpLinks.Add(new KnowledgePointLinkDto
                {
                    Text = rel.ToKpId,
                    TargetKpId = rel.ToKpId,
                    Relationship = "related"
                });
            }
            content.AppendLine();
        }

        // 对比关系
        if (groupedRelations.ContainsKey(RelationType.Contrast))
        {
            content.AppendLine("### ⚖️ 对比学习");
            foreach (var rel in groupedRelations[RelationType.Contrast])
            {
                content.AppendLine($"- [[{rel.ToKpId}]]");
                if (!string.IsNullOrEmpty(rel.Description))
                {
                    content.AppendLine($"  - {rel.Description}");
                }

                kpLinks.Add(new KnowledgePointLinkDto
                {
                    Text = rel.ToKpId,
                    TargetKpId = rel.ToKpId,
                    Relationship = "contrast"
                });
            }
            content.AppendLine();
        }

        // 相似关系
        if (groupedRelations.ContainsKey(RelationType.Similar))
        {
            content.AppendLine("### 🔄 相似概念");
            foreach (var rel in groupedRelations[RelationType.Similar])
            {
                content.AppendLine($"- [[{rel.ToKpId}]]");
                if (!string.IsNullOrEmpty(rel.Description))
                {
                    content.AppendLine($"  - {rel.Description}");
                }

                kpLinks.Add(new KnowledgePointLinkDto
                {
                    Text = rel.ToKpId,
                    TargetKpId = rel.ToKpId,
                    Relationship = "similar"
                });
            }
        }

        return new SlideCardDto
        {
            SlideId = $"{kp.KpId}_relations",
            Type = SlideTypeDto.Relations,
            Order = order,
            Title = "知识关联",
            Content = content.ToString(),
            KpLinks = kpLinks,
            Config = new SlideConfigDto
            {
                AllowSkip = true,
                RequireComplete = false,
                EstimatedTime = 45
            }
        };
    }

    /// <summary>
    /// 构建元数据
    /// </summary>
    private SlideMetaDto BuildMeta(List<SlideCardDto> slides, KnowledgePoint kp)
    {
        var totalEstimatedTime = slides.Sum(s => s.Config.EstimatedTime);

        // 根据知识点重要性判断难度
        var difficulty = kp.Importance switch
        {
            > 0.7f => "advanced",
            > 0.4f => "intermediate",
            _ => "beginner"
        };

        return new SlideMetaDto
        {
            TotalSlides = slides.Count,
            EstimatedTime = totalEstimatedTime,
            Difficulty = difficulty,
            GeneratedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 从文本中提取知识点链接
    /// </summary>
    private List<KnowledgePointLinkDto> ExtractKpLinksFromText(string text, List<KnowledgeRelation> relations)
    {
        var links = new List<KnowledgePointLinkDto>();

        foreach (var relation in relations)
        {
            // 检查文本中是否提到了相关知识点
            // 这里使用简单的字符串匹配，实际可以使用更复杂的 NLP 技术
            if (text.Contains(relation.ToKpId, StringComparison.OrdinalIgnoreCase))
            {
                links.Add(new KnowledgePointLinkDto
                {
                    Text = relation.ToKpId,
                    TargetKpId = relation.ToKpId,
                    Relationship = relation.Type.ToString().ToLowerInvariant()
                });
            }
        }

        return links;
    }
}
