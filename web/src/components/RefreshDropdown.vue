<template>
  <div class="refresh-dropdown">
    <button
      class="refresh-btn"
      type="button"
      @click="toggleDropdown"
      :disabled="scanStatus === 'scanning'"
    >
      <span>{{ label }}</span>
      <span class="arrow">{{ open ? '▾' : '▸' }}</span>
    </button>
    <div v-if="open" class="dropdown-menu">
      <div class="dropdown-item" @click="onItemClick('scan')">
        <span class="icon">🔄</span>
        <span>重新扫描</span>
      </div>
      <div class="dropdown-item" @click="onItemClick('exercises')">
        <span class="icon">📝</span>
        <span>刷新习题</span>
      </div>
      <div class="dropdown-item" @click="onItemClick('knowledge')">
        <span class="icon">📚</span>
        <span>刷新知识点</span>
      </div>
      <div class="dropdown-item" @click="onItemClick('graph')">
        <span class="icon">🔗</span>
        <span>刷新系统图谱</span>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'

const props = defineProps({
  scanStatus: {
    type: String,
    default: 'idle',
  },
})

const emit = defineEmits(['refresh'])

const open = ref(false)
const label = ref('刷新')

const toggleDropdown = () => {
  open.value = !open.value
}

const onItemClick = (type) => {
  open.value = false

  const labels = {
    scan: '重新扫描',
    exercises: '刷新习题',
    knowledge: '刷新知识点',
    graph: '刷新系统图谱'
  }

  label.value = labels[type] || '刷新'
  emit('refresh', type)
}

const closeDropdown = (event) => {
  if (!event.target.closest('.refresh-dropdown')) {
    open.value = false
  }
}

onMounted(() => {
  document.addEventListener('click', closeDropdown)
})

onUnmounted(() => {
  document.removeEventListener('click', closeDropdown)
})
</script>

<style scoped>
.refresh-dropdown {
  position: relative;
}

.refresh-btn {
  border: none;
  background: #3772ff;
  color: #fff;
  padding: 8px 14px;
  border-radius: 8px;
  cursor: pointer;
  font-size: 13px;
  display: flex;
  align-items: center;
  gap: 6px;
}

.refresh-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.refresh-btn .arrow {
  font-size: 10px;
}

.dropdown-menu {
  position: absolute;
  top: 100%;
  right: 0;
  margin-top: 8px;
  background: #fff;
  border: 1px solid #edf0f5;
  border-radius: 10px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  min-width: 160px;
  z-index: 100;
  overflow: hidden;
}

.dropdown-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 14px;
  cursor: pointer;
  font-size: 13px;
  color: #374151;
  transition: background 0.2s;
}

.dropdown-item:hover {
  background: #f3f6fb;
}

.dropdown-item .icon {
  font-size: 16px;
}
</style>
