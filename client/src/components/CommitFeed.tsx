// client/src/components/CommitFeed.tsx
import { useCommitsStore } from '../stores/commitsStore'

export default function CommitFeed() {
  const { commits, isLoading } = useCommitsStore()

  if (isLoading) return <div className="bg-gray-900 rounded-lg p-6 border border-gray-800 h-64 animate-pulse" />

  return (
    <div className="bg-gray-900 rounded-lg border border-gray-800">
      <h2 className="text-lg font-semibold text-white p-6 pb-0">Recent Commits</h2>
      <div className="divide-y divide-gray-800">
        {commits.slice(0, 20).map((commit) => (
          <div key={commit.sha} className="px-6 py-3">
            <div className="flex items-center justify-between">
              <p className="text-sm text-white font-mono truncate max-w-md">
                {commit.message}
              </p>
              <span className="text-xs text-gray-500 shrink-0 ml-4">
                {new Date(commit.authoredAt).toLocaleDateString()}
              </span>
            </div>
            <p className="text-xs text-gray-500 mt-1">
              {commit.repoName} &middot; {commit.sha.slice(0, 7)}
            </p>
          </div>
        ))}
      </div>
    </div>
  )
}
