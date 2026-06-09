import { authHeaders, assertOk, gatewayUrl } from './http'
import { readSseStream, type SseEvent } from './sse'
import type { ChatAttachmentRef } from '../types/attachments'
import type { ChatGenerationParameters } from '../types/chatParams'
import type { ChatStreamMessageDto, CozeBotSummary, DomainItem, GatewayHealth } from '../types'

export async function fetchHealth(): Promise<GatewayHealth> {
  const res = await fetch(`${gatewayUrl()}/v1/health`)
  if (!res.ok) throw new Error(`服务不可用 (${res.status})`)
  return res.json() as Promise<GatewayHealth>
}

export async function fetchDomains(): Promise<DomainItem[]> {
  const res = await fetch(`${gatewayUrl()}/v1/domains`, { headers: authHeaders() })
  await assertOk(res, '获取智能体列表失败')
  return res.json() as Promise<DomainItem[]>
}

export async function fetchCozeBots(): Promise<CozeBotSummary[]> {
  const res = await fetch(`${gatewayUrl()}/v1/coze/bots`, { headers: authHeaders() })
  if (!res.ok) return []
  return res.json() as Promise<CozeBotSummary[]>
}

export function buildHistory(
  messages: { role: string; content: string }[],
  userMessage: string,
  normalizeUser?: (content: string) => string
): ChatStreamMessageDto[] {
  const norm = normalizeUser ?? ((c: string) => c)
  const list: ChatStreamMessageDto[] = []
  const recent = messages
    .filter((m) => m.role === 'user' || m.role === 'assistant')
    .slice(-40)
  recent.forEach((m) =>
    list.push({
      role: m.role,
      content: m.role === 'user' ? norm(m.content) : m.content,
    })
  )
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
    parameters?: ChatGenerationParameters
  },
  onEvent: (event: SseEvent) => void,
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
        parameters: params.parameters
          ? {
              temperature: params.parameters.temperature,
              topP: params.parameters.topP,
              maxTokens: params.parameters.maxTokens,
            }
          : undefined,
      }),
    signal,
  })
  if (!res.ok) {
    const text = await res.text()
    throw new Error(`网关错误 ${res.status}${text ? `: ${text.slice(0, 200)}` : ''}`)
  }
  await readSseStream(res.body, onEvent, signal)
}
