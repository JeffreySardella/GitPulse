// client/src/components/SummaryCards.tsx
import { useStatsStore } from '../stores/statsStore'
import { useReposStore } from '../stores/reposStore'

export default function SummaryCards() {
  const { totalCommits, totalRepos } = useStatsStore()
  const { repos } = useReposStore()

  const totalStars = repos.reduce((sum, r) => sum + r.stars, 0)

  const cards = [
    { label: 'Total Commits (90d)', value: totalCommits },
    { label: 'Repositories', value: totalRepos },
    { label: 'Total Stars', value: totalStars },
  ]

  return (
    <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
      {cards.map((card) => (
        <div key={card.label} className="bg-gray-900 rounded-lg p-6 border border-gray-800">
          <p className="text-sm text-gray-400">{card.label}</p>
          <p className="text-3xl font-bold text-white mt-1">{card.value}</p>
        </div>
      ))}
    </div>
  )
}
