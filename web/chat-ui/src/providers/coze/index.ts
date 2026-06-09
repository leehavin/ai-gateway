import type { ProviderPlugin } from '../types'
import { buildCozeSlashMenu, stripCozeSlashTokens } from './slashMenu'

export { fetchCozeWorkflows, streamCozeWorkflow, resumeCozeWorkflow } from './api'
export { useCozeResources } from './useCozeResources'
export { useCozeChat } from './useCozeChat'
export type { CozeChatDeps } from './useCozeChat'

export const cozeProvider: ProviderPlugin = {
  id: 'coze',
  displayName: 'Coze',
  slashPrefix: 'coze',
  description: '工作流、Bot、上传、新会话',
  buildSlashMenu: buildCozeSlashMenu,
  stripSlashTokens: stripCozeSlashTokens,
}
