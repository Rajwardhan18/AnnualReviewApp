export type IconName =
  | 'dashboard' | 'bell' | 'chart' | 'users' | 'org'
  | 'logout' | 'chevron-left' | 'chevron-right'

/** Minimalist flat line icons (single-stroke, currentColor). */
export function Icon({ name, size = 18 }: { name: IconName; size?: number }) {
  const c = {
    width: size, height: size, viewBox: '0 0 24 24', fill: 'none',
    stroke: 'currentColor', strokeWidth: 1.8,
    strokeLinecap: 'round' as const, strokeLinejoin: 'round' as const,
    'aria-hidden': true, style: { display: 'block' as const },
  }
  switch (name) {
    case 'dashboard':
      return (<svg {...c}><rect x="3" y="3" width="7" height="7" rx="1.6" /><rect x="14" y="3" width="7" height="7" rx="1.6" /><rect x="14" y="14" width="7" height="7" rx="1.6" /><rect x="3" y="14" width="7" height="7" rx="1.6" /></svg>)
    case 'bell':
      return (<svg {...c}><path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9" /><path d="M13.7 21a2 2 0 0 1-3.4 0" /></svg>)
    case 'chart':
      return (<svg {...c}><line x1="5" y1="21" x2="5" y2="11" /><line x1="12" y1="21" x2="12" y2="4" /><line x1="19" y1="21" x2="19" y2="14" /></svg>)
    case 'users':
      return (<svg {...c}><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" /><circle cx="9" cy="7" r="4" /><path d="M22 21v-2a4 4 0 0 0-3-3.87" /><path d="M16 3.13a4 4 0 0 1 0 7.75" /></svg>)
    case 'org':
      return (<svg {...c}><line x1="4" y1="7.5" x2="20" y2="7.5" /><line x1="4" y1="16.5" x2="20" y2="16.5" /><circle cx="9" cy="7.5" r="2.4" fill="currentColor" stroke="none" /><circle cx="15" cy="16.5" r="2.4" fill="currentColor" stroke="none" /></svg>)
    case 'logout':
      return (<svg {...c}><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" /><polyline points="16 17 21 12 16 7" /><line x1="21" y1="12" x2="9" y2="12" /></svg>)
    case 'chevron-left':
      return (<svg {...c}><polyline points="15 18 9 12 15 6" /></svg>)
    case 'chevron-right':
      return (<svg {...c}><polyline points="9 18 15 12 9 6" /></svg>)
  }
}
