using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using ASimpleTutor.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ASimpleTutor.Tests.KnowledgeBuilding;

public class KnowledgeBuildingTests
{
    private readonly Mock<ILogger<KnowledgeBuilder>> _loggerMock;
    private readonly Mock<IScannerService> _scannerServiceMock;
    private readonly Mock<ILLMService> _llmServiceMock;
    private readonly Mock<IKnowledgeSystemCoordinator> _coordinatorMock;
    private readonly KnowledgeBuilder _knowledgeBuilder;

    public KnowledgeBuildingTests()
    {
        _loggerMock = new Mock<ILogger<KnowledgeBuilder>>();
        _scannerServiceMock = new Mock<IScannerService>();
        _llmServiceMock = new Mock<ILLMService>();
        _coordinatorMock = new Mock<IKnowledgeSystemCoordinator>();

        _knowledgeBuilder = new KnowledgeBuilder(_coordinatorMock.Object);
    }

    [Fact]
    public async Task BuildAsync_WithValidDocuments_ShouldReturnKnowledgeSystem()
    {
        // Arrange
        var bookRootId = "test_book";
        var rootPath = "/test/path";

        var testDocuments = CreateTestDocuments();

        var mockKnowledgeSystem = new KnowledgeSystem
        {
            BookHubId = bookRootId,
            KnowledgePoints = new List<KnowledgePoint>(),
            Tree = new KnowledgeTreeNode
            {
                Id = "root",
                Title = "Root"
            }
        };

        _coordinatorMock
            .Setup(c => c.BuildAsync(bookRootId, rootPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((mockKnowledgeSystem, testDocuments));

        // Act
        var (knowledgeSystem, documents) = await _knowledgeBuilder.BuildAsync(bookRootId, rootPath);

        // Assert
        Assert.NotNull(knowledgeSystem);
        Assert.Equal(bookRootId, knowledgeSystem.BookHubId);
        Assert.NotNull(knowledgeSystem.Tree);
        Assert.NotNull(documents);
        Assert.Equal(testDocuments.Count, documents.Count);
    }

    [Fact]
    public async Task BuildAsync_WithNoDocuments_ShouldReturnEmptyKnowledgeSystem()
    {
        // Arrange
        var bookRootId = "test_book";
        var rootPath = "/test/path";

        var mockKnowledgeSystem = new KnowledgeSystem
        {
            BookHubId = bookRootId,
            KnowledgePoints = new List<KnowledgePoint>(),
            Tree = new KnowledgeTreeNode
            {
                Id = "root",
                Title = "Root"
            }
        };

        _coordinatorMock
            .Setup(c => c.BuildAsync(bookRootId, rootPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync((mockKnowledgeSystem, new List<Document>()));

        // Act
        var (knowledgeSystem, documents) = await _knowledgeBuilder.BuildAsync(bookRootId, rootPath);

        // Assert
        Assert.NotNull(knowledgeSystem);
        Assert.Equal(bookRootId, knowledgeSystem.BookHubId);
        Assert.NotNull(knowledgeSystem.Tree);
        Assert.NotNull(documents);
        Assert.Empty(documents);
    }

    private static List<Document> CreateTestDocuments()
    {
        return new List<Document>
        {
            new Document
            {
                DocId = "doc1",
                Path = "/test/path/doc1.md",
                Title = "文档1",
                Sections = new List<Section>
                {
                    new Section
                    {
                        SectionId = "sec1",
                        HeadingPath = new List<string> { "第一章", "第一节" },
                        StartLine = 0,
                        EndLine = 100
                    }
                },
                ContentHash = "test_hash"
            }
        };
    }
}