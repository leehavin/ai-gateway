import type { DomainItem } from '../types'

export function categoryForDomain(d: DomainItem): string {
  if (d.provider === 'coze') return 'Coze 智能体'
  if (d.provider === 'custom') return '自研插件'
  if (d.dbgpt?.knowledgeSpaceName) return '知识库'
  if (d.chatMode === 'DbGptData' || d.dbgpt?.chatMode === 'chat_data') return '数据问数'
  if (d.provider === 'dbgpt') return 'DB-GPT 智能体'
  return '智能体'
}
