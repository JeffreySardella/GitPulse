import { describe, it, expect, beforeEach } from 'vitest'
import { useStatsStore } from '../../stores/statsStore'
import { useCommitsStore } from '../../stores/commitsStore'
import { useLanguagesStore } from '../../stores/languagesStore'
import { useReposStore } from '../../stores/reposStore'

describe('data stores initial state', () => {
  it('statsStore starts empty', () => {
    const state = useStatsStore.getState()
    expect(state.totalCommits).toBe(0)
    expect(state.dailySnapshots).toEqual([])
    expect(state.isLoading).toBe(false)
  })

  it('commitsStore starts empty', () => {
    const state = useCommitsStore.getState()
    expect(state.commits).toEqual([])
    expect(state.isLoading).toBe(false)
  })

  it('languagesStore starts empty', () => {
    const state = useLanguagesStore.getState()
    expect(state.languages).toEqual([])
    expect(state.isLoading).toBe(false)
  })

  it('reposStore starts empty', () => {
    const state = useReposStore.getState()
    expect(state.repos).toEqual([])
    expect(state.isLoading).toBe(false)
  })
})
