import { apiFetch } from './http'

export const getBookHubs = () => apiFetch('/books/hubs')
export const activateBookHub = (bookHubId) =>
  apiFetch('/books/activate', {
    method: 'POST',
    body: JSON.stringify({ bookHubId }),
  })

export const scanBooks = () => apiFetch('/books/scan', { method: 'POST' })

export const getChapters = () => apiFetch('/chapters')
export const searchChapters = (q, limit = 20) =>
  apiFetch(`/chapters/search?q=${encodeURIComponent(q)}&limit=${limit}`)

export const getKnowledgePoints = (chapterId) =>
  apiFetch(`/chapters/knowledge-points?chapterId=${encodeURIComponent(chapterId)}`)

export const getOverview = (kpId) =>
  apiFetch(`/knowledge-points/overview?kpId=${encodeURIComponent(kpId)}`)

export const getSourceContent = (kpId) =>
  apiFetch(`/knowledge-points/source-content?kpId=${encodeURIComponent(kpId)}`)

export const getDetailedContent = (kpId, level = 'brief') =>
  apiFetch(
    `/knowledge-points/detailed-content?kpId=${encodeURIComponent(kpId)}&level=${encodeURIComponent(level)}`,
  )

export const getSlideCards = (kpId) =>
  apiFetch(`/knowledge-points/slide-cards?kpId=${encodeURIComponent(kpId)}`)

export const getExercisesStatus = (kpId) =>
  apiFetch(`/knowledge-points/exercises/status?kpId=${encodeURIComponent(kpId)}`)

export const getExercises = (kpId) =>
  apiFetch(`/knowledge-points/exercises?kpId=${encodeURIComponent(kpId)}`)

export const submitExercise = (exerciseId, answer) =>
  apiFetch('/exercises/submit', {
    method: 'POST',
    body: JSON.stringify({ exerciseId, answer }),
  })

export const submitFeedback = (kpId, answers) =>
  apiFetch('/exercises/feedback', {
    method: 'POST',
    body: JSON.stringify({ kpId, answers }),
  })

// Settings APIs
export const getLlmSettings = () => apiFetch('/settings/llm')

export const updateLlmSettings = (settings) =>
  apiFetch('/settings/llm', {
    method: 'PUT',
    body: JSON.stringify(settings),
  })

export const testLlmConnection = (settings) =>
  apiFetch('/settings/llm/test', {
    method: 'POST',
    body: JSON.stringify(settings),
  })

export const getTtsSettings = () => apiFetch('/settings/tts')

export const updateTtsSettings = (settings) =>
  apiFetch('/settings/tts', {
    method: 'PUT',
    body: JSON.stringify(settings),
  })
