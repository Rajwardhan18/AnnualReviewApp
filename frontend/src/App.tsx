import { Navigate, Route, Routes } from 'react-router-dom'
import { useAuth } from './auth/AuthContext'
import Layout from './components/Layout'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
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
  if (adminOnly && user.userType !== 'Admin') return <Navigate to="/" replace />
  return <Layout>{children}</Layout>
}

export default function App() {
  const { user, loading } = useAuth()

  return (
    <Routes>
      <Route path="/login" element={user && !loading ? <Navigate to="/" replace /> : <LoginPage />} />
      <Route path="/register" element={user && !loading ? <Navigate to="/" replace /> : <RegisterPage />} />

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
