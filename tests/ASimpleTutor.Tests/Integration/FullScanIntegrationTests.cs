using ASimpleTutor.Core.Configuration;
using ASimpleTutor.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ASimpleTutor.Tests.Integration;

/// <summary>
/// 完整扫描流程集成测试
/// 对应测试需求文档：TC-INT-006 ~ TC-INT-010
/// </summary>
public class FullScanIntegrationTests
{
    private readonly Mock<ILogger<MarkdownScanner>> _loggerMock;
    private readonly IOptions<SectioningOptions> _sectioningOptions;

    public FullScanIntegrationTests()
    {
        _loggerMock = new Mock<ILogger<MarkdownScanner>>();
        _sectioningOptions = Options.Create(new SectioningOptions());
    }

    [Fact]
    public async Task FullScan_ShouldProcessMultipleMarkdownFiles()
    {
        // Arrange
        using var tempDir = new TempDirectory();
        tempDir.CreateFile("chapter1.md", "# Chapter 1\n## Section 1.1\nContent for section 1.1");
        tempDir.CreateFile("chapter2.md", "# Chapter 2\n## Section 2.1\nContent for section 2.1");

        var scanner = new MarkdownScanner(_loggerMock.Object, _sectioningOptions);

        // Act
        var documents = await scanner.ScanAsync(tempDir.Path, CancellationToken.None);

        // Assert
        documents.Should().NotBeNull();
        documents.Should().HaveCount(2);
        documents.ForEach(doc => doc.Sections.Should().NotBeEmpty());
    }

    [Fact]
    public async Task FullScan_ShouldHandleNestedDirectories()
    {
        // Arrange
        using var tempDir = new TempDirectory();
        tempDir.CreateDirectory("subdir");
        tempDir.CreateFile("file1.md", "# Root File\nContent");
        tempDir.CreateFile("subdir/file2.md", "# Subdirectory File\nContent");

        var scanner = new MarkdownScanner(_loggerMock.Object, _sectioningOptions);

        // Act
        var documents = await scanner.ScanAsync(tempDir.Path, CancellationToken.None);

        // Assert
        documents.Should().NotBeNull();
        documents.Should().HaveCount(2);
    }

    [Fact]
    public async Task FullScan_ShouldHandleMixedFileTypes()
    {
        // Arrange
        using var tempDir = new TempDirectory();
        tempDir.CreateFile("doc.md", "# Markdown File\nContent");
        tempDir.CreateFile("doc.txt", "Plain text file");
        tempDir.CreateFile("doc.cs", "// C# file");

        var scanner = new MarkdownScanner(_loggerMock.Object, _sectioningOptions);

        // Act
        var documents = await scanner.ScanAsync(tempDir.Path, CancellationToken.None);

        // Assert
        documents.Should().NotBeNull();
        documents.Should().HaveCount(1); // 只应该处理 Markdown 文件
    }

    [Fact]
    public async Task FullScan_ShouldHandleLargeMarkdownFile()
    {
        // Arrange
        using var tempDir = new TempDirectory();
        var largeContent = "# Title\n" + string.Join("\n", Enumerable.Range(1, 100).Select(i => $"## Section {i}\nContent for section {i}"));
        tempDir.CreateFile("large.md", largeContent);

        var scanner = new MarkdownScanner(_loggerMock.Object, _sectioningOptions);

        // Act
        var documents = await scanner.ScanAsync(tempDir.Path, CancellationToken.None);

        // Assert
        documents.Should().NotBeNull();
        documents.Should().HaveCount(1);
        documents[0].Sections.Should().NotBeEmpty();
    }

    [Fact]
    public async Task FullScan_ShouldHandleEmptyMarkdownFile()
    {
        // Arrange
        using var tempDir = new TempDirectory();
        tempDir.CreateFile("empty.md", string.Empty);

        var scanner = new MarkdownScanner(_loggerMock.Object, _sectioningOptions);

        // Act
        var documents = await scanner.ScanAsync(tempDir.Path, CancellationToken.None);

        // Assert
        documents.Should().NotBeNull();
        documents.Should().HaveCount(1);
    }

    #region Helper Class

    private class TempDirectory : IDisposable
    {
        public string Path { get; }

        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "full-scan-test-", System.IO.Path.GetRandomFileName());
            Directory.CreateDirectory(Path);
        }

        public void CreateFile(string relativePath, string content)
        {
            var fullPath = System.IO.Path.Combine(Path, relativePath);
            var directory = System.IO.Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllText(fullPath, content);
        }

        public void CreateDirectory(string relativePath)
        {
            var fullPath = System.IO.Path.Combine(Path, relativePath);
            Directory.CreateDirectory(fullPath);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }

    #endregion
}
