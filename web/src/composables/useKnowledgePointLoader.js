import { getKnowledgePoints, getOverview, getDetailedContent, getSourceContent, getSlideCards, getExercisesStatus } from '../api'

/**
 * 知识点加载逻辑
 */
export function useKnowledgePointLoader(state) {
  const {
    knowledgePoints,
    selectedKp,
    slides,
    currentSlideIndex,
    audioAvailable,
    exercisesStatus,
    learningLoading,
    setError,
  } = state

  // 加载知识点列表
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
      // 返回第一个知识点，由调用者决定是否选择
      return knowledgePoints.value
    } catch (error) {
      setError(error)
      return []
    } finally {
      learningLoading.value = false
    }
  }

  // 检查音频是否可用
  const checkAudioAvailability = async (kpId) => {
    // 预留音频检查逻辑
    return false
  }

  // 选择知识点
  const selectKp = async (kp) => {
    if (!kp || selectedKp.value?.id === kp.id) return
    selectedKp.value = kp
    slides.value = []
    currentSlideIndex.value = 0
    exercisesStatus.value = 'idle'
    await loadSlides(kp.id)
    await loadExercisesStatus(kp.id)
    audioAvailable.value = await checkAudioAvailability(kp.id)
  }

  // 加载习题状态
  const loadExercisesStatus = async (kpId) => {
    try {
      const data = await getExercisesStatus(kpId)
      exercisesStatus.value = data.status || (data.hasExercises ? 'ready' : 'idle')
    } catch (error) {
      exercisesStatus.value = 'idle'
    }
  }

  // 加载幻灯片
  const loadSlides = async (kpId) => {
    learningLoading.value = true
    setError(null)
    try {
      const slideItems = []

      // 尝试加载 SlideCards
      try {
        const slideCardsData = await getSlideCards(kpId)
        if (slideCardsData?.slideCards?.length > 0) {
          slideItems.push(...slideCardsData.slideCards.map(sc => ({
            id: sc.slideId,
            content: sc.htmlContent,
            type: sc.type.toLowerCase(),
            title: sc.title,
            speechScript: sc.speechScript,
            audioUrl: sc.audioUrl,
            speed: sc.speed || 1.0,
            sources: sc.sourceReferences?.map(sr => ({
              text: sr.content?.substring(0, 50) + '...',
              fileName: sr.filePath ? sr.filePath.split('/').pop() : '',
              ...sr
            })) || []
          })))
        }
      } catch (error) {
        console.error('加载 SlideCards 失败，使用降级方案:', error)
      }

      // 降级方案：使用 Overview/Detailed/Source API
      if (slideItems.length === 0) {
        const overviewData = await getOverview(kpId)
        if (overviewData) {
          let overviewContent = ''
          if (typeof overviewData.overview === 'object') {
            overviewContent = `<div class="overview-content">`

            if (overviewData.overview.definition) {
              overviewContent += `<div class="definition-section">
                <h2>定义</h2>
                <p>${overviewData.overview.definition}</p>
              </div>`
            }

            if (overviewData.overview.keyPoints && overviewData.overview.keyPoints.length > 0) {
              overviewContent += `<div class="keypoints-section">
                <h2>关键点</h2>
                <ul>
                  ${overviewData.overview.keyPoints.map(point => `<li>${point}</li>`).join('')}
                </ul>
              </div>`
            }

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
            overviewContent = `<p>${overviewData.overview}</p>`
          }

          slideItems.push({
            id: `slide_${kpId}_1`,
            content: `<h1>${overviewData.title}</h1>${overviewContent}`,
            type: 'content',
            sources: []
          })

          // 详细内容
          try {
            const detailedContent = await getDetailedContent(kpId, 'detailed')
            if (detailedContent?.levels) {
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
            console.error('加载详细内容失败:', error)
          }

          // 原文来源
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
            console.error('加载原文失败:', error)
          }
        }
      }

      slides.value = slideItems
    } catch (error) {
      setError(error)
    } finally {
      learningLoading.value = false
    }
  }

  return {
    loadKnowledgePoints,
    selectKp,
    loadSlides,
    loadExercisesStatus,
  }
}
