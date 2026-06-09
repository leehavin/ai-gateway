import { computed, onMounted, onUnmounted, ref, watch, type Ref } from 'vue'
import {
  deleteServerSession,
  fetchServerSession,
  fetchServerSessions,
} from '../api/sessions'
import { isNarrowScreen, NARROW_BREAKPOINT } from './useLayout'
import type { ChatSession, SessionMeta } from '../types'
import { newSessionId, formatHistoryTime, formatHistoryDateGroup } from '../utils/session'

export interface HistoryGroup {
  title: string
  items: SessionMeta[]
}

export function useHistory(domainId: Ref<string>) {
  const historyList = ref<SessionMeta[]>([])
  const activeId = ref('')
  const searchKey = ref('')
  const asideExpanded = ref(!isNarrowScreen())

  function closeAsideIfNarrow() {
    if (isNarrowScreen()) asideExpanded.value = false
  }

  function bindNarrowMq() {
    if (typeof window === 'undefined') return
    const mq = window.matchMedia(NARROW_BREAKPOINT)
    const onChange = (e: MediaQueryListEvent) => {
      asideExpanded.value = !e.matches
    }
    mq.addEventListener('change', onChange)
    return () => mq.removeEventListener('change', onChange)
  }

  async function refresh() {
    try {
      historyList.value = await fetchServerSessions(domainId.value)
    } catch {
      historyList.value = []
    }
  }

  watch(domainId, () => {
    if (!domainId.value) {
      historyList.value = []
      return
    }
    searchKey.value = ''
    void refresh()
  })

  let unbindMq: (() => void) | undefined
  onMounted(() => { unbindMq = bindNarrowMq() })
  onUnmounted(() => { unbindMq?.() })

  const filteredList = computed(() => {
    const q = searchKey.value.trim()
    if (!q) return historyList.value
    return historyList.value.filter(
      (x) => x.title.includes(q) || (x.preview && x.preview.includes(q))
    )
  })

  const groupedList = computed((): HistoryGroup[] => {
    const map = new Map<string, SessionMeta[]>()
    for (const item of filteredList.value) {
      const g = formatHistoryDateGroup(item.updatedAt)
      if (!map.has(g)) map.set(g, [])
      map.get(g)!.push(item)
    }
    return Array.from(map.entries()).map(([title, items]) => ({ title, items }))
  })

  async function selectSession(id: string): Promise<ChatSession> {
    activeId.value = id
    closeAsideIfNarrow()
    return await fetchServerSession(domainId.value, id)
  }

  async function removeSession(id: string): Promise<ChatSession> {
    try {
      await deleteServerSession(id)
    } catch {
      /* 服务端失败可忽略，列表刷新后消失 */
    }
    await refresh()
    if (activeId.value === id) {
      activeId.value = historyList.value[0]?.id ?? ''
    }
    if (activeId.value) return await selectSession(activeId.value)
    return { id: newSessionId(), messages: [] }
  }

  function startNewSession(): ChatSession {
    closeAsideIfNarrow()
    return { id: newSessionId(), messages: [] }
  }

  function toggleAside() {
    asideExpanded.value = !asideExpanded.value
  }

  /** 仅更新内存中的标题（服务端无专用 rename 接口，刷新后恢复服务端标题） */
  function renameSessionTitle(id: string, title: string) {
    const item = historyList.value.find((x) => x.id === id)
    if (item) item.title = title.trim() || item.title
  }

  /** 仅更新内存中的置顶状态 */
  function togglePin(id: string) {
    const item = historyList.value.find((x) => x.id === id)
    if (item) item.pinned = !item.pinned
  }

  return {
    historyList,
    activeId,
    searchKey,
    asideExpanded,
    groupedList,
    filteredList,
    refresh,
    selectSession,
    removeSession,
    startNewSession,
    toggleAside,
    closeAsideIfNarrow,
    bindNarrowMq,
    formatHistoryTime,
    renameSessionTitle,
    togglePin,
  }
}
