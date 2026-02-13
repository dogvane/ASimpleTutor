<template>
  <header class="topbar">
    <div class="left">
      <div class="select">
        <label>书籍</label>
        <select :value="activeBookHubId" @change="onBookChange">
          <option v-for="hub in bookHubs" :key="hub.id" :value="hub.id">
            {{ hub.name }}
          </option>
        </select>
      </div>
      <div class="path">
        <span class="label">当前路径</span>
        <span class="value">{{ currentPath || '未选择' }}</span>
      </div>
    </div>
    <div class="right">
      <div class="settings-btn-wrapper">
        <button class="settings-btn" type="button" @click="$emit('open-settings')" title="设置">
          <span>⚙️</span>
        </button>
      </div>
      <RefreshDropdown
        :scan-status="scanStatus"
        :scan-progress="scanProgress"
        @refresh="$emit('refresh', $event)"
      />
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
import RefreshDropdown from './RefreshDropdown.vue'

defineProps({
  bookHubs: {
    type: Array,
    default: () => [],
  },
  activeBookHubId: {
    type: String,
    default: '',
  },
  scanStatus: {
    type: String,
    default: 'idle',
  },
  scanProgress: {
    type: Object,
    default: null,
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

const emit = defineEmits(['book-change', 'refresh', 'search-input', 'search-keydown', 'open-settings'])

const onBookChange = (event) => {
  emit('book-change', event.target.value)
}
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

.settings-btn-wrapper {
  display: flex;
  align-items: center;
}

.settings-btn {
  border: none;
  background: transparent;
  color: #6b7280;
  padding: 8px;
  border-radius: 8px;
  cursor: pointer;
  font-size: 18px;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s;
}

.settings-btn:hover {
  background: #f3f6fb;
  color: #3772ff;
}

.search input {
  border: 1px solid #d7dbe6;
  padding: 8px 12px;
  border-radius: 10px;
  font-size: 13px;
  min-width: 220px;
}
</style>
