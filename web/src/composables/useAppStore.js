import { ref, computed } from 'vue'

/**
 * 全局应用状态管理
 */
export function useAppStore() {
  // 书籍中心相关
  const bookHubs = ref([])
  const activeBookHubId = ref('')

  // 扫描状态
  const scanStatus = ref('idle')
  const scanMessage = ref('')
  const scanProgress = ref(null)

  // 章节相关
  const chapters = ref([])
  const chaptersLoading = ref(false)
  const expandedIds = ref([])
  const selectedChapter = ref(null)

  // 搜索相关
  const searchQuery = ref('')
  const searchResults = ref([])
  const searchActiveIndex = ref(0)

  // 知识点相关
  const knowledgePoints = ref([])
  const selectedKp = ref(null)

  // 习题相关
  const exercisesStatus = ref('idle')
  const exercisesDrawerOpen = ref(false)
  const exercises = ref([])
  const exercisesAnswers = ref({})
  const exercisesFeedback = ref({})

  // 习题幻灯片相关
  const quizAnswers = ref({})
  const quizFeedback = ref({})

  // 幻灯片相关
  const slides = ref([])
  const currentSlideIndex = ref(0)
  const audioAvailable = ref(false)

  // 加载和错误状态
  const globalError = ref(null)
  const globalLoading = ref(false)
  const learningLoading = ref(false)

  // 知识图谱相关
  const knowledgeGraph = ref(null)
  const knowledgeGraphLoading = ref(false)

  // 设置对话框
  const settingsDialogOpen = ref(false)

  // 计算属性
  const currentPath = computed(() => {
    if (selectedChapter.value && selectedKp.value) {
      return `${selectedChapter.value.title} > ${selectedKp.value.title}`
    }
    return selectedChapter.value?.title || ''
  })

  const hasNextChapter = computed(() => {
    if (!selectedChapter.value) return false
    const currentIndex = chapters.value.findIndex(ch => ch.id === selectedChapter.value.id)
    return currentIndex !== -1 && currentIndex < chapters.value.length - 1
  })

  // 重置学习状态
  const resetLearningState = () => {
    knowledgePoints.value = []
    selectedKp.value = null
    slides.value = []
    currentSlideIndex.value = 0
    exercisesStatus.value = 'idle'
    exercises.value = []
    exercisesAnswers.value = {}
    exercisesFeedback.value = {}
    quizAnswers.value = {}
    quizFeedback.value = {}
  }

  // 设置错误
  const setError = (error) => {
    if (!error) {
      globalError.value = null
      return
    }

    globalError.value = {
      message: error.message || '服务暂时不可用，请检查网络。',
      code: error.code || '',
    }
  }

  return {
    // 状态
    bookHubs,
    activeBookHubId,
    scanStatus,
    scanMessage,
    scanProgress,
    chapters,
    chaptersLoading,
    expandedIds,
    selectedChapter,
    searchQuery,
    searchResults,
    searchActiveIndex,
    knowledgePoints,
    selectedKp,
    exercisesStatus,
    exercisesDrawerOpen,
    exercises,
    exercisesAnswers,
    exercisesFeedback,
    quizAnswers,
    quizFeedback,
    slides,
    currentSlideIndex,
    audioAvailable,
    globalError,
    globalLoading,
    learningLoading,
    knowledgeGraph,
    knowledgeGraphLoading,
    settingsDialogOpen,

    // 计算属性
    currentPath,
    hasNextChapter,

    // 方法
    resetLearningState,
    setError,
  }
}
