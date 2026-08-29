import { useEffect, useState } from 'react'
import { get, post } from '../api/client'
import type { AppNotification, NotificationType } from '../types'

const TYPE_LABEL: Record<NotificationType, string> = {
  PlanReleased: 'Plan released',
  HalfYearlyReleased: 'Half-yearly',
  FinalReviewReleased: 'Year-end review',
  ReviewerAssigned: 'Assignment',
  Reminder: 'Reminder',
  RatingsReleased: 'Ratings released',
}
const TYPE_BADGE: Record<NotificationType, string> = {
  PlanReleased: 'pro',
  HalfYearlyReleased: 'InProgress',
  FinalReviewReleased: 'InProgress',
  ReviewerAssigned: 'trait',
  Reminder: 'personal',
  RatingsReleased: 'Completed',
}

export default function NotificationsPage() {
  const [items, setItems] = useState<AppNotification[]>([])
  const [loading, setLoading] = useState(true)

  const load = () => get<AppNotification[]>('/api/notifications/mine').then(setItems)
  useEffect(() => { load().finally(() => setLoading(false)) }, [])

  const markRead = async (id: number) => {
    await post(`/api/notifications/${id}/read`)
    setItems((xs) => xs.map((n) => (n.id === id ? { ...n, isRead: true } : n)))
  }
  const markAll = async () => {
    await post('/api/notifications/read-all')
    setItems((xs) => xs.map((n) => ({ ...n, isRead: true })))
  }

  const unread = items.filter((n) => !n.isRead).length

  return (
    <>
      <div className="page-head">
        <h1>Notifications</h1>
        <p>Plan releases, half-yearly checkpoints and reviewer assignments.</p>
      </div>

      <div className="card">
        <div className="btn-row" style={{ justifyContent: 'space-between' }}>
          <span className="muted">{unread} unread · {items.length} total</span>
          <button className="secondary small" disabled={unread === 0} onClick={markAll}>Mark all read</button>
        </div>
      </div>

      {loading ? <div className="loading">Loading…</div> : items.length === 0 ? (
        <div className="card"><div className="empty">No notifications yet.</div></div>
      ) : (
        items.map((n) => (
          <div key={n.id} className="card" style={{ borderLeft: n.isRead ? undefined : '3px solid var(--primary)' }}>
            <div className="goal-top" style={{ marginBottom: 8 }}>
              <div className="btn-row">
                <span className={`badge ${TYPE_BADGE[n.type]}`}>{TYPE_LABEL[n.type]}</span>
                <strong>{n.subject}</strong>
                {!n.isRead && <span className="badge Submitted">new</span>}
              </div>
              <span className="muted" style={{ fontSize: 12 }}>{new Date(n.createdAt).toLocaleString()}</span>
            </div>
            <pre style={{ margin: 0, whiteSpace: 'pre-wrap', fontFamily: 'inherit', color: 'var(--ink)', fontSize: 13.5 }}>{n.body}</pre>
            <div className="btn-row" style={{ marginTop: 10 }}>
              {!n.emailSent && <span className="muted" style={{ fontSize: 12 }}>· in-app only (email disabled)</span>}
              {!n.isRead && <button className="ghost small" onClick={() => markRead(n.id)}>Mark read</button>}
            </div>
          </div>
        ))
      )}
    </>
  )
}
