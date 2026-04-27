const trimTrailingSlash = (value) => value.replace(/\/$/, '')

const configuredBaseUrl = typeof import.meta !== 'undefined' ? import.meta.env.VITE_API_BASE_URL ?? '' : ''

export const apiBaseUrl = configuredBaseUrl.trim() ? trimTrailingSlash(configuredBaseUrl.trim()) : ''

export const buildApiUrl = (path) => `${apiBaseUrl}${path.startsWith('/') ? path : `/${path}`}`