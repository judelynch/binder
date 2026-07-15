import { Navigate, Route, Routes } from 'react-router-dom'
import { AppShell } from './components/AppShell'
import { ProtectedRoute } from './components/ProtectedRoute'
import { RequireAdmin } from './components/RequireAdmin'
import { AdminPage } from './pages/AdminPage'
import { CardsManagementPage } from './pages/admin/CardsManagementPage'
import { SyncPage } from './pages/admin/SyncPage'
import { VariantsPage } from './pages/admin/VariantsPage'
import { BinderDetailPage } from './pages/BinderDetailPage'
import { BinderTablePage } from './pages/BinderTablePage'
import { BindersPage } from './pages/BindersPage'
import { CardDetailPage } from './pages/CardDetailPage'
import { CardsPage } from './pages/CardsPage'
import { HomePage } from './pages/HomePage'
import { LoginPage } from './pages/LoginPage'
import { RegisterPage } from './pages/RegisterPage'
import { SetDetailPage } from './pages/SetDetailPage'
import { SetsPage } from './pages/SetsPage'

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
          <Route path="/binders/:id/table" element={<BinderTablePage />} />
          <Route path="/sets" element={<SetsPage />} />
          <Route path="/sets/:setId" element={<SetDetailPage />} />
          <Route path="/cards" element={<CardsPage />} />
          <Route path="/cards/:cardId" element={<CardDetailPage />} />
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
