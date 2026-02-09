using ASimpleTutor.Core.Models;
using FluentAssertions;
using System.Collections.Generic;
using Xunit;

namespace ASimpleTutor.Tests.DataModels;

/// <summary>
/// 数据模型测试
/// 对应测试需求文档：TC-DM-001 ~ TC-DM-005
/// </summary>
public class DataModelTests
{
    #region Document 模型测试

    [Fact]
    public void Document_ShouldInitializeWithDefaultValues()
    {
        // Act
        var document = new Document();

        // Assert
        document.DocId.Should().BeEmpty();
        document.Path.Should().BeEmpty();
        document.Title.Should().BeEmpty();
        document.Sections.Should().NotBeNull();
        document.Sections.Should().BeEmpty();
    }

    [Fact]
    public void Document_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var sections = new List<Section> {
            new Section { SectionId = "section1", HeadingPath = new List<string> { "Chapter 1", "Section 1" } }
        };

        // Act
        var document = new Document
        {
            DocId = "doc1",
            BookRootId = "book1",
            Path = "/path/to/doc.md",
            Title = "Test Document",
            Sections = sections,
            ContentHash = "hash123"
        };

        // Assert
        document.DocId.Should().Be("doc1");
        document.BookRootId.Should().Be("book1");
        document.Path.Should().Be("/path/to/doc.md");
        document.Title.Should().Be("Test Document");
        document.Sections.Should().BeSameAs(sections);
        document.ContentHash.Should().Be("hash123");
    }

    [Fact]
    public void Document_ShouldHaveSectionsList()
    {
        // Act
        var document = new Document();

        // Assert
        document.Sections.Should().NotBeNull();
        document.Sections.Should().BeEmpty();
    }

    #endregion

    #region Section 模型测试

    [Fact]
    public void Section_ShouldInitializeWithDefaultValues()
    {
        // Act
        var section = new Section();

        // Assert
        section.SectionId.Should().BeEmpty();
        section.HeadingPath.Should().NotBeNull();
        section.HeadingPath.Should().BeEmpty();
        section.SubSections.Should().NotBeNull();
        section.SubSections.Should().BeEmpty();
        section.StartLine.Should().Be(0);
        section.EndLine.Should().Be(0);
        section.OriginalLength.Should().Be(0);
        section.EffectiveLength.Should().Be(0);
        section.FilteredLength.Should().Be(0);
        section.IsExcluded.Should().BeFalse();
    }

    [Fact]
    public void Section_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var subSections = new List<Section> {
            new Section { SectionId = "sub1", HeadingPath = new List<string> { "Chapter 1", "Section 1", "Subsection 1" } }
        };

        // Act
        var section = new Section
        {
            SectionId = "section1",
            HeadingPath = new List<string> { "Chapter 1", "Section 1" },
            SubSections = subSections,
            StartLine = 10,
            EndLine = 20,
            OriginalLength = 1000,
            EffectiveLength = 800,
            FilteredLength = 200,
            IsExcluded = true
        };

        // Assert
        section.SectionId.Should().Be("section1");
        section.HeadingPath.Should().ContainInOrder("Chapter 1", "Section 1");
        section.SubSections.Should().BeSameAs(subSections);
        section.StartLine.Should().Be(10);
        section.EndLine.Should().Be(20);
        section.OriginalLength.Should().Be(1000);
        section.EffectiveLength.Should().Be(800);
        section.FilteredLength.Should().Be(200);
        section.IsExcluded.Should().BeTrue();
    }

    #endregion

    #region KnowledgePoint 模型测试

    [Fact]
    public void KnowledgePoint_ShouldInitializeWithDefaultValues()
    {
        // Act
        var kp = new KnowledgePoint();

        // Assert
        kp.KpId.Should().BeEmpty();
        kp.Title.Should().BeEmpty();
        kp.Aliases.Should().NotBeNull();
        kp.Aliases.Should().BeEmpty();
        kp.ChapterPath.Should().NotBeNull();
        kp.ChapterPath.Should().BeEmpty();
        kp.Importance.Should().Be(0);
        kp.SnippetIds.Should().NotBeNull();
        kp.SnippetIds.Should().BeEmpty();
    }

    [Fact]
    public void KnowledgePoint_ShouldSetPropertiesCorrectly()
    {
        // Act
        var kp = new KnowledgePoint
        {
            KpId = "kp1",
            BookRootId = "book1",
            Title = "Test Knowledge Point",
            Aliases = new List<string> { "alias1", "alias2" },
            ChapterPath = new List<string> { "Chapter 1", "Section 1" },
            Importance = 0.8f,
            SnippetIds = new List<string> { "snippet1", "snippet2" }
        };

        // Assert
        kp.KpId.Should().Be("kp1");
        kp.BookRootId.Should().Be("book1");
        kp.Title.Should().Be("Test Knowledge Point");
        kp.Aliases.Should().ContainInOrder("alias1", "alias2");
        kp.ChapterPath.Should().ContainInOrder("Chapter 1", "Section 1");
        kp.Importance.Should().Be(0.8f);
        kp.SnippetIds.Should().ContainInOrder("snippet1", "snippet2");
    }

    [Fact]
    public void KnowledgePoint_ShouldHandleEmptyCollections()
    {
        // Act
        var kp = new KnowledgePoint
        {
            Aliases = new List<string>(),
            ChapterPath = new List<string>(),
            SnippetIds = new List<string>()
        };

        // Assert
        kp.Aliases.Should().NotBeNull();
        kp.Aliases.Should().BeEmpty();
        kp.ChapterPath.Should().NotBeNull();
        kp.ChapterPath.Should().BeEmpty();
        kp.SnippetIds.Should().NotBeNull();
        kp.SnippetIds.Should().BeEmpty();
    }

    #endregion

    #region SourceSnippet 模型测试

    [Fact]
    public void SourceSnippet_ShouldInitializeWithDefaultValues()
    {
        // Act
        var snippet = new SourceSnippet();

        // Assert
        snippet.SnippetId.Should().BeEmpty();
        snippet.BookRootId.Should().BeEmpty();
        snippet.DocId.Should().BeEmpty();
        snippet.FilePath.Should().BeEmpty();
        snippet.HeadingPath.Should().NotBeNull();
        snippet.HeadingPath.Should().BeEmpty();
        snippet.Content.Should().BeEmpty();
        snippet.StartLine.Should().Be(0);
        snippet.EndLine.Should().Be(0);
    }

    [Fact]
    public void SourceSnippet_ShouldSetPropertiesCorrectly()
    {
        // Act
        var snippet = new SourceSnippet
        {
            SnippetId = "snippet1",
            BookRootId = "book1",
            DocId = "doc1",
            FilePath = "/path/to/doc.md",
            HeadingPath = new List<string> { "Chapter 1", "Section 1" },
            Content = "This is a test snippet",
            StartLine = 10,
            EndLine = 15
        };

        // Assert
        snippet.SnippetId.Should().Be("snippet1");
        snippet.BookRootId.Should().Be("book1");
        snippet.DocId.Should().Be("doc1");
        snippet.FilePath.Should().Be("/path/to/doc.md");
        snippet.HeadingPath.Should().ContainInOrder("Chapter 1", "Section 1");
        snippet.Content.Should().Be("This is a test snippet");
        snippet.StartLine.Should().Be(10);
        snippet.EndLine.Should().Be(15);
    }

    #endregion

    #region KnowledgeSystem 模型测试

    [Fact]
    public void KnowledgeSystem_ShouldInitializeWithDefaultValues()
    {
        // Act
        var ks = new KnowledgeSystem();

        // Assert
        ks.BookRootId.Should().BeEmpty();
        ks.KnowledgePoints.Should().NotBeNull();
        ks.KnowledgePoints.Should().BeEmpty();
        ks.Snippets.Should().NotBeNull();
        ks.Snippets.Should().BeEmpty();
    }

    [Fact]
    public void KnowledgeSystem_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var kps = new List<KnowledgePoint> {
            new KnowledgePoint { KpId = "kp1", Title = "KP1" }
        };
        var snippets = new Dictionary<string, SourceSnippet> {
            { "snippet1", new SourceSnippet { SnippetId = "snippet1", Content = "Content 1" } }
        };

        // Act
        var ks = new KnowledgeSystem
        {
            BookRootId = "book1",
            KnowledgePoints = kps,
            Snippets = snippets
        };

        // Assert
        ks.BookRootId.Should().Be("book1");
        ks.KnowledgePoints.Should().BeSameAs(kps);
        ks.Snippets.Should().BeSameAs(snippets);
    }

    #endregion

    #region Exercise 模型测试

    [Fact]
    public void Exercise_ShouldInitializeWithDefaultValues()
    {
        // Act
        var exercise = new Exercise();

        // Assert
        exercise.ExerciseId.Should().BeEmpty();
        exercise.Question.Should().BeEmpty();
        exercise.Type.Should().Be(ExerciseType.SingleChoice);
        exercise.Options.Should().NotBeNull();
        exercise.Options.Should().BeEmpty();
        exercise.CorrectAnswer.Should().BeEmpty();
        exercise.KeyPoints.Should().NotBeNull();
        exercise.KeyPoints.Should().BeEmpty();
    }

    [Fact]
    public void Exercise_ShouldSetPropertiesCorrectly()
    {
        // Act
        var exercise = new Exercise
        {
            ExerciseId = "ex1",
            Question = "What is Markdown?",
            Type = ExerciseType.SingleChoice,
            Options = new List<string> { "Option 1", "Option 2", "Option 3" },
            CorrectAnswer = "Option 2",
            KeyPoints = new List<string> { "Point 1", "Point 2" },
            Explanation = "This is an explanation"
        };

        // Assert
        exercise.ExerciseId.Should().Be("ex1");
        exercise.Question.Should().Be("What is Markdown?");
        exercise.Type.Should().Be(ExerciseType.SingleChoice);
        exercise.Options.Should().ContainInOrder("Option 1", "Option 2", "Option 3");
        exercise.CorrectAnswer.Should().Be("Option 2");
        exercise.KeyPoints.Should().ContainInOrder("Point 1", "Point 2");
        exercise.Explanation.Should().Be("This is an explanation");
    }

    [Fact]
    public void Exercise_ShouldHandleDifferentTypes()
    {
        // Act & Assert
        var singleChoice = new Exercise { Type = ExerciseType.SingleChoice };
        singleChoice.Type.Should().Be(ExerciseType.SingleChoice);

        var multiChoice = new Exercise { Type = ExerciseType.MultiChoice };
        multiChoice.Type.Should().Be(ExerciseType.MultiChoice);

        var trueFalse = new Exercise { Type = ExerciseType.TrueFalse };
        trueFalse.Type.Should().Be(ExerciseType.TrueFalse);

        var shortAnswer = new Exercise { Type = ExerciseType.ShortAnswer };
        shortAnswer.Type.Should().Be(ExerciseType.ShortAnswer);
    }

    #endregion

    #region ExerciseFeedback 模型测试

    [Fact]
    public void ExerciseFeedback_ShouldInitializeWithDefaultValues()
    {
        // Act
        var feedback = new ExerciseFeedback();

        // Assert
        feedback.IsCorrect.Should().BeNull();
        feedback.Explanation.Should().BeEmpty();
        feedback.ReferenceAnswer.Should().BeNull();
        feedback.CoveredPoints.Should().NotBeNull();
        feedback.CoveredPoints.Should().BeEmpty();
        feedback.MissingPoints.Should().NotBeNull();
        feedback.MissingPoints.Should().BeEmpty();
        feedback.ErrorAnalysis.Should().BeNull();
        feedback.MasteryAdjustment.Should().BeNull();
    }

    [Fact]
    public void ExerciseFeedback_ShouldSetPropertiesCorrectly()
    {
        // Act
        var feedback = new ExerciseFeedback
        {
            IsCorrect = true,
            Explanation = "Correct answer",
            ReferenceAnswer = "The correct answer",
            CoveredPoints = new List<string> { "Point 1", "Point 2" },
            MissingPoints = new List<string> { "Point 3" },
            ErrorAnalysis = "Missing point 3",
            MasteryAdjustment = 0.1f
        };

        // Assert
        feedback.IsCorrect.Should().BeTrue();
        feedback.Explanation.Should().Be("Correct answer");
        feedback.ReferenceAnswer.Should().Be("The correct answer");
        feedback.CoveredPoints.Should().ContainInOrder("Point 1", "Point 2");
        feedback.MissingPoints.Should().ContainInOrder("Point 3");
        feedback.ErrorAnalysis.Should().Be("Missing point 3");
        feedback.MasteryAdjustment.Should().Be(0.1f);
    }

    #endregion
}
