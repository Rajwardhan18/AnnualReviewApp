interface Props {
  /** Height of the sunrise mark in px. */
  size?: number
  /** Font size of the wordmark (defaults to ~0.82 × size). */
  wordSize?: number
  /** Colour of the "ARIS" part of the wordmark ("e" is always the accent gold). */
  wordColor?: string
  /** Set false to render just the sunrise mark. */
  showWord?: boolean
}

/**
 * ARISe brand mark — a rising sun ("rise to excellence") beside the ARISe wordmark.
 * ARISe = Achieve · Reflect · Innovate · Strategize (for Excellence).
 */
export default function AriseLogo({ size = 40, wordSize, wordColor = '#ffffff', showWord = true }: Props) {
  const w = wordSize ?? size * 0.82
  const gid = 'ariseSun'
  return (
    <span className="arise-logo" style={{ display: 'inline-flex', alignItems: 'center', gap: size * 0.3 }}>
      <svg width={size} height={size} viewBox="0 0 100 100" fill="none"
        xmlns="http://www.w3.org/2000/svg" role="img" aria-label="ARISe" style={{ display: 'block' }}>
        <defs>
          <linearGradient id={gid} x1="50" y1="66" x2="50" y2="40" gradientUnits="userSpaceOnUse">
            <stop offset="0" stopColor="#0e9f6e" />
            <stop offset="1" stopColor="#f6b93b" />
          </linearGradient>
        </defs>
        {/* rays */}
        <g stroke="#f6b93b" strokeWidth="5" strokeLinecap="round">
          <line x1="50" y1="12" x2="50" y2="26" />
          <line x1="27" y1="21" x2="34" y2="33" />
          <line x1="73" y1="21" x2="66" y2="33" />
          <line x1="12" y1="42" x2="24" y2="47" />
          <line x1="88" y1="42" x2="76" y2="47" />
        </g>
        {/* rising sun (half disc) */}
        <path d="M28 66 A22 22 0 0 1 72 66 Z" fill={`url(#${gid})`} />
        {/* horizon */}
        <rect x="18" y="70" width="64" height="5" rx="2.5" fill="#0e9f6e" />
      </svg>
      {showWord && (
        <span className="arise-word" style={{ fontSize: w, color: wordColor }}>
          ARIS<span className="arise-e">e</span>
        </span>
      )}
    </span>
  )
}
