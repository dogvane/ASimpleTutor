import { apiFetch } from './http'

export const getBookRoots = () => apiFetch('/books/roots')
export const activateBookRoot = (bookRootId) =>
  apiFetch('/books/activate', {
    method: 'POST',
    body: JSON.stringify({ bookRootId }),
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
