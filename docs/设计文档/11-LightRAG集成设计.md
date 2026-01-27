# LightRAG 集成设计：详细方案

## 1. 集成模式

### 1.1 两种可选模式

| 模式 | 描述 | 适用场景 |
|------|------|----------|
| **HTTP 调用** | LightRAG 作为独立服务，通过 REST API 调用 | 微服务架构、多语言支持 |
| **SDK 嵌入** | LightRAG.NET 作为类库嵌入 ASimpleTutor 进程 | 同 .NET 技术栈、追求性能 |

**推荐**：MVP 阶段采用 **SDK 嵌入** 模式

### 1.2 分层架构

```
┌─────────────────────────────────────┐
│       ASimpleTutor.Core             │
│  知识点提取 │ 学习内容生成 │ 习题    │
└──────────────┬──────────────────────┘
               │ IRagAdapter
┌──────────────┴──────────────────────┐
│      ASimpleTutor.LightRAG.Adapter  │
│  ChunkStrategy │ SourceMapper │ ...  │
└──────────────┬──────────────────────┘
               │ ISimpleRagService
┌──────────────┴──────────────────────┐
│         LightRAG.NET.Core           │
│  文档处理 │ 向量存储 │ 图存储       │
└─────────────────────────────────────┘
```

## 2. 核心适配层设计

### 2.1 IRagAdapter 接口

```csharp
// LightRAG 适配器接口
public interface IRagAdapter
{
    // 文档处理
    Task InsertAsync(DocumentInput input);

    // 语义检索
    Task<List<SearchResult>> SearchAsync(string query, int topK);

    // 获取 chunk
    Task<TextChunk?> GetChunkAsync(string chunkId);

    // 清空数据
    Task ClearAsync();
}
```

### 2.2 SourceMapper 服务

**职责**：解决 LightRAG chunk 粒度与 ASimpleTutor 段落级追溯的差异

```
LightRAG 提供: chunkId + content + metadata
SourceMapper 映射: chunkId → filePath + headingPath + lineNumbers
```

### 2.3 ChunkStrategy 策略

**职责**：定制分块策略，保留章节结构

| LightRAG 默认 | ASimpleTutor 定制 |
|---------------|-------------------|
| Token 分块（1200 token） | 按章节边界聚类 |
| 固定重叠 | 重叠处保留段落完整性 |
| 无结构感知 | 保留 Markdown 标题层级 |

## 3. 集成工作流

### 3.1 文档扫描与入库

```
扫描 .md 文件
    ↓
解析 Markdown 结构（保留章节）
    ↓
LightRAG.Insert → chunks + 向量化
    ↓
SourceMapper.Register → chunk → 原文位置映射
    ↓
完成知识体系构建
```

### 3.2 学习内容生成

```
用户选择知识点
    ↓
LightRAG.Search → 相关 chunks
    ↓
SourceMapper.Map → 原文片段（带追溯信息）
    ↓
LLM 生成学习内容（精要速览 + 层次化展开）
    ↓
返回给用户
```

### 3.3 习题生成

```
用户选择知识点
    ↓
LightRAG.Search → 相关 chunks
    ↓
SourceMapper.Map → 原文片段
    ↓
LLM 生成习题（1~3 题）
    ↓
用户作答 → LLM 评判 → 反馈
```

## 4. 数据流

```
┌────────────────────────────────────────────────────────────────┐
│                         文档层                                  │
│  .md 文件 → 解析 → 章节结构                                     │
└────────────────────────────┬───────────────────────────────────┘
                             ↓
┌────────────────────────────┴───────────────────────────────────┐
│                      LightRAG 层                                │
│  Insert → 分块 + 向量化 + 存储                                   │
│  Search → 向量检索 → 返回 chunks                                 │
└────────────────────────────┬───────────────────────────────────┘
                             ↓
┌────────────────────────────┴───────────────────────────────────┐
│                     适配层                                       │
│  SourceMapper: chunk → 原文映射                                  │
│  ChunkStrategy: 定制分块策略                                     │
└────────────────────────────┬───────────────────────────────────┘
                             ↓
┌────────────────────────────┴───────────────────────────────────┐
│                    ASimpleTutor 层                              │
│  知识点提取 → 知识树构建                                         │
│  学习内容生成 → 习题生成 → 练习反馈                               │
└────────────────────────────────────────────────────────────────┘
```

## 5. 失败处理

| 场景 | 处理方式 |
|------|----------|
| LightRAG 不可用 | 降级为纯内存向量存储 |
| 原文映射失败 | 只返回 chunk 内容，跳过行号 |
| 检索结果为空 | 使用关键词匹配替代 |

## 6. 相关文档

- [10-模块边界划分.md](10-模块边界划分.md)
- [12-简化集成设计.md](12-简化集成设计.md)
