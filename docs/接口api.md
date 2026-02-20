# API 接口文档

本文档描述系统中所有 REST API 接口。

> 相关文档：[需求文档](./需求文档.md) | [数据结构](./数据结构.md)

## 目录

- [概述](#概述)
- [书籍管理](#书籍管理)
- [章节管理](#章节管理)
- [知识点](#知识点)
- [习题](#习题)
- [知识图谱](#知识图谱)
- [学习进度](#学习进度)
- [设置](#设置)
- [管理](#管理)

---

## 概述

### 基础 URL

```
http://localhost:5000/api/v1
```

### 统一响应格式

**成功响应：**
```json
{
  "items": [...],
  "success": true,
  "message": "操作成功"
}
```

**错误响应：**
```json
{
  "error": {
    "code": "ERROR_CODE",
    "message": "错误描述信息"
  }
}
```

### HTTP 状态码

| 状态码 | 说明 |
|--------|------|
| 200    | 成功 |
| 400    | 请求参数错误 |
| 404    | 资源不存在 |
| 500    | 服务器内部错误 |

---

## 书籍管理

基础路径：`/api/v1/books`

### 1. 获取书籍中心列表

**端点：** `GET /api/v1/books/hubs`

**描述：** 获取所有已配置的书籍中心列表。

**请求参数：** 无

**响应示例：**
```json
{
  "items": [
    {
      "id": "book-hub-1",
      "name": "C# 入门教程",
      "path": "G:\\books\\csharp-tutorial",
      "isActive": true
    }
  ],
  "activeId": "book-hub-1"
}
```

---

### 2. 激活书籍

**端点：** `POST /api/v1/books/activate`

**描述：** 切换当前激活的书籍中心。

**请求体：**
```json
{
  "bookHubId": "book-hub-1"
}
```

**响应示例：**
```json
{
  "success": true,
  "message": "已激活书籍中心: C# 入门教程"
}
```

---

### 3. 触发扫描

**端点：** `POST /api/v1/books/scan`

**描述：** 触发书籍目录扫描和知识体系构建（后台异步执行）。

**请求参数：** 无

**响应示例：**
```json
{
  "success": true,
  "taskId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "scanning",
  "message": "扫描任务已启动"
}
```

---

### 4. 获取扫描进度

**端点：** `GET /api/v1/books/scan-progress`

**描述：** 获取当前扫描任务的进度信息。

**请求参数：** 无

**响应示例：**
```json
{
  "taskId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "scanning",
  "currentStage": "提取知识点",
  "progressPercent": 45,
  "message": "正在处理第 15/30 个章节",
  "processedKpCount": 45,
  "totalKpCount": 100
}
```

**状态值：**
- `idle` - 空闲
- `scanning` - 扫描中
- `completed` - 完成
- `failed` - 失败

---

### 5. 清除缓存

**端点：** `DELETE /api/v1/books/cache`

**描述：** 清除已保存的知识系统缓存。

**请求参数：** 无

**响应示例：**
```json
{
  "success": true,
  "message": "缓存已清除"
}
```

---

## 章节管理

基础路径：`/api/v1/chapters`

### 1. 获取章节树

**端点：** `GET /api/v1/chapters`

**描述：** 获取完整的章节树结构。

**请求参数：** 无

**响应示例：**
```json
{
  "items": [
    {
      "id": "chapter-1",
      "title": "第一章：C# 简介",
      "level": 1,
      "expanded": true,
      "children": [
        {
          "id": "section-1-1",
          "title": "1.1 什么是 C#",
          "level": 2,
          "expanded": false,
          "children": []
        }
      ]
    }
  ]
}
```

**字段说明：**
- `id` - 章节/知识点唯一标识符
- `title` - 章节标题
- `level` - 层级深度（从 1 开始）
- `expanded` - 是否默认展开
- `children` - 子章节列表

---

### 2. 搜索章节

**端点：** `GET /api/v1/chapters/search`

**描述：** 根据关键词搜索章节。

**请求参数：**
| 参数 | 类型   | 必填 | 说明           |
|------|--------|------|----------------|
| q    | string | 是   | 搜索关键词     |
| limit| int    | 否   | 最大返回数量，默认 20 |

**响应示例：**
```json
{
  "items": [
    {
      "id": "section-1-1",
      "title": "1.1 什么是 C#",
      "level": 2,
      "parentId": "chapter-1"
    }
  ],
  "total": 1
}
```

---

### 3. 获取章节下的知识点列表

**端点：** `GET /api/v1/chapters/knowledge-points`

**描述：** 获取指定章节下的所有知识点。

**请求参数：**
| 参数      | 类型   | 必填 | 说明       |
|-----------|--------|------|------------|
| chapterId | string | 是   | 章节 ID    |

**响应示例：**
```json
{
  "items": [
    {
      "kpId": "kp-001",
      "title": "C# 语言特性",
      "summary": ""
    }
  ]
}
```

---

## 知识点

基础路径：`/api/v1/knowledge-points`

### 1. 获取知识点概览（精要速览）

**端点：** `GET /api/v1/knowledge-points/overview`

**描述：** 获取知识点的快速概览，包含定义、要点、误区。

**请求参数：**
| 参数 | 类型   | 必填 | 说明     |
|------|--------|------|----------|
| kpId | string | 是   | 知识点 ID |

**响应示例：**
```json
{
  "id": "kp-001",
  "title": "C# 语言特性",
  "overview": {
    "definition": "C# 是一种现代的、面向对象的编程语言...",
    "keyPoints": [
      "类型安全",
      "面向对象",
      "组件集成"
    ],
    "pitfalls": [
      "容易与 C++ 混淆"
    ]
  },
  "generatedAt": "2026-02-20T10:30:00Z"
}
```

---

### 2. 获取知识点原文对照

**端点：** `GET /api/v1/knowledge-points/source`

**描述：** 获取知识点的原文片段引用。

**请求参数：**
| 参数 | 类型   | 必填 | 说明     |
|------|--------|------|----------|
| kpId | string | 是   | 知识点 ID |

**响应示例：**
```json
{
  "id": "kp-001",
  "title": "C# 语言特性",
  "sourceItems": [
    {
      "filePath": "G:\\books\\csharp-tutorial\\chapter1.md",
      "headingPath": ["第一章：C# 简介", "1.1 什么是 C#"],
      "startLine": 15,
      "endLine": 25,
      "content": "C# 是一种现代的、面向对象的编程语言..."
    }
  ]
}
```

---

### 3. 获取知识点详细内容（分层展开）

**端点：** `GET /api/v1/knowledge-points/detailed-content`

**描述：** 获取知识点的分层学习内容（L1 概览 / L2 详细 / L3 深入）。

**请求参数：**
| 参数 | 类型   | 必填 | 说明                                   |
|------|--------|------|----------------------------------------|
| kpId | string | 是   | 知识点 ID                              |
| level| string | 否   | 内容层级：`1`/`2`/`3`，不传则返回所有层级 |

**响应示例：**
```json
{
  "id": "kp-001",
  "title": "C# 语言特性",
  "levels": {
    "1": {
      "level": 1,
      "title": "快速概览",
      "content": "C# 是微软开发的一种现代编程语言..."
    },
    "2": {
      "level": 2,
      "title": "详细讲解",
      "content": "C# 的核心特性包括：1. 类型安全..."
    },
    "3": {
      "level": 3,
      "title": "深入探讨",
      "content": "C# 的运行时环境为 CLR..."
    }
  },
  "generatedAt": "2026-02-20T10:30:00Z"
}
```

---

### 4. 获取知识点幻灯片卡片

**端点：** `GET /api/v1/knowledge-points/slide-cards`

**描述：** 获取知识点的 PPT 教学幻灯片卡片列表。

**请求参数：**
| 参数  | 类型   | 必填 | 说明     |
|-------|--------|------|----------|
| kpId  | string | 是   | 知识点 ID |

**响应示例：**
```json
{
  "items": [
    {
      "slideId": "slide-001",
      "kpId": "kp-001",
      "type": "Cover",
      "order": 1,
      "title": "C# 语言特性",
      "htmlContent": "<h1>C# 语言特性</h1><p>本节介绍 C# 的核心特性...</p>",
      "audioUrl": "/audio/kp-001-slide-001.mp3",
      "speechScript": "欢迎学习本节内容，C# 是一种现代的编程语言...",
      "sourceReferences": [],
      "config": {
        "allowSkip": true,
        "requireComplete": false,
        "autoPlayAudio": false
      }
    },
    {
      "slideId": "slide-002",
      "kpId": "kp-001",
      "type": "Explanation",
      "order": 2,
      "title": "什么是 C#",
      "htmlContent": "...",
      "speechScript": "C# 是微软公司开发的一种面向对象的编程语言..."
    },
    {
      "slideId": "slide-003",
      "kpId": "kp-001",
      "type": "Quiz",
      "order": 3,
      "title": "随堂测验",
      "exercises": [
        {
          "exerciseId": "ex-001",
          "type": "SingleChoice",
          "question": "C# 是由哪家公司开发的？",
          "options": ["微软", "谷歌", "苹果", "亚马逊"],
          "correctAnswer": "微软"
        }
      ]
    }
  ],
  "generatedAt": "2026-02-20T10:30:00Z"
}
```

**幻灯片类型（SlideType）：**
- `Cover` - 封面/导言
- `Explanation` - 概念解释
- `Example` - 示例/案例分析
- `DeepDive` - 深入探讨
- `Quiz` - 随堂测验
- `Source` - 原文对照
- `Relations` - 知识关联
- `Summary` - 总结回顾

---

## 习题

基础路径：`/api/v1/exercises`

### 1. 生成习题

**端点：** `POST /api/v1/exercises/generate`

**描述：** 为指定知识点生成习题（1~3 题）。

**请求参数：**
| 参数 | 类型   | 必填 | 说明     |
|------|--------|------|----------|
| kpId | string | 是   | 知识点 ID |

**响应示例：**
```json
{
  "kpId": "kp-001",
  "exercises": [
    {
      "exerciseId": "ex-001",
      "type": "SingleChoice",
      "difficulty": 2,
      "question": "C# 是由哪家公司开发的？",
      "options": ["微软", "谷歌", "苹果", "亚马逊"],
      "correctAnswer": "微软",
      "explanation": "C# 是微软公司在 2000 年推出的编程语言"
    }
  ],
  "generatedAt": "2026-02-20T10:35:00Z"
}
```

**习题类型（ExerciseType）：**
- `SingleChoice` - 单选题
- `MultiChoice` - 多选题
- `TrueFalse` - 判断题
- `ShortAnswer` - 简答题

---

### 2. 获取已生成的习题

**端点：** `GET /api/v1/exercises/knowledge-points/exercises`

**描述：** 获取指定知识点的已生成习题。

**请求参数：**
| 参数 | 类型   | 必填 | 说明     |
|------|--------|------|----------|
| kpId | string | 是   | 知识点 ID |

**响应示例：**
```json
{
  "items": [
    {
      "exerciseId": "ex-001",
      "type": "SingleChoice",
      "question": "C# 是由哪家公司开发的？",
      "options": ["微软", "谷歌", "苹果", "亚马逊"]
    }
  ]
}
```

---

### 3. 提交答案并获取反馈

**端点：** `POST /api/v1/exercises/submit`

**描述：** 提交单个或多个习题答案，获取 AI 评分和反馈。

**请求体：**
```json
{
  "kpId": "kp-001",
  "submissions": [
    {
      "exerciseId": "ex-001",
      "answer": "微软",
      "timeSpentSeconds": 30
    }
  ]
}
```

**响应示例：**
```json
{
  "kpId": "kp-001",
  "results": [
    {
      "exerciseId": "ex-001",
      "userAnswer": "微软",
      "isCorrect": true,
      "feedback": {
        "isCorrect": true,
        "explanation": "正确！C# 是微软公司开发的编程语言。",
        "referenceAnswer": "微软",
        "coveredPoints": ["知道 C# 的开发商"],
        "missingPoints": [],
        "masteryAdjustment": 0.1
      }
    }
  ],
  "evaluatedAt": "2026-02-20T10:36:00Z"
}
```

---

## 知识图谱

基础路径：`/api/v1/knowledge-graph`

### 1. 构建知识图谱

**端点：** `POST /api/v1/knowledge-graph/build`

**描述：** 根据知识点列表构建知识图谱。

**请求体：**
```json
{
  "bookHubId": "book-hub-1",
  "options": {
    "includeChapterNodes": true,
    "minImportance": 0.3,
    "maxNodes": 100,
    "calculateNodePositions": true
  }
}
```

**响应示例：**
```json
{
  "graphId": "graph-001",
  "bookHubId": "book-hub-1",
  "nodes": [
    {
      "nodeId": "kp-001",
      "title": "C# 语言特性",
      "type": "Concept",
      "importance": 0.85,
      "chapterPath": ["第一章：C# 简介"],
      "metadata": {
        "size": 1.5,
        "color": "#667eea"
      }
    }
  ],
  "edges": [
    {
      "edgeId": "edge-001",
      "sourceNodeId": "kp-001",
      "targetNodeId": "kp-002",
      "type": "Related",
      "weight": 0.8
    }
  ]
}
```

---

### 2. 获取知识图谱子图

**端点：** `POST /api/v1/knowledge-graph/subgraph`

**描述：** 获取以某个节点为中心的局部子图。

**请求体：**
```json
{
  "bookHubId": "book-hub-1",
  "rootNodeId": "kp-001",
  "depth": 2,
  "options": {}
}
```

**响应示例：**
```json
{
  "nodes": [...],
  "edges": [...]
}
```

---

### 3. 搜索知识图谱节点

**端点：** `POST /api/v1/knowledge-graph/search`

**描述：** 根据关键词搜索知识图谱节点。

**请求体：**
```json
{
  "query": "C# 语言",
  "maxResults": 10,
  "options": {}
}
```

**响应示例：**
```json
{
  "nodes": [...],
  "edges": [...],
  "totalNodes": 5,
  "totalEdges": 8
}
```

---

### 4. 获取节点邻居

**端点：** `POST /api/v1/knowledge-graph/neighbors`

**描述：** 获取指定节点的邻居节点和关联边。

**请求体：**
```json
{
  "nodeId": "kp-001",
  "maxNeighbors": 10,
  "options": {}
}
```

**响应示例：**
```json
{
  "nodes": [...],
  "edges": [...],
  "totalNodes": 3,
  "totalEdges": 5
}
```

---

## 学习进度

基础路径：`/api/v1/progress`

### 1. 获取知识点学习进度

**端点：** `GET /api/v1/progress`

**描述：** 获取用户对指定知识点的学习进度。

**请求参数：**
| 参数  | 类型   | 必填 | 说明                       |
|-------|--------|------|----------------------------|
| kpId  | string | 是   | 知识点 ID                  |
| userId| string | 否   | 用户 ID，默认 "default"    |

**响应示例：**
```json
{
  "userId": "default",
  "kpId": "kp-001",
  "status": "Learning",
  "masteryLevel": 0.65,
  "reviewCount": 3,
  "lastReviewTime": "2026-02-20T10:40:00Z",
  "completedSlideIds": ["slide-001", "slide-002"]
}
```

**学习状态（LearningStatus）：**
- `Todo` - 未开始
- `Learning` - 学习中
- `Mastered` - 已掌握

---

### 2. 获取学习进度概览

**端点：** `GET /api/v1/progress/overview`

**描述：** 获取所有知识点的学习进度概览。

**请求参数：**
| 参数  | 类型   | 必填 | 说明                       |
|-------|--------|------|----------------------------|
| userId| string | 否   | 用户 ID，默认 "default"    |

**响应示例：**
```json
{
  "userId": "default",
  "total": 100,
  "mastered": 20,
  "learning": 50,
  "todo": 30,
  "averageMasteryLevel": 0.58,
  "items": [
    {
      "kpId": "kp-001",
      "title": "C# 语言特性",
      "importance": 0.85,
      "status": "Learning",
      "masteryLevel": 0.65,
      "reviewCount": 3
    }
  ]
}
```

---

### 3. 更新学习进度

**端点：** `PUT /api/v1/progress`

**描述：** 更新用户对知识点的学习进度。

**请求体：**
```json
{
  "kpId": "kp-001",
  "status": "Learning",
  "masteryLevel": 0.7,
  "completedSlideIds": ["slide-001", "slide-002", "slide-003"],
  "addCompletedSlideId": "slide-004"
}
```

**响应示例：**
```json
{
  "userId": "default",
  "kpId": "kp-001",
  "status": "Learning",
  "masteryLevel": 0.7,
  "reviewCount": 4,
  "lastReviewTime": "2026-02-20T10:45:00Z",
  "completedSlideIds": ["slide-001", "slide-002", "slide-003", "slide-004"]
}
```

---

### 4. 获取错题本

**端点：** `GET /api/v1/progress/mistakes`

**描述：** 获取用户的错题记录。

**请求参数：**
| 参数  | 类型   | 必填 | 说明                       |
|-------|--------|------|----------------------------|
| userId| string | 否   | 用户 ID，默认 "default"    |

**响应示例：**
```json
{
  "userId": "default",
  "items": [
    {
      "recordId": "mistake-001",
      "exerciseId": "ex-005",
      "kpId": "kp-003",
      "kpTitle": "C# 数据类型",
      "question": "C# 中 int 类型占用多少字节？",
      "userAnswer": "2",
      "correctAnswer": "4",
      "errorAnalysis": "混淆了 short 和 int 的字节数",
      "createdAt": "2026-02-19T15:30:00Z",
      "errorCount": 2
    }
  ],
  "total": 1
}
```

---

### 5. 标记错题为已解决

**端点：** `PUT /api/v1/progress/mistakes/{recordId}`

**描述：** 标记错题记录为已解决。

**路径参数：**
| 参数     | 类型   | 说明         |
|----------|--------|--------------|
| recordId | string | 错题记录 ID  |

**请求体：**
```json
{
  "isResolved": true
}
```

**响应示例：**
```json
{
  "recordId": "mistake-001",
  "isResolved": true
}
```

---

## 设置

基础路径：`/api/v1/settings`

### 1. 获取 LLM 配置

**端点：** `GET /api/v1/settings/llm`

**描述：** 获取当前 LLM 服务配置。

**响应示例：**
```json
{
  "items": [
    {
      "provider": "OpenAI",
      "apiKey": "sk-xxxxxxxxxxxx",
      "model": "gpt-4",
      "baseUrl": "https://api.openai.com/v1",
      "maxTokens": 2000,
      "temperature": 0.7
    }
  ]
}
```

---

### 2. 更新 LLM 配置

**端点：** `PUT /api/v1/settings/llm`

**描述：** 更新 LLM 服务配置（实时生效）。

**请求体：**
```json
{
  "provider": "OpenAI",
  "apiKey": "sk-xxxxxxxxxxxx",
  "model": "gpt-4",
  "baseUrl": "https://api.openai.com/v1",
  "maxTokens": 2000,
  "temperature": 0.7
}
```

**响应示例：**
```json
{
  "success": true,
  "message": "配置已保存并实时生效",
  "items": [...]
}
```

---

### 3. 测试 LLM 连接

**端点：** `POST /api/v1/settings/llm/test`

**描述：** 测试 LLM 服务连接是否正常。

**请求体：**
```json
{
  "provider": "OpenAI",
  "apiKey": "sk-xxxxxxxxxxxx",
  "model": "gpt-4",
  "baseUrl": "https://api.openai.com/v1"
}
```

**响应示例：**
```json
{
  "success": true,
  "message": "连接成功！模型响应正常。",
  "items": [
    {
      "success": true,
      "message": "连接成功",
      "responseTime": 1250,
      "model": "gpt-4"
    }
  ]
}
```

---

### 4. 获取 TTS 配置

**端点：** `GET /api/v1/settings/tts`

**描述：** 获取当前 TTS 服务配置。

**响应示例：**
```json
{
  "items": [
    {
      "provider": "Azure",
      "apiKey": "xxxxxxxxxxxx",
      "region": "eastus",
      "voice": "zh-CN-XiaoxiaoNeural"
    }
  ]
}
```

---

### 5. 更新 TTS 配置

**端点：** `PUT /api/v1/settings/tts`

**描述：** 更新 TTS 服务配置（实时生效）。

**请求体：**
```json
{
  "provider": "Azure",
  "apiKey": "xxxxxxxxxxxx",
  "region": "eastus",
  "voice": "zh-CN-XiaoxiaoNeural"
}
```

**响应示例：**
```json
{
  "success": true,
  "message": "配置已保存并实时生效",
  "items": [...]
}
```

---

## 管理

基础路径：`/api/v1/admin`

### 1. 构建知识体系

**端点：** `POST /api/v1/admin/build`

**描述：** 手动触发知识体系构建（同步执行）。

**请求参数：** 无（使用当前激活的书籍中心）

**响应示例：**
```json
{
  "success": true,
  "message": "知识体系构建完成",
  "knowledgePointCount": 100,
  "documentCount": 5,
  "bookHubId": "book-hub-1"
}
```

---

### 2. 获取系统状态

**端点：** `GET /api/v1/admin/status`

**描述：** 获取系统当前状态信息。

**请求参数：** 无

**响应示例：**
```json
{
  "bookHubName": "C# 入门教程",
  "knowledgePointCount": 100,
  "snippetCount": 0,
  "documentCount": 5,
  "documents": [
    {
      "docId": "doc-001",
      "title": "第一章：C# 简介",
      "sections": [
        {
          "sectionId": "sec-001",
          "headingPath": ["第一章：C# 简介", "1.1 什么是 C#"],
          "subSections": []
        }
      ]
    }
  ],
  "status": "完整",
  "timestamp": "2026-02-20T10:50:00Z"
}
```

**状态值：**
- `未就绪` - 尚未构建知识体系
- `完整` - 已完成构建

---

## 错误码参考

| 错误码                  | 说明                           |
|-------------------------|--------------------------------|
| `BAD_REQUEST`           | 请求参数错误                   |
| `NOT_FOUND`             | 资源不存在                     |
| `BOOKHUB_NOT_FOUND`     | 书籍中心不存在                 |
| `KP_NOT_FOUND`          | 知识点不存在                   |
| `SCAN_FAILED`           | 扫描失败                       |
| `GET_SETTINGS_FAILED`   | 获取配置失败                   |
| `UPDATE_SETTINGS_FAILED`| 更新配置失败                   |
| `CONNECTION_FAILED`     | 连接测试失败                   |
| `TEST_FAILED`           | 测试失败                       |
