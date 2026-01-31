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
          <div v-for="exercise in exercises" :key="exercise.id" class="card">
            <div class="question">
              <span class="type">{{ typeLabel(exercise.type) }}</span>
              <span>{{ exercise.question }}</span>
            </div>
            <div class="answer">
              <template v-if="exercise.type === 'choice'">
                <label v-for="option in exercise.options" :key="option" class="option">
                  <input
                    type="radio"
                    :name="exercise.id"
                    :value="option"
                    :checked="answers[exercise.id] === option"
                    @change="$emit('answer-change', { id: exercise.id, value: option })"
                  />
                  {{ option }}
                </label>
              </template>
              <template v-else>
                <textarea
                  :placeholder="exercise.type === 'fill' ? '请输入答案' : '请输入你的理解'"
                  :value="answers[exercise.id] || ''"
                  @input="$emit('answer-change', { id: exercise.id, value: $event.target.value })"
                ></textarea>
              </template>
            </div>
            <div class="actions">
              <button type="button" class="submit" @click="$emit('submit-one', exercise.id)">提交本题</button>
              <div v-if="feedback[exercise.id]" class="feedback">
                <span :class="['badge', feedback[exercise.id].correct ? 'ok' : 'bad']">
                  {{ feedback[exercise.id].correct ? '正确' : '错误' }}
                </span>
                <div class="explain">{{ feedback[exercise.id].explanation }}</div>
                <div class="ref">参考答案：{{ feedback[exercise.id].referenceAnswer }}</div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <footer>
        <button type="button" class="primary" @click="$emit('submit-all')">提交全部并获取反馈</button>
      </footer>
    </div>
  </div>
</template>

<script setup>
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

const typeLabel = (type) => {
  if (type === 'choice') return '选择题'
  if (type === 'fill') return '填空题'
  if (type === 'short') return '简答题'
  return '题目'
}
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

.card {
  border: 1px solid #edf0f5;
  border-radius: 12px;
  padding: 12px;
  background: #f9fafb;
}

.question {
  font-size: 14px;
  font-weight: 600;
  color: #1f2937;
  display: flex;
  gap: 8px;
}

.type {
  background: #e0e7ff;
  color: #4338ca;
  font-size: 11px;
  padding: 2px 6px;
  border-radius: 6px;
}

.answer {
  margin-top: 8px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.option {
  font-size: 13px;
  color: #374151;
  display: flex;
  gap: 6px;
  align-items: center;
}

textarea {
  width: 100%;
  min-height: 72px;
  resize: vertical;
  border-radius: 8px;
  border: 1px solid #d7dbe6;
  padding: 8px;
  font-size: 13px;
}

.actions {
  margin-top: 10px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.submit {
  border: none;
  background: #3772ff;
  color: #fff;
  padding: 6px 12px;
  border-radius: 8px;
  font-size: 12px;
  align-self: flex-start;
  cursor: pointer;
}

.feedback {
  border-top: 1px dashed #e5e7eb;
  padding-top: 8px;
  font-size: 12px;
  color: #4b5563;
}

.badge {
  display: inline-flex;
  align-items: center;
  padding: 2px 6px;
  border-radius: 999px;
  font-size: 11px;
  margin-bottom: 6px;
}

.badge.ok {
  background: #ecfdf3;
  color: #047857;
}

.badge.bad {
  background: #fff1f2;
  color: #be123c;
}

.ref {
  color: #6b7280;
  margin-top: 4px;
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
