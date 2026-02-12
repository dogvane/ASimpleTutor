<template>
  <div v-if="overview?.overview?.definition" class="definition">{{ overview.overview.definition }}</div>

  <div v-if="overview?.overview?.keyPoints?.length" class="section">
    <div class="section-title">核心要点</div>
    <ul class="point-list">
      <li v-for="(point, index) in overview.overview.keyPoints" :key="index">{{ point }}</li>
    </ul>
  </div>

  <div v-if="overview?.overview?.pitfalls?.length" class="section">
    <div class="section-title">常见误区</div>
    <ul class="pitfall-list">
      <li v-for="(pitfall, index) in overview.overview.pitfalls" :key="index">{{ pitfall }}</li>
    </ul>
  </div>

  <div class="meta" v-if="overview?.generatedAt">生成时间：{{ formatTime(overview.generatedAt) }}</div>
</template>

<script setup>
const formatTime = (time) => {
  if (!time) return ''
  const date = new Date(time)
  return date.toLocaleString('zh-CN')
}

defineProps({
  overview: {
    type: Object,
    default: null,
  },
})
</script>

<style scoped>
.definition {
  font-size: 15px;
  line-height: 1.7;
  color: #374151;
  margin-bottom: 16px;
  padding: 12px 16px;
  background: #f8fafc;
  border-radius: 8px;
  border-left: 3px solid #3772ff;
}

.section {
  margin-top: 16px;
}

.section-title {
  font-size: 13px;
  font-weight: 600;
  color: #374151;
  margin-bottom: 8px;
}

.point-list,
.pitfall-list {
  margin: 0;
  padding-left: 20px;
}

.point-list li {
  font-size: 14px;
  line-height: 1.6;
  color: #374151;
  margin-bottom: 6px;
}

.pitfall-list li {
  font-size: 14px;
  line-height: 1.6;
  color: #dc2626;
  margin-bottom: 6px;
  padding: 4px 8px;
  background: #fef2f2;
  border-radius: 4px;
  list-style: none;
  position: relative;
  padding-left: 20px;
}

.pitfall-list li::before {
  content: "!";
  position: absolute;
  left: 6px;
  top: 50%;
  transform: translateY(-50%);
  font-size: 12px;
  font-weight: bold;
  color: #dc2626;
}

.meta {
  margin-top: 16px;
  font-size: 11px;
  color: #9ca3af;
}
</style>
