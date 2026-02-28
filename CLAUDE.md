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

## .NET 编码规范

### 命名规范
- **类型/方法**：PascalCase (`ChaptersController`, `ExtractAsync`)
- **参数/局部变量**：camelCase (`kpId`, `filePath`)
- **私有字段**：`_camelCase` (`_logger`, `_knowledgeSystem`)
- **接口**：`I` 前缀 (`ILLMService`, `IScannerService`)
- **常量**：PascalCase (`MaxRetries`)

### 异步编程
- 所有 I/O 操作使用 `async/await`
- 异步方法以 `Async` 后缀结尾
- 必须传递 `CancellationToken cancellationToken = default`
- **禁止** `.Result` / `.Wait()`（死锁风险）
- 并发循环使用 `Parallel.ForEachAsync`

### 依赖注入
- 构造函数注入依赖
- Controller 只编排，逻辑进 Service
- 外部依赖抽接口（`ILLMService`, `IScannerService`）
- 使用 `[FromServices]` 按需注入

### Controller 验证模式
```csharp
private bool ValidateRequest(string kpId, out KnowledgePoint? kp, out IActionResult? errorResult)
{
    kp = null;
    errorResult = null;

    if (_knowledgeSystem == null)
    {
        errorResult = NotFound(new { error = new { code = "NOT_INITIALIZED", message = "系统未初始化" } });
        return false;
    }

    if (string.IsNullOrEmpty(kpId))
    {
        errorResult = BadRequest(new { error = new { code = "INVALID_PARAM", message = "参数不能为空" } });
        return false;
    }

    kp = _knowledgeSystem.KnowledgePoints.FirstOrDefault(p => p.KpId == kpId);
    if (kp == null)
    {
        errorResult = NotFound(new { error = new { code = "NOT_FOUND", message = $"知识点不存在: {kpId}" } });
        return false;
    }

    return true;
}

// 使用
if (!ValidateRequest(kpId, out var kp, out var errorResult)) return errorResult!;
```

### JSON 处理（Newtonsoft.Json）
- DTO 属性用英文，必要时 `[JsonProperty("chinese_name")]` 映射
- 枚举用 `[JsonConverter(typeof(StringEnumConverter))]`
- 清理 markdown 代码块：移除 ` ```json` 和 ` ``` `

### 日志记录
- 使用结构化占位符：`_logger.LogInformation("处理完成: KpId={KpId}, Count={Count}", kpId, count)`
- **禁止**记录密钥/敏感原文：`ApiKey=***`
- LLM 响应只记录摘要：前 500 字符或仅长度

### 错误处理
- `try-catch` 包裹 `await` 调用
- 记录详细日志后返回 `null` 或抛出
- 并发任务失败记录日志，提供降级方案

### 资源安全
- 共享资源用 `lock` 同步
- 并发限制用 `SemaphoreSlim`
- 计数器用 `Interlocked` 原子操作

### csproj 配置
```xml
<PropertyGroup>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
</PropertyGroup>
```

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

