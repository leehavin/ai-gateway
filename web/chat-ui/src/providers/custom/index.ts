import type { ProviderPlugin } from '../types'

export const customProvider: ProviderPlugin = {
  id: 'custom',
  displayName: '自研',
  slashPrefix: 'custom',
  description: '自研 HTTP 领域（预留）',
  buildSlashMenu: () => [
    {
      id: 'custom-hint',
      kind: 'action',
      label: '自研领域对话',
      desc: '经 Gateway 转发至 custom.endpoint',
      category: '自研',
      providerId: 'custom',
    },
  ],
  stripSlashTokens: (message) => message.replace(/^\/custom(?:\s+\S+)?\s*/i, '').trim(),
}
