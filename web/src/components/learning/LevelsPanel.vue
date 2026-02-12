<template>
  <div class="level-tabs">
    <button :class="{ active: activeLevel === 'brief' }" @click="$emit('level-change', 'brief')">简要</button>
    <button :class="{ active: activeLevel === 'detailed' }" @click="$emit('level-change', 'detailed')">详细</button>
    <button :class="{ active: activeLevel === 'deep' }" @click="$emit('level-change', 'deep')">深入</button>
  </div>
  <div class="level-content">
    <p>{{ levels?.levels?.[activeLevel]?.content || '暂无内容' }}</p>
    <div v-if="levels?.levels?.[activeLevel]?.keyPoints?.length" class="tags">
      <span v-for="point in levels.levels[activeLevel].keyPoints" :key="point" class="tag">{{ point }}</span>
    </div>
  </div>
</template>

<script setup>
defineProps({
  levels: {
    type: Object,
    default: null,
  },
  activeLevel: {
    type: String,
    default: 'brief',
  },
})

defineEmits(['level-change'])
</script>

<style scoped>
.level-tabs {
  display: flex;
  gap: 8px;
  margin-bottom: 8px;
}

.level-tabs button {
  border: 1px solid #dfe3ef;
  background: #fff;
  padding: 6px 10px;
  border-radius: 8px;
  font-size: 12px;
  cursor: pointer;
}

.level-tabs button.active {
  border-color: #3772ff;
  color: #1d4ed8;
  background: #edf3ff;
}

.level-content {
  flex: 1;
}

.level-content p {
  margin: 0 0 10px 0;
  font-size: 14px;
  line-height: 1.6;
  color: #374151;
}

.tags {
  margin-top: 10px;
  display: flex;
  gap: 6px;
  flex-wrap: wrap;
}

.tag {
  padding: 4px 8px;
  background: #eef2ff;
  border-radius: 999px;
  font-size: 11px;
  color: #4f46e5;
}
</style>
