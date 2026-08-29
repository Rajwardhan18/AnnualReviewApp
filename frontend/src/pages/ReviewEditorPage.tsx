import { useEffect, useMemo, useRef, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { get, post, put, ApiError } from '../api/client'
import type { AchievementInput, CompanyTrait, GoalInput, GoalStatus, GoalType, ReviewDetail, SkillRatingInput, User } from '../types'
import StarRating from '../components/StarRating'

const MIN_PROFESSIONAL = 5
const MIN_PERSONAL = 2
type Tab = 'professional' | 'skills' | 'personal' | 'achievements'
type EditGoal = GoalInput & { id?: number }

const STATUSES: GoalStatus[] = ['NotStarted', 'InProgress', 'Completed', 'Dropped']
const STATUS_LABEL: Record<GoalStatus, string> = {
  NotStarted: 'Not Started', InProgress: 'In Progress', Completed: 'Completed', Dropped: 'Dropped',
}

const MIN_ACHIEVEMENTS = 5

function emptyGoal(goalType: GoalType): EditGoal {
  return {
    goalType, title: '', specific: '', measurable: '', achievable: '', relevant: '', timeBound: '',
    companyTraitId: null, status: 'NotStarted', completionPercentage: 0, statusComment: '', statusDate: null, target: '',
  }
}
function emptyAchievement(): AchievementInput {
  return { projectName: '', clientName: '', workDescription: '', companyTraitId: null }
}
// Professional goals use SMART + trait; Personal goals just need a title and a target.
const isGoalComplete = (g: EditGoal) =>
  g.goalType === 'Personal'
    ? !!g.title.trim() && !!(g.target ?? '').trim()
    : !!g.title.trim() && !!g.specific.trim() && !!g.measurable.trim() &&
      !!g.achievable.trim() && !!g.relevant.trim() && !!g.timeBound.trim() && g.companyTraitId != null

export default function ReviewEditorPage() {
  const { id } = useParams()
  const reviewId = Number(id)
  const navigate = useNavigate()

  const [review, setReview] = useState<ReviewDetail | null>(null)
  const [traits, setTraits] = useState<CompanyTrait[]>([])
  const [peers, setPeers] = useState<User[]>([])
  const [goals, setGoals] = useState<EditGoal[]>([])
  const [achievements, setAchievements] = useState<AchievementInput[]>([])
  const [rnd, setRnd] = useState<string[]>([])
  const [future, setFuture] = useState<string[]>([])
  const [ratings, setRatings] = useState<Record<number, SkillRatingInput>>({})
  const [peerId, setPeerId] = useState<number | ''>('')
  const [summary, setSummary] = useState('')
  const [midYear, setMidYear] = useState('')
  const [final, setFinal] = useState('')

  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [errors, setErrors] = useState<string[]>([])
  const [message, setMessage] = useState('')
  const [tab, setTab] = useState<Tab>('professional')
  const [saveState, setSaveState] = useState<'idle' | 'pending' | 'saving' | 'saved' | 'error'>('idle')

  // Auto-save plumbing. `hydrated` skips the first change (the initial data load);
  // `inFlight` serializes so an auto-save never overlaps a manual save/submit.
  const hydrated = useRef(false)
  const inFlight = useRef(false)
  const autoSaveTimer = useRef<number | undefined>(undefined)

  useEffect(() => {
    Promise.all([
      get<ReviewDetail>(`/api/reviews/${reviewId}`),
      get<CompanyTrait[]>('/api/traits'),
      get<User[]>('/api/users/peers'),
    ]).then(([r, t, p]) => {
      setReview(r)
      setTraits(t)
      setPeers(p)
      setPeerId(r.selectedPeerId ?? '')
      setSummary(r.selfSummary ?? '')
      setMidYear(r.midYearReflection ?? '')
      setFinal(r.finalReflection ?? '')

      // Requirement 5: default to a single starter goal per section; users add the rest.
      if (r.goals.length > 0) {
        setGoals(r.goals.map((g) => ({ ...g })))
      } else {
        setGoals([emptyGoal('Professional'), emptyGoal('Personal')])
      }
      setAchievements(r.achievements.map((a) => ({
        projectName: a.projectName, clientName: a.clientName, workDescription: a.workDescription,
        companyTraitId: a.companyTraitId ?? null,
      })))
      setRnd(r.rndImprovements.map((x) => x.description))
      setFuture(r.futureSkills.map((x) => x.name))

      const map: Record<number, SkillRatingInput> = {}
      for (const sk of r.roleSkills) {
        const existing = r.skillRatings.find((x) => x.skillId === sk.id)
        map[sk.id] = { skillId: sk.id, selfRating: existing?.selfRating ?? 0, comments: existing?.comments ?? '' }
      }
      setRatings(map)
    }).catch((e: any) => setErrors([e.message || 'Failed to load review']))
      .finally(() => setLoading(false))
  }, [reviewId])

  const professional = useMemo(() => goals.filter((g) => g.goalType === 'Professional'), [goals])
  const personal = useMemo(() => goals.filter((g) => g.goalType === 'Personal'), [goals])
  const readOnly = review?.status !== 'Draft'
  const midYearSubmitted = !!review?.midYearSubmittedAt
  const finalSubmitted = !!review?.finalReflectionSubmittedAt
  const midYearOpen = !!review?.halfYearlyReleased && !midYearSubmitted
  const finalOpen = !!review?.finalReviewReleased && !finalSubmitted
  // Goal progress + reflections stay editable until the year-end review is submitted.
  const progressLocked = finalSubmitted

  const updateGoal = (target: EditGoal, patch: Partial<EditGoal>) =>
    setGoals((gs) => gs.map((g) => (g === target ? { ...g, ...patch } : g)))
  const addGoal = (kind: GoalType) => setGoals((gs) => [...gs, emptyGoal(kind)])
  const removeGoal = (target: EditGoal) => setGoals((gs) => gs.filter((g) => g !== target))

  const updateAch = (i: number, patch: Partial<AchievementInput>) =>
    setAchievements((as) => as.map((a, idx) => (idx === i ? { ...a, ...patch } : a)))
  const addAch = () => setAchievements((as) => [...as, emptyAchievement()])
  const removeAch = (i: number) => setAchievements((as) => as.filter((_, idx) => idx !== i))

  function buildPayload() {
    return {
      selectedPeerId: peerId === '' ? null : Number(peerId),
      selfSummary: summary,
      goals: goals.map((g) => ({
        goalType: g.goalType, title: g.title, specific: g.specific, measurable: g.measurable,
        achievable: g.achievable, relevant: g.relevant, timeBound: g.timeBound,
        companyTraitId: g.companyTraitId ?? null, target: g.target ?? null,
        status: g.status, completionPercentage: g.completionPercentage,
        statusComment: g.statusComment ?? null, statusDate: g.statusDate || null,
      })),
      skillRatings: Object.values(ratings).filter((r) => r.selfRating > 0)
        .map((r) => ({ skillId: r.skillId, selfRating: r.selfRating, comments: r.comments })),
      achievements: achievements.filter((a) => a.projectName.trim() || a.workDescription.trim()),
      rndImprovements: rnd.filter((d) => d.trim()).map((d) => ({ description: d })),
      futureSkills: future.filter((n) => n.trim()).map((n) => ({ name: n })),
    }
  }

  // Progress payload (goal status/notes + reflections). Each reflection is only sent while its
  // own checkpoint is open, so a submitted-and-locked reflection is never overwritten.
  function buildProgressPayload() {
    return {
      goals: goals.filter((g) => g.id).map((g) => ({
        goalId: g.id, status: g.status, completionPercentage: g.completionPercentage,
        statusComment: g.statusComment ?? null, statusDate: g.statusDate || null,
      })),
      midYearReflection: midYearOpen ? midYear : null,
      finalReflection: finalOpen ? final : null,
    }
  }

  // Silently persist whatever is currently editable: the draft plan while the
  // review is a Draft, or goal progress + mid-year reflection once submitted.
  // Does not touch local state so it never clobbers what the user is typing.
  async function autoSave() {
    if (inFlight.current || busy || !review || finalSubmitted) return
    inFlight.current = true
    setSaveState('saving')
    try {
      if (!readOnly) {
        await put<ReviewDetail>(`/api/reviews/${reviewId}/plan`, buildPayload())
      } else {
        await put<ReviewDetail>(`/api/reviews/${reviewId}/progress`, buildProgressPayload())
      }
      setSaveState('saved')
    } catch {
      setSaveState('error')
    } finally {
      inFlight.current = false
    }
  }

  // Debounce: schedule an auto-save ~1.2s after the last edit to any field.
  useEffect(() => {
    if (!review || finalSubmitted) return
    if (!hydrated.current) { hydrated.current = true; return } // ignore the load-time hydration
    setSaveState('pending')
    window.clearTimeout(autoSaveTimer.current)
    autoSaveTimer.current = window.setTimeout(() => { void autoSave() }, 1200)
    return () => window.clearTimeout(autoSaveTimer.current)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [goals, achievements, rnd, future, ratings, peerId, summary, midYear, final])

  async function save(thenSubmit: boolean) {
    window.clearTimeout(autoSaveTimer.current)
    setErrors([]); setMessage(''); setBusy(true)
    try {
      await put<ReviewDetail>(`/api/reviews/${reviewId}/plan`, buildPayload())
      if (thenSubmit) {
        const updated = await post<ReviewDetail>(`/api/reviews/${reviewId}/submit`)
        setReview(updated)
        setGoals(updated.goals.map((g) => ({ ...g })))
        setMessage('Your annual plan has been submitted.')
      } else {
        setMessage('Draft saved.')
      }
      setSaveState('saved')
      window.scrollTo({ top: 0, behavior: 'smooth' })
    } catch (e: any) {
      setSaveState('error')
      if (e instanceof ApiError && e.body?.errors) setErrors(e.body.errors)
      else setErrors([e.message || 'Save failed'])
      window.scrollTo({ top: 0, behavior: 'smooth' })
    } finally {
      setBusy(false)
    }
  }

  async function saveProgress() {
    window.clearTimeout(autoSaveTimer.current)
    setErrors([]); setMessage(''); setBusy(true)
    try {
      const updated = await put<ReviewDetail>(`/api/reviews/${reviewId}/progress`, buildProgressPayload())
      setReview(updated)
      setSaveState('saved')
      setMessage('Goal progress saved.')
      window.scrollTo({ top: 0, behavior: 'smooth' })
    } catch (e: any) {
      setSaveState('error')
      setErrors([e.message || 'Save failed'])
    } finally {
      setBusy(false)
    }
  }

  async function submitMidYear() {
    window.clearTimeout(autoSaveTimer.current)
    setErrors([]); setMessage(''); setBusy(true)
    try {
      const updated = await post<ReviewDetail>(`/api/reviews/${reviewId}/submit-midyear`, buildProgressPayload())
      setReview(updated)
      setMessage('Your mid-year review has been submitted and is now locked.')
      window.scrollTo({ top: 0, behavior: 'smooth' })
    } catch (e: any) {
      setErrors([e.message || 'Submit failed'])
    } finally {
      setBusy(false)
    }
  }

  async function submitFinal() {
    window.clearTimeout(autoSaveTimer.current)
    setErrors([]); setMessage(''); setBusy(true)
    try {
      const updated = await post<ReviewDetail>(`/api/reviews/${reviewId}/submit-final`, buildProgressPayload())
      setReview(updated)
      setMessage('Your year-end review has been submitted and is now locked.')
      window.scrollTo({ top: 0, behavior: 'smooth' })
    } catch (e: any) {
      setErrors([e.message || 'Submit failed'])
    } finally {
      setBusy(false)
    }
  }

  if (loading) return <div className="loading">Loading…</div>
  if (!review) return <div className="empty">Review not found.</div>

  const proComplete = professional.filter(isGoalComplete).length
  const perComplete = personal.filter(isGoalComplete).length
  const skillsRated = Object.values(ratings).filter((r) => r.selfRating > 0).length
  const skillsTotal = review.roleSkills.length
  const achCount = achievements.filter((a) => a.projectName.trim() || a.workDescription.trim()).length

  const criteria = [
    { label: 'A peer is selected for the review', met: peerId !== '' },
    { label: `At least ${MIN_PROFESSIONAL} professional goals fully filled (SMART + trait)`, met: proComplete >= MIN_PROFESSIONAL },
    { label: `At least ${MIN_PERSONAL} personal goals filled (title + target)`, met: perComplete >= MIN_PERSONAL },
    { label: `At least ${MIN_ACHIEVEMENTS} previous-year achievements added`, met: achCount >= MIN_ACHIEVEMENTS },
    { label: `All ${skillsTotal} role skills rated`, met: skillsTotal === 0 || skillsRated >= skillsTotal },
  ]
  const allMet = criteria.every((c) => c.met)

  const autoSaveBadge = finalSubmitted ? null : (
    saveState === 'saving' ? <span className="autosave saving"><span className="dot" />Saving…</span>
    : saveState === 'pending' ? <span className="autosave pending"><span className="dot" />Unsaved changes…</span>
    : saveState === 'saved' ? <span className="autosave saved">✓ All changes saved</span>
    : saveState === 'error' ? <span className="autosave err">Auto-save failed — use the button</span>
    : null
  )

  return (
    <>
      <div className="page-head">
        <h1>Annual Plan · {review.cycleName}</h1>
        <p>
          {review.functionName ? `${review.functionName} · ${review.roleName}` : 'No role assigned'} ·{' '}
          <span className={`badge ${review.status}`}>{review.status}</span>
        </p>
      </div>

      {message && <div className="success">{message}</div>}
      {errors.length > 0 && (
        <div className="error">
          <strong>Please fix the following:</strong>
          <ul>{errors.map((e, i) => <li key={i}>{e}</li>)}</ul>
        </div>
      )}

      {/* Requirement 1: minimum filling criteria + actions AT THE TOP */}
      {!readOnly ? (
        <div className="card" style={{ borderColor: allMet ? 'var(--green)' : '#f0d9a8' }}>
          <h3>Minimum filling criteria to submit</h3>
          <p className="section-hint">Save a draft any time. Submitting requires all of the following:</p>
          <ul className="criteria">
            {criteria.map((c, i) => (
              <li key={i} className={c.met ? 'met' : ''}>
                <span className={`mark ${c.met ? 'ok' : 'no'}`}>{c.met ? '✓' : '!'}</span>{c.label}
              </li>
            ))}
          </ul>
          <div className="btn-row" style={{ marginTop: 12 }}>
            <button className="secondary" disabled={busy} onClick={() => save(false)}>Save draft</button>
            <button disabled={busy || !allMet} onClick={() => save(true)}
              title={allMet ? 'Submit your plan' : 'Complete the minimum criteria first'}>Submit plan</button>
            {autoSaveBadge}
            <span className="muted">Changes save automatically. Submitting locks the plan (you can still update goal progress).</span>
          </div>
        </div>
      ) : (
        <div className="card" style={{ borderColor: finalSubmitted ? 'var(--green)' : 'var(--primary)' }}>
          <h3>
            {finalSubmitted || review.finalReviewReleased ? 'Year-end review'
              : review.halfYearlyReleased ? 'Half-yearly review'
              : 'Track goal progress'}
            {finalSubmitted && <span className="badge Completed" style={{ marginLeft: 8 }}>Submitted &amp; locked</span>}
          </h3>
          <p className="section-hint" style={{ margin: 0 }}>
            {finalSubmitted
              ? <>Your year-end review was submitted on {new Date(review.finalReflectionSubmittedAt!).toLocaleDateString()} and is now locked.</>
              : finalOpen
                ? <>The year-end review is open{review.finalReviewDueDate ? ` (due ${new Date(review.finalReviewDueDate).toLocaleDateString()})` : ''}. Update your final goal progress, write your year-end self-reflection below, then submit. This is your final self-assessment for the cycle.</>
                : midYearSubmitted
                  ? <>Your mid-year review was submitted on {new Date(review.midYearSubmittedAt!).toLocaleDateString()}. Keep your goal progress up to date — your year-end self-reflection opens when the year-end review is released.</>
                  : review.halfYearlyReleased
                    ? <>The mid-year checkpoint is open{review.halfYearlyDueDate ? ` (due ${new Date(review.halfYearlyDueDate).toLocaleDateString()})` : ''}. Update your goal progress below, add a mid-year reflection, then submit. Manager &amp; peer reviews stay at year-end.</>
                    : <>This plan is submitted and locked, but you can update each goal's status, completion % and notes through the year.</>}
            {' '}
            <button className="ghost small" onClick={() => navigate(`/reviews/${reviewId}`)}>View full review →</button>
          </p>
          {review.halfYearlyReleased && (
            <div className="field" style={{ marginTop: 12 }}>
              <label>Mid-year reflection{midYearSubmitted && ' · submitted'}</label>
              <textarea rows={3} value={midYear} disabled={midYearSubmitted} onChange={(e) => setMidYear(e.target.value)}
                placeholder="How is the year tracking? Wins, blockers, changes in focus…" />
            </div>
          )}
          {review.finalReviewReleased && (
            <div className="field" style={{ marginTop: 12 }}>
              <label>Year-end self-reflection{finalSubmitted && ' · submitted'}</label>
              <textarea rows={4} value={final} disabled={finalSubmitted} onChange={(e) => setFinal(e.target.value)}
                placeholder="Reflect on the full year — your biggest achievements, what you learned, how you grew against your goals, and where you want to focus next." />
            </div>
          )}
          {!finalSubmitted && (
            <div className="btn-row" style={{ marginTop: 12 }}>
              <button className="secondary" disabled={busy} onClick={saveProgress}>Save progress</button>
              {midYearOpen && <button disabled={busy} onClick={submitMidYear}>Submit mid-year review</button>}
              {finalOpen && <button disabled={busy} onClick={submitFinal}>Submit year-end review</button>}
              {autoSaveBadge}
              <span className="muted">
                Progress saves automatically.
                {finalOpen ? ' Submitting locks your year-end review.'
                  : midYearOpen ? ' Submitting locks your mid-year review.' : ''}
              </span>
            </div>
          )}
        </div>
      )}

      {/* Peer selection */}
      <div className="card">
        <h3>Peer for review</h3>
        <p className="section-hint">Select a peer developer to review your plan. They become your peer reviewer once you submit.</p>
        <div className="field" style={{ maxWidth: 420 }}>
          <label>Peer</label>
          <select value={peerId} disabled={readOnly}
            onChange={(e) => setPeerId(e.target.value === '' ? '' : Number(e.target.value))}>
            <option value="">Select a peer…</option>
            {peers.map((p) => (
              <option key={p.id} value={p.id}>
                {p.fullName}{p.roleName ? ` — ${p.functionName} ${p.roleName}` : ''}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* Tabs */}
      <div className="tabs">
        <button className={tab === 'professional' ? 'active' : ''} onClick={() => setTab('professional')}>
          Professional Goals <span className={`count-hint ${proComplete >= MIN_PROFESSIONAL ? 'ok' : 'bad'}`}>{proComplete}/{MIN_PROFESSIONAL}</span>
        </button>
        <button className={tab === 'skills' ? 'active' : ''} onClick={() => setTab('skills')}>
          Skill Assessment <span className={`count-hint ${skillsTotal > 0 && skillsRated >= skillsTotal ? 'ok' : 'bad'}`}>{skillsRated}/{skillsTotal}</span>
        </button>
        <button className={tab === 'personal' ? 'active' : ''} onClick={() => setTab('personal')}>
          Personal Goals <span className={`count-hint ${perComplete >= MIN_PERSONAL ? 'ok' : 'bad'}`}>{perComplete}/{MIN_PERSONAL}</span>
        </button>
        <button className={tab === 'achievements' ? 'active' : ''} onClick={() => setTab('achievements')}>
          Previous Year Achievements <span className={`count-hint ${achCount >= MIN_ACHIEVEMENTS ? 'ok' : 'bad'}`}>{achCount}/{MIN_ACHIEVEMENTS}</span>
        </button>
      </div>

      {tab === 'professional' && (
        <div className="card">
          <h3>Professional Goals</h3>
          <p className="section-hint">Minimum {MIN_PROFESSIONAL} goals, each in the SMART template and tagged to a company trait.</p>
          {professional.map((g, i) => (
            <GoalAccordion key={`pro-${i}`} goal={g} index={i} traits={traits} planReadOnly={readOnly} progressLocked={progressLocked}
              onChange={(patch) => updateGoal(g, patch)} onRemove={() => removeGoal(g)} />
          ))}
          {!readOnly && <button className="secondary" onClick={() => addGoal('Professional')}>+ Add professional goal</button>}
        </div>
      )}

      {tab === 'skills' && (
        <>
          <div className="card">
            <h3>Skill Assessment</h3>
            <p className="section-hint">Rate yourself (1–10) against every skill identified for your role.</p>
            {review.roleSkills.length === 0 && <div className="muted">No skills are mapped to your role yet.</div>}
            {review.roleSkills.map((sk) => (
              <div key={sk.id} className="field" style={{ display: 'grid', gridTemplateColumns: '1fr auto 1.5fr', gap: 12, alignItems: 'center' }}>
                <div>
                  <strong>{sk.name}</strong>
                  {sk.category && <div className="muted" style={{ fontSize: 12 }}>{sk.category}</div>}
                </div>
                <StarRating value={ratings[sk.id]?.selfRating ?? 0} readonly={readOnly}
                  onChange={(v) => setRatings((m) => ({ ...m, [sk.id]: { ...m[sk.id], selfRating: v } }))} />
                <input placeholder="Optional comment" disabled={readOnly}
                  value={ratings[sk.id]?.comments ?? ''}
                  onChange={(e) => setRatings((m) => ({ ...m, [sk.id]: { ...m[sk.id], comments: e.target.value } }))} />
              </div>
            ))}
          </div>

          {/* Initiatives Undertaken (formerly Key R&D Improvements) */}
          <div className="card">
            <h3>Initiatives Undertaken</h3>
            <p className="section-hint">Initiatives and research &amp; development contributions you made.</p>
            <TextList items={rnd} setItems={setRnd} readOnly={readOnly}
              placeholder="e.g. Built a proof-of-concept caching layer that cut DB load 40%" addLabel="+ Add initiative" multiline />
          </div>

          {/* Requirement 4: Future skills to acquire */}
          <div className="card">
            <h3>Future Skills to Acquire</h3>
            <p className="section-hint">Skills you plan to learn or deepen.</p>
            <TextList items={future} setItems={setFuture} readOnly={readOnly}
              placeholder="e.g. Kubernetes, Rust, event-driven design" addLabel="+ Add future skill" />
          </div>
        </>
      )}

      {tab === 'personal' && (
        <div className="card">
          <h3>Personal Goals</h3>
          <p className="section-hint">Minimum {MIN_PERSONAL} goals, each in the SMART template and tagged to a company trait.</p>
          {personal.map((g, i) => (
            <GoalAccordion key={`per-${i}`} goal={g} index={i} traits={traits} planReadOnly={readOnly} progressLocked={progressLocked}
              onChange={(patch) => updateGoal(g, patch)} onRemove={() => removeGoal(g)} />
          ))}
          {!readOnly && <button className="secondary" onClick={() => addGoal('Personal')}>+ Add personal goal</button>}
        </div>
      )}

      {tab === 'achievements' && (
        <div className="card">
          <h3>Previous Year Achievements</h3>
          <p className="section-hint">Projects you delivered last year: project, client and work done ({MIN_ACHIEVEMENTS} required). Your managers rate each of these — the ratings appear here once they review.</p>
          {achievements.length === 0 && <div className="muted" style={{ marginBottom: 12 }}>No achievements added yet.</div>}
          {achievements.map((a, i) => {
            const rated = review.achievements[i]
            return (
              <div key={i} className="goal-block">
                <div className="goal-top">
                  <span className="badge trait">Achievement #{i + 1}</span>
                  {!readOnly && <button className="danger small" onClick={() => removeAch(i)}>Remove</button>}
                </div>
                <div className="grid-2">
                  <div className="field"><label>Project name</label>
                    <input disabled={readOnly} value={a.projectName} onChange={(e) => updateAch(i, { projectName: e.target.value })} placeholder="e.g. Checkout revamp" /></div>
                  <div className="field"><label>Client name</label>
                    <input disabled={readOnly} value={a.clientName} onChange={(e) => updateAch(i, { clientName: e.target.value })} placeholder="e.g. Acme Corp" /></div>
                </div>
                <div className="field"><label>Work description</label>
                  <textarea rows={2} disabled={readOnly} value={a.workDescription} onChange={(e) => updateAch(i, { workDescription: e.target.value })}
                    placeholder="What you delivered and its impact" /></div>
                <div className="grid-2">
                  <div className="field"><label>Company trait (optional)</label>
                    <select disabled={readOnly} value={a.companyTraitId ?? ''}
                      onChange={(e) => updateAch(i, { companyTraitId: e.target.value === '' ? null : Number(e.target.value) })}>
                      <option value="">Select trait…</option>
                      {traits.map((t) => <option key={t.id} value={t.id}>{t.name}</option>)}
                    </select></div>
                  <div className="field">
                    <label>Manager ratings</label>
                    <div className="pill-row" style={{ paddingTop: 4 }}>
                      <span className={`badge ${rated?.manager1Rating != null ? 'pro' : 'Draft'}`}>Manager 1: {rated?.manager1Rating != null ? `${rated.manager1Rating}/10` : 'pending'}</span>
                      <span className={`badge ${rated?.manager2Rating != null ? 'pro' : 'Draft'}`}>Manager 2: {rated?.manager2Rating != null ? `${rated.manager2Rating}/10` : 'pending'}</span>
                    </div>
                  </div>
                </div>
              </div>
            )
          })}
          {!readOnly && <button className="secondary" onClick={addAch}>+ Add achievement</button>}
        </div>
      )}

      {/* Self summary */}
      <div className="card">
        <h3>Self summary (optional)</h3>
        <textarea rows={3} disabled={readOnly} value={summary} onChange={(e) => setSummary(e.target.value)}
          placeholder="A short summary of your focus for the year…" />
      </div>

      {!readOnly && (
        <div className="btn-row">
          <button className="secondary" disabled={busy} onClick={() => save(false)}>Save draft</button>
          <button disabled={busy || !allMet} onClick={() => save(true)}>Submit plan</button>
        </div>
      )}
    </>
  )
}

function TextList({ items, setItems, readOnly, placeholder, addLabel, multiline }: {
  items: string[]; setItems: (fn: (s: string[]) => string[]) => void
  readOnly: boolean; placeholder: string; addLabel: string; multiline?: boolean
}) {
  const set = (i: number, v: string) => setItems((s) => s.map((x, idx) => (idx === i ? v : x)))
  const remove = (i: number) => setItems((s) => s.filter((_, idx) => idx !== i))
  return (
    <>
      {items.length === 0 && <div className="muted" style={{ marginBottom: 10 }}>None added yet.</div>}
      {items.map((v, i) => (
        <div key={i} style={{ display: 'flex', gap: 8, marginBottom: 8, alignItems: 'flex-start' }}>
          {multiline
            ? <textarea rows={2} style={{ flex: 1 }} disabled={readOnly} value={v} placeholder={placeholder} onChange={(e) => set(i, e.target.value)} />
            : <input style={{ flex: 1 }} disabled={readOnly} value={v} placeholder={placeholder} onChange={(e) => set(i, e.target.value)} />}
          {!readOnly && <button className="danger small" onClick={() => remove(i)}>✕</button>}
        </div>
      ))}
      {!readOnly && <button className="secondary" onClick={() => setItems((s) => [...s, ''])}>{addLabel}</button>}
    </>
  )
}

function GoalAccordion({ goal, index, traits, planReadOnly, progressLocked, onChange, onRemove }: {
  goal: EditGoal
  index: number
  traits: CompanyTrait[]
  planReadOnly: boolean
  progressLocked?: boolean
  onChange: (patch: Partial<EditGoal>) => void
  onRemove: () => void
}) {
  const complete = isGoalComplete(goal)
  const [open, setOpen] = useState(() => !goal.title.trim())
  const s = (k: keyof EditGoal) => (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) =>
    onChange({ [k]: e.target.value } as Partial<EditGoal>)
  const traitName = traits.find((t) => t.id === goal.companyTraitId)?.name

  return (
    <div className={`accordion${open ? ' open' : ''}${!complete ? ' invalid' : ''}`}>
      <div className="acc-head" onClick={() => setOpen((o) => !o)} role="button" aria-expanded={open}>
        <span className="acc-chevron">▶</span>
        <span className={`badge ${goal.goalType === 'Professional' ? 'pro' : 'personal'}`}>#{index + 1}</span>
        <span className="acc-title">
          {goal.title.trim() || <span className="muted-inline">Untitled goal</span>}
          {traitName && <span className="muted-inline"> · {traitName}</span>}
        </span>
        <span className={`badge ${goal.status}`}>{STATUS_LABEL[goal.status]} · {goal.completionPercentage}%</span>
        <span className={`acc-status ${complete ? 'ok' : 'todo'}`}>{complete ? 'Complete' : 'Incomplete'}</span>
        {!planReadOnly && (
          <button className="danger small" onClick={(e) => { e.stopPropagation(); onRemove() }}>Remove</button>
        )}
      </div>
      {open && (
        <div className="acc-body">
          <div className="field">
            <label>{goal.goalType === 'Personal' ? 'Goal' : 'Title'}</label>
            <span className="field-help">A short name for the goal you'll recognise at a glance.</span>
            <input value={goal.title} disabled={planReadOnly} onChange={s('title')}
              placeholder={goal.goalType === 'Personal' ? 'e.g. Improve public speaking' : 'Run a half marathon'} />
          </div>
          {goal.goalType === 'Personal' ? (
            <div className="field">
              <label>Target</label>
              <textarea rows={2} value={goal.target ?? ''} disabled={planReadOnly}
                onChange={(e) => onChange({ target: e.target.value })}
                placeholder="What does success look like? e.g. Present at 2 team demos by Q4" />
            </div>
          ) : (
            <div className="smart-grid">
              <div className="field">
                <label>S — Specific</label>
                <span className="field-help">State exactly what you'll accomplish, who's involved, and where. Avoid vague words like "improve" or "more".</span>
                <textarea rows={2} value={goal.specific} disabled={planReadOnly} onChange={s('specific')} placeholder="What exactly will be achieved, and by whom?" />
              </div>
              <div className="field">
                <label>M — Measurable</label>
                <span className="field-help">Define the number, metric, or evidence that proves the goal is met — so you can answer "how much" and "how will I know I'm done?"</span>
                <textarea rows={2} value={goal.measurable} disabled={planReadOnly} onChange={s('measurable')} placeholder="e.g. 21 km in under 2 hours, tracked weekly" />
              </div>
              <div className="field">
                <label>A — Achievable / Actionable</label>
                <span className="field-help">Confirm it's realistic with the time, skills, and resources you actually have — and name the first concrete action.</span>
                <textarea rows={2} value={goal.achievable} disabled={planReadOnly} onChange={s('achievable')} placeholder="What makes this doable, and what's step one?" />
              </div>
              <div className="field">
                <label>R — Relevant</label>
                <span className="field-help">Explain why this matters now and how it connects to your bigger priorities.</span>
                <textarea rows={2} value={goal.relevant} disabled={planReadOnly} onChange={s('relevant')} placeholder="Why is this worth doing right now?" />
              </div>
              <div className="field">
                <label>T — Time-bound (target date)</label>
                <span className="field-help">Set a firm deadline that creates urgency and gives you a point to review progress against.</span>
                <input type="date" value={goal.timeBound} disabled={planReadOnly} onChange={s('timeBound')} />
              </div>
              <div className="field">
                <label>Company trait</label>
                <select value={goal.companyTraitId ?? ''} disabled={planReadOnly}
                  onChange={(e) => onChange({ companyTraitId: e.target.value === '' ? null : Number(e.target.value) })}>
                  <option value="">Select trait…</option>
                  {traits.map((t) => <option key={t.id} value={t.id}>{t.name}</option>)}
                </select>
              </div>
            </div>
          )}

          {/* Progress tracking — editable after submission, but locks once the mid-year review is submitted */}
          <div style={{ borderTop: '1px dashed var(--line)', marginTop: 12, paddingTop: 12 }}>
            <label style={{ marginBottom: 8 }}>Progress</label>
            <div className="grid-3">
              <div className="field">
                <label>Status</label>
                <select value={goal.status} disabled={progressLocked} onChange={(e) => onChange({ status: e.target.value as GoalStatus })}>
                  {STATUSES.map((st) => <option key={st} value={st}>{STATUS_LABEL[st]}</option>)}
                </select>
              </div>
              <div className="field">
                <label>Completion: {goal.completionPercentage}%</label>
                <input type="range" min={0} max={100} step={5} value={goal.completionPercentage} disabled={progressLocked}
                  onChange={(e) => onChange({ completionPercentage: Number(e.target.value) })} />
              </div>
              <div className="field">
                <label>Date</label>
                <input type="date" value={goal.statusDate ?? ''} disabled={progressLocked} onChange={(e) => onChange({ statusDate: e.target.value || null })} />
              </div>
            </div>
            <div className="field">
              <label>Progress comment</label>
              <input value={goal.statusComment ?? ''} disabled={progressLocked} onChange={(e) => onChange({ statusComment: e.target.value })}
                placeholder="What's the latest on this goal?" />
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
