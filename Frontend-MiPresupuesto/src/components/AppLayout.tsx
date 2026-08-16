import { useState } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router-dom'
import { BarChart3, Tags, WalletCards, ReceiptText, PiggyBank, UserRound, LogOut, Menu, X, Sparkles } from 'lucide-react'
import { useAuth } from '../auth/AuthContext'

const links = [
  { to: '/', label: 'Resumen', icon: BarChart3 },
  { to: '/gastos', label: 'Gastos', icon: ReceiptText },
  { to: '/presupuestos', label: 'Presupuestos', icon: PiggyBank },
  { to: '/categorias', label: 'Categorías', icon: Tags },
  { to: '/metodos-pago', label: 'Métodos de pago', icon: WalletCards },
  { to: '/perfil', label: 'Mi perfil', icon: UserRound },
]

export function AppLayout() {
  const { user, logout } = useAuth()
  const [open, setOpen] = useState(false)
  const location = useLocation()
  const current = links.find(link => link.to === location.pathname)?.label ?? 'Mi Presupuesto'
  return <div className="app-shell">
    {open && <div className="sidebar-overlay" onClick={() => setOpen(false)}/>}
    <aside className={`sidebar ${open ? 'open' : ''}`}>
      <div className="brand"><span className="brand-mark"><Sparkles size={22}/></span><span>Mi Presupuesto</span><button className="sidebar-close" onClick={() => setOpen(false)}><X/></button></div>
      <nav>{links.map(({ to, label, icon: Icon }) =>
        <NavLink key={to} to={to} end={to === '/'} onClick={() => setOpen(false)} className={({ isActive }) => isActive ? 'active' : ''}>
          <Icon size={20}/><span>{label}</span>
        </NavLink>)}</nav>
      <div className="sidebar-user">
        <span className="avatar">{user?.name?.charAt(0).toUpperCase()}</span>
        <div><strong>{user?.name}</strong><small>{user?.email}</small></div>
        <button className="icon-btn" onClick={logout} title="Cerrar sesión"><LogOut size={18}/></button>
      </div>
    </aside>
    <main className="main-area">
      <header className="mobile-header"><button className="icon-btn" onClick={() => setOpen(true)}><Menu/></button><strong>{current}</strong><span className="avatar small">{user?.name?.charAt(0).toUpperCase()}</span></header>
      <div className="page-container"><Outlet/></div>
    </main>
  </div>
}
