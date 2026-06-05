export type ChatRole = 'user' | 'assistant'

export interface ChatCitation {
  title: string
  url?: string
  snippet?: string
}

export interface ChatMessage {
  id: string
  role: ChatRole
  content: string
  loading?: boolean
  error?: boolean
  thinking?: string
  citations?: ChatCitation[]
  attachments?: { fileId: string; name: string }[]
  /** 用户反馈：有用 / 无用 */
  feedback?: 'up' | 'down'
}

export type { ChatGenerationParameters } from './chatParams'
export { DEFAULT_CHAT_PARAMS } from './chatParams'

export interface ChatSession {
  id: string
  messages: ChatMessage[]
}

export interface SessionMeta {
  id: string
  title: string
  updatedAt: number
  preview?: string
  /** 用户手动重命名的标题 */
  customTitle?: string
  /** 置顶会话 */
  pinned?: boolean
}

export interface CozeDomainInfo {
  botId?: string
  endpoint?: string
}

export interface DbgptDomainInfo {
  chatMode?: string
  appId?: string
  datasourceId?: string
  knowledgeSpaceName?: string
}

export interface DomainItem {
  id: string
  displayName: string
  chatMode: string
  provider: string
  model?: string
  placeholder?: string
  quickPrompts: string[]
  dbgpt?: DbgptDomainInfo
  coze?: CozeDomainInfo
}

export interface CozeBotSummary {
  domainId: string
  displayName: string
  botId: string
  endpoint: string
  apiKeyRef: string
}

export interface CozeWorkflowItem {
  workflowId: string
  displayName: string
  description?: string
  iconUrl?: string
  appId?: string
  inputParameter: string
}

export interface CozeWorkflowInterrupt {
  workflowId: string
  eventId: string
  interruptType: number
  nodeTitle?: string
  prompt?: string
}

export interface GatewayHealth {
  status: string
  service: string
  useMock: boolean
  dbgptReachable: boolean
  dbgptBaseUrl?: string
  domainCount: number
}

export interface ChatStreamMessageDto {
  role: string
  content: string
}
