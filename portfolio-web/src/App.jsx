import { useState } from 'react'
import { Navigate, Route, Routes } from 'react-router-dom'
import './App.css'
import { AuthProvider } from './AuthContext'
import AdminPage from './AdminPage'
import ChatBox from './ChatBox'
import PortfolioPanel from './PortfolioPanel'

function PublicPage() {
  const [isChatOpen, setIsChatOpen] = useState(false)

  return (
    <>
      <main className="layout" data-chat-open={isChatOpen ? 'true' : undefined}>
        <PortfolioPanel />
      </main>

      {/* Floating chat panel */}
      {isChatOpen && (
        <div className="chat-overlay">
          <div className="chat-overlay-header">
            <span className="chat-overlay-title">🤖 Ask the Agent</span>
            <button
              className="chat-close-btn"
              onClick={() => setIsChatOpen(false)}
              aria-label="Close chat"
            >
              ✕
            </button>
          </div>
          <div className="chat-overlay-body">
            <ChatBox mode="public" />
          </div>
        </div>
      )}

      {/* FAB */}
      <button
        className={`chat-fab${isChatOpen ? ' chat-fab--active' : ''}`}
        onClick={() => setIsChatOpen((o) => !o)}
        aria-label={isChatOpen ? 'Close chat' : 'Open AI chat'}
        title={isChatOpen ? 'Close chat' : 'Chat with my AI agent'}
      >
        {isChatOpen ? '✕' : '🤖'}
      </button>
    </>
  )
}

function App() {
  return (
    <AuthProvider>
      <Routes>
        <Route path="/" element={<PublicPage />} />
        <Route path="/admin" element={<AdminPage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </AuthProvider>
  )
}

export default App
