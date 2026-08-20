interface Props {
  height?: number
  /** Colour of the "sparrow" wordmark. */
  wordColor?: string
  /** Thin stroke between facets — match the surface behind the logo. */
  facetStroke?: string
}

/**
 * Sparrow brand mark: the "sparrow" wordmark next to a low-poly green bird.
 * This is an SVG recreation — drop the official asset in to replace it (see LoginPage).
 */
export default function SparrowLogo({ height = 40, wordColor = '#e9eef0', facetStroke = 'rgba(0,0,0,0.12)' }: Props) {
  const birdSize = height * 1.05
  return (
    <span className="sparrow-logo" style={{ display: 'inline-flex', alignItems: 'center', gap: height * 0.3 }}>
      <span className="sparrow-word" style={{ color: wordColor, fontSize: height * 0.82, lineHeight: 1 }}>sparrow</span>
      <svg width={birdSize} height={birdSize} viewBox="0 0 100 100" fill="none"
        xmlns="http://www.w3.org/2000/svg" role="img" aria-label="Sparrow logo"
        style={{ display: 'block' }}>
        <g stroke={facetStroke} strokeWidth="0.75" strokeLinejoin="round">
          {/* head + beak */}
          <polygon points="72,24 90,16 79,37" fill="#78e08f" />
          {/* upper back */}
          <polygon points="72,24 79,37 57,33" fill="#2fc463" />
          {/* leading wing */}
          <polygon points="72,24 57,33 49,15" fill="#54d477" />
          {/* wing tip */}
          <polygon points="49,15 57,33 33,28" fill="#0f8f45" />
          {/* wing underside */}
          <polygon points="33,28 57,33 43,49" fill="#1aa752" />
          {/* chest */}
          <polygon points="57,33 79,37 62,53" fill="#16a04c" />
          {/* body */}
          <polygon points="57,33 62,53 43,49" fill="#3ccb6a" />
          {/* belly */}
          <polygon points="43,49 62,53 49,68" fill="#69dd85" />
          {/* upper tail */}
          <polygon points="43,49 49,68 28,60" fill="#0c7d3c" />
          {/* tail fork */}
          <polygon points="28,60 49,68 37,84" fill="#149a4b" />
          {/* tail tip */}
          <polygon points="28,60 37,84 20,73" fill="#0a5f30" />
        </g>
      </svg>
    </span>
  )
}
