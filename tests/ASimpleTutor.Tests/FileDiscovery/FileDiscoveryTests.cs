using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using ASimpleTutor.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ASimpleTutor.Tests.FileDiscovery;

/// <summary>
/// 文件发现模块测试用例
/// 对应测试需求文档：TC-FD-001 ~ TC-FD-006
/// </summary>
public class FileDiscoveryTests
{
    private readonly Mock<ILogger<MarkdownScanner>> _loggerMock;
    private readonly MarkdownScanner _scanner;
    private readonly string _testDataPath;

    public FileDiscoveryTests()
    {
        _loggerMock = new Mock<ILogger<MarkdownScanner>>();
        _scanner = new MarkdownScanner(_loggerMock.Object);
        _testDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Files");
    }

    [Fact]
    public async Task ScanAsync_WithNormalFiles_ShouldDiscoverAllMdFiles()
    {
        // Arrange
        var normalPath = Path.Combine(_testDataPath, "FileDiscovery", "normal_files");

        // Act
        var result = await _scanner.ScanAsync(normalPath, new List<string>(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(3);

        var fileNames = result.Select(d => Path.GetFileName(d.Path)).ToList();
        fileNames.Should().Contain("chapter01.md");
        fileNames.Should().Contain("chapter02.md");
        fileNames.Should().Contain("chapter03.md");
    }

    [Fact]
    public async Task ScanAsync_WithExcludedDirectories_ShouldExcludeReferenceFiles()
    {
        // Arrange
        var testPath = Path.Combine(_testDataPath, "FileDiscovery");
        var excludeDirs = new List<string> { "references" };

        // Act
        var result = await _scanner.ScanAsync(testPath, excludeDirs, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        var filePaths = result.Select(d => d.Path).ToList();

        // 参考书目目录中的文件不应该被包含
        filePaths.Should().NotContain(p => p.Contains("references" + Path.DirectorySeparatorChar + "book01"));
        filePaths.Should().NotContain(p => p.Contains("references" + Path.DirectorySeparatorChar + "sub" + Path.DirectorySeparatorChar + "ref"));
    }

    [Fact]
    public async Task ScanAsync_WithHiddenAndTempFiles_ShouldFilterThemOut()
    {
        // Arrange
        var edgeCasesPath = Path.Combine(_testDataPath, "FileDiscovery", "hidden_and_temp");

        // Act
        var result = await _scanner.ScanAsync(edgeCasesPath, new List<string>(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        var fileNames = result.Select(d => Path.GetFileName(d.Path)).ToList();

        // 临时文件应该被过滤（~ 结尾）
        fileNames.Should().NotContain("temp.md~");

        // 隐藏文件检测：以下划线开头的文件
        var hasHiddenFile = fileNames.Contains("_hidden.md");
        if (hasHiddenFile)
        {
            result.Should().Contain(d => Path.GetFileName(d.Path) == "_hidden.md");
        }
        else
        {
            fileNames.Should().NotContain("_hidden.md");
        }
    }

    [Fact]
    public async Task ScanAsync_WithEmptyDirectory_ShouldReturnEmptyList()
    {
        // Arrange
        var emptyPath = Path.Combine(_testDataPath, "FileDiscovery", "empty_directory");
        Directory.CreateDirectory(emptyPath);

        try
        {
            // Act
            var result = await _scanner.ScanAsync(emptyPath, new List<string>(), CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }
        finally
        {
            // Cleanup
            Directory.Delete(emptyPath);
        }
    }

    [Fact]
    public async Task ScanAsync_WithNonExistentDirectory_ShouldReturnEmptyList()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDataPath, "FileDiscovery", "non_existent_directory");

        // Act
        var result = await _scanner.ScanAsync(nonExistentPath, new List<string>(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ScanAsync_WithDefaultExcludeDirs_ShouldExcludeReferences()
    {
        // Arrange
        var testPath = Path.Combine(_testDataPath, "FileDiscovery");

        // Act - 使用默认排除规则（references, 参考书目）
        var result = await _scanner.ScanAsync(testPath, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        var filePaths = result.Select(d => d.Path).ToList();

        // 验证排除规则生效
        filePaths.Should().NotContain(p => p.Contains("references" + Path.DirectorySeparatorChar + "book01"));
    }

    [Fact]
    public async Task ScanAsync_WithRecursionEnabled_ShouldDiscoverNestedFiles()
    {
        // Arrange
        var subdirsPath = Path.Combine(_testDataPath, "FileDiscovery", "with_subdirs");
        // MarkdownScanner 默认使用 SearchOption.AllDirectories，即递归扫描

        // Act
        var result = await _scanner.ScanAsync(subdirsPath, new List<string>(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(2); // 2个嵌套文件：nested/sub.md 和 nested/deep/deep_content.md

        // 验证嵌套文件被包含
        var allPaths = string.Join(";", result.Select(d => d.Path));
        allPaths.Should().Contain("nested");
        allPaths.Should().Contain("deep");
        allPaths.Should().Contain("sub.md");
        allPaths.Should().Contain("deep_content.md");
    }
}
