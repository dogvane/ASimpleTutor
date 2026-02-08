<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import {
  activateBookRoot,
  getBookRoots,
  getChapters,
  getExercises,
  getExercisesStatus,
  getKnowledgePoints,
  getOverview,
  getSourceContent,
  getDetailedContent,
  scanBooks,
  searchChapters,
  submitExercise,
  submitFeedback,
} from './api'
import ChapterTree from './components/ChapterTree.vue'
import EmptyState from './components/EmptyState.vue'
import ErrorBanner from './components/ErrorBanner.vue'
import ExercisesDrawer from './components/ExercisesDrawer.vue'
import LoadingOverlay from './components/LoadingOverlay.vue'
import SlideViewer from './components/SlideViewer.vue'
import TopBar from './components/TopBar.vue'

const bookRoots = ref([])
const activeBookRootId = ref('')
const scanStatus = ref('idle')
const scanMessage = ref('')
const chapters = ref([])
const chaptersLoading = ref(false)
const expandedIds = ref([])
const selectedChapter = ref(null)
const searchQuery = ref('')
const searchResults = ref([])
const searchActiveIndex = ref(0)
const knowledgePoints = ref([])
const selectedKp = ref(null)
const exercisesStatus = ref('idle')
const exercisesDrawerOpen = ref(false)
const exercises = ref([])
const exercisesAnswers = ref({})
const exercisesFeedback = ref({})

// 习题幻灯片相关状态
const quizAnswers = ref({})
const quizFeedback = ref({})
const globalError = ref(null)
const globalLoading = ref(false)
const learningLoading = ref(false)

// 幻灯片相关状态
const slides = ref([])
const currentSlideIndex = ref(0)
const audioAvailable = ref(false)

// 检查音频是否可用
const checkAudioAvailability = async (kpId) => {
  // 预留音频检查逻辑
  // 实际实现时，这里应该检查是否存在对应的音频文件
  // 目前返回false，因为这是预留功能
  return false
}

const currentPath = computed(() => {
  if (selectedChapter.value && selectedKp.value) {
    return `${selectedChapter.value.title} > ${selectedKp.value.title}`
  }
  return selectedChapter.value?.title || ''
})

const currentSlideTitle = computed(() => {
  if (selectedKp.value) {
    return selectedKp.value.title
  }
  return selectedChapter.value?.title || ''
})

const hasNextChapter = computed(() => {
  if (!selectedChapter.value) return false
  const currentIndex = chapters.value.findIndex(ch => ch.id === selectedChapter.value.id)
  return currentIndex !== -1 && currentIndex < chapters.value.length - 1
})

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

const resetLearningState = () => {
  knowledgePoints.value = []
  selectedKp.value = null
  slides.value = []
  currentSlideIndex.value = 0
  exercisesStatus.value = 'idle'
  exercises.value = []
  exercisesAnswers.value = {}
  exercisesFeedback.value = {}
  
  // 重置习题幻灯片相关状态
  quizAnswers.value = {}
  quizFeedback.value = {}
}

const loadBookRoots = async () => {
  globalLoading.value = true
  setError(null)
  try {
    const data = await getBookRoots()
    bookRoots.value = data.items || []
    const nextActiveId = data.activeId || bookRoots.value[0]?.id || ''
    activeBookRootId.value = nextActiveId
    if (nextActiveId) {
      if (!data.activeId) {
        await activateBookRoot(nextActiveId)
      } else {
        await loadChapters()
      }
    } else {
      setError({ message: '未配置书籍目录，请先配置并激活。', code: 'BOOKROOT_NOT_FOUND' })
    }
  } catch (error) {
    setError(error)
  } finally {
    globalLoading.value = false
  }
}

const scanAndLoad = async () => {
  if (!activeBookRootId.value) {
    setError({ message: '请先激活书籍目录', code: 'BOOKROOT_NOT_FOUND' })
    return
  }
  scanStatus.value = 'scanning'
  scanMessage.value = ''
  chapters.value = []
  expandedIds.value = []
  selectedChapter.value = null
  resetLearningState()
  setError(null)
  
  // 不阻塞全局状态，允许用户进行其他操作
  try {
    // 发送扫描请求
    await scanBooks()
    
    // 扫描请求发送成功后，立即开始加载章节
    // 这样可以在服务端处理学习内容生成的同时，前端显示章节列表
    await loadChapters()
    scanStatus.value = 'ready'
  } catch (error) {
    scanStatus.value = 'failed'
    scanMessage.value = error.message
    setError(error)
  }
}

const loadChapters = async () => {
  chaptersLoading.value = true
  setError(null)
  try {
    const data = await getChapters()
    const normalizeNode = (node) => ({
      id: node.id ?? node.Id,
      title: node.title ?? node.Title,
      level: node.level ?? node.Level,
      expanded: node.expanded ?? node.Expanded,
      children: ((node.children ?? node.Children) || []).map(normalizeNode),
    })
    const rawItems = Array.isArray(data)
      ? data
      : Array.isArray(data?.items)
        ? data.items
        : Array.isArray(data?.Items)
          ? data.Items
          : data?.id || data?.Id
            ? [data]
            : []
    chapters.value = rawItems.map(normalizeNode)
    expandedIds.value = chapters.value
      .filter((item) => item.expanded)
      .map((item) => item.id)
    if (chapters.value.length) {
      selectChapter(chapters.value[0])
    }
  } catch (error) {
    setError(error)
  } finally {
    chaptersLoading.value = false
  }
}

const changeBookRoot = async (bookRootId) => {
  if (!bookRootId || bookRootId === activeBookRootId.value) return
  globalLoading.value = true
  setError(null)
  try {
    await activateBookRoot(bookRootId)
    activeBookRootId.value = bookRootId
    await scanAndLoad()
  } catch (error) {
    setError(error)
  } finally {
    globalLoading.value = false
  }
}

const toggleExpand = (id) => {
  if (expandedIds.value.includes(id)) {
    expandedIds.value = expandedIds.value.filter((item) => item !== id)
  } else {
    expandedIds.value = [...expandedIds.value, id]
  }
}

const selectChapter = async (chapter) => {
  if (!chapter || selectedChapter.value?.id === chapter.id) return
  selectedChapter.value = chapter
  resetLearningState()
  await loadKnowledgePoints(chapter.id)
  
  // 实现章节串联播放功能
  if (knowledgePoints.value.length > 0) {
    // 加载第一个知识点的幻灯片
    await selectKp(knowledgePoints.value[0])
  }
}

const loadKnowledgePoints = async (chapterId) => {
  learningLoading.value = true
  setError(null)
  try {
    const data = await getKnowledgePoints(chapterId)
    knowledgePoints.value = (data.items || []).map((item) => ({
      id: item.id ?? item.kpId ?? item.KpId,
      title: item.title ?? item.Title,
      summary: item.summary ?? item.Summary,
    }))
    if (knowledgePoints.value.length) {
      selectKp(knowledgePoints.value[0])
    }
  } catch (error) {
    setError(error)
  } finally {
    learningLoading.value = false
  }
}

const selectKp = async (kp) => {
  if (!kp || selectedKp.value?.id === kp.id) return
  selectedKp.value = kp
  slides.value = []
  currentSlideIndex.value = 0
  exercisesStatus.value = 'idle'
  await loadSlides(kp.id)
  await loadExercisesStatus(kp.id)
  // 检查音频是否可用
  audioAvailable.value = await checkAudioAvailability(kp.id)
}

const loadSlides = async (kpId) => {
  learningLoading.value = true
  setError(null)
  try {
    // 加载知识点概览作为幻灯片内容
    const overviewData = await getOverview(kpId)
    if (overviewData) {
      // 构建幻灯片
      const slideItems = []
      
      // 处理overview内容，构建完整的概览幻灯片
      let overviewContent = ''
      if (typeof overviewData.overview === 'object') {
        // 如果是对象，提取完整内容
        overviewContent = `<div class="overview-content">`
        
        // 添加定义
        if (overviewData.overview.definition) {
          overviewContent += `<div class="definition-section">
            <h2>定义</h2>
            <p>${overviewData.overview.definition}</p>
          </div>`
        }
        
        // 添加关键点
        if (overviewData.overview.keyPoints && overviewData.overview.keyPoints.length > 0) {
          overviewContent += `<div class="keypoints-section">
            <h2>关键点</h2>
            <ul>
              ${overviewData.overview.keyPoints.map(point => `<li>${point}</li>`).join('')}
            </ul>
          </div>`
        }
        
        // 添加注意事项
        if (overviewData.overview.pitfalls && overviewData.overview.pitfalls.length > 0) {
          overviewContent += `<div class="pitfalls-section">
            <h2>注意事项</h2>
            <ul>
              ${overviewData.overview.pitfalls.map(pitfall => `<li>${pitfall}</li>`).join('')}
            </ul>
          </div>`
        }
        
        overviewContent += `</div>`
      } else if (typeof overviewData.overview === 'string') {
        // 如果是字符串，直接使用
        overviewContent = `<p>${overviewData.overview}</p>`
      }
      
      // 添加概览幻灯片
      slideItems.push({
        id: `slide_${kpId}_1`,
        content: `<h1>${overviewData.title}</h1>${overviewContent}`,
        type: 'content',
        sources: [] // 预留原文来源信息
      })
      
      // 加载详细内容，获取不同层级的学习内容
      try {
        const detailedContent = await getDetailedContent(kpId, 'detailed')
        if (detailedContent?.levels) {
          // 添加详细内容幻灯片
          if (detailedContent.levels.brief?.content) {
            slideItems.push({
              id: `slide_${kpId}_2`,
              content: `<h2>概览</h2><p>${detailedContent.levels.brief.content}</p>`,
              type: 'content',
              sources: []
            })
          }
          
          if (detailedContent.levels.detailed?.content) {
            slideItems.push({
              id: `slide_${kpId}_3`,
              content: `<h2>详细内容</h2><p>${detailedContent.levels.detailed.content}</p>`,
              type: 'content',
              sources: []
            })
          }
          
          if (detailedContent.levels.deep?.content) {
            slideItems.push({
              id: `slide_${kpId}_4`,
              content: `<h2>深度解析</h2><p>${detailedContent.levels.deep.content}</p>`,
              type: 'content',
              sources: []
            })
          }
        }
      } catch (error) {
        // 详细内容加载失败不影响主流程
        console.error('加载详细内容失败:', error)
      }
      
      // 加载原文内容作为幻灯片
      try {
        const sourceData = await getSourceContent(kpId)
        if (sourceData?.sourceItems?.length > 0) {
          const sourceContent = sourceData.sourceItems.map(item => `
            <div class="source-item">
              <h3>${item.fileName}</h3>
              <p>${item.headingPath}</p>
              <div class="source-text">${item.content}</div>
            </div>
          `).join('')
          
          slideItems.push({
            id: `slide_${kpId}_source`,
            content: `<h2>原文来源</h2>${sourceContent}`,
            type: 'source',
            sources: sourceData.sourceItems.map(item => ({
              text: item.content.substring(0, 50) + '...',
              ...item
            }))
          })
        }
      } catch (error) {
        // 原文加载失败不影响主流程
        console.error('加载原文失败:', error)
      }
      
      // 检查是否有习题，如果有，为每道习题创建单独的幻灯片
      const exercisesStatusData = await getExercisesStatus(kpId)
      if (exercisesStatusData?.hasExercises) {
        try {
          const exercisesData = await getExercises(kpId)
          if (exercisesData?.items?.length > 0) {
            // 为每道习题创建单独的幻灯片
            exercisesData.items.forEach((exercise, index) => {
              slideItems.push({
                id: `slide_${kpId}_exercise_${index}`,
                content: `<h2>${exercise.question}</h2>`,
                type: 'quiz',
                exercise: exercise,
                sources: []
              })
            })
          }
        } catch (error) {
          // 习题加载失败不影响主流程
          console.error('加载习题失败:', error)
        }
      }
      
      slides.value = slideItems
    }
  } catch (error) {
    setError(error)
  } finally {
    learningLoading.value = false
  }
}

const loadExercisesStatus = async (kpId) => {
  try {
    const data = await getExercisesStatus(kpId)
    exercisesStatus.value = data.status || (data.hasExercises ? 'ready' : 'idle')
  } catch (error) {
    exercisesStatus.value = 'idle'
  }
}

const openExercises = async () => {
  exercisesDrawerOpen.value = true
  if (exercisesStatus.value !== 'ready') return
  try {
    const data = await getExercises(selectedKp.value.id)
    exercises.value = data.items || []
    const answers = {}
    exercises.value.forEach((item) => {
      answers[item.id] = ''
    })
    exercisesAnswers.value = answers
    exercisesFeedback.value = {}
  } catch (error) {
    setError(error)
  }
}

const closeExercises = () => {
  exercisesDrawerOpen.value = false
}

const updateAnswer = ({ id, value }) => {
  exercisesAnswers.value = { ...exercisesAnswers.value, [id]: value }
}

const submitAllAnswers = async () => {
  if (!selectedKp.value) return
  const answers = Object.entries(exercisesAnswers.value).map(([exerciseId, answer]) => ({
    exerciseId,
    answer,
  }))
  try {
    const data = await submitFeedback(selectedKp.value.id, answers)
    const feedbackMap = {}
    data.items?.forEach((item) => {
      feedbackMap[item.exerciseId] = item
    })
    exercisesFeedback.value = feedbackMap
  } catch (error) {
    setError(error)
  }
}

const submitOneAnswer = async (exerciseId) => {
  const answer = exercisesAnswers.value[exerciseId]
  try {
    const data = await submitExercise(exerciseId, answer)
    exercisesFeedback.value = {
      ...exercisesFeedback.value,
      [exerciseId]: data,
    }
  } catch (error) {
    setError(error)
  }
}

// 习题幻灯片相关函数
const updateQuizAnswer = ({ exerciseId, value }) => {
  quizAnswers.value = { ...quizAnswers.value, [exerciseId]: value }
}

const submitQuizAnswer = async (event) => {
  const { exerciseId, answer } = event
  try {
    const data = await submitExercise(exerciseId, answer)
    quizFeedback.value = {
      ...quizFeedback.value,
      [exerciseId]: data,
    }
    return data
  } catch (error) {
    setError(error)
    return null
  }
}

const onSearchInput = (value) => {
  searchQuery.value = value
}

const onSearchKeydown = (event) => {
  if (!searchQuery.value || !searchResults.value.length) return
  if (event.key === 'ArrowDown') {
    event.preventDefault()
    searchActiveIndex.value = (searchActiveIndex.value + 1) % searchResults.value.length
  }
  if (event.key === 'ArrowUp') {
    event.preventDefault()
    searchActiveIndex.value =
      (searchActiveIndex.value - 1 + searchResults.value.length) % searchResults.value.length
  }
  if (event.key === 'Enter') {
    event.preventDefault()
    const item = searchResults.value[searchActiveIndex.value]
    if (item) {
      handleSearchSelect(item)
    }
  }
}

const handleSearchSelect = (item) => {
  const target = findChapterById(chapters.value, item.id)
  if (target) {
    selectChapter(target)
  }
  searchQuery.value = ''
  searchResults.value = []
}

const handleRefresh = async (type) => {
  if (!activeBookRootId.value) {
    setError({ message: '请先激活书籍目录', code: 'BOOKROOT_NOT_FOUND' })
    return
  }

  switch (type) {
    case 'scan':
      await scanAndLoad()
      break
    case 'exercises':
      if (selectedKp.value) {
        await loadExercisesStatus(selectedKp.value.id)
      }
      break
    case 'knowledge':
      if (selectedChapter.value) {
        await loadKnowledgePoints(selectedChapter.value.id)
      }
      break
    case 'graph':
      await loadChapters()
      break
    default:
      console.warn('未知的刷新类型:', type)
  }
}

const findChapterById = (list, id) => {
  for (const node of list) {
    if (node.id === id) return node
    if (node.children?.length) {
      const found = findChapterById(node.children, id)
      if (found) return found
    }
  }
  return null
}

const handleSlideChange = (event) => {
  if (event === 'next_knowledge_point') {
    // 切换到下一个知识点
    const currentIndex = knowledgePoints.value.findIndex(kp => kp.id === selectedKp.value?.id)
    if (currentIndex !== -1 && currentIndex < knowledgePoints.value.length - 1) {
      selectKp(knowledgePoints.value[currentIndex + 1])
    }
  } else if (typeof event === 'number') {
    currentSlideIndex.value = event
  }
}

const handleNextChapter = () => {
  if (!selectedChapter.value) return
  const currentIndex = chapters.value.findIndex(ch => ch.id === selectedChapter.value.id)
  if (currentIndex !== -1 && currentIndex < chapters.value.length - 1) {
    const nextChapter = chapters.value[currentIndex + 1]
    selectChapter(nextChapter)
  }
}

let searchTimer
watch(
  searchQuery,
  (value) => {
    if (searchTimer) clearTimeout(searchTimer)
    if (!value) {
      searchResults.value = []
      return
    }
    searchTimer = setTimeout(async () => {
      try {
        const data = await searchChapters(value)
        searchResults.value = data.items || []
        searchActiveIndex.value = 0
      } catch (error) {
        setError(error)
      }
    }, 300)
  },
)

onMounted(() => {
  loadBookRoots()
})
</script>

<template>
  <div class="app-shell">
    <TopBar
      :book-roots="bookRoots"
      :active-book-root-id="activeBookRootId"
      :scan-status="scanStatus"
      :current-path="currentPath"
      :search-query="searchQuery"
      @book-change="changeBookRoot"
      @scan="scanAndLoad"
      @refresh="handleRefresh"
      @search-input="onSearchInput"
      @search-keydown="onSearchKeydown"
    />

    <ErrorBanner v-if="globalError" :message="globalError.message" :code="globalError.code" :retry="loadBookRoots" />

    <div class="content">
      <aside class="sidebar">
        <div class="section-title">章节列表</div>
        <ChapterTree
          :chapters="chapters"
          :expanded-ids="expandedIds"
          :selected-id="selectedChapter?.id || ''"
          :search-query="searchQuery"
          :search-results="searchResults"
          :search-active-index="searchActiveIndex"
          @toggle="toggleExpand"
          @select="selectChapter"
          @select-search="handleSearchSelect"
        />
        
        <div v-if="knowledgePoints.length > 0" class="section-title">知识点</div>
        <div v-if="knowledgePoints.length > 0" class="knowledge-points-list">
          <div 
            v-for="kp in knowledgePoints" 
            :key="kp.id"
            class="knowledge-point-item"
            :class="{ active: selectedKp?.id === kp.id }"
            @click="selectKp(kp)"
          >
            {{ kp.title }}
          </div>
        </div>
      </aside>

      <main class="main">
        <div v-if="learningLoading" class="loading-container">
          <div class="loading-spinner"></div>
          <div class="loading-text">加载中...</div>
        </div>
        <div v-else-if="!selectedKp" class="empty-container">
          <EmptyState title="暂无知识点" description="请选择其他章节或重新扫描。" :action="scanAndLoad" action-text="重新扫描" />
        </div>
        <SlideViewer
          v-else
          :slides="slides"
          v-model:current-index="currentSlideIndex"
          :title="currentSlideTitle"
          :audio-available="audioAvailable"
          :quiz-answers="quizAnswers"
          :quiz-feedback="quizFeedback"
          :has-next-chapter="hasNextChapter"
          @slide-change="handleSlideChange"
          @open-exercises="openExercises"
          @update-quiz-answer="updateQuizAnswer"
          @submit-quiz-answer="submitQuizAnswer"
          @next-chapter="handleNextChapter"
        />
      </main>
    </div>

    <LoadingOverlay v-if="globalLoading" :text="scanStatus === 'scanning' ? '扫描中...' : '加载中...'" />

    <ExercisesDrawer
      :open="exercisesDrawerOpen"
      :status="exercisesStatus"
      :exercises="exercises"
      :answers="exercisesAnswers"
      :feedback="exercisesFeedback"
      @close="closeExercises"
      @answer-change="updateAnswer"
      @submit-all="submitAllAnswers"
      @submit-one="submitOneAnswer"
    />
  </div>
</template>

<style scoped>
.loading-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
}

.loading-spinner {
  border: 4px solid rgba(0, 0, 0, 0.1);
  border-radius: 50%;
  border-top: 4px solid #007bff;
  width: 40px;
  height: 40px;
  animation: spin 1s linear infinite;
  margin-bottom: 16px;
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.loading-text {
  font-size: 16px;
  color: #666;
}

.empty-container {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
}

.knowledge-points-list {
  margin-top: 20px;
  padding: 0 16px;
}

.knowledge-point-item {
  padding: 8px 12px;
  margin-bottom: 8px;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.2s;
  font-size: 14px;
}

.knowledge-point-item:hover {
  background-color: rgba(0, 123, 255, 0.1);
}

.knowledge-point-item.active {
  background-color: rgba(0, 123, 255, 0.2);
  font-weight: 500;
  border-left: 3px solid #007bff;
}
</style>
