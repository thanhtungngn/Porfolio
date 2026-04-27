export const createId = () =>
  typeof crypto !== 'undefined' && crypto.randomUUID
    ? crypto.randomUUID()
    : `${Date.now()}-${Math.random()}`

export const initialMessages = [
  {
    id: createId(),
    role: 'assistant',
    content: 'Hi! Ask about the portfolio, projects, or backend engineering work.',
  },
]

export const normalizeChatReply = async (response) => {
  const data = await response.json().catch(() => null)

  if (!response.ok) {
    if (typeof data === 'string' && data.trim()) {
      return { ok: false, message: data }
    }

    return {
      ok: false,
      message: data?.error ?? data?.title ?? 'Unable to answer right now.',
    }
  }

  if (typeof data === 'string') {
    return { ok: true, message: data }
  }

  return {
    ok: true,
    message: data?.reply ?? 'No reply returned.',
    sources: Array.isArray(data?.sources) ? data.sources : [],
  }
}