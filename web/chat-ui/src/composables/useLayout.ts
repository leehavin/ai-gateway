import { onMounted, onUnmounted, ref } from 'vue'

export const NARROW_BREAKPOINT = '(max-width: 860px)'

export function isNarrowScreen(): boolean {
  if (typeof window === 'undefined') return false
  return window.matchMedia(NARROW_BREAKPOINT).matches
}

export function useLayout() {
  const isNarrow = ref(isNarrowScreen())
  let mq: MediaQueryList | null = null

  function onMqChange(e: MediaQueryListEvent) {
    isNarrow.value = e.matches
  }

  onMounted(() => {
    mq = window.matchMedia(NARROW_BREAKPOINT)
    isNarrow.value = mq.matches
    mq.addEventListener('change', onMqChange)
  })

  onUnmounted(() => {
    mq?.removeEventListener('change', onMqChange)
  })

  return { isNarrow }
}
