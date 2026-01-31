<template>
  <div class="chapter-tree">
    <div v-if="searchQuery" class="search-results">
      <div class="result-title">搜索结果</div>
      <ul>
        <li
          v-for="(item, index) in searchResults"
          :key="item.id"
          :class="['result-item', { active: index === searchActiveIndex }]"
          @click="$emit('select-search', item)"
        >
          <div class="title" v-html="highlight(item.title)"></div>
          <div class="meta">{{ item.parentId || '根节点' }}</div>
        </li>
      </ul>
      <div v-if="!searchResults.length" class="empty">未找到匹配章节</div>
    </div>

    <ul v-else class="tree">
      <ChapterNode
        v-for="node in chapters"
        :key="node.id"
        :node="node"
        :expanded-ids="expandedIds"
        :selected-id="selectedId"
        @toggle="$emit('toggle', $event)"
        @select="$emit('select', $event)"
      />
      <li v-if="!chapters.length" class="empty">暂无章节</li>
    </ul>
  </div>
</template>

<script>
import ChapterNode from './ChapterNode.vue'

export default {
  name: 'ChapterTree',
  components: { ChapterNode },
  props: {
    chapters: { type: Array, default: () => [] },
    expandedIds: { type: Array, default: () => [] },
    selectedId: { type: String, default: '' },
    searchQuery: { type: String, default: '' },
    searchResults: { type: Array, default: () => [] },
    searchActiveIndex: { type: Number, default: 0 },
  },
  emits: ['toggle', 'select', 'select-search'],
  setup(props) {
    const escapeRegExp = (value) => value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')

    const highlight = (text) => {
      if (!props.searchQuery) return text
      const reg = new RegExp(`(${escapeRegExp(props.searchQuery)})`, 'gi')
      return text.replace(reg, '<mark>$1</mark>')
    }

    return { highlight }
  },
}
</script>

<style scoped>
.chapter-tree {
  padding: 16px;
  height: 100%;
  overflow: auto;
}

.tree,
.tree ul {
  list-style: none;
  margin: 0;
  padding: 0;
}

.search-results {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.result-title {
  font-size: 12px;
  color: #6b7280;
}

.result-item {
  padding: 8px 10px;
  border-radius: 8px;
  cursor: pointer;
  border: 1px solid transparent;
}

.result-item.active,
.result-item:hover {
  background: #f3f6fb;
  border-color: #d7def0;
}

.result-item .title {
  font-size: 13px;
  color: #1f2937;
}

.result-item .meta {
  font-size: 11px;
  color: #9ca3af;
  margin-top: 2px;
}

.empty {
  font-size: 12px;
  color: #9ca3af;
  padding: 12px 4px;
}

mark {
  background: #fff3bf;
  padding: 0 2px;
  border-radius: 2px;
}
</style>
