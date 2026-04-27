export const authStorageKey = 'portfolio.authToken'

export const withAuthHeader = (headers, authToken) =>
  authToken
    ? {
        ...headers,
        Authorization: `Bearer ${authToken}`,
      }
    : headers

export const readStoredToken = () => localStorage.getItem(authStorageKey) ?? ''

export const persistToken = (token) => {
  localStorage.setItem(authStorageKey, token)
}

export const clearStoredToken = () => {
  localStorage.removeItem(authStorageKey)
}