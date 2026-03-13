// client/src/pages/DashboardPage.tsx
import { useEffect } from 'react'
import DashboardHeader from '../components/DashboardHeader'
import SummaryCards from '../components/SummaryCards'
import CommitHeatmap from '../components/CommitHeatmap'
import LanguageChart from '../components/LanguageChart'
import { useStatsStore } from '../stores/statsStore'
import { useCommitsStore } from '../stores/commitsStore'
import { useLanguagesStore } from '../stores/languagesStore'
import { useReposStore } from '../stores/reposStore'

export default function DashboardPage() {
  const fetchStats = useStatsStore((s) => s.fetchStats)
  const fetchCommits = useCommitsStore((s) => s.fetchCommits)
  const fetchLanguages = useLanguagesStore((s) => s.fetchLanguages)
  const fetchRepos = useReposStore((s) => s.fetchRepos)

  useEffect(() => {
    fetchStats()
    fetchCommits()
    fetchLanguages()
    fetchRepos()
  }, [fetchStats, fetchCommits, fetchLanguages, fetchRepos])

  return (
    <div className="min-h-screen bg-gray-950 text-gray-100">
      <DashboardHeader />
      <main className="max-w-7xl mx-auto px-8 py-6 space-y-6">
        <SummaryCards />
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2">
            <CommitHeatmap />
          </div>
          <LanguageChart />
        </div>
        {/* RepoList, CommitFeed go here */}
      </main>
    </div>
  )
}
