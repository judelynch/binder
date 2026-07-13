import { Navigate, Route, Routes } from 'react-router-dom'
import { AppShell } from './components/AppShell'
import { ProtectedRoute } from './components/ProtectedRoute'
import { RequireAdmin } from './components/RequireAdmin'
import { AdminPage } from './pages/AdminPage'
import { CardsManagementPage } from './pages/admin/CardsManagementPage'
import { SyncPage } from './pages/admin/SyncPage'
import { VariantsPage } from './pages/admin/VariantsPage'
import { BinderDetailPage } from './pages/BinderDetailPage'
import { BindersPage } from './pages/BindersPage'
import { CardsPage } from './pages/CardsPage'
import { HomePage } from './pages/HomePage'
import { LoginPage } from './pages/LoginPage'
import { RegisterPage } from './pages/RegisterPage'

function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />

      <Route element={<ProtectedRoute />}>
        <Route element={<AppShell />}>
          <Route path="/" element={<HomePage />} />
          <Route path="/binders" element={<BindersPage />} />
          <Route path="/binders/:id" element={<BinderDetailPage />} />
          <Route path="/cards" element={<CardsPage />} />
          <Route element={<RequireAdmin />}>
            <Route path="/admin" element={<AdminPage />}>
              <Route index element={<Navigate to="sync" replace />} />
              <Route path="sync" element={<SyncPage />} />
              <Route path="cards" element={<CardsManagementPage />} />
              <Route path="variants" element={<VariantsPage />} />
            </Route>
          </Route>
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default App
