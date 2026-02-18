# 知识图谱 API 文档

## 概述

知识图谱 API 提供知识图谱的构建、查询和可视化功能：
- 构建知识图谱
- 获取知识图谱子图
- 搜索知识图谱节点
- 获取节点邻居

Base URL: `/api/v1/knowledge-graph`

---

## 1. 构建知识图谱

### 请求

```
POST /api/v1/knowledge-graph/build
```

**请求体**

```json
{
  "bookHubId": "book1",
  "options": {
    "includeChapterNodes": true,
    "minImportance": 0.0,
    "maxNodes": 1000,
    "calculateNodePositions": true,
    "addDefaultRelations": true
  }
}
```

**字段说明**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| bookHubId | string | 是 | 书籍中心 ID |
| options | object | 否 | 知识图谱构建选项 |
| options.includeChapterNodes | boolean | 否 | 是否包含章节节点（默认 true） |
| options.minImportance | number | 否 | 最小重要性阈值（默认 0.0） |
| options.maxNodes | number | 否 | 最大节点数（默认 1000） |
| options.calculateNodePositions | boolean | 否 | 是否计算节点位置（默认 true） |
| options.addDefaultRelations | boolean | 否 | 是否添加默认关系（默认 true） |

### 响应

**成功（200 OK）**

```json
{
  "graphId": "graph_123",
  "bookHubId": "book1",
  "nodes": [
    {
      "nodeId": "kp_001",
      "title": "单例模式",
      "type": "Concept",
      "importance": 0.9,
      "chapterPath": ["创建型模式", "单例模式"],
      "metadata": {
        "size": 1.85,
        "color": "#667eea",
        "position": {
          "x": 400,
          "y": 500
        }
      }
    }
  ],
  "edges": [
    {
      "edgeId": "edge_123",
      "sourceNodeId": "kp_001",
      "targetNodeId": "kp_002",
      "type": "DependsOn",
      "weight": 0.8,
      "description": "依赖"
    }
  ],
  "createdAt": "2026-02-18T10:00:00Z",
  "updatedAt": "2026-02-18T10:00:00Z"
}
```

**字段说明**

| 字段 | 类型 | 说明 |
|------|------|------|
| graphId | string | 知识图谱唯一标识符 |
| bookHubId | string | 书籍中心 ID |
| nodes | array | 知识图谱节点列表 |
| edges | array | 知识图谱边列表 |
| createdAt | string | 创建时间 |
| updatedAt | string | 更新时间 |

---

## 2. 获取知识图谱子图

### 请求

```
POST /api/v1/knowledge-graph/subgraph
```

**请求体**

```json
{
  "bookHubId": "book1",
  "rootNodeId": "kp_001",
  "depth": 2,
  "knowledgePoints": [...],
  "options": {
    "includeChapterNodes": true,
    "minImportance": 0.0
  }
}
```

**字段说明**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| bookHubId | string | 是 | 书籍中心 ID |
| rootNodeId | string | 是 | 根节点 ID |
| depth | number | 否 | 子图深度（默认 2） |
| knowledgePoints | array | 是 | 知识点列表 |
| options | object | 否 | 知识图谱构建选项 |

### 响应

**成功（200 OK）**

返回子图的节点和边，结构与构建知识图谱相同。

---

## 3. 搜索知识图谱节点

### 请求

```
POST /api/v1/knowledge-graph/search
```

**请求体**

```json
{
  "query": "单例",
  "maxResults": 10,
  "knowledgePoints": [...],
  "options": {
    "includeChapterNodes": true,
    "minImportance": 0.0
  }
}
```

**字段说明**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| query | string | 是 | 搜索关键词 |
| maxResults | number | 否 | 最大结果数（默认 10） |
| knowledgePoints | array | 是 | 知识点列表 |
| options | object | 否 | 知识图谱构建选项 |

### 响应

**成功（200 OK）**

```json
{
  "nodes": [
    {
      "nodeId": "kp_001",
      "title": "单例模式",
      "type": "Concept",
      "importance": 0.9,
      "chapterPath": ["创建型模式", "单例模式"]
    }
  ],
  "edges": [
    {
      "edgeId": "edge_123",
      "sourceNodeId": "kp_001",
      "targetNodeId": "kp_002",
      "type": "Related",
      "weight": 0.6,
      "description": "相关"
    }
  ],
  "totalNodes": 5,
  "totalEdges": 8
}
```

**字段说明**

| 字段 | 类型 | 说明 |
|------|------|------|
| nodes | array | 搜索到的节点列表 |
| edges | array | 相关的边列表 |
| totalNodes | number | 总节点数 |
| totalEdges | number | 总边数 |

---

## 4. 获取节点邻居

### 请求

```
POST /api/v1/knowledge-graph/neighbors
```

**请求体**

```json
{
  "nodeId": "kp_001",
  "maxNeighbors": 10,
  "knowledgePoints": [...],
  "options": {
    "includeChapterNodes": true,
    "minImportance": 0.0
  }
}
```

**字段说明**

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| nodeId | string | 是 | 节点 ID |
| maxNeighbors | number | 否 | 最大邻居数（默认 10） |
| knowledgePoints | array | 是 | 知识点列表 |
| options | object | 否 | 知识图谱构建选项 |

### 响应

**成功（200 OK）**

返回邻居节点和关联边，结构与搜索知识图谱节点相同。

---

## 5. 错误响应

**错误（500 Internal Server Error）**

```json
{
  "message": "Failed to build knowledge graph: ...",
  "code": "KNOWLEDGE_GRAPH_BUILD_FAILED"
}
```

**常见错误代码**

| 错误代码 | 说明 |
|---------|------|
| BOOKHUB_NOT_FOUND | 书籍中心不存在 |
| KNOWLEDGE_GRAPH_BUILD_FAILED | 构建知识图谱失败 |
| KNOWLEDGE_SYSTEM_NOT_FOUND | 知识体系不存在 |

---

## 6. 数据模型

### 6.1 知识图谱节点类型

| 类型 | 说明 |
|------|------|
| Concept | 概念 |
| Chapter | 章节 |
| Process | 流程 |
| Api | API |
| BestPractice | 最佳实践 |

### 6.2 知识图谱边类型

| 类型 | 说明 |
|------|------|
| Related | 相关 |
| DependsOn | 依赖 |
| Contains | 包含 |
| Contrast | 对比 |
| ExampleOf | 示例 |
| Extends | 扩展 |
| Implements | 实现 |

---

## 7. 使用示例

### 7.1 构建知识图谱

```javascript
const response = await fetch('/api/v1/knowledge-graph/build', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    bookHubId: 'book1',
    options: {
      includeChapterNodes: true,
      minImportance: 0.5,
      maxNodes: 100
    }
  })
});

const graph = await response.json();
console.log(`构建完成，包含 ${graph.nodes.length} 个节点和 ${graph.edges.length} 条边`);
```

### 7.2 搜索节点

```javascript
const response = await fetch('/api/v1/knowledge-graph/search', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    query: '单例模式',
    maxResults: 5,
    knowledgePoints: allKnowledgePoints,
    options: {}
  })
});

const result = await response.json();
console.log(`找到 ${result.totalNodes} 个相关节点`);
```

### 7.3 获取节点邻居

```javascript
const response = await fetch('/api/v1/knowledge-graph/neighbors', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    nodeId: 'kp_001',
    maxNeighbors: 10,
    knowledgePoints: allKnowledgePoints,
    options: {}
  })
});

const result = await response.json();
console.log(`节点有 ${result.totalNodes} 个邻居`);
```
