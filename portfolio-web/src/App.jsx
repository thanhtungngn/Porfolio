import { useEffect, useState } from 'react'
import { Navigate, Route, Routes } from 'react-router-dom'
import './App.css'
import { AuthProvider } from './AuthContext'
import AdminPage from './AdminPage'
import ChatBox from './ChatBox'
import PortfolioPanel from './PortfolioPanel'
import { emptyPortfolio } from './portfolio'

function PublicPage({ portfolio }) {
  return (
    <main className="layout">
      <PortfolioPanel portfolio={portfolio} />
      <ChatBox mode="public" />
    </main>
  )
}

function App() {
  const [portfolio, setPortfolio] = useState(emptyPortfolio)

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

  return (
    <AuthProvider>
      <Routes>
        <Route path="/" element={<PublicPage portfolio={portfolio} />} />
        <Route path="/admin" element={<AdminPage portfolio={portfolio} />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </AuthProvider>
  )
}

export default App
