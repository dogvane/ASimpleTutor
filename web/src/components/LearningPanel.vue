<template>
  <section class="learning-panel">
    <header class="panel-header">
      <div>
        <div class="label">章节</div>
        <div class="title">{{ chapterTitle || '未选择章节' }}</div>
      </div>
      <div class="status">
        <span v-if="exercisesStatus === 'generating'" class="chip warning">习题生成中</span>
        <span v-else-if="exercisesStatus === 'ready'" class="chip success">习题已就绪</span>
      </div>
    </header>

    <div class="kp-tabs" v-if="knowledgePoints.length">
      <button
        v-for="kp in knowledgePoints"
        :key="kp.id"
        type="button"
        :class="['kp-chip', { active: kp.id === selectedKpId }]"
        @click="$emit('select-kp', kp)"
      >
        {{ kp.title }}
        <span v-if="kp.id === selectedKpId" class="arrow">▾</span>
      </button>
    </div>

    <div v-else class="empty-wrapper">
      <slot name="empty"></slot>
    </div>

    <div v-if="selectedKpId" class="content">
      <div class="tabs">
        <button :class="{ active: learningTab === 'overview' }" @click="$emit('tab-change', 'overview')">精要速览</button>
        <button :class="{ active: learningTab === 'source' }" @click="$emit('tab-change', 'source')">原文对照</button>
        <button :class="{ active: learningTab === 'levels' }" @click="$emit('tab-change', 'levels')">层次展开</button>
      </div>

      <div class="panel-body">
        <slot name="loading"></slot>
        <div v-if="learningTab === 'overview'" class="overview">
          <h3>{{ overview?.title || '概览' }}</h3>
          <div v-if="overview?.overview?.definition" class="definition">{{ overview.overview.definition }}</div>

          <div v-if="overview?.overview?.keyPoints?.length" class="section">
            <div class="section-title">核心要点</div>
            <ul class="point-list">
              <li v-for="(point, index) in overview.overview.keyPoints" :key="index">{{ point }}</li>
            </ul>
          </div>

          <div v-if="overview?.overview?.pitfalls?.length" class="section">
            <div class="section-title">常见误区</div>
            <ul class="pitfall-list">
              <li v-for="(pitfall, index) in overview.overview.pitfalls" :key="index">{{ pitfall }}</li>
            </ul>
          </div>

          <div class="meta" v-if="overview?.generatedAt">生成时间：{{ formatTime(overview.generatedAt) }}</div>
        </div>

        <div v-else-if="learningTab === 'source'" class="source">
          <div v-if="source?.sourceItems?.length">
            <div v-for="item in source.sourceItems" :key="item.filePath + item.lineStart" class="snippet">
              <div class="snippet-meta">
                <span>{{ item.fileName }}</span>
                <span class="divider">|</span>
                <span>{{ item.headingPath.join(' > ') }}</span>
                <span class="divider">|</span>
                <span>行 {{ item.lineStart }}-{{ item.lineEnd }}</span>
              </div>
              <pre>{{ item.content }}</pre>
            </div>
          </div>
          <div v-else class="empty-text">暂无原文对照内容</div>
        </div>

        <div v-else class="levels">
          <div class="level-tabs">
            <button :class="{ active: activeLevel === 'brief' }" @click="$emit('level-change', 'brief')">简要</button>
            <button :class="{ active: activeLevel === 'detailed' }" @click="$emit('level-change', 'detailed')">详细</button>
            <button :class="{ active: activeLevel === 'deep' }" @click="$emit('level-change', 'deep')">深入</button>
          </div>
          <div class="level-content">
            <p>{{ levels?.levels?.[activeLevel]?.content || '暂无内容' }}</p>
            <div v-if="levels?.levels?.[activeLevel]?.keyPoints?.length" class="tags">
              <span v-for="point in levels.levels[activeLevel].keyPoints" :key="point" class="tag">{{ point }}</span>
            </div>
          </div>
        </div>
      </div>

      <div class="exercise-bar">
        <button
          class="exercise-btn"
          type="button"
          :disabled="exercisesStatus !== 'ready'"
          @click="$emit('open-exercises')"
        >
          {{ exercisesStatus === 'ready' ? '进入习题' : exercisesStatus === 'generating' ? '习题生成中' : '暂无习题' }}
        </button>
      </div>
    </div>
  </section>
</template>

<script setup>
const formatTime = (time) => {
  if (!time) return ''
  const date = new Date(time)
  return date.toLocaleString('zh-CN')
}

defineProps({
  chapterTitle: {
    type: String,
    default: '',
  },
  knowledgePoints: {
    type: Array,
    default: () => [],
  },
  selectedKpId: {
    type: String,
    default: '',
  },
  overview: {
    type: Object,
    default: null,
  },
  source: {
    type: Object,
    default: null,
  },
  levels: {
    type: Object,
    default: null,
  },
  learningTab: {
    type: String,
    default: 'overview',
  },
  activeLevel: {
    type: String,
    default: 'brief',
  },
  exercisesStatus: {
    type: String,
    default: 'idle',
  },
})

defineEmits(['select-kp', 'tab-change', 'level-change', 'open-exercises'])
</script>

<style scoped>
.learning-panel {
  display: flex;
  flex-direction: column;
  height: 100%;
}

.panel-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding-bottom: 12px;
  border-bottom: 1px solid #edf0f5;
}

.label {
  font-size: 12px;
  color: #6b7280;
}

.title {
  font-size: 18px;
  font-weight: 600;
  color: #1f2937;
}

.kp-tabs {
  margin: 16px 0;
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
}

.kp-chip {
  border: 1px solid #dfe3ef;
  background: #fff;
  padding: 6px 12px;
  border-radius: 999px;
  font-size: 12px;
  cursor: pointer;
  color: #374151;
}

.kp-chip.active {
  border-color: #3772ff;
  color: #1d4ed8;
  background: #edf3ff;
  font-weight: 600;
}

.kp-chip .arrow {
  margin-left: 6px;
}

.content {
  background: #fff;
  border-radius: 16px;
  border: 1px solid #edf0f5;
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  flex: 1;
}

.tabs {
  display: flex;
  gap: 8px;
}

.tabs button {
  border: 1px solid #dfe3ef;
  background: #f9fafb;
  padding: 6px 12px;
  border-radius: 8px;
  font-size: 12px;
  cursor: pointer;
}

.tabs button.active {
  background: #3772ff;
  color: #fff;
  border-color: #3772ff;
}

.panel-body {
  min-height: 200px;
}

.overview h3 {
  margin: 0 0 12px;
  font-size: 18px;
  color: #1f2937;
}

.overview .definition {
  font-size: 15px;
  line-height: 1.7;
  color: #374151;
  margin-bottom: 16px;
  padding: 12px 16px;
  background: #f8fafc;
  border-radius: 8px;
  border-left: 3px solid #3772ff;
}

.overview .section {
  margin-top: 16px;
}

.overview .section-title {
  font-size: 13px;
  font-weight: 600;
  color: #374151;
  margin-bottom: 8px;
}

.overview .point-list,
.overview .pitfall-list {
  margin: 0;
  padding-left: 20px;
}

.overview .point-list li {
  font-size: 14px;
  line-height: 1.6;
  color: #374151;
  margin-bottom: 6px;
}

.overview .pitfall-list li {
  font-size: 14px;
  line-height: 1.6;
  color: #dc2626;
  margin-bottom: 6px;
  padding: 4px 8px;
  background: #fef2f2;
  border-radius: 4px;
  list-style: none;
  position: relative;
  padding-left: 20px;
}

.overview .pitfall-list li::before {
  content: "!";
  position: absolute;
  left: 6px;
  top: 50%;
  transform: translateY(-50%);
  font-size: 12px;
  font-weight: bold;
  color: #dc2626;
}

.meta {
  margin-top: 16px;
  font-size: 11px;
  color: #9ca3af;
}

.source .snippet {
  border: 1px solid #edf0f5;
  background: #f9fafb;
  border-radius: 12px;
  padding: 10px 12px;
  margin-bottom: 12px;
}

.snippet-meta {
  font-size: 11px;
  color: #6b7280;
  margin-bottom: 6px;
}

.snippet-meta .divider {
  margin: 0 6px;
  color: #d1d5db;
}

.source pre {
  white-space: pre-wrap;
  margin: 0;
  font-size: 12px;
  color: #1f2937;
}

.level-tabs {
  display: flex;
  gap: 8px;
  margin-bottom: 8px;
}

.level-tabs button {
  border: 1px solid #dfe3ef;
  background: #fff;
  padding: 6px 10px;
  border-radius: 8px;
  font-size: 12px;
  cursor: pointer;
}

.level-tabs button.active {
  border-color: #3772ff;
  color: #1d4ed8;
  background: #edf3ff;
}

.tags {
  margin-top: 10px;
  display: flex;
  gap: 6px;
  flex-wrap: wrap;
}

.tag {
  padding: 4px 8px;
  background: #eef2ff;
  border-radius: 999px;
  font-size: 11px;
  color: #4f46e5;
}

.exercise-bar {
  display: flex;
  justify-content: flex-end;
}

.exercise-btn {
  border: none;
  background: #10b981;
  color: #fff;
  padding: 8px 14px;
  border-radius: 8px;
  cursor: pointer;
  font-size: 13px;
}

.exercise-btn:disabled {
  background: #d1d5db;
  cursor: not-allowed;
}

.chip {
  padding: 4px 8px;
  border-radius: 999px;
  font-size: 11px;
}

.chip.warning {
  background: #fff7ed;
  color: #b45309;
}

.chip.success {
  background: #ecfdf3;
  color: #047857;
}

.empty-wrapper {
  margin-top: 16px;
}

.empty-text {
  font-size: 12px;
  color: #9ca3af;
}
</style>
