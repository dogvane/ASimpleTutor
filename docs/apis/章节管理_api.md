# 章节管理 API 文档

## 概述

章节管理 API 提供对书籍章节树的访问功能：
- 获取章节树
- 搜索章节
- 获取章节下的知识点列表

Base URL: `/api/v1/chapters`

---

## 1. 获取章节树

### 请求

```
GET /api/v1/chapters
```

### 响应

**成功（200 OK）**

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
      "expanded": true,
      "children": [
        {
          "id": "ch_002_001",
          "title": "2.1 单例模式",
          "level": 2,
          "expanded": false,
          "children": []
        }
      ]
    }
  ]
}
```

**字段说明**

| 字段 | 类型 | 说明 |
|------|------|------|
| id | string | 章节唯一标识符 |
| title | string | 章节标题（仅显示最后一级） |
| level | number | 层级（1=顶层章节，2=子章节） |
| expanded | boolean | 是否展开（默认第一层展开） |
| children | array | 子章节列表 |

**注意事项**

- 标题仅显示最后一级，例如 "1.1 单一职责原则" 而非 "第1章 面向对象设计原则 > 1.1 单一职责原则"
- 习题、小结、参考文献等章节会被自动过滤
- 仅显示两层结构（顶层章节 + 一级子章节）

---

## 2. 搜索章节

### 请求

```
GET /api/v1/chapters/search?q={query}&limit={limit}
```

**Query Parameters**

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| q | string | 是 | 搜索关键词 |
| limit | number | 否 | 返回结果数量限制（默认 20） |

**示例请求**

```
GET /api/v1/chapters/search?q=职责&limit=10
```

### 响应

**成功（200 OK）**

```json
{
  "items": [
    {
      "id": "ch_001_001",
      "title": "第1章 面向对象设计原则 > 1.1 单一职责原则",
      "level": 2,
      "parentId": "ch_001"
    },
    {
      "id": "ch_003_002",
      "title": "第3章 接口隔离原则",
      "level": 1,
      "parentId": null
    }
  ],
  "total": 2
}
```

**字段说明**

| 字段 | 类型 | 说明 |
|------|------|------|
| id | string | 章节唯一标识符 |
| title | string | 完整章节路径标题 |
| level | number | 层级（基于标题路径长度） |
| parentId | string\|null | 父章节 ID |
| total | number | 匹配结果总数 |

**注意事项**

- 搜索为不区分大小写的模糊匹配
- 习题、小结、参考文献等章节会被自动过滤

---

## 3. 获取章节下的知识点列表

### 请求

```
GET /api/v1/chapters/knowledge-points?chapterId={chapterId}
```

**Query Parameters**

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| chapterId | string | 是 | 章节 ID |

**示例请求**

```
GET /api/v1/chapters/knowledge-points?chapterId=ch_001
```

### 响应

**成功（200 OK）**

```json
{
  "items": [
    {
      "kpId": "kp_001",
      "title": "单一职责原则",
      "summary": ""
    },
    {
      "kpId": "kp_002",
      "title": "开闭原则",
      "summary": ""
    }
  ]
}
```

**字段说明**

| 字段 | 类型 | 说明 |
|------|------|------|
| kpId | string | 知识点唯一标识符 |
| title | string | 知识点标题 |
| summary | string | 摘要（当前版本为空字符串） |

**注意事项**

- 会返回当前章节及其所有子章节的知识点
- 如果章节不包含知识点，返回空列表

---

## 错误码说明

| 错误码 | HTTP 状态码 | 说明 |
|--------|------------|------|
| BAD_REQUEST | 400 | 请求参数错误 |
| NOT_FOUND | 404 | 知识系统未构建 |

### 错误响应格式

```json
{
  "error": {
    "code": "NOT_FOUND",
    "message": "请先激活书籍目录并构建知识体系"
  }
}
```

---

## 使用示例

### cURL

**获取章节树**
```bash
curl http://localhost:5202/api/v1/chapters
```

**搜索章节**
```bash
curl "http://localhost:5202/api/v1/chapters/search?q=职责&limit=10"
```

**获取章节知识点**
```bash
curl "http://localhost:5202/api/v1/chapters/knowledge-points?chapterId=ch_001"
```

### JavaScript

```javascript
// 获取章节树
const chapters = await fetch('/api/v1/chapters')
  .then(res => res.json())
  .then(data => data.items)

// 搜索章节
const searchResults = await fetch('/api/v1/chapters/search?q=职责&limit=10')
  .then(res => res.json())
  .then(data => data.items)

// 获取章节知识点
const kps = await fetch('/api/v1/chapters/knowledge-points?chapterId=ch_001')
  .then(res => res.json())
  .then(data => data.items)
```
