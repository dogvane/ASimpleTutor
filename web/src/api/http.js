const defaultBase = '/api/v1'

export class ApiError extends Error {
  constructor(message, code, status, details) {
    super(message)
    this.name = 'ApiError'
    this.code = code
    this.status = status
    this.details = details
  }
}

export async function apiFetch(path, options = {}) {
  const baseUrl = import.meta.env.VITE_API_BASE || defaultBase
  const url = `${baseUrl}${path}`
  const headers = {
    'Content-Type': 'application/json',
    ...(options.headers || {}),
  }

  const response = await fetch(url, {
    ...options,
    headers,
  })

  const contentType = response.headers.get('content-type') || ''
  const hasJson = contentType.includes('application/json')
  const data = hasJson ? await response.json() : null

  if (!response.ok) {
    const error = data?.error || {}
    throw new ApiError(error.message || '请求失败', error.code, response.status, error.details)
  }

  return data
}
