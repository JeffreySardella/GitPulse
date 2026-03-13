// client/src/pages/LoginPage.tsx
const GITHUB_CLIENT_ID = import.meta.env.VITE_GITHUB_CLIENT_ID

export default function LoginPage() {
  const githubAuthUrl = `https://github.com/login/oauth/authorize?client_id=${GITHUB_CLIENT_ID}&scope=read:user,repo`

  return (
    <div className="min-h-screen bg-gray-950 flex items-center justify-center">
      <div className="text-center">
        <h1 className="text-4xl font-bold text-white mb-2">GitPulse</h1>
        <p className="text-gray-400 mb-8">Developer analytics dashboard</p>
        <a
          href={githubAuthUrl}
          className="inline-flex items-center gap-2 bg-white text-gray-900 font-semibold px-6 py-3 rounded-lg hover:bg-gray-200 transition-colors"
        >
          Sign in with GitHub
        </a>
      </div>
    </div>
  )
}
