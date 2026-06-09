import type { CozeBotSummary, CozeWorkflowItem, DomainItem } from '../types'

/** 斜杠 / @ 命令菜单项（与 UI 组件解耦，由各 provider 生成）。 */
export interface SlashCommandItem {
  id: string
  kind: 'domain' | 'action'
  label: string
  desc?: string
  category: string
  providerId?: string
  domainId?: string
  workflowId?: string
  slashPrefix?: string
  promptText?: string
  action?: SlashActionKind
}

export type SlashActionKind =
  | 'switch-domain'
  | 'insert-mention'
  | 'insert-slash-prefix'
  | 'upload'
  | 'new-chat'
  | 'run-workflow'
  | 'refresh-workflows'
  | 'fill-prompt'

export interface SlashMenuContext {
  domains: DomainItem[]
  domainId: string
  activeProvider?: string
  cozeBots: CozeBotSummary[]
  cozeWorkflows: CozeWorkflowItem[]
  cozeWorkflowsLoading: boolean
  cozeWorkflowsError: string | null
}

export interface SlashTrigger {
  kind: 'mention' | 'slash'
  /** 裸 `/` 为空串；`/coze` 为 `coze`；`/dbgpt` 为 `dbgpt` */
  query: string
  start: number
  end: number
}

export interface ProviderPlugin {
  id: string
  displayName: string
  /** 不含前导 `/`，如 `coze`、`dbgpt` */
  slashPrefix: string
  description?: string
  /** 构建 `/prefix` 子菜单；query 为 prefix 后的过滤词 */
  buildSlashMenu(ctx: SlashMenuContext, filter: string): SlashCommandItem[]
  /** 打开该 provider 斜杠菜单时（如刷新工作流列表） */
  onSlashMenuOpen?(ctx: SlashMenuContext): void
  /** 从用户消息中剥离本 provider 的斜杠命令残留 */
  stripSlashTokens?(message: string): string
}

export type ProviderChatBanner =
  | { kind: 'workflow-interrupt'; nodeTitle?: string; onCancel: () => void }
  | {
      kind: 'workflow-pending'
      workflow: CozeWorkflowItem
      onCancel: () => void
    }
