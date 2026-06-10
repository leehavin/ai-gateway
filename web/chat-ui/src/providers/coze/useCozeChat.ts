import { computed, ref, type Ref } from 'vue'
import type { SseEvent } from '../../api/sse'
import { resumeCozeWorkflow, streamCozeWorkflow } from './api'
import type { ChatAttachmentRef } from '../../types/attachments'
import type {
  ChatMessage,
  ChatSession,
  CozeWorkflowInterrupt,
  CozeWorkflowItem,
} from '../../types'
import type { ProviderChatBanner } from '../types'
import { normalizeOutgoingMessage } from '../../utils/composerTokens'
import {
  countRequiredAttachments,
  requiredAttachmentLabels,
  workflowPlaceholderHint,
  workflowToastMessage,
} from './workflowInputHint'

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
  getReadyAttachments: () => ChatAttachmentRef[]
  formatAttachmentNote: (refs: ChatAttachmentRef[]) => string
  clearAttachments: () => void
  hasUploading: () => boolean
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
    files: ChatAttachmentRef[] = [],
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
            attachments: files.length ? files : undefined,
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

  async function executeWorkflow(
    workflow: CozeWorkflowItem,
    userInput: string,
    files: ChatAttachmentRef[] = []
  ) {
    workflowInterrupt.value = null
    pendingWorkflow.value = null
    deps.setStartPage(false)
    deps.closeAsideIfNarrow()

    const trimmed = userInput.trim()
    const note = deps.formatAttachmentNote(files)
    const displayContent = trimmed
      ? `▶ ${workflow.displayName}：${trimmed}${note}`
      : `▶ ${workflow.displayName}${note}`

    deps.session.value.messages.push({
      id: uid(),
      role: 'user',
      content: displayContent,
      attachments: files.length ? [...files] : undefined,
    })

    const assistant: ChatMessage = {
      id: uid(),
      role: 'assistant',
      content: '',
      loading: true,
    }
    deps.session.value.messages.push(assistant)
    await runWorkflowAssistantStream(assistant, workflow, trimmed, files)
  }

  function queueWorkflow(workflow: CozeWorkflowItem) {
    const input = deps.inputValue.value.trim()
    const files = deps.getReadyAttachments()
    const requiredCount = countRequiredAttachments(workflow)

    // 需要附件时：除非附件已齐且用户已输入说明，否则进入待输入状态（避免有文字无附件就执行）
    if (requiredCount > 0) {
      pendingWorkflow.value = workflow
      if (files.length >= requiredCount && input) {
        deps.inputValue.value = ''
        deps.clearAttachments()
        void executeWorkflow(workflow, input, files)
        return
      }
      deps.showToast(workflowToastMessage(workflow))
      deps.composerFocusNonce.value++
      return
    }

    if (input) {
      deps.inputValue.value = ''
      void executeWorkflow(workflow, input, files)
      return
    }

    pendingWorkflow.value = workflow
    deps.showToast(workflowToastMessage(workflow))
    deps.composerFocusNonce.value++
  }

  function findWorkflow(
    workflows: CozeWorkflowItem[],
    workflowId?: string,
    workflowName?: string
  ): CozeWorkflowItem | null {
    if (workflowId) {
      const hit = workflows.find((w) => w.workflowId === workflowId)
      if (hit) return hit
    }
    if (workflowName) {
      const key = workflowName.trim().toLowerCase()
      const hit = workflows.find(
        (w) =>
          w.workflowId === workflowName ||
          w.displayName.trim().toLowerCase() === key
      )
      if (hit) return hit
    }
    return null
  }

  function syntheticWorkflow(workflowId: string, workflowName?: string): CozeWorkflowItem {
    return {
      workflowId,
      displayName: workflowName?.trim() || '工作流',
      inputParameter: 'BOT_USER_INPUT',
    }
  }

  function queueWorkflowById(workflowId: string, workflows: CozeWorkflowItem[]) {
    const wf = findWorkflow(workflows, workflowId)
    if (!wf) {
      deps.showToast('工作流不可用，请刷新 /coze 列表')
      return
    }
    queueWorkflow(wf)
  }

  async function runWorkflowDirect(
    workflow: CozeWorkflowItem,
    userInput: string,
    files: ChatAttachmentRef[] = []
  ) {
    deps.inputValue.value = ''
    await executeWorkflow(workflow, userInput, files)
  }

  async function tryHandleSubmit(raw: string): Promise<boolean> {
    if (!deps.enabled.value) return false
    if (deps.hasUploading()) return true

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
      await runWorkflowAssistantStream(assistant, wf, msg, [], interrupt)
      return true
    }

    const msg = normalizeOutgoingMessage(raw)
    const files = deps.getReadyAttachments()
    if (pendingWorkflow.value) {
      const wf = pendingWorkflow.value
      const requiredCount = countRequiredAttachments(wf)
      if (!msg && !files.length) return true
      if (requiredCount > 0 && files.length < requiredCount) {
        deps.showToast(`请先上传：${requiredAttachmentLabels(wf).join('、')}`)
        return true
      }

      deps.inputValue.value = ''
      deps.clearAttachments()
      await executeWorkflow(wf, msg, files)
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
    findWorkflow,
    syntheticWorkflow,
    runWorkflowDirect,
    tryHandleSubmit,
  }
}
