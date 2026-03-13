// client/src/test/components/SummaryCards.test.tsx
import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import SummaryCards from '../../components/SummaryCards'
import { useStatsStore } from '../../stores/statsStore'
import { useReposStore } from '../../stores/reposStore'

describe('SummaryCards', () => {
  it('renders summary values', () => {
    useStatsStore.setState({ totalCommits: 42, totalRepos: 5 })
    useReposStore.setState({ repos: [
      { name: 'r1', fullName: 'user/r1', language: 'TS', stars: 10, commitCount: 5 },
    ]})

    render(<SummaryCards />)

    expect(screen.getByText('42')).toBeInTheDocument()
    expect(screen.getByText('5')).toBeInTheDocument()
    expect(screen.getByText('10')).toBeInTheDocument()
  })
})
