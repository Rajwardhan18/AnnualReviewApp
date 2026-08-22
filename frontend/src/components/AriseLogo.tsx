interface Props {
  /** Height of the mark in px. */
  size?: number
  /** Font size of the wordmark (defaults to ~0.82 × size). */
  wordSize?: number
  /** Colour of the "ARIS" part of the wordmark ("e" is always the accent gold). */
  wordColor?: string
  /** Set false to render just the mark. */
  showWord?: boolean
}

/**
 * ARISe brand mark — a modern gradient app-tile with ascending chevrons ("rise").
 * ARISe = Achieve · Reflect · Innovate · Strategize (for Excellence).
 */
export default function AriseLogo({ size = 40, wordSize, wordColor = '#ffffff', showWord = true }: Props) {
  const w = wordSize ?? size * 0.82
  return (
    <span className="arise-logo" style={{ display: 'inline-flex', alignItems: 'center', gap: size * 0.32 }}>
      <svg width={size} height={size} viewBox="0 0 100 100" fill="none"
        xmlns="http://www.w3.org/2000/svg" role="img" aria-label="ARISe" style={{ display: 'block' }}>
        <defs>
          <linearGradient id="ariseTile" x1="14" y1="90" x2="88" y2="12" gradientUnits="userSpaceOnUse">
            <stop offset="0" stopColor="#0b7c56" />
            <stop offset="0.55" stopColor="#12b981" />
            <stop offset="1" stopColor="#5fe0a8" />
          </linearGradient>
          <radialGradient id="ariseGlow" cx="82" cy="18" r="46" gradientUnits="userSpaceOnUse">
            <stop offset="0" stopColor="#f6c445" stopOpacity="0.6" />
            <stop offset="1" stopColor="#f6c445" stopOpacity="0" />
          </radialGradient>
        </defs>
        <rect x="6" y="6" width="88" height="88" rx="26" fill="url(#ariseTile)" />
        <rect x="6" y="6" width="88" height="88" rx="26" fill="url(#ariseGlow)" />
        <g fill="none" stroke="#ffffff" strokeWidth="10" strokeLinecap="round" strokeLinejoin="round">
          <path d="M30 63 L50 45 L70 63" />
          <path d="M34 50 L50 34 L66 50" strokeOpacity="0.92" />
        </g>
      </svg>
      {showWord && (
        <span className="arise-word" style={{ fontSize: w, color: wordColor }}>
          ARIS<span className="arise-e">e</span>
        </span>
      )}
    </span>
  )
}
