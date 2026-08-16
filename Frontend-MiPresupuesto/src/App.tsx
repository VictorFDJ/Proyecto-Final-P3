import { lazy, Suspense } from 'react'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { useAuth } from './auth/AuthContext'
import { AppLayout } from './components/AppLayout'
import { Spinner } from './components/UI'

const AuthPage = lazy(() => import('./pages/AuthPage').then(module => ({ default: module.AuthPage })))
const DashboardPage = lazy(() => import('./pages/DashboardPage').then(module => ({ default: module.DashboardPage })))
const ExpensesPage = lazy(() => import('./pages/ExpensesPage').then(module => ({ default: module.ExpensesPage })))
const CatalogPage = lazy(() => import('./pages/CatalogPage').then(module => ({ default: module.CatalogPage })))
const BudgetsPage = lazy(() => import('./pages/BudgetsPage').then(module => ({ default: module.BudgetsPage })))
const ProfilePage = lazy(() => import('./pages/ProfilePage').then(module => ({ default: module.ProfilePage })))

function Protected() {
  const { token } = useAuth()
  return token ? <AppLayout/> : <Navigate to="/login" replace/>
}

export default function App() {
  return <BrowserRouter><Suspense fallback={<Spinner/>}><Routes>
    <Route path="/login" element={<AuthPage mode="login"/>}/>
    <Route path="/registro" element={<AuthPage mode="register"/>}/>
    <Route element={<Protected/>}>
      <Route index element={<DashboardPage/>}/>
      <Route path="gastos" element={<ExpensesPage/>}/>
      <Route path="presupuestos" element={<BudgetsPage/>}/>
      <Route path="categorias" element={<CatalogPage kind="categories"/>}/>
      <Route path="metodos-pago" element={<CatalogPage kind="payment-methods"/>}/>
      <Route path="perfil" element={<ProfilePage/>}/>
    </Route>
    <Route path="*" element={<Navigate to="/" replace/>}/>
  </Routes></Suspense></BrowserRouter>
}
