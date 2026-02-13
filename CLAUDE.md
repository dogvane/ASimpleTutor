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

## 测试前约束（必须遵守）

- 运行 `dotnet test` 之前，先执行清理脚本：`powershell -File scripts/clean.ps1`
- 不要直接执行 `rm -rf ...` / `rmdir /s ...` 这类删除命令；统一通过 `scripts/clean.ps1` 清理构建产物

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
- 业务并发：使用了 `ILLMService` 接口的业务，在调用该接口的方法，如果需要循环调用，可以使用 `Parallel.ForEach` 的方式进行并发

### 资源安全

- 共享资源访问：使用 `lock` 或 `Monitor` 进行同步，避免死锁
- 线程安全设计：优先使用不可变对象和线程安全集合
- 内存管理：注意避免闭包陷阱和内存泄漏（如未取消的任务）

### 错误处理

- 异步异常：使用 `try-catch` 包裹 `await` 调用，避免 `AggregateException`
- 容错机制：并发任务失败时，记录详细日志并提供降级方案
- 批量任务：使用 `WhenAll` 时，考虑单个任务失败对整体的影响

## API URL 设计原则

- 资源优先：URL 以资源名词命名，使用复数形式
  - ✅ `/api/v1/chapters`、`/api/v1/knowledge-points`
  - ❌ `/api/v1/getChapters`、`/api/v1/chapter/list`
- 参数 POST 提交：不在 URL 中使用 `/{id}/` 形式传递资源标识，参数统一放 body
  - ✅ `POST /api/v1/exercises/submit`
  - ❌ `POST /api/v1/exercises/{exerciseId}/submit`

## 常用命令（工程落地后）

```bash
dotnet build
dotnet test
dotnet run --project src/<YourProject>
dotnet format
```

