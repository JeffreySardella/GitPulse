import { create } from 'zustand'
import api, { setAccessToken } from '../lib/api'

interface AuthState {
  isAuthenticated: boolean
  isLoading: boolean
  login: (code: string) => Promise<void>
  logout: () => void
  restoreSession: () => Promise<boolean>
}

export const useAuthStore = create<AuthState>((set) => ({
  isAuthenticated: false,
  isLoading: false,

  login: async (code: string) => {
    set({ isLoading: true })
    try {
      const { data } = await api.post('/auth/github', { code })
      setAccessToken(data.accessToken)
      sessionStorage.setItem('refreshToken', data.refreshToken)
      set({ isAuthenticated: true, isLoading: false })
    } catch {
      set({ isAuthenticated: false, isLoading: false })
    }
  },

  logout: () => {
    setAccessToken(null)
    sessionStorage.removeItem('refreshToken')
    set({ isAuthenticated: false })
  },

  restoreSession: async () => {
    const refreshToken = sessionStorage.getItem('refreshToken')
    if (!refreshToken) return false

    set({ isLoading: true })
    try {
      const { data } = await api.post('/auth/refresh', { refreshToken })
      setAccessToken(data.accessToken)
      sessionStorage.setItem('refreshToken', data.refreshToken)
      set({ isAuthenticated: true, isLoading: false })
      return true
    } catch {
      sessionStorage.removeItem('refreshToken')
      set({ isAuthenticated: false, isLoading: false })
      return false
    }
  },
}))
