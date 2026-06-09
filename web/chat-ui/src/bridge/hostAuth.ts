/**
 * CefSharp / WebView2 宿主鉴权桥接。
 *
 * 优先级：宿主运行时注入 > sessionStorage > window.__DATACHAT_*__ > VITE_* > demo-token
 *
 * 宿主 C# 示例（加载页面前）：
 *   await browser.ExecuteScriptAsync("window.__DATACHAT_TOKEN__='...';window.__DATACHAT_USER_ID__='42';window.__DATACHAT_USER_NAME__='张三';");
 *
 * 或加载后 postMessage：
 *   chrome.webview.postMessage({ type: 'datachat:setUser', userId: '42', userName: '张三', token: '...' });
 */

export type HostUser = {
  userId: string
  userName: string
}

const STORAGE_TOKEN = 'datachat:token'
const STORAGE_USER_ID = 'datachat:userId'
const STORAGE_USER_NAME = 'datachat:userName'

declare global {
  interface Window {
    __DATACHAT_TOKEN__?: string
    __DATACHAT_USER_ID__?: string
    __DATACHAT_USER_NAME__?: string
    DataChatHost?: {
      getToken: () => string | undefined
      setToken: (token: string) => void
      getUser: () => HostUser | undefined
      setUser: (user: HostUser, token?: string) => void
      isEmbedded: () => boolean
    }
  }
}

let runtimeToken: string | null = null
let runtimeUser: HostUser | null = null
let embeddedHost = false
const tokenListeners = new Set<() => void>()
const userListeners = new Set<() => void>()

function notifyTokenChange() {
  tokenListeners.forEach((fn) => fn())
}

function notifyUserChange() {
  userListeners.forEach((fn) => fn())
}

function readStorage(key: string): string | null {
  if (typeof sessionStorage === 'undefined') return null
  const v = sessionStorage.getItem(key)
  return v?.trim() ? v.trim() : null
}

function writeStorage(key: string, value: string | null) {
  if (typeof sessionStorage === 'undefined') return
  if (!value) sessionStorage.removeItem(key)
  else sessionStorage.setItem(key, value)
}

function readInitialToken(): string | null {
  const stored = readStorage(STORAGE_TOKEN)
  if (stored) return stored
  const env = import.meta.env.VITE_DATACHAT_TOKEN
  if (typeof env === 'string' && env.trim()) return env.trim()
  if (typeof window !== 'undefined' && window.__DATACHAT_TOKEN__?.trim()) {
    return window.__DATACHAT_TOKEN__.trim()
  }
  return null
}

function readInitialUser(): HostUser | null {
  const userId = readStorage(STORAGE_USER_ID) ?? window.__DATACHAT_USER_ID__?.trim()
  const userName = readStorage(STORAGE_USER_NAME) ?? window.__DATACHAT_USER_NAME__?.trim()
  if (!userId) return null
  return { userId, userName: userName || userId }
}

export function isEmbeddedHost(): boolean {
  return embeddedHost
}

function requireLoginEnv(): boolean {
  return import.meta.env.VITE_REQUIRE_LOGIN === 'true'
}

export function getAuthToken(): string {
  const stored = runtimeToken ?? readInitialToken()
  if (stored) return stored
  return requireLoginEnv() ? '' : 'demo-token'
}

export function getHostUser(): HostUser | null {
  return runtimeUser ?? readInitialUser()
}

export function isUserSessionToken(): boolean {
  const token = getAuthToken()
  return !!token && token !== 'demo-token' && token.includes('.')
}

export function setAuthToken(token: string | null | undefined) {
  runtimeToken = token?.trim() ? token.trim() : null
  writeStorage(STORAGE_TOKEN, runtimeToken)
  notifyTokenChange()
}

export function setHostUser(user: HostUser | null | undefined, token?: string | null) {
  if (!user?.userId?.trim()) {
    runtimeUser = null
    writeStorage(STORAGE_USER_ID, null)
    writeStorage(STORAGE_USER_NAME, null)
  } else {
    runtimeUser = {
      userId: user.userId.trim(),
      userName: (user.userName || user.userId).trim(),
    }
    writeStorage(STORAGE_USER_ID, runtimeUser.userId)
    writeStorage(STORAGE_USER_NAME, runtimeUser.userName)
  }
  if (token !== undefined) setAuthToken(token)
  notifyUserChange()
}

export function clearAuth() {
  setHostUser(null, null)
}

export function onAuthTokenChange(listener: () => void): () => void {
  tokenListeners.add(listener)
  return () => tokenListeners.delete(listener)
}

export function onHostUserChange(listener: () => void): () => void {
  userListeners.add(listener)
  return () => userListeners.delete(listener)
}

function handleHostMessage(data: unknown) {
  if (!data || typeof data !== 'object') return
  const msg = data as Record<string, unknown>
  embeddedHost = true

  if (msg.type === 'datachat:setUser') {
    const userId = typeof msg.userId === 'string' ? msg.userId : ''
    const userName = typeof msg.userName === 'string' ? msg.userName : userId
    const token = typeof msg.token === 'string' ? msg.token : undefined
    if (userId) setHostUser({ userId, userName }, token)
    else if (token) setAuthToken(token)
    postToParent({ type: 'datachat:userAck', ok: true })
    return
  }

  if (msg.type === 'datachat:setToken' && typeof msg.token === 'string') {
    setAuthToken(msg.token)
    postToParent({ type: 'datachat:tokenAck', ok: true })
    return
  }

  if (msg.type === 'datachat:getToken') {
    postToParent({ type: 'datachat:token', token: getAuthToken() })
    return
  }

  if (msg.type === 'datachat:getUser') {
    postToParent({ type: 'datachat:user', user: getHostUser(), token: getAuthToken() })
  }
}

function postToParent(payload: object) {
  try {
    window.parent?.postMessage(payload, '*')
  } catch {
    /* ignore */
  }
}

export function initHostAuthBridge() {
  if (typeof window === 'undefined') return

  runtimeToken = readInitialToken()
  runtimeUser = readInitialUser()

  if (window.__DATACHAT_USER_ID__?.trim() || window.__DATACHAT_TOKEN__?.trim()) {
    embeddedHost = true
  }

  window.DataChatHost = {
    getToken: () => getAuthToken(),
    setToken: (t) => setAuthToken(t),
    getUser: () => getHostUser() ?? undefined,
    setUser: (user, token) => setHostUser(user, token),
    isEmbedded: () => isEmbeddedHost(),
  }

  window.addEventListener('message', (ev) => handleHostMessage(ev.data))

  const webview = (window as unknown as { chrome?: { webview?: { addEventListener?: (t: string, h: (e: MessageEvent) => void) => void } } })
    .chrome?.webview
  webview?.addEventListener?.('message', (ev: MessageEvent) => handleHostMessage(ev.data))

  postToParent({ type: 'datachat:ready' })
}
