import { getAuthToken, setHostUser, type HostUser } from '../bridge/hostAuth'

export type AuthResponse = {
  token: string
  userId: string
  userName: string
  expiresAt: number
}

export type AuthMeResponse = {
  userId: string
  userName: string
  isSharedToken: boolean
}

const gatewayUrl = () => {
  const env = import.meta.env.VITE_GATEWAY_URL
  const base = env === undefined || env === '' ? '' : env
  return base.replace(/\/$/, '')
}

function authHeaders(): HeadersInit {
  const t = getAuthToken()
  return {
    'Content-Type': 'application/json',
    ...(t ? { Authorization: `Bearer ${t}` } : {}),
  }
}

export async function login(username: string, password: string): Promise<AuthResponse> {
  const res = await fetch(`${gatewayUrl()}/v1/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password }),
  })
  if (!res.ok) {
    const text = await res.text()
    let message = `登录失败 (${res.status})`
    if (text) {
      try {
        const body = JSON.parse(text) as { message?: string }
        if (body.message) message = body.message
      } catch {
        message = text.slice(0, 200)
      }
    }
    throw new Error(message)
  }
  const data = (await res.json()) as AuthResponse
  applyAuthResponse(data)
  return data
}

export async function fetchMe(): Promise<AuthMeResponse | null> {
  const res = await fetch(`${gatewayUrl()}/v1/auth/me`, { headers: authHeaders() })
  if (res.status === 401) return null
  if (!res.ok) throw new Error(`获取用户信息失败 (${res.status})`)
  const data = (await res.json()) as AuthMeResponse
  if (!data.isSharedToken) {
    setHostUser({ userId: data.userId, userName: data.userName })
  }
  return data
}

export function applyAuthResponse(data: AuthResponse) {
  setHostUser({ userId: data.userId, userName: data.userName }, data.token)
}

export function toDisplayUser(user: HostUser | null): string {
  if (!user) return '您'
  return user.userName || user.userId
}
