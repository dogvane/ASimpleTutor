import { buildKnowledgeGraph } from '../api'

/**
 * 知识图谱加载和管理逻辑
 */
export function useKnowledgeGraphLoader(state) {
  const { activeBookHubId, knowledgeGraph, knowledgeGraphLoading, setError } = state

  /**
   * 构建知识图谱
   */
  const buildGraph = async (options = {}) => {
    if (!activeBookHubId.value) {
      setError({ message: '请先选择书籍中心', code: 'BOOKHUB_NOT_FOUND' })
      return null
    }

    knowledgeGraphLoading.value = true

    try {
      const graph = await buildKnowledgeGraph(activeBookHubId.value, options)
      knowledgeGraph.value = graph
      return graph
    } catch (error) {
      console.error('构建知识图谱失败:', error)
      setError({
        message: '构建知识图谱失败，请检查网络连接或稍后重试',
        code: 'KNOWLEDGE_GRAPH_BUILD_FAILED',
      })
      return null
    } finally {
      knowledgeGraphLoading.value = false
    }
  }

  /**
   * 刷新知识图谱
   */
  const refreshKnowledgeGraph = async (options = {}) => {
    return await buildGraph(options)
  }

  /**
   * 重置知识图谱状态
   */
  const resetKnowledgeGraph = () => {
    knowledgeGraph.value = null
  }

  return {
    buildGraph,
    refreshKnowledgeGraph,
    resetKnowledgeGraph,
  }
}
