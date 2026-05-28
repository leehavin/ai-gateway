import { computed, ref, watch, type Ref } from 'vue'
import { buildHistory, streamChat } from '../api/gateway'
import type { ChatMessage, ChatSession, DomainItem } from '../types'
import { resolveActiveSession, saveSession, upsertSessionMeta } from '../utils/session'
import type { useAttachments } from './useAttachments'
import type { useHistory } from './useHistory'

function timeGreeting(): string {
  const h = new Date().getHours()
  if (h < 12) return '上午'
  if (h < 18) return '下午'
  return '晚上'
}

function uid() {
  return crypto.randomUUID?.() ?? `${Date.now()}-${Math.random().toString(16).slice(2)}`
}

export function useChat(
  domainId: Ref<string>,
  activeDomain: Ref<DomainItem | undefined>,
  history: ReturnType<typeof useHistory>,
  attachments: ReturnType<typeof useAttachments>
) {
  const session = ref<ChatSession>(resolveActiveSession(domainId.value))
  const inputValue = ref('')
  const sending = ref(false)
  const startPage = ref(session.value.messages.length === 0)
  let abortCtrl: AbortController | null = null

  history.activeId.value = session.value.id

  const userName = ref('您')
  const greetText = computed(() => `${userName.value}，${timeGreeting()}好！`)

  const description = computed(() => [
    `当前领域：${activeDomain.value?.displayName ?? domainId.value}`,
    'DataChat 统一网关接入 DB-GPT、Coze 与自研领域服务。',
    'AI 生成内容可能有误，请谨慎甄别使用。',
  ])

  const introPrompt = computed(() => {
    const list = activeDomain.value?.quickPrompts ?? []
    const icons = ['icon-info-o', 'icon-star', 'icon-priority', 'icon-priority']
    const colors = ['#5e7ce0', 'rgb(255, 215, 0)', '#3ac295', '#7b8de5']
    return {
      direction: 'horizontal' as const,
      list: list.slice(0, 4).map((label, i) => ({
        value: `p-${i}`,
        label,
        iconConfig: { name: icons[i % icons.length], color: colors[i % colors.length] },
        desc: label,
      })),
    }
  })

  const simplePrompt = computed(() =>
    (activeDomain.value?.quickPrompts ?? []).slice(0, 2).map((label, i) => ({
      value: `s-${i}`,
      label,
      iconConfig: { name: 'icon-info-o', color: '#5e7ce0' },
    }))
  )

  const placeholder = computed(
    () => activeDomain.value?.placeholder || '@智能体，输入您的问题…'
  )

  function applySession(next: ChatSession) {
    abortCtrl?.abort()
    abortCtrl = null
    sending.value = false
    session.value = next
    history.activeId.value = next.id
    startPage.value = next.messages.length === 0
    inputValue.value = ''
    attachments.clearAttachments()
  }

  function persist() {
    saveSession(domainId.value, session.value)
    if (session.value.messages.length > 0) {
      upsertSessionMeta(domainId.value, session.value)
      history.refresh()
    }
  }

  watch(domainId, () => {
    history.refresh()
    applySession(resolveActiveSession(domainId.value))
  })

  function newConversation() {
    applySession(history.startNewSession())
  }

  function loadSessionById(id: string) {
    applySession(history.selectSession(id))
  }

  async function onSubmit(text: string) {
    const msg = (text || inputValue.value).trim()
    const files = attachments.readyAttachments.value
    if ((!msg && !files.length) || sending.value || attachments.hasUploading.value) return
    const displayContent = (msg || '请根据附件回答') + attachments.formatAttachmentNote(files)
    inputValue.value = ''
    startPage.value = false
    history.closeAsideIfNarrow()
    sending.value = true

    session.value.messages.push({
      id: uid(),
      role: 'user',
      content: displayContent,
      attachments: files.length ? [...files] : undefined,
    })
    attachments.clearAttachments()
    const assistant: ChatMessage = {
      id: uid(),
      role: 'assistant',
      content: '',
      loading: true,
    }
    session.value.messages.push(assistant)
    persist()

    abortCtrl = new AbortController()
    const historyMsgs = buildHistory(
      session.value.messages
        .filter((m) => !m.loading && !m.error)
        .map((m) => ({ role: m.role, content: m.content })),
      displayContent
    )

    try {
      await streamChat(
        {
          sessionId: session.value.id,
          domain: domainId.value,
          message: msg || '请根据附件内容回答。',
          messages: historyMsgs,
          attachments: files.length ? files : undefined,
        },
        (delta) => {
          assistant.loading = false
          assistant.content += delta
          persist()
        },
        abortCtrl.signal
      )
      assistant.loading = false
    } catch (e) {
      assistant.loading = false
      if (e instanceof DOMException && e.name === 'AbortError') return
      assistant.error = true
      assistant.content =
        assistant.content || `[错误] ${e instanceof Error ? e.message : String(e)}`
    } finally {
      sending.value = false
      assistant.loading = false
      persist()
    }
  }

  return {
    session,
    inputValue,
    sending,
    startPage,
    greetText,
    description,
    introPrompt,
    simplePrompt,
    placeholder,
    newConversation,
    loadSessionById,
    applySession,
    onSubmit,
  }
}
