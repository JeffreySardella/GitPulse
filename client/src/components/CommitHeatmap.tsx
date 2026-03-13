// client/src/components/CommitHeatmap.tsx
import { Bar } from 'react-chartjs-2'
import { useStatsStore } from '../stores/statsStore'
import '../lib/chartSetup'

export default function CommitHeatmap() {
  const { dailySnapshots, isLoading } = useStatsStore()

  if (isLoading) return <div className="bg-gray-900 rounded-lg p-6 border border-gray-800 h-64 animate-pulse" />

  const labels = dailySnapshots.map((s) => {
    const date = new Date(s.date)
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
  })

  const data = {
    labels,
    datasets: [
      {
        label: 'Commits',
        data: dailySnapshots.map((s) => s.commitCount),
        backgroundColor: 'rgba(34, 197, 94, 0.6)',
        borderColor: 'rgba(34, 197, 94, 1)',
        borderWidth: 1,
        borderRadius: 2,
      },
    ],
  }

  const options = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false },
    },
    scales: {
      x: {
        ticks: { color: '#9ca3af', maxTicksLimit: 15 },
        grid: { display: false },
      },
      y: {
        ticks: { color: '#9ca3af' },
        grid: { color: '#1f2937' },
      },
    },
  }

  return (
    <div className="bg-gray-900 rounded-lg p-6 border border-gray-800">
      <h2 className="text-lg font-semibold text-white mb-4">Commit Activity (90 days)</h2>
      <div className="h-64">
        <Bar data={data} options={options} />
      </div>
    </div>
  )
}
