using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using ASimpleTutor.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;

namespace ASimpleTutor.Tests.Integration;

/// <summary>
/// 习题生成与练习反馈集成测试用例
/// 对应测试需求文档：TC-INT-001 ~ TC-INT-004
/// </summary>
public class ExerciseIntegrationTests
{
    private readonly Mock<ISimpleRagService> _ragServiceMock;
    private readonly Mock<ISourceTracker> _sourceTrackerMock;
    private readonly Mock<ILLMService> _llmServiceMock;
    private readonly Mock<ILogger<ExerciseService>> _loggerMock;
    private readonly ExerciseService _exerciseService;

    public ExerciseIntegrationTests()
    {
        _ragServiceMock = new Mock<ISimpleRagService>();
        _sourceTrackerMock = new Mock<ISourceTracker>();
        _llmServiceMock = new Mock<ILLMService>();
        _loggerMock = new Mock<ILogger<ExerciseService>>();

        _exerciseService = new ExerciseService(
            _ragServiceMock.Object,
            _sourceTrackerMock.Object,
            _llmServiceMock.Object,
            _loggerMock.Object);
    }

    #region 完整练习流程测试 - TC-INT-001

    [Fact]
    public async Task FullExerciseFlow_ShouldCompleteSuccessfully()
    {
        // Arrange - 完整流程测试：生成习题 -> 作答 -> 获取反馈
        var kp = CreateTestKnowledgePoint();
        var snippets = CreateTestSnippets();

        // 1. 设置原文片段获取
        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<IEnumerable<string>>()))
            .Returns(snippets);

        // 2. 设置习题生成响应
        var exerciseResponse = new ExercisesResponse
        {
            Exercises = new List<ExerciseDto>
            {
                new ExerciseDto
                {
                    Type = "SingleChoice",
                    Question = "关于智能体的描述，以下正确的是？",
                    Options = new List<string> { "只能被动响应", "能够感知并行动", "不需要计算", "只能用于游戏" },
                    CorrectAnswer = "B",
                    KeyPoints = new List<string> { "感知环境", "采取行动" },
                    Explanation = "智能体能够感知环境并自主采取行动"
                }
            }
        };
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<ExercisesResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(exerciseResponse);

        // Act - 步骤1: 生成习题
        var exercises = await _exerciseService.GenerateAsync(kp, count: 1, CancellationToken.None);

        // Assert - 验证习题生成
        exercises.Should().ContainSingle();
        var exercise = exercises[0];
        exercise.EvidenceSnippetIds.Should().NotBeEmpty();

        // Act - 步骤2: 用户作答（正确答案）
        var feedback = await _exerciseService.JudgeAsync(exercise, "B", CancellationToken.None);

        // Assert - 验证反馈
        feedback.IsCorrect.Should().BeTrue();
        feedback.Explanation.Should().Contain("正确");
    }

    [Fact]
    public async Task FullExerciseFlow_WithWrongAnswer_ShouldReturnCorrectFeedback()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        var snippets = CreateTestSnippets();

        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<IEnumerable<string>>()))
            .Returns(snippets);

        var exerciseResponse = new ExercisesResponse
        {
            Exercises = new List<ExerciseDto>
            {
                new ExerciseDto
                {
                    Type = "SingleChoice",
                    Question = "关于智能体的描述，以下正确的是？",
                    Options = new List<string> { "只能被动响应", "能够感知并行动", "不需要计算", "只能用于游戏" },
                    CorrectAnswer = "B",
                    KeyPoints = new List<string> { "感知环境", "采取行动" },
                    Explanation = "智能体能够感知环境并自主采取行动"
                }
            }
        };
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<ExercisesResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(exerciseResponse);

        // Act
        var exercises = await _exerciseService.GenerateAsync(kp, count: 1, CancellationToken.None);
        var exercise = exercises[0];
        var feedback = await _exerciseService.JudgeAsync(exercise, "A", CancellationToken.None);

        // Assert
        feedback.IsCorrect.Should().BeFalse();
        feedback.ReferenceAnswer.Should().Be("B");
        feedback.Explanation.Should().Contain("错误");
    }

    #endregion

    #region 多知识点习题生成测试 - TC-INT-002

    [Fact]
    public async Task MultipleKnowledgePoints_ShouldGenerateSeparateExercises()
    {
        // Arrange - 多知识点批量生成
        var kp1 = new KnowledgePoint
        {
            KpId = "kp_agent",
            Title = "智能体的定义",
            ChapterPath = new List<string> { "第一章" },
            SnippetIds = new List<string> { "snippet_001" }
        };

        var kp2 = new KnowledgePoint
        {
            KpId = "kp_llm",
            Title = "大语言模型的概念",
            ChapterPath = new List<string> { "第二章" },
            SnippetIds = new List<string> { "snippet_002" }
        };

        var snippets = CreateTestSnippets();

        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<IEnumerable<string>>()))
            .Returns(snippets);

        // 使用回调函数每次返回不同的响应对象
        int callCount = 0;
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<ExercisesResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return new ExercisesResponse
                {
                    Exercises = new List<ExerciseDto>
                    {
                        new ExerciseDto { Type = "SingleChoice", Question = "测试题", CorrectAnswer = "A", KeyPoints = new List<string>(), Explanation = "解释" }
                    }
                };
            });

        // Act
        var exercises1 = await _exerciseService.GenerateAsync(kp1, count: 1, CancellationToken.None);
        var exercises2 = await _exerciseService.GenerateAsync(kp2, count: 1, CancellationToken.None);

        // Assert
        exercises1.Should().ContainSingle();
        exercises2.Should().ContainSingle();
        exercises1[0].KpId.Should().Be("kp_agent");
        exercises2[0].KpId.Should().Be("kp_llm");
    }

    #endregion

    #region 习题与学习内容一致性测试 - TC-INT-003

    [Fact]
    public async Task Exercise_ShouldBeBasedOnEvidenceSnippets()
    {
        // Arrange - 习题内容应与原文片段一致
        var kp = CreateTestKnowledgePoint();
        var snippets = new List<SourceSnippet>
        {
            new SourceSnippet
            {
                SnippetId = "snippet_001",
                Content = "智能体（Agent）是一个能够感知环境并采取行动的系统。",
                FilePath = "/docs/ch01.md",
                HeadingPath = new List<string> { "第一章", "基本概念" },
                StartLine = 10,
                EndLine = 12
            }
        };

        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<IEnumerable<string>>()))
            .Returns(snippets);

        // 模拟 LLM 生成基于原文内容的习题
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<ExercisesResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string systemPrompt, string userMessage, CancellationToken ct) =>
            {
                // 验证用户消息包含原文内容
                userMessage.Should().Contain("智能体");
                userMessage.Should().Contain("感知环境");

                return new ExercisesResponse
                {
                    Exercises = new List<ExerciseDto>
                    {
                        new ExerciseDto
                        {
                            Type = "SingleChoice",
                            Question = "智能体能够做什么？",
                            Options = new List<string> { "A", "B", "C", "D" },
                            CorrectAnswer = "A",
                            KeyPoints = new List<string> { "感知环境" },
                            Explanation = "智能体能够感知环境"
                        }
                    }
                };
            });

        // Act
        var exercises = await _exerciseService.GenerateAsync(kp, count: 1, CancellationToken.None);

        // Assert
        exercises.Should().ContainSingle();
        exercises[0].EvidenceSnippetIds.Should().Contain("snippet_001");
    }

    #endregion

    #region 原文跳转功能测试 - TC-INT-004

    [Fact]
    public async Task Feedback_ShouldReferenceOriginalSource()
    {
        // Arrange - 反馈应包含原文引用信息
        var kp = CreateTestKnowledgePoint();
        var snippets = CreateTestSnippets();

        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<IEnumerable<string>>()))
            .Returns(snippets);

        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<ExercisesResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExercisesResponse
            {
                Exercises = new List<ExerciseDto>
                {
                    new ExerciseDto
                    {
                        Type = "SingleChoice",
                        Question = "测试题",
                        Options = new List<string> { "A", "B", "C", "D" },
                        CorrectAnswer = "A",
                        KeyPoints = new List<string>(),
                        Explanation = "解释"
                    }
                }
            });

        // Act
        var exercises = await _exerciseService.GenerateAsync(kp, count: 1, CancellationToken.None);
        var exercise = exercises[0];

        // Assert - 验证习题关联原文片段
        exercise.EvidenceSnippetIds.Should().NotBeEmpty();
        exercise.EvidenceSnippetIds.Should().Contain("snippet_001");
        exercise.EvidenceSnippetIds.Should().Contain("snippet_002");
    }

    [Fact]
    public async Task ExerciseId_ShouldBeTraceable()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        var snippets = CreateTestSnippets();

        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<IEnumerable<string>>()))
            .Returns(snippets);

        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<ExercisesResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExercisesResponse
            {
                Exercises = new List<ExerciseDto>
                {
                    new ExerciseDto
                    {
                        Type = "SingleChoice",
                        Question = "测试题1",
                        Options = new List<string> { "A", "B", "C", "D" },
                        CorrectAnswer = "A",
                        KeyPoints = new List<string>(),
                        Explanation = "解释1"
                    },
                    new ExerciseDto
                    {
                        Type = "FillBlank",
                        Question = "测试题2 _____",
                        CorrectAnswer = "答案",
                        KeyPoints = new List<string>(),
                        Explanation = "解释2"
                    },
                    new ExerciseDto
                    {
                        Type = "ShortAnswer",
                        Question = "测试题3",
                        CorrectAnswer = "参考答案",
                        KeyPoints = new List<string>(),
                        Explanation = "解释3"
                    }
                }
            });

        // Act
        var exercises = await _exerciseService.GenerateAsync(kp, count: 3, CancellationToken.None);

        // Assert - 验证 ID 可追溯
        exercises.Should().HaveCount(3);
        foreach (var exercise in exercises)
        {
            exercise.ExerciseId.Should().StartWith(kp.KpId + "_ex_");
            exercise.ExerciseId.Should().NotBeNullOrEmpty();
        }
    }

    #endregion

    #region 混合题型测试

    [Fact]
    public async Task GenerateAsync_ShouldReturnMixedExerciseTypes()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        var snippets = CreateTestSnippets();

        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<IEnumerable<string>>()))
            .Returns(snippets);

        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<ExercisesResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExercisesResponse
            {
                Exercises = new List<ExerciseDto>
                {
                    new ExerciseDto
                    {
                        Type = "SingleChoice",
                        Question = "选择题",
                        Options = new List<string> { "A", "B", "C", "D" },
                        CorrectAnswer = "A",
                        KeyPoints = new List<string>(),
                        Explanation = "解释1"
                    },
                    new ExerciseDto
                    {
                        Type = "FillBlank",
                        Question = "填空题 _____",
                        CorrectAnswer = "答案",
                        KeyPoints = new List<string>(),
                        Explanation = "解释2"
                    },
                    new ExerciseDto
                    {
                        Type = "ShortAnswer",
                        Question = "简答题",
                        CorrectAnswer = "参考答案",
                        KeyPoints = new List<string>(),
                        Explanation = "解释3"
                    }
                }
            });

        // Act
        var result = await _exerciseService.GenerateAsync(kp, count: 3, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(e => e.Type == ExerciseType.SingleChoice);
        result.Should().Contain(e => e.Type == ExerciseType.FillBlank);
        result.Should().Contain(e => e.Type == ExerciseType.ShortAnswer);
    }

    #endregion

    #region 降级流程测试

    [Fact]
    public async Task GenerateAsync_WhenPrimaryFails_ShouldFallback()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        var snippets = CreateTestSnippets();

        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<IEnumerable<string>>()))
            .Returns(snippets);

        // 首次调用失败，降级后返回空列表
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<ExercisesResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("LLM 服务暂时不可用"));

        // Act
        var result = await _exerciseService.GenerateAsync(kp, count: 1, CancellationToken.None);

        // Assert - 降级策略：返回空列表而非抛出异常
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task JudgeAsync_WhenGradingFails_ShouldProvideReferenceAnswer()
    {
        // Arrange - 使用填空题测试降级逻辑（填空题依赖LLM，失败时提供参考答案）
        var exercise = new Exercise
        {
            ExerciseId = "ex_001",
            KpId = "kp_001",
            Type = ExerciseType.FillBlank,
            Question = "测试填空题",
            CorrectAnswer = "正确答案是环境",
            KeyPoints = new List<string> { "要点1" }
        };

        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<FillBlankFeedbackResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("判题服务不可用"));

        // Act
        var result = await _exerciseService.JudgeAsync(exercise, "用户答案", CancellationToken.None);

        // Assert - 降级策略：提供参考答案
        result.ReferenceAnswer.Should().Be(exercise.CorrectAnswer);
        result.Explanation.Should().Contain("错误"); // 降级时会返回"判题过程出现错误"
    }

    #endregion

    #region Helper Methods

    private static KnowledgePoint CreateTestKnowledgePoint()
    {
        return new KnowledgePoint
        {
            KpId = "kp_001",
            BookRootId = "book_001",
            Title = "智能体（Agent）的定义",
            Aliases = new List<string> { "Agent", "智能体" },
            ChapterPath = new List<string> { "第一章", "1.1 基本概念" },
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
                HeadingPath = new List<string> { "第一章", "1.1 基本概念" },
                Content = "智能体（Agent）是一个能够感知环境并采取行动的系统。",
                StartLine = 10,
                EndLine = 15
            },
            new SourceSnippet
            {
                SnippetId = "snippet_002",
                BookRootId = "book_001",
                DocId = "doc_001",
                FilePath = "/docs/ch01.md",
                HeadingPath = new List<string> { "第一章", "1.2 智能体特性" },
                Content = "现代AI智能体通常具备以下特性：感知能力、推理能力、学习能力和决策能力。",
                StartLine = 20,
                EndLine = 25
            }
        };
    }

    #endregion
}
