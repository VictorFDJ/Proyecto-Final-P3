import { createContext, useContext, useState, type ReactNode } from 'react'
import { AlertCircle, CheckCircle2, X } from 'lucide-react'

export function Modal({ open, title, children, onClose }: { open: boolean; title: string; children: ReactNode; onClose(): void }) {
  if (!open) return null
  return <div className="modal-backdrop" onMouseDown={event => event.target === event.currentTarget && onClose()}>
    <section className="modal" role="dialog" aria-modal="true">
      <header><h2>{title}</h2><button className="icon-btn" onClick={onClose} aria-label="Cerrar"><X size={20}/></button></header>
      {children}
    </section>
  </div>
}

export function EmptyState({ icon, title, text }: { icon: ReactNode; title: string; text: string }) {
  return <div className="empty-state"><span>{icon}</span><h3>{title}</h3><p>{text}</p></div>
}

export function Spinner() { return <div className="spinner-wrap"><div className="spinner"/><span>Cargando...</span></div> }

interface Toast { id: number; message: string; type: 'success' | 'error' }
const ToastContext = createContext<{ show(message: string, type?: Toast['type']): void } | null>(null)
export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([])
  const show = (message: string, type: Toast['type'] = 'success') => {
    const id = Date.now(); setToasts(items => [...items, { id, message, type }])
    window.setTimeout(() => setToasts(items => items.filter(item => item.id !== id)), 3500)
  }
  return <ToastContext.Provider value={{ show }}>{children}<div className="toast-stack">{toasts.map(toast =>
    <div key={toast.id} className={`toast ${toast.type}`}>
      {toast.type === 'success' ? <CheckCircle2 size={19}/> : <AlertCircle size={19}/>}<span>{toast.message}</span>
    </div>)}</div></ToastContext.Provider>
}
export function useToast() {
  const value = useContext(ToastContext)
  if (!value) throw new Error('useToast debe usarse dentro de ToastProvider')
  return value
}

export function FieldError({ errors, name }: { errors: Record<string, string[]>; name: string }) {
  const match = Object.entries(errors).find(([key]) => key.toLowerCase() === name.toLowerCase())
  return match ? <small className="field-error">{match[1][0]}</small> : null
}
