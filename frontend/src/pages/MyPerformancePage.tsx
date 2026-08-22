import { useEffect, useState } from 'react'
import { get } from '../api/client'
import type { GoalStatus, MyPerformance, MyPerformanceCycle } from '../types'

const BAND_BADGE: Record<string, { bg: string; fg: string }> = {
  'Outstanding': { bg: '#e2f6ec', fg: '#167a4c' },
  'Exceeds': { bg: '#e6f0ff', fg: '#2f5fd0' },
  'Meets': { bg: '#eef1f6', fg: '#62708a' },
  'Below': { bg: '#fff3dc', fg: '#a86b00' },
  'Needs Improvement': { bg: '#fdecec', fg: '#a02929' },
}
const STATUS_LABEL: Record<GoalStatus, string> = {
  NotStarted: 'Not Started', InProgress: 'In Progress', Completed: 'Completed', Dropped: 'Dropped',
}

function Stat({ label, value, sub }: { label: string; value: React.ReactNode; sub?: string }) {
  return (
    <div className="card" style={{ margin: 0, flex: '1 1 150px' }}>
      <div className="muted" style={{ fontSize: 12, textTransform: 'uppercase', letterSpacing: '.04em', fontWeight: 600 }}>{label}</div>
      <div style={{ fontSize: 28, fontWeight: 800, lineHeight: 1.1, marginTop: 6 }}>{value}</div>
      {sub && <div className="muted" style={{ fontSize: 12, marginTop: 2 }}>{sub}</div>}
    </div>
  )
}

function CycleBlock({ c }: { c: MyPerformanceCycle }) {
  const band = c.band ? BAND_BADGE[c.band] ?? BAND_BADGE['Meets'] : null
  return (
    <>
      <div className="page-head" style={{ marginTop: 8 }}>
        <h1 style={{ fontSize: 18 }}>{c.cycleName}</h1>
        <p><span className={`badge ${c.status}`}>{c.status}</span></p>
      </div>

      {/* Ratings — gated on release */}
      {c.ratingsReleased ? (
        <>
          <div style={{ display: 'flex', gap: 14, flexWrap: 'wrap', marginBottom: 18 }}>
            <Stat label="Overall average" value={c.overallAverage ?? '—'} sub="mean of self, peer & manager ratings" />
            <Stat label="Performance band" value={band ? <span className="badge" style={{ background: band.bg, color: band.fg, fontSize: 16, padding: '4px 12px' }}>{c.band}</span> : '—'} />
            <Stat label="Team average" value={c.teamAverage ?? '—'} sub="cohort mean for context" />
          </div>
          <div className="card">
            <h3>Rating breakdown</h3>
            <table>
              <thead><tr><th>Source</th><th>Score</th></tr></thead>
              <tbody>
                <tr><td>Self</td><td>{c.selfScore ?? '—'}</td></tr>
                <tr><td>Peer</td><td>{c.peerScore ?? '—'}</td></tr>
                <tr><td>Manager 1</td><td>{c.manager1Score ?? '—'}</td></tr>
                <tr><td>Manager 2</td><td>{c.manager2Score ?? '—'}</td></tr>
              </tbody>
            </table>
            <p className="section-hint" style={{ marginTop: 10, marginBottom: 0 }}>Reviewer identities are not shown.</p>
          </div>
        </>
      ) : (
        <div className="card">
          <div className="empty">Your ratings for this cycle haven't been released yet. You'll see them here once the admin closes the cycle.</div>
        </div>
      )}

      {/* Self-progress — always available */}
      <div className="card">
        <h3>My goal progress</h3>
        <div style={{ display: 'flex', gap: 14, flexWrap: 'wrap', marginBottom: 16 }}>
          <Stat label="Avg completion" value={`${c.avgCompletion}%`} />
          <Stat label="Completed" value={c.completed} sub={`of ${c.goalCount} goals`} />
          <Stat label="In progress" value={c.inProgress} />
          <Stat label="Not started" value={c.notStarted} />
        </div>
        {c.goals.length === 0 ? <p className="muted">No goals yet.</p> : (
          <div style={{ overflowX: 'auto' }}>
            <table>
              <thead><tr><th>Goal</th><th>Type</th><th>Status</th><th style={{ width: 220 }}>Completion</th></tr></thead>
              <tbody>
                {c.goals.map((g) => (
                  <tr key={g.id}>
                    <td><strong>{g.title || 'Untitled'}</strong>{g.target ? <div className="muted" style={{ fontSize: 12 }}>Target: {g.target}</div> : null}</td>
                    <td><span className={`badge ${g.goalType === 'Professional' ? 'pro' : 'personal'}`}>{g.goalType}</span></td>
                    <td><span className={`badge ${g.status}`}>{STATUS_LABEL[g.status]}</span></td>
                    <td>
                      <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                        <div style={{ flex: 1, height: 8, background: 'var(--line)', borderRadius: 999 }}>
                          <div style={{ width: `${g.completionPercentage}%`, height: '100%', background: 'var(--primary)', borderRadius: 999 }} />
                        </div>
                        <span className="muted" style={{ fontSize: 12, minWidth: 34, textAlign: 'right' }}>{g.completionPercentage}%</span>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </>
  )
}

export default function MyPerformancePage() {
  const [data, setData] = useState<MyPerformance | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    get<MyPerformance>('/api/performance/me').then(setData).finally(() => setLoading(false))
  }, [])

  if (loading) return <div className="loading">Loading…</div>

  return (
    <>
      <div className="page-head">
        <h1>My Performance</h1>
        <p>Your self-progress and — once released — your ratings and overall average.</p>
      </div>
      {!data || data.cycles.length === 0 ? (
        <div className="card"><div className="empty">No review cycles yet.</div></div>
      ) : (
        data.cycles.map((c) => <CycleBlock key={c.reviewId} c={c} />)
      )}
    </>
  )
}
