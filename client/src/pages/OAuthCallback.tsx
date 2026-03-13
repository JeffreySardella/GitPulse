// client/src/pages/OAuthCallback.tsx
import { useEffect } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useAuthStore } from '../stores/authStore'

export default function OAuthCallback() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const login = useAuthStore((s) => s.login)

  useEffect(() => {
    const code = searchParams.get('code')
    if (!code) {
      navigate('/')
      return
    }

    login(code).then(() => {
      const { isAuthenticated } = useAuthStore.getState()
      if (isAuthenticated) {
        navigate('/dashboard')
      } else {
        navigate('/?error=auth_failed')
      }
    })
  }, [searchParams, login, navigate])

  return (
    <div className="min-h-screen bg-gray-950 flex items-center justify-center">
      <p className="text-gray-400">Authenticating...</p>
    </div>
  )
}
