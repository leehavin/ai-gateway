import { computed, ref, type Ref } from 'vue'
import type { SseEvent } from '../../api/sse'
import { resumeCozeWorkflow, streamCozeWorkflow } from './api'
import type {
  ChatMessage,
  ChatSession,
  CozeWorkflowInterrupt,
  CozeWorkflowItem,
} from '../../types'
import type { ProviderChatBanner } from '../types'
import { normalizeOutgoingMessage } from '../../utils/composerTokens'
import { workflowPlaceholderHint, workflowToastMessage } from './workflowInputHint'

function uid() {
  return crypto.randomUUID?.() ?? `${Date.now()}-${Math.random().toString(16).slice(2)}`
}

export interface CozeChatDeps {
  enabled: Ref<boolean>
  domainId: Ref<string>
  session: Ref<ChatSession>
  inputValue: Ref<string>
  sending: Ref<boolean>
  composerFocusNonce: Ref<number>
  showToast: (msg: string) => void
  closeAsideIfNarrow: () => void
  refreshHistory: () => void
  setStartPage: (v: boolean) => void
  getAbortSignal: () => AbortSignal | undefined
  setAbortController: (c: AbortController | null) => void
}

/** Coze 专有对话逻辑：工作流执行 / 中断续跑。 */
export function useCozeChat(deps: CozeChatDeps) {
  const workflowInterrupt = ref<CozeWorkflowInterrupt | null>(null)
  const pendingWorkflow = ref<CozeWorkflowItem | null>(null)

  const placeholderHint = computed(() => {
    if (!deps.enabled.value) return null
    if (workflowInterrupt.value) {
      const title = workflowInterrupt.value.nodeTitle || '工作流'
      return `回答「${title}」后继续执行…`
    }
    if (pendingWorkflow.value) {
      return workflowPlaceholderHint(pendingWorkflow.value)
    }
    return null
  })

  const banners = computed((): ProviderChatBanner[] => {
    if (!deps.enabled.value) return []
    if (workflowInterrupt.value) {
      return [
        {
          kind: 'workflow-interrupt',
          nodeTitle: workflowInterrupt.value.nodeTitle,
          onCancel: clearState,
        },
      ]
    }
    if (pendingWorkflow.value) {
      return [
        {
          kind: 'workflow-pending',
          workflow: pendingWorkflow.value,
          onCancel: clearState,
        },
      ]
    }
    return []
  })

  function clearState() {
    workflowInterrupt.value = null
    pendingWorkflow.value = null
    deps.sending.value = false
  }

  function applyWorkflowSseToAssistant(assistant: ChatMessage, ev: SseEvent) {
    assistant.loading = false
    assistant.error = false
    if (ev.text) assistant.content += ev.text
    if (ev.workflowInterrupt) workflowInterrupt.value = ev.workflowInterrupt
    if (ev.workflowDone?.debugUrl && !assistant.content.includes(ev.workflowDone.debugUrl)) {
      assistant.content += `\n\n[调试](${ev.workflowDone.debugUrl})`
    }
  }

  async function runWorkflowAssistantStream(
    assistant: ChatMessage,
    workflow: CozeWorkflowItem,
    input: string,
    resume?: CozeWorkflowInterrupt
  ) {
    const abortCtrl = new AbortController()
    deps.setAbortController(abortCtrl)
    deps.sending.value = true
    try {
      const onEvent = (ev: SseEvent) => applyWorkflowSseToAssistant(assistant, ev)
      if (resume) {
        await resumeCozeWorkflow(
          {
            domain: deps.domainId.value,
            workflowId: resume.workflowId,
            eventId: resume.eventId,
            interruptType: resume.interruptType,
            resumeData: input,
          },
          onEvent,
          abortCtrl.signal
        )
      } else {
        await streamCozeWorkflow(
          {
            domain: deps.domainId.value,
            workflowId: workflow.workflowId,
            input: input.trim() || undefined,
          },
          onEvent,
          abortCtrl.signal
        )
      }
    } catch (e) {
      assistant.loading = false
      if (e instanceof DOMException && e.name === 'AbortError') return
      assistant.error = true
      assistant.content =
        assistant.content || `[错误] ${e instanceof Error ? e.message : String(e)}`
      workflowInterrupt.value = null
    } finally {
      deps.sending.value = false
      assistant.loading = false
      deps.setAbortController(null)
      if (!workflowInterrupt.value) deps.refreshHistory()
    }
  }

  async function executeWorkflow(workflow: CozeWorkflowItem, userInput: string) {
    workflowInterrupt.value = null
    pendingWorkflow.value = null
    deps.setStartPage(false)
    deps.closeAsideIfNarrow()

    const trimmed = userInput.trim()
    const displayContent = trimmed
      ? `▶ ${workflow.displayName}：${trimmed}`
      : `▶ ${workflow.displayName}`

    deps.session.value.messages.push({
      id: uid(),
      role: 'user',
      content: displayContent,
    })

    const assistant: ChatMessage = {
      id: uid(),
      role: 'assistant',
      content: '',
      loading: true,
    }
    deps.session.value.messages.push(assistant)
    await runWorkflowAssistantStream(assistant, workflow, trimmed)
  }

  function queueWorkflow(workflow: CozeWorkflowItem) {
    const input = deps.inputValue.value.trim()
    if (input) {
      deps.inputValue.value = ''
      void executeWorkflow(workflow, input)
      return
    }
    pendingWorkflow.value = workflow
    deps.showToast(workflowToastMessage(workflow))
    deps.composerFocusNonce.value++
  }

  function queueWorkflowById(workflowId: string, workflows: CozeWorkflowItem[]) {
    const wf = workflows.find((w) => w.workflowId === workflowId)
    if (!wf) {
      deps.showToast('工作流不可用，请刷新 /coze 列表')
      return
    }
    queueWorkflow(wf)
  }

  async function tryHandleSubmit(raw: string): Promise<boolean> {
    if (!deps.enabled.value) return false

    if (workflowInterrupt.value) {
      const msg = normalizeOutgoingMessage(raw)
      if (!msg) return true
      deps.inputValue.value = ''
      const interrupt = workflowInterrupt.value
      workflowInterrupt.value = null

      deps.session.value.messages.push({ id: uid(), role: 'user', content: msg })
      const assistant: ChatMessage = {
        id: uid(),
        role: 'assistant',
        content: '',
        loading: true,
      }
      deps.session.value.messages.push(assistant)

      const wf: CozeWorkflowItem = {
        workflowId: interrupt.workflowId,
        displayName: interrupt.nodeTitle || '工作流',
        inputParameter: 'BOT_USER_INPUT',
      }
      await runWorkflowAssistantStream(assistant, wf, msg, interrupt)
      return true
    }

    const msg = normalizeOutgoingMessage(raw)
    if (pendingWorkflow.value) {
      if (!msg) return true
      const wf = pendingWorkflow.value
      deps.inputValue.value = ''
      await executeWorkflow(wf, msg)
      return true
    }

    return false
  }

  return {
    workflowInterrupt,
    pendingWorkflow,
    placeholderHint,
    banners,
    clearState,
    queueWorkflow,
    queueWorkflowById,
    tryHandleSubmit,
  }
}
