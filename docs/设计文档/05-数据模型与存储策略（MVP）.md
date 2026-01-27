# 数据模型与存储策略（MVP）

## 1. 设计目标

- 支撑：知识体系、原文片段、学习内容、练习题
- MVP 优先：实现简单、可解释、可缓存

## 2. 核心数据模型（概念级）

### 2.0 BookRoot（书籍目录）

用于支持“配置多个书籍目录，但仅激活一个”的系统入口设计。

- `book_root_id`
- `name`（展示名称，可选；默认可用目录名）
- `path`（绝对路径或工作区相对路径）
- `reference_dir_names[]`（可选；默认建议：`["references", "参考书目"]`）
- `exclude_globs[]`（可选；用于扫描与主索引排除规则，例如 `**/references/**`）
- `enabled`（是否可用，可选）
- `order`（展示排序，可选）

### 2.0.1 ActiveBookRoot（当前激活）

- `active_book_root_id`

> 约定：所有运行期的知识体系与内容生成均基于 `active_book_root_id` 对应的目录。

### 2.1 Document

- `doc_id`
- `book_root_id`
- `path`
- `title`（可取第一个 H1 或文件名）
- `sections[]`
- `content_hash`（可选，用于缓存）

> 约定：`Document` 默认指“主书内容文档”（参与知识体系构建与主学习检索）。

### 2.1.1 ReferenceDocument（参考资料文档，规划项）

用于表示 Book Root 内参考书目目录下的 `.md` 文件。

- `ref_doc_id`
- `book_root_id`
- `path`
- `title`
- `content_hash`（可选）

MVP 行为：

- `ReferenceDocument` 不参与知识体系构建
- `ReferenceDocument` 不进入主 RAG 索引

后续规划：

- 为参考资料单独建立 `ReferenceRagIndex`，用于“深度探索/扩展学习”检索

### 2.2 Section / Paragraph

- `section_id`
- `heading_path[]`（标题层级路径）
- `paragraphs[]`

### 2.3 Snippet（原文片段）

- `snippet_id`
- `book_root_id`
- `doc_id`
- `heading_path[]`
- `text`

> 说明：`Snippet` 在 MVP 默认来自主书文档。若后续引入参考资料检索，可扩展为同时支持 `doc_id`（主书）与 `ref_doc_id`（参考）二选一的引用方式。

### 2.4 KnowledgePoint（知识点）

- `kp_id`
- `book_root_id`
- `title`
- `aliases[]`（可选）
- `chapter_path[]`
- `importance`（0~1 或 1~5）
- `snippet_ids[]`（至少 1 个）
- `relations[]`（可选：to_kp_id + type）

### 2.5 LearningPack

- `kp_id`
- `summary`：{definition, bullets[], pitfalls[]}
- `levels[]`：[{level, title, content}]
- `snippet_ids[]`
- `related_kp_ids[]`（可选）

### 2.6 Exercise / Feedback

- `exercise_id`
- `kp_id`
- `type`（single_choice/fill_blank/short_answer）
- `question`
- `options[]`（选择题）
- `answer_key`（选择题/填空参考）
- `evidence_snippet_ids[]`

- `feedback`：{is_correct?, explanation, reference_answer?, covered_points?}

## 3. 存储策略（MVP）

### 3.1 默认：内存存储

- 启动时基于 `active_book_root_id` 构建 `KnowledgeSystem` 全量放内存
- 适用于书籍规模中小（MVP 预期）

### 3.2 可选：本地缓存落盘

- 缓存内容（按 `book_root_id` 分目录或加前缀进行命名空间隔离）：
  - 文档解析结果（sections/snippets）
  - 知识体系（knowledge points + relations）
- 缓存失效：
  - `content_hash` 或文件 `mtime` 变更
- 格式：JSON 文件或轻量 DB（如 SQLite）均可；MVP 推荐 JSON 简化。

### 3.3 不做

- 多用户隔离
- 云端同步
- 进度与历史记录（需求明确不做）

## 4. 相关文档

- [00-总体设计.md](00-总体设计.md)
- [01-文档扫描与知识体系构建.md](01-文档扫描与知识体系构建.md)
- [03-学习内容生成（精要速览-原文对照-层次化展开）.md](03-学习内容生成（精要速览-原文对照-层次化展开）.md)
- [04-习题生成与练习反馈.md](04-习题生成与练习反馈.md)

