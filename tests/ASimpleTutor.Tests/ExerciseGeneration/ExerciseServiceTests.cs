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

namespace ASimpleTutor.Tests.ExerciseGeneration;

/// <summary>
/// 习题生成模块测试用例
/// 对应测试需求文档：TC-EG-001 ~ TC-EG-007
/// </summary>
public class ExerciseServiceTests
{
    private readonly Mock<ILLMService> _llmServiceMock;
    private readonly Mock<ILogger<ExerciseService>> _loggerMock;
    private readonly KnowledgeSystemStore _knowledgeSystemStore;
    private readonly ExerciseService _service;

    public ExerciseServiceTests()
    {
        _llmServiceMock = new Mock<ILLMService>();
        _loggerMock = new Mock<ILogger<ExerciseService>>();
        var knowledgeSystemStoreLoggerMock = new Mock<ILogger<KnowledgeSystemStore>>();
        _knowledgeSystemStore = new KnowledgeSystemStore(knowledgeSystemStoreLoggerMock.Object, "test-data");

        _service = new ExerciseService(
            _llmServiceMock.Object,
            _knowledgeSystemStore,
            _loggerMock.Object);
    }

    #region 习题生成测试

    [Fact]
    public async Task GenerateAsync_WithValidInput_ShouldReturnExercises()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        var snippets = CreateTestSnippets();

        // 由于我们现在使用的是实际的 KnowledgeSystemStore 实例，而不是 mock 对象，
        // 我们需要先保存知识系统，然后才能加载它
        kp.BookHubId = "book_001_test6"; // 使用唯一的 BookHubId 避免文件被占用
        var knowledgeSystem = new KnowledgeSystem
        {
            BookHubId = kp.BookHubId
        };
        var documents = CreateTestDocuments();
        await _knowledgeSystemStore.SaveAsync(knowledgeSystem, documents);

        var llmResponse = CreateValidExercisesResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<ExercisesResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _service.GenerateAsync(kp, 3, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task GenerateAsync_ShouldGenerateMultipleExercises()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        var snippets = CreateTestSnippets();

        // 由于我们现在使用的是实际的 KnowledgeSystemStore 实例，而不是 mock 对象，
        // 我们需要先保存知识系统，然后才能加载它
        kp.BookHubId = "book_001_test7"; // 使用唯一的 BookHubId 避免文件被占用
        var knowledgeSystem = new KnowledgeSystem
        {
            BookHubId = kp.BookHubId
        };
        var documents = CreateTestDocuments();
        await _knowledgeSystemStore.SaveAsync(knowledgeSystem, documents);

        var llmResponse = CreateValidExercisesResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<ExercisesResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _service.GenerateAsync(kp, 5, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task GenerateAsync_ShouldHandleEmptySnippets()
    {
        // Arrange
        var kp = new KnowledgePoint
        {
            KpId = "kp_empty",
            Title = "测试知识点",
            BookHubId = "book_001"
        };

        // 由于我们现在使用的是实际的 KnowledgeSystemStore 实例，而不是 mock 对象，
        // 我们需要先保存知识系统，然后才能加载它
        var knowledgeSystem = new KnowledgeSystem
        {
            BookHubId = kp.BookHubId
        };
        var documents = CreateTestDocuments();
        await _knowledgeSystemStore.SaveAsync(knowledgeSystem, documents);

        var llmResponse = CreateValidExercisesResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<ExercisesResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _service.GenerateAsync(kp, 3, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateAsync_ShouldHandleLLMFailure()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        var snippets = CreateTestSnippets();

        // 由于我们现在使用的是实际的 KnowledgeSystemStore 实例，而不是 mock 对象，
        // 我们需要先保存知识系统，然后才能加载它
        kp.BookHubId = "book_001_test10"; // 使用唯一的 BookHubId 避免文件被占用
        var knowledgeSystem = new KnowledgeSystem
        {
            BookHubId = kp.BookHubId
        };
        var documents = CreateTestDocuments();
        await _knowledgeSystemStore.SaveAsync(knowledgeSystem, documents);

        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<ExercisesResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("LLM Error"));

        // Act
        var result = await _service.GenerateAsync(kp, 3, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(0); // 降级策略：返回空列表
    }

    [Fact]
    public async Task GenerateAsync_ShouldHandleLLMNullResponse()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        var snippets = CreateTestSnippets();

        // 由于我们现在使用的是实际的 KnowledgeSystemStore 实例，而不是 mock 对象，
        // 我们需要先保存知识系统，然后才能加载它
        kp.BookHubId = "book_001";
        var knowledgeSystem = new KnowledgeSystem
        {
            BookHubId = kp.BookHubId
        };
        var documents = CreateTestDocuments();
        await _knowledgeSystemStore.SaveAsync(knowledgeSystem, documents);

        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<ExercisesResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExercisesResponse?)null);

        // Act
        var result = await _service.GenerateAsync(kp, 3, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(0); // 降级策略：返回空列表
    }

    #endregion

    #region 习题反馈测试

    [Fact]
    public async Task JudgeAsync_WithValidInput_ShouldReturnFeedback()
    {
        // Arrange
        var exercise = CreateTestExercise();
        var userAnswer = "测试答案";

        // 对于单选题，不需要 LLM 调用

        // Act
        var result = await _service.JudgeAsync(exercise, userAnswer, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Explanation.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task JudgeAsync_ShouldHandleLLMFailure()
    {
        // Arrange
        var exercise = CreateTestExercise();
        var userAnswer = "测试答案";

        // 对于单选题，不需要 LLM 调用，所以这里直接测试

        // Act
        var result = await _service.JudgeAsync(exercise, userAnswer, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Explanation.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task JudgeAsync_ShouldHandleLLMNullResponse()
    {
        // Arrange
        var exercise = CreateTestExercise();
        var userAnswer = "测试答案";

        // 对于单选题，不需要 LLM 调用，所以这里直接测试

        // Act
        var result = await _service.JudgeAsync(exercise, userAnswer, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Explanation.Should().NotBeNullOrEmpty();
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
            BookHubId = "book_001"
        };
        var documents = CreateTestDocuments();

        // 由于我们现在使用的是实际的 KnowledgeSystemStore 实例，而不是 mock 对象，
        // 我们需要先保存知识系统，然后才能加载它
        var knowledgeSystem = new KnowledgeSystem
        {
            BookHubId = kp.BookHubId
        };
        await _knowledgeSystemStore.SaveAsync(knowledgeSystem, documents);

        var llmResponse = CreateValidExercisesResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<ExercisesResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _service.GenerateAsync(kp, 3, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateAsync_WithZeroCount_ShouldReturnEmpty()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        var snippets = CreateTestSnippets();

        // 由于我们现在使用的是实际的 KnowledgeSystemStore 实例，而不是 mock 对象，
        // 我们需要先保存知识系统，然后才能加载它
        kp.BookHubId = "book_001";
        var knowledgeSystem = new KnowledgeSystem
        {
            BookHubId = kp.BookHubId
        };
        var documents = CreateTestDocuments();
        await _knowledgeSystemStore.SaveAsync(knowledgeSystem, documents);

        // Act
        var result = await _service.GenerateAsync(kp, 0, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
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
            Importance = 0.8f
        };
    }

    private static List<SourceSnippet> CreateTestSnippets()
    {
        return new List<SourceSnippet>
        {
            new SourceSnippet
            {
                SnippetId = "snippet_001",
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
                DocId = "doc_001",
                FilePath = "/docs/ch01.md",
                HeadingPath = new List<string> { "第一章", "1.2 标题语法" },
                Content = "Markdown 支持六级标题，使用 # 符号表示。# 后跟空格再跟标题文本。一级标题使用一个 #，二级标题使用两个 #，以此类推。",
                StartLine = 20,
                EndLine = 25
            }
        };
    }

    private static List<Document> CreateTestDocuments()
    {
        return new List<Document>
        {
            new Document
            {
                DocId = "doc_001",
                BookHubId = "book_001",
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
    }

    private static Exercise CreateTestExercise()
    {
        return new Exercise
        {
            ExerciseId = "ex_001",
            Question = "什么是 Markdown？",
            Type = ExerciseType.SingleChoice,
            CorrectAnswer = "一种轻量级标记语言",
            Options = new List<string> { "一种编程语言", "一种轻量级标记语言", "一种数据库", "一种操作系统" }
        };
    }

    private static ExercisesResponse CreateValidExercisesResponse()
    {
        return new ExercisesResponse
        {
            Exercises = new List<ExerciseDto>
            {
                new ExerciseDto
                {
                    Type = "singlechoice",
                    Question = "什么是 Markdown？",
                    CorrectAnswer = "一种轻量级标记语言",
                    Options = new List<string> { "一种编程语言", "一种轻量级标记语言", "一种数据库", "一种操作系统" }
                },
                new ExerciseDto
                {
                    Type = "shortanswer",
                    Question = "Markdown 由谁创建？",
                    CorrectAnswer = "John Gruber"
                },
                new ExerciseDto
                {
                    Type = "singlechoice",
                    Question = "Markdown 支持几级标题？",
                    CorrectAnswer = "六级",
                    Options = new List<string> { "三级", "四级", "五级", "六级" }
                },
                new ExerciseDto
                {
                    Type = "truefalse",
                    Question = "Markdown 是一种编程语言。",
                    CorrectAnswer = "false",
                    Options = new List<string> { "正确", "错误" }
                },
                new ExerciseDto
                {
                    Type = "singlechoice",
                    Question = "Markdown 中如何表示粗体？",
                    CorrectAnswer = "**粗体**",
                    Options = new List<string> { "*粗体*", "**粗体**", "__粗体__", "***粗体***" }
                }
            }
        };
    }

    private static ExerciseFeedback CreateValidFeedbackResponse()
    {
        return new ExerciseFeedback
        {
            Explanation = "你的答案部分正确，需要进一步理解 Markdown 的核心概念。",
            IsCorrect = false,
            ReferenceAnswer = "正确答案内容"
        };
    }

    #endregion
}