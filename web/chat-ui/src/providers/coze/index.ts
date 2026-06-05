import type { ProviderPlugin } from '../types'
import { buildCozeSlashMenu, stripCozeSlashTokens } from './slashMenu'

export { fetchCozeWorkflows, streamCozeWorkflow, resumeCozeWorkflow } from './api'
export { useCozeResources } from './useCozeResources'
export { useCozeChat } from './useCozeChat'
export type { CozeChatDeps } from './useCozeChat'

let refreshWorkflowsHandler: (() => void) | null = null

export function bindCozeWorkflowRefresh(refresh: () => void) {
  refreshWorkflowsHandler = refresh
}

export const cozeProvider: ProviderPlugin = {
  id: 'coze',
  displayName: 'Coze',
  slashPrefix: 'coze',
  description: '工作流、Bot、上传、新会话',
  buildSlashMenu: buildCozeSlashMenu,
  onSlashMenuOpen: () => {
    refreshWorkflowsHandler?.()
  },
  stripSlashTokens: stripCozeSlashTokens,
}
