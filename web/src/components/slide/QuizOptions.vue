<template>
  <div v-if="exercise && options" class="quiz-options">
    <div
      v-for="(option, index) in options"
      :key="index"
      class="quiz-option"
      :class="{
        selected: selectedOption === option,
        correct: feedback && feedback.correct && selectedOption === option,
        incorrect: feedback && !feedback.correct && selectedOption === option
      }"
      @click="$emit('select', option)"
    >
      {{ option }}
    </div>
  </div>
</template>

<script setup>
const props = defineProps({
  exercise: {
    type: Object,
    default: null,
  },
  selectedOption: {
    type: String,
    default: '',
  },
  feedback: {
    type: Object,
    default: null,
  },
})

defineEmits(['select'])

const options = props.exercise?.options
</script>

<style scoped>
.quiz-options {
  margin-top: 20px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.quiz-option {
  padding: 12px 16px;
  background-color: #f8f9fa;
  border: 2px solid #dee2e6;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s ease;
  font-size: 14px;
}

.quiz-option:hover {
  background-color: #e9ecef;
  border-color: #adb5bd;
}

.quiz-option.selected {
  border-color: #007bff;
  background-color: #e3f2fd;
}

.quiz-option.correct {
  border-color: #28a745;
  background-color: #d4edda;
  color: #155724;
}

.quiz-option.incorrect {
  border-color: #dc3545;
  background-color: #f8d7da;
  color: #721c24;
}
</style>
