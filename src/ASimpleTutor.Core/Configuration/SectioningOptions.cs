namespace ASimpleTutor.Core.Configuration;

/// <summary>
/// 章节划分配置
/// </summary>
public class SectioningOptions
{
    public const string SectionName = "Sectioning";
    
    public int TargetLength { get; set; } = 3000;
    public int MinLength { get; set; } = 500;
    public int MaxLength { get; set; } = 10000;
    
    public SizeThresholdsOptions SizeThresholds { get; set; } = new();
    public StrategyWeightsOptions StrategyWeights { get; set; } = new();
}

/// <summary>
/// 章节规模分类阈值配置
/// </summary>
public class SizeThresholdsOptions
{
    public int Small { get; set; } = 5000;
    public int Medium { get; set; } = 20000;
}

/// <summary>
/// 层级选择策略权重配置
/// </summary>
public class StrategyWeightsOptions
{
    public double TargetLengthMatch { get; set; } = 1.0;
    public double AvoidTooFine { get; set; } = 0.8;
    public double AvoidTooCoarse { get; set; } = 0.8;
    public double LevelContinuity { get; set; } = 0.6;
    public double MinDepthFirst { get; set; } = 0.5;
}