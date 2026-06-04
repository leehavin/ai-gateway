import type { ChatSession } from '../types'

export function newSessionId(): string {
  return crypto.randomUUID?.() ?? 'xxxxxxxxxxxx4xxxyxxxxxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0
    return (c === 'x' ? r : (r & 0x3) | 0x8).toString(16)
  })
}

export function sessionTitleFromMessages(session: ChatSession): string {
  const firstUser = session.messages.find((m) => m.role === 'user' && m.content.trim())
  if (!firstUser) return '新对话'
  const t = firstUser.content.trim()
  return t.length > 36 ? `${t.slice(0, 36)}…` : t
}

export function formatHistoryTime(ts: number): string {
  const d = new Date(ts)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${pad(d.getHours())}:${pad(d.getMinutes())}`
}

export function formatHistoryDateGroup(ts: number): string {
  const now = new Date()
  const d = new Date(ts)
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate())
  const that = new Date(d.getFullYear(), d.getMonth(), d.getDate())
  const diff = (today.getTime() - that.getTime()) / 86400000
  if (diff === 0) return '今天'
  if (diff === 1) return '昨天'
  if (diff < 7) return '近 7 天'
  return `${d.getMonth() + 1}月${d.getDate()}日`
}
