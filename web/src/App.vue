<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import {
  activateBookRoot,
  getBookRoots,
  getChapters,
  getDetailedContent,
  getExercises,
  getExercisesStatus,
  getKnowledgePoints,
  getOverview,
  getSourceContent,
  scanBooks,
  searchChapters,
  submitExercise,
  submitFeedback,
} from './api'
import ChapterTree from './components/ChapterTree.vue'
import EmptyState from './components/EmptyState.vue'
import ErrorBanner from './components/ErrorBanner.vue'
import ExercisesDrawer from './components/ExercisesDrawer.vue'
import LearningPanel from './components/LearningPanel.vue'
import LoadingOverlay from './components/LoadingOverlay.vue'
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
const learningTab = ref('overview')
const activeLevel = ref('brief')
const overviewData = ref(null)
const sourceData = ref(null)
const levelsData = ref(null)
const learningLoading = ref(false)
const exercisesStatus = ref('idle')
const exercisesDrawerOpen = ref(false)
const exercises = ref([])
const exercisesAnswers = ref({})
const exercisesFeedback = ref({})
const globalError = ref(null)
const globalLoading = ref(false)

const currentPath = computed(() => {
  if (selectedChapter.value && selectedKp.value) {
    return `${selectedChapter.value.title} > ${selectedKp.value.title}`
  }
  return selectedChapter.value?.title || ''
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
  overviewData.value = null
  sourceData.value = null
  levelsData.value = null
  exercisesStatus.value = 'idle'
  exercises.value = []
  exercisesAnswers.value = {}
  exercisesFeedback.value = {}
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
  globalLoading.value = true
  setError(null)
  try {
    await scanBooks()
    await loadChapters()
    scanStatus.value = 'ready'
  } catch (error) {
    scanStatus.value = 'failed'
    scanMessage.value = error.message
    setError(error)
  } finally {
    globalLoading.value = false
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
  learningTab.value = 'overview'
  activeLevel.value = 'brief'
  overviewData.value = null
  sourceData.value = null
  levelsData.value = null
  exercisesStatus.value = 'idle'
  await loadOverview(kp.id)
  await loadExercisesStatus(kp.id)
}

const loadOverview = async (kpId) => {
  learningLoading.value = true
  setError(null)
  try {
    overviewData.value = await getOverview(kpId)
  } catch (error) {
    setError(error)
  } finally {
    learningLoading.value = false
  }
}

const loadSource = async (kpId) => {
  learningLoading.value = true
  setError(null)
  try {
    sourceData.value = await getSourceContent(kpId)
  } catch (error) {
    setError(error)
  } finally {
    learningLoading.value = false
  }
}

const loadLevels = async (kpId, level) => {
  learningLoading.value = true
  setError(null)
  try {
    levelsData.value = await getDetailedContent(kpId, level)
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

watch(
  learningTab,
  (tab) => {
    if (!selectedKp.value) return
    if (tab === 'source' && !sourceData.value) {
      loadSource(selectedKp.value.id)
    }
    if (tab === 'levels' && !levelsData.value) {
      loadLevels(selectedKp.value.id, activeLevel.value)
    }
  },
)

watch(
  activeLevel,
  (level) => {
    if (learningTab.value === 'levels' && selectedKp.value) {
      loadLevels(selectedKp.value.id, level)
    }
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
      </aside>

      <main class="main">
        <LearningPanel
          :chapter-title="selectedChapter?.title || ''"
          :knowledge-points="knowledgePoints"
          :selected-kp-id="selectedKp?.id || ''"
          :overview="overviewData"
          :source="sourceData"
          :levels="levelsData"
          :learning-tab="learningTab"
          :active-level="activeLevel"
          :exercises-status="exercisesStatus"
          @select-kp="selectKp"
          @tab-change="learningTab = $event"
          @level-change="activeLevel = $event"
          @open-exercises="openExercises"
        >
          <template #empty>
            <EmptyState title="暂无知识点" description="请选择其他章节或重新扫描。" :action="scanAndLoad" action-text="重新扫描" />
          </template>
          <template #loading>
            <div v-if="learningLoading" class="inline-loading">内容加载中...</div>
          </template>
        </LearningPanel>
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
