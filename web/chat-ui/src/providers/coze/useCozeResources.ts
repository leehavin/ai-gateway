import { ref, watch, type Ref } from 'vue'
import { fetchCozeWorkflows } from './api'
import type { CozeWorkflowItem } from '../../types'

/** Coze 领域：工作流列表（供 /coze 菜单与 registry 使用）。 */
export function useCozeResources(domainId: Ref<string>, enabled: Ref<boolean>) {
  const workflows = ref<CozeWorkflowItem[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function refresh() {
    if (!enabled.value) {
      workflows.value = []
      error.value = null
      return
    }
    loading.value = true
    error.value = null
    try {
      workflows.value = await fetchCozeWorkflows(domainId.value)
    } catch (e) {
      workflows.value = []
      error.value = e instanceof Error ? e.message : String(e)
    } finally {
      loading.value = false
    }
  }

  watch([domainId, enabled], () => void refresh(), { immediate: true })

  return { workflows, loading, error, refresh }
}
