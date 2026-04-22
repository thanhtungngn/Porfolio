import { useEffect, useMemo, useState } from 'react'
import './App.css'

const createId = () =>
  typeof crypto !== 'undefined' && crypto.randomUUID
    ? crypto.randomUUID()
    : `${Date.now()}-${Math.random()}`

const emptyPortfolio = {
  name: 'Loading...',
  role: '',
  summary: '',
  technologies: [],
  highlights: [],
  contact: {
    email: '',
    gitHub: '',
    linkedIn: '',
  },
}

function App() {
  const [portfolio, setPortfolio] = useState(emptyPortfolio)
  const [provider, setProvider] = useState('openai')
  const [model, setModel] = useState('')
  const [prompt, setPrompt] = useState('')
  const [loading, setLoading] = useState(false)
  const [messages, setMessages] = useState([
    {
      id: createId(),
      role: 'assistant',
      content:
        'Hi! I can explain this portfolio in detail. Pick OpenAI or Ollama and ask anything.',
    },
  ])

  useEffect(() => {
    const loadPortfolio = async () => {
      const response = await fetch('/api/portfolio')
      const data = await response.json()
      setPortfolio(data)
    }

    loadPortfolio().catch(() => {
      setPortfolio({
        ...emptyPortfolio,
        name: 'Portfolio unavailable',
        summary: 'Please start the backend API and refresh the page.',
      })
    })
  }, [])

  const canSend = useMemo(() => !loading && prompt.trim().length > 0, [loading, prompt])

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
      const response = await fetch('/api/chat', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          provider,
          model: model.trim() || null,
          message: userMessage.content,
        }),
      })

      const data = await response.json()
      const assistantText = response.ok ? data.reply : data.error ?? 'Unable to answer right now.'

      setMessages((current) => [
        ...current,
        {
          id: createId(),
          role: 'assistant',
          content: assistantText,
        },
      ])
    } catch {
      setMessages((current) => [
        ...current,
        {
          id: createId(),
          role: 'assistant',
          content: 'Cannot reach backend. Ensure API is running on http://localhost:5050.',
        },
      ])
    } finally {
      setLoading(false)
    }
  }

  return (
    <main className="layout">
      <section className="portfolio-panel">
        <h1>{portfolio.name}</h1>
        <h2>{portfolio.role}</h2>
        <p>{portfolio.summary}</p>

        <article>
          <h3>Technology focus</h3>
          <ul>
            {portfolio.technologies.map((item) => (
              <li key={item}>{item}</li>
            ))}
          </ul>
        </article>

        <article>
          <h3>What this portfolio demonstrates</h3>
          <ul>
            {portfolio.highlights.map((item) => (
              <li key={item}>{item}</li>
            ))}
          </ul>
        </article>

        <article>
          <h3>Contact</h3>
          <p>{portfolio.contact.email}</p>
          <p>
            <a href={portfolio.contact.gitHub} target="_blank" rel="noreferrer">
              GitHub
            </a>{' '}
            ·{' '}
            <a href={portfolio.contact.linkedIn} target="_blank" rel="noreferrer">
              LinkedIn
            </a>
          </p>
        </article>
      </section>

      <aside className="chat-panel">
        <h3>Ask the portfolio agent</h3>

        <form className="chat-controls" onSubmit={sendPrompt}>
          <label>
            Provider
            <select value={provider} onChange={(event) => setProvider(event.target.value)}>
              <option value="openai">OpenAI</option>
              <option value="ollama">Ollama</option>
            </select>
          </label>

          <label>
            Model (optional)
            <input
              type="text"
              value={model}
              onChange={(event) => setModel(event.target.value)}
              placeholder={provider === 'openai' ? 'gpt-4o-mini' : 'llama3.2'}
            />
          </label>

          <label>
            Ask a question
            <textarea
              value={prompt}
              onChange={(event) => setPrompt(event.target.value)}
              placeholder="What projects are most relevant to backend engineering?"
              rows={3}
            />
          </label>

          <button disabled={!canSend}>{loading ? 'Thinking...' : 'Send'}</button>
        </form>

        <div className="chat-log" aria-live="polite">
          {messages.map((message) => (
            <div key={message.id} className={`message ${message.role}`}>
              <strong>{message.role === 'user' ? 'You' : 'Agent'}</strong>
              <p>{message.content}</p>
            </div>
          ))}
        </div>
      </aside>
    </main>
  )
}

export default App
