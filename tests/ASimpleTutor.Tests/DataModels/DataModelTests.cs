using ASimpleTutor.Core.Models;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace ASimpleTutor.Tests.DataModels;

/// <summary>
/// 数据模型与存储策略模块测试
/// 对应测试需求文档：TC-BR、TC-DOC、TC-SS、TC-KP、TC-KS、TC-LP、TC-EX、TC-CONV、TC-JSON
/// </summary>
public class DataModelTests
{
    #region BookRoot Tests (TC-BR-001 ~ TC-BR-003)

    [Fact]
    public void BookRoot_BasicCreation_ShouldHaveCorrectValues()
    {
        // Arrange & Act
        var bookRoot = new BookRoot
        {
            Id = "test_book_001",
            Name = "测试书籍",
            Path = "/test/path",
            Enabled = true,
            Order = 1
        };

        // Assert
        bookRoot.Id.Should().Be("test_book_001");
        bookRoot.Name.Should().Be("测试书籍");
        bookRoot.Path.Should().Be("/test/path");
        bookRoot.Enabled.Should().BeTrue();
        bookRoot.Order.Should().Be(1);
    }

    [Fact]
    public void BookRoot_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var bookRoot = new BookRoot();

        // Assert
        bookRoot.Enabled.Should().BeTrue();
        bookRoot.ReferenceDirNames.Should().Contain("references");
        bookRoot.ReferenceDirNames.Should().Contain("参考书目");
        bookRoot.ExcludeGlobs.Should().NotBeNull();
        bookRoot.ExcludeGlobs.Should().BeEmpty();
    }

    [Fact]
    public void BookRoot_WithCustomReferenceDirs_ShouldOverrideDefaults()
    {
        // Arrange & Act
        var bookRoot = new BookRoot
        {
            Id = "custom_book",
            Name = "自定义书籍",
            Path = "/custom/path",
            ReferenceDirNames = new List<string> { "文献", "bibliography" }
        };

        // Assert
        bookRoot.ReferenceDirNames.Should().Contain("文献");
        bookRoot.ReferenceDirNames.Should().Contain("bibliography");
        bookRoot.ReferenceDirNames.Should().NotContain("references");
    }

    [Fact]
    public void ActiveBookRoot_Creation_ShouldHaveCorrectValue()
    {
        // Arrange & Act
        var activeBookRoot = new ActiveBookRoot
        {
            ActiveBookRootId = "book_001"
        };

        // Assert
        activeBookRoot.ActiveBookRootId.Should().Be("book_001");
    }

    [Fact]
    public void ActiveBookRoot_NoActiveBook_ShouldBeNull()
    {
        // Arrange & Act
        var activeBookRoot = new ActiveBookRoot();

        // Assert
        activeBookRoot.ActiveBookRootId.Should().BeNull();
    }

    #endregion

    #region Document/Section/Paragraph Tests (TC-DOC-001 ~ TC-DOC-004)

    [Fact]
    public void Document_BasicCreation_ShouldHaveCorrectValues()
    {
        // Arrange & Act
        var document = new Document
        {
            DocId = "doc_001",
            BookRootId = "book_001",
            Path = "/test/chapter01.md",
            Title = "第一章：测试章节"
        };

        // Assert
        document.DocId.Should().Be("doc_001");
        document.BookRootId.Should().Be("book_001");
        document.Path.Should().Be("/test/chapter01.md");
        document.Title.Should().Be("第一章：测试章节");
        document.Sections.Should().NotBeNull();
        document.Sections.Should().BeEmpty();
    }

    [Fact]
    public void Document_WithSections_ShouldContainSections()
    {
        // Arrange
        var document = new Document
        {
            DocId = "doc_002",
            Sections = new List<Section>
            {
                new Section { SectionId = "section_001" },
                new Section { SectionId = "section_002" }
            }
        };

        // Assert
        document.Sections.Should().HaveCount(2);
    }

    [Fact]
    public void Section_HeadingPath_ShouldStoreHierarchy()
    {
        // Arrange & Act
        var section = new Section
        {
            SectionId = "section_001",
            HeadingPath = new List<string> { "第一章", "第一节", "1.1 测试小节" }
        };

        // Assert
        section.HeadingPath.Should().HaveCount(3);
        section.HeadingPath[0].Should().Be("第一章");
        section.HeadingPath[1].Should().Be("第一节");
        section.HeadingPath[2].Should().Be("1.1 测试小节");
    }

    [Fact]
    public void Paragraph_LineRange_ShouldStoreCorrectValues()
    {
        // Arrange & Act
        var paragraph = new Paragraph
        {
            ParagraphId = "p_001",
            StartLine = 10,
            EndLine = 15,
            Content = "这是段落内容",
            Type = ParagraphType.Text
        };

        // Assert
        paragraph.StartLine.Should().Be(10);
        paragraph.EndLine.Should().Be(15);
        paragraph.Content.Should().Be("这是段落内容");
        paragraph.Type.Should().Be(ParagraphType.Text);
    }

    [Fact]
    public void Paragraph_AllParagraphTypes_ShouldBeAssignable()
    {
        // Arrange & Act
        var textParagraph = new Paragraph { Type = ParagraphType.Text };
        var codeParagraph = new Paragraph { Type = ParagraphType.Code };
        var quoteParagraph = new Paragraph { Type = ParagraphType.Quote };
        var listParagraph = new Paragraph { Type = ParagraphType.List };

        // Assert
        textParagraph.Type.Should().Be(ParagraphType.Text);
        codeParagraph.Type.Should().Be(ParagraphType.Code);
        quoteParagraph.Type.Should().Be(ParagraphType.Quote);
        listParagraph.Type.Should().Be(ParagraphType.List);
    }

    [Fact]
    public void Document_ContentHash_ShouldBeNullable()
    {
        // Arrange & Act
        var documentWithoutHash = new Document();
        var documentWithHash = new Document { ContentHash = "abc123hash" };

        // Assert
        documentWithoutHash.ContentHash.Should().BeNull();
        documentWithHash.ContentHash.Should().Be("abc123hash");
    }

    #endregion

    #region SourceSnippet Tests (TC-SS-001 ~ TC-SS-003)

    [Fact]
    public void SourceSnippet_BasicCreation_ShouldHaveCorrectValues()
    {
        // Arrange & Act
        var snippet = new SourceSnippet
        {
            SnippetId = "snippet_001",
            BookRootId = "book_001",
            DocId = "doc_001",
            FilePath = "/test/chapter01.md",
            HeadingPath = new List<string> { "第一章", "第一节" },
            Content = "这是原文片段内容",
            StartLine = 10,
            EndLine = 15
        };

        // Assert
        snippet.SnippetId.Should().Be("snippet_001");
        snippet.BookRootId.Should().Be("book_001");
        snippet.DocId.Should().Be("doc_001");
        snippet.FilePath.Should().Be("/test/chapter01.md");
        snippet.Content.Should().Be("这是原文片段内容");
        snippet.StartLine.Should().Be(10);
        snippet.EndLine.Should().Be(15);
    }

    [Fact]
    public void SourceSnippet_Traceability_ShouldBePreservable()
    {
        // Arrange
        var snippet = new SourceSnippet
        {
            SnippetId = "snippet_trace",
            FilePath = "docs/chapter01.md",
            HeadingPath = new List<string> { "第一章", "核心概念" },
            StartLine = 25,
            EndLine = 30
        };

        // Assert - 验证可追溯性
        snippet.FilePath.Should().Be("docs/chapter01.md");
        snippet.HeadingPath.Should().Contain("核心概念");
        snippet.StartLine.Should().BeLessThanOrEqualTo(snippet.EndLine);
    }

    [Fact]
    public void SourceSnippet_ChunkId_ShouldBeNullable()
    {
        // Arrange & Act
        var snippetWithoutChunk = new SourceSnippet();
        var snippetWithChunk = new SourceSnippet { ChunkId = "chunk_12345" };

        // Assert
        snippetWithoutChunk.ChunkId.Should().BeNull();
        snippetWithChunk.ChunkId.Should().Be("chunk_12345");
    }

    [Fact]
    public void SourceSnippet_EmptyHeadingPath_ShouldWork()
    {
        // Arrange & Act
        var snippet = new SourceSnippet
        {
            SnippetId = "snippet_empty",
            HeadingPath = new List<string>()
        };

        // Assert
        snippet.HeadingPath.Should().NotBeNull();
        snippet.HeadingPath.Should().BeEmpty();
    }

    #endregion

    #region KnowledgePoint Tests (TC-KP-001 ~ TC-KP-005)

    [Fact]
    public void KnowledgePoint_BasicCreation_ShouldHaveCorrectValues()
    {
        // Arrange & Act
        var kp = new KnowledgePoint
        {
            KpId = "kp_001",
            BookRootId = "book_001",
            Title = "核心概念A",
            Importance = 0.8f
        };

        // Assert
        kp.KpId.Should().Be("kp_001");
        kp.BookRootId.Should().Be("book_001");
        kp.Title.Should().Be("核心概念A");
        kp.Importance.Should().Be(0.8f);
        kp.Aliases.Should().NotBeNull();
        kp.Aliases.Should().BeEmpty();
        kp.SnippetIds.Should().NotBeNull();
        kp.SnippetIds.Should().BeEmpty();
        kp.Relations.Should().NotBeNull();
        kp.Relations.Should().BeEmpty();
    }

    [Fact]
    public void KnowledgePoint_ImportanceRange_ShouldAcceptValidValues()
    {
        // Arrange & Act
        var kpMin = new KnowledgePoint { Importance = 0.0f };
        var kpMid = new KnowledgePoint { Importance = 0.5f };
        var kpMax = new KnowledgePoint { Importance = 1.0f };

        // Assert
        kpMin.Importance.Should().Be(0.0f);
        kpMid.Importance.Should().Be(0.5f);
        kpMax.Importance.Should().Be(1.0f);
    }

    [Fact]
    public void KnowledgePoint_ChapterPath_ShouldStoreHierarchy()
    {
        // Arrange & Act
        var kp = new KnowledgePoint
        {
            KpId = "kp_chapter",
            ChapterPath = new List<string> { "第一章", "第二节", "核心概念" }
        };

        // Assert
        kp.ChapterPath.Should().HaveCount(3);
        kp.ChapterPath[0].Should().Be("第一章");
        kp.ChapterPath[1].Should().Be("第二节");
        kp.ChapterPath[2].Should().Be("核心概念");
    }

    [Fact]
    public void KnowledgePoint_WithMultipleSnippets_ShouldStoreAll()
    {
        // Arrange & Act
        var kp = new KnowledgePoint
        {
            KpId = "kp_multi_snippets",
            SnippetIds = new List<string> { "snippet_1", "snippet_2", "snippet_3" }
        };

        // Assert
        kp.SnippetIds.Should().HaveCount(3);
        kp.SnippetIds.Should().Contain("snippet_1");
        kp.SnippetIds.Should().Contain("snippet_2");
        kp.SnippetIds.Should().Contain("snippet_3");
    }

    [Fact]
    public void KnowledgePoint_WithAliases_ShouldStoreAll()
    {
        // Arrange & Act
        var kp = new KnowledgePoint
        {
            KpId = "kp_aliases",
            Aliases = new List<string> { "AI", "人工智能", "Artificial Intelligence" }
        };

        // Assert
        kp.Aliases.Should().HaveCount(3);
    }

    [Fact]
    public void KnowledgeRelation_Prerequisite_ShouldBeCreatable()
    {
        // Arrange & Act
        var relation = new KnowledgeRelation
        {
            ToKpId = "kp_002",
            Type = RelationType.Prerequisite
        };

        // Assert
        relation.ToKpId.Should().Be("kp_002");
        relation.Type.Should().Be(RelationType.Prerequisite);
    }

    [Fact]
    public void KnowledgeRelation_AllRelationTypes_ShouldBeAssignable()
    {
        // Arrange & Act
        var prerequisite = new KnowledgeRelation { Type = RelationType.Prerequisite };
        var comparison = new KnowledgeRelation { Type = RelationType.Comparison };
        var contains = new KnowledgeRelation { Type = RelationType.Contains };
        var related = new KnowledgeRelation { Type = RelationType.Related };

        // Assert
        prerequisite.Type.Should().Be(RelationType.Prerequisite);
        comparison.Type.Should().Be(RelationType.Comparison);
        contains.Type.Should().Be(RelationType.Contains);
        related.Type.Should().Be(RelationType.Related);
    }

    [Fact]
    public void KnowledgePoint_WithRelations_ShouldStoreAll()
    {
        // Arrange & Act
        var kp = new KnowledgePoint
        {
            KpId = "kp_with_relations",
            Relations = new List<KnowledgeRelation>
            {
                new KnowledgeRelation { ToKpId = "kp_002", Type = RelationType.Prerequisite },
                new KnowledgeRelation { ToKpId = "kp_003", Type = RelationType.Comparison }
            }
        };

        // Assert
        kp.Relations.Should().HaveCount(2);
        kp.Relations[0].Type.Should().Be(RelationType.Prerequisite);
        kp.Relations[1].Type.Should().Be(RelationType.Comparison);
    }

    #endregion

    #region KnowledgeSystem Tests (TC-KS-001 ~ TC-KS-005)

    [Fact]
    public void KnowledgeSystem_BasicCreation_ShouldHaveCorrectValues()
    {
        // Arrange & Act
        var ks = new KnowledgeSystem
        {
            BookRootId = "book_001"
        };

        // Assert
        ks.BookRootId.Should().Be("book_001");
        ks.KnowledgePoints.Should().NotBeNull();
        ks.KnowledgePoints.Should().BeEmpty();
        ks.Snippets.Should().NotBeNull();
        ks.Snippets.Should().BeEmpty();
        ks.Tree.Should().BeNull();
    }

    [Fact]
    public void KnowledgeSystem_SnippetsDictionary_ShouldStoreAndRetrieve()
    {
        // Arrange
        var ks = new KnowledgeSystem
        {
            Snippets = new Dictionary<string, SourceSnippet>
            {
                ["snippet_1"] = new SourceSnippet { SnippetId = "snippet_1", Content = "内容1" },
                ["snippet_2"] = new SourceSnippet { SnippetId = "snippet_2", Content = "内容2" }
            }
        };

        // Act & Assert
        ks.Snippets.Should().ContainKey("snippet_1");
        ks.Snippets.Should().ContainKey("snippet_2");
        ks.Snippets["snippet_1"].Content.Should().Be("内容1");
        ks.Snippets["snippet_2"].Content.Should().Be("内容2");
    }

    [Fact]
    public void KnowledgeTreeNode_BasicCreation_ShouldHaveCorrectValues()
    {
        // Arrange & Act
        var node = new KnowledgeTreeNode
        {
            Id = "node_001",
            Title = "测试节点"
        };

        // Assert
        node.Id.Should().Be("node_001");
        node.Title.Should().Be("测试节点");
        node.Children.Should().NotBeNull();
        node.Children.Should().BeEmpty();
        node.KnowledgePoint.Should().BeNull();
        node.HeadingPath.Should().NotBeNull();
        node.HeadingPath.Should().BeEmpty();
    }

    [Fact]
    public void KnowledgeTreeNode_Hierarchy_ShouldBuildCorrectly()
    {
        // Arrange
        var rootNode = new KnowledgeTreeNode
        {
            Id = "root",
            Title = "根节点",
            HeadingPath = new List<string>()
        };

        var childNode = new KnowledgeTreeNode
        {
            Id = "child_001",
            Title = "子节点",
            HeadingPath = new List<string> { "根节点", "子节点" }
        };

        rootNode.Children.Add(childNode);

        // Assert
        rootNode.Children.Should().HaveCount(1);
        rootNode.Children[0].Title.Should().Be("子节点");
    }

    [Fact]
    public void KnowledgeTreeNode_WithKnowledgePoint_ShouldAssociate()
    {
        // Arrange
        var kp = new KnowledgePoint { KpId = "kp_001", Title = "知识点A" };
        var node = new KnowledgeTreeNode
        {
            Id = "node_kp",
            Title = "知识点节点",
            KnowledgePoint = kp
        };

        // Assert
        node.KnowledgePoint.Should().NotBeNull();
        node.KnowledgePoint!.KpId.Should().Be("kp_001");
    }

    [Fact]
    public void KnowledgeTreeNode_NullKnowledgePoint_ShouldBeAllowed()
    {
        // Arrange & Act
        var sectionNode = new KnowledgeTreeNode
        {
            Id = "section_001",
            Title = "章节节点（非叶子）"
            // KnowledgePoint 保持为 null
        };

        // Assert
        sectionNode.KnowledgePoint.Should().BeNull();
    }

    [Fact]
    public void KnowledgeSystem_WithKnowledgePoints_ShouldStoreAll()
    {
        // Arrange
        var ks = new KnowledgeSystem
        {
            KnowledgePoints = new List<KnowledgePoint>
            {
                new KnowledgePoint { KpId = "kp_001", Title = "知识点A" },
                new KnowledgePoint { KpId = "kp_002", Title = "知识点B" }
            }
        };

        // Assert
        ks.KnowledgePoints.Should().HaveCount(2);
        ks.KnowledgePoints[0].KpId.Should().Be("kp_001");
        ks.KnowledgePoints[1].KpId.Should().Be("kp_002");
    }

    #endregion

    #region LearningPack Tests (TC-LP-001 ~ TC-LP-004)

    [Fact]
    public void LearningPack_BasicCreation_ShouldHaveCorrectValues()
    {
        // Arrange & Act
        var lp = new LearningPack
        {
            KpId = "kp_001"
        };

        // Assert
        lp.KpId.Should().Be("kp_001");
        lp.Summary.Should().NotBeNull();
        lp.Levels.Should().NotBeNull();
        lp.Levels.Should().BeEmpty();
        lp.SnippetIds.Should().NotBeNull();
        lp.SnippetIds.Should().BeEmpty();
        lp.RelatedKpIds.Should().NotBeNull();
        lp.RelatedKpIds.Should().BeEmpty();
    }

    [Fact]
    public void Summary_DefinitionAndKeyPoints_ShouldStore()
    {
        // Arrange & Act
        var summary = new Summary
        {
            Definition = "这是一个定义",
            KeyPoints = new List<string> { "要点1", "要点2", "要点3" },
            Pitfalls = new List<string> { "常见误区1" }
        };

        // Assert
        summary.Definition.Should().Be("这是一个定义");
        summary.KeyPoints.Should().HaveCount(3);
        summary.Pitfalls.Should().HaveCount(1);
    }

    [Fact]
    public void Summary_EmptyPitfalls_ShouldBeAllowed()
    {
        // Arrange & Act
        var summary = new Summary
        {
            Definition = "定义",
            KeyPoints = new List<string> { "要点1" },
            Pitfalls = new List<string>()
        };

        // Assert
        summary.Pitfalls.Should().NotBeNull();
        summary.Pitfalls.Should().BeEmpty();
    }

    [Fact]
    public void ContentLevel_AllLevels_ShouldBeCreatable()
    {
        // Arrange & Act
        var level1 = new ContentLevel { Level = 1, Title = "概览", Content = "概览内容" };
        var level2 = new ContentLevel { Level = 2, Title = "详细", Content = "详细内容" };
        var level3 = new ContentLevel { Level = 3, Title = "深入", Content = "深入内容" };

        // Assert
        level1.Level.Should().Be(1);
        level1.Title.Should().Be("概览");
        level2.Level.Should().Be(2);
        level2.Title.Should().Be("详细");
        level3.Level.Should().Be(3);
        level3.Title.Should().Be("深入");
    }

    [Fact]
    public void ContentLevel_LevelValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var levels = new List<ContentLevel>
        {
            new ContentLevel { Level = 1 },
            new ContentLevel { Level = 2 },
            new ContentLevel { Level = 3 }
        };

        // Assert
        levels[0].Level.Should().Be(1);
        levels[1].Level.Should().Be(2);
        levels[2].Level.Should().Be(3);
    }

    [Fact]
    public void LearningPack_WithLevels_ShouldStoreAll()
    {
        // Arrange
        var lp = new LearningPack
        {
            KpId = "kp_001",
            Levels = new List<ContentLevel>
            {
                new ContentLevel { Level = 1, Title = "概览", Content = "L1内容" },
                new ContentLevel { Level = 2, Title = "详细", Content = "L2内容" }
            }
        };

        // Assert
        lp.Levels.Should().HaveCount(2);
        lp.Levels[0].Level.Should().Be(1);
        lp.Levels[1].Level.Should().Be(2);
    }

    [Fact]
    public void LearningPack_SnippetAssociation_ShouldWork()
    {
        // Arrange
        var lp = new LearningPack
        {
            KpId = "kp_001",
            SnippetIds = new List<string> { "snippet_1", "snippet_2" }
        };

        // Assert
        lp.SnippetIds.Should().HaveCount(2);
    }

    [Fact]
    public void LearningPack_RelatedKpIds_ShouldStoreReferences()
    {
        // Arrange
        var lp = new LearningPack
        {
            KpId = "kp_001",
            RelatedKpIds = new List<string> { "kp_002", "kp_003" }
        };

        // Assert
        lp.RelatedKpIds.Should().HaveCount(2);
        lp.RelatedKpIds.Should().Contain("kp_002");
        lp.RelatedKpIds.Should().Contain("kp_003");
    }

    #endregion

    #region Exercise Tests (TC-EX-001 ~ TC-EX-008)

    [Fact]
    public void Exercise_BasicCreation_ShouldHaveCorrectValues()
    {
        // Arrange & Act
        var exercise = new Exercise
        {
            ExerciseId = "ex_001",
            KpId = "kp_001",
            Type = ExerciseType.SingleChoice,
            Question = "测试问题",
            CorrectAnswer = "A"
        };

        // Assert
        exercise.ExerciseId.Should().Be("ex_001");
        exercise.KpId.Should().Be("kp_001");
        exercise.Type.Should().Be(ExerciseType.SingleChoice);
        exercise.Question.Should().Be("测试问题");
        exercise.CorrectAnswer.Should().Be("A");
        exercise.Options.Should().NotBeNull();
        exercise.Options.Should().BeEmpty();
        exercise.EvidenceSnippetIds.Should().NotBeNull();
        exercise.EvidenceSnippetIds.Should().BeEmpty();
        exercise.KeyPoints.Should().NotBeNull();
        exercise.KeyPoints.Should().BeEmpty();
    }

    [Fact]
    public void Exercise_AllTypes_ShouldBeCreatable()
    {
        // Arrange & Act
        var singleChoice = new Exercise { Type = ExerciseType.SingleChoice };
        var fillBlank = new Exercise { Type = ExerciseType.FillBlank };
        var shortAnswer = new Exercise { Type = ExerciseType.ShortAnswer };

        // Assert
        singleChoice.Type.Should().Be(ExerciseType.SingleChoice);
        fillBlank.Type.Should().Be(ExerciseType.FillBlank);
        shortAnswer.Type.Should().Be(ExerciseType.ShortAnswer);
    }

    [Fact]
    public void Exercise_ChoiceQuestion_ShouldHaveOptions()
    {
        // Arrange
        var exercise = new Exercise
        {
            ExerciseId = "ex_choice",
            Type = ExerciseType.SingleChoice,
            Question = "以下哪项是正确的？",
            Options = new List<string> { "A. 选项1", "B. 选项2", "C. 选项3", "D. 选项4" },
            CorrectAnswer = "B"
        };

        // Assert
        exercise.Options.Should().HaveCount(4);
        exercise.CorrectAnswer.Should().Be("B");
    }

    [Fact]
    public void Exercise_FillBlankQuestion_ShouldHaveNoOptions()
    {
        // Arrange
        var exercise = new Exercise
        {
            ExerciseId = "ex_fillblank",
            Type = ExerciseType.FillBlank,
            Question = "人工智能的缩写是_____。",
            CorrectAnswer = "AI"
        };

        // Assert
        exercise.Options.Should().NotBeNull();
        exercise.Options.Should().BeEmpty();
        exercise.CorrectAnswer.Should().Be("AI");
    }

    [Fact]
    public void Exercise_ShortAnswer_ShouldHaveKeyPoints()
    {
        // Arrange
        var exercise = new Exercise
        {
            ExerciseId = "ex_short",
            Type = ExerciseType.ShortAnswer,
            Question = "请简述什么是人工智能？",
            CorrectAnswer = "参考答案内容...",
            KeyPoints = new List<string> { "感知能力", "决策能力", "执行能力" }
        };

        // Assert
        exercise.KeyPoints.Should().HaveCount(3);
        exercise.KeyPoints.Should().Contain("感知能力");
    }

    [Fact]
    public void Exercise_EvidenceSnippetIds_ShouldStoreReferences()
    {
        // Arrange
        var exercise = new Exercise
        {
            ExerciseId = "ex_evidence",
            EvidenceSnippetIds = new List<string> { "snippet_1", "snippet_2" }
        };

        // Assert
        exercise.EvidenceSnippetIds.Should().HaveCount(2);
    }

    [Fact]
    public void ExerciseFeedback_CorrectAnswer_ShouldHaveTrue()
    {
        // Arrange & Act
        var feedback = new ExerciseFeedback
        {
            IsCorrect = true,
            Explanation = "回答正确！",
            ReferenceAnswer = "A"
        };

        // Assert
        feedback.IsCorrect.Should().BeTrue();
        feedback.Explanation.Should().Be("回答正确！");
        feedback.ReferenceAnswer.Should().Be("A");
    }

    [Fact]
    public void ExerciseFeedback_WrongAnswer_ShouldHaveFalse()
    {
        // Arrange & Act
        var feedback = new ExerciseFeedback
        {
            IsCorrect = false,
            Explanation = "回答错误，正确答案是 A",
            ReferenceAnswer = "A"
        };

        // Assert
        feedback.IsCorrect.Should().BeFalse();
    }

    [Fact]
    public void ExerciseFeedback_ShortAnswer_ShouldAllowNullIsCorrect()
    {
        // Arrange & Act
        var feedback = new ExerciseFeedback
        {
            IsCorrect = null, // 简答题可能无法明确判定
            Explanation = "部分要点已覆盖",
            CoveredPoints = new List<string> { "要点1", "要点2" },
            MissingPoints = new List<string> { "要点3" }
        };

        // Assert
        feedback.IsCorrect.Should().BeNull();
        feedback.CoveredPoints.Should().HaveCount(2);
        feedback.MissingPoints.Should().HaveCount(1);
    }

    [Fact]
    public void ExerciseFeedback_Points_ShouldBeLists()
    {
        // Arrange & Act
        var feedback = new ExerciseFeedback
        {
            CoveredPoints = new List<string> { "已覆盖的要点" },
            MissingPoints = new List<string> { "遗漏的要点" }
        };

        // Assert
        feedback.CoveredPoints.Should().NotBeNull();
        feedback.MissingPoints.Should().NotBeNull();
    }

    #endregion

    #region ExerciseTypeConverter Tests (TC-CONV-001 ~ TC-CONV-005)

    [Fact]
    public void ExerciseTypeConverter_EnglishValues_ShouldDeserialize()
    {
        // Arrange
        var json = @"{ ""type"": ""SingleChoice"" }";
        var options = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new ExerciseTypeConverter() }
        };

        // Act
        var result = JsonConvert.DeserializeObject<ExerciseTypeHolder>(json, options);

        // Assert
        result!.Type.Should().Be(ExerciseType.SingleChoice);
    }

    [Fact]
    public void ExerciseTypeConverter_ChineseValues_ShouldDeserialize()
    {
        // Arrange
        var json = @"{ ""type"": ""选择题"" }";
        var options = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new ExerciseTypeConverter() }
        };

        // Act
        var result = JsonConvert.DeserializeObject<ExerciseTypeHolder>(json, options);

        // Assert
        result!.Type.Should().Be(ExerciseType.SingleChoice);
    }

    [Fact]
    public void ExerciseTypeConverter_FillBlankChinese_ShouldDeserialize()
    {
        // Arrange
        var json = @"{ ""type"": ""填空题"" }";
        var options = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new ExerciseTypeConverter() }
        };

        // Act
        var result = JsonConvert.DeserializeObject<ExerciseTypeHolder>(json, options);

        // Assert
        result!.Type.Should().Be(ExerciseType.FillBlank);
    }

    [Fact]
    public void ExerciseTypeConverter_ShortAnswerChinese_ShouldDeserialize()
    {
        // Arrange
        var json = @"{ ""type"": ""简答题"" }";
        var options = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new ExerciseTypeConverter() }
        };

        // Act
        var result = JsonConvert.DeserializeObject<ExerciseTypeHolder>(json, options);

        // Assert
        result!.Type.Should().Be(ExerciseType.ShortAnswer);
    }

    [Fact]
    public void ExerciseTypeConverter_ShortAliases_ShouldDeserialize()
    {
        // Arrange & Act & Assert
        var aliases = new Dictionary<string, ExerciseType>
        {
            ["单选"] = ExerciseType.SingleChoice,
            ["填空"] = ExerciseType.FillBlank,
            ["简答"] = ExerciseType.ShortAnswer
        };

        foreach (var alias in aliases)
        {
            var json = string.Format(@"{{ ""type"": ""{0}"" }}", alias.Key);
            var options = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new ExerciseTypeConverter() }
            };
            var result = JsonConvert.DeserializeObject<ExerciseTypeHolder>(json, options);
            result!.Type.Should().Be(alias.Value, string.Format("Alias '{0}' should map to {1}", alias.Key, alias.Value));
        }
    }

    [Fact]
    public void ExerciseTypeConverter_CaseInsensitive_ShouldDeserialize()
    {
        // Arrange
        var json = @"{ ""type"": ""singlechoice"" }";
        var options = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new ExerciseTypeConverter() }
        };

        // Act
        var result = JsonConvert.DeserializeObject<ExerciseTypeHolder>(json, options);

        // Assert
        result!.Type.Should().Be(ExerciseType.SingleChoice);
    }

    [Fact]
    public void ExerciseTypeConverter_Uppercase_ShouldDeserialize()
    {
        // Arrange
        var json = @"{ ""type"": ""FILLBLANK"" }";
        var options = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new ExerciseTypeConverter() }
        };

        // Act
        var result = JsonConvert.DeserializeObject<ExerciseTypeHolder>(json, options);

        // Assert
        result!.Type.Should().Be(ExerciseType.FillBlank);
    }

    [Fact]
    public void ExerciseTypeConverter_Serialization_ShouldOutputEnglish()
    {
        // Arrange
        var exercise = new Exercise { Type = ExerciseType.SingleChoice };
        var options = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new ExerciseTypeConverter() }
        };

        // Act
        var json = JsonConvert.SerializeObject(exercise, options);

        // Assert
        json.Should().Contain("SingleChoice");
        json.Should().NotContain("1"); // 不应该是数字
    }

    [Fact]
    public void ExerciseTypeConverter_UnknownValue_ShouldReturnDefault()
    {
        // Arrange
        var json = @"{ ""type"": ""未知类型"" }";
        var options = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new ExerciseTypeConverter() }
        };

        // Act
        var result = JsonConvert.DeserializeObject<ExerciseTypeHolder>(json, options);

        // Assert
        result!.Type.Should().Be(ExerciseType.SingleChoice); // 默认值
    }

    [Fact]
    public void ExerciseTypeConverter_NullValue_ShouldReturnDefault()
    {
        // Arrange
        var json = @"{ ""type"": null }";
        var options = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> { new ExerciseTypeConverter() }
        };

        // Act
        var result = JsonConvert.DeserializeObject<ExerciseTypeHolder>(json, options);

        // Assert
        result!.Type.Should().Be(ExerciseType.SingleChoice); // 默认值
    }

    #endregion

    #region JSON Serialization Tests (TC-JSON-001 ~ TC-JSON-005)

    [Fact]
    public void JsonSerialization_Exercise_ShouldSerializeCorrectly()
    {
        // Arrange
        var exercise = new Exercise
        {
            ExerciseId = "ex_001",
            KpId = "kp_001",
            Type = ExerciseType.SingleChoice,
            Question = "测试问题？",
            Options = new List<string> { "A. 选项1", "B. 选项2" },
            CorrectAnswer = "A"
        };

        // Act
        var json = JsonConvert.SerializeObject(exercise, new ExerciseTypeConverter());

        // Assert
        json.Should().Contain("\"ExerciseId\":\"ex_001\"");
        json.Should().Contain("\"Type\":\"SingleChoice\"");
        json.Should().Contain("\"Question\":\"测试问题？\"");
    }

    [Fact]
    public void JsonSerialization_Exercise_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = @"{
            ""ExerciseId"": ""ex_001"",
            ""KpId"": ""kp_001"",
            ""Type"": ""SingleChoice"",
            ""Question"": ""测试问题？"",
            ""Options"": [""A. 选项1"", ""B. 选项2""],
            ""CorrectAnswer"": ""A""
        }";

        // Act
        var result = JsonConvert.DeserializeObject<Exercise>(json, new ExerciseTypeConverter());

        // Assert
        result.Should().NotBeNull();
        result!.ExerciseId.Should().Be("ex_001");
        result.Type.Should().Be(ExerciseType.SingleChoice);
        result.Options.Should().HaveCount(2);
    }

    [Fact]
    public void JsonSerialization_KnowledgeSystem_ShouldSerializeCorrectly()
    {
        // Arrange
        var ks = new KnowledgeSystem
        {
            BookRootId = "book_001",
            KnowledgePoints = new List<KnowledgePoint>
            {
                new KnowledgePoint { KpId = "kp_001", Title = "知识点A" }
            },
            Snippets = new Dictionary<string, SourceSnippet>
            {
                ["snippet_1"] = new SourceSnippet { SnippetId = "snippet_1", Content = "内容" }
            }
        };

        // Act
        var json = JsonConvert.SerializeObject(ks);

        // Assert
        json.Should().Contain("\"BookRootId\":\"book_001\"");
        json.Should().Contain("\"KnowledgePoints\"");
        json.Should().Contain("\"Snippets\"");
    }

    [Fact]
    public void JsonSerialization_KnowledgeSystem_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = @"{
            ""BookRootId"": ""book_001"",
            ""KnowledgePoints"": [
                { ""KpId"": ""kp_001"", ""Title"": ""知识点A"", ""Importance"": 0.8 }
            ],
            ""Snippets"": {
                ""snippet_1"": { ""SnippetId"": ""snippet_1"", ""Content"": ""内容"" }
            },
            ""Tree"": null
        }";

        // Act
        var result = JsonConvert.DeserializeObject<KnowledgeSystem>(json);

        // Assert
        result.Should().NotBeNull();
        result!.BookRootId.Should().Be("book_001");
        result.KnowledgePoints.Should().HaveCount(1);
        result.Snippets.Should().ContainKey("snippet_1");
    }

    [Fact]
    public void JsonSerialization_EmptyCollections_ShouldSerialize()
    {
        // Arrange
        var exercise = new Exercise
        {
            ExerciseId = "ex_empty",
            Options = new List<string>(),
            EvidenceSnippetIds = new List<string>(),
            KeyPoints = new List<string>()
        };

        // Act
        var json = JsonConvert.SerializeObject(exercise);

        // Assert
        json.Should().Contain("\"Options\":[]");
        json.Should().Contain("\"EvidenceSnippetIds\":[]");
        json.Should().Contain("\"KeyPoints\":[]");
    }

    [Fact]
    public void JsonSerialization_NullValues_ShouldSerialize()
    {
        // Arrange
        var document = new Document
        {
            DocId = "doc_001",
            ContentHash = null
        };

        // Act
        var json = JsonConvert.SerializeObject(document);

        // Assert
        json.Should().Contain("\"DocId\":\"doc_001\"");
        // null 值通常会被忽略或序列化为 null
    }

    [Fact]
    public void JsonSerialization_LearningPack_ShouldSerializeLevels()
    {
        // Arrange
        var lp = new LearningPack
        {
            KpId = "kp_001",
            Summary = new Summary
            {
                Definition = "定义",
                KeyPoints = new List<string> { "要点1", "要点2" }
            },
            Levels = new List<ContentLevel>
            {
                new ContentLevel { Level = 1, Title = "概览", Content = "概览内容" }
            }
        };

        // Act
        var json = JsonConvert.SerializeObject(lp);

        // Assert
        json.Should().Contain("\"Summary\"");
        json.Should().Contain("\"Definition\":\"定义\"");
        json.Should().Contain("\"Levels\"");
        json.Should().Contain("\"Level\":1");
    }

    [Fact]
    public void JsonSerialization_RoundTrip_ShouldPreserveData()
    {
        // Arrange
        var original = new Exercise
        {
            ExerciseId = "ex_roundtrip",
            KpId = "kp_001",
            Type = ExerciseType.FillBlank,
            Question = "完整性问题？",
            CorrectAnswer = "答案",
            KeyPoints = new List<string> { "要点1", "要点2", "要点3" }
        };

        // Act
        var json = JsonConvert.SerializeObject(original);
        var deserialized = JsonConvert.DeserializeObject<Exercise>(json, new ExerciseTypeConverter());

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.ExerciseId.Should().Be(original.ExerciseId);
        deserialized.KpId.Should().Be(original.KpId);
        deserialized.Type.Should().Be(original.Type);
        deserialized.Question.Should().Be(original.Question);
        deserialized.CorrectAnswer.Should().Be(original.CorrectAnswer);
        deserialized.KeyPoints.Should().HaveCount(3);
    }

    #endregion
}

/// <summary>
/// 用于测试 ExerciseTypeConverter 的辅助类
/// </summary>
public class ExerciseTypeHolder
{
    [JsonConverter(typeof(ExerciseTypeConverter))]
    public ExerciseType Type { get; set; } = ExerciseType.SingleChoice;
}
