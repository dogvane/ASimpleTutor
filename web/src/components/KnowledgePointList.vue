<template>
  <div class="knowledge-points-list">
    <div
      v-for="kp in knowledgePoints"
      :key="kp.id"
      class="knowledge-point-item"
      :class="{ active: selectedKpId === kp.id }"
      @click="$emit('select', kp)"
    >
      {{ kp.title }}
    </div>
  </div>
</template>

<script setup>
defineProps({
  knowledgePoints: {
    type: Array,
    default: () => [],
  },
  selectedKpId: {
    type: String,
    default: '',
  },
})

defineEmits(['select'])
</script>

<style scoped>
.knowledge-points-list {
  margin-top: 20px;
  padding: 0 16px;
}

.knowledge-point-item {
  padding: 8px 12px;
  margin-bottom: 8px;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.2s;
  font-size: 14px;
}

.knowledge-point-item:hover {
  background-color: rgba(0, 123, 255, 0.1);
}

.knowledge-point-item.active {
  background-color: rgba(0, 123, 255, 0.2);
  font-weight: 500;
  border-left: 3px solid #007bff;
}
</style>
