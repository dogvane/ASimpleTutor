using ASimpleTutor.Core.Models;

namespace ASimpleTutor.Tests.QualityControl;

/// <summary>
/// 质量控制服务 - 用于验证知识点质量
/// </summary>
public class QualityControlValidator
{
    public const int MaxTitleLength = 60;
    private static readonly HashSet<string> IgnoredHeadings = new(StringComparer.OrdinalIgnoreCase)
    {
        "概述", "总结", "简介", "引言", "前言", "目录", "参考文献", "参考书目"
    };

    /// <summary>
    /// 验证知识点质量
    /// </summary>
    public List<QualityIssue> Validate(List<KnowledgePoint> knowledgePoints)
    {
        var issues = new List<QualityIssue>();

        foreach (var kp in knowledgePoints)
        {
            // 检查标题长度
            if (string.IsNullOrWhiteSpace(kp.Title))
            {
                issues.Add(new QualityIssue(kp.KpId, QualityIssueType.EmptyTitle, "知识点标题为空"));
            }
            else if (kp.Title.Length > MaxTitleLength)
            {
                issues.Add(new QualityIssue(kp.KpId, QualityIssueType.TitleTooLong,
                    $"标题超过{MaxTitleLength}字符: {kp.Title.Length}字符"));
            }

            // 检查无意义章节名
            if (IgnoredHeadings.Contains(kp.Title))
            {
                issues.Add(new QualityIssue(kp.KpId, QualityIssueType.NonsensicalTitle,
                    $"标题为无意义章节名: {kp.Title}"));
            }



            // 检查重要性评分范围
            if (kp.Importance < 0.0f || kp.Importance > 1.0f)
            {
                issues.Add(new QualityIssue(kp.KpId, QualityIssueType.InvalidImportance,
                    $"重要性评分超出范围: {kp.Importance}"));
            }
        }

        return issues;
    }

    /// <summary>
    /// 过滤低质量知识点
    /// </summary>
    public List<KnowledgePoint> FilterLowQuality(List<KnowledgePoint> knowledgePoints)
    {
        var issues = Validate(knowledgePoints);
        var invalidIds = issues.Select(i => i.KpId).ToHashSet();
        return knowledgePoints.Where(kp => !invalidIds.Contains(kp.KpId)).ToList();
    }
}

/// <summary>
/// 质量问题
/// </summary>
public class QualityIssue
{
    public string KpId { get; }
    public QualityIssueType Type { get; }
    public string Message { get; }

    public QualityIssue(string kpId, QualityIssueType type, string message)
    {
        KpId = kpId;
        Type = type;
        Message = message;
    }
}

/// <summary>
/// 质量问题类型
/// </summary>
public enum QualityIssueType
{
    EmptyTitle,
    TitleTooLong,
    NonsensicalTitle,
    MissingEvidence,
    InvalidImportance
}

/// <summary>
/// 质量控制模块测试用例
/// 对应测试需求文档：TC-QC-001 ~ TC-QC-004
/// </summary>
public class QualityControlTests
{
    private readonly QualityControlValidator _validator;

    public QualityControlTests()
    {
        _validator = new QualityControlValidator();
    }

    [Fact]
    public void Validate_WithValidKnowledgePoints_ShouldReturnNoIssues()
    {
        // Arrange
        var kps = new List<KnowledgePoint>
        {
            new KnowledgePoint
            {
                KpId = "kp_0001",
                Title = "核心概念",
                Importance = 0.8f,

            },
            new KnowledgePoint
            {
                KpId = "kp_0002",
                Title = "重要术语",
                Importance = 0.6f,

            }
        };

        // Act
        var issues = _validator.Validate(kps);

        // Assert
        issues.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithTitleTooLong_ShouldDetectIssue()
    {
        // Arrange
        var longTitle = new string('A', QualityControlValidator.MaxTitleLength + 1);
        var kps = new List<KnowledgePoint>
        {
            new KnowledgePoint
            {
                KpId = "kp_0001",
                Title = longTitle,
                Importance = 0.5f,

            }
        };

        // Act
        var issues = _validator.Validate(kps);

        // Assert
        issues.Should().Contain(i => i.Type == QualityIssueType.TitleTooLong);
    }

    [Fact]
    public void Validate_WithEmptyTitle_ShouldDetectIssue()
    {
        // Arrange
        var kps = new List<KnowledgePoint>
        {
            new KnowledgePoint
            {
                KpId = "kp_0001",
                Title = "",
                Importance = 0.5f,

            }
        };

        // Act
        var issues = _validator.Validate(kps);

        // Assert
        issues.Should().Contain(i => i.Type == QualityIssueType.EmptyTitle);
    }

    [Fact]
    public void Validate_WithNonsensicalTitle_ShouldDetectIssue()
    {
        // Arrange
        var kps = new List<KnowledgePoint>
        {
            new KnowledgePoint
            {
                KpId = "kp_0001",
                Title = "概述",
                Importance = 0.5f,

            },
            new KnowledgePoint
            {
                KpId = "kp_0002",
                Title = "总结",
                Importance = 0.5f,
            },
            new KnowledgePoint
            {
                KpId = "kp_0003",
                Title = "简介",
                Importance = 0.5f,

            }
        };

        // Act
        var issues = _validator.Validate(kps);

        // Assert
        issues.Should().HaveCount(3);
        issues.All(i => i.Type == QualityIssueType.NonsensicalTitle).Should().BeTrue();
    }

    [Fact]
    public void Validate_WithInvalidImportance_ShouldDetectIssue()
    {
        // Arrange
        var kps = new List<KnowledgePoint>
        {
            new KnowledgePoint
            {
                KpId = "kp_0001",
                Title = "重要性过高",
                Importance = 1.5f
            },
            new KnowledgePoint
            {
                KpId = "kp_0002",
                Title = "重要性为负",
                Importance = -0.1f
            }
        };

        // Act
        var issues = _validator.Validate(kps);

        // Assert
        issues.Should().Contain(i => i.Type == QualityIssueType.InvalidImportance);
    }

    [Fact]
    public void FilterLowQuality_ShouldRemoveInvalidPoints()
    {
        // Arrange
        var kps = new List<KnowledgePoint>
        {
            new KnowledgePoint { KpId = "kp_0001", Title = "有效知识点", Importance = 0.5f },
            new KnowledgePoint { KpId = "kp_0002", Title = "概述", Importance = 0.5f }, // 无效
            new KnowledgePoint { KpId = "kp_0003", Title = "另一个有效点", Importance = 0.8f },
            new KnowledgePoint { KpId = "kp_0004", Title = "", Importance = 0.5f } // 无效
        };

        // Act
        var filtered = _validator.FilterLowQuality(kps);

        // Assert
        filtered.Should().HaveCount(2);
        filtered.Select(k => k.KpId).Should().Contain("kp_0001");
        filtered.Select(k => k.KpId).Should().Contain("kp_0003");
    }

    [Fact]
    public void Validate_WithMixedQuality_ShouldReportAllIssues()
    {
        // Arrange
        var kps = new List<KnowledgePoint>
        {
            new KnowledgePoint { KpId = "kp_0001", Title = "有效知识点", Importance = 0.5f },
            new KnowledgePoint { KpId = "kp_0002", Title = new string('X', 70), Importance = 0.5f },
            new KnowledgePoint { KpId = "kp_0003", Title = "概述", Importance = 0.5f },
            new KnowledgePoint { KpId = "kp_0004", Title = "无证据", Importance = 0.5f }
        };

        // Act
        var issues = _validator.Validate(kps);

        // Assert
        issues.Should().HaveCount(2); // 2个问题（标题过长和无意义标题）
        issues.Select(i => i.KpId).Should().Contain("kp_0002");
        issues.Select(i => i.KpId).Should().Contain("kp_0003");
    }
}
