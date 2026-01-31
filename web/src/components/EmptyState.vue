<template>
  <div class="empty-state">
    <div class="title">{{ title }}</div>
    <div class="desc">{{ description }}</div>
    <button v-if="action" type="button" class="action" @click="action">{{ actionText }}</button>
  </div>
</template>

<script setup>
defineProps({
  title: {
    type: String,
    default: '暂无数据'
  },
  description: {
    type: String,
    default: '请稍后重试或切换条件。'
  },
  action: {
    type: Function,
    default: null
  },
  actionText: {
    type: String,
    default: '重试'
  }
})
</script>

<style scoped>
.empty-state {
  padding: 32px 20px;
  border-radius: 16px;
  border: 1px dashed #e5e7eb;
  background: #fafafa;
  text-align: center;
}

.title {
  font-size: 16px;
  font-weight: 600;
  color: #374151;
  margin-bottom: 6px;
}

.desc {
  font-size: 13px;
  color: #6b7280;
  margin-bottom: 12px;
}

.action {
  border: none;
  background: #3772ff;
  color: #fff;
  padding: 8px 14px;
  border-radius: 8px;
  cursor: pointer;
}
</style>
