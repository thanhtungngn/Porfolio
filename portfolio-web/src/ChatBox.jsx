import { useState, useEffect, useRef } from 'react'
import { buildApiUrl } from './api'
import { createId, initialMessages, normalizeChatReply } from './chat'
import { withAuthHeader } from './auth'
import { useAuth } from './useAuth'

export default function ChatBox({ mode = 'public' }) {
  const { authToken, isAuthenticated } = useAuth()
  const [provider, setProvider] = useState('openai')
  const [model, setModel] = useState('')
  const [useRag, setUseRag] = useState(true)
  const [prompt, setPrompt] = useState('')
  const [loading, setLoading] = useState(false)
  const [messages, setMessages] = useState(() => initialMessages)
  const logEndRef = useRef(null)

  useEffect(() => {
    logEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  const canConfigure = mode === 'admin' && isAuthenticated
  const canSend = !loading && prompt.trim().length > 0

  const renderMessages = () => (
    <div className="chat-log" aria-live="polite">
      {messages.map((message) => (
        <div key={message.id} className={`message ${message.role}`}>
          <strong>{message.role === 'user' ? 'You' : 'Agent'}</strong>
          <p>{message.content}</p>
        </div>
      ))}
      <div ref={logEndRef} />
    </div>
  )

  const sendPrompt = async (event) => {
    event.preventDefault()

    if (!canSend) {
      return
    }

    const userMessage = { id: createId(), role: 'user', content: prompt.trim() }
    setMessages((current) => [...current, userMessage])
    setPrompt('')
    setLoading(true)

    try {
      const useProtectedRag = canConfigure && useRag
      const response = await fetch(buildApiUrl(useProtectedRag ? '/api/chat/rag' : '/api/chat'), {
        method: 'POST',
        headers: withAuthHeader(
          {
            'Content-Type': 'application/json',
          },
          authToken,
        ),
        body: JSON.stringify({
          provider,
          model: model.trim() || null,
          useRag: true,
          message: userMessage.content,
        }),
      })

      const result = await normalizeChatReply(response)
      const sourceLine =
        result.ok && Array.isArray(result.sources) && result.sources.length > 0
          ? `\n\nSources: ${result.sources.map((source) => source.source).join(', ')}`
          : ''

      setMessages((current) => [
        ...current,
        {
          id: createId(),
          role: 'assistant',
          content: `${result.message}${sourceLine}`,
        },
      ])
    } catch {
      setMessages((current) => [
        ...current,
        {
          id: createId(),
          role: 'assistant',
          content: 'Cannot reach backend. Ensure the API base URL is configured and the service is running.',
        },
      ])
    } finally {
      setLoading(false)
    }
  }

  return (
    <aside className={`chat-panel ${mode === 'admin' ? 'admin-chat-panel' : ''}`}>
      <div className="panel-heading">
        <div>
          <p className="eyebrow">Portfolio agent</p>
          <h3>{mode === 'admin' ? 'Configured chat' : 'Ask the portfolio agent'}</h3>
        </div>
        {canConfigure ? <span className="status-pill">Admin</span> : null}
      </div>

      {canConfigure ? (
        <>
          <form className="chat-controls" onSubmit={sendPrompt}>
            <label>
              Provider
              <select value={provider} onChange={(event) => setProvider(event.target.value)}>
                <option value="openai">OpenAI</option>
                <option value="ollama">Ollama</option>
              </select>
            </label>

            <label>
              Model override
              <input
                type="text"
                value={model}
                onChange={(event) => setModel(event.target.value)}
                placeholder={provider === 'openai' ? 'gpt-4o-mini' : 'llama3.2'}
              />
            </label>

            <label className="rag-toggle">
              <input
                type="checkbox"
                checked={useRag}
                onChange={(event) => setUseRag(event.target.checked)}
              />
              Use RAG context
            </label>

            <label>
              Ask a question
              <textarea
                value={prompt}
                onChange={(event) => setPrompt(event.target.value)}
                placeholder="Test the assistant with private knowledge enabled."
                rows={3}
              />
            </label>

            <button disabled={!canSend}>{loading ? 'Thinking...' : 'Send'}</button>
          </form>

          {renderMessages()}
        </>
      ) : (
        <form className="chat-compose" onSubmit={sendPrompt}>
          {renderMessages()}

          <label className="chat-compose-label">
            <span>Message</span>
            <textarea
              value={prompt}
              onChange={(event) => setPrompt(event.target.value)}
              placeholder="What projects are most relevant to backend engineering?"
              rows={3}
            />
          </label>

          <button disabled={!canSend}>{loading ? 'Thinking...' : 'Send'}</button>
        </form>
      )}
    </aside>
  )
}