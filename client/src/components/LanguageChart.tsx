// client/src/components/LanguageChart.tsx
import { Bar } from 'react-chartjs-2'
import { useLanguagesStore } from '../stores/languagesStore'
import '../lib/chartSetup'

const COLORS = [
  '#3b82f6', '#22c55e', '#eab308', '#ef4444', '#a855f7',
  '#ec4899', '#f97316', '#06b6d4', '#84cc16', '#6366f1',
]

export default function LanguageChart() {
  const { languages, isLoading } = useLanguagesStore()

  if (isLoading) return <div className="bg-gray-900 rounded-lg p-6 border border-gray-800 h-64 animate-pulse" />

  const data = {
    labels: languages.map((l) => l.language),
    datasets: [
      {
        label: 'Repositories',
        data: languages.map((l) => l.repoCount),
        backgroundColor: COLORS.slice(0, languages.length),
        borderWidth: 0,
        borderRadius: 4,
      },
    ],
  }

  const options = {
    indexAxis: 'y' as const,
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false },
    },
    scales: {
      x: {
        ticks: { color: '#9ca3af', stepSize: 1 },
        grid: { color: '#1f2937' },
      },
      y: {
        ticks: { color: '#9ca3af' },
        grid: { display: false },
      },
    },
  }

  return (
    <div className="bg-gray-900 rounded-lg p-6 border border-gray-800">
      <h2 className="text-lg font-semibold text-white mb-4">Languages</h2>
      <div className="h-64">
        <Bar data={data} options={options} />
      </div>
    </div>
  )
}
