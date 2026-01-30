# 数据模型与存储策略（MVP）

## 1. 设计目标

- 支撑：知识体系、原文片段、学习内容、练习题
- MVP 优先：实现简单、可解释、可缓存

## 2. 核心数据模型

> **命名约定**：字段名使用 PascalCase（如 `BookRootId`），与 C# 代码实现保持一致。

### 2.0 BookRoot（书籍目录）

用于支持"配置多个书籍目录，但仅激活一个"的系统入口设计。

| 字段 | 类型 | 说明 |
|------|------|------|
| `Id` | string | 唯一标识 |
| `Name` | string | 展示名称，可选；默认可用目录名 |
| `Path` | string | 绝对路径或工作区相对路径 |
| `ReferenceDirNames` | List<string> | 可选；默认：`["references", "参考书目"]` |
| `ExcludeGlobs` | List<string> | 可选；用于扫描与主索引排除规则，如 `**/references/**` |
| `Enabled` | bool | 是否可用，可选 |
| `Order` | int | 展示排序，可选 |

### 2.0.1 ActiveBookRoot（当前激活）

| 字段 | 类型 | 说明 |
|------|------|------|
| `ActiveBookRootId` | string | 当前激活的 BookRoot ID |

> 约定：所有运行期的知识体系与内容生成均基于 `ActiveBookRootId` 对应的目录。

### 2.1 Document（文档）

| 字段 | 类型 | 说明 |
|------|------|------|
| `DocId` | string | 唯一标识 |
| `BookRootId` | string | 所属书籍目录 |
| `Path` | string | 文件路径 |
| `Title` | string | 标题（取第一个 H1 或文件名） |
| `Sections` | List<Section> | 章节列表 |
| `ContentHash` | string | 内容哈希，用于缓存（可选） |

> 约定：`Document` 默认指"主书内容文档"（参与知识体系构建与主学习检索）。

### 2.1.1 ReferenceDocument（参考资料文档，规划项）

用于表示 Book Root 内参考书目目录下的 `.md` 文件。

| 字段 | 类型 | 说明 |
|------|------|------|
| `RefDocId` | string | 唯一标识 |
| `BookRootId` | string | 所属书籍目录 |
| `Path` | string | 文件路径 |
| `Title` | string | 标题 |
| `ContentHash` | string | 内容哈希（可选） |

MVP 行为：
- `ReferenceDocument` 不参与知识体系构建
- `ReferenceDocument` 不进入主 RAG 索引

后续规划：
- 为参考资料单独建立 `ReferenceRagIndex`，用于"深度探索/扩展学习"检索

### 2.2 Section（章节）

| 字段 | 类型 | 说明 |
|------|------|------|
| `SectionId` | string | 唯一标识 |
| `HeadingPath` | List<string> | 标题层级路径，如 `["第一章", "1.1 基础概念"]` |
| `Paragraphs` | List<Paragraph> | 段落列表 |

### 2.2.1 Paragraph（段落）

| 字段 | 类型 | 说明 |
|------|------|------|
| `ParagraphId` | string | 唯一标识 |
| `Content` | string | 段落内容文本 |
| `Type` | ParagraphType | 段落类型枚举 |
| `StartLine` | int | 起始行号（用于原文追溯） |
| `EndLine` | int | 结束行号（用于原文追溯） |

#### ParagraphType 枚举

| 值 | 说明 |
|------|------|
| `Text` | 普通文本段落 |
| `Code` | 代码块 |
| `Quote` | 引用块 |
| `List` | 列表项 |

### 2.3 SourceSnippet（原文片段）

> 代码实现类名：`SourceSnippet`（与设计文档 `Snippet` 命名略有差异）

| 字段 | 类型 | 说明 |
|------|------|------|
| `SnippetId` | string | 唯一标识 |
| `BookRootId` | string | 所属书籍目录 |
| `DocId` | string | 所属文档 ID |
| `FilePath` | string | 文件路径 |
| `HeadingPath` | List<string> | 标题层级路径 |
| `Content` | string | 片段内容文本 |
| `StartLine` | int | 起始行号 |
| `EndLine` | int | 结束行号 |

> 说明：`SourceSnippet` 在 MVP 默认来自主书文档。若后续引入参考资料检索，可扩展支持 `DocId`（主书）与 `RefDocId`（参考）二选一的引用方式。

### 2.4 KnowledgePoint（知识点）

| 字段 | 类型 | 说明 |
|------|------|------|
| `KpId` | string | 唯一标识 |
| `BookRootId` | string | 所属书籍目录 |
| `Title` | string | 标题 |
| `Aliases` | List<string> | 别名/同义词（可选） |
| `ChapterPath` | List<string> | 章节路径 |
| `Importance` | float | 重要性评分（0.0 ~ 1.0） |
| `SnippetIds` | List<string> | 关联的原文片段 ID 列表（至少 1 个） |
| `Relations` | List<KnowledgeRelation> | 关联的其他知识点（可选） |

#### KnowledgeRelation（知识点关联）

| 字段 | 类型 | 说明 |
|------|------|------|
| `ToKpId` | string | 关联的知识点 ID |
| `Type` | RelationType | 关联类型 |

#### RelationType 枚举

| 值 | 说明 |
|------|------|
| `Prerequisite` | 前置知识 |
| `Comparison` | 对比/比较 |
| `Contains` | 包含关系 |
| `Related` | 一般关联 |

### 2.5 KnowledgeSystem（知识系统容器）

运行时使用的完整知识体系数据结构。

| 字段 | 类型 | 说明 |
|------|------|------|
| `BookRootId` | string | 所属书籍目录 |
| `KnowledgePoints` | List<KnowledgePoint> | 知识点列表 |
| `Snippets` | Dictionary<string, SourceSnippet> | 原文片段字典（ID -> 对象） |
| `Tree` | KnowledgeTreeNode | 知识树根节点 |

### 2.5.1 KnowledgeTreeNode（知识树节点）

按章节路径组织的知识点树结构。

| 字段 | 类型 | 说明 |
|------|------|------|
| `Id` | string | 节点 ID（通常为 `root` 或章节标题） |
| `Title` | string | 节点标题 |
| `Children` | List<KnowledgeTreeNode> | 子节点 |
| `KnowledgePointIds` | List<string> | 该节点下的知识点 ID 列表 |

### 2.6 LearningPack（学习内容包）

| 字段 | 类型 | 说明 |
|------|------|------|
| `KpId` | string | 关联的知识点 ID |
| `Summary` | Summary | 精要速览 |
| `Levels` | List<ContentLevel> | 层次化内容列表 |
| `SnippetIds` | List<string> | 原文片段 ID 列表 |
| `RelatedKpIds` | List<string> | 关联知识点 ID 列表（可选） |

#### Summary（精要速览）

| 字段 | 类型 | 说明 |
|------|------|------|
| `Definition` | string | 一句话定义 |
| `KeyPoints` | List<string> | 核心要点列表 |
| `Pitfalls` | List<string> | 常见误区/注意事项（可选） |

#### ContentLevel（层次化内容）

| 字段 | 类型 | 说明 |
|------|------|------|
| `Level` | int | 层级（1=概览，2=详细，3=深入） |
| `Title` | string | 层级标题（如"概览"、"详细"、"深入"） |
| `Content` | string | 详细内容 |

### 2.7 Exercise（练习题）

| 字段 | 类型 | 说明 |
|------|------|------|
| `ExerciseId` | string | 唯一标识 |
| `KpId` | string | 关联的知识点 ID |
| `Type` | ExerciseType | 习题类型 |
| `Question` | string | 问题内容 |
| `Options` | List<string> | 选项列表（选择题） |
| `CorrectAnswer` | string | 正确答案 |
| `EvidenceSnippetIds` | List<string> | 证据原文片段 ID 列表 |
| `KeyPoints` | List<string> | 考查要点列表 |

#### ExerciseType 枚举

| 值 | 说明 | 中文对照 |
|------|------|---------|
| `SingleChoice` | 单项选择题 | 选择题 |
| `FillBlank` | 填空题 | 填空题 |
| `ShortAnswer` | 简答题 | 简答题 |

### 2.8 ExerciseFeedback（练习反馈）

| 字段 | 类型 | 说明 |
|------|------|------|
| `IsCorrect` | bool? | 是否正确（填空/简答可能为 null 表示不确定） |
| `Explanation` | string | 解释说明 |
| `ReferenceAnswer` | string? | 参考答案（可选） |
| `CoveredPoints` | List<string> | 已覆盖的要点 |
| `MissingPoints` | List<string> | 遗漏的要点 |

## 3. 存储策略（MVP）

### 3.1 默认：内存存储

- 启动时基于 `ActiveBookRootId` 构建 `KnowledgeSystem` 全量放内存
- 适用于书籍规模中小（MVP 预期）

### 3.2 可选：本地缓存落盘

- 缓存内容（按 `BookRootId` 分目录或加前缀进行命名空间隔离）：
  - 文档解析结果（Sections/Snippets）
  - 知识体系（KnowledgePoints + Relations）
  - 学习内容（LearningPacks）
  - 习题（Exercises）
- 缓存失效：
  - `ContentHash` 或文件 `mtime` 变更
- 格式：JSON 文件或轻量 DB（如 SQLite）均可；MVP 推荐 JSON 简化。

### 3.3 缓存文件命名约定

```
<book_root_id>/
├── documents/              # 文档解析结果
│   └── <doc_id>.json
├── snippets/               # 原文片段
│   └── <snippet_id>.json
├── knowledge_system.json   # 知识体系完整结构
├── learning_packs/         # 学习内容
│   └── <kp_id>.json
└── exercises/              # 习题
    └── <kp_id>/
        └── <exercise_id>.json
```

### 3.4 不做

- 多用户隔离
- 云端同步
- 进度与历史记录（需求明确不做）

## 4. 数据流示意

```
Markdown 文件
    ↓
MarkdownScanner → Document[] + SourceSnippet[]
    ↓
KnowledgeBuilder → KnowledgeSystem
    ↓
┌──────────────────────────────────────┐
│  LearningGenerator → LearningPack    │
│  ExerciseService → Exercise[]        │
│         ↓                            │
│  用户选择知识点 → 生成学习内容/习题   │
└──────────────────────────────────────┘
```

## 5. 相关文档

- [00-总体设计.md](00-总体设计.md)
- [01-文档扫描与知识体系构建.md](01-文档扫描与知识体系构建.md)
- [03-学习内容生成（精要速览-原文对照-层次化展开）.md](03-学习内容生成（精要速览-原文对照-层次化展开）.md)
- [04-习题生成与练习反馈.md](04-习题生成与练习反馈.md)
