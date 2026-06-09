import { registerProvider } from './registry'
import { cozeProvider } from './coze'
import { dbgptProvider } from './dbgpt'
import { customProvider } from './custom'

registerProvider(cozeProvider)
registerProvider(dbgptProvider)
registerProvider(customProvider)

export {
  buildRootSlashMenu,
  getProvider,
  getProviderBySlashPrefix,
  listProviders,
  registerProvider,
  resolveSlashMenuItems,
  stripAllProviderSlashTokens,
} from './registry'
export { categoryForDomain } from './category'
export type {
  ProviderPlugin,
  SlashCommandItem,
  SlashMenuContext,
  SlashTrigger,
  SlashActionKind,
  ProviderChatBanner,
} from './types'
export { cozeProvider, useCozeResources, useCozeChat } from './coze/index'
export type { CozeChatDeps } from './coze/useCozeChat'
export { dbgptProvider } from './dbgpt'
export { customProvider } from './custom'
