using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using ASimpleTutor.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;

namespace ASimpleTutor.Tests.LearningGeneration;

/// <summary>
/// 学习内容生成模块测试用例
/// 对应测试需求文档：TC-LG-001 ~ TC-LG-007
/// </summary>
public class LearningGeneratorTests
{
    private readonly Mock<ISimpleRagService> _ragServiceMock;
    private readonly Mock<ISourceTracker> _sourceTrackerMock;
    private readonly Mock<ILLMService> _llmServiceMock;
    private readonly Mock<ILogger<LearningGenerator>> _loggerMock;
    private readonly LearningGenerator _generator;

    public LearningGeneratorTests()
    {
        _ragServiceMock = new Mock<ISimpleRagService>();
        _sourceTrackerMock = new Mock<ISourceTracker>();
        _llmServiceMock = new Mock<ILLMService>();
        _loggerMock = new Mock<ILogger<LearningGenerator>>();

        _generator = new LearningGenerator(
            _ragServiceMock.Object,
            _sourceTrackerMock.Object,
            _llmServiceMock.Object,
            _loggerMock.Object);
    }

    #region 正常生成测试

    [Fact]
    public async Task GenerateAsync_WithValidInput_ShouldReturnLearningPack()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        var snippets = CreateTestSnippets();

        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<IEnumerable<string>>()))
            .Returns(snippets);

        var llmResponse = CreateValidLLMResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<LearningContentResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _generator.GenerateAsync(kp, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.KpId.Should().Be(kp.KpId);
        result.SnippetIds.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GenerateAsync_ShouldGenerateSummaryWithDefinition()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        var snippets = CreateTestSnippets();

        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<IEnumerable<string>>()))
            .Returns(snippets);

        var llmResponse = CreateValidLLMResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<LearningContentResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _generator.GenerateAsync(kp, CancellationToken.None);

        // Assert
        result.Summary.Should().NotBeNull();
        result.Summary.Definition.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateAsync_ShouldGenerateKeyPoints()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        var snippets = CreateTestSnippets();

        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<IEnumerable<string>>()))
            .Returns(snippets);

        var llmResponse = CreateValidLLMResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<LearningContentResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _generator.GenerateAsync(kp, CancellationToken.None);

        // Assert
        result.Summary.Should().NotBeNull();
        result.Summary.KeyPoints.Should().NotBeNull();
        result.Summary.KeyPoints.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GenerateAsync_ShouldGeneratePitfalls()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        var snippets = CreateTestSnippets();

        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<IEnumerable<string>>()))
            .Returns(snippets);

        var llmResponse = CreateValidLLMResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<LearningContentResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _generator.GenerateAsync(kp, CancellationToken.None);

        // Assert
        result.Summary.Should().NotBeNull();
        result.Summary.Pitfalls.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateAsync_ShouldGenerateThreeLevels()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        var snippets = CreateTestSnippets();

        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<IEnumerable<string>>()))
            .Returns(snippets);

        var llmResponse = CreateValidLLMResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<LearningContentResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _generator.GenerateAsync(kp, CancellationToken.None);

        // Assert
        result.Levels.Should().NotBeNull();
        result.Levels.Should().HaveCount(3);

        // 验证层级顺序
        result.Levels[0].Level.Should().Be(1);
        result.Levels[1].Level.Should().Be(2);
        result.Levels[2].Level.Should().Be(3);
    }

    [Fact]
    public async Task GenerateAsync_ShouldHaveCorrectLevelStructure()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        var snippets = CreateTestSnippets();

        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<IEnumerable<string>>()))
            .Returns(snippets);

        var llmResponse = CreateValidLLMResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<LearningContentResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _generator.GenerateAsync(kp, CancellationToken.None);

        // Assert
        result.Levels[0].Title.Should().Be("概览");
        result.Levels[1].Title.Should().Be("详细");
        result.Levels[2].Title.Should().Be("深入");
    }

    #endregion

    #region 原文片段关联测试

    [Fact]
    public async Task GenerateAsync_ShouldAssociateSnippetIds()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        var snippets = CreateTestSnippets();

        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<IEnumerable<string>>()))
            .Returns(snippets);

        var llmResponse = CreateValidLLMResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<LearningContentResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _generator.GenerateAsync(kp, CancellationToken.None);

        // Assert
        result.SnippetIds.Should().NotBeEmpty();
        result.SnippetIds.Should().Contain("snippet_001");
        result.SnippetIds.Should().Contain("snippet_002");
    }

    [Fact]
    public async Task GenerateAsync_WithEmptySnippets_ShouldAssociateEmptyList()
    {
        // Arrange
        var kp = new KnowledgePoint
        {
            KpId = "kp_empty",
            Title = "测试知识点",
            SnippetIds = new List<string>()
        };

        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<IEnumerable<string>>()))
            .Returns(new List<SourceSnippet>());

        var llmResponse = CreateValidLLMResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<LearningContentResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _generator.GenerateAsync(kp, CancellationToken.None);

        // Assert
        result.SnippetIds.Should().BeEmpty();
    }

    #endregion

    #region 降级策略测试

    [Fact]
    public async Task GenerateAsync_WhenLLMFails_ShouldUseFallback()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        var snippets = CreateTestSnippets();

        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<IEnumerable<string>>()))
            .Returns(snippets);

        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<LearningContentResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("LLM Error"));

        // Act
        var result = await _generator.GenerateAsync(kp, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Summary.Should().NotBeNull();
        result.Levels.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateAsync_WhenLLMFails_ShouldGenerateL1Fallback()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        var snippets = CreateTestSnippets();

        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<IEnumerable<string>>()))
            .Returns(snippets);

        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<LearningContentResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("LLM Error"));

        // Act
        var result = await _generator.GenerateAsync(kp, CancellationToken.None);

        // Assert
        result.Levels.Should().NotBeNull();
        result.Levels.Should().Contain(l => l.Level == 1);
        result.Levels.First(l => l.Level == 1).Title.Should().Be("概览");
    }

    [Fact]
    public async Task GenerateAsync_WhenLLMFails_ShouldExtractKeyPointsFromSnippets()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        var snippets = CreateTestSnippets();

        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<IEnumerable<string>>()))
            .Returns(snippets);

        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<LearningContentResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("LLM Error"));

        // Act
        var result = await _generator.GenerateAsync(kp, CancellationToken.None);

        // Assert
        result.Summary.KeyPoints.Should().NotBeNull();
        // 降级策略会从原文片段提取句子作为要点
    }

    [Fact]
    public async Task GenerateAsync_WhenLLMReturnsNull_ShouldUseFallback()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        var snippets = CreateTestSnippets();

        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<IEnumerable<string>>()))
            .Returns(snippets);

        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<LearningContentResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((LearningContentResponse?)null);

        // Act
        var result = await _generator.GenerateAsync(kp, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Summary.Should().NotBeNull();
    }

    #endregion

    #region 边界条件测试

    [Fact]
    public async Task GenerateAsync_WithEmptyTitle_ShouldStillGenerate()
    {
        // Arrange
        var kp = new KnowledgePoint
        {
            KpId = "kp_002",
            Title = "",
            ChapterPath = new List<string> { "第一章" },
            Importance = 0.5f,
            SnippetIds = new List<string> { "snippet_001" }
        };
        var snippets = CreateTestSnippets();

        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<IEnumerable<string>>()))
            .Returns(snippets);

        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<LearningContentResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("LLM Error"));

        // Act
        var result = await _generator.GenerateAsync(kp, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.KpId.Should().Be("kp_002");
    }

    [Fact]
    public async Task GenerateAsync_WithNoSnippets_ShouldUseFallback()
    {
        // Arrange
        var kp = new KnowledgePoint
        {
            KpId = "kp_003",
            Title = "无片段知识点",
            ChapterPath = new List<string> { "第一章" },
            Importance = 0.5f,
            SnippetIds = new List<string>()
        };

        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<string[]>()))
            .Returns(new List<SourceSnippet>());

        // Act
        var result = await _generator.GenerateAsync(kp, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.SnippetIds.Should().BeEmpty();
    }

    #endregion

    #region 关联知识点测试

    [Fact]
    public async Task GenerateAsync_ShouldReturnEmptyRelatedKpIds()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        var snippets = CreateTestSnippets();

        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<IEnumerable<string>>()))
            .Returns(snippets);

        var llmResponse = CreateValidLLMResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<LearningContentResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _generator.GenerateAsync(kp, CancellationToken.None);

        // Assert
        result.RelatedKpIds.Should().NotBeNull();
        result.RelatedKpIds.Should().BeEmpty(); // MVP 阶段暂不实现
    }

    #endregion

    #region Helper Methods

    private static KnowledgePoint CreateTestKnowledgePoint()
    {
        return new KnowledgePoint
        {
            KpId = "kp_001",
            BookRootId = "book_001",
            Title = "什么是 Markdown",
            Aliases = new List<string> { "MD", "标记语言" },
            ChapterPath = new List<string> { "第一章", "1.1 基础概念" },
            Importance = 0.8f,
            SnippetIds = new List<string> { "snippet_001", "snippet_002" },
            Relations = new List<KnowledgeRelation>()
        };
    }

    private static List<SourceSnippet> CreateTestSnippets()
    {
        return new List<SourceSnippet>
        {
            new SourceSnippet
            {
                SnippetId = "snippet_001",
                BookRootId = "book_001",
                DocId = "doc_001",
                FilePath = "/docs/ch01.md",
                HeadingPath = new List<string> { "第一章", "1.1 基础概念" },
                Content = "Markdown 是一种轻量级标记语言，由 John Gruber 于 2004 年创建。它的设计目标是易读易写。Markdown 语法简单直观，通过简单的符号来标记文本格式。",
                StartLine = 10,
                EndLine = 15
            },
            new SourceSnippet
            {
                SnippetId = "snippet_002",
                BookRootId = "book_001",
                DocId = "doc_001",
                FilePath = "/docs/ch01.md",
                HeadingPath = new List<string> { "第一章", "1.2 标题语法" },
                Content = "Markdown 支持六级标题，使用 # 符号表示。# 后跟空格再跟标题文本。一级标题使用一个 #，二级标题使用两个 #，以此类推。",
                StartLine = 20,
                EndLine = 25
            }
        };
    }

    private static LearningContentResponse CreateValidLLMResponse()
    {
        return new LearningContentResponse
        {
            Summary = new Summary
            {
                Definition = "Markdown 是一种轻量级标记语言，设计目标是让文档易读易写。",
                KeyPoints = new List<string>
                {
                    "使用 # 符号表示标题，支持六级",
                    "使用 *、- 或数字列表表示列表",
                    "使用 **text** 表示粗体，*text* 表示斜体"
                },
                Pitfalls = new List<string>
                {
                    "Markdown 不是 HTML 的替代品",
                    "不同解析器可能有细微差异"
                }
            },
            Levels = new List<ContentLevel>
            {
                new ContentLevel { Level = 1, Title = "概览", Content = "Markdown 是一种轻量级标记语言..." },
                new ContentLevel { Level = 2, Title = "详细", Content = "Markdown 的基本语法包括标题、列表、链接..." },
                new ContentLevel { Level = 3, Title = "深入", Content = "深入了解 Markdown 的扩展语法和最佳实践..." }
            }
        };
    }

    #endregion
}
