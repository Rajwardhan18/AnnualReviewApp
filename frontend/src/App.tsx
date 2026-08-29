import { Navigate, Route, Routes } from 'react-router-dom'
import { useAuth } from './auth/AuthContext'
import Layout from './components/Layout'
import LoginPage from './pages/LoginPage'
import ChangePasswordPage from './pages/ChangePasswordPage'
import DashboardPage from './pages/DashboardPage'
import ReviewEditorPage from './pages/ReviewEditorPage'
import ReviewViewPage from './pages/ReviewViewPage'
import AdminPage from './pages/AdminPage'
import UsersPage from './pages/UsersPage'
import RatingsDashboardPage from './pages/RatingsDashboardPage'
import NotificationsPage from './pages/NotificationsPage'
import MyPerformancePage from './pages/MyPerformancePage'
import type { ReactNode } from 'react'

function Protected({ children, adminOnly }: { children: ReactNode; adminOnly?: boolean }) {
  const { user, loading } = useAuth()
  if (loading) return <div className="loading">Loading…</div>
  if (!user) return <Navigate to="/login" replace />
  // First-login (or post-reset) users must set a new password before anything else.
  if (user.mustChangePassword) return <Navigate to="/change-password" replace />
  if (adminOnly && user.userType !== 'Admin') return <Navigate to="/" replace />
  return <Layout>{children}</Layout>
}

// Change-password screen: requires a logged-in user, but is reachable whether or
// not the change is forced (so it also serves as a voluntary "change my password").
function ChangePasswordGate() {
  const { user, loading } = useAuth()
  if (loading) return <div className="loading">Loading…</div>
  if (!user) return <Navigate to="/login" replace />
  return <ChangePasswordPage />
}

export default function App() {
  const { user, loading } = useAuth()

  return (
    <Routes>
      <Route path="/login" element={user && !loading ? <Navigate to="/" replace /> : <LoginPage />} />
      <Route path="/change-password" element={<ChangePasswordGate />} />

      <Route path="/" element={<Protected><DashboardPage /></Protected>} />
      <Route path="/reviews/:id/edit" element={<Protected><ReviewEditorPage /></Protected>} />
      <Route path="/reviews/:id" element={<Protected><ReviewViewPage /></Protected>} />
      <Route path="/notifications" element={<Protected><NotificationsPage /></Protected>} />
      <Route path="/performance" element={<Protected><MyPerformancePage /></Protected>} />
      <Route path="/admin/ratings" element={<Protected adminOnly><RatingsDashboardPage /></Protected>} />
      <Route path="/admin/users" element={<Protected adminOnly><UsersPage /></Protected>} />
      <Route path="/admin" element={<Protected adminOnly><AdminPage /></Protected>} />

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
