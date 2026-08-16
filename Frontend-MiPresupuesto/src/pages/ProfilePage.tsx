import { useEffect, useState, type FormEvent } from 'react'
import { AtSign, CheckCircle2, KeyRound, LockKeyhole, ShieldCheck, UserRound } from 'lucide-react'
import { useAuth } from '../auth/AuthContext'
import { FieldError, Spinner, useToast } from '../components/UI'
import { api, ApiError } from '../lib/api'
import type { User } from '../types'

export function ProfilePage() {
  const { user, updateUser } = useAuth()
  const { show } = useToast()
  const [loading, setLoading] = useState(true)
  const [name, setName] = useState(user?.name ?? '')
  const [nameBusy, setNameBusy] = useState(false)
  const [passwordBusy, setPasswordBusy] = useState(false)
  const [nameErrors, setNameErrors] = useState<Record<string, string[]>>({})
  const [passwordErrors, setPasswordErrors] = useState<Record<string, string[]>>({})
  const [passwords, setPasswords] = useState({ currentPassword: '', newPassword: '', confirmation: '' })

  useEffect(() => {
    api<User>('/profile')
      .then(profile => { updateUser(profile); setName(profile.name) })
      .catch(error => show(error instanceof ApiError ? error.message : 'No se pudo cargar el perfil.', 'error'))
      .finally(() => setLoading(false))
  }, [])

  async function saveName(event: FormEvent) {
    event.preventDefault(); setNameErrors({}); setNameBusy(true)
    try {
      const updated = await api<User>('/profile/name', { method: 'PUT', body: JSON.stringify({ name }) })
      updateUser(updated); show('Tu nombre se actualizó correctamente.')
    } catch (error) {
      if (error instanceof ApiError) { setNameErrors(error.fields); show(error.message, 'error') }
      else show('No se pudo actualizar el perfil.', 'error')
    } finally { setNameBusy(false) }
  }

  async function savePassword(event: FormEvent) {
    event.preventDefault(); setPasswordErrors({})
    if (passwords.newPassword !== passwords.confirmation) {
      setPasswordErrors({ confirmation: ['Las contraseñas nuevas no coinciden.'] }); return
    }
    setPasswordBusy(true)
    try {
      await api<void>('/profile/password', {
        method: 'PUT',
        body: JSON.stringify({ currentPassword: passwords.currentPassword, newPassword: passwords.newPassword }),
      })
      setPasswords({ currentPassword: '', newPassword: '', confirmation: '' })
      show('Contraseña actualizada correctamente.')
    } catch (error) {
      if (error instanceof ApiError) { setPasswordErrors(error.fields); show(error.message, 'error') }
      else show('No se pudo actualizar la contraseña.', 'error')
    } finally { setPasswordBusy(false) }
  }

  if (loading) return <Spinner/>

  return <div className="page-stack">
    <div className="page-heading"><div><span className="eyebrow">TU CUENTA</span><h1>Perfil y seguridad</h1><p>Actualiza tus datos personales y protege tu acceso.</p></div></div>

    <section className="profile-hero card">
      <span className="profile-avatar">{user?.name.charAt(0).toUpperCase()}</span>
      <div><h2>{user?.name}</h2><p><AtSign size={16}/>{user?.email}</p></div>
      <span className="verified"><CheckCircle2 size={17}/> Cuenta activa</span>
    </section>

    <div className="profile-grid">
      <section className="card settings-card">
        <header><span><UserRound/></span><div><h2>Información personal</h2><p>Así aparecerá tu nombre en la aplicación.</p></div></header>
        <form className="settings-form" onSubmit={saveName}>
          <label><span>Nombre completo</span><input value={name} onChange={event => setName(event.target.value)} autoComplete="name"/><FieldError errors={nameErrors} name="name"/></label>
          <label><span>Correo electrónico</span><input value={user?.email ?? ''} disabled/><small className="input-help">El correo de acceso no se puede modificar.</small></label>
          <button className="btn primary" disabled={nameBusy || name.trim() === user?.name}>{nameBusy ? 'Guardando...' : 'Guardar información'}</button>
        </form>
      </section>

      <section className="card settings-card">
        <header><span className="security-icon"><ShieldCheck/></span><div><h2>Cambiar contraseña</h2><p>Usa al menos 8 caracteres para mayor seguridad.</p></div></header>
        <form className="settings-form" onSubmit={savePassword}>
          <label><span>Contraseña actual</span><div className="input-with-icon"><LockKeyhole/><input type="password" value={passwords.currentPassword} onChange={event => setPasswords({...passwords,currentPassword:event.target.value})} autoComplete="current-password"/></div><FieldError errors={passwordErrors} name="currentPassword"/></label>
          <label><span>Nueva contraseña</span><div className="input-with-icon"><KeyRound/><input type="password" value={passwords.newPassword} onChange={event => setPasswords({...passwords,newPassword:event.target.value})} autoComplete="new-password"/></div><FieldError errors={passwordErrors} name="newPassword"/></label>
          <label><span>Confirmar nueva contraseña</span><div className="input-with-icon"><KeyRound/><input type="password" value={passwords.confirmation} onChange={event => setPasswords({...passwords,confirmation:event.target.value})} autoComplete="new-password"/></div><FieldError errors={passwordErrors} name="confirmation"/></label>
          <button className="btn primary" disabled={passwordBusy}>{passwordBusy ? 'Actualizando...' : 'Actualizar contraseña'}</button>
        </form>
      </section>
    </div>
  </div>
}
