import { categoryForDomain } from '../category'
import type { SlashCommandItem, SlashMenuContext } from '../types'

export function buildDbgptSlashMenu(ctx: SlashMenuContext, _filter: string): SlashCommandItem[] {
  const active = ctx.domains.find((d) => d.id === ctx.domainId)
  const items: SlashCommandItem[] = []

  if (active?.dbgpt?.datasourceId) {
    items.push({
      id: 'dbgpt-datasource',
      kind: 'action',
      label: `当前数据源：${active.dbgpt.datasourceId}`,
      desc: '数据问数领域，可直接输入自然语言查询',
      category: 'DB-GPT',
      providerId: 'dbgpt',
    })
  }

  for (const [i, label] of (active?.quickPrompts ?? []).entries()) {
    items.push({
      id: `dbgpt-prompt-${i}`,
      kind: 'action',
      label,
      desc: '填入输入框',
      category: '快捷问题',
      providerId: 'dbgpt',
      action: 'fill-prompt',
      promptText: label,
    })
  }

  for (const d of ctx.domains.filter((x) => x.provider === 'dbgpt')) {
    if (d.id === ctx.domainId) continue
    items.push({
      id: `dbgpt-domain-${d.id}`,
      kind: 'domain',
      label: d.displayName,
      desc: `${categoryForDomain(d)} · ${d.id}`,
      category: '切换领域',
      providerId: 'dbgpt',
      domainId: d.id,
      action: 'switch-domain',
    })
  }

  if (items.length === 0) {
    items.push({
      id: 'dbgpt-hint',
      kind: 'action',
      label: '直接输入问题',
      desc: 'DB-GPT 对话经网关 /v1/chat/stream',
      category: 'DB-GPT',
      providerId: 'dbgpt',
    })
  }

  return items
}

export function stripDbgptSlashTokens(message: string): string {
  return message.replace(/^\/dbgpt(?:\s+\S+)?\s*/i, '').trim()
}
