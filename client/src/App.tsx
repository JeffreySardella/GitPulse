// client/src/App.tsx
import { useEffect, useState } from 'react'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { useAuthStore } from './stores/authStore'
import LoginPage from './pages/LoginPage'
import OAuthCallback from './pages/OAuthCallback'
import DashboardPage from './pages/DashboardPage'

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)
  return isAuthenticated ? children : <Navigate to="/" />
}

function App() {
  const [isRestoring, setIsRestoring] = useState(true)
  const restoreSession = useAuthStore((s) => s.restoreSession)

  useEffect(() => {
    restoreSession().finally(() => setIsRestoring(false))
  }, [restoreSession])

  if (isRestoring) {
    return (
      <div className="min-h-screen bg-gray-950 flex items-center justify-center">
        <div className="w-8 h-8 border-2 border-gray-600 border-t-white rounded-full animate-spin" />
      </div>
    )
  }

  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<LoginPage />} />
        <Route path="/callback" element={<OAuthCallback />} />
        <Route
          path="/dashboard"
          element={
            <ProtectedRoute>
              <DashboardPage />
            </ProtectedRoute>
          }
        />
      </Routes>
    </BrowserRouter>
  )
}

export default App
