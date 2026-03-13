import { describe, it, expect, beforeEach } from 'vitest'
import { useAuthStore } from '../../stores/authStore'

describe('authStore', () => {
  beforeEach(() => {
    useAuthStore.setState({
      isAuthenticated: false,
      isLoading: false,
    })
    sessionStorage.clear()
  })

  it('starts unauthenticated', () => {
    const state = useAuthStore.getState()
    expect(state.isAuthenticated).toBe(false)
  })

  it('logout clears auth state', () => {
    useAuthStore.setState({ isAuthenticated: true })
    useAuthStore.getState().logout()

    const state = useAuthStore.getState()
    expect(state.isAuthenticated).toBe(false)
  })

  it('restoreSession returns false when no refresh token', async () => {
    const result = await useAuthStore.getState().restoreSession()

    expect(result).toBe(false)
    expect(useAuthStore.getState().isAuthenticated).toBe(false)
  })
})
