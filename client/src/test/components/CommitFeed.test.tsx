// client/src/test/components/CommitFeed.test.tsx
import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import CommitFeed from '../../components/CommitFeed'
import { useCommitsStore } from '../../stores/commitsStore'

describe('CommitFeed', () => {
  it('renders loading state', () => {
    useCommitsStore.setState({ isLoading: true, commits: [] })
    const { container } = render(<CommitFeed />)
    expect(container.querySelector('.animate-pulse')).toBeInTheDocument()
  })

  it('renders commits', () => {
    useCommitsStore.setState({
      isLoading: false,
      commits: [
        { sha: 'abc1234567890', message: 'fix: resolve login bug', authoredAt: '2026-03-12T00:00:00Z', repoName: 'my-repo' },
      ],
    })

    render(<CommitFeed />)
    expect(screen.getByText('fix: resolve login bug')).toBeInTheDocument()
    expect(screen.getByText(/abc1234/)).toBeInTheDocument()
  })
})
