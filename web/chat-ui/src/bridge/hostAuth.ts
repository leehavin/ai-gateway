/**
 * CefSharp / WebView2 宿主鉴权桥接。
 *
 * 优先级：宿主运行时注入 > window.__DATACHAT_TOKEN__ > VITE_DATACHAT_TOKEN > demo-token
 *
 * 宿主 C# 示例（加载页面前）：
 *   await browser.ExecuteScriptAsync("window.__DATACHAT_TOKEN__='your-token';");
 *
 * 或加载后 postMessage：
 *   chrome.webview.postMessage({ type: 'datachat:setToken', token: '...' });
 */

declare global {
  interface Window {
    __DATACHAT_TOKEN__?: string
    DataChatHost?: {
      getToken: () => string | undefined
      setToken: (token: string) => void
    }
  }
}

let runtimeToken: string | null = null
const tokenListeners = new Set<() => void>()

function notifyTokenChange() {
  tokenListeners.forEach((fn) => fn())
}

function readInitialToken(): string | null {
  const env = import.meta.env.VITE_DATACHAT_TOKEN
  if (typeof env === 'string' && env.trim()) return env.trim()
  if (typeof window !== 'undefined' && window.__DATACHAT_TOKEN__?.trim()) {
    return window.__DATACHAT_TOKEN__.trim()
  }
  return null
}

export function getAuthToken(): string {
  return runtimeToken ?? readInitialToken() ?? 'demo-token'
}

export function setAuthToken(token: string | null | undefined) {
  runtimeToken = token?.trim() ? token.trim() : null
  notifyTokenChange()
}

export function onAuthTokenChange(listener: () => void): () => void {
  tokenListeners.add(listener)
  return () => tokenListeners.delete(listener)
}

function handleHostMessage(data: unknown) {
  if (!data || typeof data !== 'object') return
  const msg = data as Record<string, unknown>

  if (msg.type === 'datachat:setToken' && typeof msg.token === 'string') {
    setAuthToken(msg.token)
    postToParent({ type: 'datachat:tokenAck', ok: true })
    return
  }

  if (msg.type === 'datachat:getToken') {
    postToParent({ type: 'datachat:token', token: getAuthToken() })
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

  window.DataChatHost = {
    getToken: () => getAuthToken(),
    setToken: (t) => setAuthToken(t),
  }

  window.addEventListener('message', (ev) => handleHostMessage(ev.data))

  // WebView2 chrome.webview
  const webview = (window as unknown as { chrome?: { webview?: { addEventListener?: (t: string, h: (e: MessageEvent) => void) => void } } })
    .chrome?.webview
  webview?.addEventListener?.('message', (ev: MessageEvent) => handleHostMessage(ev.data))

  postToParent({ type: 'datachat:ready' })
}
