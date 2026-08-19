import { useEffect, useMemo, useState } from 'react'
import { useParams } from 'react-router-dom'
import { get, post, ApiError } from '../api/client'
import { useAuth } from '../auth/AuthContext'
import type { Goal, ReviewDetail, User } from '../types'
import StarRating from '../components/StarRating'

export default function ReviewViewPage() {
  const { id } = useParams()
  const reviewId = Number(id)
  const { user } = useAuth()

  const [review, setReview] = useState<ReviewDetail | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [tab, setTab] = useState<'professional' | 'skills' | 'personal' | 'achievements'>('professional')

  const reload = () =>
    get<ReviewDetail>(`/api/reviews/${reviewId}`).then(setReview).catch((e: any) => setError(e.message))

  useEffect(() => {
    reload().finally(() => setLoading(false))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [reviewId])

  if (loading) return <div className="loading">Loading…</div>
  if (error) return <div className="error">{error}</div>
  if (!review) return <div className="empty">Review not found.</div>

  const isAdmin = user?.userType === 'Admin'
  const myAssignment = review.reviewers.find((r) => r.reviewerId === user?.id)
  const myAssessment = review.assessments.find((a) => a.reviewerId === user?.id)

  const professional = review.goals.filter((g) => g.goalType === 'Professional')
  const personal = review.goals.filter((g) => g.goalType === 'Personal')

  return (
    <>
      <div className="page-head">
        <h1>{review.developerName} · {review.cycleName}</h1>
        <p>
          {review.functionName ? `${review.functionName} · ${review.roleName}` : 'No role'} ·{' '}
          <span className={`badge ${review.status}`}>{review.status}</span>
          {review.submittedAt && <> · Submitted {new Date(review.submittedAt).toLocaleDateString()}</>}
        </p>
      </div>

      {review.status === 'Draft' && (
        <div className="card"><p className="muted" style={{ margin: 0 }}>This developer has not submitted their plan yet.</p></div>
      )}

      {/* Reviewers panel */}
      <div className="card">
        <h3>Assigned reviewers</h3>
        {review.reviewers.length === 0 ? (
          <p className="muted">No reviewers assigned yet.</p>
        ) : (
          <div className="pill-row">
            {review.reviewers.map((r) => (
              <span key={r.reviewerId} className={`badge ${r.reviewerType === 'Manager' ? 'pro' : 'personal'}`}>
                {r.reviewerType}: {r.reviewerName} {r.hasSubmitted ? '✓' : '· pending'}
              </span>
            ))}
          </div>
        )}
        {review.selectedPeerName && (
          <p className="muted" style={{ marginTop: 10 }}>Developer-selected peer: <strong>{review.selectedPeerName}</strong></p>
        )}
        {isAdmin && <AssignPanel review={review} onDone={reload} />}
      </div>

      {/* Tabbed plan content: Professional Goals · Skill Assessment · Personal Goals */}
      <div className="tabs">
        <button className={tab === 'professional' ? 'active' : ''} onClick={() => setTab('professional')}>
          Professional Goals <span className="muted">({professional.length})</span>
        </button>
        <button className={tab === 'skills' ? 'active' : ''} onClick={() => setTab('skills')}>
          Skill Assessment <span className="muted">({review.skillRatings.length})</span>
        </button>
        <button className={tab === 'personal' ? 'active' : ''} onClick={() => setTab('personal')}>
          Personal Goals <span className="muted">({personal.length})</span>
        </button>
        <button className={tab === 'achievements' ? 'active' : ''} onClick={() => setTab('achievements')}>
          Key Achievements <span className="muted">({review.achievements.length})</span>
        </button>
      </div>

      {tab === 'professional' && (
        <div className="card">
          <h3>Professional Goals ({professional.length})</h3>
          {professional.length === 0 ? <p className="muted">No professional goals.</p>
            : professional.map((g) => <GoalView key={g.id} g={g} />)}
        </div>
      )}

      {tab === 'skills' && (
        <div className="card">
          <h3>Skill assessment</h3>
          {review.skillRatings.length === 0 ? <p className="muted">No skill ratings.</p> : (
            <table>
              <thead><tr><th>Skill</th><th>Self rating</th><th>Comment</th></tr></thead>
              <tbody>
                {review.skillRatings.map((s) => (
                  <tr key={s.skillId}>
                    <td>{s.skillName}</td>
                    <td><StarRating value={s.selfRating} readonly /></td>
                    <td className="muted">{s.comments || '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
          <h3 style={{ marginTop: 20 }}>Key R&amp;D Improvements</h3>
          {review.rndImprovements.length === 0 ? <p className="muted">None recorded.</p> : (
            <ul style={{ margin: 0, paddingLeft: 18 }}>
              {review.rndImprovements.map((r) => <li key={r.id}>{r.description}</li>)}
            </ul>
          )}
          <h3 style={{ marginTop: 20 }}>Future Skills to Acquire</h3>
          {review.futureSkills.length === 0 ? <p className="muted">None recorded.</p> : (
            <div className="pill-row">
              {review.futureSkills.map((f) => <span key={f.id} className="badge trait">{f.name}</span>)}
            </div>
          )}
        </div>
      )}

      {tab === 'personal' && (
        <div className="card">
          <h3>Personal Goals ({personal.length})</h3>
          {personal.length === 0 ? <p className="muted">No personal goals.</p>
            : personal.map((g) => <GoalView key={g.id} g={g} />)}
        </div>
      )}

      {tab === 'achievements' && (
        <div className="card">
          <h3>Last Year — Key Achievements ({review.achievements.length})</h3>
          {review.achievements.length === 0 ? <p className="muted">No achievements recorded.</p> : (
            review.achievements.map((a) => (
              <div key={a.id} className="goal-block">
                <div className="goal-top">
                  <span style={{ fontWeight: 600 }}>{a.projectName || 'Untitled project'}
                    {a.clientName && <span className="muted"> · {a.clientName}</span>}</span>
                  <span className="pill-row">
                    {a.managerRating != null && <span className="badge pro">Mgr rating {a.managerRating}/10</span>}
                    {a.companyTraitName && <span className="badge trait">{a.companyTraitName}</span>}
                  </span>
                </div>
                {a.workDescription && <div className="kv"><span className="k">Work</span><span>{a.workDescription}</span></div>}
              </div>
            ))
          )}
        </div>
      )}

      {review.selfSummary && (
        <div className="card"><h3>Self summary</h3><p style={{ margin: 0 }}>{review.selfSummary}</p></div>
      )}

      {/* Reviewer assessment form */}
      {myAssignment && review.status !== 'Draft' && (
        <AssessmentForm review={review} alreadySubmitted={!!myAssessment?.submittedAt}
          existing={myAssessment} onDone={reload} />
      )}

      {/* All submitted assessments */}
      {review.assessments.filter((a) => a.submittedAt).length > 0 && (
        <div className="card">
          <h3>Reviewer assessments</h3>
          {review.assessments.filter((a) => a.submittedAt).map((a) => (
            <div key={a.id} className="goal-block">
              <div className="goal-top">
                <span className={`badge ${a.reviewerType === 'Manager' ? 'pro' : 'personal'}`}>
                  {a.reviewerType}: {a.reviewerName}
                </span>
                <span>Overall: <StarRating value={a.overallRating} readonly /></span>
              </div>
              {a.strengths && <div className="kv"><span className="k">Strengths</span><span>{a.strengths}</span></div>}
              {a.improvements && <div className="kv"><span className="k">Improvements</span><span>{a.improvements}</span></div>}
              {a.skillRatings.length > 0 && (
                <div className="pill-row" style={{ marginTop: 8 }}>
                  {a.skillRatings.map((s) => (
                    <span key={s.skillId} className="badge trait">{s.skillName}: {s.rating}/10</span>
                  ))}
                </div>
              )}
            </div>
          ))}
        </div>
      )}
    </>
  )
}

const STATUS_LABEL: Record<string, string> = {
  NotStarted: 'Not Started', InProgress: 'In Progress', Completed: 'Completed', Dropped: 'Dropped',
}

function GoalView({ g }: { g: Goal }) {
  return (
    <div className="goal-block">
      <div className="goal-top">
        <strong>{g.title}</strong>
        <span className="pill-row">
          <span className={`badge ${g.status}`}>{STATUS_LABEL[g.status] ?? g.status} · {g.completionPercentage}%</span>
          {g.companyTraitName && <span className="badge trait">{g.companyTraitName}</span>}
        </span>
      </div>
      <div className="kv"><span className="k">Specific</span><span>{g.specific}</span></div>
      <div className="kv"><span className="k">Measurable</span><span>{g.measurable}</span></div>
      <div className="kv"><span className="k">Achievable</span><span>{g.achievable}</span></div>
      <div className="kv"><span className="k">Relevant</span><span>{g.relevant}</span></div>
      <div className="kv"><span className="k">Time-bound</span><span>{g.timeBound}</span></div>
      {(g.statusComment || g.statusDate) && (
        <div className="kv"><span className="k">Progress</span>
          <span>{g.statusComment || '—'}{g.statusDate ? ` (as of ${new Date(g.statusDate).toLocaleDateString()})` : ''}</span></div>
      )}
    </div>
  )
}

function AssignPanel({ review, onDone }: { review: ReviewDetail; onDone: () => Promise<any> }) {
  const [managers, setManagers] = useState<User[]>([])
  const [peers, setPeers] = useState<User[]>([])
  const [m1, setM1] = useState<number | ''>('')
  const [m2, setM2] = useState<number | ''>('')
  const [peer, setPeer] = useState<number | ''>(review.selectedPeerId ?? '')
  const [busy, setBusy] = useState(false)
  const [msg, setMsg] = useState('')
  const [err, setErr] = useState('')

  useEffect(() => {
    get<User[]>('/api/users/managers').then(setManagers).catch(() => {})
    get<User[]>('/api/users?type=Developer').then((devs) =>
      setPeers(devs.filter((d) => d.id !== review.developerId))).catch(() => {})
    // Prefill from existing assignment.
    const mgrs = review.reviewers.filter((r) => r.reviewerType === 'Manager')
    if (mgrs[0]) setM1(mgrs[0].reviewerId)
    if (mgrs[1]) setM2(mgrs[1].reviewerId)
    const pr = review.reviewers.find((r) => r.reviewerType === 'Peer')
    if (pr) setPeer(pr.reviewerId)
  }, [review])

  const submit = async () => {
    setErr(''); setMsg('')
    if (m1 === '' || m2 === '' || peer === '') { setErr('Select two managers and one peer.'); return }
    if (m1 === m2) { setErr('The two managers must be different.'); return }
    setBusy(true)
    try {
      await post(`/api/reviews/${review.id}/assign`, { managerIds: [Number(m1), Number(m2)], peerId: Number(peer) })
      setMsg('Reviewers assigned.')
      await onDone()
    } catch (e: any) {
      setErr(e instanceof ApiError ? e.message : 'Assignment failed')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div style={{ marginTop: 16, borderTop: '1px solid var(--line)', paddingTop: 16 }}>
      <h3 style={{ marginBottom: 6 }}>Admin: assign reviewers</h3>
      <p className="section-hint">Assign exactly 2 managers and 1 peer (requirement 11).</p>
      {msg && <div className="success">{msg}</div>}
      {err && <div className="error">{err}</div>}
      <div className="grid-3">
        <div className="field">
          <label>Manager 1</label>
          <select value={m1} onChange={(e) => setM1(e.target.value === '' ? '' : Number(e.target.value))}>
            <option value="">Select…</option>
            {managers.map((m) => <option key={m.id} value={m.id}>{m.fullName}</option>)}
          </select>
        </div>
        <div className="field">
          <label>Manager 2</label>
          <select value={m2} onChange={(e) => setM2(e.target.value === '' ? '' : Number(e.target.value))}>
            <option value="">Select…</option>
            {managers.map((m) => <option key={m.id} value={m.id}>{m.fullName}</option>)}
          </select>
        </div>
        <div className="field">
          <label>Peer</label>
          <select value={peer} onChange={(e) => setPeer(e.target.value === '' ? '' : Number(e.target.value))}>
            <option value="">Select…</option>
            {peers.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
          </select>
        </div>
      </div>
      <button disabled={busy} onClick={submit}>Save assignment</button>
    </div>
  )
}

function AssessmentForm({ review, alreadySubmitted, existing, onDone }: {
  review: ReviewDetail
  alreadySubmitted: boolean
  existing?: ReviewDetail['assessments'][number]
  onDone: () => Promise<any>
}) {
  const [overall, setOverall] = useState(existing?.overallRating ?? 0)
  const [strengths, setStrengths] = useState(existing?.strengths ?? '')
  const [improvements, setImprovements] = useState(existing?.improvements ?? '')
  const initialSkills = useMemo(() => {
    const map: Record<number, number> = {}
    for (const sk of review.roleSkills) {
      map[sk.id] = existing?.skillRatings.find((x) => x.skillId === sk.id)?.rating ?? 0
    }
    return map
  }, [review, existing])
  const [skills, setSkills] = useState<Record<number, number>>(initialSkills)
  const [busy, setBusy] = useState(false)
  const [err, setErr] = useState('')
  const [msg, setMsg] = useState('')

  const submit = async () => {
    setErr(''); setMsg('')
    if (overall < 1) { setErr('Give an overall rating (1–10).'); return }
    setBusy(true)
    try {
      await post(`/api/reviews/${review.id}/assessment`, {
        overallRating: overall,
        strengths, improvements,
        skillRatings: Object.entries(skills).filter(([, v]) => v > 0).map(([k, v]) => ({ skillId: Number(k), rating: v })),
      })
      setMsg('Assessment submitted. Thank you.')
      await onDone()
    } catch (e: any) {
      setErr(e.message || 'Submit failed')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="card" style={{ borderColor: 'var(--primary)' }}>
      <h3>Your assessment {alreadySubmitted && <span className="badge Completed">submitted</span>}</h3>
      <p className="section-hint">Provide your rating and feedback for this developer. You can re-submit to update.</p>
      {msg && <div className="success">{msg}</div>}
      {err && <div className="error">{err}</div>}
      <div className="field">
        <label>Overall rating</label>
        <StarRating value={overall} onChange={setOverall} />
      </div>
      <div className="grid-2">
        <div className="field"><label>Strengths</label><textarea rows={3} value={strengths} onChange={(e) => setStrengths(e.target.value)} /></div>
        <div className="field"><label>Areas for improvement</label><textarea rows={3} value={improvements} onChange={(e) => setImprovements(e.target.value)} /></div>
      </div>
      {review.roleSkills.length > 0 && (
        <div className="field">
          <label>Skill ratings</label>
          {review.roleSkills.map((sk) => (
            <div key={sk.id} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '6px 0' }}>
              <span>{sk.name}</span>
              <StarRating value={skills[sk.id] ?? 0} onChange={(v) => setSkills((m) => ({ ...m, [sk.id]: v }))} />
            </div>
          ))}
        </div>
      )}
      <button disabled={busy} onClick={submit}>{alreadySubmitted ? 'Update assessment' : 'Submit assessment'}</button>
    </div>
  )
}
