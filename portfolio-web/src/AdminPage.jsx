import { useState } from 'react'
import { Link } from 'react-router-dom'
import ChatBox from './ChatBox'
import PortfolioPanel from './PortfolioPanel'
import { withAuthHeader } from './auth'
import { useAuth } from './useAuth'

export default function AdminPage() {
  const {
    authChecking,
    authError,
    authNotice,
    authenticate,
    authenticatedUser,
    authToken,
    isAuthenticated,
    logout,
  } = useAuth()
  const [authUsername, setAuthUsername] = useState('')
  const [authPassword, setAuthPassword] = useState('')
  const [knowledgeFile, setKnowledgeFile] = useState(null)
  const [knowledgeSource, setKnowledgeSource] = useState('')
  const [ingesting, setIngesting] = useState(false)
  const [ingestNotice, setIngestNotice] = useState('')
  const [ingestError, setIngestError] = useState('')

  const handleLogin = async (event) => {
    event.preventDefault()
    const result = await authenticate({
      username: authUsername,
      password: authPassword,
      path: '/api/auth/login',
    })

    if (result.ok) {
      setAuthPassword('')
    }
  }

  const handleRegister = async (event) => {
    event.preventDefault()
    const result = await authenticate({
      username: authUsername,
      password: authPassword,
      path: '/api/auth/register',
    })

    if (result.ok) {
      setAuthPassword('')
    }
  }

  const handleLogout = () => {
    logout()
    setKnowledgeFile(null)
    setKnowledgeSource('')
    setIngestNotice('')
    setIngestError('')
  }

  const uploadKnowledge = async (event) => {
    event.preventDefault()

    if (!knowledgeFile) {
      setIngestError('Choose a file before uploading.')
      return
    }

    setIngesting(true)
    setIngestNotice('')
    setIngestError('')

    const formData = new FormData()
    formData.append('file', knowledgeFile)

    if (knowledgeSource.trim()) {
      formData.append('source', knowledgeSource.trim())
    }

    try {
      const response = await fetch('/api/rag/ingest', {
        method: 'POST',
        headers: withAuthHeader({}, authToken),
        body: formData,
      })

      const data = await response.json().catch(() => ({}))
      if (!response.ok) {
        throw new Error(data.error ?? data.title ?? 'Upload failed.')
      }

      setKnowledgeFile(null)
      setKnowledgeSource('')
      setIngestNotice(`Uploaded successfully. ${data.chunksIngested} chunks added to vector DB.`)
    } catch (error) {
      setIngestError(error.message)
    } finally {
      setIngesting(false)
    }
  }

  return (
    <main className="layout admin-layout">
      <section className="admin-shell">
        <header className="admin-header">
          <div>
            <p className="eyebrow">Admin route</p>
            <h1>Authentication and RAG controls</h1>
            <p>
              Sign in here to manage protected knowledge uploads and test the assistant with RAG enabled.
            </p>
          </div>
          <Link to="/" className="secondary-link">
            Back to portfolio
          </Link>
        </header>

        <div className="admin-grid">
          <div className="admin-column">
            <section className="auth-panel admin-card">
              <div className="panel-heading">
                <h4>Protected access</h4>
                {isAuthenticated ? <span className="status-pill">Authenticated</span> : null}
              </div>

              {!isAuthenticated ? (
                <form className="auth-form" onSubmit={handleLogin}>
                  <label>
                    Username
                    <input
                      type="text"
                      value={authUsername}
                      onChange={(event) => setAuthUsername(event.target.value)}
                      autoComplete="username"
                    />
                  </label>

                  <label>
                    Password
                    <input
                      type="password"
                      value={authPassword}
                      onChange={(event) => setAuthPassword(event.target.value)}
                      autoComplete="current-password"
                    />
                  </label>

                  <button type="submit" disabled={authChecking}>
                    {authChecking ? 'Checking...' : 'Sign in'}
                  </button>

                  <button type="button" className="secondary-button" disabled={authChecking} onClick={handleRegister}>
                    Create account
                  </button>
                </form>
              ) : (
                <div className="auth-summary">
                  <p>Authenticated as {authenticatedUser}.</p>
                  <button type="button" className="secondary-button" onClick={handleLogout}>
                    Sign out
                  </button>
                </div>
              )}

              {authNotice ? <p className="notice success">{authNotice}</p> : null}
              {authError ? <p className="notice error">{authError}</p> : null}
            </section>

            <section className="auth-panel admin-card">
              <div className="panel-heading">
                <h4>Portfolio preview</h4>
                <span className="status-pill">Public</span>
              </div>
              <div className="admin-preview">
                <PortfolioPanel />
              </div>
            </section>
          </div>

          <div className="admin-column">
            {isAuthenticated ? (
              <section className="auth-panel admin-card">
                <div className="panel-heading">
                  <h4>Upload knowledge</h4>
                  <span className="status-pill">RAG</span>
                </div>

                <form className="auth-form" onSubmit={uploadKnowledge}>
                  <label>
                    File
                    <input
                      type="file"
                      accept=".pdf,.txt,.md,.json,.csv,.xml,.yaml,.yml,text/*,application/json,application/xml"
                      onChange={(event) => setKnowledgeFile(event.target.files?.[0] ?? null)}
                    />
                  </label>

                  <label>
                    Source label
                    <input
                      type="text"
                      value={knowledgeSource}
                      onChange={(event) => setKnowledgeSource(event.target.value)}
                      placeholder="team-notes-q2"
                    />
                  </label>

                  <button type="submit" disabled={ingesting || !knowledgeFile}>
                    {ingesting ? 'Uploading...' : 'Upload to vector DB'}
                  </button>
                </form>

                {ingestNotice ? <p className="notice success">{ingestNotice}</p> : null}
                {ingestError ? <p className="notice error">{ingestError}</p> : null}
              </section>
            ) : (
              <section className="auth-panel admin-card admin-empty-state">
                <h4>RAG management is locked</h4>
                <p>Authenticate first to upload private documents and enable the protected chat mode.</p>
              </section>
            )}

            <ChatBox mode="admin" />
          </div>
        </div>
      </section>
    </main>
  )
}