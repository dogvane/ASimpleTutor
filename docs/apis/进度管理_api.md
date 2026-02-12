# 进度管理 API 文档

## 概述

进度管理 API 提供用户学习进度追踪功能：
- 获取单个知识点进度
- 获取所有知识点进度概览
- 更新学习状态
- 错题本管理

Base URL: `/api/v1/progress`

---

## 1. 获取单个知识点进度

### 请求

```
GET /api/v1/progress?kpId={kpId}&userId={userId}
```

**Query Parameters**

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| kpId | string | 是 | 知识点 ID |
| userId | string | 否 | 用户 ID（默认 "default"） |

**示例请求**

```
GET /api/v1/progress?kpId=kp_001&userId=default
```

### 响应

**成功（200 OK）**

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

**字段说明**

| 字段 | 类型 | 说明 |
|------|------|------|
| userId | string | 用户 ID |
| kpId | string | 知识点 ID |
| status | string | 学习状态（todo/learning/mastered） |
| masteryLevel | number | 掌握度（0.0 ~ 1.0） |
| reviewCount | number | 复习次数 |
| lastReviewTime | string\|null | 最后复习时间（ISO 8601 格式） |
| completedSlideIds | array | 已完成的幻灯片 ID 列表 |

**学习状态枚举**

| 值 | 说明 |
|----|------|
| todo | 未开始 |
| learning | 学习中 |
| mastered | 已掌握 |

---

## 2. 获取所有知识点进度概览

### 请求

```
GET /api/v1/progress/overview?userId={userId}
```

**Query Parameters**

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| userId | string | 否 | 用户 ID（默认 "default"） |

**示例请求**

```
GET /api/v1/progress/overview?userId=default
```

### 响应

**成功（200 OK）**

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
    },
    {
      "kpId": "kp_002",
      "title": "开闭原则",
      "importance": 4,
      "status": "learning",
      "masteryLevel": 0.6,
      "reviewCount": 2
    },
    {
      "kpId": "kp_003",
      "title": "里氏替换原则",
      "importance": 3,
      "status": "todo",
      "masteryLevel": 0,
      "reviewCount": 0
    }
  ]
}
```

**字段说明**

| 字段 | 类型 | 说明 |
|------|------|------|
| userId | string | 用户 ID |
| total | number | 总知识点数 |
| mastered | number | 已掌握数量 |
| learning | number | 学习中数量 |
| todo | number | 未开始数量 |
| averageMasteryLevel | number | 平均掌握度 |
| items | array | 各知识点进度列表 |

**items 字段说明**

| 字段 | 类型 | 说明 |
|------|------|------|
| kpId | string | 知识点 ID |
| title | string | 知识点标题 |
| importance | number | 重要性等级 |
| status | string | 学习状态 |
| masteryLevel | number | 掌握度（0.0 ~ 1.0） |
| reviewCount | number | 复习次数 |

---

## 3. 更新学习状态

### 请求

```
PUT /api/v1/progress?userId={userId}
Content-Type: application/json
```

**Query Parameters**

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| userId | string | 否 | 用户 ID（默认 "default"） |

**请求体**

```json
{
  "kpId": "kp_001",
  "status": "learning",
  "masteryLevel": 0.7,
  "completedSlideIds": ["slide_001", "slide_002", "slide_003"]
}
```

或使用 `addCompletedSlideId` 增量添加：

```json
{
  "kpId": "kp_001",
  "addCompletedSlideId": "slide_004"
}
```

**字段说明**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| kpId | string | 是 | 知识点 ID |
| status | string | 否 | 学习状态（todo/learning/mastered） |
| masteryLevel | number | 否 | 掌握度（0.0 ~ 1.0） |
| completedSlideIds | array | 否 | 已完成的幻灯片 ID 列表（会覆盖已有） |
| addCompletedSlideId | string | 否 | 添加已完成的幻灯片 ID（增量） |

### 响应

**成功（200 OK）**

```json
{
  "userId": "default",
  "kpId": "kp_001",
  "status": "learning",
  "masteryLevel": 0.7,
  "reviewCount": 4,
  "lastReviewTime": "2025-01-15T10:35:00Z",
  "completedSlideIds": ["slide_001", "slide_002", "slide_003", "slide_004"]
}
```

**注意事项**

- `masteryLevel` 会被自动限制在 0.0 ~ 1.0 范围内
- 当 `masteryLevel >= 0.8` 时，状态会自动设置为 `mastered`
- 当 `masteryLevel > 0` 时，状态会自动设置为 `learning`
- 每次更新都会增加 `reviewCount` 并更新 `lastReviewTime`

---

## 4. 获取错题本

### 请求

```
GET /api/v1/progress/mistakes?userId={userId}
```

**Query Parameters**

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| userId | string | 否 | 用户 ID（默认 "default"） |

**示例请求**

```
GET /api/v1/progress/mistakes?userId=default
```

### 响应

**成功（200 OK）**

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

**字段说明**

| 字段 | 类型 | 说明 |
|------|------|------|
| userId | string | 用户 ID |
| items | array | 错题记录列表 |
| total | number | 错题总数 |

**items 字段说明**

| 字段 | 类型 | 说明 |
|------|------|------|
| recordId | string | 错题记录唯一标识 |
| exerciseId | string | 习题 ID |
| kpId | string | 知识点 ID |
| kpTitle | string | 知识点标题 |
| question | string | 题目内容（当前版本为空） |
| userAnswer | string | 用户答案 |
| correctAnswer | string | 正确答案 |
| errorAnalysis | string | 错误分析 |
| createdAt | string | 创建时间（ISO 8601 格式） |
| errorCount | number | 错误次数 |

---

## 5. 更新错题状态

### 请求

```
PUT /api/v1/progress/mistakes/{recordId}
Content-Type: application/json
```

**路径参数**

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| recordId | string | 是 | 错题记录 ID |

**请求体**

```json
{
  "isResolved": true
}
```

**字段说明**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| isResolved | boolean | 是 | 是否已解决（设为 true 时标记为已解决） |

### 响应

**成功（200 OK）**

```json
{
  "recordId": "mistake_001",
  "isResolved": true
}
```

**错误响应**

错题记录不存在（404 Not Found）：

```json
{
  "error": {
    "code": "NOT_FOUND",
    "message": "错题记录不存在"
  }
}
```

---

## 错误码说明

| 错误码 | HTTP 状态码 | 说明 |
|--------|------------|------|
| BAD_REQUEST | 400 | 请求参数错误 |
| NOT_FOUND | 404 | 资源不存在 |
| KP_NOT_FOUND | 404 | 知识点不存在 |

### 错误响应格式

```json
{
  "error": {
    "code": "KP_NOT_FOUND",
    "message": "知识点不存在: kp_001"
  }
}
```

---

## 使用示例

### cURL

**获取知识点进度**
```bash
curl "http://localhost:5202/api/v1/progress?kpId=kp_001&userId=default"
```

**获取进度概览**
```bash
curl "http://localhost:5202/api/v1/progress/overview?userId=default"
```

**更新学习状态**
```bash
curl -X PUT "http://localhost:5202/api/v1/progress?userId=default" \
  -H "Content-Type: application/json" \
  -d '{
    "kpId": "kp_001",
    "status": "learning",
    "masteryLevel": 0.7
  }'
```

**增量添加已完成幻灯片**
```bash
curl -X PUT "http://localhost:5202/api/v1/progress?userId=default" \
  -H "Content-Type: application/json" \
  -d '{
    "kpId": "kp_001",
    "addCompletedSlideId": "slide_004"
  }'
```

**获取错题本**
```bash
curl "http://localhost:5202/api/v1/progress/mistakes?userId=default"
```

**标记错题已解决**
```bash
curl -X PUT "http://localhost:5202/api/v1/progress/mistakes/mistake_001" \
  -H "Content-Type: application/json" \
  -d '{
    "isResolved": true
  }'
```

### JavaScript

```javascript
// 获取知识点进度
const progress = await fetch('/api/v1/progress?kpId=kp_001&userId=default')
  .then(res => res.json())

// 获取进度概览
const overview = await fetch('/api/v1/progress/overview?userId=default')
  .then(res => res.json())

// 更新学习状态
await fetch('/api/v1/progress?userId=default', {
  method: 'PUT',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    kpId: 'kp_001',
    status: 'learning',
    masteryLevel: 0.7
  })
})

// 增量添加已完成幻灯片
await fetch('/api/v1/progress?userId=default', {
  method: 'PUT',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    kpId: 'kp_001',
    addCompletedSlideId: 'slide_004'
  })
})

// 获取错题本
const mistakes = await fetch('/api/v1/progress/mistakes?userId=default')
  .then(res => res.json())
  .then(data => data.items)

// 标记错题已解决
await fetch('/api/v1/progress/mistakes/mistake_001', {
  method: 'PUT',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ isResolved: true })
})
```
