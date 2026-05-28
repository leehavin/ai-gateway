export type ChatRole = 'user' | 'assistant'

export interface ChatMessage {
  id: string
  role: ChatRole
  content: string
  loading?: boolean
  error?: boolean
  attachments?: { fileId: string; name: string }[]
}

export interface ChatSession {
  id: string
  messages: ChatMessage[]
}

export interface SessionMeta {
  id: string
  title: string
  updatedAt: number
  preview?: string
}

export interface DomainItem {
  id: string
  displayName: string
  chatMode: string
  provider: string
  model?: string
  placeholder?: string
  quickPrompts: string[]
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
