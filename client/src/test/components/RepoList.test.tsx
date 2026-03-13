// client/src/test/components/RepoList.test.tsx
import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import RepoList from '../../components/RepoList'
import { useReposStore } from '../../stores/reposStore'

describe('RepoList', () => {
  it('renders loading state', () => {
    useReposStore.setState({ isLoading: true, repos: [] })
    const { container } = render(<RepoList />)
    expect(container.querySelector('.animate-pulse')).toBeInTheDocument()
  })

  it('renders repos', () => {
    useReposStore.setState({
      isLoading: false,
      repos: [
        { name: 'my-repo', fullName: 'user/my-repo', language: 'TypeScript', stars: 3, commitCount: 10 },
      ],
    })

    render(<RepoList />)
    expect(screen.getByText('my-repo')).toBeInTheDocument()
  })
})
