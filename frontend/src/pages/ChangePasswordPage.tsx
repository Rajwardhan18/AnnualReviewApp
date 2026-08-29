import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { post, ApiError } from '../api/client'
import AuthHero from '../components/AuthHero'
import type { User } from '../types'

export default function ChangePasswordPage() {
  const { user, applyUser, logout } = useAuth()
  const navigate = useNavigate()
  const forced = !!user?.mustChangePassword

  const [current, setCurrent] = useState('')
  const [next, setNext] = useState('')
  const [confirm, setConfirm] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    if (next.length < 6) { setError('Your new password must be at least 6 characters.'); return }
    if (next !== confirm) { setError('The new passwords do not match.'); return }
    if (next === current) { setError('Your new password must be different from the current one.'); return }
    setBusy(true)
    try {
      const updated = await post<User>('/api/auth/change-password', { currentPassword: current, newPassword: next })
      applyUser(updated)
      navigate('/')
    } catch (err: any) {
      if (err instanceof ApiError && err.body?.message) setError(err.body.message)
      else setError(err.message || 'Could not change your password.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="auth-wrap">
      <div className="card auth-card">
        <AuthHero />

        <div className="auth-form">
          <h1 className="auth-title">{forced ? 'Set a new password' : 'Change your password'}</h1>
          <p className="muted" style={{ marginTop: 4 }}>
            {forced
              ? 'For your security, choose a new password before continuing.'
              : 'Update the password you use to sign in.'}
          </p>
          {error && <div className="error">{error}</div>}
          <form onSubmit={submit}>
            <div className="field">
              <label>{forced ? 'Temporary password' : 'Current password'}</label>
              <input type="password" value={current} onChange={(e) => setCurrent(e.target.value)} required autoFocus />
            </div>
            <div className="field">
              <label>New password</label>
              <input type="password" value={next} onChange={(e) => setNext(e.target.value)} required minLength={6} />
              <span className="field-help">At least 6 characters.</span>
            </div>
            <div className="field">
              <label>Confirm new password</label>
              <input type="password" value={confirm} onChange={(e) => setConfirm(e.target.value)} required />
            </div>
            <button type="submit" disabled={busy} style={{ width: '100%' }}>
              {busy ? 'Saving…' : forced ? 'Set password & continue' : 'Change password'}
            </button>
          </form>
          <p className="muted" style={{ marginTop: 16 }}>
            <button className="ghost small" onClick={() => { logout(); navigate('/login') }}>Sign out</button>
          </p>
        </div>
      </div>
    </div>
  )
}
