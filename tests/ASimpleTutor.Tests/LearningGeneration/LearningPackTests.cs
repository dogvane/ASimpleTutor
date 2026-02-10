using ASimpleTutor.Core.Models;
using FluentAssertions;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace ASimpleTutor.Tests.LearningGeneration;

/// <summary>
/// LearningPack 数据模型测试用例
/// 对应测试需求文档：TC-LG-MOD-001 ~ TC-LG-MOD-003
/// </summary>
public class LearningPackTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    [Fact]
    public void LearningPack_DefaultValues_ShouldBeInitialized()
    {
        // Arrange & Act
        var lp = new LearningPack();

        // Assert
        lp.KpId.Should().Be(string.Empty);
        lp.Summary.Should().NotBeNull();
        lp.Levels.Should().NotBeNull();
        lp.RelatedKpIds.Should().NotBeNull();
    }

    [Fact]
    public void LearningPack_CanSetProperties()
    {
        // Arrange
        var lp = new LearningPack
        {
            KpId = "test_kp",
            Summary = new Summary
            {
                Definition = "测试定义",
                KeyPoints = new List<string> { "要点1", "要点2" },
                Pitfalls = new List<string> { "误区1" }
            },
            Levels = new List<ContentLevel>
            {
                new ContentLevel { Level = 1, Title = "概览", Content = "内容" }
            },

            RelatedKpIds = new List<string> { "kp_related" }
        };

        // Assert
        lp.KpId.Should().Be("test_kp");
        lp.Summary.Definition.Should().Be("测试定义");
        lp.Levels.Should().HaveCount(1);

        lp.RelatedKpIds.Should().HaveCount(1);
    }

    [Fact]
    public void LearningPack_CanSerializeToJson()
    {
        // Arrange
        var lp = new LearningPack
        {
            KpId = "test_kp",
            Summary = new Summary
            {
                Definition = "测试定义",
                KeyPoints = new List<string> { "要点1" },
                Pitfalls = new List<string>()
            },
            Levels = new List<ContentLevel>
            {
                new ContentLevel { Level = 1, Title = "概览", Content = "内容" }
            },

            RelatedKpIds = new List<string>()
        };

        // Act
        var json = JsonSerializer.Serialize(lp, JsonOptions);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("test_kp");
        json.Should().Contain("测试定义");
    }

    [Fact]
    public void LearningPack_CanDeserializeFromJson()
    {
        // Arrange - 使用 PascalCase（与模型属性名一致）
        var json = @"{
            ""KpId"": ""test_kp"",
            ""Summary"": {
                ""Definition"": ""测试定义"",
                ""KeyPoints"": [""要点1""],
                ""Pitfalls"": []
            },
            ""Levels"": [
                { ""Level"": 1, ""Title"": ""概览"", ""Content"": ""内容"" }
            ],
            ""SnippetIds"": [""s1""],
            ""RelatedKpIds"": []
        }";

        // Act
        var lp = JsonSerializer.Deserialize<LearningPack>(json);

        // Assert
        lp.Should().NotBeNull();
        lp!.KpId.Should().Be("test_kp");
        lp.Summary.Definition.Should().Be("测试定义");
        lp.Levels.Should().HaveCount(1);
    }

    [Fact]
    public void Summary_DefaultValues_ShouldBeInitialized()
    {
        // Arrange & Act
        var summary = new Summary();

        // Assert
        summary.Definition.Should().Be(string.Empty);
        summary.KeyPoints.Should().NotBeNull();
        summary.Pitfalls.Should().NotBeNull();
    }

    [Fact]
    public void Summary_CanSetProperties()
    {
        // Arrange
        var summary = new Summary
        {
            Definition = "Markdown 是一种轻量级标记语言。",
            KeyPoints = new List<string> { "要点1", "要点2", "要点3" },
            Pitfalls = new List<string> { "误区1", "误区2" }
        };

        // Assert
        summary.Definition.Should().Be("Markdown 是一种轻量级标记语言。");
        summary.KeyPoints.Should().HaveCount(3);
        summary.Pitfalls.Should().HaveCount(2);
    }

    [Fact]
    public void ContentLevel_DefaultValues_ShouldBeInitialized()
    {
        // Arrange & Act
        var level = new ContentLevel();

        // Assert
        level.Level.Should().Be(0);
        level.Title.Should().Be(string.Empty);
        level.Content.Should().Be(string.Empty);
    }

    [Fact]
    public void ContentLevel_CanSetProperties()
    {
        // Arrange
        var level = new ContentLevel
        {
            Level = 2,
            Title = "详细",
            Content = "这是详细内容的描述。"
        };

        // Assert
        level.Level.Should().Be(2);
        level.Title.Should().Be("详细");
        level.Content.Should().Be("这是详细内容的描述。");
    }

    [Fact]
    public void ContentLevel_LevelValue_ShouldBeValid()
    {
        // Arrange
        var levels = new List<ContentLevel>
        {
            new ContentLevel { Level = 1, Title = "概览", Content = "L1" },
            new ContentLevel { Level = 2, Title = "详细", Content = "L2" },
            new ContentLevel { Level = 3, Title = "深入", Content = "L3" }
        };

        // Assert
        levels[0].Level.Should().Be(1);
        levels[1].Level.Should().Be(2);
        levels[2].Level.Should().Be(3);
    }

    [Fact]
    public void Summary_Serialization_ShouldIncludeAllFields()
    {
        // Arrange
        var summary = new Summary
        {
            Definition = "测试定义",
            KeyPoints = new List<string> { "要点1" },
            Pitfalls = new List<string> { "误区1" }
        };

        // Act
        var json = JsonSerializer.Serialize(summary, JsonOptions);

        // Assert
        // 验证 JSON 中包含属性值
        json.Should().Contain("测试定义");
        json.Should().Contain("要点1");
        json.Should().Contain("误区1");
    }

    [Fact]
    public void ContentLevel_Serialization_ShouldIncludeAllFields()
    {
        // Arrange
        var level = new ContentLevel
        {
            Level = 2,
            Title = "详细",
            Content = "详细内容"
        };

        // Act
        var json = JsonSerializer.Serialize(level, JsonOptions);

        // Assert
        json.Should().Contain("2");
        json.Should().Contain("详细");
        json.Should().Contain("详细内容");
    }

    [Fact]
    public void LearningPack_ComplexStructure_ShouldSerializeCorrectly()
    {
        // Arrange
        var lp = new LearningPack
        {
            KpId = "kp_complex",
            Summary = new Summary
            {
                Definition = "复杂概念的精确定义。",
                KeyPoints = new List<string> { "第一点", "第二点", "第三点" },
                Pitfalls = new List<string> { "常见错误" }
            },
            Levels = new List<ContentLevel>
            {
                new ContentLevel { Level = 1, Title = "概览", Content = "概览内容" },
                new ContentLevel { Level = 2, Title = "详细", Content = "详细说明" },
                new ContentLevel { Level = 3, Title = "深入", Content = "深入分析" }
            },

            RelatedKpIds = new List<string> { "kp_related_1" }
        };

        // Act
        var json = JsonSerializer.Serialize(lp, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true
        });

        // Assert
        json.Should().Contain("kp_complex");
        json.Should().Contain("概览");
        json.Should().Contain("详细");
        json.Should().Contain("深入");
    }

    [Fact]
    public void LearningPack_WithEmptyLists_ShouldSerializeCorrectly()
    {
        // Arrange
        var lp = new LearningPack
        {
            KpId = "kp_empty",
            Summary = new Summary(),
            Levels = new List<ContentLevel>(),

            RelatedKpIds = new List<string>()
        };

        // Act
        var json = JsonSerializer.Serialize(lp, JsonOptions);

        // Assert
        json.Should().Contain("kp_empty");
        json.Should().Contain("Summary");
        json.Should().Contain("Levels");
    }

    [Fact]
    public void LearningPack_RoundTrip_ShouldPreserveData()
    {
        // Arrange
        var original = new LearningPack
        {
            KpId = "roundtrip_test",
            Summary = new Summary
            {
                Definition = "往返测试定义",
                KeyPoints = new List<string> { "测试要点1", "测试要点2" },
                Pitfalls = new List<string> { "测试误区" }
            },
            Levels = new List<ContentLevel>
            {
                new ContentLevel { Level = 1, Title = "L1", Content = "L1内容" },
                new ContentLevel { Level = 2, Title = "L2", Content = "L2内容" }
            },

            RelatedKpIds = new List<string>()
        };

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<LearningPack>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.KpId.Should().Be(original.KpId);
        deserialized.Summary.Definition.Should().Be(original.Summary.Definition);
        deserialized.Summary.KeyPoints.Should().HaveCount(2);
        deserialized.Levels.Should().HaveCount(2);
    }
}
