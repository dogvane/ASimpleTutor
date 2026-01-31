<template>
  <div class="loading-overlay" role="status" aria-live="polite">
    <div class="spinner"></div>
    <span class="text">{{ text }}</span>
  </div>
</template>

<script setup>
defineProps({
  text: {
    type: String,
    default: '加载中...'
  }
})
</script>

<style scoped>
.loading-overlay {
  position: absolute;
  inset: 0;
  background: rgba(255, 255, 255, 0.85);
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 12px;
  z-index: 5;
}

.spinner {
  width: 20px;
  height: 20px;
  border-radius: 50%;
  border: 3px solid #d9dde5;
  border-top-color: #3772ff;
  animation: spin 0.9s linear infinite;
}

.text {
  font-size: 14px;
  color: #4b5563;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}
</style>
