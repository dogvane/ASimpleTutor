using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using ASimpleTutor.Core.Models.Dto;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ASimpleTutor.Core.Services;

/// <summary>
/// 幻灯片构建服务
/// </summary>
public class SlideBuilderService
{
    private readonly ILogger<SlideBuilderService> _logger;

    public SlideBuilderService(ILogger<SlideBuilderService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 为知识点构建幻灯片
    /// </summary>
    public List<SlideCardDto> BuildSlides(KnowledgePoint kp)
    {
        var slides = new List<SlideCardDto>();
        int order = 1;

        // 1. 构建概览幻灯片
        slides.Add(BuildOverviewSlide(kp, ref order));

        // 2. 构建定义幻灯片
        if (kp.Summary?.Definition != null)
        {
            slides.Add(BuildDefinitionSlide(kp, ref order));
        }

        // 3. 构建要点幻灯片
        if (kp.Summary?.KeyPoints != null && kp.Summary.KeyPoints.Count > 0)
        {
            slides.Add(BuildKeyPointsSlide(kp, ref order));
        }

        // 4. 构建常见误区幻灯片
        if (kp.Summary?.Pitfalls != null && kp.Summary.Pitfalls.Count > 0)
        {
            slides.Add(BuildPitfallsSlide(kp, ref order));
        }

        // 5. 构建层次化内容幻灯片
        if (kp.Levels != null && kp.Levels.Count > 0)
        {
            slides.AddRange(BuildContentLevelSlides(kp, ref order));
        }

        // 6. 构建知识关联幻灯片
        slides.Add(BuildRelationsSlide(kp, ref order));

        // 7. 构建总结幻灯片
        slides.Add(BuildSummarySlide(kp, ref order));

        return slides;
    }

    private SlideCardDto BuildOverviewSlide(KnowledgePoint kp, ref int order)
    {
        var content = new StringBuilder();
        content.AppendLine($"# {kp.Title}");
        content.AppendLine();
        content.AppendLine($"## 章节路径");
        content.AppendLine($"{string.Join(" > ", kp.ChapterPath)}");
        content.AppendLine();
        content.AppendLine($"## 知识点类型");
        content.AppendLine($"{kp.Type}");
        content.AppendLine();
        content.AppendLine($"## 重要程度");
        content.AppendLine($"{(kp.Importance * 100):F0}%");
        content.AppendLine();
        content.AppendLine($"## 来源文档");
        content.AppendLine($"{kp.DocId}");

        return new SlideCardDto
        {
            SlideId = $"{kp.KpId}_overview",
            Type = SlideTypeDto.Cover,
            Order = order++,
            Title = "概览",
            Content = content.ToString()
        };
    }

    private SlideCardDto BuildDefinitionSlide(KnowledgePoint kp, ref int order)
    {
        var content = new StringBuilder();
        content.AppendLine($"# {kp.Title}");
        content.AppendLine();
        content.AppendLine($"## 定义");
        content.AppendLine($"{kp.Summary?.Definition}");

        if (kp.Aliases != null && kp.Aliases.Count > 0)
        {
            content.AppendLine();
            content.AppendLine($"## 别名");
            content.AppendLine($"{string.Join(", ", kp.Aliases)}");
        }

        return new SlideCardDto
        {
            SlideId = $"{kp.KpId}_definition",
            Type = SlideTypeDto.Explanation,
            Order = order++,
            Title = "定义",
            Content = content.ToString()
        };
    }

    private SlideCardDto BuildKeyPointsSlide(KnowledgePoint kp, ref int order)
    {
        var content = new StringBuilder();
        content.AppendLine($"# {kp.Title}");
        content.AppendLine();
        content.AppendLine($"## 核心要点");

        if (kp.Summary?.KeyPoints != null)
        {
            for (int i = 0; i < kp.Summary.KeyPoints.Count; i++)
            {
                content.AppendLine($"{(i + 1)}. {kp.Summary.KeyPoints[i]}");
            }
        }

        return new SlideCardDto
        {
            SlideId = $"{kp.KpId}_keypoints",
            Type = SlideTypeDto.Detail,
            Order = order++,
            Title = "核心要点",
            Content = content.ToString()
        };
    }

    private SlideCardDto BuildPitfallsSlide(KnowledgePoint kp, ref int order)
    {
        var content = new StringBuilder();
        content.AppendLine($"# {kp.Title}");
        content.AppendLine();
        content.AppendLine($"## 常见误区");

        if (kp.Summary?.Pitfalls != null)
        {
            for (int i = 0; i < kp.Summary.Pitfalls.Count; i++)
            {
                content.AppendLine($"{(i + 1)}. {kp.Summary.Pitfalls[i]}");
            }
        }

        return new SlideCardDto
        {
            SlideId = $"{kp.KpId}_pitfalls",
            Type = SlideTypeDto.Detail,
            Order = order++,
            Title = "常见误区",
            Content = content.ToString()
        };
    }

    private List<SlideCardDto> BuildContentLevelSlides(KnowledgePoint kp, ref int order)
    {
        var slides = new List<SlideCardDto>();

        if (kp.Levels != null)
        {
            foreach (var level in kp.Levels)
            {
                var content = new StringBuilder();
                content.AppendLine($"# {kp.Title}");
                content.AppendLine();
                content.AppendLine($"## {level.Title}");
                content.AppendLine($"{level.Content}");

                slides.Add(new SlideCardDto
                {
                    SlideId = $"{kp.KpId}_level{level.Level}",
                    Type = SlideTypeDto.DeepDive,
                    Order = order++,
                    Title = level.Title,
                    Content = content.ToString(),
                    Config = new SlideConfigDto
                    {
                        AllowSkip = true,
                        RequireComplete = false,
                        EstimatedTime = 90
                    }
                });
            }
        }

        return slides;
    }

    private SlideCardDto BuildRelationsSlide(KnowledgePoint kp, ref int order)
    {
        var content = new StringBuilder();
        content.AppendLine($"# {kp.Title}");
        content.AppendLine();
        content.AppendLine($"## 知识关联");
        content.AppendLine("当前版本暂不支持知识关联功能。");

        var kpLinks = new List<KnowledgePointLinkDto>();

        return new SlideCardDto
        {
            SlideId = $"{kp.KpId}_relations",
            Type = SlideTypeDto.Relations,
            Order = order++,
            Title = "知识关联",
            Content = content.ToString(),
            KpLinks = kpLinks
        };
    }

    private SlideCardDto BuildSummarySlide(KnowledgePoint kp, ref int order)
    {
        var content = new StringBuilder();
        content.AppendLine($"# {kp.Title}");
        content.AppendLine();
        content.AppendLine($"## 学习总结");
        content.AppendLine("请回顾以下内容：");
        content.AppendLine();
        content.AppendLine("1. 知识点的定义和核心概念");
        content.AppendLine("2. 关键要点和常见误区");
        content.AppendLine("3. 不同层次的内容理解");
        content.AppendLine();
        content.AppendLine("## 学习建议");
        content.AppendLine("- 结合原文片段加深理解");
        content.AppendLine("- 尝试将知识点应用到实际场景");
        content.AppendLine("- 与相关知识点建立联系");

        return new SlideCardDto
        {
            SlideId = $"{kp.KpId}_summary",
            Type = SlideTypeDto.Summary,
            Order = order++,
            Title = "总结",
            Content = content.ToString()
        };
    }

    /// <summary>
    /// 构建元数据
    /// </summary>
    private SlideMetaDto BuildMeta(List<SlideCardDto> slides, KnowledgePoint kp)
    {
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
            EstimatedTime = slides.Count * 60, // 每张幻灯片默认60秒
            Difficulty = difficulty,
            GeneratedAt = DateTime.UtcNow
        };
    }
}
