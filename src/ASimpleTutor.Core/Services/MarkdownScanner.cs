using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using Microsoft.Extensions.Logging;

namespace ASimpleTutor.Core.Services;

/// <summary>
/// Markdown 文档扫描服务
/// </summary>
public class MarkdownScanner : IScannerService
{
    private readonly ILogger<MarkdownScanner> _logger;

    public MarkdownScanner(ILogger<MarkdownScanner> logger)
    {
        _logger = logger;
    }

    public async Task<List<Document>> ScanAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        return await ScanAsync(rootPath, new List<string> { "references", "参考书目" }, cancellationToken);
    }

    public async Task<List<Document>> ScanAsync(string rootPath, List<string> excludeDirNames, CancellationToken cancellationToken)
    {
        var documents = new List<Document>();

        // 规范化路径，防止路径遍历攻击
        try
        {
            rootPath = Path.GetFullPath(rootPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "路径规范化失败: {RootPath}", rootPath);
            return documents;
        }

        if (!Directory.Exists(rootPath))
        {
            _logger.LogWarning("目录不存在: {RootPath}", rootPath);
            return documents;
        }

        // 限制搜索深度，避免性能问题
        var markdownFiles = GetMarkdownFiles(rootPath, excludeDirNames, maxDepth: 10);

        foreach (var filePath in markdownFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 检查是否在排除目录中
            if (IsInExcludedDirectory(filePath, rootPath, excludeDirNames))
            {
                _logger.LogDebug("跳过排除目录中的文件: {FilePath}", filePath);
                continue;
            }

            // 检查是否是隐藏文件或临时文件
            var fileName = Path.GetFileName(filePath);
            if (IsHiddenOrTempFile(fileName))
            {
                _logger.LogDebug("跳过隐藏或临时文件: {FilePath}", filePath);
                continue;
            }

            try
            {
                var doc = await ParseDocumentAsync(filePath, cancellationToken);
                if (doc != null)
                {
                    documents.Add(doc);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "解析文档失败: {FilePath}", filePath);
            }
        }

        _logger.LogInformation("扫描完成，共发现 {Count} 个文档", documents.Count);
        return documents;
    }

    private static bool IsInExcludedDirectory(string filePath, string rootPath, List<string> excludeDirNames)
    {
        var relativePath = Path.GetRelativePath(rootPath, filePath);
        var parts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return parts.Any(part => excludeDirNames.Contains(part, StringComparer.OrdinalIgnoreCase));
    }

    private static bool IsHiddenOrTempFile(string fileName)
    {
        // 隐藏文件（以 . 开头）
        if (fileName.StartsWith('.'))
        {
            return true;
        }

        // 临时文件（以 ~ 结尾）
        if (fileName.EndsWith('~'))
        {
            return true;
        }

        return false;
    }

    private static List<string> GetMarkdownFiles(string rootPath, List<string> excludeDirNames, int maxDepth)
    {
        var files = new List<string>();
        
        // 使用队列实现广度优先搜索，避免递归深度过大
        var queue = new Queue<(string path, int depth)>();
        queue.Enqueue((rootPath, 0));
        
        while (queue.Count > 0)
        {
            var (currentPath, depth) = queue.Dequeue();
            
            // 检查深度限制
            if (depth >= maxDepth)
            {
                continue;
            }
            
            try
            {
                // 获取当前目录中的文件
                var currentFiles = Directory.GetFiles(currentPath, "*.md");
                foreach (var file in currentFiles)
                {
                    // 规范化文件路径
                    var normalizedPath = Path.GetFullPath(file);
                    
                    // 检查是否在排除目录中
                    if (!IsInExcludedDirectory(normalizedPath, rootPath, excludeDirNames))
                    {
                        // 检查是否是隐藏或临时文件
                        var fileName = Path.GetFileName(normalizedPath);
                        if (!IsHiddenOrTempFile(fileName))
                        {
                            files.Add(normalizedPath);
                        }
                    }
                }
                
                // 获取当前目录中的子目录
                var subdirectories = Directory.GetDirectories(currentPath);
                foreach (var subdir in subdirectories)
                {
                    // 规范化目录路径
                    var normalizedPath = Path.GetFullPath(subdir);
                    
                    // 检查是否在排除目录中
                    if (!IsInExcludedDirectory(normalizedPath, rootPath, excludeDirNames))
                    {
                        // 检查是否是隐藏目录
                        var dirName = Path.GetFileName(normalizedPath);
                        if (!IsHiddenOrTempFile(dirName))
                        {
                            queue.Enqueue((normalizedPath, depth + 1));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 忽略访问权限错误
                System.Diagnostics.Debug.WriteLine($"访问目录失败: {currentPath}, 错误: {ex.Message}");
            }
        }
        
        return files;
    }

    private async Task<Document?> ParseDocumentAsync(string filePath, CancellationToken cancellationToken)
    {
        // 再次规范化文件路径，确保安全性
        try
        {
            filePath = Path.GetFullPath(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件路径规范化失败: {FilePath}", filePath);
            return null;
        }

        // 验证文件是否存在
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("文件不存在: {FilePath}", filePath);
            return null;
        }

        // 验证文件扩展名
        if (Path.GetExtension(filePath)?.ToLower() != ".md")
        {
            _logger.LogWarning("文件不是 Markdown 文件: {FilePath}", filePath);
            return null;
        }

        string content;
        try
        {
            content = await File.ReadAllTextAsync(filePath, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "读取文件失败: {FilePath}", filePath);
            return null;
        }

        var lines = content.Split('\n');
        var docId = GenerateId(filePath);

        var doc = new Document
        {
            DocId = docId,
            BookRootId = string.Empty, // 稍后设置
            Path = filePath,
            Title = ExtractTitle(content, lines),
            ContentHash = ComputeHash(content)
        };

        // 解析章节和段落
        var sections = ParseSections(lines);
        doc.Sections = sections;

        return doc;
    }

    private static string ExtractTitle(string content, string[] lines)
    {
        // 尝试从第一个 H1 标题提取
        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("# "))
            {
                return trimmed.TrimStart('#').Trim();
            }
        }

        // 如果没有 H1，使用文件名
        return Path.GetFileNameWithoutExtension(content);
    }

    private static List<Section> ParseSections(string[] lines)
    {
        var sections = new List<Section>();
        var currentSection = new Section
        {
            SectionId = "root",
            HeadingPath = new List<string>(),
            Paragraphs = new List<Paragraph>()
        };

        var paragraphId = 0;
        var currentHeadingLevel = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.TrimStart();

            // 检测标题
            var headingMatch = System.Text.RegularExpressions.Regex.Match(trimmed, @"^(#{1,6})\s+(.+)");
            if (headingMatch.Success)
            {
                // 保存之前的段落
                if (currentSection.Paragraphs.Count > 0)
                {
                    sections.Add(currentSection);
                }

                var headingLevel = headingMatch.Groups[1].Length;
                var headingText = headingMatch.Groups[2].Value.Trim();

                // 更新当前标题路径
                if (headingLevel <= currentHeadingLevel)
                {
                    currentSection.HeadingPath = currentSection.HeadingPath.Take(headingLevel - 1).ToList();
                }
                currentSection.HeadingPath.Add(headingText);
                currentHeadingLevel = headingLevel;

                currentSection = new Section
                {
                    SectionId = $"section_{sections.Count}",
                    HeadingPath = currentSection.HeadingPath.ToList(),
                    Paragraphs = new List<Paragraph>()
                };
            }
            else if (!string.IsNullOrWhiteSpace(line) && !trimmed.StartsWith("```") && !trimmed.StartsWith(">"))
            {
                // 普通段落
                var paragraphText = line.Trim();

                // 跳过列表项符号
                if (paragraphText.StartsWith("- ") || paragraphText.StartsWith("* ") || paragraphText.StartsWith("1. "))
                {
                    currentSection.Paragraphs.Add(new Paragraph
                    {
                        ParagraphId = $"p_{paragraphId++}",
                        Content = paragraphText,
                        StartLine = i,
                        EndLine = i,
                        Type = ParagraphType.List
                    });
                }
                else if (!string.IsNullOrEmpty(paragraphText))
                {
                    currentSection.Paragraphs.Add(new Paragraph
                    {
                        ParagraphId = $"p_{paragraphId++}",
                        Content = paragraphText,
                        StartLine = i,
                        EndLine = i,
                        Type = ParagraphType.Text
                    });
                }
            }
            else if (trimmed.StartsWith("```"))
            {
                // 代码块
                var codeBuilder = new System.Text.StringBuilder();
                var j = i + 1;
                while (j < lines.Length && !lines[j].TrimStart().StartsWith("```"))
                {
                    codeBuilder.AppendLine(lines[j]);
                    j++;
                }

                currentSection.Paragraphs.Add(new Paragraph
                {
                    ParagraphId = $"p_{paragraphId++}",
                    Content = codeBuilder.ToString(),
                    StartLine = i,
                    EndLine = j,
                    Type = ParagraphType.Code
                });

                i = j;
            }
            else if (trimmed.StartsWith(">"))
            {
                // 引用块
                currentSection.Paragraphs.Add(new Paragraph
                {
                    ParagraphId = $"p_{paragraphId++}",
                    Content = trimmed.TrimStart('>').Trim(),
                    StartLine = i,
                    EndLine = i,
                    Type = ParagraphType.Quote
                });
            }
        }

        // 保存最后的段落
        if (currentSection.Paragraphs.Count > 0)
        {
            sections.Add(currentSection);
        }

        return sections;
    }

    private static string GenerateId(string filePath)
    {
        return Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(filePath)))
            .Replace("/", "_")
            .Replace("+", "-")
            .Substring(0, 16);
    }

    private static string ComputeHash(string content)
    {
        return Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(content)))
            .Replace("/", "_")
            .Replace("+", "-");
    }
}
