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

namespace ASimpleTutor.Tests.LearningGeneration;

/// <summary>
/// 学习内容生成模块测试用例
/// 对应测试需求文档：TC-LG-001 ~ TC-LG-007
/// </summary>
public class LearningGeneratorTests
{
    private readonly Mock<ILLMService> _llmServiceMock;
    private readonly Mock<ILogger<LearningGenerator>> _loggerMock;
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly KnowledgeSystemStore _knowledgeSystemStore;
    private readonly LearningGenerator _generator;

    public LearningGeneratorTests()
    {
        _llmServiceMock = new Mock<ILLMService>();
        _loggerMock = new Mock<ILogger<LearningGenerator>>();
        _settingsServiceMock = new Mock<ISettingsService>();
        var knowledgeSystemStoreLoggerMock = new Mock<ILogger<KnowledgeSystemStore>>();
        _knowledgeSystemStore = new KnowledgeSystemStore(knowledgeSystemStoreLoggerMock.Object, "test-data");

        // 设置 settings service 的默认返回值
        _settingsServiceMock
            .Setup(s => s.GetTtsSettingsAsync())
            .ReturnsAsync(new TtsSettingsResponse
            {
                Enabled = false,
                ApiKeyMasked = "test_key",
                BaseUrl = "http://localhost:11434",
                Voice = "test_voice",
                Speed = 1.0f,
                IsValid = true
            });

        _generator = new LearningGenerator(
            _knowledgeSystemStore,
            _llmServiceMock.Object,
            _loggerMock.Object,
            _settingsServiceMock.Object);
    }

    #region 正常生成测试

    [Fact]
    public async Task GenerateAsync_WithValidInput_ShouldReturnLearningPack()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();

        // 由于我们现在使用的是实际的 KnowledgeSystemStore 实例，而不是 mock 对象，
        // 我们需要先保存知识系统，然后才能加载它
        kp.BookHubId = "book_001_test9"; // 使用唯一的 BookHubId 避免文件被占用
        var knowledgeSystem = new KnowledgeSystem
        {
            BookHubId = kp.BookHubId
        };
        var (testFilePath, documents) = CreateTestDocuments(kp.BookHubId);
        await _knowledgeSystemStore.SaveAsync(knowledgeSystem, documents);

        var llmResponse = CreateValidLLMResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<LearningContentDto>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _generator.GenerateAsync(kp, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.KpId.Should().Be(kp.KpId);
    }

    [Fact]
    public async Task GenerateAsync_ShouldGenerateSummaryWithDefinition()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();

        // 由于我们现在使用的是实际的 KnowledgeSystemStore 实例，而不是 mock 对象，
        // 我们需要先保存知识系统，然后才能加载它
        kp.BookHubId = "book_001_test11"; // 使用唯一的 BookHubId 避免文件被占用
        var knowledgeSystem = new KnowledgeSystem
        {
            BookHubId = kp.BookHubId
        };
        var (testFilePath, documents) = CreateTestDocuments(kp.BookHubId);
        await _knowledgeSystemStore.SaveAsync(knowledgeSystem, documents);

        var llmResponse = CreateValidLLMResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<LearningContentDto>(
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

        // 由于我们现在使用的是实际的 KnowledgeSystemStore 实例，而不是 mock 对象，
        // 我们需要先保存知识系统，然后才能加载它
        kp.BookHubId = "book_001_test12"; // 使用唯一的 BookHubId 避免文件被占用
        var knowledgeSystem = new KnowledgeSystem
        {
            BookHubId = kp.BookHubId
        };
        var documents = new List<Document>
        {
            new Document
            {
                DocId = "doc_001",
                BookHubId = kp.BookHubId,
                Path = "/docs/ch01.md",
                Title = "第一章 基础概念",
                Sections = new List<Section>
                {
                    new Section
                    {
                        SectionId = "section_001",
                        HeadingPath = new List<string> { "第一章", "1.1 基础概念" },
                        StartLine = 10,
                        EndLine = 15,
                        OriginalLength = 100,
                        EffectiveLength = 80,
                        FilteredLength = 20,
                        IsExcluded = false
                    },
                    new Section
                    {
                        SectionId = "section_002",
                        HeadingPath = new List<string> { "第一章", "1.2 标题语法" },
                        StartLine = 20,
                        EndLine = 25,
                        OriginalLength = 100,
                        EffectiveLength = 80,
                        FilteredLength = 20,
                        IsExcluded = false
                    }
                }
            }
        };
        await _knowledgeSystemStore.SaveAsync(knowledgeSystem, documents);

        var llmResponse = CreateValidLLMResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<LearningContentDto>(
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

        // 由于我们现在使用的是实际的 KnowledgeSystemStore 实例，而不是 mock 对象，
        // 我们需要先保存知识系统，然后才能加载它
        kp.BookHubId = "book_001_test13"; // 使用唯一的 BookHubId 避免文件被占用
        var knowledgeSystem = new KnowledgeSystem
        {
            BookHubId = kp.BookHubId
        };
        var (testFilePath, documents) = CreateTestDocuments(kp.BookHubId);
        await _knowledgeSystemStore.SaveAsync(knowledgeSystem, documents);

        var llmResponse = CreateValidLLMResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<LearningContentDto>(
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

        // 由于我们现在使用的是实际的 KnowledgeSystemStore 实例，而不是 mock 对象，
        // 我们需要先保存知识系统，然后才能加载它
        kp.BookHubId = "book_001_test14"; // 使用唯一的 BookHubId 避免文件被占用
        var knowledgeSystem = new KnowledgeSystem
        {
            BookHubId = kp.BookHubId
        };
        var (testFilePath, documents) = CreateTestDocuments(kp.BookHubId);
        await _knowledgeSystemStore.SaveAsync(knowledgeSystem, documents);

        var llmResponse = CreateValidLLMResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<LearningContentDto>(
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

        // 由于我们现在使用的是实际的 KnowledgeSystemStore 实例，而不是 mock 对象，
        // 我们需要先保存知识系统，然后才能加载它
        kp.BookHubId = "book_001_test15"; // 使用唯一的 BookHubId 避免文件被占用
        var knowledgeSystem = new KnowledgeSystem
        {
            BookHubId = kp.BookHubId
        };
        var (testFilePath, documents) = CreateTestDocuments(kp.BookHubId);
        await _knowledgeSystemStore.SaveAsync(knowledgeSystem, documents);

        var llmResponse = CreateValidLLMResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<LearningContentDto>(
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

    #region 降级策略测试

    [Fact]
    public async Task GenerateAsync_WhenLLMFails_ShouldUseFallback()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();

        // 由于我们现在使用的是实际的 KnowledgeSystemStore 实例，而不是 mock 对象，
        // 我们需要先保存知识系统，然后才能加载它
        kp.BookHubId = "book_001_test16"; // 使用唯一的 BookHubId 避免文件被占用
        var knowledgeSystem = new KnowledgeSystem
        {
            BookHubId = kp.BookHubId
        };
        var (testFilePath, documents) = CreateTestDocuments(kp.BookHubId);
        await _knowledgeSystemStore.SaveAsync(knowledgeSystem, documents);

        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<LearningContentDto>(
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

        // 由于我们现在使用的是实际的 KnowledgeSystemStore 实例，而不是 mock 对象，
        // 我们需要先保存知识系统，然后才能加载它
        kp.BookHubId = "book_001_test17"; // 使用唯一的 BookHubId 避免文件被占用
        var knowledgeSystem = new KnowledgeSystem
        {
            BookHubId = kp.BookHubId
        };
        var (testFilePath, documents) = CreateTestDocuments(kp.BookHubId);
        await _knowledgeSystemStore.SaveAsync(knowledgeSystem, documents);

        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<LearningContentDto>(
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

        // 由于我们现在使用的是实际的 KnowledgeSystemStore 实例，而不是 mock 对象，
        // 我们需要先保存知识系统，然后才能加载它
        kp.BookHubId = "book_001_test18"; // 使用唯一的 BookHubId 避免文件被占用
        var knowledgeSystem = new KnowledgeSystem
        {
            BookHubId = kp.BookHubId
        };
        var (testFilePath, documents) = CreateTestDocuments(kp.BookHubId);
        await _knowledgeSystemStore.SaveAsync(knowledgeSystem, documents);

        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<LearningContentDto>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("LLM Error"));

        // Act
        var result = await _generator.GenerateAsync(kp, CancellationToken.None);

        // Assert
        result.Summary.KeyPoints.Should().NotBeNull();
        // 降级策略会从文档内容提取句子作为要点
    }

    [Fact]
    public async Task GenerateAsync_WhenLLMReturnsNull_ShouldUseFallback()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();

        // 由于我们现在使用的是实际的 KnowledgeSystemStore 实例，而不是 mock 对象，
        // 我们需要先保存知识系统，然后才能加载它
        kp.BookHubId = "book_001_test19"; // 使用唯一的 BookHubId 避免文件被占用
        var knowledgeSystem = new KnowledgeSystem
        {
            BookHubId = kp.BookHubId
        };
        var documents = new List<Document>
        {
            new Document
            {
                DocId = "doc_001",
                BookHubId = kp.BookHubId,
                Path = "/docs/ch01.md",
                Title = "第一章 基础概念",
                Sections = new List<Section>
                {
                    new Section
                    {
                        SectionId = "section_001",
                        HeadingPath = new List<string> { "第一章", "1.1 基础概念" },
                        StartLine = 10,
                        EndLine = 15,
                        OriginalLength = 100,
                        EffectiveLength = 80,
                        FilteredLength = 20,
                        IsExcluded = false
                    },
                    new Section
                    {
                        SectionId = "section_002",
                        HeadingPath = new List<string> { "第一章", "1.2 标题语法" },
                        StartLine = 20,
                        EndLine = 25,
                        OriginalLength = 100,
                        EffectiveLength = 80,
                        FilteredLength = 20,
                        IsExcluded = false
                    }
                }
            }
        };
        await _knowledgeSystemStore.SaveAsync(knowledgeSystem, documents);

        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<LearningContentDto>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((LearningContentDto?)null);

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
            BookHubId = "book_001_test20"
        };

        // 由于我们现在使用的是实际的 KnowledgeSystemStore 实例，而不是 mock 对象，
        // 我们需要先保存知识系统，然后才能加载它
        var knowledgeSystem = new KnowledgeSystem
        {
            BookHubId = kp.BookHubId
        };
        var (testFilePath, documents) = CreateTestDocuments(kp.BookHubId);
        await _knowledgeSystemStore.SaveAsync(knowledgeSystem, documents);

        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<LearningContentDto>(
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
    public async Task GenerateAsync_WithNoDocuments_ShouldUseFallback()
    {
        // Arrange
        var kp = new KnowledgePoint
        {
            KpId = "kp_003",
            Title = "无文档知识点",
            ChapterPath = new List<string> { "第一章" },
            Importance = 0.5f,
            BookHubId = "book_001_test21"
        };

        // 由于我们现在使用的是实际的 KnowledgeSystemStore 实例，而不是 mock 对象，
        // 我们需要先保存知识系统，然后才能加载它
        var knowledgeSystem = new KnowledgeSystem
        {
            BookHubId = kp.BookHubId
        };
        var documents = new List<Document>();
        await _knowledgeSystemStore.SaveAsync(knowledgeSystem, documents);

        // Act
        var result = await _generator.GenerateAsync(kp, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region 关联知识点测试

    [Fact]
    public async Task GenerateAsync_ShouldReturnEmptyRelatedKpIds()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();

        // 由于我们现在使用的是实际的 KnowledgeSystemStore 实例，而不是 mock 对象，
        // 我们需要先保存知识系统，然后才能加载它
        kp.BookHubId = "book_001_test22"; // 使用唯一的 BookHubId 避免文件被占用
        var knowledgeSystem = new KnowledgeSystem
        {
            BookHubId = kp.BookHubId
        };
        var (testFilePath, documents) = CreateTestDocuments(kp.BookHubId);
        await _knowledgeSystemStore.SaveAsync(knowledgeSystem, documents);

        var llmResponse = CreateValidLLMResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<LearningContentDto>(
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
            BookHubId = "book_001",
            Title = "什么是 Markdown",
            Aliases = new List<string> { "MD", "标记语言" },
            ChapterPath = new List<string> { "第一章", "1.1 基础概念" },
            Importance = 0.8f,
            DocId = "doc_001",
            SectionId = "section_001" // 添加正确的 SectionId
        };
    }

    private static LearningContentDto CreateValidLLMResponse()
    {
        return new LearningContentDto
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

    private static (string filePath, List<Document> documents) CreateTestDocuments(string bookHubId)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ASimpleTutorTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var filePath = Path.Combine(tempDir, "test_chapter.md");
        File.WriteAllText(filePath, @"# 第一章 基础概念

## 1.1 基础概念

Markdown 是一种轻量级标记语言，由 John Gruber 于 2004 年创建。它的设计目标是易读易写。Markdown 语法简单直观，通过简单的符号来标记文本格式。Markdown 支持粗体、斜体、标题、列表、链接、图片等基本格式，可以方便地转换为 HTML、PDF 等格式。

## 1.2 标题语法

Markdown 支持六级标题，使用 # 符号表示。# 后跟空格再跟标题文本。一级标题使用一个 #，二级标题使用两个 #，以此类推。Markdown 的标题语法使得文档结构清晰，便于阅读和导航。在使用标题时，建议按照层次结构组织内容，避免跳级使用标题。

## 1.3 扩展语法

Markdown 的扩展语法包括表格、删除线、任务列表等，这些语法通过不同的 Markdown 解析器实现。常见的扩展包括 GitHub Flavored Markdown (GFM)、CommonMark 等，这些扩展增强了 Markdown 的功能，使其更适合编写技术文档和协作。");

        var documents = new List<Document>
        {
            new Document
            {
                DocId = "doc_001",
                BookHubId = bookHubId,
                Path = filePath,
                Title = "第一章 基础概念",
                Sections = new List<Section>
                {
                    new Section
                    {
                        SectionId = "section_001",
                        HeadingPath = new List<string> { "第一章", "1.1 基础概念" },
                        StartLine = 4,
                        EndLine = 5,
                        OriginalLength = 100,
                        EffectiveLength = 80,
                        FilteredLength = 20,
                        IsExcluded = false
                    },
                    new Section
                    {
                        SectionId = "section_002",
                        HeadingPath = new List<string> { "第一章", "1.2 标题语法" },
                        StartLine = 7,
                        EndLine = 8,
                        OriginalLength = 100,
                        EffectiveLength = 80,
                        FilteredLength = 20,
                        IsExcluded = false
                    }
                }
            }
        };

        return (filePath, documents);
    }

    #endregion
}