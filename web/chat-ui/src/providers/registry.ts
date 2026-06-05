import type { ProviderPlugin, SlashCommandItem, SlashMenuContext, SlashTrigger } from './types'

const plugins = new Map<string, ProviderPlugin>()
const byPrefix = new Map<string, ProviderPlugin>()

export function registerProvider(plugin: ProviderPlugin): void {
  plugins.set(plugin.id, plugin)
  byPrefix.set(plugin.slashPrefix.toLowerCase(), plugin)
}

export function getProvider(id: string | undefined): ProviderPlugin | undefined {
  if (!id) return undefined
  return plugins.get(id)
}

export function getProviderBySlashPrefix(prefix: string): ProviderPlugin | undefined {
  return byPrefix.get(prefix.toLowerCase())
}

export function listProviders(): ProviderPlugin[] {
  return [...plugins.values()]
}

export function buildRootSlashMenu(_ctx: SlashMenuContext): SlashCommandItem[] {
  const items: SlashCommandItem[] = [
    {
      id: 'root-mention',
      kind: 'action',
      label: '@ 切换智能体',
      desc: '插入 @ 并从列表选择领域',
      category: '快捷',
      action: 'insert-mention',
    },
  ]
  for (const p of listProviders()) {
    items.push({
      id: `root-prefix-${p.id}`,
      kind: 'action',
      label: `/${p.slashPrefix}`,
      desc: p.description ?? `${p.displayName} 命令`,
      category: '命令',
      providerId: p.id,
      slashPrefix: p.slashPrefix,
      action: 'insert-slash-prefix',
    })
  }
  return items
}

export function resolveSlashMenuItems(
  trigger: SlashTrigger,
  ctx: SlashMenuContext
): SlashCommandItem[] {
  const q = trigger.query
  if (!q) return buildRootSlashMenu(ctx)

  const plugin = getProviderBySlashPrefix(q)
  if (!plugin) return buildRootSlashMenu(ctx)

  plugin.onSlashMenuOpen?.(ctx)
  const filter = q === plugin.slashPrefix ? '' : q.slice(plugin.slashPrefix.length).trim()
  const items = plugin.buildSlashMenu(ctx, filter)
  if (!filter) return items
  const needle = filter.toLowerCase()
  return items.filter(
    (i) =>
      i.label.toLowerCase().includes(needle) ||
      i.desc?.toLowerCase().includes(needle) ||
      i.category.toLowerCase().includes(needle)
  )
}

export function stripAllProviderSlashTokens(message: string): string {
  let s = message.trim()
  for (const p of listProviders()) {
    if (p.stripSlashTokens) s = p.stripSlashTokens(s)
  }
  return s.replace(/^\/\s*/, '').trim()
}
