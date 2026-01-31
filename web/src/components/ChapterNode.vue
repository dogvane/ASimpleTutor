<template>
  <li>
    <div
      :class="['node', { selected: node.id === selectedId }]"
      @click="select"
    >
      <span v-if="hasChildren" class="toggle" @click.stop="toggle">{{ isExpanded ? '▾' : '▸' }}</span>
      <span v-else class="toggle">•</span>
      <span class="title">{{ node.title }}</span>
    </div>
    <ul v-if="hasChildren && isExpanded" class="children">
      <ChapterNode
        v-for="child in node.children"
        :key="child.id"
        :node="child"
        :expanded-ids="expandedIds"
        :selected-id="selectedId"
        @toggle="$emit('toggle', $event)"
        @select="$emit('select', $event)"
      />
    </ul>
  </li>
</template>

<script>
import { computed } from 'vue'

export default {
  name: 'ChapterNode',
  props: {
    node: { type: Object, required: true },
    expandedIds: { type: Array, default: () => [] },
    selectedId: { type: String, default: '' },
  },
  emits: ['toggle', 'select'],
  setup(props, { emit }) {
    const isExpanded = computed(() => props.expandedIds.includes(props.node.id))
    const hasChildren = computed(() => props.node.children?.length > 0)

    const toggle = () => {
      if (hasChildren.value) {
        emit('toggle', props.node.id)
      }
    }

    const select = () => {
      emit('select', props.node)
    }

    return { isExpanded, hasChildren, toggle, select }
  },
}
</script>

<style scoped>
li {
  list-style: none;
}

.node {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 6px 8px;
  border-radius: 8px;
  cursor: pointer;
  font-size: 13px;
  color: #374151;
}

.node:hover {
  background: #f3f6fb;
}

.node.selected {
  background: #e8f0ff;
  color: #1d4ed8;
  font-weight: 600;
}

.toggle {
  width: 16px;
  text-align: center;
  color: #9ca3af;
}

.children {
  margin-left: 18px;
  border-left: 1px dashed #e5e7eb;
  padding-left: 12px;
}
</style>
