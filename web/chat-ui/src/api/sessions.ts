import { getAuthToken } from '../bridge/hostAuth'
import type { ChatMessage, ChatSession, SessionMeta } from '../types'

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

export interface ServerSessionSummary {
  id: string
  title: string
  domainId: string
  updatedAt: number
  preview?: string
}

export interface ServerSessionDetail {
  id: string
  title: string
  domainId: string
  updatedAt: number
  messages: Array<{
    id: string
    role: string
    content: string
    extrasJson?: string | null
    createdAt: number
  }>
}

function parseExtras(extrasJson?: string | null): {
  thinking?: string
  citations?: ChatMessage['citations']
} {
  if (!extrasJson) return {}
  try {
    const j = JSON.parse(extrasJson) as {
      thinking?: string
      citations?: ChatMessage['citations']
    }
    return { thinking: j.thinking, citations: j.citations }
  } catch {
    return {}
  }
}

export async function fetchServerSessions(domainId: string): Promise<SessionMeta[]> {
  const q = new URLSearchParams({ domain: domainId })
  const res = await fetch(`${gatewayUrl()}/v1/sessions?${q}`, { headers: authHeaders() })
  if (res.status === 404) return []
  if (!res.ok) throw new Error(`加载会话列表失败 ${res.status}`)
  const list = (await res.json()) as ServerSessionSummary[]
  return list.map((s) => ({
    id: s.id,
    title: s.title,
    updatedAt: s.updatedAt,
    preview: s.preview,
  }))
}

export async function fetchServerSession(
  domainId: string,
  sessionId: string
): Promise<ChatSession> {
  const res = await fetch(`${gatewayUrl()}/v1/sessions/${sessionId}`, { headers: authHeaders() })
  if (!res.ok) throw new Error(`加载会话失败 ${res.status}`)
  const detail = (await res.json()) as ServerSessionDetail
  const messages: ChatMessage[] = detail.messages.map((m) => {
    const extra = parseExtras(m.extrasJson)
    return {
      id: m.id,
      role: m.role as ChatMessage['role'],
      content: m.content,
      thinking: extra.thinking,
      citations: extra.citations,
    }
  })
  return { id: detail.id, messages }
}

export async function deleteServerSession(sessionId: string): Promise<void> {
  const res = await fetch(`${gatewayUrl()}/v1/sessions/${sessionId}`, {
    method: 'DELETE',
    headers: authHeaders(),
  })
  if (!res.ok && res.status !== 404) throw new Error(`删除会话失败 ${res.status}`)
}
