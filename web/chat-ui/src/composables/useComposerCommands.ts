import { computed, ref, type Ref } from 'vue'
import type { CozeBotSummary, DomainItem } from '../types'

export type CommandMenuKind = 'mention' | 'slash'

export interface CommandMenuItem {
  id: string
  kind: 'domain' | 'action'
  label: string
  desc?: string
  category: string
  domainId?: string
  action?: 'switch-domain' | 'upload' | 'new-chat' | 'insert-mention'
}

export interface CommandTrigger {
  kind: CommandMenuKind
  query: string
  start: number
  end: number
}

function categoryForDomain(d: DomainItem): string {
  if (d.provider === 'coze') return 'Coze 智能体'
  if (d.provider === 'custom') return '自研插件'
  if (d.dbgpt?.knowledgeSpaceName) return '知识库'
  if (d.chatMode === 'DbGptData' || d.dbgpt?.chatMode === 'chat_data') return '数据问数'
  if (d.provider === 'dbgpt') return 'DB-GPT 智能体'
  return '智能体'
}

export function detectCommandTrigger(
  value: string,
  cursor: number,
  activeProvider?: string
): CommandTrigger | null {
  const before = value.slice(0, cursor)
  const slashCoze = before.match(/\/(coze[\w-]*)$/i)
  if (slashCoze) {
    const raw = slashCoze[1]
    return {
      kind: 'slash',
      query: raw.toLowerCase(),
      start: cursor - slashCoze[0].length,
      end: cursor,
    }
  }
  if (before.endsWith('/')) {
    const start = cursor - 1
    const query = activeProvider === 'coze' ? 'coze' : ''
    return { kind: 'slash', query, start, end: cursor }
  }
  // 仅匹配「词首」@，避免邮箱 user@corp.com 误触发
  const mention = before.match(/(?:^|[\s])@([^\s@]*)$/)
  if (mention) {
    const atStart = mention.index! + (mention[0].startsWith('@') ? 0 : mention[0].indexOf('@'))
    return {
      kind: 'mention',
      query: mention[1],
      start: atStart,
      end: cursor,
    }
  }
  return null
}

function buildCozeSlashItems(
  domains: DomainItem[],
  cozeBots: CozeBotSummary[]
): CommandMenuItem[] {
  const cozeDomains = domains.filter((d) => d.provider === 'coze')
  const items: CommandMenuItem[] = [
    {
      id: 'coze-upload',
      kind: 'action',
      label: '上传文件并对话',
      desc: '支持图片、PDF、文档等，由网关转发给当前 Bot',
      category: 'Coze',
      action: 'upload',
    },
    {
      id: 'coze-new',
      kind: 'action',
      label: '新建 Coze 会话',
      desc: '清空当前对话并保留 Coze 领域',
      category: 'Coze',
      action: 'new-chat',
    },
  ]
  for (const bot of cozeBots) {
    items.push({
      id: `coze-bot-${bot.domainId}`,
      kind: 'domain',
      label: bot.displayName,
      desc: `Bot ${bot.botId}`,
      category: '切换 Bot',
      domainId: bot.domainId,
      action: 'switch-domain',
    })
  }
  for (const d of cozeDomains) {
    if (cozeBots.some((b) => b.domainId === d.id)) continue
    items.push({
      id: `coze-domain-${d.id}`,
      kind: 'domain',
      label: d.displayName,
      desc: d.coze?.botId ? `Bot ${d.coze.botId}` : undefined,
      category: '切换 Bot',
      domainId: d.id,
      action: 'switch-domain',
    })
  }
  return items
}

export function useComposerCommands(
  inputValue: Ref<string>,
  domains: Ref<DomainItem[]>,
  cozeBots: Ref<CozeBotSummary[]>,
  activeProvider: Ref<string | undefined>
) {
  const cursorPos = ref(0)
  const activeIndex = ref(0)

  const trigger = computed(() =>
    detectCommandTrigger(inputValue.value, cursorPos.value, activeProvider.value)
  )

  const menuOpen = computed(
    () => trigger.value !== null && menuItems.value.length > 0
  )

  const menuItems = computed((): CommandMenuItem[] => {
    const t = trigger.value
    if (!t) return []

    if (t.kind === 'slash') {
      const q = t.query
      const isCozeCtx = activeProvider.value === 'coze' || q.startsWith('coze')
      if (!isCozeCtx) {
        return [
          {
            id: 'slash-mention',
            kind: 'action',
            label: '@ 切换智能体',
            desc: '插入 @ 并从列表选择领域',
            category: '快捷',
            action: 'insert-mention',
          },
        ]
      }
      const items = buildCozeSlashItems(domains.value, cozeBots.value)
      const needle = q === 'coze' || q === '' ? '' : q.replace(/^coze/, '').trim()
      if (!needle) return items
      return items.filter(
        (i) =>
          i.label.toLowerCase().includes(needle) ||
          i.desc?.toLowerCase().includes(needle) ||
          i.category.toLowerCase().includes(needle)
      )
    }

    const needle = t.query.toLowerCase()
    const list = domains.value
      .filter((d) => {
        if (!needle) return true
        const hay = `${d.displayName} ${d.id} ${d.provider} ${categoryForDomain(d)}`.toLowerCase()
        return hay.includes(needle)
      })
      .map((d) => ({
        id: `domain-${d.id}`,
        kind: 'domain' as const,
        label: d.displayName,
        desc: `${categoryForDomain(d)} · ${d.id}`,
        category: categoryForDomain(d),
        domainId: d.id,
        action: 'switch-domain' as const,
      }))
    return list
  })

  const groupedMenuItems = computed(() => {
    const groups = new Map<string, CommandMenuItem[]>()
    for (const item of menuItems.value) {
      const arr = groups.get(item.category) ?? []
      arr.push(item)
      groups.set(item.category, arr)
    }
    return [...groups.entries()].map(([category, items]) => ({ category, items }))
  })

  function setCursor(pos: number) {
    cursorPos.value = pos
  }

  function clampActiveIndex() {
    const n = menuItems.value.length
    if (n === 0) activeIndex.value = 0
    else if (activeIndex.value >= n) activeIndex.value = n - 1
    else if (activeIndex.value < 0) activeIndex.value = 0
  }

  function moveActive(delta: number) {
    const n = menuItems.value.length
    if (n === 0) return
    activeIndex.value = (activeIndex.value + delta + n) % n
  }

  function flatItemAt(index: number): CommandMenuItem | undefined {
    return menuItems.value[index]
  }

  function applyMentionInsert(item: CommandMenuItem): string {
    const t = trigger.value
    if (!t) return inputValue.value
    const tag = `@${item.label} `
    return inputValue.value.slice(0, t.start) + tag + inputValue.value.slice(t.end)
  }

  function removeTriggerText(): string {
    const t = trigger.value
    if (!t) return inputValue.value
    return inputValue.value.slice(0, t.start) + inputValue.value.slice(t.end)
  }

  return {
    cursorPos,
    activeIndex,
    trigger,
    menuOpen,
    menuItems,
    groupedMenuItems,
    setCursor,
    clampActiveIndex,
    moveActive,
    flatItemAt,
    applyMentionInsert,
    removeTriggerText,
    resetActiveIndex: () => {
      activeIndex.value = 0
    },
  }
}
