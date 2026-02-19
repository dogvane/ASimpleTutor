<script setup>
import { computed, onMounted, watch } from 'vue'
import ChapterTree from './components/ChapterTree.vue'
import EmptyState from './components/EmptyState.vue'
import ErrorBanner from './components/ErrorBanner.vue'
import ExercisesDrawer from './components/ExercisesDrawer.vue'
import LoadingOverlay from './components/LoadingOverlay.vue'
import SettingsDialog from './components/settings/SettingsDialog.vue'
import SlideViewer from './components/SlideViewer.vue'
import TopBar from './components/TopBar.vue'
import KnowledgePointList from './components/KnowledgePointList.vue'
import { useAppStore } from './composables/useAppStore'
import { useChapterLoader } from './composables/useChapterLoader'
import { useKnowledgePointLoader } from './composables/useKnowledgePointLoader'
import { useExercise } from './composables/useExercise'
import { useKnowledgeGraphLoader } from './composables/useKnowledgeGraphLoader'

// 初始化状态管理
const state = useAppStore()
const {
  bookHubs,
  activeBookHubId,
  scanStatus,
  chapters,
  expandedIds,
  selectedChapter,
  searchQuery,
  searchResults,
  searchActiveIndex,
  knowledgePoints,
  selectedKp,
  slides,
  currentSlideIndex,
  audioAvailable,
  quizAnswers,
  quizFeedback,
  globalError,
  globalLoading,
  learningLoading,
  knowledgeGraph,
  knowledgeGraphLoading,
  settingsDialogOpen,
  currentPath,
  hasNextChapter,
  exercisesStatus,
  exercisesDrawerOpen,
  exercises,
  exercisesAnswers,
  exercisesFeedback,
  setError,
} = state

// 初始化各功能模块
const chapterLoader = useChapterLoader(state)
const {
  loadBookHubs,
  scanAndLoad,
  changeBookHub,
  toggleExpand,
  selectChapter,
  setupSearch,
  findChapterById,
  nextChapter: getNextChapter,
} = chapterLoader

const kpLoader = useKnowledgePointLoader(state)
const {
  loadKnowledgePoints,
  selectKp,
  refreshExercises,
} = kpLoader

const exerciseModule = useExercise(state)
const {
  openExercises,
  closeExercises,
  updateAnswer,
  submitAllAnswers,
  submitOneAnswer,
  updateQuizAnswer,
  submitQuizAnswer,
} = exerciseModule

const knowledgeGraphLoader = useKnowledgeGraphLoader(state)
const {
  refreshKnowledgeGraph,
} = knowledgeGraphLoader

// 计算属性
const currentSlideTitle = computed(() => {
  if (selectedKp.value) {
    return selectedKp.value.title
  }
  return selectedChapter.value?.title || ''
})

// 搜索处理 - 需要在 setupSearch 调用前定义
const handleSearchSelect = (item) => {
  const target = findChapterById(chapters.value, item.id)
  if (target) {
    handleSelectChapter(target)
  }
  searchQuery.value = ''
  searchResults.value = []
}

const { onSearchInput, onSearchKeydown, watchSearch } = setupSearch(handleSearchSelect)

// 选择章节后自动加载知识点和章节内容
const handleSelectChapter = async (chapter) => {
  selectChapter(chapter)
  const kps = await loadKnowledgePoints(chapter.id)
  if (kps.length > 0) {
    await selectKp(kps[0])
  }
  // 加载章节学习内容
  const chapterContent = await chapterLoader.loadChapterContent(chapter.id)
  if (chapterContent) {
    console.log('章节学习内容已加载:', chapterContent)
  }
}

// 刷新处理
const handleRefresh = async (type) => {
  if (!activeBookHubId.value) {
    setError({ message: '请先激活书籍中心', code: 'BOOKHUB_NOT_FOUND' })
    return
  }

  switch (type) {
    case 'scan':
      await scanAndLoad()
      break
    case 'exercises':
      await refreshExercises()
      break
    case 'knowledge':
      if (selectedChapter.value) {
        const kps = await loadKnowledgePoints(selectedChapter.value.id)
        if (kps.length > 0 && (!selectedKp.value || !kps.find(kp => kp.id === selectedKp.value.id))) {
          await selectKp(kps[0])
        }
      }
      break
    case 'graph':
      await refreshKnowledgeGraph()
      break
    default:
      console.warn('未知的刷新类型:', type)
  }
}

// 幻灯片切换处理
const handleSlideChange = (event) => {
  if (event === 'next_knowledge_point') {
    const currentIndex = knowledgePoints.value.findIndex(kp => kp.id === selectedKp.value?.id)
    if (currentIndex !== -1 && currentIndex < knowledgePoints.value.length - 1) {
      selectKp(knowledgePoints.value[currentIndex + 1])
    }
  } else if (typeof event === 'number') {
    currentSlideIndex.value = event
  }
}

// 下一章处理
const handleNextChapter = () => {
  const next = getNextChapter()
  if (next) {
    handleSelectChapter(next)
  }
}

// 设置对话框处理
const openSettings = () => {
  settingsDialogOpen.value = true
}

const handleSettingsSaved = () => {
  console.log('配置已保存并实时生效')
}

// 监听搜索
watchSearch(watch)

// 初始化
onMounted(() => {
  loadBookHubs()
})
</script>

<template>
  <div class="app-shell">
    <TopBar
      :book-hubs="bookHubs"
      :active-book-hub-id="activeBookHubId"
      :scan-status="scanStatus"
      :scan-progress="scanProgress"
      :current-path="currentPath"
      :search-query="searchQuery"
      @book-change="changeBookHub"
      @scan="scanAndLoad"
      @refresh="handleRefresh"
      @search-input="onSearchInput"
      @search-keydown="onSearchKeydown"
      @open-settings="openSettings"
    />

    <ErrorBanner v-if="globalError" :message="globalError.message" :code="globalError.code" :retry="loadBookHubs" />

    <div class="content">
      <aside class="sidebar">
        <div class="sidebar-content">
          <div class="section-title">章节列表</div>
          <ChapterTree
            :chapters="chapters"
            :expanded-ids="expandedIds"
            :selected-id="selectedChapter?.id || ''"
            :search-query="searchQuery"
            :search-results="searchResults"
            :search-active-index="searchActiveIndex"
            @toggle="toggleExpand"
            @select="handleSelectChapter"
            @select-search="handleSearchSelect"
          />

          <div v-if="knowledgePoints.length > 0" class="section-title">知识点</div>
          <KnowledgePointList
            v-if="knowledgePoints.length > 0"
            :knowledge-points="knowledgePoints"
            :selected-kp-id="selectedKp?.id || ''"
            @select="selectKp"
          />
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

    <SettingsDialog
      :open="settingsDialogOpen"
      @close="settingsDialogOpen = false"
      @saved="handleSettingsSaved"
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
</style>
