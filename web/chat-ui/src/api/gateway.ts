import { readSseStream } from './sse'
import type { ChatAttachmentRef } from '../types/attachments'
import type { ChatStreamMessageDto, DomainItem, GatewayHealth } from '../types'

/** 开发留空则走 Vite 代理（vite.config.ts → Gateway :5080） */
const gatewayUrl = () => {
  const env = import.meta.env.VITE_GATEWAY_URL
  const base = env === undefined || env === '' ? '' : env
  return base.replace(/\/$/, '')
}
const token = () => import.meta.env.VITE_DATACHAT_TOKEN || 'demo-token'

function authHeaders(): HeadersInit {
  const t = token()
  return {
    'Content-Type': 'application/json',
    ...(t ? { Authorization: `Bearer ${t}` } : {}),
  }
}

export async function fetchHealth(): Promise<GatewayHealth> {
  const res = await fetch(`${gatewayUrl()}/v1/health`)
  if (!res.ok) throw new Error(`健康检查失败: ${res.status}`)
  return res.json() as Promise<GatewayHealth>
}

export async function fetchDomains(): Promise<DomainItem[]> {
  const res = await fetch(`${gatewayUrl()}/v1/domains`, { headers: authHeaders() })
  if (!res.ok) {
    const text = await res.text()
    throw new Error(`获取领域列表失败 ${res.status}${text ? `: ${text.slice(0, 120)}` : ''}`)
  }
  return res.json() as Promise<DomainItem[]>
}

export function buildHistory(
  messages: { role: string; content: string }[],
  userMessage: string
): ChatStreamMessageDto[] {
  const list: ChatStreamMessageDto[] = []
  const recent = messages
    .filter((m) => m.role === 'user' || m.role === 'assistant')
    .slice(-40)
  recent.forEach((m) => list.push({ role: m.role, content: m.content }))
  const last = list[list.length - 1]
  if (!(last?.role === 'user' && last.content === userMessage)) {
    list.push({ role: 'user', content: userMessage })
  }
  return list
}

export async function streamChat(
  params: {
    sessionId: string
    domain: string
    message: string
    messages: ChatStreamMessageDto[]
    attachments?: ChatAttachmentRef[]
  },
  onDelta: (text: string) => void,
  signal?: AbortSignal
): Promise<void> {
  const res = await fetch(`${gatewayUrl()}/v1/chat/stream`, {
    method: 'POST',
    headers: authHeaders(),
      body: JSON.stringify({
        sessionId: params.sessionId,
        domain: params.domain,
        message: params.message,
        stream: true,
        messages: params.messages,
        attachments: params.attachments?.length ? params.attachments : undefined,
      }),
    signal,
  })
  if (!res.ok) {
    const text = await res.text()
    throw new Error(`网关错误 ${res.status}${text ? `: ${text.slice(0, 200)}` : ''}`)
  }
  await readSseStream(res.body, onDelta, signal)
}
