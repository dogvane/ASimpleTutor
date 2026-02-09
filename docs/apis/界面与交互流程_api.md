# 界面与交互流程 API 文档

> 基于 ASP.NET WebAPI 风格设计

## 基础信息

- **Base URL**: `/api/v1`
- **Content-Type**: `application/json`
- **认证**: 无（当前 MVP 阶段）

---

## 1. 知识系统构建相关

### 1.1 构建知识系统

**功能描述**：扫描指定目录，构建知识系统

**请求方式**：`POST /api/v1/knowledge/build`

**输入**：
| 参数 | 类型 | 位置 | 说明 |
|------|------|------|------|
| bookRootId | string | body | 书籍根目录 ID |
| basePath | string | body | 书籍数据源的根路径 |

Request Body:
```json
{
  "bookRootId": "br_001",
  "basePath": "g:\\books\\design-patterns"
}
```

**输出**：
```json
{
  "success": true,
  "message": "知识系统构建成功",
  "bookRootId": "br_001"
}
```

---

## 2. 学习内容生成相关

### 2.1 生成学习内容

**功能描述**：为指定知识点生成学习内容

**请求方式**：`POST /api/v1/learning/generate`

**输入**：
| 参数 | 类型 | 位置 | 说明 |
|------|------|------|------|
| kpId | string | body | 知识点 ID |
| bookRootId | string | body | 书籍根目录 ID |

Request Body:
```json
{
  "kpId": "kp_001",
  "bookRootId": "br_001"
}
```

**输出**：
```json
{
  "kpId": "kp_001",
  "summary": {
    "definition": "单一职责原则（Single Responsibility Principle, SRP）是面向对象设计的基本原则之一...",
    "keyPoints": ["一个类应该只有一个引起变化的原因", "职责分离", "高内聚低耦合"],
    "pitfalls": ["职责划分过细导致类数量激增", "过度设计"]
  },
  "levels": [
    {
      "level": 1,
      "title": "概览",
      "content": "单一职责原则是面向对象设计的基本原则之一，强调一个类应该只有一个引起变化的原因..."
    },
    {
      "level": 2,
      "title": "详细",
      "content": "单一职责原则要求一个类只负责一项职责，这样可以降低类的复杂度，提高代码的可读性和可维护性..."
    },
    {
      "level": 3,
      "title": "深入",
      "content": "单一职责原则的核心是职责的划分，需要在实践中根据具体情况平衡职责的粒度..."
    }
  ],
  "slideCards": [
    {
      "slideId": "slide_001",
      "kpId": "kp_001",
      "type": "cover",
      "order": 0,
      "title": "单一职责原则",
      "htmlContent": "<h2>单一职责原则</h2><p>单一职责原则（Single Responsibility Principle, SRP）是面向对象设计的基本原则之一...</p>",
      "sourceReferences": [],
      "config": {
        "allowSkip": true,
        "requireComplete": false
      }
    },
    {
      "slideId": "slide_002",
      "kpId": "kp_001",
      "type": "explanation",
      "order": 1,
      "title": "详细解释",
      "htmlContent": "<h2>详细解释</h2><p>单一职责原则要求一个类只负责一项职责，这样可以降低类的复杂度，提高代码的可读性和可维护性...</p>",
      "sourceReferences": [],
      "config": {
        "allowSkip": true,
        "requireComplete": false
      }
    }
  ],
  "snippetIds": ["snippet_001", "snippet_002"],
  "relatedKpIds": []
}
```

---

## 3. 习题相关

### 3.1 生成习题

**功能描述**：为指定知识点生成习题

**请求方式**：`POST /api/v1/exercises/generate`

**输入**：
| 参数 | 类型 | 位置 | 说明 |
|------|------|------|------|
| kpId | string | body | 知识点 ID |
| bookRootId | string | body | 书籍根目录 ID |

Request Body:
```json
{
  "kpId": "kp_001",
  "bookRootId": "br_001"
}
```

**输出**：
```json
{
  "kpId": "kp_001",
  "exercises": [
    {
      "exerciseId": "ex_001",
      "kpId": "kp_001",
      "type": "SingleChoice",
      "difficulty": 2,
      "question": "以下哪项不是单一职责原则的优点？",
      "options": [
        "A. 提高代码可读性",
        "B. 降低类复杂度",
        "C. 增加类之间耦合度",
        "D. 便于测试"
      ],
      "correctAnswer": "C",
      "explanation": "单一职责原则旨在降低类的复杂度，提高代码的可读性和可维护性，与增加耦合度无关。",
      "hint": "单一职责原则的核心是职责分离"
    },
    {
      "exerciseId": "ex_002",
      "kpId": "kp_001",
      "type": "TrueFalse",
      "difficulty": 1,
      "question": "单一职责原则要求一个类只能有一个方法。",
      "options": ["True", "False"],
      "correctAnswer": "False",
      "explanation": "单一职责原则要求一个类只负责一项职责，而不是只能有一个方法。",
      "hint": "职责是指引起变化的原因"
    }
  ]
}
```

---

### 3.2 提交答案并获取反馈

**功能描述**：提交习题答案并获取反馈

**请求方式**：`POST /api/v1/exercises/feedback`

**输入**：
| 参数 | 类型 | 位置 | 说明 |
|------|------|------|------|
| exerciseId | string | body | 习题 ID |
| kpId | string | body | 知识点 ID |
| userAnswer | string | body | 用户答案 |
| bookRootId | string | body | 书籍根目录 ID |

Request Body:
```json
{
  "exerciseId": "ex_001",
  "kpId": "kp_001",
  "userAnswer": "C",
  "bookRootId": "br_001"
}
```

**输出**：
```json
{
  "exerciseId": "ex_001",
  "kpId": "kp_001",
  "userAnswer": "C",
  "isCorrect": true,
  "explanation": "单一职责原则旨在降低类的复杂度，提高代码的可读性和可维护性，与增加耦合度无关。",
  "referenceAnswer": "C"
}
```

---

## 4. 知识系统存储相关

### 4.1 加载知识系统

**功能描述**：加载指定书籍根目录的知识系统

**请求方式**：`GET /api/v1/knowledge/load`

**输入**：
| 参数 | 类型 | 位置 | 说明 |
|------|------|------|------|
| bookRootId | string | query | 书籍根目录 ID |

**输出**：
```json
{
  "bookRootId": "br_001",
  "knowledgePoints": [
    {
      "kpId": "kp_001",
      "title": "单一职责原则",
      "type": "Concept",
      "importance": 5,
      "snippetIds": ["snippet_001", "snippet_002"],
      "chapterPath": ["第1章 面向对象设计原则", "1.1 单一职责原则"]
    }
  ],
  "snippets": {
    "snippet_001": {
      "snippetId": "snippet_001",
      "content": "单一职责原则是面向对象设计的基本原则之一...",
      "filePath": "g:\\books\\design-patterns\\ch01.md",
      "startLine": 15,
      "endLine": 28
    }
  },
  "knowledgeTree": {
    "id": "ch_001",
    "title": "第1章 面向对象设计原则",
    "children": [
      {
        "id": "ch_001_001",
        "title": "1.1 单一职责原则",
        "children": [
          {
            "id": "kp_001",
            "title": "单一职责原则",
            "children": []
          }
        ]
      }
    ]
  }
}
```

---

## 5. 错误响应

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
| 500 | INTERNAL_ERROR | 服务器内部错误 |

### 业务错误码

| code | 说明 |
|------|------|
| BOOKROOT_NOT_FOUND | 书籍目录不存在 |
| KP_NOT_FOUND | 知识点不存在 |
| EXERCISE_NOT_FOUND | 习题不存在 |
| BUILD_FAILED | 知识系统构建失败 |
| GENERATION_FAILED | 内容生成失败 |
| LLM_ERROR | LLM 服务错误 |
| STORAGE_ERROR | 存储服务错误 |
