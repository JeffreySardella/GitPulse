import { create } from 'zustand'
import api from '../lib/api'

interface DailySnapshot {
  date: string
  commitCount: number
  activeRepos: number
}

interface StatsState {
  totalCommits: number
  totalRepos: number
  dailySnapshots: DailySnapshot[]
  isLoading: boolean
  error: string | null
  fetchStats: () => Promise<void>
}

export const useStatsStore = create<StatsState>((set) => ({
  totalCommits: 0,
  totalRepos: 0,
  dailySnapshots: [],
  isLoading: false,
  error: null,

  fetchStats: async () => {
    set({ isLoading: true, error: null })
    try {
      const { data } = await api.get('/stats')
      set({
        totalCommits: data.totalCommits,
        totalRepos: data.totalRepos,
        dailySnapshots: data.dailySnapshots,
        isLoading: false,
      })
    } catch (err) {
      set({ error: 'Failed to load stats', isLoading: false })
    }
  },
}))
