import { useState } from 'react'
import { EmployeeManager } from './EmployeeManager'
import { api } from '../api'

interface Props {
  token: string | null
  onLogin: (token: string) => void
}

export function AdminPanel({ token, onLogin }: Props) {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError]       = useState('')
  const [loading, setLoading]   = useState(false)

  if (token) {
    return <EmployeeManager token={token} />
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setLoading(true)
    setError('')
    try {
      const data = await api.login(username, password)
      if (data.token) {
        onLogin(data.token)
      } else {
        setError('ユーザー名またはパスワードが正しくありません。')
      }
    } catch {
      setError('ログインに失敗しました。')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="max-w-sm mx-auto mt-12">
      <div className="bg-white rounded-xl border border-slate-200 shadow-sm p-8">
        <div className="text-center mb-6">
          <span className="text-3xl">🔐</span>
          <h2 className="mt-2 text-lg font-semibold text-slate-800">管理者ログイン</h2>
          <p className="text-sm text-slate-500 mt-1">社員管理・打刻修正は管理者のみ操作可</p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">
              ユーザー名
            </label>
            <input
              type="text"
              value={username}
              onChange={e => setUsername(e.target.value)}
              autoComplete="username"
              placeholder="admin"
              className="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm
                         focus:outline-none focus:ring-2 focus:ring-emerald-500"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">
              パスワード
            </label>
            <input
              type="password"
              value={password}
              onChange={e => setPassword(e.target.value)}
              autoComplete="current-password"
              placeholder="••••••••"
              className="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm
                         focus:outline-none focus:ring-2 focus:ring-emerald-500"
            />
          </div>

          {error && <p className="text-sm text-red-600">{error}</p>}

          <button
            type="submit"
            disabled={loading || !username || !password}
            className="w-full py-2 bg-emerald-600 text-white rounded-lg text-sm font-medium
                       hover:bg-emerald-700 disabled:opacity-50 disabled:cursor-not-allowed
                       transition-colors"
          >
            {loading ? 'ログイン中...' : 'ログイン'}
          </button>
        </form>

        <p className="text-xs text-slate-400 text-center mt-5">
          デモ: <span className="font-mono">admin</span> /{' '}
          <span className="font-mono">admin123</span>
        </p>
      </div>
    </div>
  )
}
