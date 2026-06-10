/**
 * CefSharp / WebView2 宿主业务上下文桥接（案件文档 → 工作流）。
 *
 * 宿主 C# 示例（五书核稿）：
 *   await host.RunWorkflowAsync("patent-oa", "7649037302673063970", "请对本案五书进行核稿",
 *     new[] { (fileId, "环头.doc") });
 *
 * 或 postMessage：
 *   chrome.webview.postMessage({
 *     type: 'datachat:runWorkflow',
 *     domainId: 'patent-oa',
 *     workflowId: '7649037302673063970',
 *     input: '请对本案五书进行核稿',
 *     files: [{ fileId: '...', name: '环头.doc' }],
 *   });
 */

export type HostWorkflowFile = {
  fileId: string
  name: string
}

export type HostRunWorkflowPayload = {
  domainId?: string
  workflowId?: string
  workflowName?: string
  input?: string
  files?: HostWorkflowFile[]
  /** 默认 true：新开会话再执行 */
  newSession?: boolean
  /** true：仅预选工作流并预填附件，不自动发送 */
  prefillOnly?: boolean
}

type HostRunWorkflowHandler = (payload: HostRunWorkflowPayload) => void | Promise<void>

const runWorkflowHandlers = new Set<HostRunWorkflowHandler>()
const pendingRunWorkflow: HostRunWorkflowPayload[] = []

function postToParent(payload: object) {
  try {
    window.parent?.postMessage(payload, '*')
  } catch {
    /* ignore */
  }
}

function parseFiles(raw: unknown): HostWorkflowFile[] | undefined {
  if (!Array.isArray(raw)) return undefined
  const files: HostWorkflowFile[] = []
  for (const item of raw) {
    if (!item || typeof item !== 'object') continue
    const row = item as Record<string, unknown>
    const fileId = typeof row.fileId === 'string' ? row.fileId.trim() : ''
    const name = typeof row.name === 'string' ? row.name.trim() : ''
    if (fileId && name) files.push({ fileId, name })
  }
  return files.length ? files : undefined
}

function dispatchRunWorkflow(payload: HostRunWorkflowPayload) {
  if (runWorkflowHandlers.size === 0) {
    pendingRunWorkflow.push(payload)
    return
  }
  for (const handler of runWorkflowHandlers) {
    void Promise.resolve(handler(payload)).catch(() => {
      /* 宿主回调异常不影响其他订阅者 */
    })
  }
}

/** App 挂载处理器后冲刷排队消息（避免宿主在 chat-ui 就绪前 postMessage 丢失）。 */
export function flushPendingHostRunWorkflow() {
  if (runWorkflowHandlers.size === 0 || pendingRunWorkflow.length === 0) return
  const queued = pendingRunWorkflow.splice(0, pendingRunWorkflow.length)
  for (const payload of queued) dispatchRunWorkflow(payload)
}

function handleHostContextMessage(data: unknown) {
  if (!data || typeof data !== 'object') return
  const msg = data as Record<string, unknown>

  // 兼容旧版 IPSpace：仅 localPath 无法由浏览器读取，需宿主先 register 再发 runWorkflow
  if (msg.type === 'datachat:setOutFile') {
    const fileId = typeof msg.fileId === 'string' ? msg.fileId.trim() : ''
    const fileName =
      (typeof msg.fileName === 'string' && msg.fileName.trim()) ||
      (typeof msg.name === 'string' && msg.name.trim()) ||
      ''
    if (fileId && fileName) {
      dispatchRunWorkflow({
        domainId: typeof msg.domainId === 'string' ? msg.domainId.trim() : 'patent-oa',
        workflowId:
          typeof msg.workflowId === 'string' ? msg.workflowId.trim() : '7649037302673063970',
        input:
          typeof msg.input === 'string'
            ? msg.input
            : typeof msg.caseSerial === 'string' && msg.caseSerial.trim()
              ? `请对本案（${msg.caseSerial.trim()}）五书进行核稿`
              : '请对本案五书进行核稿',
        files: [{ fileId, name: fileName }],
        newSession: true,
      })
      postToParent({ type: 'datachat:setOutFileAck', ok: true })
      return
    }
    postToParent({
      type: 'datachat:setOutFileAck',
      ok: false,
      error: '缺少 fileId，请升级 IPSpace 宿主以登记文件后触发工作流',
    })
    return
  }

  if (msg.type === 'datachat:runWorkflow') {
    const payload: HostRunWorkflowPayload = {
      domainId: typeof msg.domainId === 'string' ? msg.domainId.trim() : undefined,
      workflowId: typeof msg.workflowId === 'string' ? msg.workflowId.trim() : undefined,
      workflowName: typeof msg.workflowName === 'string' ? msg.workflowName.trim() : undefined,
      input: typeof msg.input === 'string' ? msg.input : undefined,
      files: parseFiles(msg.files),
      newSession: msg.newSession !== false,
      prefillOnly: msg.prefillOnly === true,
    }
    if (!payload.workflowId && !payload.workflowName) {
      postToParent({ type: 'datachat:runWorkflowAck', ok: false, error: 'workflowId 或 workflowName 必填' })
      return
    }
    dispatchRunWorkflow(payload)
    postToParent({ type: 'datachat:runWorkflowAck', ok: true })
  }
}

export function onHostRunWorkflow(handler: HostRunWorkflowHandler): () => void {
  runWorkflowHandlers.add(handler)
  flushPendingHostRunWorkflow()
  return () => runWorkflowHandlers.delete(handler)
}

export function initHostContextBridge() {
  if (typeof window === 'undefined') return

  window.addEventListener('message', (ev) => handleHostContextMessage(ev.data))

  const webview = (
    window as unknown as {
      chrome?: { webview?: { addEventListener?: (t: string, h: (e: MessageEvent) => void) => void } }
    }
  ).chrome?.webview
  webview?.addEventListener?.('message', (ev: MessageEvent) => handleHostContextMessage(ev.data))

  if (window.DataChatHost) {
    window.DataChatHost.runWorkflow = (payload: HostRunWorkflowPayload) => {
      handleHostContextMessage({ type: 'datachat:runWorkflow', ...payload })
    }
  }
}

declare global {
  interface Window {
    __DATACHAT_TOKEN__?: string
    __DATACHAT_USER_ID__?: string
    __DATACHAT_USER_NAME__?: string
    DataChatHost?: {
      getToken: () => string | undefined
      setToken: (token: string) => void
      getUser: () => { userId: string; userName: string } | undefined
      setUser: (user: { userId: string; userName: string }, token?: string) => void
      isEmbedded: () => boolean
      runWorkflow?: (payload: HostRunWorkflowPayload) => void
    }
  }
}
