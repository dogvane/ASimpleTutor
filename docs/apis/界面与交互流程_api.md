# 界面与交互流程 API 文档

> 基于 ASP.NET WebAPI 风格设计

## 基础信息

- **Base URL**: `/api/v1`
- **Content-Type**: `application/json`
- **认证**: 无（当前 MVP 阶段）

---

## 1. 书籍目录相关

### 1.1 获取书籍目录列表

**功能描述**：获取已配置的书籍目录列表，用于下拉选择

**请求方式**：`GET /api/v1/books/roots`

**输入**：无

**输出**：
```json
{
  "items": [
    {
      "id": "br_001",
      "name": "《设计模式》",
      "path": "/books/design-patterns",
      "description": "经典设计模式教程"
    },
    {
      "id": "br_002",
      "name": "《代码整洁之道》",
      "path": "/books/clean-code",
      "description": "编写优雅代码的指南"
    }
  ],
  "activeId": "br_001"
}
```

---

### 1.2 切换书籍

**功能描述**：切换当前学习的书籍目录，触发重新扫描

**请求方式**：`POST /api/v1/books/activate`

**输入**：
| 参数 | 类型 | 位置 | 说明 |
|------|------|------|------|
| bookRootId | string | body | 书籍目录 ID |

Request Body:
```json
{
  "bookRootId": "br_001"
}
```

**输出**：
```json
{
  "success": true,
  "message": "切换成功，正在扫描..."
}
```

---

### 1.3 触发扫描

**功能描述**：触发当前书籍目录的重新扫描与知识体系重建

**请求方式**：`POST /api/v1/books/scan`

**输入**：无（使用当前激活的 bookRootId）

**输出**：
```json
{
  "success": true,
  "taskId": "task_12345",
  "status": "started"
}
```

---

## 2. 章节相关

### 2.1 获取章节树

**功能描述**：获取书籍的章节树结构

**请求方式**：`GET /api/v1/chapters`

**输入**：无（使用当前激活的 bookRootId）

**输出**：
```json
{
  "items": [
    {
      "id": "ch_001",
      "title": "第1章 面向对象设计原则",
      "level": 1,
      "expanded": true,
      "children": [
        {
          "id": "ch_001_001",
          "title": "1.1 单一职责原则",
          "level": 2,
          "expanded": false,
          "children": []
        },
        {
          "id": "ch_001_002",
          "title": "1.2 开闭原则",
          "level": 2,
          "expanded": false,
          "children": []
        }
      ]
    },
    {
      "id": "ch_002",
      "title": "第2章 创建型模式",
      "level": 1,
      "expanded": false,
      "children": []
    }
  ]
}
```

---

### 2.2 搜索章节

**功能描述**：实时搜索章节，支持键盘导航

**请求方式**：`GET /api/v1/chapters/search`

**输入**：
| 参数 | 类型 | 位置 | 说明 |
|------|------|------|------|
| q | string | query | 搜索关键词 |
| limit | int | query | 返回数量限制，默认 20 |

**输出**：
```json
{
  "items": [
    {
      "id": "ch_001",
      "title": "第1章 面向对象设计原则",
      "level": 1,
      "parentId": null
    },
    {
      "id": "ch_001_001",
      "title": "1.1 单一职责原则",
      "level": 2,
      "parentId": "ch_001"
    }
  ],
  "total": 5
}
```

---

## 3. 章节知识点相关

### 3.1 获取章节下的知识点列表

**功能描述**：获取指定章节下的知识点列表

**请求方式**：`GET /api/v1/chapters/knowledge-points`

**输入**：
| 参数 | 类型 | 位置 | 说明 |
|------|------|------|------|
| chapterId | string | query | 章节 ID |

**输出**：
```json
{
  "items": [
    {
      "id": "kp_001",
      "title": "概念定义",
      "summary": "单一职责原则是..."
    },
    {
      "id": "kp_002",
      "title": "应用场景",
      "summary": "当一个类负责多种职责时..."
    },
    {
      "id": "kp_003",
      "title": "注意事项",
      "summary": "粒度把控..."
    }
  ]
}
```

---

## 4. 知识点相关

### 4.1 获取精要速览

**功能描述**：获取知识点的 AI 生成精要速览

**请求方式**：`GET /api/v1/knowledge-points/overview`

**输入**：
| 参数 | 类型 | 位置 | 说明 |
|------|------|------|------|
| kpId | string | query | 知识点 ID |

**输出**：
```json
{
  "id": "kp_001",
  "title": "概念定义",
  "overview": "单一职责原则（Single Responsibility Principle, SRP）是面向对象设计的基本原则之一...",
  "generatedAt": "2025-01-30T10:30:00Z"
}
```

---

### 4.2 获取原文对照

**功能描述**：获取知识点关联的原文片段，包含出处信息

**请求方式**：`GET /api/v1/knowledge-points/source-content`

**输入**：
| 参数 | 类型 | 位置 | 说明 |
|------|------|------|------|
| kpId | string | query | 知识点 ID |

**输出**：
```json
{
  "id": "kp_001",
  "title": "概念定义",
  "sourceItems": [
    {
      "filePath": "/books/design-patterns/ch01.md",
      "fileName": "ch01.md",
      "headingPath": "第1章 面向对象设计原则 > 1.1 单一职责原则",
      "lineStart": 15,
      "lineEnd": 28,
      "content": "一个类应该只有一个引起变化的原因。这意味着一个类应该只有一个职责..."
    }
  ]
}
```

---

### 4.3 获取层次展开内容

**功能描述**：获取分层展开的内容（概览→详细→深入）

**请求方式**：`GET /api/v1/knowledge-points/detailed-content`

**输入**：
| 参数 | 类型 | 位置 | 说明 |
|------|------|------|------|
| kpId | string | query | 知识点 ID |
| level | string | query | 展开层级：brief/detailed/deep，默认 brief |

**输出**：
```json
{
  "id": "kp_001",
  "title": "概念定义",
  "levels": {
    "brief": {
      "content": "单一职责原则：类的职责应该单一",
      "keyPoints": ["职责分离", "高内聚低耦合"]
    },
    "detailed": {
      "content": "详细解释...",
      "examples": [
        {
          "title": "反例",
          "code": "..."
        },
        {
          "title": "正例",
          "code": "..."
        }
      ]
    },
    "deep": {
      "content": "深入分析...",
      "relatedPatterns": ["策略模式", "外观模式"],
      "bestPractices": [...]
    }
  }
}
```

---

## 5. 习题相关

### 5.1 检查习题状态

**功能描述**：检查知识点是否已生成习题，若无则触发异步生成

**请求方式**：`GET /api/v1/knowledge-points/exercises/status`

**输入**：
| 参数 | 类型 | 位置 | 说明 |
|------|------|------|------|
| kpId | string | query | 知识点 ID |

**输出**：
```json
{
  "kpId": "kp_001",
  "hasExercises": true,
  "exerciseCount": 3,
  "status": "ready",
  "generatedAt": "2025-01-30T10:35:00Z"
}
```

或未生成时：
```json
{
  "kpId": "kp_002",
  "hasExercises": false,
  "status": "generating",
  "message": "习题生成中..."
}
```

---

### 5.2 获取习题列表

**功能描述**：获取知识点的习题列表

**请求方式**：`GET /api/v1/knowledge-points/exercises`

**输入**：
| 参数 | 类型 | 位置 | 说明 |
|------|------|------|------|
| kpId | string | query | 知识点 ID |

**输出**：
```json
{
  "kpId": "kp_001",
  "items": [
    {
      "id": "ex_001",
      "type": "choice",
      "question": "以下哪项不是单一职责原则的优点？",
      "options": [
        "A. 提高代码可读性",
        "B. 降低类复杂度",
        "C. 增加类之间耦合度",
        "D. 便于测试"
      ],
      "answer": "C"
    },
    {
      "id": "ex_002",
      "type": "fill",
      "question": "单一职责原则的英文缩写是 ____",
      "answer": "SRP"
    },
    {
      "id": "ex_003",
      "type": "short",
      "question": "请简述单一职责原则的核心思想",
      "answerKeywords": ["一个类", "一个职责", "引起变化的原因"]
    }
  ]
}
```

---

### 5.3 提交答案

**功能描述**：提交单道习题答案

**请求方式**：`POST /api/v1/exercises/submit`

**输入**：
| 参数 | 类型 | 位置 | 说明 |
|------|------|------|------|
| exerciseId | string | body | 习题 ID |
| answer | string | body | 用户答案 |

Request Body:
```json
{
  "exerciseId": "ex_001",
  "answer": "C"
}
```

**输出**：
```json
{
  "exerciseId": "ex_001",
  "correct": true,
  "explanation": "单一职责原则旨在降低类的复杂度，与增加耦合度无关...",
  "referenceAnswer": "C"
}
```

---

### 5.4 批量提交并获取反馈

**功能描述**：一次性提交所有习题答案并获取反馈

**请求方式**：`POST /api/v1/exercises/feedback`

**输入**：
| 参数 | 类型 | 位置 | 说明 |
|------|------|------|------|
| kpId | string | body | 知识点 ID |
| answers | array | body | 用户答案列表 |

Request Body:
```json
{
  "kpId": "kp_001",
  "answers": [
    { "exerciseId": "ex_001", "answer": "C" },
    { "exerciseId": "ex_002", "answer": "SRP" },
    { "exerciseId": "ex_003", "answer": "一个类应该只有一个引起变化的原因" }
  ]
}
```

**输出**：
```json
{
  "kpId": "kp_001",
  "summary": {
    "total": 3,
    "correct": 2,
    "incorrect": 1
  },
  "items": [
    {
      "exerciseId": "ex_001",
      "correct": true,
      "explanation": "...",
      "referenceAnswer": "C"
    },
    {
      "exerciseId": "ex_002",
      "correct": false,
      "explanation": "...",
      "referenceAnswer": "SRP"
    },
    {
      "exerciseId": "ex_003",
      "correct": true,
      "explanation": "...",
      "referenceAnswer": "一个类应该只有一个引起变化的原因"
    }
  ]
}
```

---

## 6. 错误响应

### 通用错误格式

```json
{
  "error": {
    "code": "ERR_CODE",
    "message": "错误描述信息",
    "details": {}
  }
}
```

### 常见错误码

| HttpStatus | code | 说明 |
|------------|------|------|
| 400 | BAD_REQUEST | 请求参数错误 |
| 404 | NOT_FOUND | 资源不存在 |
| 409 | CONFLICT | 扫描进行中冲突 |
| 500 | INTERNAL_ERROR | 服务器内部错误 |

### 业务错误码

| code | 说明 |
|------|------|
| BOOKROOT_NOT_FOUND | 书籍目录不存在 |
| CHAPTER_NOT_FOUND | 章节不存在 |
| KP_NOT_FOUND | 知识点不存在 |
| EXERCISE_NOT_FOUND | 习题不存在 |
| EXERCISE_NOT_READY | 习题正在生成中 |
| SCAN_IN_PROGRESS | 扫描进行中 |
| SCAN_FAILED | 扫描失败 |
| GENERATION_FAILED | 内容生成失败 |
| LLM_ERROR | LLM 服务错误 |
