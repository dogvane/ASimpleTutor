# 书籍管理 API 文档

## 概述

书籍管理 API 提供对多个书籍中心（BookHub）的管理功能：
- 获取书籍中心列表
- 激活指定书籍中心
- 触发知识体系扫描
- 清除缓存

Base URL: `/api/v1/books`

---

## 1. 获取书籍中心列表

### 请求

```
GET /api/v1/books/hubs
```

### 响应

**成功（200 OK）**

```json
{
  "items": [
    {
      "id": "book1",
      "name": "设计模式",
      "path": "g:\\books\\design-patterns",
      "isActive": true
    },
    {
      "id": "book2",
      "name": "神经网络",
      "path": "g:\\books\\neural-network",
      "isActive": false
    }
  ],
  "activeId": "book1"
}
```

**字段说明**

| 字段 | 类型 | 说明 |
|------|------|------|
| id | string | 书籍中心唯一标识符 |
| name | string | 书籍中心显示名称 |
| path | string | 书籍中心的本地文件系统路径 |
| isActive | boolean | 是否为当前激活的书籍中心 |
| activeId | string | 当前激活的书籍中心 ID |

---

## 2. 激活书籍中心

### 请求

```
POST /api/v1/books/activate
Content-Type: application/json
```

**请求体**

```json
{
  "bookHubId": "book1"
}
```

**字段说明**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| bookHubId | string | 是 | 书籍中心 ID |

### 响应

**成功（200 OK）**

```json
{
  "success": true,
  "message": "已激活书籍中心: 设计模式"
}
```

**错误响应**

书籍中心不存在（404 Not Found）：

```json
{
  "error": {
    "code": "BOOKHUB_NOT_FOUND",
    "message": "书籍中心不存在: book1"
  }
}
```

目录不存在（400 Bad Request）：

```json
{
  "error": {
    "code": "BAD_REQUEST",
    "message": "目录不存在: g:\\books\\design-patterns"
  }
}
```

---

## 3. 触发扫描（构建知识体系）

### 请求

```
POST /api/v1/books/scan
```

### 响应

**成功（200 OK）**

```json
{
  "success": true,
  "taskId": "guid-here",
  "status": "completed"
}
```

**字段说明**

| 字段 | 类型 | 说明 |
|------|------|------|
| success | boolean | 扫描是否成功 |
| taskId | string | 任务 ID（用于跟踪） |
| status | string | 任务状态（completed/processing/failed） |

**错误响应**

未激活书籍中心（400 Bad Request）：

```json
{
  "error": {
    "code": "BAD_REQUEST",
    "message": "请先激活书籍中心"
  }
}
```

扫描失败（500 Internal Server Error）：

```json
{
  "error": {
    "code": "SCAN_FAILED",
    "message": "知识体系构建失败: [详细错误信息]"
  }
}
```

**副作用**

- 构建完成后会保存到持久化存储
- 更新内存中的知识系统，供其他控制器使用

---

## 4. 清除缓存

### 请求

```
DELETE /api/v1/books/cache
```

### 响应

**成功（200 OK）**

```json
{
  "success": true,
  "message": "缓存已清除"
}
```

**成功（200 OK，无缓存）**

```json
{
  "success": false,
  "message": "无缓存可清除"
}
```

**错误响应**

未激活书籍中心（400 Bad Request）：

```json
{
  "error": {
    "code": "BAD_REQUEST",
    "message": "请先激活书籍中心"
  }
}
```

**副作用**

- 删除本地存储的知识系统文件
- 清除内存中的知识系统引用

---

## 错误码说明

| 错误码 | HTTP 状态码 | 说明 |
|--------|------------|------|
| BAD_REQUEST | 400 | 请求参数错误 |
| BOOKHUB_NOT_FOUND | 404 | 书籍中心不存在 |
| SCAN_FAILED | 500 | 知识体系构建失败 |

---

## 使用示例

### cURL

**获取书籍列表**
```bash
curl http://localhost:5202/api/v1/books/hubs
```

**激活书籍**
```bash
curl -X POST http://localhost:5202/api/v1/books/activate \
  -H "Content-Type: application/json" \
  -d '{
    "bookHubId": "book1"
  }'
```

**触发扫描**
```bash
curl -X POST http://localhost:5202/api/v1/books/scan
```

**清除缓存**
```bash
curl -X DELETE http://localhost:5202/api/v1/books/cache
```

### JavaScript

```javascript
// 获取书籍列表
const books = await fetch('/api/v1/books/hubs')
  .then(res => res.json())
  .then(data => data.items)

// 激活书籍
await fetch('/api/v1/books/activate', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ bookHubId: 'book1' })
})

// 触发扫描
const scanResult = await fetch('/api/v1/books/scan', {
  method: 'POST'
}).then(res => res.json())

// 清除缓存
await fetch('/api/v1/books/cache', {
  method: 'DELETE'
})
```
