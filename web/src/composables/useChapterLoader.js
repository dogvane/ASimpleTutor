import { getBookHubs, activateBookHub, getChapters, scanBooks, searchChapters } from '../api'

/**
 * 章节加载逻辑
 */
export function useChapterLoader(state) {
  const {
    bookHubs,
    activeBookHubId,
    scanStatus,
    scanMessage,
    chapters,
    chaptersLoading,
    expandedIds,
    selectedChapter,
    globalError,
    globalLoading,
    searchQuery,
    searchResults,
    searchActiveIndex,
    resetLearningState,
    setError,
  } = state

  // 加载书籍中心列表
  const loadBookHubs = async () => {
    globalLoading.value = true
    setError(null)
    try {
      const data = await getBookHubs()
      bookHubs.value = data.items || []
      const nextActiveId = data.activeId || bookHubs.value[0]?.id || ''
      activeBookHubId.value = nextActiveId
      if (nextActiveId) {
        if (!data.activeId) {
          await activateBookHub(nextActiveId)
        } else {
          await loadChapters()
        }
      } else {
        setError({ message: '未配置书籍中心，请先配置并激活。', code: 'BOOKHUB_NOT_FOUND' })
      }
    } catch (error) {
      setError(error)
    } finally {
      globalLoading.value = false
    }
  }

  // 扫描并加载
  const scanAndLoad = async () => {
    if (!activeBookHubId.value) {
      setError({ message: '请先激活书籍中心', code: 'BOOKHUB_NOT_FOUND' })
      return
    }
    scanStatus.value = 'scanning'
    scanMessage.value = ''
    chapters.value = []
    expandedIds.value = []
    selectedChapter.value = null
    resetLearningState()
    setError(null)

    try {
      await scanBooks()
      await loadChapters()
      scanStatus.value = 'ready'
    } catch (error) {
      scanStatus.value = 'failed'
      scanMessage.value = error.message
      setError(error)
    }
  }

  // 加载章节列表
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

  // 切换书籍中心
  const changeBookHub = async (bookHubId) => {
    if (!bookHubId || bookHubId === activeBookHubId.value) return
    globalLoading.value = true
    setError(null)
    try {
      await activateBookHub(bookHubId)
      activeBookHubId.value = bookHubId
      await scanAndLoad()
    } catch (error) {
      setError(error)
    } finally {
      globalLoading.value = false
    }
  }

  // 切换章节展开状态
  const toggleExpand = (id) => {
    if (expandedIds.value.includes(id)) {
      expandedIds.value = expandedIds.value.filter((item) => item !== id)
    } else {
      expandedIds.value = [...expandedIds.value, id]
    }
  }

  // 选择章节
  const selectChapter = async (chapter) => {
    if (!chapter || selectedChapter.value?.id === chapter.id) return
    selectedChapter.value = chapter
    resetLearningState()
    // 知识点加载由 useKnowledgePointLoader 处理
  }

  // 搜索章节
  let searchTimer
  const setupSearch = (onSelectCallback) => {
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
        if (item && onSelectCallback) {
          onSelectCallback(item)
        }
      }
    }

    const watchSearch = (watch) => {
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
    }

    return { onSearchInput, onSearchKeydown, watchSearch }
  }

  // 查找章节
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

  // 下一章
  const nextChapter = () => {
    if (!selectedChapter.value) return null
    const currentIndex = chapters.value.findIndex(ch => ch.id === selectedChapter.value.id)
    if (currentIndex !== -1 && currentIndex < chapters.value.length - 1) {
      return chapters.value[currentIndex + 1]
    }
    return null
  }

  return {
    loadBookHubs,
    scanAndLoad,
    loadChapters,
    changeBookHub,
    toggleExpand,
    selectChapter,
    setupSearch,
    findChapterById,
    nextChapter,
  }
}
