import { create } from 'zustand'
import api from '../lib/api'

interface Commit {
  sha: string
  message: string
  authoredAt: string
  repoName: string
}

interface CommitsState {
  commits: Commit[]
  isLoading: boolean
  error: string | null
  fetchCommits: (limit?: number) => Promise<void>
}

export const useCommitsStore = create<CommitsState>((set) => ({
  commits: [],
  isLoading: false,
  error: null,

  fetchCommits: async (limit = 50) => {
    set({ isLoading: true, error: null })
    try {
      const { data } = await api.get(`/commits?limit=${limit}`)
      set({ commits: data, isLoading: false })
    } catch {
      set({ error: 'Failed to load commits', isLoading: false })
    }
  },
}))
