import { onMounted, ref } from 'vue'
import { fetchCozeBots, fetchDomains, fetchHealth } from '../api/gateway'
import type { CozeBotSummary, DomainItem, GatewayHealth } from '../types'

export function useDomains() {
  const domains = ref<DomainItem[]>([])
  const cozeBots = ref<CozeBotSummary[]>([])
  const health = ref<GatewayHealth | null>(null)
  const loading = ref(true)
  const error = ref<string | null>(null)
  const domainId = ref(import.meta.env.VITE_DEFAULT_DOMAIN || '')

  async function refresh() {
    loading.value = true
    error.value = null
    try {
      health.value = await fetchHealth()
      const [list, bots] = await Promise.all([fetchDomains(), fetchCozeBots()])
      domains.value = list.map((d) => ({
        ...d,
        quickPrompts: d.quickPrompts ?? [],
      }))
      cozeBots.value = bots
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

  return { domains, cozeBots, health, loading, error, domainId, refresh }
}
