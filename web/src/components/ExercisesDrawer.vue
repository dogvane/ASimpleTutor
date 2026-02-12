<template>
  <div v-if="open" class="drawer">
    <div class="overlay" @click="$emit('close')"></div>
    <div class="panel">
      <header>
        <div>
          <h2>习题练习</h2>
          <p>完成后提交以获取反馈</p>
        </div>
        <button type="button" class="close" @click="$emit('close')">✕</button>
      </header>

      <div class="content">
        <div v-if="status === 'generating'" class="hint">习题生成中，请稍后再试</div>
        <div v-else-if="!exercises.length" class="hint">暂无习题</div>
        <div v-else class="list">
          <ExerciseCard
            v-for="exercise in exercises"
            :key="exercise.id"
            :exercise="exercise"
            :selected-answer="answers[exercise.id]"
            :feedback="feedback[exercise.id]"
            @answer-change="$emit('answer-change', $event)"
            @submit-one="$emit('submit-one', $event)"
          />
        </div>
      </div>

      <footer>
        <button type="button" class="primary" @click="$emit('submit-all')">提交全部并获取反馈</button>
      </footer>
    </div>
  </div>
</template>

<script setup>
import ExerciseCard from './exercise/ExerciseCard.vue'

defineProps({
  open: {
    type: Boolean,
    default: false,
  },
  status: {
    type: String,
    default: 'idle',
  },
  exercises: {
    type: Array,
    default: () => [],
  },
  answers: {
    type: Object,
    default: () => ({}),
  },
  feedback: {
    type: Object,
    default: () => ({}),
  },
})

defineEmits(['close', 'answer-change', 'submit-one', 'submit-all'])
</script>

<style scoped>
.drawer {
  position: fixed;
  inset: 0;
  z-index: 40;
  display: flex;
}

.overlay {
  flex: 1;
  background: rgba(15, 23, 42, 0.4);
}

.panel {
  width: min(520px, 92vw);
  background: #fff;
  display: flex;
  flex-direction: column;
  height: 100%;
  padding: 20px;
  gap: 16px;
}

header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

header h2 {
  margin: 0;
  font-size: 18px;
}

header p {
  margin: 4px 0 0;
  font-size: 12px;
  color: #6b7280;
}

.close {
  border: none;
  background: transparent;
  font-size: 18px;
  cursor: pointer;
}

.content {
  flex: 1;
  overflow: auto;
}

.hint {
  font-size: 13px;
  color: #6b7280;
  padding: 12px 0;
}

.list {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

footer {
  border-top: 1px solid #edf0f5;
  padding-top: 12px;
}

.primary {
  width: 100%;
  border: none;
  background: #10b981;
  color: #fff;
  padding: 10px;
  border-radius: 10px;
  cursor: pointer;
  font-size: 13px;
}
</style>
