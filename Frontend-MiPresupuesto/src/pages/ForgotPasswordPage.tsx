import { useState, type FormEvent } from 'react'
import { ArrowLeft, ArrowRight, CheckCircle2, KeyRound, Mail, ShieldCheck, Sparkles } from 'lucide-react'
import { Link, Navigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { FieldError } from '../components/UI'
import { api, ApiError } from '../lib/api'

interface ForgotResponse { message: string; developmentToken?: string }

export function ForgotPasswordPage() {
  const { token: sessionToken } = useAuth()
  const [email, setEmail] = useState('')
  const [token, setToken] = useState('')
  const [password, setPassword] = useState('')
  const [confirmation, setConfirmation] = useState('')
  const [requested, setRequested] = useState(false)
  const [complete, setComplete] = useState(false)
  const [busy, setBusy] = useState(false)
  const [message, setMessage] = useState('')
  const [errors, setErrors] = useState<Record<string, string[]>>({})

  if (sessionToken) return <Navigate to="/" replace/>

  async function requestCode(event: FormEvent) {
    event.preventDefault(); setBusy(true); setMessage(''); setErrors({})
    try {
      const response = await api<ForgotResponse>('/auth/forgot-password', {
        method: 'POST', body: JSON.stringify({ email }),
      })
      setRequested(true); setMessage(response.message)
      if (response.developmentToken) setToken(response.developmentToken)
    } catch (error) {
      if (error instanceof ApiError) { setMessage(error.message); setErrors(error.fields) }
      else setMessage('No se pudo conectar con el servidor.')
    } finally { setBusy(false) }
  }

  async function resetPassword(event: FormEvent) {
    event.preventDefault(); setBusy(true); setMessage(''); setErrors({})
    if (password !== confirmation) {
      setErrors({ confirmation: ['Las contraseñas no coinciden.'] }); setBusy(false); return
    }
    try {
      await api<void>('/auth/reset-password', {
        method: 'POST', body: JSON.stringify({ email, token, newPassword: password }),
      })
      setComplete(true)
    } catch (error) {
      if (error instanceof ApiError) { setMessage(error.message); setErrors(error.fields) }
      else setMessage('No se pudo conectar con el servidor.')
    } finally { setBusy(false) }
  }

  return <main className="auth-page">
    <section className="auth-showcase"><div className="showcase-content">
      <div className="brand auth-brand"><span className="brand-mark"><Sparkles size={22}/></span><span>Mi Presupuesto</span></div>
      <h1>Recupera tu acceso.<br/><em>Sin perder tus datos.</em></h1>
      <p>Usaremos un código temporal y de un solo uso para que puedas elegir una contraseña nueva.</p>
      <div className="floating-card"><ShieldCheck/><div><small>Recuperación segura</small><strong>El código expira en 15 minutos</strong></div></div>
    </div></section>
    <section className="auth-form-side">
      {complete ? <div className="auth-form success-panel">
        <span><CheckCircle2/></span><h2>Contraseña actualizada</h2><p>Ya puedes entrar con tu nueva contraseña.</p>
        <Link className="btn primary" to="/login">Ir al inicio de sesión <ArrowRight size={18}/></Link>
      </div> : !requested ? <form className="auth-form" onSubmit={requestCode}>
        <span className="eyebrow">RECUPERAR ACCESO</span><h2>¿Olvidaste tu contraseña?</h2>
        <p>Escribe el correo de tu cuenta para generar un código temporal.</p>
        {message && <div className="form-alert">{message}</div>}
        <label><span>Correo electrónico</span><div className="input-with-icon"><Mail/><input autoFocus type="email" value={email} onChange={event=>setEmail(event.target.value)} placeholder="tu@correo.com" autoComplete="email"/></div><FieldError errors={errors} name="email"/></label>
        <button className="btn primary auth-submit" disabled={busy}>{busy?'Generando...':'Generar código'}<ArrowRight size={18}/></button>
        <div className="auth-switch"><Link to="/login"><ArrowLeft size={14}/> Volver al inicio de sesión</Link></div>
      </form> : <form className="auth-form" onSubmit={resetPassword}>
        <span className="eyebrow">NUEVA CONTRASEÑA</span><h2>Confirma el cambio</h2><p>{message}</p>
        {token ? <div className="reset-code"><ShieldCheck/><div><strong>Código de desarrollo generado</strong><small>Ya lo colocamos automáticamente. Expira en 15 minutos.</small></div></div> : <div className="form-alert">Introduce el código recibido por correo.</div>}
        <label><span>Código de recuperación</span><div className="input-with-icon"><KeyRound/><input value={token} onChange={event=>setToken(event.target.value)} autoComplete="one-time-code"/></div><FieldError errors={errors} name="token"/></label>
        <label><span>Nueva contraseña</span><div className="input-with-icon"><KeyRound/><input type="password" value={password} onChange={event=>setPassword(event.target.value)} autoComplete="new-password" placeholder="Mínimo 8 caracteres"/></div><FieldError errors={errors} name="newPassword"/></label>
        <label><span>Confirmar contraseña</span><div className="input-with-icon"><KeyRound/><input type="password" value={confirmation} onChange={event=>setConfirmation(event.target.value)} autoComplete="new-password"/></div><FieldError errors={errors} name="confirmation"/></label>
        {message && Object.keys(errors).length > 0 && <div className="form-alert">{message}</div>}
        <button className="btn primary auth-submit" disabled={busy}>{busy?'Actualizando...':'Guardar nueva contraseña'}<ArrowRight size={18}/></button>
        <div className="auth-switch"><button type="button" className="link-button" onClick={()=>{setRequested(false);setMessage('');setErrors({})}}>Solicitar otro código</button></div>
      </form>}
    </section>
  </main>
}
