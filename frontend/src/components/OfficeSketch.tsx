function Workstation({ transform }: { transform?: string }) {
  return (
    <g transform={transform}>
      {/* screen glow */}
      <circle className="os-glow" cx="124" cy="120" r="26" />
      {/* desk */}
      <path className="os-stroke os-faint" d="M4 150 H156" />
      <path className="os-stroke os-faint" d="M16 150 V176 M144 150 V176" />
      {/* monitor */}
      <rect className="os-screen" x="100" y="104" width="48" height="32" rx="3" />
      <path className="os-stroke" d="M124 136 V144 M112 144 H136" />
      <path className="os-code" d="M106 113 H132" />
      <path className="os-code os-gold" d="M106 119 H140" />
      <path className="os-code" d="M106 125 H124" />
      <rect className="os-cursor" x="128" y="121.5" width="4" height="6" rx="1" />
      {/* developer */}
      <path className="os-stroke os-faint" d="M10 150 L6 124 M6 150 H30" />
      <path className="os-stroke" d="M22 150 V120" />
      <circle className="os-stroke" cx="28" cy="109" r="8.5" />
      <path className="os-arm os-stroke" d="M24 122 L70 142" />
      <path className="os-stroke os-faint" d="M58 146 H92" />
    </g>
  )
}

/** Hand-sketch animation of developers at work, for the ARISe landing hero. */
export default function OfficeSketch() {
  return (
    <svg className="office-sketch" viewBox="0 0 440 200" role="img"
      aria-label="Sketch of developers working at their desks in an office"
      xmlns="http://www.w3.org/2000/svg">
      <style>{`
        .os-stroke { fill: none; stroke: rgba(255,255,255,.6); stroke-width: 1.6; stroke-linecap: round; stroke-linejoin: round; }
        .os-faint { stroke: rgba(255,255,255,.3); }
        .os-screen { fill: rgba(95,224,168,.07); stroke: rgba(255,255,255,.6); stroke-width: 1.6; }
        .os-code { fill: none; stroke: #6fe3ab; stroke-width: 2.6; stroke-linecap: round; opacity: .9; }
        .os-gold { stroke: #f6c445; }
        .os-cursor { fill: #f6c445; animation: os-blink 1.1s steps(1) infinite; }
        .os-glow { fill: #f6c445; opacity: .06; animation: os-glow 3.6s ease-in-out infinite; }
        .os-arm { transform-box: fill-box; transform-origin: 0 50%; animation: os-type 1.7s ease-in-out infinite; }
        .os-leaf { transform-box: fill-box; transform-origin: 50% 100%; animation: os-sway 4.5s ease-in-out infinite; }
        .os-leaf.l2 { animation-delay: -1.2s; } .os-leaf.l3 { animation-delay: -2.4s; }
        .os-steam { fill: none; stroke: rgba(255,255,255,.34); stroke-width: 1.4; stroke-linecap: round; opacity: 0; animation: os-steam 3.4s ease-out infinite; }
        .os-steam.s2 { animation-delay: 1.7s; }
        .os-hand { transform-box: view-box; transform-origin: 232px 46px; animation: os-clock 10s linear infinite; }
        @keyframes os-blink { 0%,48% { opacity: 1 } 50%,100% { opacity: 0 } }
        @keyframes os-glow { 0%,100% { opacity: .05 } 50% { opacity: .15 } }
        @keyframes os-type { 0%,100% { transform: translateY(0) } 50% { transform: translateY(1.6px) } }
        @keyframes os-sway { 0%,100% { transform: rotate(-5deg) } 50% { transform: rotate(5deg) } }
        @keyframes os-steam { 0% { opacity: 0; transform: translateY(0) } 30% { opacity: .5 } 100% { opacity: 0; transform: translateY(-13px) } }
        @keyframes os-clock { to { transform: rotate(360deg) } }
        @media (prefers-reduced-motion: reduce) {
          .os-cursor, .os-glow, .os-arm, .os-leaf, .os-steam, .os-hand { animation: none; }
          .os-cursor { opacity: 1; }
        }
      `}</style>

      {/* floor */}
      <path className="os-stroke os-faint" d="M16 176 H424" />

      {/* window (top-left) */}
      <rect className="os-stroke os-faint" x="26" y="22" width="74" height="48" rx="2" />
      <path className="os-stroke os-faint" d="M63 22 V70 M26 46 H100" />

      {/* wall clock */}
      <circle className="os-stroke" cx="232" cy="46" r="14" />
      <circle cx="232" cy="46" r="1.7" fill="rgba(255,255,255,.6)" />
      <path className="os-stroke" d="M232 46 V39" />
      <path className="os-stroke os-faint" d="M232 46 L239 48" />
      <g className="os-hand"><path className="os-gold" style={{ strokeWidth: 1.4, fill: 'none', strokeLinecap: 'round' }} d="M232 46 V34" /></g>

      {/* ARISe poster (top-right) */}
      <rect className="os-stroke os-faint" x="344" y="20" width="66" height="46" rx="3" />
      <rect x="352" y="29" width="18" height="18" rx="5" fill="#128a3d" />
      <path d="M356 41 L361 36 L366 41" fill="none" stroke="#fff" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" />
      <path className="os-stroke os-faint" d="M378 32 H402 M378 38 H398 M378 44 H394" />

      {/* workstations */}
      <Workstation transform="translate(28,0)" />
      <Workstation transform="translate(412,0) scale(-1,1)" />

      {/* centre: plant + coffee */}
      <g>
        <path className="os-stroke os-faint" d="M196 176 L198 160 H214 L216 176" />
        <path className="os-leaf os-code" d="M206 160 C 194 152 192 140 200 132" />
        <path className="os-leaf l2 os-code" d="M206 160 C 206 148 206 140 206 130" />
        <path className="os-leaf l3 os-code" d="M206 160 C 218 152 220 140 212 132" />
      </g>
      <g transform="translate(230,150)">
        <path className="os-stroke" d="M0 2 h11 v8 a2.5 2.5 0 0 1 -2.5 2.5 h-6 a2.5 2.5 0 0 1 -2.5 -2.5 z" />
        <path className="os-stroke" d="M11 4 h2.5 a2.5 2.5 0 0 1 0 5 h-2.5" />
        <path className="os-steam" d="M3 0 c -2 -3 2 -5 0 -8" />
        <path className="os-steam s2" d="M8 0 c -2 -3 2 -5 0 -8" />
      </g>
    </svg>
  )
}
