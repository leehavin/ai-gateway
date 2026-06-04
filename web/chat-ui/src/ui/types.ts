/** DataChat 对话 UI 公共类型 */

export type BubbleAlign = 'left' | 'right'

export interface AvatarConfig {
  imgSrc?: string
  name?: string
}

export interface PromptIconConfig {
  name: string
  size?: string
  color?: string
}

export interface PromptItem {
  value: string | number
  label: string
  iconConfig?: PromptIconConfig
  desc?: string
}

export type PromptDirection = 'horizontal' | 'vertical'

export type FileUploadStatus = 'uploading' | 'success' | 'error'

export interface ChatFileItem {
  uid: number
  name: string
  size?: number
  status: FileUploadStatus
  response?: unknown
  error?: unknown
}

export const ATTACHMENT_ACCEPT =
  '.txt,.md,.csv,.json,.pdf,.png,.jpg,.jpeg,.webp,.gif,.doc,.docx,.xls,.xlsx'

export const ATTACHMENT_MAX_COUNT = 5
export const ATTACHMENT_MAX_SIZE_MB = 10
