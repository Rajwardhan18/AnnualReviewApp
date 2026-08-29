import { useEffect, useState } from 'react'
import { get, post, put, ApiError } from '../api/client'
import type { CompanyTrait, Cycle, FunctionItem, Role, Skill } from '../types'

type Tab = 'org' | 'skills' | 'mapping' | 'traits' | 'cycles'

export default function AdminPage() {
  const [tab, setTab] = useState<Tab>('org')
  return (
    <>
      <div className="page-head">
        <h1>Admin Console</h1>
        <p>Manage the organisation model, skills, traits and review cycles.</p>
      </div>
      <div className="tabs">
        <button className={tab === 'org' ? 'active' : ''} onClick={() => setTab('org')}>Functions &amp; Roles</button>
        <button className={tab === 'skills' ? 'active' : ''} onClick={() => setTab('skills')}>Skills</button>
        <button className={tab === 'mapping' ? 'active' : ''} onClick={() => setTab('mapping')}>Role → Skills</button>
        <button className={tab === 'traits' ? 'active' : ''} onClick={() => setTab('traits')}>Company Traits</button>
        <button className={tab === 'cycles' ? 'active' : ''} onClick={() => setTab('cycles')}>Cycles</button>
      </div>
      {tab === 'org' && <OrgTab />}
      {tab === 'skills' && <SkillsTab />}
      {tab === 'mapping' && <MappingTab />}
      {tab === 'traits' && <TraitsTab />}
      {tab === 'cycles' && <CyclesTab />}
    </>
  )
}

function useErr() {
  const [err, setErr] = useState('')
  const wrap = async (fn: () => Promise<void>) => {
    setErr('')
    try { await fn() } catch (e: any) { setErr(e instanceof ApiError ? e.message : (e.message || 'Error')) }
  }
  return { err, wrap }
}

function OrgTab() {
  const [functions, setFunctions] = useState<FunctionItem[]>([])
  const [roles, setRoles] = useState<Role[]>([])
  const [fName, setFName] = useState('')
  const [rName, setRName] = useState('')
  const [rFunc, setRFunc] = useState<number | ''>('')
  const { err, wrap } = useErr()

  const load = () => {
    get<FunctionItem[]>('/api/functions').then(setFunctions)
    get<Role[]>('/api/roles').then(setRoles)
  }
  useEffect(() => { load() }, [])

  return (
    <>
      {err && <div className="error">{err}</div>}
      <div className="grid-2">
        <div className="card">
          <h2>Functions</h2>
          <p className="section-hint">Developer disciplines (e.g. Frontend, Backend).</p>
          <div className="field"><label>New function name</label><input value={fName} onChange={(e) => setFName(e.target.value)} /></div>
          <button onClick={() => wrap(async () => { await post('/api/functions', { name: fName }); setFName(''); load() })}>Add function</button>
          <table style={{ marginTop: 14 }}>
            <tbody>{functions.map((f) => <tr key={f.id}><td>{f.name}</td><td className="muted">{f.description}</td></tr>)}</tbody>
          </table>
        </div>
        <div className="card">
          <h2>Roles</h2>
          <p className="section-hint">Career roles within a function (e.g. SDE-1, SDE-2).</p>
          <div className="field"><label>Function</label>
            <select value={rFunc} onChange={(e) => setRFunc(e.target.value === '' ? '' : Number(e.target.value))}>
              <option value="">Select…</option>
              {functions.map((f) => <option key={f.id} value={f.id}>{f.name}</option>)}
            </select>
          </div>
          <div className="field"><label>Role name</label><input value={rName} onChange={(e) => setRName(e.target.value)} /></div>
          <button disabled={rFunc === ''} onClick={() => wrap(async () => { await post('/api/roles', { name: rName, functionId: Number(rFunc) }); setRName(''); load() })}>Add role</button>
          <table style={{ marginTop: 14 }}>
            <tbody>{roles.map((r) => <tr key={r.id}><td>{r.name}</td><td className="muted">{r.functionName}</td></tr>)}</tbody>
          </table>
        </div>
      </div>
    </>
  )
}

function SkillsTab() {
  const [skills, setSkills] = useState<Skill[]>([])
  const [name, setName] = useState('')
  const [category, setCategory] = useState('')
  const { err, wrap } = useErr()
  const load = () => get<Skill[]>('/api/skills').then(setSkills)
  useEffect(() => { load() }, [])
  return (
    <div className="card">
      <h2>Skills master</h2>
      <p className="section-hint">The master list of skills that can be mapped to roles.</p>
      {err && <div className="error">{err}</div>}
      <div className="grid-2">
        <div className="field"><label>Skill name</label><input value={name} onChange={(e) => setName(e.target.value)} /></div>
        <div className="field"><label>Category (optional)</label><input value={category} onChange={(e) => setCategory(e.target.value)} /></div>
      </div>
      <button onClick={() => wrap(async () => { await post('/api/skills', { name, category: category || null }); setName(''); setCategory(''); load() })}>Add skill</button>
      <table style={{ marginTop: 14 }}>
        <thead><tr><th>Skill</th><th>Category</th></tr></thead>
        <tbody>{skills.map((s) => <tr key={s.id}><td>{s.name}</td><td className="muted">{s.category || '—'}</td></tr>)}</tbody>
      </table>
    </div>
  )
}

function MappingTab() {
  const [roles, setRoles] = useState<Role[]>([])
  const [skills, setSkills] = useState<Skill[]>([])
  const [roleId, setRoleId] = useState<number | ''>('')
  const [selected, setSelected] = useState<Set<number>>(new Set())
  const { err, wrap } = useErr()
  const [msg, setMsg] = useState('')

  useEffect(() => {
    get<Role[]>('/api/roles').then(setRoles)
    get<Skill[]>('/api/skills').then(setSkills)
  }, [])

  useEffect(() => {
    setMsg('')
    if (roleId === '') { setSelected(new Set()); return }
    get<Skill[]>(`/api/roles/${roleId}/skills`).then((rs) => setSelected(new Set(rs.map((s) => s.id))))
  }, [roleId])

  const toggle = (id: number) => setSelected((s) => {
    const n = new Set(s); n.has(id) ? n.delete(id) : n.add(id); return n
  })

  return (
    <div className="card">
      <h2>Map skills to a role</h2>
      <p className="section-hint">Select the skills required for a role. These become the skills each developer rates.</p>
      {err && <div className="error">{err}</div>}
      {msg && <div className="success">{msg}</div>}
      <div className="field" style={{ maxWidth: 420 }}>
        <label>Role</label>
        <select value={roleId} onChange={(e) => setRoleId(e.target.value === '' ? '' : Number(e.target.value))}>
          <option value="">Select a role…</option>
          {roles.map((r) => <option key={r.id} value={r.id}>{r.functionName} · {r.name}</option>)}
        </select>
      </div>
      {roleId !== '' && (
        <>
          <div className="grid-3">
            {skills.map((s) => (
              <label key={s.id} style={{ display: 'flex', gap: 8, alignItems: 'center', fontWeight: 400 }}>
                <input type="checkbox" style={{ width: 'auto' }} checked={selected.has(s.id)} onChange={() => toggle(s.id)} />
                {s.name} {s.category && <span className="muted">({s.category})</span>}
              </label>
            ))}
          </div>
          <button style={{ marginTop: 14 }} onClick={() => wrap(async () => {
            await put('/api/roles/skills', { roleId: Number(roleId), skillIds: [...selected] })
            setMsg('Role skills updated.')
          })}>Save mapping</button>
        </>
      )}
    </div>
  )
}

function TraitsTab() {
  const [traits, setTraits] = useState<CompanyTrait[]>([])
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const { err, wrap } = useErr()
  const load = () => get<CompanyTrait[]>('/api/traits').then(setTraits)
  useEffect(() => { load() }, [])
  return (
    <div className="card">
      <h2>Company traits</h2>
      <p className="section-hint">Values each goal is tagged against (e.g. Leadership, Ownership, Integrity).</p>
      {err && <div className="error">{err}</div>}
      <div className="grid-2">
        <div className="field"><label>Trait name</label><input value={name} onChange={(e) => setName(e.target.value)} /></div>
        <div className="field"><label>Description (optional)</label><input value={description} onChange={(e) => setDescription(e.target.value)} /></div>
      </div>
      <button onClick={() => wrap(async () => { await post('/api/traits', { name, description: description || null }); setName(''); setDescription(''); load() })}>Add trait</button>
      <table style={{ marginTop: 14 }}>
        <tbody>{traits.map((t) => <tr key={t.id}><td><span className="badge trait">{t.name}</span></td><td className="muted">{t.description}</td></tr>)}</tbody>
      </table>
    </div>
  )
}

function CyclesTab() {
  const [cycles, setCycles] = useState<Cycle[]>([])
  const [name, setName] = useState('')
  const [year, setYear] = useState<number>(new Date().getFullYear())
  const [start, setStart] = useState('')
  const [end, setEnd] = useState('')
  const [due, setDue] = useState('')
  const { err, wrap } = useErr()
  const [msg, setMsg] = useState('')
  const load = () => get<Cycle[]>('/api/cycles').then(setCycles)
  useEffect(() => { load() }, [])

  return (
    <>
      {err && <div className="error">{err}</div>}
      {msg && <div className="success">{msg}</div>}
      <div className="card">
        <h2>Create a cycle</h2>
        <p className="section-hint">A new annual plan &amp; review cycle. Release it to create a review for every developer.</p>
        <div className="grid-2">
          <div className="field"><label>Name</label><input value={name} onChange={(e) => setName(e.target.value)} placeholder="e.g. FY2027 Annual Cycle" /></div>
          <div className="field"><label>Year</label><input type="number" value={year} onChange={(e) => setYear(Number(e.target.value))} /></div>
          <div className="field"><label>Start date</label><input type="date" value={start} onChange={(e) => setStart(e.target.value)} /></div>
          <div className="field"><label>End date</label><input type="date" value={end} onChange={(e) => setEnd(e.target.value)} /></div>
          <div className="field"><label>Plan due date</label><input type="date" value={due} onChange={(e) => setDue(e.target.value)} /></div>
        </div>
        <button onClick={() => wrap(async () => {
          await post('/api/cycles', { name, year, startDate: start || new Date().toISOString(), endDate: end || new Date().toISOString(), dueDate: due || null })
          setName(''); setDue(''); setMsg('Cycle created.'); load()
        })}>Create cycle</button>
      </div>

      <div className="card">
        <h2>Cycles</h2>
        <div style={{ overflowX: 'auto' }}>
        <table>
          <thead><tr><th>Name</th><th>Due</th><th>Reviews</th><th>Annual</th><th>Half-yearly</th><th>Final review</th><th>Ratings</th><th></th></tr></thead>
          <tbody>
            {cycles.map((c) => (
              <tr key={c.id}>
                <td>{c.name} <span className="muted">· {c.year}</span></td>
                <td>{c.dueDate ? new Date(c.dueDate).toLocaleDateString() : <span className="muted">—</span>}</td>
                <td>{c.reviewCount}</td>
                <td>{c.isReleased ? <span className="badge Completed">Released</span> : <span className="badge Draft">Not released</span>}</td>
                <td>{c.halfYearlyReleased
                  ? <span className="badge InProgress">Released{c.halfYearlyDueDate ? ` · due ${new Date(c.halfYearlyDueDate).toLocaleDateString()}` : ''}</span>
                  : <span className="muted">—</span>}</td>
                <td>{c.finalReviewReleased
                  ? <span className="badge InProgress">Released{c.finalReviewDueDate ? ` · due ${new Date(c.finalReviewDueDate).toLocaleDateString()}` : ''}</span>
                  : <span className="muted">—</span>}</td>
                <td className="pill-row">
                  {c.ratingsReleased
                    ? <span className="badge Completed">Ratings released</span>
                    : <span className="muted">—</span>}
                  {c.ended && <span className="badge Dropped">Ended</span>}
                </td>
                <td>
                  <div className="btn-row">
                    {!c.isReleased && (
                      <button className="small" onClick={() => wrap(async () => {
                        const r = await post<{ reviewsCreated: number; totalDevelopers: number; notified: number }>(`/api/cycles/${c.id}/release`)
                        setMsg(`Annual plan released. ${r.reviewsCreated} new review(s); ${r.notified} developer(s) notified.`); load()
                      })}>Release annual</button>
                    )}
                    {c.isReleased && !c.halfYearlyReleased && (
                      <button className="small secondary" onClick={() => wrap(async () => {
                        const r = await post<{ notified: number }>(`/api/cycles/${c.id}/release-halfyearly`, { halfYearlyDueDate: `${c.year}-07-15` })
                        setMsg(`Half-yearly review released; ${r.notified} developer(s) notified.`); load()
                      })}>Release half-yearly</button>
                    )}
                    {c.isReleased && c.halfYearlyReleased && !c.finalReviewReleased && !c.ended && (
                      <button className="small secondary" onClick={() => wrap(async () => {
                        const r = await post<{ notified: number }>(`/api/cycles/${c.id}/release-finalreview`, { finalReviewDueDate: `${c.year}-12-15` })
                        setMsg(`Year-end review released; ${r.notified} developer(s) notified.`); load()
                      })}>Release final review</button>
                    )}
                    {c.isReleased && c.finalReviewReleased && !c.ended && (
                      <button className="small danger" onClick={() => wrap(async () => {
                        await post(`/api/cycles/${c.id}/end`)
                        setMsg('Cycle closed. You can now release ratings.'); load()
                      })}>End cycle</button>
                    )}
                    {c.ended && !c.ratingsReleased && (
                      <button className="small" onClick={() => wrap(async () => {
                        const r = await post<{ notified: number }>(`/api/cycles/${c.id}/release-ratings`)
                        setMsg(`Ratings released; ${r.notified} developer(s) notified.`); load()
                      })}>Release ratings</button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        </div>
      </div>
    </>
  )
}
