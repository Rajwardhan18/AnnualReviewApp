import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { get } from '../api/client'
import AuthHero from '../components/AuthHero'
import type { FunctionItem, Role, UserType } from '../types'

export default function RegisterPage() {
  const { register } = useAuth()
  const navigate = useNavigate()

  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [userType, setUserType] = useState<Exclude<UserType, 'Admin'>>('Developer')
  const [functions, setFunctions] = useState<FunctionItem[]>([])
  const [roles, setRoles] = useState<Role[]>([])
  const [functionId, setFunctionId] = useState<number | ''>('')
  const [roleId, setRoleId] = useState<number | ''>('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    get<FunctionItem[]>('/api/functions').then(setFunctions).catch(() => {})
  }, [])

  useEffect(() => {
    setRoleId('')
    if (functionId === '') { setRoles([]); return }
    get<Role[]>(`/api/roles?functionId=${functionId}`).then(setRoles).catch(() => setRoles([]))
  }, [functionId])

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    if (userType === 'Developer' && (functionId === '' || roleId === '')) {
      setError('Developers must choose a function and a role.')
      return
    }
    setBusy(true)
    try {
      await register({
        fullName, email, password, userType,
        functionId: userType === 'Developer' ? Number(functionId) : null,
        roleId: userType === 'Developer' ? Number(roleId) : null,
      })
      navigate('/')
    } catch (err: any) {
      setError(err.message || 'Registration failed')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="auth-wrap">
      <div className="card auth-card">
        <AuthHero />
        <div className="auth-form">
        <h1 className="auth-title">Create your account</h1>
        <p className="muted" style={{ marginTop: 0 }}>Register as a Developer or Manager.</p>
        {error && <div className="error">{error}</div>}
        <form onSubmit={submit}>
          <div className="field">
            <label>Full name</label>
            <input value={fullName} onChange={(e) => setFullName(e.target.value)} required />
          </div>
          <div className="field">
            <label>Email</label>
            <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
          </div>
          <div className="field">
            <label>Password (min 6 chars)</label>
            <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} minLength={6} required />
          </div>
          <div className="field">
            <label>I am a…</label>
            <select value={userType} onChange={(e) => setUserType(e.target.value as any)}>
              <option value="Developer">Developer</option>
              <option value="Manager">Manager</option>
            </select>
          </div>

          {userType === 'Developer' && (
            <div className="grid-2">
              <div className="field">
                <label>Function</label>
                <select value={functionId} onChange={(e) => setFunctionId(e.target.value === '' ? '' : Number(e.target.value))} required>
                  <option value="">Select…</option>
                  {functions.map((f) => <option key={f.id} value={f.id}>{f.name}</option>)}
                </select>
              </div>
              <div className="field">
                <label>Role</label>
                <select value={roleId} onChange={(e) => setRoleId(e.target.value === '' ? '' : Number(e.target.value))} disabled={functionId === ''} required>
                  <option value="">Select…</option>
                  {roles.map((r) => <option key={r.id} value={r.id}>{r.name}</option>)}
                </select>
              </div>
            </div>
          )}

          <button type="submit" disabled={busy} style={{ width: '100%', marginTop: 6 }}>
            {busy ? 'Creating…' : 'Create account'}
          </button>
        </form>
        <p className="muted" style={{ marginTop: 16 }}>
          Already have an account? <Link to="/login">Sign in</Link>
        </p>
        </div>
      </div>
    </div>
  )
}
