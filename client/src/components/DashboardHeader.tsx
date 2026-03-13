// client/src/components/DashboardHeader.tsx
import { useAuthStore } from '../stores/authStore'

export default function DashboardHeader() {
  const logout = useAuthStore((s) => s.logout)

  return (
    <header className="flex items-center justify-between px-8 py-4 border-b border-gray-800">
      <h1 className="text-xl font-bold text-white">GitPulse</h1>
      <button
        onClick={logout}
        className="text-sm text-gray-400 hover:text-white transition-colors"
      >
        Sign out
      </button>
    </header>
  )
}
