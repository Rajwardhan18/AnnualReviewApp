import { useEffect, useMemo, useState } from 'react'
import { get } from '../api/client'
import type { Cycle, DeveloperRatingRow, RatingsDashboard } from '../types'

const BAND_COLORS: Record<string, { bg: string; fg: string; fill: string }> = {
  'Outstanding': { bg: '#e2f6ec', fg: '#167a4c', fill: 'rgba(31,157,100,0.14)' },
  'Exceeds': { bg: '#e6f0ff', fg: '#2f5fd0', fill: 'rgba(63,91,217,0.12)' },
  'Meets': { bg: '#eef1f6', fg: '#62708a', fill: 'rgba(120,130,150,0.10)' },
  'Below': { bg: '#fff3dc', fg: '#a86b00', fill: 'rgba(217,138,0,0.12)' },
  'Needs Improvement': { bg: '#fdecec', fg: '#a02929', fill: 'rgba(214,69,69,0.12)' },
}
const BAND_ORDER = ['Needs Improvement', 'Below', 'Meets', 'Exceeds', 'Outstanding']

function BandBadge({ band }: { band: string | null }) {
  if (!band) return <span className="muted">—</span>
  const c = BAND_COLORS[band] ?? BAND_COLORS['Meets']
  return <span className="badge" style={{ background: c.bg, color: c.fg }}>{band}</span>
}

export default function RatingsDashboardPage() {
  const [cycles, setCycles] = useState<Cycle[]>([])
  const [cycleId, setCycleId] = useState<number | ''>('')
  const [data, setData] = useState<RatingsDashboard | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    get<Cycle[]>('/api/cycles').then((cs) => {
      setCycles(cs)
      const active = cs.find((c) => c.isActive) ?? cs[0]
      if (active) setCycleId(active.id)
    })
  }, [])

  useEffect(() => {
    if (cycleId === '') return
    setLoading(true)
    get<RatingsDashboard>(`/api/dashboard/ratings?cycleId=${cycleId}`)
      .then(setData)
      .finally(() => setLoading(false))
  }, [cycleId])

  const rated = useMemo(() => (data?.developers ?? []).filter((d) => d.weightedFinal !== null), [data])

  return (
    <>
      <div className="page-head">
        <h1>Developer Ratings</h1>
        <p>Weighted, normalized final ratings and their fit on a normal curve.</p>
      </div>

      <div className="card">
        <div className="btn-row" style={{ justifyContent: 'space-between' }}>
          <div className="btn-row">
            <label style={{ margin: 0 }}>Cycle:</label>
            <select style={{ width: 'auto' }} value={cycleId}
              onChange={(e) => setCycleId(e.target.value === '' ? '' : Number(e.target.value))}>
              {cycles.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
            </select>
          </div>
          {data && (
            <div className="muted" style={{ fontSize: 13 }}>
              Weights — Self {pct(data.weights.self)} · Peer {pct(data.weights.peer)} ·
              Mgr 1 {pct(data.weights.manager1)} · Mgr 2 {pct(data.weights.manager2)}
            </div>
          )}
        </div>
      </div>

      {loading || !data ? <div className="loading">Loading…</div> : (
        <>
          {rated.length === 0 ? (
            <div className="card"><div className="empty">No completed ratings for this cycle yet.</div></div>
          ) : (
            <>
              <div className="card">
                <h2>Normal-curve fit</h2>
                <p className="section-hint">
                  Mean <strong>{data.curve.mean}</strong> · Std dev <strong>{data.curve.stdDev}</strong> ·
                  {' '}{data.curve.count} developer{data.curve.count === 1 ? '' : 's'} rated.
                  Each dot is a developer placed at their final score; shaded regions are performance bands (±½σ, ±1½σ).
                </p>
                <BellCurve data={data} rated={rated} />
                <div className="pill-row" style={{ marginTop: 14 }}>
                  {BAND_ORDER.map((band) => {
                    const b = data.curve.buckets.find((x) => x.band === band)
                    const c = BAND_COLORS[band]
                    return (
                      <span key={band} className="badge" style={{ background: c.bg, color: c.fg }}>
                        {band}: {b?.count ?? 0}
                      </span>
                    )
                  })}
                </div>
              </div>

              <div className="card">
                <h2>Ratings breakdown</h2>
                <div style={{ overflowX: 'auto' }}>
                  <table>
                    <thead>
                      <tr>
                        <th>Developer</th>
                        <th>Function / Role</th>
                        <th>Self<br /><span className="muted">10%</span></th>
                        <th>Peer<br /><span className="muted">20%</span></th>
                        <th>Mgr 1<br /><span className="muted">30%</span></th>
                        <th>Mgr 2<br /><span className="muted">40%</span></th>
                        <th>Weighted<br /><span className="muted">final</span></th>
                        <th>z-score</th>
                        <th>Percentile</th>
                        <th>Curved</th>
                        <th>Band</th>
                      </tr>
                    </thead>
                    <tbody>
                      {data.developers.map((d) => (
                        <tr key={d.reviewId}>
                          <td><strong>{d.developerName}</strong></td>
                          <td className="muted">{d.functionName ? `${d.functionName} · ${d.roleName}` : '—'}</td>
                          <td>{num(d.selfScore)}</td>
                          <td>{num(d.peerScore)}</td>
                          <td>{num(d.manager1Score)}</td>
                          <td>{num(d.manager2Score)}</td>
                          <td><strong>{d.weightedFinal ?? '—'}</strong></td>
                          <td>{d.zScore ?? '—'}</td>
                          <td>{d.percentile !== null ? `${d.percentile}%` : '—'}</td>
                          <td>{d.curvedScore ?? '—'}</td>
                          <td><BandBadge band={d.band} /></td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            </>
          )}
        </>
      )}
    </>
  )
}

function BellCurve({ data, rated }: { data: RatingsDashboard; rated: DeveloperRatingRow[] }) {
  const W = 720, H = 240
  const mL = 24, mR = 24, top = 16, baseY = 190
  const plotW = W - mL - mR
  const { mean, stdDev } = data.curve
  const std = stdDev > 0 ? stdDev : 1 // avoid divide-by-zero; flat cohort renders a centered marker

  // x domain fixed to the 1-10 rating scale for a stable axis.
  const xMin = 1, xMax = 10
  const xScale = (score: number) => mL + ((score - xMin) / (xMax - xMin)) * plotW
  const gauss = (x: number) => Math.exp(-0.5 * ((x - mean) / std) ** 2) // peak = 1 at mean

  // Gaussian path (peak scaled to plot height).
  const peakH = baseY - top
  const samples = 120
  const pts: string[] = []
  for (let i = 0; i <= samples; i++) {
    const x = xMin + (i / samples) * (xMax - xMin)
    const y = baseY - gauss(x) * peakH
    pts.push(`${i === 0 ? 'M' : 'L'}${xScale(x).toFixed(1)},${y.toFixed(1)}`)
  }
  const curvePath = pts.join(' ')

  // Band region boundaries at z = ±0.5, ±1.5 → score = mean + z*std, clamped to [1,10].
  const cut = (z: number) => Math.min(xMax, Math.max(xMin, mean + z * std))
  const regions = [
    { band: 'Needs Improvement', from: xMin, to: cut(-1.5) },
    { band: 'Below', from: cut(-1.5), to: cut(-0.5) },
    { band: 'Meets', from: cut(-0.5), to: cut(0.5) },
    { band: 'Exceeds', from: cut(0.5), to: cut(1.5) },
    { band: 'Outstanding', from: cut(1.5), to: xMax },
  ]

  // Jitter dots vertically a touch when finals collide.
  const seen: Record<string, number> = {}

  return (
    <div style={{ overflowX: 'auto' }}>
      <svg viewBox={`0 0 ${W} ${H}`} width="100%" style={{ maxWidth: W, display: 'block' }} role="img"
        aria-label="Normal curve of developer final ratings">
        {/* band regions */}
        {regions.map((r) => (
          r.to > r.from && (
            <rect key={r.band} x={xScale(r.from)} y={top} width={xScale(r.to) - xScale(r.from)}
              height={baseY - top} fill={BAND_COLORS[r.band].fill} />
          )
        ))}
        {/* baseline */}
        <line x1={mL} y1={baseY} x2={W - mR} y2={baseY} stroke="#c9d2e0" />
        {/* mean line */}
        <line x1={xScale(Math.min(xMax, Math.max(xMin, mean)))} y1={top}
          x2={xScale(Math.min(xMax, Math.max(xMin, mean)))} y2={baseY}
          stroke="#8894ab" strokeDasharray="4 4" />
        <text x={xScale(Math.min(xMax, Math.max(xMin, mean)))} y={top - 4} textAnchor="middle"
          fontSize="10" fill="#6b7684">μ={data.curve.mean}</text>
        {/* gaussian curve */}
        <path d={curvePath} fill="none" stroke="#3f5bd9" strokeWidth={2} />
        {/* x axis ticks */}
        {Array.from({ length: 10 }, (_, i) => i + 1).map((s) => (
          <g key={s}>
            <line x1={xScale(s)} y1={baseY} x2={xScale(s)} y2={baseY + 4} stroke="#c9d2e0" />
            <text x={xScale(s)} y={baseY + 16} textAnchor="middle" fontSize="10" fill="#6b7684">{s}</text>
          </g>
        ))}
        {/* developer dots */}
        {rated.map((d) => {
          const key = d.weightedFinal!.toFixed(1)
          const n = (seen[key] = (seen[key] ?? 0) + 1)
          const cx = xScale(d.weightedFinal!)
          const cy = baseY - 10 - (n - 1) * 16
          const c = BAND_COLORS[d.band ?? 'Meets']
          return (
            <g key={d.reviewId}>
              <circle cx={cx} cy={cy} r={6} fill={c.fg} stroke="#fff" strokeWidth={1.5}>
                <title>{d.developerName}: {d.weightedFinal} ({d.band}, z={d.zScore})</title>
              </circle>
              <text x={cx} y={cy - 9} textAnchor="middle" fontSize="9" fill="#3a4150">
                {initials(d.developerName)}
              </text>
            </g>
          )
        })}
      </svg>
    </div>
  )
}

const pct = (w: number) => `${Math.round(w * 100)}%`
const num = (v: number | null) => (v === null ? <span className="muted">—</span> : v)
const initials = (name: string) => name.split(' ').map((p) => p[0]).slice(0, 2).join('').toUpperCase()
