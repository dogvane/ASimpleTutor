<template>
  <header class="topbar">
    <div class="left">
      <div class="select">
        <label>书籍</label>
        <select :value="activeBookRootId" @change="onBookChange">
          <option v-for="root in bookRoots" :key="root.id" :value="root.id">
            {{ root.name }}
          </option>
        </select>
      </div>
      <div class="path">
        <span class="label">当前路径</span>
        <span class="value">{{ currentPath || '未选择' }}</span>
      </div>
    </div>
    <div class="right">
      <div class="refresh-dropdown">
        <button class="refresh-btn" type="button" @click="toggleDropdown" :disabled="scanStatus === 'scanning'">
          <span>{{ refreshLabel }}</span>
          <span class="arrow">{{ dropdownOpen ? '▾' : '▸' }}</span>
        </button>
        <div v-if="dropdownOpen" class="dropdown-menu">
          <div class="dropdown-item" @click="onRefresh('scan')">
            <span class="icon">🔄</span>
            <span>重新扫描</span>
          </div>
          <div class="dropdown-item" @click="onRefresh('exercises')">
            <span class="icon">📝</span>
            <span>刷新习题</span>
          </div>
          <div class="dropdown-item" @click="onRefresh('knowledge')">
            <span class="icon">📚</span>
            <span>刷新知识点</span>
          </div>
          <div class="dropdown-item" @click="onRefresh('graph')">
            <span class="icon">🔗</span>
            <span>刷新系统图谱</span>
          </div>
        </div>
      </div>
      <div class="search">
        <input
          type="text"
          placeholder="搜索章节..."
          :value="searchQuery"
          @input="$emit('search-input', $event.target.value)"
          @keydown="$emit('search-keydown', $event)"
        />
      </div>
    </div>
  </header>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'

defineProps({
  bookRoots: {
    type: Array,
    default: () => [],
  },
  activeBookRootId: {
    type: String,
    default: '',
  },
  scanStatus: {
    type: String,
    default: 'idle',
  },
  currentPath: {
    type: String,
    default: '',
  },
  searchQuery: {
    type: String,
    default: '',
  },
})

const emit = defineEmits(['book-change', 'scan', 'refresh', 'search-input', 'search-keydown'])

const dropdownOpen = ref(false)
const refreshLabel = ref('刷新')

const onBookChange = (event) => {
  emit('book-change', event.target.value)
}

const toggleDropdown = () => {
  dropdownOpen.value = !dropdownOpen.value
}

const onRefresh = (type) => {
  dropdownOpen.value = false
  
  const labels = {
    scan: '重新扫描',
    exercises: '刷新习题',
    knowledge: '刷新知识点',
    graph: '刷新系统图谱'
  }
  
  refreshLabel.value = labels[type] || '刷新'
  emit('refresh', type)
}

const closeDropdown = (event) => {
  if (!event.target.closest('.refresh-dropdown')) {
    dropdownOpen.value = false
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
.topbar {
  height: 64px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 20px;
  border-bottom: 1px solid #edf0f5;
  background: #fff;
  gap: 16px;
}

.left {
  display: flex;
  align-items: center;
  gap: 20px;
}

.select {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
  color: #6b7280;
}

.select select {
  border: 1px solid #d7dbe6;
  padding: 6px 10px;
  border-radius: 8px;
  background: #fff;
  font-size: 13px;
}

.path {
  display: flex;
  flex-direction: column;
  font-size: 12px;
  color: #6b7280;
}

.path .value {
  font-size: 13px;
  color: #1f2937;
  font-weight: 600;
}

.right {
  display: flex;
  align-items: center;
  gap: 12px;
}

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

.scan {
  border: none;
  background: #3772ff;
  color: #fff;
  padding: 8px 14px;
  border-radius: 8px;
  cursor: pointer;
  font-size: 13px;
}

.scan:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.search input {
  border: 1px solid #d7dbe6;
  padding: 8px 12px;
  border-radius: 10px;
  font-size: 13px;
  min-width: 220px;
}
</style>
