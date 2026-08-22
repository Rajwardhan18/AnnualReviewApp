import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import AuthHero from '../components/AuthHero'

export default function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('admin@company.com')
  const [password, setPassword] = useState('Admin@123')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setBusy(true)
    try {
      await login(email, password)
      navigate('/')
    } catch (err: any) {
      setError(err.message || 'Login failed')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="auth-wrap">
      <div className="card auth-card">
        <AuthHero />

        <div className="auth-form">
          <h1 className="auth-title" style={{ textAlign: 'center' }}>Welcome to ARISe</h1>
          <p className="muted" style={{ marginTop: 0, textAlign: 'center' }}>To Infinity and Beyond</p>
          {error && <div className="error">{error}</div>}
          <form onSubmit={submit}>
            <div className="field">
              <label>Email</label>
              <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required autoFocus />
            </div>
            <div className="field">
              <label>Password</label>
              <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
            </div>
            <button type="submit" disabled={busy} style={{ width: '100%' }}>
              {busy ? 'Signing in…' : 'Sign in'}
            </button>
          </form>
          <p className="muted" style={{ marginTop: 16 }}>
            New here? <Link to="/register">Create an account</Link>
          </p>
          <p className="muted" style={{ fontSize: 12 }}>
            Seeded admin: <code>admin@company.com</code> / <code>Admin@123</code>
          </p>
        </div>
      </div>
    </div>
  )
}
