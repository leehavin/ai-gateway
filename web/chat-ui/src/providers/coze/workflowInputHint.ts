import type { CozeWorkflowItem } from '../../types'

export function workflowMenuDesc(wf: CozeWorkflowItem): string {
  if (wf.inputSummary) return wf.inputSummary
  if (wf.description) return `工作流 · ${wf.description}`
  return `工作流 ${wf.workflowId}`
}

export function workflowPendingMessage(wf: CozeWorkflowItem): string {
  if (wf.inputHint) return wf.inputHint
  if (wf.inputSummary) return `已选「${wf.displayName}」：${wf.inputSummary}`
  return `已选工作流：${wf.displayName}，输入后发送`
}

export function workflowPlaceholderHint(wf: CozeWorkflowItem): string {
  return wf.inputHint ?? `已选「${wf.displayName}」，输入后发送执行`
}
