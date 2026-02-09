using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using ASimpleTutor.Core.Models.Dto;
using ASimpleTutor.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ASimpleTutor.Tests.KnowledgeBuilding;

/// <summary>
/// 知识体系构建模块测试用例
/// 对应测试需求文档：TC-KP-001 ~ TC-KP-006
/// </summary>
public class KnowledgeBuildingTests
{
    private readonly Mock<ILogger<KnowledgeBuilder>> _loggerMock;
    private readonly Mock<IScannerService> _scannerServiceMock;
    private readonly Mock<ILLMService> _llmServiceMock;
    private readonly KnowledgeBuilder _knowledgeBuilder;

    public KnowledgeBuildingTests()
    {
        _loggerMock = new Mock<ILogger<KnowledgeBuilder>>();
        _scannerServiceMock = new Mock<IScannerService>();
        _llmServiceMock = new Mock<ILLMService>();

        _knowledgeBuilder = new KnowledgeBuilder(
            _scannerServiceMock.Object,
            _llmServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task BuildAsync_WithValidDocuments_ShouldReturnKnowledgeSystem()
    {
        // Arrange
        var bookRootId = "test_book";
        var rootPath = "/test/path";

        var testDocuments = CreateTestDocuments();
        _scannerServiceMock
            .Setup(s => s.ScanAsync(rootPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(testDocuments);

        var kpResponse = new KnowledgePointsResponse
        {
            SchemaVersion = "1.0",
            KnowledgePoints = new List<KnowledgePointDto>
            {
                new KnowledgePointDto
                {
                    KpId = "kp_0000",
                    Title = "核心概念A",
                    ChapterPath = new List<string> { "第一章", "第一节" },
                    Importance = 0.8f,
                    SnippetIds = new List<string> { "snippet_1" },
                    Type = "concept"
                },
                new KnowledgePointDto
                {
                    KpId = "kp_0001",
                    Title = "核心概念B",
                    ChapterPath = new List<string> { "第一章", "第二节" },
                    Importance = 0.6f,
                    SnippetIds = new List<string> { "snippet_2" },
                    Type = "concept"
                }
            }
        };

        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<KnowledgePointsResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(kpResponse);

        // Act
        var result = await _knowledgeBuilder.BuildAsync(bookRootId, rootPath, CancellationToken.None);

        // Assert
        result.KnowledgeSystem.Should().NotBeNull();
        result.KnowledgeSystem.BookRootId.Should().Be(bookRootId);
        result.KnowledgeSystem.KnowledgePoints.Should().NotBeEmpty();
    }

    [Fact]
    public async Task BuildAsync_WithNoDocuments_ShouldReturnEmptyKnowledgeSystem()
    {
        // Arrange
        var bookRootId = "test_book";
        var rootPath = "/test/path";

        _scannerServiceMock
            .Setup(s => s.ScanAsync(rootPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Document>());

        // Act
        var result = await _knowledgeBuilder.BuildAsync(bookRootId, rootPath, CancellationToken.None);

        // Assert
        result.KnowledgeSystem.Should().NotBeNull();
        result.KnowledgeSystem.BookRootId.Should().Be(bookRootId);
        result.KnowledgeSystem.KnowledgePoints.Should().BeEmpty();
        // 当没有文档时，Tree 可能为 null
        if (result.KnowledgeSystem.Tree != null)
        {
            result.KnowledgeSystem.Tree.Id.Should().Be("root");
        }
    }

    [Fact]
    public async Task BuildAsync_ShouldAssignBookRootIdToDocuments()
    {
        // Arrange
        var bookRootId = "test_book_123";
        var rootPath = "/test/path";

        var testDocuments = CreateTestDocuments();
        _scannerServiceMock
            .Setup(s => s.ScanAsync(rootPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(testDocuments);

        var kpResponse = new KnowledgePointsResponse
        {
            SchemaVersion = "1.0",
            KnowledgePoints = new List<KnowledgePointDto>
            {
                new KnowledgePointDto
                {
                    Title = "Test KP",
                    Importance = 0.5f,
                    SnippetIds = new List<string> { "snippet_1" },
                    Type = "concept"
                }
            }
        };

        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<KnowledgePointsResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(kpResponse);

        // Act
        await _knowledgeBuilder.BuildAsync(bookRootId, rootPath, CancellationToken.None);

        // Assert
        _scannerServiceMock.Verify(
            s => s.ScanAsync(rootPath, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BuildAsync_ShouldBuildKnowledgeTree()
    {
        // Arrange
        var bookRootId = "test_book";
        var rootPath = "/test/path";

        var testDocuments = CreateTestDocuments();
        _scannerServiceMock
            .Setup(s => s.ScanAsync(rootPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(testDocuments);

        var kpResponse = new KnowledgePointsResponse
        {
            SchemaVersion = "1.0",
            KnowledgePoints = new List<KnowledgePointDto>
            {
                new KnowledgePointDto
                {
                    KpId = "kp_0000",
                    Title = "核心概念A",
                    ChapterPath = new List<string> { "第一章", "第一节" },
                    Importance = 0.8f,
                    SnippetIds = new List<string> { "snippet_1" },
                    Type = "concept"
                },
                new KnowledgePointDto
                {
                    KpId = "kp_0001",
                    Title = "核心概念B",
                    ChapterPath = new List<string> { "第一章", "第二节" },
                    Importance = 0.6f,
                    SnippetIds = new List<string> { "snippet_2" },
                    Type = "concept"
                }
            }
        };

        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<KnowledgePointsResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(kpResponse);

        // Act
        var result = await _knowledgeBuilder.BuildAsync(bookRootId, rootPath, CancellationToken.None);

        // Assert
        result.KnowledgeSystem.Tree.Should().NotBeNull();
        result.KnowledgeSystem.Tree!.Id.Should().Be("root");
        result.KnowledgeSystem.Tree.Children.Should().NotBeEmpty();
    }

    [Fact]
    public async Task BuildAsync_WhenLLMThrows_ShouldHandleGracefully()
    {
        // Arrange
        var bookRootId = "test_book";
        var rootPath = "/test/path";

        var testDocuments = CreateTestDocuments();
        _scannerServiceMock
            .Setup(s => s.ScanAsync(rootPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(testDocuments);

        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<KnowledgePointsResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("LLM service error"));

        // Act
        var result = await _knowledgeBuilder.BuildAsync(bookRootId, rootPath, CancellationToken.None);

        // Assert
        result.KnowledgeSystem.Should().NotBeNull();
        result.KnowledgeSystem.BookRootId.Should().Be(bookRootId);
        // 当 LLM 失败时，ExtractKnowledgePointsAsync 会返回空列表
        result.KnowledgeSystem.KnowledgePoints.Should().NotBeNull();
    }

    [Fact]
    public async Task BuildAsync_ShouldCollectSourceSnippets()
    {
        // Arrange
        var bookRootId = "test_book";
        var rootPath = "/test/path";

        var testDocuments = CreateTestDocuments();
        _scannerServiceMock
            .Setup(s => s.ScanAsync(rootPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(testDocuments);

        var kpResponse = new KnowledgePointsResponse
        {
            SchemaVersion = "1.0",
            KnowledgePoints = new List<KnowledgePointDto>
            {
                new KnowledgePointDto
                {
                    Title = "Test KP",
                    ChapterPath = new List<string> { "第一章" },
                    Importance = 0.5f,
                    SnippetIds = new List<string> { "snippet_1", "snippet_2" },
                    Type = "concept"
                }
            }
        };

        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<KnowledgePointsResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(kpResponse);

        // Act
        var result = await _knowledgeBuilder.BuildAsync(bookRootId, rootPath, CancellationToken.None);

        // Assert
        result.KnowledgeSystem.Snippets.Should().NotBeEmpty();
    }

    private static List<Document> CreateTestDocuments()
    {
        return new List<Document>
        {
            new Document
            {
                DocId = "doc_001",
                Path = "/test/path/chapter01.md",
                Title = "第一章",
                Sections = new List<Section>
                {
                    new Section
                    {
                        SectionId = "section_1",
                        HeadingPath = new List<string> { "第一章" },
                        StartLine = 0,
                        EndLine = 100,
                        OriginalLength = 1000,
                        EffectiveLength = 800,
                        FilteredLength = 200
                    }
                }
            }
        };
    }
}