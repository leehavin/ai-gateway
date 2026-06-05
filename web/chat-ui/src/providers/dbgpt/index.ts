import type { ProviderPlugin } from '../types'
import { buildDbgptSlashMenu, stripDbgptSlashTokens } from './slashMenu'

export const dbgptProvider: ProviderPlugin = {
  id: 'dbgpt',
  displayName: 'DB-GPT',
  slashPrefix: 'dbgpt',
  description: '数据问数、快捷问题、切换 DB-GPT 领域',
  buildSlashMenu: buildDbgptSlashMenu,
  stripSlashTokens: stripDbgptSlashTokens,
}
