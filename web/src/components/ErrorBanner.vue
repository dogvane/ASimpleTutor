<template>
  <div class="error-banner" role="alert">
    <div class="message">
      <strong>出错了：</strong>
      <span>{{ message }}</span>
      <span v-if="code" class="code">({{ code }})</span>
    </div>
    <button v-if="retry" type="button" class="retry" @click="retry">重试</button>
  </div>
</template>

<script setup>
defineProps({
  message: {
    type: String,
    default: '服务暂时不可用，请稍后重试。'
  },
  code: {
    type: String,
    default: ''
  },
  retry: {
    type: Function,
    default: null
  }
})
</script>

<style scoped>
.error-banner {
  background: #fff4f4;
  border: 1px solid #ffd1d1;
  color: #9b1c1c;
  padding: 10px 12px;
  border-radius: 10px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
  font-size: 13px;
}

.code {
  margin-left: 6px;
  font-family: ui-monospace, SFMono-Regular, SFMono, Menlo, monospace;
  font-size: 12px;
}

.retry {
  border: 1px solid #f1a3a3;
  background: #fff;
  color: #9b1c1c;
  padding: 6px 10px;
  border-radius: 6px;
  cursor: pointer;
}
</style>
