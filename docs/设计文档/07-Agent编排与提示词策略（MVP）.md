# Agent 编排与提示词策略（MVP）

本设计文档聚焦：如何把“知识体系构建 → 学习内容生成 → 习题生成/反馈”串成可运行的 Agent 编排，并用稳定的 JSON 契约 + 原文证据溯源来降低幻觉、提升可调试性。

> 说明：当前代码实现以「多服务（非显式多 Agent）」方式落地：`KnowledgeBuilder`、`LearningGenerator`、`ExerciseService` 内部各自持有 prompt 字符串并调用 `ILLMService.ChatJsonAsync<T>()`。本文以此为 MVP 基线，给出一致的字段与策略。

## 1. 目标

- 产出可直接入库/展示的结构化结果（对应 Core Models）。
- 可追溯到书籍原文证据（snippet/chunk 级）。
- 降低幻觉：超出片段就明确“不确定/片段不足”。
- 可调试：失败可降级，输出可校验，日志可定位。

## 2. 现状对齐（代码中的“Agent”映射）

当前 MVP 的“Agent”建议按服务拆分，保持与接口一致：

- Knowledge Builder Agent → `IKnowledgeBuilder` / `KnowledgeBuilder.BuildAsync()`
  - 输入：`bookRootId`、`rootPath`
  - 输出：`KnowledgeSystem`（`KnowledgePoints` + `Snippets` + `Tree`）
- Content Agent → `ILearningGenerator` / `LearningGenerator.GenerateAsync()`
  - 输入：`KnowledgePoint`
  - 输出：`LearningPack`（`Summary` + `Levels` + `SnippetIds`）
- Exercise Agent → `IExerciseGenerator` / `ExerciseService.GenerateAsync()`
  - 输入：`KnowledgePoint` + `count`
  - 输出：`List<Exercise>`（每题带 `EvidenceSnippetIds`）
- Feedback/Judge Agent（内置在 Exercise Agent）→ `IExerciseFeedback` / `ExerciseService.JudgeAsync()`

MVP 阶段不强制引入“统一编排器”。如果后续要做显式编排，可在 API 层或 Core 新增一个 `Orchestrator`，但输出契约与术语应先稳定。

### 2.1 参考 DeepTutor agents.md：角色化分工与“循环”编排（仅设计）

参考文档 [docs/参考文档/DeepTutor/agents.md](../参考文档/DeepTutor/agents.md) 的核心启发是：

- 不把“一个大模型”当作单一 Agent，而是把一次任务拆成多个**角色明确**的子 Agent。
- 编排上倾向使用“Manager 决策 → Worker 执行 → Note/Response 总结”的循环，以便可控、可追踪。
- 提示词配置与代码解耦（例如按模块/语言/版本组织），便于迭代与回滚。

结合本项目（MVP）的落地建议：

- 仍保持服务级拆分（`KnowledgeBuilder` / `LearningGenerator` / `ExerciseService`），但在设计层面引入“角色”概念，作为后续演进的方向：
  - `KnowledgeBuilder` ≈ Extractor（抽取知识点）
  - `LearningGenerator` ≈ Content Composer（学习内容组织/改写）
  - `ExerciseService.Generate` ≈ Question Generator（出题）
  - `ExerciseService.Judge` ≈ Evaluator（判题/反馈）

后续如果引入显式编排器，可参考 DeepTutor 的做法抽象出以下通用角色（不要求一次性全实现）：

- **Manager/Router**：根据状态（是否有 kp、是否缺证据、是否生成失败）选择下一步 action。
- **Tool Strategist（可选）**：决定是否需要 RAG / Web / Code 等工具；本项目 MVP 主要是 RAG 与本地逻辑。
- **Note/Summarizer（可选）**：把工具/原文/模型输出整理成可复用的“中间结构”，减少上下文噪声。
- **Response/Formatter**：把结构化内容转成 API/UI 需要的最终格式。

> 注意：这里只借鉴“分工与编排模式”，不复制 agents.md 中的任何提示词正文。

## 3. 术语统一：Snippet / Chunk / Evidence

当前实现里：

- `snippet_id` / `SnippetId`：系统内部用于“原文证据”的唯一 ID。
- `chunk_id`：在 RAG/检索语境里常用的片段 ID。

代码中 `SourceTracker.TrackAsync()` 的实现是“整篇文档作为一个 chunk”，并生成：

- `chunkId = {docId}_chunk_0`
- 同时作为 `SourceSnippet.SnippetId` 与 `SourceSnippet.ChunkId`

因此 MVP 约定：

- `KnowledgePoint.SnippetIds` 存储的就是 `chunk_id`（格式为 `{docId}_chunk_0`）。
- `Exercise.EvidenceSnippetIds` 存储的也是同一套 ID。

注意：本文后续统一使用 **snippet_id（=chunk_id）** 表述，以减少歧义。

## 4. 编排流程（端到端）

### 4.1 KnowledgeBuilder：扫描 → 入库 → 追溯 → 提取

1) 扫描 Markdown 文档：`IScannerService.ScanAsync(rootPath)`
2) 写入 RAG：`ISimpleRagService.InsertAsync(docId, content, metadata)`
3) 写入追溯：`ISourceTracker.TrackAsync(docId, content, metadata)`
4) LLM 抽取知识点：`ILLMService.ChatJsonAsync<KnowledgePointsResponse>()`
5) 构建知识树：按 `KnowledgePoint.ChapterPath` 聚合
6) 汇总证据：根据 `kp.SnippetIds` 收集 `KnowledgeSystem.Snippets`

### 4.2 LearningGenerator：基于证据生成学习包

1) 从 `kp.SnippetIds` 拉取原文：`ISourceTracker.GetSources(kp.SnippetIds)`
2) LLM 生成学习内容：`ILLMService.ChatJsonAsync<LearningContentResponse>()`
3) 组装 `LearningPack`：写入 `SnippetIds` 与（可选）`RelatedKpIds`
4) 失败降级：根据原文片段做最小可用内容

### 4.3 ExerciseService：生成题目 → 判题反馈

- 生成：同样基于 `kp.SnippetIds` 的原文；生成后统一回填 `ExerciseId/KpId/EvidenceSnippetIds`
- 判题：
  - 选择题本地判
  - 填空/简答用 LLM 判（输出 JSON）

## 5. 输出契约（JSON）

### 5.1 知识点抽取（KnowledgePointsResponse）

与 `KnowledgeBuilder` 当前 prompt/DTO 对齐（注意 snake_case）：

```json
{
  "schema_version": "1.0",
  "knowledge_points": [
    {
      "kp_id": "string",
      "title": "string",
      "aliases": ["string"],
      "chapter_path": ["string"],
      "importance": 0.0,
      "snippet_ids": ["{docId}_chunk_0"],
      "summary": "string"
    }
  ]
}
```

落地规则（MVP 强约束）：

- `snippet_ids` 必填且至少 1 个；必须使用系统提供的可用 ID（见 prompt 中的列表）。
- `chapter_path` 必须是数组（用于 `KnowledgeTree` 构建）。

### 5.2 学习内容（LearningContentResponse）

与 `LearningGenerator` 当前 prompt/类对齐（注意：这里是驼峰 + 下划线混用风险点，建议后续统一）。当前实现接受：

```json
{
  "summary": {
    "definition": "string",
    "key_points": ["string"],
    "pitfalls": ["string"]
  },
  "levels": [
    { "level": 1, "title": "概览", "content": "string" }
  ]
}
```

### 5.3 习题生成（ExercisesResponse）

与 `ExerciseService.GenerateExercisesAsync()` prompt 对齐：

```json
{
  "exercises": [
    {
      "type": "SingleChoice|FillBlank|ShortAnswer",
      "question": "string",
      "options": ["string"],
      "correct_answer": "string",
      "key_points": ["string"],
      "explanation": "string"
    }
  ]
}
```

注意：`Exercise` 模型本身没有 `explanation` 字段，当前反序列化会忽略该字段（Newtonsoft 默认行为）。MVP 可以保留该字段用于调试，但若要入库/展示解释，建议后续把 `Exercise` 模型扩展为包含 `Explanation`。

### 5.4 判题反馈（FillBlankFeedbackResponse）

与 `ExerciseService` prompt 对齐：

```json
{
  "is_correct": true,
  "explanation": "string",
  "covered_points": ["string"],
  "missing_points": ["string"]
}
```

## 6. Prompt 原则与模板化建议

### 6.1 MVP 必须坚持的原则

- 输入边界：只基于系统提供的原文片段与上下文（尤其是 `snippet_id` 列表）。
- 输出格式：强制 JSON；字段名、类型、枚举值明确。
- 质量约束：
  - KP 必须“可学习、可测”（标题具体，不要泛泛的“概述/介绍”）。
  - `Summary.Pitfalls` 必须有（允许空，但优先给出）。
  - 题目必须可解释、答案明确，且能从片段中找到依据。

### 6.2 两阶段生成（extract → compose）

建议保持两类 prompt：

1) Extract：只抽取结构（知识点/要点/题目骨架/判题规则）
2) Compose：在 Extract 的结构基础上进行可读性改写（学习内容层次化、答案解释等）

当前实现中：KnowledgeBuilder 已经是偏 Extract；Learning/Exercise 是偏 Compose。

### 6.3 Prompt 存放位置

当前 prompt 写在服务类字符串中。后续建议把 prompt 抽到 `src/ASimpleTutor.Core/Prompts/`，按文件维护版本（例如 `knowledge_points.v1.md`），并在 `AppConfig` 中提供版本切换。

参考 DeepTutor 的配置组织方式（仅设计）：

- 目录按“模块/语言/用途/版本”组织，例如：
  - `src/ASimpleTutor.Core/Prompts/zh/knowledge_builder/v1/system.md`
  - `src/ASimpleTutor.Core/Prompts/zh/learning_generator/v1/system.md`
  - `src/ASimpleTutor.Core/Prompts/zh/exercise_service/v1/system.md`
- 配置（例如 `appsettings.json`）只存“选用哪个版本/哪种语言”的指针，不把提示词硬编码在配置里。
- prompt 文件只包含模板与占位符定义（例如 `{snippetTexts}`、`{kpTitle}`），具体填充在代码里完成。

这样做的收益是：prompt 可审阅、可版本化、可回滚，且不影响编译产物。

## 7. 可靠性与降级（与现有实现对齐）

### 7.1 JSON 强制与重试

`ILLMService.ChatJsonAsync<T>()` 已经做了：

- 追加“必须是有效 JSON，不要包含其他文本”的硬约束
- 清理 ```json 代码块包裹
- 最多重试 2 次（`maxRetries = 2`）

建议在文档层明确：所有可入库输出都必须走 `ChatJsonAsync<T>()`。

### 7.2 自检（强建议，MVP 可做轻量）

建议在每个阶段增加轻量校验（失败则重试/降级）：

- KnowledgePointsResponse：
  - `knowledge_points` 非空（或明确返回空的原因）
  - 每个 `snippet_ids` 至少 1 个
  - `title` 非空且去重
- LearningContentResponse：
  - `summary.definition` 非空
  - `levels` 至少包含 level=1
- ExercisesResponse：
  - 数量与 `count` 基本一致（允许少量不足，但要记录原因）
  - `type` 必须是枚举之一

### 7.3 证据追溯的现状限制（重要）

当前 `SourceTracker` 会把每个文档截断为最多 500 字存入 `SourceSnippet.Content`（用于展示/对照），并且 `HeadingPath` 目前为空。

这意味着：

- “可追溯”在 MVP 中是文档级别、且展示内容是截断的。
- 如果要更精确到章节/段落/行号，需要把 Track 逻辑升级为“按 section/heading 分 chunk”，并完整保存片段内容或可回源定位。

## 8. 版本与配置（建议补充到配置文档）

- 模型与 baseUrl：由 `LLMService` 构造参数控制（支持 OpenAI 官方与自定义兼容端点）。
- 建议在配置中明确：`model`、`baseUrl`、`apiKey`、以及 prompt 版本号。

## 9. 后续迭代清单（按收益排序）

1) 统一 JSON 字段命名策略（snake_case vs camelCase），避免跨服务的“字段风格漂移”。
2) SourceTracker 真正按章节/段落 chunk，并填充 `HeadingPath/StartLine/EndLine`，实现可点击回溯。
3) 为 `Exercise` 增加 `Explanation` 字段，或定义单独的 DTO 以避免信息丢失。
4) 引入 schema 校验（JSON Schema 或手写校验）并把失败原因打到日志。
5) 在 API 层引入“编排器”统一执行链路与 tracing（包括 requestId、bookRootId、kpId）。

## 10. 相关文档

- [00-总体设计.md](00-总体设计.md)
- [01-文档扫描与知识体系构建.md](01-文档扫描与知识体系构建.md)
- [03-学习内容生成（精要速览-原文对照-层次化展开）.md](03-学习内容生成（精要速览-原文对照-层次化展开）.md)
- [04-习题生成与练习反馈.md](04-习题生成与练习反馈.md)
- [05-数据模型与存储策略（MVP）.md](05-数据模型与存储策略（MVP）.md)

