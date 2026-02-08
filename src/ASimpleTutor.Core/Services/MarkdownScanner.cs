using ASimpleTutor.Core.Configuration;
using ASimpleTutor.Core.Interfaces;
using ASimpleTutor.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ASimpleTutor.Core.Services;

/// <summary>
/// Markdown 文档扫描服务
/// </summary>
public class MarkdownScanner : IScannerService
{
    private readonly ILogger<MarkdownScanner> _logger;
    private readonly SectioningOptions _sectioningOptions;

    public MarkdownScanner(ILogger<MarkdownScanner> logger, IOptions<SectioningOptions> sectioningOptions)
    {
        _logger = logger;
        _sectioningOptions = sectioningOptions.Value;
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

            var doc = await ParseDocumentAsync(filePath, cancellationToken);
            if (doc != null)
            {
                documents.Add(doc);
            }
        }

        return documents;
    }

    private List<string> GetMarkdownFiles(string rootPath, List<string> excludeDirNames, int maxDepth)
    {
        var files = new List<string>();
        var queue = new Queue<(string Path, int Depth)>();
        queue.Enqueue((rootPath, 0));

        while (queue.Count > 0)
        {
            var (currentPath, depth) = queue.Dequeue();

            if (depth > maxDepth)
            {
                continue;
            }

            try
            {
                // 获取当前目录中的 Markdown 文件
                var markdownFiles = Directory.GetFiles(currentPath, "*.md", SearchOption.TopDirectoryOnly);
                files.AddRange(markdownFiles);

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
        // 尝试从 H1 标题提取
        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("# "))
            {
                return trimmed.Substring(2).Trim();
            }
        }

        // 如果没有 H1 标题，尝试从文件名提取
        return "无标题文档";
    }

    private List<Section> ParseSections(string[] lines)
    {
        // 阶段一：全局预扫描
        var headingTree = BuildHeadingTree(lines);
        var levelStatistics = CalculateLevelStatistics(headingTree);
        
        // 阶段二：确定最优层级（局部动态分层）
        var optimalLevels = DetermineOptimalLevels(headingTree, levelStatistics);
        
        // 阶段三：正式划分
        return BuildSections(lines, headingTree, optimalLevels);
    }

    /// <summary>
    /// 阶段一：构建标题树
    /// </summary>
    private List<HeadingNode> BuildHeadingTree(string[] lines)
    {
        var root = new HeadingNode { Level = 0, Text = "root", LineNumber = 0 };
        var stack = new Stack<HeadingNode>();
        stack.Push(root);
        var inCodeBlock = false;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.TrimStart();
            
            // 检测代码块开始/结束
            if (trimmed.StartsWith("```"))
            {
                inCodeBlock = !inCodeBlock;
                continue;
            }
            
            // 如果在代码块内，跳过标题识别
            if (inCodeBlock)
            {
                continue;
            }
            
            var headingMatch = System.Text.RegularExpressions.Regex.Match(trimmed, @"^(#{1,6})\s+(.+)");
            
            if (headingMatch.Success)
            {
                var level = headingMatch.Groups[1].Length;
                var text = headingMatch.Groups[2].Value.Trim();
                
                var node = new HeadingNode
                {
                    Level = level,
                    Text = text,
                    LineNumber = i
                };
                
                // 找到合适的父节点
                while (stack.Count > 1 && stack.Peek().Level >= level)
                {
                    stack.Pop();
                }
                
                var parent = stack.Peek();
                parent.Children.Add(node);
                node.Parent = parent;
                stack.Push(node);
            }
        }
        
        // 计算每个节点的内容长度
        CalculateContentLengths(root, lines);
        
        return root.Children;
    }

    /// <summary>
    /// 计算每个节点的内容长度
    /// </summary>
    private void CalculateContentLengths(HeadingNode root, string[] lines)
    {
        // 收集所有标题节点
        var allNodes = new List<HeadingNode>();
        CollectNodes(root, allNodes);
        
        // 按行号排序
        allNodes.Sort((a, b) => a.LineNumber.CompareTo(b.LineNumber));
        
        // 计算每个节点的内容长度（包括子标题的内容）
        for (int i = 0; i < allNodes.Count; i++)
        {
            var node = allNodes[i];
            var startLine = node.LineNumber + 1;
            
            // 找到当前节点的下一个同级或更高级标题
            int endLine = lines.Length;
            for (int j = i + 1; j < allNodes.Count; j++)
            {
                var nextNode = allNodes[j];
                if (nextNode.Level <= node.Level)
                {
                    endLine = nextNode.LineNumber;
                    break;
                }
            }
            
            // 计算原始字符数和过滤后的内容长度
            var (originalLength, effectiveLength) = CalculateContentLengths(lines, startLine, endLine);
            node.ContentLength = effectiveLength;
            node.OriginalLength = originalLength;
            node.EffectiveLength = effectiveLength;
        }
    }

    /// <summary>
    /// 计算原始字符数和过滤后的内容长度
    /// </summary>
    private (int originalLength, int effectiveLength) CalculateContentLengths(string[] lines, int startLine, int endLine)
    {
        var originalLength = 0;
        var effectiveLength = 0;
        var inCodeBlock = false;
        
        for (int j = startLine; j < endLine; j++)
        {
            var line = lines[j];
            var trimmed = line.TrimStart();
            
            // 原始字符数统计（包含所有内容）
            originalLength += line.Length;
            
            // 检测代码块开始
            if (trimmed.StartsWith("```"))
            {
                inCodeBlock = !inCodeBlock;
                continue;
            }
            
            // 如果在代码块内，跳过该行
            if (inCodeBlock)
            {
                continue;
            }
            
            // 过滤HTML标签和格式信息
            var filteredLine = FilterHtmlTags(trimmed);
            
            // 只统计非空行的长度
            if (!string.IsNullOrWhiteSpace(filteredLine))
            {
                effectiveLength += filteredLine.Length;
            }
        }
        
        return (originalLength, effectiveLength);
    }

    /// <summary>
    /// 过滤HTML标签和格式信息
    /// </summary>
    private string FilterHtmlTags(string line)
    {
        // 过滤HTML标签（如 <img>, <a>, <div> 等）
        var result = System.Text.RegularExpressions.Regex.Replace(line, @"<[^>]+>", "");
        
        // 过滤Markdown图片链接格式 ![alt](url)
        result = System.Text.RegularExpressions.Regex.Replace(result, @"!\[([^\]]*)\]\([^)]+\)", "");
        
        // 过滤Markdown链接格式 [text](url)
        result = System.Text.RegularExpressions.Regex.Replace(result, @"\[([^\]]+)\]\([^)]+\)", "$1");
        
        return result;
    }

    /// <summary>
    /// 收集所有节点
    /// </summary>
    private void CollectNodes(HeadingNode node, List<HeadingNode> nodes)
    {
        if (node.Level > 0)
        {
            nodes.Add(node);
        }
        
        foreach (var child in node.Children)
        {
            CollectNodes(child, nodes);
        }
    }

    /// <summary>
    /// 阶段一：计算各层级统计信息
    /// </summary>
    private Dictionary<HeadingNode, List<LevelStatistics>> CalculateLevelStatistics(List<HeadingNode> headingTree)
    {
        var statistics = new Dictionary<HeadingNode, List<LevelStatistics>>();
        
        // 递归处理所有节点
        foreach (var headingNode in headingTree)
        {
            CalculateNodeStatistics(headingNode, statistics);
        }
        
        return statistics;
    }

    /// <summary>
    /// 递归计算节点及其子节点的统计信息
    /// </summary>
    private void CalculateNodeStatistics(HeadingNode node, Dictionary<HeadingNode, List<LevelStatistics>> statistics)
    {
        var nodeStats = new List<LevelStatistics>();
        
        // 计算该节点下不同层级的统计信息（包括当前层级）
        for (int level = node.Level; level <= 6; level++)
        {
            var sectionsAtLevel = GetSectionsAtLevel(node, level);
            
            if (sectionsAtLevel.Count > 0)
            {
                // 使用每个章节的 ContentLength 属性
                var lengths = sectionsAtLevel.Select(s => s.ContentLength).ToList();
                nodeStats.Add(new LevelStatistics
                {
                    Level = level,
                    SectionCount = sectionsAtLevel.Count,
                    AverageLength = (int)lengths.Average(),
                    MinLength = lengths.Min(),
                    MaxLength = lengths.Max()
                });
            }
        }
        
        statistics[node] = nodeStats;
        
        // 递归处理子节点
        foreach (var child in node.Children)
        {
            CalculateNodeStatistics(child, statistics);
        }
    }

    /// <summary>
    /// 获取指定层级的所有章节
    /// </summary>
    private List<HeadingNode> GetSectionsAtLevel(HeadingNode parentNode, int level)
    {
        var result = new List<HeadingNode>();
        
        // 如果当前节点的层级等于指定层级，将其添加到结果中
        if (parentNode.Level == level && parentNode.Level > 0)
        {
            result.Add(parentNode);
        }
        
        // 递归获取所有子节点中指定层级的章节
        foreach (var child in parentNode.Children)
        {
            if (child.Level == level)
            {
                result.Add(child);
            }
            else if (child.Level < level)
            {
                result.AddRange(GetSectionsAtLevel(child, level));
            }
        }
        
        return result;
    }

    /// <summary>
    /// 阶段二：确定最优层级（局部动态分层）
    /// </summary>
    private Dictionary<HeadingNode, int> DetermineOptimalLevels(List<HeadingNode> headingTree, Dictionary<HeadingNode, List<LevelStatistics>> levelStatistics)
    {
        var optimalLevels = new Dictionary<HeadingNode, int>();
        
        // 递归处理所有节点
        foreach (var headingNode in headingTree)
        {
            DetermineNodeOptimalLevelRecursive(headingNode, levelStatistics, optimalLevels);
        }
        
        return optimalLevels;
    }
    
    /// <summary>
    /// 递归确定节点及其子节点的最优层级
    /// </summary>
    private void DetermineNodeOptimalLevelRecursive(HeadingNode node, Dictionary<HeadingNode, List<LevelStatistics>> levelStatistics, Dictionary<HeadingNode, int> optimalLevels)
    {
        DetermineNodeOptimalLevel(node, levelStatistics, optimalLevels);
        
        // 递归处理所有子节点
        foreach (var child in node.Children)
        {
            DetermineNodeOptimalLevelRecursive(child, levelStatistics, optimalLevels);
        }
    }

    /// <summary>
    /// 确定单个节点的最优层级
    /// </summary>
    private void DetermineNodeOptimalLevel(HeadingNode node, Dictionary<HeadingNode, List<LevelStatistics>> levelStatistics, Dictionary<HeadingNode, int> optimalLevels)
    {
        if (levelStatistics.TryGetValue(node, out var nodeStats))
        {
            var optimalLevel = DetermineOptimalLevelForNode(node, nodeStats);
            optimalLevels[node] = optimalLevel;
        }
        else
        {
            // 如果没有统计信息，默认为当前节点的层级
            optimalLevels[node] = node.Level;
        }
    }

    /// <summary>
    /// 为单个节点确定最优层级
    /// </summary>
    private int DetermineOptimalLevelForNode(HeadingNode node, List<LevelStatistics> nodeStats)
    {
        if (nodeStats.Count == 0)
        {
            // 如果没有子节点，返回当前节点的层级作为最优层级
            _logger.LogDebug("节点 {NodeText} (Level {Level}) 没有可用的层级统计信息，使用当前层级作为最优层级", node.Text, node.Level);
            return node.Level;
        }
        
        // 动态调整目标值
        var targetLength = GetTargetLengthForNode(node);
        _logger.LogDebug("节点 {NodeText} (Level {Level}) 的目标长度: {TargetLength}", node.Text, node.Level, targetLength);
        
        var bestLevel = 1;
        var bestScore = double.MinValue;
        
        foreach (var stats in nodeStats)
        {
            var score = CalculateLevelScore(stats, targetLength);
            _logger.LogDebug("  层级 {Level}: 平均长度 {AvgLength}, 章节数 {Count}, 得分 {Score}", stats.Level, stats.AverageLength, stats.SectionCount, score);
            
            if (score > bestScore)
            {
                bestScore = score;
                bestLevel = stats.Level;
            }
        }
        
        _logger.LogDebug("节点 {NodeText} (Level {Level}) 选择的最优层级: {BestLevel}", node.Text, node.Level, bestLevel);
        
        return bestLevel;
    }

    /// <summary>
    /// 根据节点的内容规模动态调整目标值
    /// </summary>
    private int GetTargetLengthForNode(HeadingNode node)
    {
        // 对于所有节点都返回一个固定的目标长度，这样我们就可以更精细地划分章节
        return _sectioningOptions.TargetLength;
    }

    /// <summary>
    /// 计算节点及其所有子节点的总长度
    /// </summary>
    private int CalculateTotalLength(HeadingNode node)
    {
        var total = node.ContentLength;
        
        foreach (var child in node.Children)
        {
            total += CalculateTotalLength(child);
        }
        
        return total;
    }

    /// <summary>
    /// 计算层级的得分
    /// </summary>
    private double CalculateLevelScore(LevelStatistics stats, int targetLength)
    {
        var weights = _sectioningOptions.StrategyWeights;
        var score = 0.0;
        
        // 规则一：目标长度匹配
        var lengthDiff = Math.Abs(stats.AverageLength - targetLength);
        var lengthScore = Math.Max(0.0, 1.0 - (lengthDiff / targetLength));
        score += weights.TargetLengthMatch * lengthScore;
        
        // 规则二：避免过细划分
        if (stats.AverageLength < _sectioningOptions.MinLength)
        {
            score -= weights.AvoidTooFine;
        }
        
        // 规则三：避免过粗划分
        if (stats.AverageLength > _sectioningOptions.MaxLength)
        {
            // 增加对过粗划分的惩罚
            var overCoarseFactor = stats.AverageLength / _sectioningOptions.MaxLength;
            score -= weights.AvoidTooCoarse * overCoarseFactor * 10; // 大幅增加惩罚因子
        }
        // 规则三补充：即使没有超过最大长度，也要惩罚过粗的划分
        else if (stats.AverageLength > targetLength * 1.5)
        {
            var overFactor = stats.AverageLength / (targetLength * 1.5);
            score -= weights.AvoidTooCoarse * overFactor * 5; // 大幅增加惩罚因子
        }
        
        // 规则四：层级连续性（简化处理，倾向于选择较深的层级）
        score += weights.MinDepthFirst * stats.Level * 5; // 大幅增加对深层级的偏好
        
        return score;
    }

    /// <summary>
    /// 阶段三：构建章节结构
    /// </summary>
    private List<Section> BuildSections(string[] lines, List<HeadingNode> headingTree, Dictionary<HeadingNode, int> optimalLevels)
    {
        var sections = new List<Section>();
        var sectionCounter = 0;
        
        // 收集所有标题节点用于计算结束行号
        var allHeadingNodes = new List<HeadingNode>();
        CollectHeadingNodesFromTree(headingTree, allHeadingNodes);
        
        // 按行号排序
        allHeadingNodes.Sort((a, b) => a.LineNumber.CompareTo(b.LineNumber));
        
        // 递归构建所有章节
        foreach (var headingNode in headingTree)
        {
            var section = BuildSectionFromHeading(headingNode, optimalLevels, lines, allHeadingNodes, ref sectionCounter);
            
            // 只添加未被排除的章节
            if (!section.IsExcluded)
            {
                sections.Add(section);
            }
        }
        
        return sections;
    }

    /// <summary>
    /// 从标题节点构建章节
    /// </summary>
    private Section BuildSectionFromHeading(HeadingNode headingNode, Dictionary<HeadingNode, int> optimalLevels, string[] lines, List<HeadingNode> allHeadingNodes, ref int sectionCounter)
    {
        var section = new Section
        {
            SectionId = $"section_{sectionCounter++}",
            HeadingPath = BuildHeadingPath(headingNode),
            SubSections = new List<Section>(),
            OriginalLength = headingNode.OriginalLength,
            EffectiveLength = headingNode.EffectiveLength,
            FilteredLength = headingNode.OriginalLength - headingNode.EffectiveLength
        };
        
        // 计算章节的行号范围
        section.StartLine = headingNode.LineNumber;
        
        // 找到下一个同级或更高级的标题作为结束行号
        var nextHeadingIndex = allHeadingNodes.FindIndex(n => n.LineNumber > headingNode.LineNumber && n.Level <= headingNode.Level);
        if (nextHeadingIndex >= 0)
        {
            section.EndLine = allHeadingNodes[nextHeadingIndex].LineNumber;
        }
        else
        {
            // 没有找到下一个标题，使用文档的最后一行
            section.EndLine = lines.Length;
        }
        
        // 获取当前节点的最优层级
        optimalLevels.TryGetValue(headingNode, out var optimalLevel);
        optimalLevel = optimalLevel > 0 ? optimalLevel : headingNode.Level;
        
        // 检查标题是否在排除列表中
        if (ShouldExcludeSection(headingNode.Text))
        {
            _logger.LogInformation("跳过排除的章节: {Title}", headingNode.Text);
            section.IsExcluded = true;
        }
        
        // 构建子章节（如果当前节点的层级小于最优层级）
        if (headingNode.Level < optimalLevel)
        {
            foreach (var child in headingNode.Children)
            {
                var childSection = BuildSectionFromHeading(child, optimalLevels, lines, allHeadingNodes, ref sectionCounter);
                section.SubSections.Add(childSection);
            }
        }
        
        return section;
    }

    /// <summary>
    /// 从标题树中收集所有标题节点
    /// </summary>
    private void CollectHeadingNodesFromTree(List<HeadingNode> headingTree, List<HeadingNode> allNodes)
    {
        foreach (var node in headingTree)
        {
            allNodes.Add(node);
            CollectHeadingNodesFromTree(node.Children, allNodes);
        }
    }

    /// <summary>
    /// 构建标题路径
    /// </summary>
    private List<string> BuildHeadingPath(HeadingNode node)
    {
        var path = new List<string>();
        var current = node;
        
        while (current != null && current.Level > 0)
        {
            path.Insert(0, current.Text);
            current = current.Parent;
        }
        
        return path;
    }

    /// <summary>
    /// 收集标题节点及其所有子节点
    /// </summary>
    private void CollectHeadingNodes(HeadingNode node, List<HeadingNode> nodes)
    {
        nodes.Add(node);
        foreach (var child in node.Children)
        {
            CollectHeadingNodes(child, nodes);
        }
    }

    private static bool IsHiddenOrTempFile(string fileName)
    {
        return fileName.StartsWith(".") || fileName.StartsWith("~") || fileName.EndsWith(".tmp") || fileName.EndsWith(".temp");
    }

    /// <summary>
    /// 判断章节标题是否应该被排除
    /// </summary>
    private bool ShouldExcludeSection(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        var normalizedTitle = title.Trim().ToLowerInvariant();
        
        foreach (var excludedTitle in _sectioningOptions.ExcludedSectionTitles)
        {
            var normalizedExcluded = excludedTitle.Trim().ToLowerInvariant();
            
            // 精确匹配
            if (normalizedTitle == normalizedExcluded)
            {
                return true;
            }
            
            // 包含匹配（处理带编号的情况，如 "1.1 本章小结"）
            if (normalizedTitle.Contains(normalizedExcluded))
            {
                return true;
            }
        }
        
        return false;
    }

    private static bool IsInExcludedDirectory(string path, string rootPath, List<string> excludeDirNames)
    {
        // 获取相对于根目录的路径
        var relativePath = Path.GetRelativePath(rootPath, path);
        var pathParts = relativePath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

        // 检查路径中的任何部分是否在排除列表中
        return pathParts.Any(part => excludeDirNames.Contains(part, StringComparer.OrdinalIgnoreCase));
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