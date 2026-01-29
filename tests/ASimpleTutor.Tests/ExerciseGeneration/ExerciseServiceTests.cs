using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using ASimpleTutor.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;

namespace ASimpleTutor.Tests.ExerciseGeneration;

/// <summary>
/// 习题生成与练习反馈模块测试用例
/// 对应测试需求文档：TC-EXG-001 ~ TC-EXG-008, TC-GRF-001 ~ TC-GRF-008, TC-FD-001 ~ TC-FD-005
/// </summary>
public class ExerciseServiceTests
{
    private readonly Mock<ISimpleRagService> _ragServiceMock;
    private readonly Mock<ISourceTracker> _sourceTrackerMock;
    private readonly Mock<ILLMService> _llmServiceMock;
    private readonly Mock<ILogger<ExerciseService>> _loggerMock;
    private readonly ExerciseService _service;

    public ExerciseServiceTests()
    {
        _ragServiceMock = new Mock<ISimpleRagService>();
        _sourceTrackerMock = new Mock<ISourceTracker>();
        _llmServiceMock = new Mock<ILLMService>();
        _loggerMock = new Mock<ILogger<ExerciseService>>();

        _service = new ExerciseService(
            _ragServiceMock.Object,
            _sourceTrackerMock.Object,
            _llmServiceMock.Object,
            _loggerMock.Object);
    }

    #region 习题生成测试 - TC-EXG-001 ~ TC-EXG-008

    [Fact]
    public async Task GenerateAsync_WithValidInput_ShouldReturnExercises()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        var snippets = CreateTestSnippets();

        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<IEnumerable<string>>()))
            .Returns(snippets);

        var llmResponse = CreateValidExerciseResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<ExercisesResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _service.GenerateAsync(kp, count: 1, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GenerateAsync_ShouldReturnExerciseWithRequiredFields()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        var snippets = CreateTestSnippets();

        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<IEnumerable<string>>()))
            .Returns(snippets);

        var llmResponse = CreateValidExerciseResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<ExercisesResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _service.GenerateAsync(kp, count: 1, CancellationToken.None);

        // Assert
        var exercise = result.Should().ContainSingle().Subject;
        exercise.ExerciseId.Should().NotBeNullOrEmpty();
        exercise.KpId.Should().Be(kp.KpId);
        exercise.Question.Should().NotBeNullOrEmpty();
        exercise.Type.Should().BeDefined();
    }

    [Fact]
    public async Task GenerateAsync_ShouldAssociateEvidenceSnippetIds()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        var snippets = CreateTestSnippets();

        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<IEnumerable<string>>()))
            .Returns(snippets);

        var llmResponse = CreateValidExerciseResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<ExercisesResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _service.GenerateAsync(kp, count: 1, CancellationToken.None);

        // Assert
        var exercise = result.Should().ContainSingle().Subject;
        exercise.EvidenceSnippetIds.Should().NotBeEmpty();
        exercise.EvidenceSnippetIds.Should().Contain("snippet_001");
    }

    [Fact]
    public async Task GenerateAsync_WithMultipleCount_ShouldReturnMultipleExercises()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        var snippets = CreateTestSnippets();

        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<IEnumerable<string>>()))
            .Returns(snippets);

        var llmResponse = CreateMultiExerciseResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<ExercisesResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _service.GenerateAsync(kp, count: 3, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GenerateAsync_WithChoiceExercise_ShouldHaveOptions()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        var snippets = CreateTestSnippets();

        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<IEnumerable<string>>()))
            .Returns(snippets);

        var llmResponse = CreateChoiceExerciseResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<ExercisesResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _service.GenerateAsync(kp, count: 1, CancellationToken.None);

        // Assert
        var exercise = result.Should().ContainSingle().Subject;
        exercise.Type.Should().Be(ExerciseType.SingleChoice);
        exercise.Options.Should().NotBeEmpty();
        exercise.Options.Should().HaveCount(4); // 1正确 + 3干扰项
        exercise.CorrectAnswer.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateAsync_WithFillBlankExercise_ShouldHaveCorrectAnswer()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        var snippets = CreateTestSnippets();

        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<IEnumerable<string>>()))
            .Returns(snippets);

        var llmResponse = CreateFillBlankExerciseResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<ExercisesResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _service.GenerateAsync(kp, count: 1, CancellationToken.None);

        // Assert
        var exercise = result.Should().ContainSingle().Subject;
        exercise.Type.Should().Be(ExerciseType.FillBlank);
        exercise.Question.Should().Contain("_____");
        exercise.CorrectAnswer.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateAsync_WithShortAnswerExercise_ShouldHaveKeyPoints()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        var snippets = CreateTestSnippets();

        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<IEnumerable<string>>()))
            .Returns(snippets);

        var llmResponse = CreateShortAnswerExerciseResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<ExercisesResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _service.GenerateAsync(kp, count: 1, CancellationToken.None);

        // Assert
        var exercise = result.Should().ContainSingle().Subject;
        exercise.Type.Should().Be(ExerciseType.ShortAnswer);
        exercise.KeyPoints.Should().NotBeNull();
        exercise.KeyPoints.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GenerateAsync_WhenLLMFails_ShouldReturnEmptyList()
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
            .ThrowsAsync(new Exception("LLM Error"));

        // Act
        var result = await _service.GenerateAsync(kp, count: 1, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateAsync_WhenLLMReturnsNull_ShouldReturnEmptyList()
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
            .ReturnsAsync((ExercisesResponse?)null);

        // Act
        var result = await _service.GenerateAsync(kp, count: 1, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateAsync_WithEmptySnippets_ShouldStillGenerate()
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

        var llmResponse = CreateValidExerciseResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<ExercisesResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _service.GenerateAsync(kp, count: 1, CancellationToken.None);

        // Assert
        result.Should().ContainSingle();
    }

    #endregion

    #region 判题反馈测试 - TC-GRF-001 ~ TC-GRF-008

    [Fact]
    public async Task JudgeAsync_WithCorrectChoice_ShouldReturnCorrectFeedback()
    {
        // Arrange
        var exercise = CreateChoiceExercise("A");
        var userAnswer = "A";

        // Act
        var result = await _service.JudgeAsync(exercise, userAnswer, CancellationToken.None);

        // Assert
        result.IsCorrect.Should().BeTrue();
        result.Explanation.Should().Contain("正确");
    }

    [Fact]
    public async Task JudgeAsync_WithWrongChoice_ShouldReturnWrongFeedback()
    {
        // Arrange
        var exercise = CreateChoiceExercise("B");
        var userAnswer = "C";

        // Act
        var result = await _service.JudgeAsync(exercise, userAnswer, CancellationToken.None);

        // Assert
        result.IsCorrect.Should().BeFalse();
        result.Explanation.Should().Contain("错误");
        result.ReferenceAnswer.Should().Be("B");
    }

    [Fact]
    public async Task JudgeAsync_WithCaseInsensitiveChoice_ShouldMatch()
    {
        // Arrange
        var exercise = CreateChoiceExercise("A");
        var userAnswer = "a";

        // Act
        var result = await _service.JudgeAsync(exercise, userAnswer, CancellationToken.None);

        // Assert
        result.IsCorrect.Should().BeTrue();
    }

    [Fact]
    public async Task JudgeAsync_WithTrimmedChoice_ShouldMatch()
    {
        // Arrange
        var exercise = CreateChoiceExercise("A");
        var userAnswer = "  A  ";

        // Act
        var result = await _service.JudgeAsync(exercise, userAnswer, CancellationToken.None);

        // Assert
        result.IsCorrect.Should().BeTrue();
    }

    [Fact]
    public async Task JudgeAsync_WithFillBlank_ShouldCallLLM()
    {
        // Arrange
        var exercise = CreateFillBlankExercise();
        var userAnswer = "正确答案是环境";

        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<FillBlankFeedbackResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FillBlankFeedbackResponse
            {
                IsCorrect = true,
                Explanation = "回答正确",
                CoveredPoints = new List<string> { "感知环境" },
                MissingPoints = new List<string>()
            });

        // Act
        var result = await _service.JudgeAsync(exercise, userAnswer, CancellationToken.None);

        // Assert
        result.IsCorrect.Should().BeTrue();
        result.ReferenceAnswer.Should().Be(exercise.CorrectAnswer);
    }

    [Fact]
    public async Task JudgeAsync_WithShortAnswer_ShouldCallLLM()
    {
        // Arrange
        var exercise = CreateShortAnswerExercise();
        var userAnswer = "智能体是能够感知环境并采取行动的系统";

        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<FillBlankFeedbackResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FillBlankFeedbackResponse
            {
                IsCorrect = true,
                Explanation = "回答完整",
                CoveredPoints = new List<string> { "感知环境", "自主决策", "执行行动" },
                MissingPoints = new List<string>()
            });

        // Act
        var result = await _service.JudgeAsync(exercise, userAnswer, CancellationToken.None);

        // Assert
        result.IsCorrect.Should().BeTrue();
        result.CoveredPoints.Should().NotBeEmpty();
    }

    [Fact]
    public async Task JudgeAsync_WithPartialFillBlank_ShouldReturnPartialFeedback()
    {
        // Arrange
        var exercise = CreateFillBlankExercise();
        var userAnswer = "部分正确答案";

        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<FillBlankFeedbackResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FillBlankFeedbackResponse
            {
                IsCorrect = null,
                Explanation = "部分正确",
                CoveredPoints = new List<string> { "要点1" },
                MissingPoints = new List<string> { "要点2", "要点3" }
            });

        // Act
        var result = await _service.JudgeAsync(exercise, userAnswer, CancellationToken.None);

        // Assert
        result.IsCorrect.Should().BeNull();
        result.MissingPoints.Should().NotBeEmpty();
    }

    [Fact]
    public async Task JudgeAsync_WhenJudgeFails_ShouldReturnFallback()
    {
        // Arrange
        var exercise = CreateFillBlankExercise();
        var userAnswer = "用户答案";

        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<FillBlankFeedbackResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("LLM Error"));

        // Act
        var result = await _service.JudgeAsync(exercise, userAnswer, CancellationToken.None);

        // Assert
        result.ReferenceAnswer.Should().Be(exercise.CorrectAnswer);
        result.Explanation.Should().Contain("错误");
    }

    [Fact]
    public async Task JudgeAsync_WithUnsupportedType_ShouldReturnError()
    {
        // Arrange
        var exercise = new Exercise
        {
            ExerciseId = "ex_001",
            KpId = "kp_001",
            Type = (ExerciseType)99, // 不支持的题型
            Question = "测试题目",
            CorrectAnswer = "A"
        };
        var userAnswer = "A";

        // Act
        var result = await _service.JudgeAsync(exercise, userAnswer, CancellationToken.None);

        // Assert
        result.Explanation.Should().Contain("不支持");
    }

    #endregion

    #region 边界条件测试 - TC-FD-003 ~ TC-FD-004

    [Fact]
    public async Task GenerateAsync_WithNullSnippetIds_ShouldHandleGracefully()
    {
        // Arrange
        var kp = new KnowledgePoint
        {
            KpId = "kp_null",
            Title = "测试知识点",
            SnippetIds = null!
        };

        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<IEnumerable<string>>()))
            .Returns(new List<SourceSnippet>());

        var llmResponse = CreateValidExerciseResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<ExercisesResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _service.GenerateAsync(kp, count: 1, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateAsync_WithZeroCount_ShouldReturnEmpty()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        var snippets = CreateTestSnippets();

        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<IEnumerable<string>>()))
            .Returns(snippets);

        // Act
        var result = await _service.GenerateAsync(kp, count: 0, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task JudgeAsync_WithEmptyUserAnswer_ShouldStillProcess()
    {
        // Arrange
        var exercise = CreateChoiceExercise("A");
        var userAnswer = "";

        // Act
        var result = await _service.JudgeAsync(exercise, userAnswer, CancellationToken.None);

        // Assert
        result.IsCorrect.Should().BeFalse();
    }

    [Fact]
    public async Task GenerateAsync_ShouldAssignUniqueExerciseIds()
    {
        // Arrange
        var kp = CreateTestKnowledgePoint();
        var snippets = CreateTestSnippets();

        _sourceTrackerMock
            .Setup(s => s.GetSources(It.IsAny<IEnumerable<string>>()))
            .Returns(snippets);

        var llmResponse = CreateMultiExerciseResponse();
        _llmServiceMock
            .Setup(s => s.ChatJsonAsync<ExercisesResponse>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmResponse);

        // Act
        var result = await _service.GenerateAsync(kp, count: 3, CancellationToken.None);

        // Assert
        var ids = result.Select(e => e.ExerciseId).ToList();
        ids.Should().HaveCount(3);
        ids.Should().OnlyHaveUniqueItems();
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
                Content = "智能体（Agent）是一个能够感知环境并采取行动的系统。它通过传感器感知环境，通过执行器作用于环境。",
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

    private static ExercisesResponse CreateValidExerciseResponse()
    {
        return new ExercisesResponse
        {
            Exercises = new List<Exercise>
            {
                new Exercise
                {
                    Type = ExerciseType.SingleChoice,
                    Question = "关于智能体的描述，以下正确的是？",
                    Options = new List<string> { "只能被动响应", "能够感知并行动", "不需要计算", "只能用于游戏" },
                    CorrectAnswer = "B",
                    KeyPoints = new List<string> { "感知环境", "采取行动" }
                }
            }
        };
    }

    private static ExercisesResponse CreateMultiExerciseResponse()
    {
        return new ExercisesResponse
        {
            Exercises = new List<Exercise>
            {
                new Exercise
                {
                    Type = ExerciseType.SingleChoice,
                    Question = "选择题1",
                    Options = new List<string> { "A", "B", "C", "D" },
                    CorrectAnswer = "A",
                    KeyPoints = new List<string> { "要点1" }
                },
                new Exercise
                {
                    Type = ExerciseType.FillBlank,
                    Question = "填空题1 _____",
                    CorrectAnswer = "答案",
                    KeyPoints = new List<string> { "要点2" }
                },
                new Exercise
                {
                    Type = ExerciseType.ShortAnswer,
                    Question = "简答题1",
                    CorrectAnswer = "参考答案",
                    KeyPoints = new List<string> { "要点3" }
                }
            }
        };
    }

    private static ExercisesResponse CreateChoiceExerciseResponse()
    {
        return new ExercisesResponse
        {
            Exercises = new List<Exercise>
            {
                new Exercise
                {
                    Type = ExerciseType.SingleChoice,
                    Question = "关于智能体的描述，以下正确的是？",
                    Options = new List<string> { "只能被动响应指令", "能够感知环境并自主行动", "不需要任何计算资源", "只能用于游戏领域" },
                    CorrectAnswer = "B",
                    KeyPoints = new List<string> { "感知环境", "自主行动" }
                }
            }
        };
    }

    private static ExercisesResponse CreateFillBlankExerciseResponse()
    {
        return new ExercisesResponse
        {
            Exercises = new List<Exercise>
            {
                new Exercise
                {
                    Type = ExerciseType.FillBlank,
                    Question = "智能体的三大核心能力是：感知_____、自主_____、执行_____。",
                    CorrectAnswer = "环境,决策,行动",
                    KeyPoints = new List<string> { "感知", "决策", "行动" }
                }
            }
        };
    }

    private static ExercisesResponse CreateShortAnswerExerciseResponse()
    {
        return new ExercisesResponse
        {
            Exercises = new List<Exercise>
            {
                new Exercise
                {
                    Type = ExerciseType.ShortAnswer,
                    Question = "请用自己的话解释什么是智能体，并列举其主要特征。",
                    CorrectAnswer = "智能体是能够感知环境并采取行动的系统。特征包括：感知能力、推理能力、学习能力、决策能力。",
                    KeyPoints = new List<string> { "感知环境", "采取行动", "推理能力", "学习能力", "决策能力" }
                }
            }
        };
    }

    private static Exercise CreateChoiceExercise(string correctAnswer)
    {
        return new Exercise
        {
            ExerciseId = "ex_choice_001",
            KpId = "kp_001",
            Type = ExerciseType.SingleChoice,
            Question = "关于智能体的描述，以下正确的是？",
            Options = new List<string> { "A. 只能被动响应", "B. 能够感知并行动", "C. 不需要计算", "D. 只能用于游戏" },
            CorrectAnswer = correctAnswer,
            EvidenceSnippetIds = new List<string> { "snippet_001" },
            KeyPoints = new List<string> { "感知环境", "采取行动" }
        };
    }

    private static Exercise CreateFillBlankExercise()
    {
        return new Exercise
        {
            ExerciseId = "ex_fill_001",
            KpId = "kp_001",
            Type = ExerciseType.FillBlank,
            Question = "智能体能够感知_____并采取行动。",
            CorrectAnswer = "环境",
            EvidenceSnippetIds = new List<string> { "snippet_001" },
            KeyPoints = new List<string> { "感知环境" }
        };
    }

    private static Exercise CreateShortAnswerExercise()
    {
        return new Exercise
        {
            ExerciseId = "ex_short_001",
            KpId = "kp_001",
            Type = ExerciseType.ShortAnswer,
            Question = "请解释什么是智能体？",
            CorrectAnswer = "智能体是能够感知环境并采取行动的系统。",
            EvidenceSnippetIds = new List<string> { "snippet_001" },
            KeyPoints = new List<string> { "感知环境", "自主决策", "执行行动" }
        };
    }

    #endregion
}

/// <summary>
/// Exercise 模型数据验证测试
/// 对应测试需求文档：Exercise 数据结构验证
/// </summary>
public class ExerciseModelTests
{
    [Fact]
    public void Exercise_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var exercise = new Exercise();

        // Assert
        exercise.ExerciseId.Should().BeEmpty();
        exercise.KpId.Should().BeEmpty();
        exercise.Question.Should().BeEmpty();
        exercise.Options.Should().NotBeNull();
        exercise.Options.Should().BeEmpty();
        exercise.CorrectAnswer.Should().BeEmpty();
        exercise.EvidenceSnippetIds.Should().NotBeNull();
        exercise.EvidenceSnippetIds.Should().BeEmpty();
        exercise.KeyPoints.Should().NotBeNull();
        exercise.KeyPoints.Should().BeEmpty();
    }

    [Fact]
    public void ExerciseFeedback_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var feedback = new ExerciseFeedback();

        // Assert
        feedback.IsCorrect.Should().BeNull();
        feedback.Explanation.Should().BeEmpty();
        feedback.ReferenceAnswer.Should().BeNull();
        feedback.CoveredPoints.Should().NotBeNull();
        feedback.CoveredPoints.Should().BeEmpty();
        feedback.MissingPoints.Should().NotBeNull();
        feedback.MissingPoints.Should().BeEmpty();
    }

    [Fact]
    public void ExerciseType_ShouldHaveAllTypes()
    {
        // Arrange & Act
        var types = Enum.GetValues<ExerciseType>();

        // Assert
        types.Should().HaveCount(3);
        types.Should().Contain(ExerciseType.SingleChoice);
        types.Should().Contain(ExerciseType.FillBlank);
        types.Should().Contain(ExerciseType.ShortAnswer);
    }

    [Fact]
    public void Exercise_CanBePopulated()
    {
        // Arrange
        var exercise = new Exercise
        {
            ExerciseId = "ex_001",
            KpId = "kp_001",
            Type = ExerciseType.SingleChoice,
            Question = "测试题目",
            Options = new List<string> { "A", "B", "C", "D" },
            CorrectAnswer = "A",
            EvidenceSnippetIds = new List<string> { "sn_001" },
            KeyPoints = new List<string> { "要点1", "要点2" }
        };

        // Assert
        exercise.ExerciseId.Should().Be("ex_001");
        exercise.KpId.Should().Be("kp_001");
        exercise.Type.Should().Be(ExerciseType.SingleChoice);
        exercise.Question.Should().Be("测试题目");
        exercise.Options.Should().HaveCount(4);
        exercise.CorrectAnswer.Should().Be("A");
        exercise.EvidenceSnippetIds.Should().HaveCount(1);
        exercise.KeyPoints.Should().HaveCount(2);
    }

    [Fact]
    public void ExerciseFeedback_CanBePopulated()
    {
        // Arrange
        var feedback = new ExerciseFeedback
        {
            IsCorrect = true,
            Explanation = "回答正确",
            ReferenceAnswer = "正确答案",
            CoveredPoints = new List<string> { "要点1" },
            MissingPoints = new List<string> { "要点2" }
        };

        // Assert
        feedback.IsCorrect.Should().BeTrue();
        feedback.Explanation.Should().Be("回答正确");
        feedback.ReferenceAnswer.Should().Be("正确答案");
        feedback.CoveredPoints.Should().HaveCount(1);
        feedback.MissingPoints.Should().HaveCount(1);
    }
}
