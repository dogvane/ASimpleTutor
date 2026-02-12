<template>
  <div v-if="feedback" class="quiz-feedback">
    <div class="feedback-header" :class="feedback.correct ? 'correct' : 'incorrect'">
      {{ feedback.correct ? '回答正确 ✅' : '回答错误 ❌' }}
    </div>
    <div class="feedback-content">{{ feedback.explanation }}</div>
    <div v-if="!feedback.correct" class="feedback-reference">
      正确答案：{{ feedback.referenceAnswer }}
    </div>
  </div>
</template>

<script setup>
defineProps({
  feedback: {
    type: Object,
    default: null,
  },
})
</script>

<style scoped>
.quiz-feedback {
  margin-top: 15px;
  padding: 15px;
  border-radius: 8px;
  background-color: #f8f9fa;
  border: 1px solid #dee2e6;
}

.feedback-header {
  font-size: 16px;
  font-weight: 600;
  margin-bottom: 8px;
}

.feedback-header.correct {
  color: #28a745;
}

.feedback-header.incorrect {
  color: #dc3545;
}

.feedback-content {
  font-size: 14px;
  line-height: 1.5;
  margin-bottom: 10px;
  color: #495057;
}

.feedback-reference {
  font-size: 14px;
  color: #6c757d;
  font-style: italic;
}
</style>
