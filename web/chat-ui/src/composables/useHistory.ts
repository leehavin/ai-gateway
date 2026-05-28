import { computed, onMounted, onUnmounted, ref, watch, type Ref } from 'vue'
import { isNarrowScreen, NARROW_BREAKPOINT } from './useLayout'
import type { SessionMeta } from '../types'
import {
  createNewSession,
  deleteSessionRecord,
  formatHistoryDateGroup,
  formatHistoryTime,
  loadHistoryIndex,
  loadSession,
  setActiveSessionId,
} from '../utils/session'

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

  function refresh() {
    historyList.value = loadHistoryIndex(domainId.value)
    const active = historyList.value[0]?.id ?? ''
    if (!activeId.value || !historyList.value.some((x) => x.id === activeId.value)) {
      activeId.value = active
    }
  }

  watch(domainId, () => {
    searchKey.value = ''
    refresh()
  }, { immediate: true })

  let unbindMq: (() => void) | undefined
  onMounted(() => {
    unbindMq = bindNarrowMq()
  })
  onUnmounted(() => {
    unbindMq?.()
  })

  const filteredList = computed(() => {
    const q = searchKey.value.trim()
    if (!q) return historyList.value
    return historyList.value.filter(
      (x) =>
        x.title.includes(q) ||
        (x.preview && x.preview.includes(q))
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

  function selectSession(id: string) {
    activeId.value = id
    setActiveSessionId(domainId.value, id)
    closeAsideIfNarrow()
    return loadSession(domainId.value, id)
  }

  function removeSession(id: string) {
    deleteSessionRecord(domainId.value, id)
    refresh()
    if (activeId.value === id) {
      activeId.value = historyList.value[0]?.id ?? ''
      if (activeId.value) setActiveSessionId(domainId.value, activeId.value)
    }
    return activeId.value ? loadSession(domainId.value, activeId.value) : createNewSession(domainId.value)
  }

  function startNewSession() {
    const session = createNewSession(domainId.value)
    activeId.value = session.id
    refresh()
    closeAsideIfNarrow()
    return session
  }

  function toggleAside() {
    asideExpanded.value = !asideExpanded.value
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
  }
}
