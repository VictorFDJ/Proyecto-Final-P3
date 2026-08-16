import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import App from './App'
import { AuthProvider } from './auth/AuthContext'
import { ToastProvider } from './components/UI'
import './styles.css'

createRoot(document.getElementById('root')!).render(
  <StrictMode><AuthProvider><ToastProvider><App/></ToastProvider></AuthProvider></StrictMode>,
)
