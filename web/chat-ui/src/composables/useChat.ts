import { computed, onMounted, onUnmounted, ref, watch, type Ref } from 'vue'
import { toDisplayUser } from '../api/auth'
import { submitMessageFeedback } from '../api/feedback'
import { buildHistory, streamChat } from '../api/gateway'
import { getHostUser, onHostUserChange } from '../bridge/hostAuth'
import type { HostRunWorkflowPayload } from '../bridge/hostContext'
import { useCozeChat } from '../providers/coze'
import { normalizeChatParams, type ChatGenerationParameters } from '../types/chatParams'
import { newSessionId } from '../utils/session'
import type { ChatAttachmentRef } from '../types/attachments'
import type { ChatMessage, ChatSession, CozeWorkflowItem, DomainItem } from '../types'
import { copyToClipboard } from '../utils/clipboard'
import {
  normalizeOutgoingMessage,
  stripAttachmentNote,
  toApiUserContent,
} from '../utils/composerTokens'
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

function emptySession(): ChatSession {
  return { id: newSessionId(), messages: [] }
}

export function useChat(
  domainId: Ref<string>,
  activeDomain: Ref<DomainItem | undefined>,
  history: ReturnType<typeof useHistory>,
  attachments: ReturnType<typeof useAttachments>,
  chatParams: Ref<ChatGenerationParameters>,
  cozeWorkflows: Ref<CozeWorkflowItem[]>,
  cozeWorkflowsLoading?: Ref<boolean>,
  refreshCozeWorkflowsNow?: (options?: {
    silent?: boolean
    force?: boolean
  }) => Promise<void>
) {
  const session = ref<ChatSession>(emptySession())
  const inputValue = ref('')
  const sending = ref(false)
  const editingUserId = ref<string | null>(null)
  const toast = ref('')

  let toastTimer: ReturnType<typeof setTimeout> | null = null
  let abortCtrl: AbortController | null = null

  function showToast(message: string) {
    toast.value = message
    if (toastTimer) clearTimeout(toastTimer)
    toastTimer = setTimeout(() => {
      toast.value = ''
      toastTimer = null
    }, 2200)
  }

  const hasHistory = computed(() => history.historyList.value.length > 0)
  const startPage = ref(false)
  const isEditing = computed(() => !!editingUserId.value)
  const composerFocusNonce = ref(0)

  const isCozeDomain = computed(() => activeDomain.value?.provider === 'coze')

  const cozeChat = useCozeChat({
    enabled: isCozeDomain,
    domainId,
    session,
    inputValue,
    sending,
    composerFocusNonce,
    showToast,
    closeAsideIfNarrow: () => history.closeAsideIfNarrow(),
    refreshHistory: () => void history.refresh(),
    setStartPage: (v) => {
      startPage.value = v
    },
    getAbortSignal: () => abortCtrl?.signal,
    setAbortController: (c) => {
      abortCtrl = c
    },
    getReadyAttachments: () => attachments.readyAttachments.value,
    formatAttachmentNote: attachments.formatAttachmentNote,
    clearAttachments: attachments.clearAttachments,
    hasUploading: () => attachments.hasUploading.value,
  })

  const userName = ref(toDisplayUser(getHostUser()))
  const greetText = computed(() => `${userName.value}，${timeGreeting()}好！`)

  let offHostUser = () => {}
  onMounted(() => {
    if (!startPage.value) composerFocusNonce.value++
    offHostUser = onHostUserChange(() => {
      userName.value = toDisplayUser(getHostUser())
    })
  })
  onUnmounted(() => offHostUser())

  const description = computed(() => {
    const name = activeDomain.value?.displayName
    return name ? [`当前领域：${name}`] : []
  })

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

  const placeholder = computed(() => {
    const cozeHint = cozeChat.placeholderHint.value
    if (cozeHint) return cozeHint
    if (editingUserId.value) return '修改后发送，将从此处重新生成回复'
    if (activeDomain.value?.placeholder) return activeDomain.value.placeholder
    return '输入问题；/ 命令 · @ 切换智能体'
  })

  function applySession(
    next: ChatSession,
    options?: { keepInput?: boolean; enterCompose?: boolean }
  ) {
    abortCtrl?.abort()
    abortCtrl = null
    sending.value = false
    editingUserId.value = null
    session.value = next
    history.activeId.value = next.id
    startPage.value = options?.enterCompose
      ? false
      : next.messages.length === 0 && hasHistory.value
    if (!options?.keepInput) inputValue.value = ''
    attachments.clearAttachments()
    cozeChat.clearState()
    if (options?.enterCompose) composerFocusNonce.value++
  }

  const preserveInputNextDomainSwitch = ref(false)

  let historyInitialized = false
  watch(history.historyList, async (list) => {
    if (historyInitialized) return
    historyInitialized = true
    if (list.length > 0) {
      const loaded = await history.selectSession(list[0].id)
      applySession(loaded)
    }
  })

  watch(domainId, () => {
    historyInitialized = false
    session.value = emptySession()
    startPage.value = false
    cozeChat.clearState()
    if (!preserveInputNextDomainSwitch.value) inputValue.value = ''
    preserveInputNextDomainSwitch.value = false
  })

  function markPreserveInputOnNextDomainSwitch() {
    preserveInputNextDomainSwitch.value = true
  }

  async function loadSessionById(id: string) {
    applySession(await history.selectSession(id))
  }

  function stopGenerating() {
    abortCtrl?.abort()
    abortCtrl = null
    sending.value = false
  }

  function buildStreamHistory(apiUserText: string, excludeMessageId?: string) {
    return buildHistory(
      session.value.messages
        .filter((m) => !m.loading && !m.error && m.id !== excludeMessageId)
        .map((m) => ({ role: m.role, content: m.content })),
      apiUserText,
      toApiUserContent
    )
  }

  async function runAssistantStream(
    assistant: ChatMessage,
    apiUserText: string,
    files: ChatAttachmentRef[]
  ) {
    const historyMsgs = buildStreamHistory(apiUserText, assistant.id)
    abortCtrl = new AbortController()
    try {
      await streamChat(
        {
          sessionId: session.value.id,
          domain: domainId.value,
          message: apiUserText || '请根据附件内容回答。',
          messages: historyMsgs,
          attachments: files.length ? files : undefined,
          parameters: normalizeChatParams(chatParams.value),
        },
        (ev) => {
          assistant.loading = false
          assistant.error = false
          if (ev.text) assistant.content += ev.text
          if (ev.thinking) assistant.thinking = (assistant.thinking ?? '') + ev.thinking
          if (ev.citations?.length) assistant.citations = ev.citations
        },
        abortCtrl.signal
      )
      assistant.loading = false
      assistant.error = false
    } catch (e) {
      assistant.loading = false
      if (e instanceof DOMException && e.name === 'AbortError') return
      assistant.error = true
      assistant.content =
        assistant.content || `[错误] ${e instanceof Error ? e.message : String(e)}`
    } finally {
      sending.value = false
      assistant.loading = false
      abortCtrl = null
      void history.refresh()
    }
  }

  async function streamAfterUserMessage(displayContent: string, files: ChatAttachmentRef[]) {
    const assistant: ChatMessage = {
      id: uid(),
      role: 'assistant',
      content: '',
      thinking: '',
      loading: true,
    }
    session.value.messages.push(assistant)
    sending.value = true
    const apiUserText = toApiUserContent(displayContent)
    await runAssistantStream(assistant, apiUserText, files)
  }

  async function onSubmit(text: string) {
    const raw = (text || inputValue.value).trim()
    if (editingUserId.value) {
      await editUserMessage(editingUserId.value, raw)
      return
    }

    if (await cozeChat.tryHandleSubmit(raw)) return

    const msg = normalizeOutgoingMessage(raw)
    const files = attachments.readyAttachments.value
    if ((!msg && !files.length) || sending.value || attachments.hasUploading.value) return

    const displayContent = (msg || '请根据附件回答') + attachments.formatAttachmentNote(files)
    inputValue.value = ''
    startPage.value = false
    history.closeAsideIfNarrow()
    sending.value = true

    const userMsg: ChatMessage = {
      id: uid(),
      role: 'user',
      content: displayContent,
      attachments: files.length ? [...files] : undefined,
    }
    session.value.messages.push(userMsg)
    attachments.clearAttachments()

    await streamAfterUserMessage(displayContent, files)
  }

  function findUserBeforeAssistant(assistantId: string) {
    const idx = session.value.messages.findIndex((m) => m.id === assistantId)
    if (idx < 1) return null
    const userMsg = session.value.messages[idx - 1]
    return userMsg.role === 'user' ? { idx, userMsg } : null
  }

  async function regenerateAssistant(assistantId: string) {
    if (sending.value) return
    const pair = findUserBeforeAssistant(assistantId)
    if (!pair) return
    session.value.messages.splice(pair.idx + 1)
    await streamAfterUserMessage(pair.userMsg.content, pair.userMsg.attachments ?? [])
  }

  async function retryAssistant(assistantId: string) {
    await regenerateAssistant(assistantId)
  }

  function startEditUserMessage(userId: string) {
    if (sending.value) return
    const msg = session.value.messages.find((m) => m.id === userId && m.role === 'user')
    if (!msg) return
    editingUserId.value = userId
    inputValue.value = stripAttachmentNote(msg.content)
    composerFocusNonce.value++
  }

  function cancelEditUserMessage() {
    editingUserId.value = null
    inputValue.value = ''
  }

  async function editUserMessage(userId: string, rawText: string) {
    if (sending.value) return
    const idx = session.value.messages.findIndex((m) => m.id === userId)
    if (idx < 0 || session.value.messages[idx].role !== 'user') return

    const msg = normalizeOutgoingMessage(rawText)
    const prev = session.value.messages[idx]
    const files = prev.attachments ?? []
    if (!msg && !files.length) return

    editingUserId.value = null
    inputValue.value = ''
    startPage.value = false

    const displayContent = (msg || '请根据附件回答') + attachments.formatAttachmentNote(files)
    prev.content = displayContent
    session.value.messages.splice(idx + 1)

    await streamAfterUserMessage(displayContent, files)
  }

  function newConversation() {
    applySession(history.startNewSession(), { enterCompose: true })
  }

  async function copyMessage(messageId: string) {
    const msg = session.value.messages.find((m) => m.id === messageId)
    if (!msg?.content.trim()) return
    const text = msg.role === 'user' ? stripAttachmentNote(msg.content) : msg.content
    const ok = await copyToClipboard(text)
    showToast(ok ? '已复制到剪贴板' : '复制失败')
  }

  function canRegenerateAssistant(msg: ChatMessage, index: number): boolean {
    if (msg.role !== 'assistant' || sending.value || msg.loading) return false
    if (index < 1) return false
    return session.value.messages[index - 1]?.role === 'user'
  }

  function canEditUserMessage(msg: ChatMessage): boolean {
    return msg.role === 'user' && !sending.value
  }

  async function setMessageFeedback(messageId: string, rating: 'up' | 'down') {
    const msg = session.value.messages.find((m) => m.id === messageId)
    if (!msg || msg.role !== 'assistant') return
    if (msg.feedback === rating) return
    msg.feedback = rating
    try {
      await submitMessageFeedback({
        sessionId: session.value.id,
        messageId,
        domain: domainId.value,
        rating,
      })
      showToast(rating === 'up' ? '感谢反馈' : '已记录，我们会持续改进')
    } catch {
      showToast('反馈提交失败，请稍后重试')
    }
  }

  function runCozeWorkflow(workflowId: string) {
    cozeChat.queueWorkflowById(workflowId, cozeWorkflows.value)
  }

  async function resolveHostWorkflow(
    payload: HostRunWorkflowPayload
  ): Promise<CozeWorkflowItem | null> {
    const deadline = Date.now() + 15000
    while (Date.now() < deadline) {
      if (refreshCozeWorkflowsNow) {
        await refreshCozeWorkflowsNow({ silent: true, force: true }).catch(() => {})
      }
      const hit = cozeChat.findWorkflow(
        cozeWorkflows.value,
        payload.workflowId,
        payload.workflowName
      )
      if (hit) return hit
      if (!cozeWorkflowsLoading?.value && cozeWorkflows.value.length > 0) break
      await new Promise((r) => setTimeout(r, 300))
    }
    if (payload.workflowId) {
      return cozeChat.syntheticWorkflow(payload.workflowId, payload.workflowName)
    }
    return cozeChat.findWorkflow(cozeWorkflows.value, undefined, payload.workflowName)
  }

  async function handleHostRunWorkflow(payload: HostRunWorkflowPayload) {
    if (sending.value) {
      showToast('当前正在生成，请稍后再试')
      return
    }

    if (payload.domainId && payload.domainId !== domainId.value) {
      preserveInputNextDomainSwitch.value = true
      domainId.value = payload.domainId
      await new Promise((r) => setTimeout(r, 400))
    }

    if (!isCozeDomain.value) {
      showToast('当前智能体不支持工作流')
      return
    }

    const workflow = await resolveHostWorkflow(payload)
    if (!workflow) {
      showToast('未找到对应工作流，请检查 workflowId / workflowName')
      return
    }

    const files = (payload.files ?? []).map((f) => ({
      fileId: f.fileId,
      name: f.name,
    }))
    const input = payload.input?.trim() ?? ''

    if (payload.newSession !== false) {
      newConversation()
    }

    if (payload.prefillOnly) {
      if (files.length) attachments.setPreuploadedFiles(files)
      if (input) inputValue.value = input
      cozeChat.queueWorkflow(workflow)
      return
    }

    await cozeChat.runWorkflowDirect(workflow, input || '请对本案五书进行核稿', files)
  }

  return {
    session,
    inputValue,
    sending,
    startPage,
    hasHistory,
    greetText,
    description,
    introPrompt,
    simplePrompt,
    placeholder,
    newConversation,
    loadSessionById,
    applySession,
    onSubmit,
    markPreserveInputOnNextDomainSwitch,
    stopGenerating,
    retryAssistant,
    regenerateAssistant,
    startEditUserMessage,
    cancelEditUserMessage,
    editUserMessage,
    copyMessage,
    setMessageFeedback,
    canRegenerateAssistant,
    canEditUserMessage,
    isEditing,
    editingUserId,
    toast,
    showToast,
    composerFocusNonce,
    providerBanners: cozeChat.banners,
    clearProviderState: cozeChat.clearState,
    runCozeWorkflow,
    handleHostRunWorkflow,
  }
}
