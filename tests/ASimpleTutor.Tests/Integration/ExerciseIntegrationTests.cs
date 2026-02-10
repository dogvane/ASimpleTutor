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

namespace ASimpleTutor.Tests.Integration;

/// <summary>
/// 习题生成与反馈集成测试
/// 对应测试需求文档：TC-INT-001 ~ TC-INT-005
/// </summary>
public class ExerciseIntegrationTests
{
    private readonly Mock<ILLMService> _llmServiceMock;
    private readonly Mock<ILogger<ExerciseService>> _loggerMock;
    private readonly KnowledgeSystemStore _knowledgeSystemStore;
    private readonly ExerciseService _exerciseService;

    public ExerciseIntegrationTests()
    {
        _llmServiceMock = new Mock<ILLMService>();
        _loggerMock = new Mock<ILogger<ExerciseService>>();
        var knowledgeSystemStoreLoggerMock = new Mock<ILogger<KnowledgeSystemStore>>();
        _knowledgeSystemStore = new KnowledgeSystemStore(knowledgeSystemStoreLoggerMock.Object, "test-data");

        _exerciseService = new ExerciseService(
            _llmServiceMock.Object,
            _knowledgeSystemStore,
            _loggerMock.Object);
    }

    #region 正常流程集成测试

    [Fact]
    public async Task GenerateAndJudge_EndToEnd_Success()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        // 由于我们现在使用的是实际的 KnowledgeSystemStore 实例，而不是 mock 对象，
        // 我们需要先保存知识系统，然后才能加载它
        kp.BookHubId = "book_001_test1";
        var knowledgeSystem = new KnowledgeSystem
        {
            BookHubId = kp.BookHubId
        };
        var documents = CreateTestDocuments();
        await _knowledgeSystemStore.SaveAsync(knowledgeSystem, documents);

        var exercisesResponse = CreateValidExercisesResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<ExercisesResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(exercisesResponse);

        // Act - 生成习题
        var exercises = await _exerciseService.GenerateAsync(kp, 3, CancellationToken.None);

        // Assert - 生成成功
        exercises.Should().NotBeNull();
        exercises.Should().HaveCountGreaterThan(0);

        // Act - 评判答案
        var exercise = exercises.First();
        var feedback = await _exerciseService.JudgeAsync(exercise, "测试答案", CancellationToken.None);

        // Assert - 评判成功
        feedback.Should().NotBeNull();
        feedback.Explanation.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateMultipleExercises_EndToEnd_Success()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        kp.BookHubId = "book_001_test2"; // 使用唯一的 BookHubId 避免文件被占用

        // 由于我们现在使用的是实际的 KnowledgeSystemStore 实例，而不是 mock 对象，
        // 我们需要先保存知识系统，然后才能加载它
        var knowledgeSystem = new KnowledgeSystem
        {
            BookHubId = kp.BookHubId
        };
        var documents = CreateTestDocuments();
        await _knowledgeSystemStore.SaveAsync(knowledgeSystem, documents);

        var exercisesResponse = CreateValidExercisesResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<ExercisesResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(exercisesResponse);

        // Act - 生成习题
        var exercises = await _exerciseService.GenerateAsync(kp, 5, CancellationToken.None);

        // Assert - 生成成功
        exercises.Should().NotBeNull();
        exercises.Should().HaveCount(5);

        // Act - 对每个习题进行评判
        foreach (var exercise in exercises)
        {
            var feedback = await _exerciseService.JudgeAsync(exercise, "测试答案", CancellationToken.None);
            feedback.Should().NotBeNull();
            feedback.Explanation.Should().NotBeNullOrEmpty();
        }
    }

    #endregion

    #region 异常流程集成测试

    [Fact]
    public async Task GenerateAndJudge_WhenLLMGeneratesFails_ShouldHandleGracefully()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        // 由于我们现在使用的是实际的 KnowledgeSystemStore 实例，而不是 mock 对象，
        // 我们需要先保存知识系统，然后才能加载它
        kp.BookHubId = "book_001_test3";
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

        // Act - 生成习题
        var exercises = await _exerciseService.GenerateAsync(kp, 3, CancellationToken.None);

        // Assert - 生成失败时返回空列表
        exercises.Should().NotBeNull();
        exercises.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateAndJudge_WhenSnippetsNotAvailable_ShouldStillWork()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        kp.BookHubId = "book_001_test4"; // 使用唯一的 BookHubId 避免文件被占用

        // 由于我们现在使用的是实际的 KnowledgeSystemStore 实例，而不是 mock 对象，
        // 我们需要先保存知识系统，然后才能加载它
        var knowledgeSystem = new KnowledgeSystem
        {
            BookHubId = kp.BookHubId
        };
        var documents = new List<Document>();
        await _knowledgeSystemStore.SaveAsync(knowledgeSystem, documents);

        var exercisesResponse = CreateValidExercisesResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<ExercisesResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(exercisesResponse);

        // Act - 生成习题
        var exercises = await _exerciseService.GenerateAsync(kp, 3, CancellationToken.None);

        // Assert - 没有片段时使用降级策略生成习题
        exercises.Should().NotBeNull();
        exercises.Should().NotBeEmpty();
    }

    #endregion

    #region 边界条件集成测试

    [Fact]
    public async Task GenerateAndJudge_WithZeroCount_ShouldReturnEmpty()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();

        // Act - 生成习题
        var exercises = await _exerciseService.GenerateAsync(kp, 0, CancellationToken.None);

        // Assert - 返回空列表
        exercises.Should().NotBeNull();
        exercises.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateAndJudge_WithEmptyKnowledgePoint_ShouldHandleGracefully()
    {
        // Arrange
        var kp = new KnowledgePoint
        {
            KpId = "kp_empty",
            Title = "",
            BookHubId = "book_001_test5" // 使用唯一的 BookHubId 避免文件被占用
        };

        // 由于我们现在使用的是实际的 KnowledgeSystemStore 实例，而不是 mock 对象，
        // 我们需要先保存知识系统，然后才能加载它
        var knowledgeSystem = new KnowledgeSystem
        {
            BookHubId = kp.BookHubId
        };
        var documents = new List<Document>();
        await _knowledgeSystemStore.SaveAsync(knowledgeSystem, documents);

        var exercisesResponse = CreateValidExercisesResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<ExercisesResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(exercisesResponse);

        // Act - 生成习题
        var exercises = await _exerciseService.GenerateAsync(kp, 3, CancellationToken.None);

        // Assert - 知识点为空时使用降级策略生成习题
        exercises.Should().NotBeNull();
        exercises.Should().NotBeEmpty();
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

    #endregion
}