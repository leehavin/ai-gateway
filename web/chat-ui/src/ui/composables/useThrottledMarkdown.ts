import { ref, watch, type Ref } from 'vue'

/** SSE 流式更新时节流 Markdown 重算（默认 ~12fps） */
export function useThrottledMarkdown(
  source: Ref<string>,
  streaming: Ref<boolean>,
  intervalMs = 80
) {
  const displayed = ref(source.value)
  let timer: ReturnType<typeof setTimeout> | undefined

  function flush() {
    if (timer) {
      clearTimeout(timer)
      timer = undefined
    }
    displayed.value = source.value
  }

  watch(source, () => {
    if (!streaming.value) {
      flush()
      return
    }
    if (timer) clearTimeout(timer)
    timer = setTimeout(() => {
      displayed.value = source.value
      timer = undefined
    }, intervalMs)
  })

  watch(streaming, (active: boolean) => {
    if (!active) flush()
  })

  return displayed
}
