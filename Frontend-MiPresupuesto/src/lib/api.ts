const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5044/api'

export class ApiError extends Error {
  constructor(message: string, public status: number, public fields: Record<string, string[]> = {}) {
    super(message)
  }
}

function token() { return localStorage.getItem('mi-presupuesto-token') }

export async function api<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers)
  const currentToken = token()
  if (currentToken) headers.set('Authorization', `Bearer ${currentToken}`)
  if (options.body && !(options.body instanceof FormData)) headers.set('Content-Type', 'application/json')
  const response = await fetch(`${API_URL}${path}`, { ...options, headers })
  if (!response.ok) {
    let message = 'No se pudo completar la solicitud.'
    let fields: Record<string, string[]> = {}
    try {
      const payload = await response.json()
      message = payload.error?.message ?? payload.title ?? message
      fields = payload.error?.errors ?? payload.errors ?? {}
    } catch { /* respuesta sin JSON */ }
    if (response.status === 401) window.dispatchEvent(new Event('auth:unauthorized'))
    throw new ApiError(message, response.status, fields)
  }
  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}

export async function download(path: string): Promise<void> {
  const headers = new Headers()
  const currentToken = token()
  if (currentToken) headers.set('Authorization', `Bearer ${currentToken}`)
  const response = await fetch(`${API_URL}${path}`, { headers })
  if (!response.ok) throw new ApiError('No se pudo descargar el archivo.', response.status)
  const blob = await response.blob()
  const disposition = response.headers.get('content-disposition') ?? ''
  const encoded = disposition.match(/filename\*=UTF-8''([^;]+)/i)?.[1]
  const plain = disposition.match(/filename="?([^";]+)"?/i)?.[1]
  const fileName = encoded ? decodeURIComponent(encoded) : plain ?? 'descarga'
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = fileName
  anchor.click()
  URL.revokeObjectURL(url)
}
