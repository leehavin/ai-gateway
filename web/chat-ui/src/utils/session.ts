import type { ChatSession, SessionMeta } from '../types'

const STORAGE_PREFIX = 'datachat:'

export function newSessionId(): string {
  return 'xxxxxxxxxxxx4xxxyxxxxxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0
    return (c === 'x' ? r : (r & 0x3) | 0x8).toString(16)
  })
}

function historyKey(domain: string) {
  return `${STORAGE_PREFIX}history:${domain}`
}

function sessionKey(domain: string, id: string) {
  return `${STORAGE_PREFIX}session:${domain}:${id}`
}

function activeKey(domain: string) {
  return `${STORAGE_PREFIX}active:${domain}`
}

function legacySessionKey(domain: string) {
  return `${STORAGE_PREFIX}session:${domain}`
}

function readJson<T>(key: string): T | null {
  try {
    const raw = localStorage.getItem(key)
    if (!raw) return null
    return JSON.parse(raw) as T
  } catch {
    return null
  }
}

function writeJson(key: string, value: unknown) {
  localStorage.setItem(key, JSON.stringify(value))
}

export function loadHistoryIndex(domain: string): SessionMeta[] {
  migrateLegacySession(domain)
  return readJson<SessionMeta[]>(historyKey(domain)) ?? []
}

export function saveHistoryIndex(domain: string, list: SessionMeta[]) {
  writeJson(historyKey(domain), list)
}

export function getActiveSessionId(domain: string): string | null {
  return localStorage.getItem(activeKey(domain))
}

export function setActiveSessionId(domain: string, id: string) {
  localStorage.setItem(activeKey(domain), id)
}

export function loadSession(domain: string, id: string): ChatSession {
  const parsed = readJson<ChatSession>(sessionKey(domain, id))
  if (parsed?.id && Array.isArray(parsed.messages)) return parsed
  return { id, messages: [] }
}

export function saveSession(domain: string, session: ChatSession) {
  writeJson(sessionKey(domain, session.id), session)
}

export function sessionTitleFromMessages(session: ChatSession): string {
  const firstUser = session.messages.find((m) => m.role === 'user' && m.content.trim())
  if (!firstUser) return '新对话'
  const t = firstUser.content.trim()
  return t.length > 36 ? `${t.slice(0, 36)}…` : t
}

export function upsertSessionMeta(domain: string, session: ChatSession) {
  const list = loadHistoryIndex(domain)
  const title = sessionTitleFromMessages(session)
  const preview =
    session.messages.filter((m) => m.role === 'assistant' && m.content).pop()?.content?.slice(0, 80) ??
    session.messages.find((m) => m.role === 'user')?.content?.slice(0, 80)
  const updatedAt = Date.now()
  const idx = list.findIndex((x) => x.id === session.id)
  const meta: SessionMeta = { id: session.id, title, updatedAt, preview }
  if (idx >= 0) list[idx] = meta
  else list.unshift(meta)
  list.sort((a, b) => b.updatedAt - a.updatedAt)
  saveHistoryIndex(domain, list)
  setActiveSessionId(domain, session.id)
}

export function createNewSession(domain: string): ChatSession {
  const session: ChatSession = { id: newSessionId(), messages: [] }
  saveSession(domain, session)
  const list = loadHistoryIndex(domain)
  list.unshift({
    id: session.id,
    title: '新对话',
    updatedAt: Date.now(),
  })
  saveHistoryIndex(domain, list)
  setActiveSessionId(domain, session.id)
  return session
}

export function deleteSessionRecord(domain: string, id: string) {
  localStorage.removeItem(sessionKey(domain, id))
  const list = loadHistoryIndex(domain).filter((x) => x.id !== id)
  saveHistoryIndex(domain, list)
  if (getActiveSessionId(domain) === id) {
    localStorage.removeItem(activeKey(domain))
  }
}

export function resolveActiveSession(domain: string): ChatSession {
  migrateLegacySession(domain)
  const list = loadHistoryIndex(domain)
  const activeId = getActiveSessionId(domain)
  if (activeId) {
    const session = loadSession(domain, activeId)
    if (session.messages.length > 0 || list.some((x) => x.id === activeId)) {
      return session
    }
  }
  if (list.length > 0) {
    const id = list[0].id
    setActiveSessionId(domain, id)
    return loadSession(domain, id)
  }
  return createNewSession(domain)
}

function migrateLegacySession(domain: string) {
  const legacy = readJson<ChatSession>(legacySessionKey(domain))
  if (!legacy?.id || !Array.isArray(legacy.messages)) return
  const existing = loadHistoryIndex(domain)
  if (existing.some((x) => x.id === legacy.id)) {
    localStorage.removeItem(legacySessionKey(domain))
    return
  }
  saveSession(domain, legacy)
  const list = [
    {
      id: legacy.id,
      title: sessionTitleFromMessages(legacy),
      updatedAt: Date.now(),
      preview: legacy.messages[legacy.messages.length - 1]?.content?.slice(0, 80),
    },
    ...existing,
  ]
  saveHistoryIndex(domain, list)
  setActiveSessionId(domain, legacy.id)
  localStorage.removeItem(legacySessionKey(domain))
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
