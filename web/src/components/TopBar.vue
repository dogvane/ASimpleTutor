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
      <button class="scan" type="button" @click="$emit('scan')" :disabled="scanStatus === 'scanning'">
        {{ scanStatus === 'scanning' ? '扫描中...' : '重新扫描' }}
      </button>
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

const emit = defineEmits(['book-change', 'scan', 'search-input', 'search-keydown'])

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
