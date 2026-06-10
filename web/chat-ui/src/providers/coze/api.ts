import { getAuthToken } from '../../bridge/hostAuth'
import { readSseStream, type SseEvent } from '../../api/sse'
import type { CozeWorkflowItem } from '../../types'

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

export async function fetchCozeWorkflows(domainId: string): Promise<CozeWorkflowItem[]> {
  const q = new URLSearchParams({ domain: domainId })
  const res = await fetch(`${gatewayUrl()}/v1/coze/workflows?${q}`, { headers: authHeaders() })
  if (!res.ok) {
    let detail = ''
    try {
      const j = (await res.json()) as { message?: string }
      detail = j.message ?? ''
    } catch {
      detail = await res.text()
    }
    throw new Error(
      `加载工作流失败 ${res.status}${detail ? `: ${detail.slice(0, 160)}` : ''}`
    )
  }
  return res.json() as Promise<CozeWorkflowItem[]>
}

export async function streamCozeWorkflow(
  params: {
    domain: string
    workflowId: string
    input?: string
    parameters?: Record<string, string>
    attachments?: { fileId: string; name?: string }[]
  },
  onEvent: (event: SseEvent) => void,
  signal?: AbortSignal
): Promise<void> {
  const res = await fetch(`${gatewayUrl()}/v1/coze/workflow/stream`, {
    method: 'POST',
    headers: authHeaders(),
    body: JSON.stringify({
      domain: params.domain,
      workflowId: params.workflowId,
      input: params.input,
      parameters: params.parameters,
      attachments: params.attachments,
    }),
    signal,
  })
  if (!res.ok) {
    const text = await res.text()
    throw new Error(`工作流执行失败 ${res.status}${text ? `: ${text.slice(0, 200)}` : ''}`)
  }
  await readSseStream(res.body, onEvent, signal)
}

export async function resumeCozeWorkflow(
  params: {
    domain: string
    workflowId: string
    eventId: string
    interruptType: number
    resumeData: string
  },
  onEvent: (event: SseEvent) => void,
  signal?: AbortSignal
): Promise<void> {
  const res = await fetch(`${gatewayUrl()}/v1/coze/workflow/resume`, {
    method: 'POST',
    headers: authHeaders(),
    body: JSON.stringify({
      domain: params.domain,
      workflowId: params.workflowId,
      eventId: params.eventId,
      interruptType: params.interruptType,
      resumeData: params.resumeData,
    }),
    signal,
  })
  if (!res.ok) {
    const text = await res.text()
    throw new Error(`工作流恢复失败 ${res.status}${text ? `: ${text.slice(0, 200)}` : ''}`)
  }
  await readSseStream(res.body, onEvent, signal)
}
