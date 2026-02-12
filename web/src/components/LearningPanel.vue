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

    <KnowledgePointTabs
      v-if="knowledgePoints.length"
      :knowledge-points="knowledgePoints"
      :selected-kp-id="selectedKpId"
      @select-kp="$emit('select-kp', $event)"
    />

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
        <h3 v-if="learningTab === 'overview'">{{ overview?.title || '概览' }}</h3>
        <OverviewPanel v-if="learningTab === 'overview'" :overview="overview" />
        <SourcePanel v-else-if="learningTab === 'source'" :source="source" />
        <LevelsPanel
          v-else-if="learningTab === 'levels'"
          :levels="levels"
          :active-level="activeLevel"
          @level-change="$emit('level-change', $event)"
        />
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
import KnowledgePointTabs from './learning/KnowledgePointTabs.vue'
import OverviewPanel from './learning/OverviewPanel.vue'
import SourcePanel from './learning/SourcePanel.vue'
import LevelsPanel from './learning/LevelsPanel.vue'

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

.panel-body h3 {
  margin: 0 0 12px;
  font-size: 18px;
  color: #1f2937;
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
</style>
