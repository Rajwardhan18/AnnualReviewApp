import { useEffect, useState, type ReactNode } from 'react'
import { NavLink, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { get } from '../api/client'
import AriseLogo from './AriseLogo'

interface NavItem { to: string; label: string; icon: string; end?: boolean; badge?: number }

export default function Layout({ children }: { children: ReactNode }) {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [collapsed, setCollapsed] = useState<boolean>(
    () => localStorage.getItem('nav_collapsed') === '1',
  )
  const [unread, setUnread] = useState(0)

  // Keep the notifications badge fresh as the user navigates.
  useEffect(() => {
    get<{ count: number }>('/api/notifications/mine/unread-count')
      .then((r) => setUnread(r.count))
      .catch(() => {})
  }, [location.pathname])

  const toggle = () => {
    setCollapsed((c) => {
      localStorage.setItem('nav_collapsed', c ? '0' : '1')
      return !c
    })
  }

  const onLogout = () => {
    logout()
    navigate('/login')
  }

  const items: NavItem[] = [
    { to: '/', label: 'Dashboard', icon: '▦', end: true },
    { to: '/notifications', label: 'Notifications', icon: '🔔', badge: unread },
  ]
  if (user?.userType === 'Admin') {
    items.push({ to: '/admin/ratings', label: 'Ratings', icon: '📊' })
    items.push({ to: '/admin/users', label: 'Users', icon: '👥' })
    items.push({ to: '/admin', label: 'Organization', icon: '⚙︎' })
  }

  const initials = (user?.fullName ?? '?')
    .split(' ').map((p) => p[0]).slice(0, 2).join('').toUpperCase()

  return (
    <div className={`app-shell${collapsed ? ' collapsed' : ''}`}>
      <aside className="sidebar">
        <div className="side-brand">
          <AriseLogo size={26} wordSize={20} wordColor="var(--brand-ink)" showWord={!collapsed} />
          <button className="ghost small side-toggle" onClick={toggle} title={collapsed ? 'Expand' : 'Collapse'}>
            {collapsed ? '»' : '«'}
          </button>
        </div>

        <nav className="side-nav">
          {items.map((it) => (
            <NavLink key={it.to} to={it.to} end={it.end}
              className={({ isActive }) => `side-link${isActive ? ' active' : ''}`}
              title={collapsed ? it.label : undefined}>
              <span className="side-icon">{it.icon}</span>
              {!collapsed && <span className="side-label">{it.label}</span>}
              {it.badge ? <span className="nav-badge">{it.badge}</span> : null}
            </NavLink>
          ))}
        </nav>

        <div className="side-foot">
          <div className="side-user" title={user?.fullName}>
            <span className="avatar">{initials}</span>
            {!collapsed && (
              <span className="side-user-meta">
                <strong>{user?.fullName}</strong>
                <span className="muted">{user?.userType}{user?.roleName ? ` · ${user.roleName}` : ''}</span>
              </span>
            )}
          </div>
          <button className="secondary small side-logout" onClick={onLogout} title="Log out">
            <span className="side-icon">⎋</span>{!collapsed && <span>Log out</span>}
          </button>
        </div>
      </aside>

      <div className="content-area">
        <main className="container">{children}</main>
      </div>
    </div>
  )
}
