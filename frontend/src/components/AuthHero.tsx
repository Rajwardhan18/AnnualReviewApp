import AriseLogo from './AriseLogo'
import SparrowLogo from './SparrowLogo'
import OfficeSketch from './OfficeSketch'

const PILLARS = [
  { word: 'Achieve', color: 'var(--p-achieve)' },
  { word: 'Reflect', color: 'var(--p-reflect)' },
  { word: 'Innovate', color: 'var(--p-innovate)' },
  { word: 'Strategize', color: 'var(--p-strategize)' },
]

/** Branded ARISe hero panel shared by the login and password screens. */
export default function AuthHero() {
  return (
    <div className="auth-hero">
      <AriseLogo size={44} wordSize={34} />
      <p className="hero-tag">
        Achieve, Reflect, Innovate and Strategize — <strong>for excellence</strong>.
        Your annual plan &amp; review, all in one place.
      </p>
      <ul className="auth-pillars">
        {PILLARS.map((p) => (
          <li key={p.word}>
            <span className="pdot" style={{ background: p.color }} />
            <b>{p.word}</b>
          </li>
        ))}
      </ul>
      <div className="hero-scene"><OfficeSketch /></div>
      <div className="hero-foot">
        <span>by</span>
        <SparrowLogo height={16} wordColor="rgba(255,255,255,0.85)" facetStroke="rgba(255,255,255,0.18)" />
      </div>
    </div>
  )
}
