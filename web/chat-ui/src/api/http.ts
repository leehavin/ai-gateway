import { clearAuth, getAuthToken, isUserSessionToken } from '../bridge/hostAuth'

export class ApiUnauthorizedError extends Error {
  constructor(message = '未登录或会话已过期') {
    super(message)
    this.name = 'ApiUnauthorizedError'
  }
}

export function gatewayUrl(): string {
  const env = import.meta.env.VITE_GATEWAY_URL
  const base = env === undefined || env === '' ? '' : env
  return base.replace(/\/$/, '')
}

export function authHeaders(): HeadersInit {
  const t = getAuthToken()
  return {
    'Content-Type': 'application/json',
    ...(t ? { Authorization: `Bearer ${t}` } : {}),
  }
}

export async function assertOk(res: Response, fallback: string): Promise<void> {
  if (res.status === 401) {
    if (isUserSessionToken()) clearAuth()
    throw new ApiUnauthorizedError()
  }
  if (!res.ok) {
    const text = await res.text()
    throw new Error(`${fallback} (${res.status})${text ? `: ${text.slice(0, 160)}` : ''}`)
  }
}
