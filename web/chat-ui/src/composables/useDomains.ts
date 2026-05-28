import { onMounted, ref } from 'vue'
import { fetchDomains, fetchHealth } from '../api/gateway'
import type { DomainItem, GatewayHealth } from '../types'

export function useDomains() {
  const domains = ref<DomainItem[]>([])
  const health = ref<GatewayHealth | null>(null)
  const loading = ref(true)
  const error = ref<string | null>(null)
  const domainId = ref(import.meta.env.VITE_DEFAULT_DOMAIN || 'patent')

  async function refresh() {
    loading.value = true
    error.value = null
    try {
      health.value = await fetchHealth()
      domains.value = await fetchDomains()
      if (domains.value.length && !domains.value.some((d) => d.id === domainId.value)) {
        domainId.value = domains.value[0].id
      }
    } catch (e) {
      error.value = e instanceof Error ? e.message : String(e)
    } finally {
      loading.value = false
    }
  }

  onMounted(() => void refresh())

  return { domains, health, loading, error, domainId, refresh }
}
