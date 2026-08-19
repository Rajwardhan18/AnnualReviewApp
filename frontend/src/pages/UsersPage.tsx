import { useEffect, useState } from 'react'
import { get, post, ApiError } from '../api/client'
import type { FunctionItem, Role, User, UserType } from '../types'

const TYPE_BADGE: Record<UserType, string> = {
  Developer: 'personal',
  Manager: 'pro',
  Admin: 'trait',
}

export default function UsersPage() {
  const [users, setUsers] = useState<User[]>([])
  const [filter, setFilter] = useState<'' | UserType>('')
  const [loading, setLoading] = useState(true)
  const [showForm, setShowForm] = useState(false)

  const load = () => get<User[]>('/api/users').then(setUsers)
  useEffect(() => { load().finally(() => setLoading(false)) }, [])

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

      {showForm && <AddUserForm onCreated={() => { load(); setShowForm(false) }} />}

      <div className="card">
        {loading ? <div className="loading">Loading…</div> : (
          <table>
            <thead>
              <tr><th>Name</th><th>Email</th><th>Type</th><th>Function</th><th>Role</th></tr>
            </thead>
            <tbody>
              {shown.map((u) => (
                <tr key={u.id}>
                  <td><strong>{u.fullName}</strong></td>
                  <td className="muted">{u.email}</td>
                  <td><span className={`badge ${TYPE_BADGE[u.userType]}`}>{u.userType}</span></td>
                  <td>{u.functionName ?? <span className="muted">—</span>}</td>
                  <td>{u.roleName ?? <span className="muted">—</span>}</td>
                </tr>
              ))}
              {shown.length === 0 && <tr><td colSpan={5} className="muted" style={{ textAlign: 'center' }}>No users.</td></tr>}
            </tbody>
          </table>
        )}
      </div>
    </>
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
      <p className="section-hint">The user can sign in immediately with the email and password you set here.</p>
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
