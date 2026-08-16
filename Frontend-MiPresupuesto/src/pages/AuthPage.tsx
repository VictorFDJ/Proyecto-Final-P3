import { useState, type FormEvent } from 'react'
import { Link, Navigate, useNavigate } from 'react-router-dom'
import { ArrowRight, BarChart3, Check, Eye, EyeOff, LockKeyhole, Mail, Sparkles, UserRound } from 'lucide-react'
import { useAuth } from '../auth/AuthContext'
import { ApiError } from '../lib/api'
import { FieldError } from '../components/UI'

export function AuthPage({ mode }: { mode: 'login' | 'register' }) {
  const auth = useAuth(); const navigate = useNavigate()
  const [form, setForm] = useState({ name: '', email: '', password: '' })
  const [visible, setVisible] = useState(false); const [busy, setBusy] = useState(false)
  const [message, setMessage] = useState(''); const [errors, setErrors] = useState<Record<string, string[]>>({})
  if (auth.token) return <Navigate to="/" replace/>
  const submit = async (event: FormEvent) => {
    event.preventDefault(); setBusy(true); setMessage(''); setErrors({})
    try {
      if (mode === 'login') await auth.login(form.email, form.password)
      else await auth.register(form.name, form.email, form.password)
      navigate('/')
    } catch (error) {
      if (error instanceof ApiError) { setMessage(error.message); setErrors(error.fields) }
      else setMessage('No se pudo conectar con el servidor.')
    } finally { setBusy(false) }
  }
  return <main className="auth-page">
    <section className="auth-showcase">
      <div className="showcase-content">
        <div className="brand auth-brand"><span className="brand-mark"><Sparkles size={22}/></span><span>Mi Presupuesto</span></div>
        <h1>Tu dinero, más claro.<br/><em>Tu futuro, más tranquilo.</em></h1>
        <p>Registra tus gastos, controla presupuestos y entiende tus hábitos desde un solo lugar.</p>
        <div className="showcase-points"><span><Check/> Reportes claros</span><span><Check/> Alertas de presupuesto</span><span><Check/> Datos privados y seguros</span></div>
        <div className="floating-card"><BarChart3/><div><small>Control mensual</small><strong>Decisiones con datos reales</strong></div></div>
      </div>
    </section>
    <section className="auth-form-side">
      <form className="auth-form" onSubmit={submit}>
        <span className="eyebrow">{mode === 'login' ? 'BIENVENIDO DE NUEVO' : 'CREA TU CUENTA'}</span>
        <h2>{mode === 'login' ? 'Inicia sesión' : 'Comienza a organizarte'}</h2>
        <p>{mode === 'login' ? 'Ingresa tus datos para ver tu resumen.' : 'Solo toma un minuto. Sin complicaciones.'}</p>
        {message && <div className="form-alert">{message}</div>}
        {mode === 'register' && <label><span>Nombre completo</span><div className="input-with-icon"><UserRound/><input value={form.name} onChange={e => setForm({...form, name:e.target.value})} placeholder="Ana Pérez" autoComplete="name"/></div><FieldError errors={errors} name="name"/></label>}
        <label><span>Correo electrónico</span><div className="input-with-icon"><Mail/><input type="email" value={form.email} onChange={e => setForm({...form, email:e.target.value})} placeholder="tu@correo.com" autoComplete="email"/></div><FieldError errors={errors} name="email"/></label>
        <label><span>Contraseña</span><div className="input-with-icon"><LockKeyhole/><input type={visible?'text':'password'} value={form.password} onChange={e => setForm({...form, password:e.target.value})} placeholder="Mínimo 8 caracteres" autoComplete={mode==='login'?'current-password':'new-password'}/><button type="button" onClick={() => setVisible(!visible)}>{visible?<EyeOff/>:<Eye/>}</button></div><FieldError errors={errors} name="password"/></label>
        <button className="btn primary auth-submit" disabled={busy}>{busy ? 'Procesando...' : mode === 'login' ? 'Entrar' : 'Crear mi cuenta'}<ArrowRight size={18}/></button>
        <div className="auth-switch">{mode === 'login' ? <>¿Aún no tienes cuenta? <Link to="/registro">Regístrate gratis</Link></> : <>¿Ya tienes cuenta? <Link to="/login">Inicia sesión</Link></>}</div>
      </form>
    </section>
  </main>
}
