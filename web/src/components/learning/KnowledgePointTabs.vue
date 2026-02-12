<template>
  <div class="kp-tabs">
    <button
      v-for="kp in knowledgePoints"
      :key="kp.id"
      type="button"
      :class="['kp-chip', { active: kp.id === selectedKpId }]"
      @click="$emit('select-kp', kp)"
    >
      {{ kp.title }}
      <span v-if="kp.id === selectedKpId" class="arrow">▾</span>
    </button>
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

defineEmits(['select-kp'])
</script>

<style scoped>
.kp-tabs {
  margin: 16px 0;
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
}

.kp-chip {
  border: 1px solid #dfe3ef;
  background: #fff;
  padding: 6px 12px;
  border-radius: 999px;
  font-size: 12px;
  cursor: pointer;
  color: #374151;
}

.kp-chip:hover {
  border-color: #3772ff;
}

.kp-chip.active {
  border-color: #3772ff;
  color: #1d4ed8;
  background: #edf3ff;
  font-weight: 600;
}

.kp-chip .arrow {
  margin-left: 6px;
}
</style>
