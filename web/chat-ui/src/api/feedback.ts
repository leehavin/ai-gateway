import { getAuthToken } from '../bridge/hostAuth'

const gatewayUrl = () => {
  const env = import.meta.env.VITE_GATEWAY_URL
  const base = env === undefined || env === '' ? '' : env
  return base.replace(/\/$/, '')
}

export async function submitMessageFeedback(payload: {
  sessionId: string
  messageId: string
  domain: string
  rating: 'up' | 'down'
  comment?: string
}): Promise<void> {
  const t = getAuthToken()
  const res = await fetch(`${gatewayUrl()}/v1/feedback`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      ...(t ? { Authorization: `Bearer ${t}` } : {}),
    },
    body: JSON.stringify(payload),
  })
  if (!res.ok) {
    const text = await res.text()
    throw new Error(`反馈提交失败 ${res.status}${text ? `: ${text.slice(0, 80)}` : ''}`)
  }
}
