import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { get } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import type { ReviewSummary } from '../types'

function StatusBadge({ s }: { s: string }) {
  return <span className={`badge ${s}`}>{s}</span>
}

function ReviewTable({ rows, showDeveloper, actionFor }: {
  rows: ReviewSummary[]
  showDeveloper: boolean
  actionFor: (r: ReviewSummary) => React.ReactNode
}) {
  if (rows.length === 0) return <div className="empty">Nothing here yet.</div>
  return (
    <table>
      <thead>
        <tr>
          <th>Cycle</th>
          {showDeveloper && <th>Developer</th>}
          <th>Function / Role</th>
          <th>Status</th>
          <th>Submitted</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        {rows.map((r) => (
          <tr key={r.id}>
            <td>{r.cycleName}</td>
            {showDeveloper && <td>{r.developerName}</td>}
            <td>{r.functionName ? `${r.functionName} · ${r.roleName}` : <span className="muted">—</span>}</td>
            <td><StatusBadge s={r.status} /></td>
            <td>{r.submittedAt ? new Date(r.submittedAt).toLocaleDateString() : <span className="muted">—</span>}</td>
            <td>{actionFor(r)}</td>
          </tr>
        ))}
      </tbody>
    </table>
  )
}

export default function DashboardPage() {
  const { user } = useAuth()
  const [mine, setMine] = useState<ReviewSummary[]>([])
  const [assigned, setAssigned] = useState<ReviewSummary[]>([])
  const [all, setAll] = useState<ReviewSummary[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const jobs: Promise<any>[] = []
    if (user?.userType === 'Developer') {
      jobs.push(get<ReviewSummary[]>('/api/reviews/mine').then(setMine).catch(() => {}))
    }
    if (user?.userType === 'Developer' || user?.userType === 'Manager') {
      jobs.push(get<ReviewSummary[]>('/api/reviews/assigned').then(setAssigned).catch(() => {}))
    }
    if (user?.userType === 'Admin') {
      jobs.push(get<ReviewSummary[]>('/api/reviews').then(setAll).catch(() => {}))
    }
    Promise.all(jobs).finally(() => setLoading(false))
  }, [user])

  if (loading) return <div className="loading">Loading…</div>

  return (
    <>
      <div className="page-head">
        <h1>Dashboard</h1>
        <p>Welcome, {user?.fullName}. Here is your plan &amp; review activity.</p>
      </div>

      {user?.userType === 'Developer' && (
        <div className="card">
          <h2>My Annual Reviews</h2>
          <p className="section-hint">Complete your SMART plan for each released cycle and submit it at the start of the cycle.</p>
          <ReviewTable
            rows={mine}
            showDeveloper={false}
            actionFor={(r) => (
              <Link className="btn small" to={r.status === 'Draft' ? `/reviews/${r.id}/edit` : `/reviews/${r.id}`}
                style={{ color: '#fff' }}>
                {r.status === 'Draft' ? 'Fill plan' : 'View'}
              </Link>
            )}
          />
        </div>
      )}

      {(user?.userType === 'Developer' || user?.userType === 'Manager') && (
        <div className="card">
          <h2>Assigned to me for review</h2>
          <p className="section-hint">Reviews where you are an assigned manager or peer. Provide your rating and feedback.</p>
          <ReviewTable
            rows={assigned}
            showDeveloper
            actionFor={(r) => <Link className="btn small" to={`/reviews/${r.id}`} style={{ color: '#fff' }}>Review &amp; rate</Link>}
          />
        </div>
      )}

      {user?.userType === 'Admin' && (
        <>
          <div className="card">
            <h2>Admin overview</h2>
            <p className="section-hint">Manage master data, release cycles, and assign reviewers.</p>
            <div className="btn-row">
              <Link className="btn" to="/admin" style={{ color: '#fff' }}>Open Admin Console</Link>
            </div>
          </div>
          <div className="card">
            <h2>All reviews</h2>
            <ReviewTable
              rows={all}
              showDeveloper
              actionFor={(r) => <Link className="btn small" to={`/reviews/${r.id}`} style={{ color: '#fff' }}>Open</Link>}
            />
          </div>
        </>
      )}
    </>
  )
}
