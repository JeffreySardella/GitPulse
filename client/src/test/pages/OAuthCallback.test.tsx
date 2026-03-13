// client/src/test/pages/OAuthCallback.test.tsx
import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import OAuthCallback from '../../pages/OAuthCallback'

const mockNavigate = vi.fn()
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return { ...actual, useNavigate: () => mockNavigate }
})

describe('OAuthCallback', () => {
  it('shows authenticating message', () => {
    render(
      <MemoryRouter initialEntries={['/callback?code=test']}>
        <OAuthCallback />
      </MemoryRouter>
    )
    expect(screen.getByText('Authenticating...')).toBeInTheDocument()
  })

  it('redirects to / when no code param', () => {
    render(
      <MemoryRouter initialEntries={['/callback']}>
        <OAuthCallback />
      </MemoryRouter>
    )
    expect(mockNavigate).toHaveBeenCalledWith('/')
  })
})
