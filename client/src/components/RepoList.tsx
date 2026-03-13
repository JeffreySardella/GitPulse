// client/src/components/RepoList.tsx
import { useReposStore } from '../stores/reposStore'

export default function RepoList() {
  const { repos, isLoading } = useReposStore()

  if (isLoading) return <div className="bg-gray-900 rounded-lg p-6 border border-gray-800 h-64 animate-pulse" />

  return (
    <div className="bg-gray-900 rounded-lg border border-gray-800">
      <h2 className="text-lg font-semibold text-white p-6 pb-0">Repositories</h2>
      <div className="divide-y divide-gray-800">
        {repos.map((repo) => (
          <div key={repo.name} className="px-6 py-4 flex items-center justify-between">
            <div>
              <p className="font-medium text-white">{repo.name}</p>
            </div>
            <div className="flex items-center gap-4 text-sm text-gray-400 shrink-0">
              <span className="flex items-center gap-1">
                <span className="w-3 h-3 rounded-full bg-blue-500 inline-block" />
                {repo.language}
              </span>
              <span>{repo.commitCount} commits</span>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
