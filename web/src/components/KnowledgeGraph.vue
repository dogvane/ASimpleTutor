<template>
  <div class="knowledge-graph-container">
    <div v-if="loading" class="loading-container">
      <div class="loading-spinner"></div>
      <div class="loading-text">正在加载知识图谱...</div>
    </div>
    <div v-else-if="!knowledgeGraph || knowledgeGraph.nodes.length === 0" class="empty-container">
      <div class="empty-icon">🔗</div>
      <div class="empty-text">暂无知识图谱数据</div>
      <div class="empty-description">请先扫描文档或刷新知识点</div>
    </div>
    <div v-else class="graph-content">
      <div class="graph-controls">
        <div class="search-box">
          <input
            type="text"
            placeholder="搜索节点..."
            :value="searchQuery"
            @input="handleSearch"
            class="search-input"
          />
        </div>
        <div class="filter-options">
          <select v-model="selectedType" @change="handleFilter" class="type-filter">
            <option value="">所有类型</option>
            <option value="Concept">概念</option>
            <option value="Chapter">章节</option>
            <option value="Process">流程</option>
            <option value="Api">API</option>
            <option value="BestPractice">最佳实践</option>
          </select>
        </div>
        <div class="stats">
          <span>节点数: {{ filteredNodes.length }}</span>
          <span>边数: {{ filteredEdges.length }}</span>
        </div>
      </div>
      <div class="graph-canvas">
        <svg ref="graphSvg" class="graph-svg" viewBox="0 0 1200 800">
          <!-- 绘制边 -->
          <g class="edges">
            <line
              v-for="edge in filteredEdges"
              :key="edge.edgeId"
              :x1="getPosition(edge.sourceNodeId).x"
              :y1="getPosition(edge.sourceNodeId).y"
              :x2="getPosition(edge.targetNodeId).x"
              :y2="getPosition(edge.targetNodeId).y"
              :class="`edge edge-${edge.type.toLowerCase()}`"
              :stroke-width="edge.weight * 2"
            >
              <title>{{ edge.description || edge.type }}</title>
            </line>
          </g>
          <!-- 绘制节点 -->
          <g class="nodes">
            <g
              v-for="node in filteredNodes"
              :key="node.nodeId"
              :transform="`translate(${getPosition(node.nodeId).x}, ${getPosition(node.nodeId).y})`"
              :class="`node node-${node.type.toLowerCase()}`"
              @click="handleNodeClick(node)"
            >
              <circle
                :r="node.metadata?.size || 20"
                :fill="node.metadata?.color || '#667eea'"
                :opacity="selectedNodeId === node.nodeId ? 0.8 : 0.6"
              />
              <text
                class="node-label"
                :fill="node.metadata?.color || '#667eea'"
                :font-size="node.metadata?.size || 20"
                text-anchor="middle"
                dy="5"
              >
                {{ node.title }}
              </text>
              <title>{{ node.title }}</title>
            </g>
          </g>
        </svg>
      </div>
      <div v-if="selectedNode" class="node-details">
        <div class="node-header">
          <div class="node-title">{{ selectedNode.title }}</div>
          <div class="node-type">{{ selectedNode.type }}</div>
        </div>
        <div class="node-info">
          <div class="info-item">
            <span class="label">重要性:</span>
            <span class="value">{{ (selectedNode.importance * 100).toFixed(0) }}%</span>
          </div>
          <div class="info-item">
            <span class="label">章节路径:</span>
            <span class="value">{{ selectedNode.chapterPath.join(' > ') }}</span>
          </div>
          <div class="info-item" v-if="selectedNode.knowledgePoint?.summary">
            <span class="label">摘要:</span>
            <span class="value">{{ selectedNode.knowledgePoint.summary }}</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'

defineProps({
  knowledgeGraph: {
    type: Object,
    default: null,
  },
  loading: {
    type: Boolean,
    default: false,
  },
})

const searchQuery = ref('')
const selectedType = ref('')
const selectedNodeId = ref(null)
const graphSvg = ref(null)

// 计算过滤后的节点和边
const filteredNodes = computed(() => {
  if (!knowledgeGraph) return []

  return knowledgeGraph.nodes.filter(node => {
    const matchesSearch = node.title.toLowerCase().includes(searchQuery.value.toLowerCase()) ||
                         node.chapterPath.some(path => path.toLowerCase().includes(searchQuery.value.toLowerCase()))
    const matchesType = !selectedType.value || node.type === selectedType.value

    return matchesSearch && matchesType
  })
})

const filteredEdges = computed(() => {
  if (!knowledgeGraph) return []

  const nodeIds = filteredNodes.value.map(node => node.nodeId)
  return knowledgeGraph.edges.filter(edge =>
    nodeIds.includes(edge.sourceNodeId) && nodeIds.includes(edge.targetNodeId)
  )
})

// 获取节点位置
const getPosition = (nodeId) => {
  const node = knowledgeGraph?.nodes.find(n => n.nodeId === nodeId)
  if (node && node.metadata?.position) {
    return {
      x: node.metadata.position.x + 600,
      y: node.metadata.position.y + 400,
    }
  }
  return { x: 600, y: 400 }
}

// 搜索处理
const handleSearch = (event) => {
  searchQuery.value = event.target.value
}

// 过滤处理
const handleFilter = () => {
  // 过滤逻辑已在 computed 属性中实现
}

// 节点点击处理
const handleNodeClick = (node) => {
  selectedNodeId.value = node.nodeId
}

// 获取选中的节点
const selectedNode = computed(() => {
  return filteredNodes.value.find(node => node.nodeId === selectedNodeId.value)
})
</script>

<style scoped>
.knowledge-graph-container {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  background: #f8fafc;
  border-radius: 8px;
  overflow: hidden;
}

.loading-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 300px;
}

.loading-spinner {
  width: 40px;
  height: 40px;
  border: 4px solid rgba(0, 0, 0, 0.1);
  border-top-color: #3772ff;
  border-radius: 50%;
  animation: spin 1s linear infinite;
  margin-bottom: 12px;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.loading-text {
  color: #64748b;
  font-size: 14px;
}

.empty-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 300px;
}

.empty-icon {
  font-size: 48px;
  color: #94a3b8;
  margin-bottom: 12px;
}

.empty-text {
  color: #64748b;
  font-size: 16px;
  margin-bottom: 8px;
}

.empty-description {
  color: #94a3b8;
  font-size: 14px;
}

.graph-content {
  display: flex;
  flex-direction: column;
  height: 100%;
}

.graph-controls {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px;
  background: white;
  border-bottom: 1px solid #e2e8f0;
}

.search-box {
  flex: 1;
}

.search-input {
  width: 100%;
  padding: 8px 12px;
  border: 1px solid #e2e8f0;
  border-radius: 6px;
  font-size: 14px;
  outline: none;
  transition: border-color 0.2s;
}

.search-input:focus {
  border-color: #3772ff;
}

.filter-options {
  display: flex;
  align-items: center;
}

.type-filter {
  padding: 8px 12px;
  border: 1px solid #e2e8f0;
  border-radius: 6px;
  font-size: 14px;
  outline: none;
  background: white;
}

.stats {
  display: flex;
  gap: 16px;
  color: #64748b;
  font-size: 12px;
}

.graph-canvas {
  flex: 1;
  overflow: hidden;
  background: #f8fafc;
}

.graph-svg {
  width: 100%;
  height: 100%;
  cursor: default;
}

.edges {
  stroke: #cbd5e1;
  stroke-width: 1;
}

.edge {
  stroke: #cbd5e1;
  fill: none;
  transition: stroke 0.2s;
}

.edge:hover {
  stroke: #3772ff;
  stroke-width: 2;
}

.edge-related {
  stroke: #cbd5e1;
}

.edge-contains {
  stroke: #f56565;
}

.edge-depends-on {
  stroke: #48bb78;
}

.nodes {
  cursor: pointer;
}

.node {
  transition: transform 0.2s;
}

.node:hover {
  transform: scale(1.1);
}

.node-label {
  font-size: 12px;
  fill: white;
  pointer-events: none;
}

.node-details {
  padding: 16px;
  background: white;
  border-top: 1px solid #e2e8f0;
  max-height: 200px;
  overflow-y: auto;
}

.node-header {
  margin-bottom: 12px;
}

.node-title {
  font-size: 18px;
  font-weight: 600;
  color: #1e293b;
  margin-bottom: 4px;
}

.node-type {
  font-size: 14px;
  color: #64748b;
}

.node-info {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.info-item {
  display: flex;
  gap: 8px;
}

.label {
  font-size: 14px;
  color: #64748b;
  min-width: 80px;
}

.value {
  font-size: 14px;
  color: #1e293b;
  flex: 1;
}
</style>
