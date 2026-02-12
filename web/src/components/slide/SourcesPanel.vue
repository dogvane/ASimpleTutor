<template>
  <div v-if="sources && sources.length > 0" class="sources-container">
    <h3>原文来源</h3>
    <div
      v-for="(source, index) in sources"
      :key="index"
      class="source-item"
    >
      <div class="source-file">{{ source.fileName }}</div>
      <div class="source-content">{{ source.content }}</div>
    </div>
  </div>
</template>

<script setup>
defineProps({
  sources: {
    type: Array,
    default: () => [],
  },
})
</script>

<style scoped>
.sources-container {
  margin-top: 30px;
  padding-top: 20px;
  border-top: 1px solid #e9ecef;
}

.sources-container h3 {
  margin: 0 0 15px 0;
  font-size: 16px;
  font-weight: 600;
  color: #495057;
}

.source-item {
  margin-bottom: 20px;
  padding: 15px;
  background-color: #f8f9fa;
  border-radius: 8px;
}

.source-file {
  font-weight: 600;
  margin-bottom: 10px;
  color: #495057;
}

.source-content {
  font-size: 14px;
  line-height: 1.5;
  color: #6c757d;
}
</style>
