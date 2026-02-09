using ASimpleTutor.Core.Configuration;
using ASimpleTutor.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ASimpleTutor.Tests.FileDiscovery;

/// <summary>
/// 文件发现与扫描测试
/// 对应测试需求文档：TC-FD-001 ~ TC-FD-005
/// </summary>
public class FileDiscoveryTests
{
    private readonly Mock<ILogger<MarkdownScanner>> _loggerMock;
    private readonly IOptions<SectioningOptions> _sectioningOptions;

    public FileDiscoveryTests()
    {
        _loggerMock = new Mock<ILogger<MarkdownScanner>>();
        _sectioningOptions = Options.Create(new SectioningOptions());
    }

    [Fact]
    public async Task MarkdownScanner_ShouldDiscoverMarkdownFiles()
    {
        // Arrange
        using var tempDir = new TempDirectory();
        tempDir.CreateFile("file1.md", "# Test 1\nContent 1");
        tempDir.CreateFile("file2.md", "# Test 2\nContent 2");
        tempDir.CreateFile("file3.txt", "Not markdown"); // 非 Markdown 文件

        var scanner = new MarkdownScanner(_loggerMock.Object, _sectioningOptions);

        // Act
        var documents = await scanner.ScanAsync(tempDir.Path, CancellationToken.None);

        // Assert
        documents.Should().NotBeNull();
        documents.Should().HaveCount(2); // 只应该找到 2 个 Markdown 文件
    }

    [Fact]
    public async Task MarkdownScanner_ShouldHandleEmptyDirectory()
    {
        // Arrange
        using var tempDir = new TempDirectory();
        var scanner = new MarkdownScanner(_loggerMock.Object, _sectioningOptions);

        // Act
        var documents = await scanner.ScanAsync(tempDir.Path, CancellationToken.None);

        // Assert
        documents.Should().NotBeNull();
        documents.Should().BeEmpty();
    }

    [Fact]
    public async Task MarkdownScanner_ShouldHandleNonExistentDirectory()
    {
        // Arrange
        var nonExistentDir = Path.Combine(Path.GetTempPath(), "non-existent-dir-", Path.GetRandomFileName());
        var scanner = new MarkdownScanner(_loggerMock.Object, _sectioningOptions);

        // Act
        var documents = await scanner.ScanAsync(nonExistentDir, CancellationToken.None);

        // Assert
        documents.Should().NotBeNull();
        documents.Should().BeEmpty();
    }

    #region Helper Class

    private class TempDirectory : IDisposable
    {
        public string Path { get; }

        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "markdown-scanner-test-", System.IO.Path.GetRandomFileName());
            Directory.CreateDirectory(Path);
        }

        public void CreateFile(string fileName, string content)
        {
            var filePath = System.IO.Path.Combine(Path, fileName);
            File.WriteAllText(filePath, content);
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
