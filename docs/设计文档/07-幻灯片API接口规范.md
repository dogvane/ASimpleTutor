# 幻灯片 API 接口规范

## 核心设计理念

**直接复用知识点现有内容**，无需调用 LLM 重新生成。

每个知识点（KnowledgePoint）包含：
- `Summary` - 精要速览（定义、关键点、误区）
- `Levels` - 层次化内容（L1/L2/L3）
- `DocId` - 关联的文档 ID
- `ChapterPath` - 章节路径（用于定位原文片段）
- `Relations` - 知识点关联关系

幻灯片（Slides）直接将上述内容按类型组织返回给前端渲染。

---

## 1. 获取知识点的精要速览

### 请求

**Endpoint**: `GET /api/v1/knowledge-points/overview`

**Query Parameters**:

| 参数 | 类型 | 必填 | 说明 |
|-----|------|-----|------|
| kpId | string | 是 | 知识点 ID |

**示例请求**:
```http
GET /api/v1/knowledge-points/overview?kpId=kp_001
```

### 响应

**成功响应** (200 OK):

```json
{
  "id": "kp_001",
  "title": "神经元模型",
  "overview": {
    "definition": "神经元是神经网络的基本处理单元，它接收输入信号，进行加权求和，然后通过激活函数产生输出。",
    "keyPoints": [
      "接收多个输入信号",
      "加权求和与偏置",
      "激活函数非线性变换"
    ],
    "pitfalls": [
      "神经元不等于大脑中的神经元",
      "权重不是固定值，需要通过学习获得"
    ]
  },
  "generatedAt": "2025-01-15T10:30:00Z"
}
```

---

## 2. 获取知识点的原文对照

### 请求

**Endpoint**: `GET /api/v1/knowledge-points/source-content`

**Query Parameters**:

| 参数 | 类型 | 必填 | 说明 |
|-----|------|-----|------|
| kpId | string | 是 | 知识点 ID |

**示例请求**:
```http
GET /api/v1/knowledge-points/source-content?kpId=kp_001
```

### 响应

**成功响应** (200 OK):

```json
{
  "id": "kp_001",
  "title": "神经元模型",
  "sourceItems": [
    {
      "filePath": "g:\\books\\neural-network.md",
      "fileName": "neural-network.md",
      "headingPath": ["第2章", "2.1节"],
      "lineStart": 15,
      "lineEnd": 20,
      "content": "神经元是神经网络的基本处理单元，它接收输入信号，进行加权求和，然后通过激活函数产生输出。"
    }
  ]
}
```

---

## 3. 获取知识点的层次展开内容

### 请求

**Endpoint**: `GET /api/v1/knowledge-points/detailed-content`

**Query Parameters**:

| 参数 | 类型 | 必填 | 说明 |
|-----|------|-----|------|
| kpId | string | 是 | 知识点 ID |
| level | string | 否 | 层次（brief/detailed/deep） |

**示例请求**:
```http
GET /api/v1/knowledge-points/detailed-content?kpId=kp_001
```

### 响应

**成功响应** (200 OK):

```json
{
  "id": "kp_001",
  "title": "神经元模型",
  "levels": {
    "brief": {
      "content": "神经元的数学表达如下：y = f(Σ wᵢxᵢ + b)",
      "keyPoints": [
        "接收多个输入信号",
        "加权求和与偏置",
        "激活函数非线性变换"
      ]
    },
    "detailed": {
      "content": "神经元的输出计算分为三个步骤：1. 加权求和 2. 偏置叠加 3. 非线性变换",
      "examples": [
        "Sigmoid: σ(x) = 1/(1+e⁻ˣ)",
        "ReLU: max(0,x)"
      ]
    },
    "deep": {
      "content": "如果没有激活函数，无论网络有多少层，最终都可以简化为线性变换。激活函数引入的非线性是神经网络能够学习复杂模式的关键。",
      "relatedPatterns": [
        "梯度消失问题",
        "激活函数选择策略"
      ],
      "bestPractices": [
        "隐藏层优先使用 ReLU",
        "输出层根据任务选择激活函数"
      ]
    }
  }
}
```

---

## 4. 获取知识点的幻灯片卡片

### 请求

**Endpoint**: `GET /api/v1/knowledge-points/slide-cards`

**Query Parameters**:

| 参数 | 类型 | 必填 | 说明 |
|-----|------|-----|------|
| kpId | string | 是 | 知识点 ID |

**示例请求**:
```http
GET /api/v1/knowledge-points/slide-cards?kpId=kp_001
```

### 响应

**成功响应** (200 OK):

```json
{
  "id": "kp_001",
  "title": "神经元模型",
  "slideCards": [
    {
      "slideId": "slide_001",
      "type": "cover",
      "order": 0,
      "title": "神经元模型",
      "htmlContent": "<h2>神经元模型</h2><p>神经元是神经网络的基本处理单元...</p>",
      "speechScript": "神经元是神经网络的基本处理单元，它接收输入信号，进行加权求和，然后通过激活函数产生输出。",
      "audioUrl": "/audio/kp_001_slide_001.mp3",
      "speed": 1.0,
      "sourceReferences": [
        {
          "snippetId": "snippet_001",
          "filePath": "g:\\books\\neural-network.md",
          "headingPath": ["第2章", "2.1节"],
          "startLine": 15,
          "endLine": 20,
          "content": "神经元是神经网络的基本处理单元..."
        }
      ],
      "config": {
        "allowSkip": true,
        "requireComplete": false
      }
    },
    {
      "slideId": "slide_002",
      "type": "explanation",
      "order": 1,
      "title": "L1 概览",
      "htmlContent": "<h2>L1 概览</h2><p>神经元的数学表达如下...</p>",
      "speechScript": "神经元的数学表达如下：y = f(Σ wᵢxᵢ + b)",
      "audioUrl": "/audio/kp_001_slide_002.mp3",
      "speed": 1.0,
      "sourceReferences": [],
      "config": {
        "allowSkip": true,
        "requireComplete": false
      }
    },
    {
      "slideId": "slide_003",
      "type": "quiz",
      "order": 2,
      "title": "随堂测验",
      "htmlContent": "<div class=\"quiz-container\">...</div>",
      "speechScript": null,
      "audioUrl": null,
      "speed": 1.0,
      "sourceReferences": [],
      "config": {
        "allowSkip": true,
        "requireComplete": false
      }
    }
  ]
}
```

### 字段说明

| 字段 | 类型 | 说明 |
|------|------|------|
| slideId | string | 幻灯片唯一标识符 |
| type | string | 幻灯片类型（cover/explanation/detail/quiz等） |
| order | number | 排序序号（从0开始） |
| title | string | 幻灯片标题 |
| htmlContent | string | HTML 格式的内容 |
| speechScript | string\|null | 语音脚本（用于 TTS），可为 null |
| audioUrl | string\|null | 音频文件 URL，可为 null |
| speed | number | TTS 语速（0.25-4.0） |
| sourceReferences | array | 原文引用列表 |
| config | object | 幻灯片配置 |

### 特殊行为

1. **SpeechScript 自动生成**：如果幻灯片卡片的 `SpeechScript` 为空，系统会自动生成
2. **AudioUrl 生成**：
   - 如果 `audioUrl` 为空但 `speechScript` 不为空，会生成新的音频文件
   - 如果 `audioUrl` 对应的本地文件不存在，会重新生成音频
   - 如果 `speechScript` 为空，则跳过音频生成

---

## 数据结构说明

### SlideType 枚举

| 类型值 | 说明 | 内容来源 |
|--------|------|----------|
| `cover` | 封面/导言 | `Summary` (Definition, KeyPoints, Pitfalls) |
| `explanation` | 概念解释 | `Levels[0]` (L1) |
| `detail` | 详细内容 | `Levels[1]` (L2) |
| `deepDive` | 深入探讨 | `Levels[2]` (L3) |
| `source` | 原文对照 | `DocId + ChapterPath → Document.Sections` |
| `quiz` | 随堂测验 | `Exercises` (如有) |
| `relations` | 知识关联 | `Relations` |
| `summary` | 总结回顾 | 可选 |

### 幻灯片内容渲染指南（前端）

#### Cover 类型

**渲染建议**：
- 标题使用大号居中显示
- 定义部分使用引用块样式
- 关键点使用列表，每项一个图标
- 误区使用警告图标

```html
<h2>神经元模型</h2>
<div class="definition">
  神经元是神经网络的基本处理单元...
</div>
<div class="key-points">
  <ul>
    <li>接收多个输入信号</li>
    <li>加权求和与偏置</li>
  </ul>
</div>
<div class="pitfalls">
  ⚠️ 神经元不等于大脑中的神经元
</div>
```

#### Explanation / Detail / DeepDive 类型

**渲染建议**：
- 使用标准 HTML 渲染
- 代码块使用语法高亮
- 表格使用表格样式
- 公式可使用 KaTeX/MathJax 渲染

```html
<h2>L1 概览</h2>
<p>神经元的数学表达如下：</p>
<pre><code>y = f(Σ wᵢxᵢ + b)</code></pre>
```

#### Quiz 类型

**渲染建议**：
- 显示题目和选项
- 支持用户交互选择
- 显示正确/错误反馈

```html
<div class="quiz-container">
  <h3>随堂测验</h3>
  <p>神经元的激活函数的作用是？</p>
  <div class="quiz-options">
    <button>引入非线性</button>
    <button>降低计算复杂度</button>
  </div>
</div>
```

---

## 错误响应

| HTTP Code | Error Code | 说明 |
|----------|-----------|------|
| 400 | BAD_REQUEST | 请求参数错误 |
| 404 | KP_NOT_FOUND | 知识点不存在 |
| 500 | INTERNAL_ERROR | 服务器内部错误 |

```json
{
  "error": {
    "code": "KP_NOT_FOUND",
    "message": "知识点不存在: kp_001"
  }
}
```

---

## 注意事项

1. **内容为 HTML 格式**：前端直接使用 HTML 渲染器
2. **幻灯片数量不固定**：根据知识点实际内容生成
3. **SpeechScript 可为空**：对于 quiz 类型的幻灯片，可能不需要语音脚本
4. **AudioUrl 可为空**：如果生成失败或不需要音频
5. **TTS 语速**：根据设置中的语速配置进行调整

---

## 与旧接口的兼容性

| 旧接口 | 新接口 | 说明 |
|--------|--------|------|
| `/overview` | `/overview` | 保持不变 |
| `/detailed-content` | `/detailed-content` | 保持不变 |
| `/source-content` | `/source-content` | 保持不变 |
| `/slide-cards` | `/slide-cards` | 新增 audioUrl 和 speed 字段 |
| `/slides` | `/slide-cards` | 已废弃，建议使用新接口 |

新接口增加了 TTS 相关支持，但保持向后兼容。
