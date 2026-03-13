import { create } from 'zustand'
import api from '../lib/api'

interface Repo {
  name: string
  fullName: string
  language: string
  stars: number
  commitCount: number
}

interface ReposState {
  repos: Repo[]
  isLoading: boolean
  error: string | null
  fetchRepos: () => Promise<void>
}

export const useReposStore = create<ReposState>((set) => ({
  repos: [],
  isLoading: false,
  error: null,

  fetchRepos: async () => {
    set({ isLoading: true, error: null })
    try {
      const { data } = await api.get('/repos')
      set({ repos: data, isLoading: false })
    } catch {
      set({ error: 'Failed to load repos', isLoading: false })
    }
  },
}))
