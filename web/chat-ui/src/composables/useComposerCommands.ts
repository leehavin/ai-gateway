import { computed, ref, type Ref } from 'vue'
import { categoryForDomain } from '../providers/category'
import {
  getProviderBySlashPrefix,
  resolveSlashMenuItems,
  type SlashCommandItem,
  type SlashMenuContext,
  type SlashTrigger,
} from '../providers'

export type CommandMenuKind = 'mention' | 'slash'
export type CommandMenuItem = SlashCommandItem

export interface CommandTrigger extends SlashTrigger {
  kind: CommandMenuKind
}

export function detectCommandTrigger(value: string, cursor: number): CommandTrigger | null {
  const before = value.slice(0, cursor)

  const slashNamed = before.match(/\/([a-z][\w-]*)$/i)
  if (slashNamed) {
    return {
      kind: 'slash',
      query: slashNamed[1].toLowerCase(),
      start: cursor - slashNamed[0].length,
      end: cursor,
    }
  }

  if (before.endsWith('/')) {
    return { kind: 'slash', query: '', start: cursor - 1, end: cursor }
  }

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

export function useComposerCommands(
  inputValue: Ref<string>,
  slashMenuContext: Ref<SlashMenuContext>
) {
  const cursorPos = ref(0)
  const activeIndex = ref(0)

  const trigger = computed((): CommandTrigger | null =>
    detectCommandTrigger(inputValue.value, cursorPos.value)
  )

  const menuItems = computed((): CommandMenuItem[] => {
    const t = trigger.value
    if (!t) return []

    if (t.kind === 'slash') {
      return resolveSlashMenuItems(t, slashMenuContext.value)
    }

    const needle = t.query.toLowerCase()
    return slashMenuContext.value.domains
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
  })

  const menuOpen = computed(() => trigger.value !== null && menuItems.value.length > 0)

  const slashMenuTitle = computed(() => {
    const t = trigger.value
    if (!t || t.kind !== 'slash') return '命令'
    if (!t.query) return '命令'
    const plugin = getProviderBySlashPrefix(t.query)
    return plugin ? `${plugin.displayName} 命令` : '命令'
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
    slashMenuTitle,
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
