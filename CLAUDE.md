# CLAUDE.md（简化版）

用于指导 Claude Code 在本仓库按 MVP 需求开发。

## 项目目标

做一个“基于书籍 Markdown 文档的智能学习系统（单本书/单目录）”：
- 启动扫描指定目录 `.md`
- Agent 建知识体系/知识点
- 选择知识点后：总结 + 原文对照 + 简单自测 + 最小反馈

需求来源：docs/需求文档.md（以此为准）

## 范围边界

做：知识点提取、展示/搜索、学习内容生成、1~3 题自测、答案反馈

不做：多书切换、学习进度/错题本/报告/章节考核等闭环沉淀

## 最小流程（按这个顺序落地）

1. 扫描 `.md` → 解析成章节/段落块（用于引用）
2. 基于解析结果 → 生成知识点树（含层级，关联可选）
3. 选知识点 → 生成：精要速览 + 分层展开（概览/详细/深入） + 原文片段引用
4. 生成 1~3 题（选/填/简）→ 用户作答 → 输出反馈与参考答案/要点

## 两条硬约束（别省略）

- 原文对照必须可追溯：至少包含 `filePath` + `标题路径` + `段落/行号范围`
- Agent 输出尽量用结构化 JSON，解析失败最多重试 1 次（修复 JSON），不要无限重试

## 文档设计阶段

- 需求文档里不能有代码描述
- 设计文档里不能有代码实现，允许有接口的代码签名描述
- 设计文档里流程图/数据流图可以用 ASCII 艺术画（Markdown 代码块），但不能过多，要简洁明了

## .NET 编码规范（常用清单）

- 启用 Nullable：`<Nullable>enable</Nullable>`
- 命名：类型/方法 PascalCase；参数/局部 camelCase；私有字段 `_camelCase`；接口 `I` 前缀
- 异步：I/O 一律 async；异步方法 `Async` 后缀；传 `CancellationToken`；禁用 `.Result`/`.Wait()`
- 分层：Controller/Endpoint 只编排；核心逻辑进 service；外部依赖（LLM/文件/索引）抽接口
- JSON：DTO 优先英文属性；必要时用 `[JsonPropertyName("...")]`；对外返回可加 `schemaVersion`
- 日志：`ILogger<T>` 结构化日志；不要记录密钥/敏感原文；LLM 仅记录耗时/摘要

## 常用命令（工程落地后）

```bash
dotnet build
dotnet test
dotnet run --project src/<YourProject>
dotnet format
```

