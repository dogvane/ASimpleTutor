using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ASimpleTutor.Core.Models;

/// <summary>
/// 知识点
/// </summary>
public class KnowledgePoint
{
    /// <summary>
    /// 知识点唯一标识符
    /// </summary>
    public string KpId { get; set; } = string.Empty;

    /// <summary>
    /// 书籍中心 ID
    /// </summary>
    public string BookHubId { get; set; } = string.Empty;

    /// <summary>
    /// 知识点标题（通常是概念名称或术语）
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 知识点类型
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public KpType Type { get; set; } = KpType.Concept;

    /// <summary>
    /// 知识点别名列表（用于搜索匹配）
    /// </summary>
    public List<string> Aliases { get; set; } = new();

    /// <summary>
    /// 所属章节路径（从根章节到当前章节的层级路径）
    /// </summary>
    public List<string> ChapterPath { get; set; } = new();

    /// <summary>
    /// 重要性评分（0.0~1.0，值越大越重要）
    /// </summary>
    public float Importance { get; set; }

    /// <summary>
    /// 预生成的学习内容（定义、要点、误区等）
    /// </summary>
    public Summary? Summary { get; set; }

    /// <summary>
    /// 预生成的层次化内容（L1/L2/L3）
    /// </summary>
    public List<ContentLevel> Levels { get; set; } = new();

    /// <summary>
    /// 预生成的幻灯片卡片列表
    /// </summary>
    public List<SlideCard> SlideCards { get; set; } = new();

    

    /// <summary>
    /// 来源文档 ID
    /// </summary>
    public string? DocId { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string SectionId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 知识点类型
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum KpType
{
    /// <summary>
    /// 概念（定义、术语、理论）
    /// </summary>
    Concept,

    /// <summary>
    /// 章节（目录节点）
    /// </summary>
    Chapter,

    /// <summary>
    /// 流程（步骤、操作流程）
    /// </summary>
    Process,

    /// <summary>
    /// API/接口
    /// </summary>
    Api,

    /// <summary>
    /// 最佳实践
    /// </summary>
    BestPractice
}



/// <summary>
/// 幻灯片卡片（用于幻灯片教学模式）
/// </summary>
public class SlideCard
{
    /// <summary>
    /// 卡片唯一标识符
    /// </summary>
    public string SlideId { get; set; } = string.Empty;

    /// <summary>
    /// 所属知识点 ID
    /// </summary>
    public string KpId { get; set; } = string.Empty;

    /// <summary>
    /// 卡片类型
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public SlideType Type { get; set; }

    /// <summary>
    /// 卡片序号（用于排序）
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 如果设置过音频，则是音频的地址文件
    /// </summary>
    public string AudioUrl { get; set; }

    /// <summary>
    /// 卡片标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// HTML 内容（可包含原文悬停标记）
    /// </summary>
    public string HtmlContent { get; set; } = string.Empty;

    /// <summary>
    /// 口语化讲解脚本（用于 TTS）
    /// </summary>
    public string? SpeechScript { get; set; }
}

/// <summary>
/// 幻灯片卡片类型
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum SlideType
{
    /// <summary>
    /// 封面/导言（Glance 概览）
    /// </summary>
    Cover,

    /// <summary>
    /// 概念解释（Detail 详细）
    /// </summary>
    Explanation,

    /// <summary>
    /// 示例/案例分析
    /// </summary>
    Example,

    /// <summary>
    /// 深入探讨（Deep 深入，可选）
    /// </summary>
    DeepDive,

    /// <summary>
    /// 随堂测验（Quiz）
    /// </summary>
    Quiz,

    /// <summary>
    /// 原文对照
    /// </summary>
    Source,

    /// <summary>
    /// 知识关联
    /// </summary>
    Relations,

    /// <summary>
    /// 总结回顾
    /// </summary>
    Summary
}

/// <summary>
/// 用户学习进度
/// </summary>
public class UserProgress
{
    /// <summary>
    /// 用户标识（当前为单用户，固定为 "default"）
    /// </summary>
    public string UserId { get; set; } = "default";

    /// <summary>
    /// 知识点 ID
    /// </summary>
    public string KpId { get; set; } = string.Empty;

    /// <summary>
    /// 学习状态
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public LearningStatus Status { get; set; } = LearningStatus.Todo;

    /// <summary>
    /// 掌握度（0.0 ~ 1.0）
    /// </summary>
    public float MasteryLevel { get; set; }

    /// <summary>
    /// 已完成的幻灯片卡片 ID 列表
    /// </summary>
    public List<string> CompletedSlideIds { get; set; } = new();

    /// <summary>
    /// 最后学习时间
    /// </summary>
    public DateTime? LastReviewTime { get; set; }

    /// <summary>
    /// 学习次数
    /// </summary>
    public int ReviewCount { get; set; }

    /// <summary>
    /// 总学习时长（秒）
    /// </summary>
    public int TotalDurationSeconds { get; set; }
}

/// <summary>
/// 学习状态
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum LearningStatus
{
    /// <summary>
    /// 未开始
    /// </summary>
    Todo,

    /// <summary>
    /// 学习中
    /// </summary>
    Learning,

    /// <summary>
    /// 已掌握
    /// </summary>
    Mastered
}

/// <summary>
/// 错题记录
/// </summary>
public class MistakeRecord
{
    /// <summary>
    /// 记录唯一标识符
    /// </summary>
    public string RecordId { get; set; } = string.Empty;

    /// <summary>
    /// 用户标识
    /// </summary>
    public string UserId { get; set; } = "default";

    /// <summary>
    /// 关联的习题 ID
    /// </summary>
    public string ExerciseId { get; set; } = string.Empty;

    /// <summary>
    /// 关联的知识点 ID
    /// </summary>
    public string KpId { get; set; } = string.Empty;

    /// <summary>
    /// 用户答案
    /// </summary>
    public string UserAnswer { get; set; } = string.Empty;

    /// <summary>
    /// 正确答案
    /// </summary>
    public string CorrectAnswer { get; set; } = string.Empty;

    /// <summary>
    /// 错误分析
    /// </summary>
    public string? ErrorAnalysis { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 是否已解决
    /// </summary>
    public bool IsResolved { get; set; }

    /// <summary>
    /// 解决时间
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// 错误次数
    /// </summary>
    public int ErrorCount { get; set; }
}

/// <summary>
/// 学习内容缓存
/// </summary>
public class LearningContentCache
{
    /// <summary>
    /// 知识点 ID
    /// </summary>
    public string KpId { get; set; } = string.Empty;

    /// <summary>
    /// 内容层级（1, 2, 3）
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// 内容类型（summary, levels, slides）
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// 内容 JSON
    /// </summary>
    public string ContentJson { get; set; } = string.Empty;

    /// <summary>
    /// 源文件 Hash 组合（用于失效检测）
    /// </summary>
    public string VersionHash { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 学习进度
/// </summary>
public class LearningProgress
{
    /// <summary>
    /// 用户标识
    /// </summary>
    public string UserId { get; set; } = "default";

    /// <summary>
    /// 知识点 ID
    /// </summary>
    public string KpId { get; set; } = string.Empty;

    /// <summary>
    /// 学习状态
    /// </summary>
    public LearningStatus Status { get; set; } = LearningStatus.Todo;

    /// <summary>
    /// 掌握度（0.0 ~ 1.0）
    /// </summary>
    public float MasteryLevel { get; set; }

    /// <summary>
    /// 复习次数
    /// </summary>
    public int ReviewCount { get; set; }

    /// <summary>
    /// 最后复习时间
    /// </summary>
    public DateTime? LastReviewTime { get; set; }

    /// <summary>
    /// 已完成的幻灯片 ID 列表
    /// </summary>
    public List<string>? CompletedSlideIds { get; set; }
}

/// <summary>
/// 章节学习包（包含PPT内容和口播说明）
/// </summary>
public class ChapterLearningPack
{
    /// <summary>
    /// 章节 ID
    /// </summary>
    public string ChapterId { get; set; } = string.Empty;

    /// <summary>
    /// 章节标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// PPT页面列表
    /// </summary>
    public List<PPTPage> PptPages { get; set; } = new();

    /// <summary>
    /// 章节习题
    /// </summary>
    public Exercise Exercise { get; set; } = new Exercise();
}

/// <summary>
/// PPT页面
/// </summary>
public class PPTPage
{
    /// <summary>
    /// 页面唯一标识符
    /// </summary>
    public string PageId { get; set; } = string.Empty;

    /// <summary>
    /// 所属章节 ID
    /// </summary>
    public string ChapterId { get; set; } = string.Empty;

    /// <summary>
    /// 页面类型
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public PPTPageType Type { get; set; } = PPTPageType.Cover;

    /// <summary>
    /// 页面顺序（用于排序）
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 页面标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// HTML 内容
    /// </summary>
    public string HtmlContent { get; set; } = string.Empty;

    /// <summary>
    /// 口语化讲解脚本（用于 TTS）
    /// </summary>
    public string? SpeechScript { get; set; }

    /// <summary>
    /// 知识图谱数据
    /// </summary>
    public KnowledgeGraphData KnowledgeGraphData { get; set; } = new KnowledgeGraphData();
}

/// <summary>
/// PPT页面类型
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum PPTPageType
{
    /// <summary>
    /// 封面/介绍
    /// </summary>
    Cover,

    /// <summary>
    /// 内容讲解
    /// </summary>
    Content,

    /// <summary>
    /// 深入分析
    /// </summary>
    Deep,

    /// <summary>
    /// 知识关联
    /// </summary>
    Relations,

    /// <summary>
    /// 总结回顾
    /// </summary>
    Summary
}

/// <summary>
/// 知识图谱数据
/// </summary>
public class KnowledgeGraphData
{
    /// <summary>
    /// 相关知识点
    /// </summary>
    public List<RelatedKnowledgePoint> RelatedPoints { get; set; } = new();
}

/// <summary>
/// 相关知识点
/// </summary>
public class RelatedKnowledgePoint
{
    /// <summary>
    /// 知识点 ID
    /// </summary>
    public string PointId { get; set; } = string.Empty;

    /// <summary>
    /// 知识点标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 关系类型
    /// </summary>
    public string Relationship { get; set; } = string.Empty;

    /// <summary>
    /// 关系权重（0.0~1.0）
    /// </summary>
    public float Weight { get; set; }
}

/// <summary>
/// TTS口播说明
/// </summary>
public class TTSScript
{
    /// <summary>
    /// 章节 ID
    /// </summary>
    public string ChapterId { get; set; } = string.Empty;

    /// <summary>
    /// 书籍中心 ID
    /// </summary>
    public string BookHubId { get; set; } = string.Empty;

    /// <summary>
    /// 页面脚本列表
    /// </summary>
    public List<PageScript> Scripts { get; set; } = new();
}

/// <summary>
/// 页面脚本
/// </summary>
public class PageScript
{
    /// <summary>
    /// 页面 ID
    /// </summary>
    public string PageId { get; set; } = string.Empty;

    /// <summary>
    /// 口播脚本文本
    /// </summary>
    public string SpeechScript { get; set; } = string.Empty;

    /// <summary>
    /// 预计播放时长（秒）
    /// </summary>
    public float Duration { get; set; }
}


