import { useEffect, useState } from 'react'
import { get, post, put, ApiError } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import type { FunctionItem, Role, User, UserType } from '../types'

const TYPE_BADGE: Record<UserType, string> = {
  Developer: 'personal',
  Manager: 'pro',
  Admin: 'trait',
}

export default function UsersPage() {
  const { user: me } = useAuth()
  const [users, setUsers] = useState<User[]>([])
  const [filter, setFilter] = useState<'' | UserType>('')
  const [loading, setLoading] = useState(true)
  const [showForm, setShowForm] = useState(false)
  const [resetTarget, setResetTarget] = useState<User | null>(null)
  const [msg, setMsg] = useState('')
  const [err, setErr] = useState('')

  const load = () => get<User[]>('/api/users').then(setUsers)
  useEffect(() => { load().finally(() => setLoading(false)) }, [])

  const setActive = async (u: User, isActive: boolean) => {
    setErr(''); setMsg('')
    try {
      const updated = await put<User>(`/api/users/${u.id}/active`, { isActive })
      setUsers((xs) => xs.map((x) => (x.id === u.id ? updated : x)))
      setMsg(`${updated.fullName} is now ${isActive ? 'active' : 'deactivated'}.`)
    } catch (e: any) {
      setErr(e instanceof ApiError ? e.message : 'Failed to update user')
    }
  }

  const shown = filter === '' ? users : users.filter((u) => u.userType === filter)

  return (
    <>
      <div className="page-head">
        <h1>Users</h1>
        <p>All registered users. Add developers (with function &amp; role), managers, and admins.</p>
      </div>

      <div className="card">
        <div className="btn-row" style={{ justifyContent: 'space-between' }}>
          <div className="btn-row">
            <label style={{ margin: 0 }}>Filter:</label>
            <select style={{ width: 'auto' }} value={filter} onChange={(e) => setFilter(e.target.value as any)}>
              <option value="">All types</option>
              <option value="Developer">Developers</option>
              <option value="Manager">Managers</option>
              <option value="Admin">Admins</option>
            </select>
            <span className="muted">{shown.length} user{shown.length === 1 ? '' : 's'}</span>
          </div>
          <button onClick={() => setShowForm((s) => !s)}>{showForm ? 'Close' : '+ Add user'}</button>
        </div>
      </div>

      {msg && <div className="success">{msg}</div>}
      {err && <div className="error">{err}</div>}
      {showForm && <AddUserForm onCreated={() => { load(); setShowForm(false) }} />}
      {resetTarget && (
        <ResetPasswordForm user={resetTarget}
          onCancel={() => setResetTarget(null)}
          onDone={(m) => { setMsg(m); setResetTarget(null) }} />
      )}

      <div className="card">
        {loading ? <div className="loading">Loading…</div> : (
          <table>
            <thead>
              <tr><th>Name</th><th>Email</th><th>Type</th><th>Function</th><th>Role</th><th>Status</th><th></th></tr>
            </thead>
            <tbody>
              {shown.map((u) => (
                <tr key={u.id} style={{ opacity: u.isActive ? 1 : 0.55 }}>
                  <td><strong>{u.fullName}</strong></td>
                  <td className="muted">{u.email}</td>
                  <td><span className={`badge ${TYPE_BADGE[u.userType]}`}>{u.userType}</span></td>
                  <td>{u.functionName ?? <span className="muted">—</span>}</td>
                  <td>{u.roleName ?? <span className="muted">—</span>}</td>
                  <td><span className={`badge ${u.isActive ? 'Completed' : 'Dropped'}`}>{u.isActive ? 'Active' : 'Inactive'}</span></td>
                  <td style={{ textAlign: 'right' }}>
                    <div className="btn-row" style={{ justifyContent: 'flex-end' }}>
                      {u.isActive
                        ? <button className="danger small" disabled={u.id === me?.id} title={u.id === me?.id ? "You can't deactivate yourself" : undefined} onClick={() => setActive(u, false)}>Deactivate</button>
                        : <button className="secondary small" onClick={() => setActive(u, true)}>Activate</button>}
                      <button className="secondary small" onClick={() => { setMsg(''); setErr(''); setResetTarget(u) }}>Reset password</button>
                    </div>
                  </td>
                </tr>
              ))}
              {shown.length === 0 && <tr><td colSpan={7} className="muted" style={{ textAlign: 'center' }}>No users.</td></tr>}
            </tbody>
          </table>
        )}
      </div>
    </>
  )
}

function ResetPasswordForm({ user, onCancel, onDone }: {
  user: User
  onCancel: () => void
  onDone: (message: string) => void
}) {
  const [pw, setPw] = useState('')
  const [confirm, setConfirm] = useState('')
  const [err, setErr] = useState('')
  const [busy, setBusy] = useState(false)

  const genPassword = () => {
    // Readable temporary password: e.g. "Cyan-Owl-4827".
    const adj = ['Cyan', 'Amber', 'Teal', 'Coral', 'Slate', 'Olive', 'Ruby', 'Indigo']
    const noun = ['Owl', 'Fox', 'Pine', 'Wren', 'Reef', 'Lark', 'Moss', 'Kite']
    const pick = (a: string[]) => a[Math.floor(Math.random() * a.length)]
    const p = `${pick(adj)}-${pick(noun)}-${1000 + Math.floor(Math.random() * 9000)}`
    setPw(p); setConfirm(p); setErr('')
  }

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    setErr('')
    if (pw.length < 6) { setErr('Password must be at least 6 characters.'); return }
    if (pw !== confirm) { setErr('Passwords do not match.'); return }
    setBusy(true)
    try {
      await post(`/api/users/${user.id}/reset-password`, { newPassword: pw })
      onDone(`Password reset for ${user.fullName} (${user.email}). Share the new password securely — they'll be prompted to set their own password at next login.`)
    } catch (e: any) {
      setErr(e instanceof ApiError ? e.message : 'Failed to reset password')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="card" style={{ borderColor: 'var(--primary)' }}>
      <h2>Reset password — {user.fullName}</h2>
      <p className="section-hint">Set a new password for <strong>{user.email}</strong>. This takes effect immediately.</p>
      {err && <div className="error">{err}</div>}
      <form onSubmit={submit}>
        <div className="grid-2">
          <div className="field">
            <label>New password (min 6)</label>
            <input type="text" value={pw} minLength={6} onChange={(e) => setPw(e.target.value)} required autoFocus />
          </div>
          <div className="field">
            <label>Confirm password</label>
            <input type="text" value={confirm} onChange={(e) => setConfirm(e.target.value)} required />
          </div>
        </div>
        <div className="btn-row">
          <button type="submit" disabled={busy}>{busy ? 'Resetting…' : 'Reset password'}</button>
          <button type="button" className="secondary" onClick={genPassword}>Generate</button>
          <button type="button" className="ghost" onClick={onCancel}>Cancel</button>
        </div>
      </form>
    </div>
  )
}

function AddUserForm({ onCreated }: { onCreated: () => void }) {
  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [userType, setUserType] = useState<UserType>('Developer')
  const [functions, setFunctions] = useState<FunctionItem[]>([])
  const [roles, setRoles] = useState<Role[]>([])
  const [functionId, setFunctionId] = useState<number | ''>('')
  const [roleId, setRoleId] = useState<number | ''>('')
  const [err, setErr] = useState('')
  const [msg, setMsg] = useState('')
  const [busy, setBusy] = useState(false)

  useEffect(() => { get<FunctionItem[]>('/api/functions').then(setFunctions).catch(() => {}) }, [])
  useEffect(() => {
    setRoleId('')
    if (functionId === '') { setRoles([]); return }
    get<Role[]>(`/api/roles?functionId=${functionId}`).then(setRoles).catch(() => setRoles([]))
  }, [functionId])

  const submit = async (e: React.FormEvent) => {
    e.preventDefault()
    setErr(''); setMsg('')
    if (userType === 'Developer' && (functionId === '' || roleId === '')) {
      setErr('Developers require a function and a role.'); return
    }
    setBusy(true)
    try {
      await post('/api/users', {
        fullName, email, password, userType,
        functionId: userType === 'Developer' ? Number(functionId) : null,
        roleId: userType === 'Developer' ? Number(roleId) : null,
      })
      setMsg(`${userType} "${fullName}" created.`)
      setFullName(''); setEmail(''); setPassword(''); setFunctionId(''); setRoleId('')
      onCreated()
    } catch (e: any) {
      setErr(e instanceof ApiError ? e.message : 'Failed to create user')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="card">
      <h2>Add a user</h2>
      <p className="section-hint">The user signs in with the email and temporary password you set here, then is prompted to choose their own password at first login.</p>
      {err && <div className="error">{err}</div>}
      {msg && <div className="success">{msg}</div>}
      <form onSubmit={submit}>
        <div className="grid-2">
          <div className="field"><label>Full name</label><input value={fullName} onChange={(e) => setFullName(e.target.value)} required /></div>
          <div className="field"><label>Email</label><input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required /></div>
          <div className="field"><label>Temporary password (min 6)</label><input type="text" value={password} onChange={(e) => setPassword(e.target.value)} minLength={6} required /></div>
          <div className="field">
            <label>User type</label>
            <select value={userType} onChange={(e) => setUserType(e.target.value as UserType)}>
              <option value="Developer">Developer</option>
              <option value="Manager">Manager</option>
              <option value="Admin">Admin</option>
            </select>
          </div>
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
        <button type="submit" disabled={busy}>{busy ? 'Creating…' : 'Create user'}</button>
      </form>
    </div>
  )
}
