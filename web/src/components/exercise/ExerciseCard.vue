<template>
  <div class="card">
    <div class="question">
      <span class="type">{{ typeLabel(exercise.type) }}</span>
      <span>{{ exercise.question }}</span>
    </div>
    <div class="answer">
      <label v-for="option in exercise.options" :key="option" class="option">
        <input
          type="radio"
          :name="exercise.id"
          :value="option"
          :checked="selectedAnswer === option"
          @change="$emit('answer-change', { id: exercise.id, value: option })"
        />
        {{ option }}
      </label>
    </div>
    <div class="actions">
      <button type="button" class="submit" @click="$emit('submit-one', exercise.id)">提交本题</button>
      <div v-if="feedback" class="feedback">
        <span :class="['badge', feedback.correct ? 'ok' : 'bad']">
          {{ feedback.correct ? '正确' : '错误' }}
        </span>
        <div class="explain">{{ feedback.explanation }}</div>
        <div class="ref">参考答案：{{ feedback.referenceAnswer }}</div>
      </div>
    </div>
  </div>
</template>

<script setup>
defineProps({
  exercise: {
    type: Object,
    required: true,
  },
  selectedAnswer: {
    type: String,
    default: '',
  },
  feedback: {
    type: Object,
    default: null,
  },
})

defineEmits(['answer-change', 'submit-one'])

const typeLabel = (type) => {
  if (type === 'SingleChoice') return '单选题'
  if (type === 'TrueFalse') return '判断题'
  return '题目'
}
</script>

<style scoped>
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
  margin-bottom: 8px;
}

.type {
  background: #e0e7ff;
  color: #4338ca;
  font-size: 11px;
  padding: 2px 6px;
  border-radius: 6px;
}

.answer {
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

.explain {
  margin-bottom: 4px;
}

.ref {
  color: #6b7280;
  margin-top: 4px;
}
</style>
