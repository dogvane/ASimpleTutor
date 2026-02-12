# 界面与交互流程 API 文档

> 基于 ASP.NET WebAPI 风格设计

## 基础信息

- **Base URL**: `/api/v1`
- **Content-Type**: `application/json`
- **认证**: 无（当前 MVP 阶段）

---

## 1. 知识系统构建相关

### 1.1 构建知识系统（管理端）

**功能描述**：扫描指定目录，构建知识系统

**请求方式**：`POST /api/v1/admin/build`

**输入**：无需输入参数（使用当前激活的书籍中心）

**输出**：
```json
{
  "success": true,
  "message": "知识体系构建完成",
  "knowledgePointCount": 25,
  "documentCount": 5,
  "bookHubId": "book1"
}
```

---

### 1.2 触发扫描（书籍管理端）

**功能描述**：触发知识体系扫描

**请求方式**：`POST /api/v1/books/scan`

**输出**：
```json
{
  "success": true,
  "taskId": "guid-here",
  "status": "completed"
}
```

---

## 2. 学习内容生成相关

### 2.1 获取精要速览

**功能描述**：获取知识点的精要速览（定义、关键点、误区）

**请求方式**：`GET /api/v1/knowledge-points/overview`

**输入**：
| 参数 | 类型 | 位置 | 说明 |
|------|------|------|------|
| kpId | string | query | 知识点 ID |

**输出**：
```json
{
  "id": "kp_001",
  "title": "单一职责原则",
  "overview": {
    "definition": "单一职责原则（Single Responsibility Principle, SRP）是面向对象设计的基本原则之一...",
    "keyPoints": [
      "一个类应该只有一个引起变化的原因",
      "职责分离",
      "高内聚低耦合"
    ],
    "pitfalls": [
      "职责划分过细导致类数量激增",
      "过度设计"
    ]
  },
  "generatedAt": "2025-01-15T10:30:00Z"
}
```

---

### 2.2 获取原文对照

**功能描述**：获取知识点关联的原文片段

**请求方式**：`GET /api/v1/knowledge-points/source-content`

**输入**：
| 参数 | 类型 | 位置 | 说明 |
|------|------|------|------|
| kpId | string | query | 知识点 ID |

**输出**：
```json
{
  "id": "kp_001",
  "title": "单一职责原则",
  "sourceItems": [
    {
      "filePath": "g:\\books\\design-patterns\\ch01.md",
      "fileName": "ch01.md",
      "headingPath": ["第1章 面向对象设计原则", "1.1 单一职责原则"],
      "lineStart": 15,
      "lineEnd": 28,
      "content": "单一职责原则是面向对象设计的基本原则之一，强调一个类应该只有一个引起变化的原因..."
    }
  ]
}
```

---

### 2.3 获取层次展开内容

**功能描述**：获取知识点的层次化内容（概览、详细、深入）

**请求方式**：`GET /api/v1/knowledge-points/detailed-content`

**输入**：
| 参数 | 类型 | 位置 | 说明 |
|------|------|------|------|
| kpId | string | query | 知识点 ID |
| level | string | query | 层次（brief/detailed/deep） |

**输出**：
```json
{
  "id": "kp_001",
  "title": "单一职责原则",
  "levels": {
    "brief": {
      "content": "单一职责原则是面向对象设计的基本原则之一，强调一个类应该只有一个引起变化的原因...",
      "keyPoints": [
        "一个类应该只有一个引起变化的原因",
        "职责分离",
        "高内聚低耦合"
      ]
    },
    "detailed": {
      "content": "单一职责原则要求一个类只负责一项职责，这样可以降低类的复杂度，提高代码的可读性和可维护性...",
      "examples": [
        "用户类只负责用户相关操作",
        "订单类只负责订单相关操作"
      ]
    },
    "deep": {
      "content": "单一职责原则的核心是职责的划分，需要在实践中根据具体情况平衡职责的粒度...",
      "relatedPatterns": [
        "开闭原则",
        "接口隔离原则"
      ],
      "bestPractices": [
        "识别类的变化原因",
        "合理划分职责边界"
      ]
    }
  }
}
```

---

### 2.4 获取幻灯片卡片

**功能描述**：获取知识点的幻灯片卡片（支持自动生成 SpeechScript 和音频 URL）

**请求方式**：`GET /api/v1/knowledge-points/slide-cards`

**输入**：
| 参数 | 类型 | 位置 | 说明 |
|------|------|------|------|
| kpId | string | query | 知识点 ID |

**输出**：
```json
{
  "id": "kp_001",
  "title": "单一职责原则",
  "slideCards": [
    {
      "slideId": "slide_001",
      "type": "cover",
      "order": 0,
      "title": "单一职责原则",
      "htmlContent": "<h2>单一职责原则</h2><p>单一职责原则（Single Responsibility Principle, SRP）是面向对象设计的基本原则之一...</p>",
      "speechScript": "单一职责原则是面向对象设计的基本原则之一，强调一个类应该只有一个引起变化的原因。",
      "audioUrl": "/audio/kp_001_slide_001.mp3",
      "speed": 1.0,
      "sourceReferences": [
        {
          "snippetId": "snippet_001",
          "filePath": "g:\\books\\design-patterns\\ch01.md",
          "headingPath": ["第1章 面向对象设计原则", "1.1 单一职责原则"],
          "startLine": 15,
          "endLine": 28,
          "content": "单一职责原则是面向对象设计的基本原则之一..."
        }
      ],
      "config": {
        "allowSkip": true,
        "requireComplete": false
      }
    }
  ]
}
```

---

## 3. 习题相关

### 3.1 检查习题状态

**功能描述**：检查指定知识点的习题生成状态

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
  "generatedAt": "2025-01-15T10:30:00Z"
}
```

---

### 3.2 获取习题列表

**功能描述**：获取指定知识点的习题列表

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
      "type": "singlechoice",
      "question": "以下哪项不是单一职责原则的优点？",
      "options": [
        "A. 提高代码可读性",
        "B. 降低类复杂度",
        "C. 增加类之间耦合度",
        "D. 便于测试"
      ],
      "answer": ""
    },
    {
      "id": "ex_002",
      "type": "truefalse",
      "question": "单一职责原则要求一个类只能有一个方法。",
      "options": ["True", "False"],
      "answer": ""
    }
  ]
}
```

---

### 3.3 提交答案

**功能描述**：提交单个习题答案并获取反馈

**请求方式**：`POST /api/v1/exercises/submit`

**输入**：
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
  "explanation": "单一职责原则旨在降低类的复杂度，提高代码的可读性和可维护性，与增加耦合度无关。",
  "referenceAnswer": "C"
}
```

---

### 3.4 批量提交并获取反馈

**功能描述**：批量提交答案并获取反馈

**请求方式**：`POST /api/v1/exercises/feedback`

**输入**：
```json
{
  "kpId": "kp_001",
  "answers": [
    {
      "exerciseId": "ex_001",
      "answer": "C"
    },
    {
      "exerciseId": "ex_002",
      "answer": "False"
    }
  ]
}
```

**输出**：
```json
{
  "kpId": "kp_001",
  "summary": {
    "total": 2,
    "correct": 2,
    "incorrect": 0
  },
  "items": [
    {
      "exerciseId": "ex_001",
      "correct": true,
      "explanation": "单一职责原则旨在降低类的复杂度，提高代码的可读性和可维护性。",
      "referenceAnswer": "C"
    },
    {
      "exerciseId": "ex_002",
      "correct": true,
      "explanation": "单一职责原则要求一个类只负责一项职责，而不是只能有一个方法。",
      "referenceAnswer": "False"
    }
  ]
}
```

---

### 3.5 刷新所有习题

**功能描述**：清空所有习题缓存并重新生成

**请求方式**：`POST /api/v1/exercises/refresh`

**输出**：
```json
{
  "message": "习题刷新完成",
  "knowledgePointCount": 10,
  "totalExerciseCount": 30,
  "status": "ready",
  "generatedAt": "2025-01-15T10:30:00Z"
}
```

---

## 4. 章节管理相关

### 4.1 获取章节树

**功能描述**：获取书籍章节树结构

**请求方式**：`GET /api/v1/chapters`

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
        }
      ]
    }
  ]
}
```

---

### 4.2 搜索章节

**功能描述**：搜索章节

**请求方式**：`GET /api/v1/chapters/search`

**输入**：
| 参数 | 类型 | 位置 | 说明 |
|------|------|------|------|
| q | string | query | 搜索关键词 |
| limit | number | query | 返回结果数量限制（默认 20） |

**输出**：
```json
{
  "items": [
    {
      "id": "ch_001_001",
      "title": "第1章 面向对象设计原则 > 1.1 单一职责原则",
      "level": 2,
      "parentId": "ch_001"
    }
  ],
  "total": 1
}
```

---

### 4.3 获取章节下的知识点列表

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
      "kpId": "kp_001",
      "title": "单一职责原则",
      "summary": ""
    }
  ]
}
```

---

## 5. 进度管理相关

### 5.1 获取单个知识点进度

**功能描述**：获取指定知识点的学习进度

**请求方式**：`GET /api/v1/progress`

**输入**：
| 参数 | 类型 | 位置 | 说明 |
|------|------|------|------|
| kpId | string | query | 知识点 ID |
| userId | string | query | 用户 ID（默认 "default"） |

**输出**：
```json
{
  "userId": "default",
  "kpId": "kp_001",
  "status": "learning",
  "masteryLevel": 0.6,
  "reviewCount": 3,
  "lastReviewTime": "2025-01-15T10:30:00Z",
  "completedSlideIds": ["slide_001", "slide_002"]
}
```

---

### 5.2 获取所有知识点进度概览

**功能描述**：获取所有知识点的进度概览

**请求方式**：`GET /api/v1/progress/overview`

**输入**：
| 参数 | 类型 | 位置 | 说明 |
|------|------|------|------|
| userId | string | query | 用户 ID（默认 "default"） |

**输出**：
```json
{
  "userId": "default",
  "total": 10,
  "mastered": 3,
  "learning": 4,
  "todo": 3,
  "averageMasteryLevel": 0.58,
  "items": [
    {
      "kpId": "kp_001",
      "title": "单一职责原则",
      "importance": 5,
      "status": "mastered",
      "masteryLevel": 0.9,
      "reviewCount": 5
    }
  ]
}
```

---

### 5.3 更新学习状态

**功能描述**：更新知识点的学习状态

**请求方式**：`PUT /api/v1/progress`

**输入**：
```json
{
  "kpId": "kp_001",
  "status": "learning",
  "masteryLevel": 0.7,
  "completedSlideIds": ["slide_001", "slide_002"]
}
```

**输出**：
```json
{
  "userId": "default",
  "kpId": "kp_001",
  "status": "learning",
  "masteryLevel": 0.7,
  "reviewCount": 4,
  "lastReviewTime": "2025-01-15T10:35:00Z",
  "completedSlideIds": ["slide_001", "slide_002"]
}
```

---

### 5.4 获取错题本

**功能描述**：获取用户的错题本

**请求方式**：`GET /api/v1/progress/mistakes`

**输入**：
| 参数 | 类型 | 位置 | 说明 |
|------|------|------|------|
| userId | string | query | 用户 ID（默认 "default"） |

**输出**：
```json
{
  "userId": "default",
  "items": [
    {
      "recordId": "mistake_001",
      "exerciseId": "ex_001",
      "kpId": "kp_001",
      "kpTitle": "单一职责原则",
      "question": "",
      "userAnswer": "A",
      "correctAnswer": "C",
      "errorAnalysis": "概念理解有误",
      "createdAt": "2025-01-15T10:30:00Z",
      "errorCount": 2
    }
  ],
  "total": 1
}
```

---

### 5.5 更新错题状态

**功能描述**：标记错题为已解决

**请求方式**：`PUT /api/v1/progress/mistakes/{recordId}`

**输入**：
```json
{
  "isResolved": true
}
```

**输出**：
```json
{
  "recordId": "mistake_001",
  "isResolved": true
}
```

---

## 6. 书籍管理相关

### 6.1 获取书籍中心列表

**功能描述**：获取所有配置的书籍中心

**请求方式**：`GET /api/v1/books/hubs`

**输出**：
```json
{
  "items": [
    {
      "id": "book1",
      "name": "设计模式",
      "path": "g:\\books\\design-patterns",
      "isActive": true
    }
  ],
  "activeId": "book1"
}
```

---

### 6.2 激活书籍中心

**功能描述**：激活指定的书籍中心

**请求方式**：`POST /api/v1/books/activate`

**输入**：
```json
{
  "bookHubId": "book1"
}
```

**输出**：
```json
{
  "success": true,
  "message": "已激活书籍中心: 设计模式"
}
```

---

### 6.3 清除缓存

**功能描述**：清除已保存的知识系统缓存

**请求方式**：`DELETE /api/v1/books/cache`

**输出**：
```json
{
  "success": true,
  "message": "缓存已清除"
}
```

---

## 7. 管理相关

### 7.1 获取系统状态

**功能描述**：获取当前系统状态

**请求方式**：`GET /api/v1/admin/status`

**输出**：
```json
{
  "bookHubName": "设计模式",
  "knowledgePointCount": 25,
  "snippetCount": 0,
  "documentCount": 5,
  "documents": [
    {
      "docId": "doc_001",
      "title": "第1章",
      "sections": [
        {
          "sectionId": "sec_001",
          "headingPath": ["第1章 面向对象设计原则"],
          "subSections": []
        }
      ]
    }
  ],
  "status": "完整",
  "timestamp": "2025-01-15T10:30:00Z"
}
```

---

## 错误响应

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
| BOOKHUB_NOT_FOUND | 书籍中心不存在 |
| KP_NOT_FOUND | 知识点不存在 |
| EXERCISE_NOT_FOUND | 习题不存在 |
| SCAN_FAILED | 知识系统构建失败 |
| GENERATION_FAILED | 内容生成失败 |
| LLM_ERROR | LLM 服务错误 |
| STORAGE_ERROR | 存储服务错误 |
