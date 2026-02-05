# Agent 编排与提示词策略

## 1. 架构选型：Semantic Kernel (SK)

利用 SK 的核心能力构建健壮的 AI 应用：
- **Kernel**: 上下文容器。
- **Skills/Plugins**: 封装原子能力（Native Functions & Semantic Functions）。
- **Planner**: 处理高阶意图（如 "帮我复习第三章" -> 分解为 检索重点 -> 生成总结 -> 出题）。
- **Memory**: 集成向量检索。

## 2. Agent 角色与 Plugins 定义

### 2.1 Knowledge Extraction Plugin
- **Function**: `ExtractConcepts`
  - **Prompt**: 分析文本，提取技术概念、定义、关系。要求严格的 JSON 输出。
- **Function**: `BuildStructure`
  - **Prompt**: 分析目录树，推断章节逻辑关系。

### 2.2 Tutor Plugin (Content Generation)
- **Function**: `ExplainConcept`
  - **Input**: `Concept`, `TargetAudienceLevel`, `SourceContext`.
  - **Prompt**: 扮演导师，基于 Context 生成解释，使用隐喻和例子。
- **Function**: `DeepDiveQA`
  - **Prompt**: 综合多源信息回答复杂问题，并给出延伸思考。

### 2.3 Assessment Plugin
- **Function**: `GenerateQuiz`
  - **Input**: `Concept`, `Difficulty`, `HistoryMistakes`.
  - **Prompt**: 生成题目，包含干扰项设计逻辑。
- **Function**: `GradeAnswer`
  - **Prompt**: 评估主观题，给出得分理由和改进建议。

### 2.4 Retrieval Plugin (RAG)
- **Native Function**: `SearchVectors`
  - 封装向量数据库查询。
- **Native Function**: `GetSourceSnippet`
  - 封装文件读取。

## 3. 提示词工程 (Prompt Engineering)

### 3.1 模板化 (Templating)
所有 Prompt 存放在 `Prompts` 目录下，按 `<Skill>/<Function>/skprompt.txt` 组织。
支持多语言 (i18n) 和多版本管理。

### 3.2 策略
- **Few-Shot Learning**: 在 Prompt 中提供 1-3 个高质量的 `Input-Output` 示例，稳定输出格式。
- **Chain of Thought (CoT)**: 要求模型 "Let's think step by step"，特别是在判题和复杂推理时。
- **Grounding**: 强制要求输出引用标记，任何事实性陈述必须有 Source 支持。

### 3.3 动态上下文窗口 (Context Management)
- **Token Budgeting**: 在组装 Context 时，根据模型 Token 上限动态截断原文，优先保留强相关片段。
- **Recency bias**: 在复习模式下，优先包含最近的错题记录。

## 4. 编排模式

### 4.1 简单任务 (Pipeline)
直接串联 Function。
e.g. `SearchVectors` -> `GetSourceSnippet` -> `ExplainConcept`.

### 4.2 复杂任务 (Planner)
对于模糊指令 "我要掌握这个概念"，由 SequentialPlanner 生成计划：
1. 检索定义。
2. 生成 L1 解释。
3. 如果用户反馈 "不懂"，触发生成 L2 解释 + 更多例子。
4. 生成 1 道自测题确认理解。

## 5. 质量评估与迭代
建立 Prompt 评估集（Golden Dataset），在 CI/CD 流程中自动评估 Prompt 修改后的效果（准确率、格式合规率）。
```