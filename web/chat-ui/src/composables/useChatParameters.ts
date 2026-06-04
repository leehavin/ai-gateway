import { ref, watch, type Ref } from 'vue'
import {
  DEFAULT_CHAT_PARAMS,
  normalizeChatParams,
  type ChatGenerationParameters,
} from '../types/chatParams'

const STORAGE_PREFIX = 'datachat:params:'

function storageKey(domainId: string) {
  return `${STORAGE_PREFIX}${domainId}`
}

function load(domainId: string): ChatGenerationParameters {
  try {
    const raw = localStorage.getItem(storageKey(domainId))
    if (!raw) return { ...DEFAULT_CHAT_PARAMS }
    const parsed = JSON.parse(raw) as Partial<ChatGenerationParameters>
    return normalizeChatParams(parsed)
  } catch {
    return { ...DEFAULT_CHAT_PARAMS }
  }
}

function save(domainId: string, params: ChatGenerationParameters) {
  localStorage.setItem(storageKey(domainId), JSON.stringify(params))
}

export function useChatParameters(domainId: Ref<string>) {
  const params = ref<ChatGenerationParameters>(load(domainId.value))
  const panelOpen = ref(false)

  watch(domainId, (id) => {
    params.value = load(id)
  })

  function persist() {
    params.value = normalizeChatParams(params.value)
    save(domainId.value, params.value)
  }

  function applyParams(next: ChatGenerationParameters) {
    params.value = normalizeChatParams(next)
    save(domainId.value, params.value)
  }

  function resetDefaults() {
    params.value = { ...DEFAULT_CHAT_PARAMS }
    persist()
  }

  function togglePanel() {
    panelOpen.value = !panelOpen.value
  }

  function closePanel() {
    panelOpen.value = false
  }

  return {
    params,
    panelOpen,
    persist,
    applyParams,
    resetDefaults,
    togglePanel,
    closePanel,
  }
}
