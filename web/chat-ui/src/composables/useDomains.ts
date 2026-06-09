import { ref, type Ref } from 'vue'
import { ApiUnauthorizedError } from '../api/http'
import { fetchCozeBots, fetchDomains, fetchHealth } from '../api/gateway'
import type { CozeBotSummary, DomainItem, GatewayHealth } from '../types'

export function useDomains(ready: Ref<boolean>) {
  const domains = ref<DomainItem[]>([])
  const cozeBots = ref<CozeBotSummary[]>([])
  const health = ref<GatewayHealth | null>(null)
  const loading = ref(true)
  const error = ref<string | null>(null)
  const domainId = ref(import.meta.env.VITE_DEFAULT_DOMAIN || '')

  async function refresh() {
    if (!ready.value) return
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
      if (!domains.value.length) {
        domainId.value = ''
      }
    } catch (e) {
      if (e instanceof ApiUnauthorizedError) throw e
      error.value = e instanceof Error ? e.message : String(e)
    } finally {
      loading.value = false
    }
  }

  return { domains, cozeBots, health, loading, error, domainId, refresh }
}
