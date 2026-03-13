import { create } from 'zustand'
import api from '../lib/api'

interface Language {
  language: string
  repoCount: number
}

interface LanguagesState {
  languages: Language[]
  isLoading: boolean
  error: string | null
  fetchLanguages: () => Promise<void>
}

export const useLanguagesStore = create<LanguagesState>((set) => ({
  languages: [],
  isLoading: false,
  error: null,

  fetchLanguages: async () => {
    set({ isLoading: true, error: null })
    try {
      const { data } = await api.get('/languages')
      set({ languages: data, isLoading: false })
    } catch {
      set({ error: 'Failed to load languages', isLoading: false })
    }
  },
}))
