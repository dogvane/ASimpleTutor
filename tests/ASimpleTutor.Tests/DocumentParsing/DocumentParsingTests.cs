using ASimpleTutor.Core.Models;
using FluentAssertions;
using System.Collections.Generic;
using Xunit;

namespace ASimpleTutor.Tests.DocumentParsing;

/// <summary>
/// 文档解析测试
/// 对应测试需求文档：TC-DP-001 ~ TC-DP-005
/// </summary>
public class DocumentParsingTests
{
    #region 文档结构测试

    [Fact]
    public void Document_ShouldHaveCorrectStructure()
    {
        // Arrange
        var document = new Document
        {
            DocId = "doc1",
            BookRootId = "book1",
            Path = "/path/to/doc.md",
            Title = "Test Document",
            Sections = new List<Section>
            {
                new Section
                {
                    SectionId = "section1",
                    HeadingPath = new List<string> { "Chapter 1" },
                    SubSections = new List<Section>
                    {
                        new Section
                        {
                            SectionId = "sub1",
                            HeadingPath = new List<string> { "Chapter 1", "Section 1" }
                        }
                    }
                }
            }
        };

        // Assert
        document.DocId.Should().Be("doc1");
        document.Sections.Should().HaveCount(1);
        document.Sections[0].SectionId.Should().Be("section1");
        document.Sections[0].SubSections.Should().HaveCount(1);
        document.Sections[0].SubSections[0].SectionId.Should().Be("sub1");
    }

    [Fact]
    public void Section_ShouldHaveCorrectStructure()
    {
        // Arrange
        var section = new Section
        {
            SectionId = "section1",
            HeadingPath = new List<string> { "Chapter 1", "Section 1" },
            StartLine = 10,
            EndLine = 20,
            OriginalLength = 500,
            EffectiveLength = 400,
            FilteredLength = 100,
            IsExcluded = false
        };

        // Assert
        section.SectionId.Should().Be("section1");
        section.HeadingPath.Should().ContainInOrder("Chapter 1", "Section 1");
        section.StartLine.Should().Be(10);
        section.EndLine.Should().Be(20);
        section.OriginalLength.Should().Be(500);
        section.EffectiveLength.Should().Be(400);
        section.FilteredLength.Should().Be(100);
        section.IsExcluded.Should().BeFalse();
    }

    [Fact]
    public void Section_ShouldHandleSubSections()
    {
        // Arrange
        var section = new Section
        {
            SectionId = "parent",
            HeadingPath = new List<string> { "Chapter 1" },
            SubSections = new List<Section>
            {
                new Section
                {
                    SectionId = "child1",
                    HeadingPath = new List<string> { "Chapter 1", "Section 1" }
                },
                new Section
                {
                    SectionId = "child2",
                    HeadingPath = new List<string> { "Chapter 1", "Section 2" }
                }
            }
        };

        // Assert
        section.SubSections.Should().HaveCount(2);
        section.SubSections[0].SectionId.Should().Be("child1");
        section.SubSections[1].SectionId.Should().Be("child2");
    }

    #endregion

    #region 文档解析边界测试

    [Fact]
    public void Document_ShouldHandleEmptySections()
    {
        // Arrange
        var document = new Document
        {
            DocId = "doc1",
            Title = "Empty Sections Document",
            Sections = new List<Section>()
        };

        // Assert
        document.Sections.Should().NotBeNull();
        document.Sections.Should().BeEmpty();
    }

    [Fact]
    public void Section_ShouldHandleEmptySubSections()
    {
        // Arrange
        var section = new Section
        {
            SectionId = "section1",
            HeadingPath = new List<string> { "Chapter 1" },
            SubSections = new List<Section>()
        };

        // Assert
        section.SubSections.Should().NotBeNull();
        section.SubSections.Should().BeEmpty();
    }

    [Fact]
    public void Section_ShouldHandleEmptyHeadingPath()
    {
        // Arrange
        var section = new Section
        {
            SectionId = "section1",
            HeadingPath = new List<string>(),
            StartLine = 1,
            EndLine = 10
        };

        // Assert
        section.HeadingPath.Should().NotBeNull();
        section.HeadingPath.Should().BeEmpty();
        section.StartLine.Should().Be(1);
        section.EndLine.Should().Be(10);
    }

    #endregion

    #region 文档哈希测试

    [Fact]
    public void Document_ShouldAllowContentHash()
    {
        // Arrange
        var document = new Document
        {
            DocId = "doc1",
            Title = "Test Document",
            ContentHash = "test-hash-123"
        };

        // Assert
        document.ContentHash.Should().Be("test-hash-123");
    }

    [Fact]
    public void Document_ShouldHandleNullContentHash()
    {
        // Arrange
        var document = new Document
        {
            DocId = "doc1",
            Title = "Test Document",
            ContentHash = null
        };

        // Assert
        document.ContentHash.Should().BeNull();
    }

    #endregion

    #region 章节长度测试

    [Fact]
    public void Section_ShouldHaveValidLengths()
    {
        // Arrange
        var section = new Section
        {
            SectionId = "section1",
            HeadingPath = new List<string> { "Chapter 1" },
            OriginalLength = 1000,
            EffectiveLength = 800,
            FilteredLength = 200
        };

        // Assert
        section.OriginalLength.Should().Be(1000);
        section.EffectiveLength.Should().Be(800);
        section.FilteredLength.Should().Be(200);
        section.OriginalLength.Should().Be(section.EffectiveLength + section.FilteredLength);
    }

    [Fact]
    public void Section_ShouldHandleZeroLengths()
    {
        // Arrange
        var section = new Section
        {
            SectionId = "section1",
            HeadingPath = new List<string> { "Chapter 1" },
            OriginalLength = 0,
            EffectiveLength = 0,
            FilteredLength = 0
        };

        // Assert
        section.OriginalLength.Should().Be(0);
        section.EffectiveLength.Should().Be(0);
        section.FilteredLength.Should().Be(0);
    }

    #endregion
}
