<template>
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
</template>

<script setup>
defineProps({
  source: {
    type: Object,
    default: null,
  },
})
</script>

<style scoped>
.snippet {
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

pre {
  white-space: pre-wrap;
  margin: 0;
  font-size: 12px;
  color: #1f2937;
}

.empty-text {
  font-size: 12px;
  color: #9ca3af;
}
</style>
