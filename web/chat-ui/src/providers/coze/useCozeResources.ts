import { ref, watch, type Ref } from 'vue'
import { fetchCozeWorkflows } from './api'
import type { CozeWorkflowItem } from '../../types'

/** Coze 领域：工作流列表（供 /coze 菜单与 registry 使用）。 */
export function useCozeResources(domainId: Ref<string>, enabled: Ref<boolean>) {
  const workflows = ref<CozeWorkflowItem[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  let loadGeneration = 0
  let debounceTimer: ReturnType<typeof setTimeout> | null = null
  let lastFetchedAt = 0

  async function refresh(options?: { silent?: boolean; force?: boolean }) {
    if (!enabled.value) {
      workflows.value = []
      error.value = null
      loading.value = false
      return
    }

    const silent = options?.silent ?? workflows.value.length > 0
    const force = options?.force ?? false
    const now = Date.now()
    if (!force && now - lastFetchedAt < 2000) return

    const gen = ++loadGeneration
    if (!silent) loading.value = true
    if (!silent) error.value = null

    try {
      const list = await fetchCozeWorkflows(domainId.value)
      if (gen !== loadGeneration) return
      workflows.value = list
      error.value = null
      lastFetchedAt = Date.now()
    } catch (e) {
      if (gen !== loadGeneration) return
      if (!silent) {
        workflows.value = []
        error.value = e instanceof Error ? e.message : String(e)
      }
    } finally {
      if (gen === loadGeneration) loading.value = false
    }
  }

  function scheduleRefresh(options?: { silent?: boolean; force?: boolean }) {
    if (debounceTimer) clearTimeout(debounceTimer)
    debounceTimer = setTimeout(() => {
      debounceTimer = null
      void refresh(options)
    }, 300)
  }

  watch([domainId, enabled], () => scheduleRefresh({ force: true }), { immediate: true })

  return {
    workflows,
    loading,
    error,
    refresh: (options?: { silent?: boolean; force?: boolean }) => scheduleRefresh(options),
    refreshNow: (options?: { silent?: boolean; force?: boolean }) => refresh(options),
  }
}
