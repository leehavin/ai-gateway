import type { CozeWorkflowInputSpec, CozeWorkflowItem } from '../../types'

export type WorkflowInputKind = 'text' | 'file' | 'image' | 'audio'

export interface GroupedWorkflowInputs {
  requiredFiles: CozeWorkflowInputSpec[]
  optionalFiles: CozeWorkflowInputSpec[]
  textInputs: CozeWorkflowInputSpec[]
}

const ACCEPT_LABELS: Record<string, string> = {
  '.pdf': 'PDF',
  '.doc': 'Word',
  '.docx': 'Word',
  '.txt': 'TXT',
  '.md': 'MD',
  '.png': 'PNG',
  '.jpg': 'JPG',
  '.jpeg': 'JPEG',
  '.webp': 'WebP',
  '.gif': 'GIF',
  '.mp3': 'MP3',
  '.wav': 'WAV',
  '.m4a': 'M4A',
}

export function isAttachmentInput(spec: CozeWorkflowInputSpec): boolean {
  const t = spec.type.toLowerCase()
  return (
    t === 'image' ||
    t === 'file' ||
    t === 'audio' ||
    t === 'document' ||
    t === 'pdf' ||
    t === 'doc' ||
    t === 'txt' ||
    t.includes('file') ||
    t.includes('image') ||
    t.includes('doc') ||
    t.startsWith('array<image') ||
    t.startsWith('array<file') ||
    t.startsWith('array<audio') ||
    t.startsWith('array<doc')
  )
}

export function isTextInput(spec: CozeWorkflowInputSpec): boolean {
  const t = spec.type.toLowerCase()
  return t === 'string' || t === 'integer' || t === 'number' || t === 'boolean'
}

export function inputKind(spec: CozeWorkflowInputSpec): WorkflowInputKind {
  const t = spec.type.toLowerCase()
  if (t === 'image' || t.startsWith('array<image')) return 'image'
  if (t === 'audio' || t.startsWith('array<audio')) return 'audio'
  if (isAttachmentInput(spec)) return 'file'
  return 'text'
}

export function inputDisplayLabel(spec: CozeWorkflowInputSpec): string {
  if (spec.label?.trim()) {
    const label = spec.label.trim()
    if (label === '输入' && isTextInput(spec)) return '补充说明'
    return label
  }
  const name = spec.name.trim().toLowerCase()
  if (name === 'doc' || name === 'document' || name === 'pdf') return '文档'
  if (name === 'img' || name === 'image') return '图片'
  if (name === 'input' || name === 'query' || name === 'question') return '补充说明'
  if (name === 'txt' || name === 'text') return '文本文件'
  return spec.name
}

export function formatAcceptShort(accept?: string[]): string {
  if (!accept?.length) return ''
  const labels = accept.map((ext) => {
    const key = ext.startsWith('.') ? ext.toLowerCase() : `.${ext.toLowerCase()}`
    return ACCEPT_LABELS[key] ?? ext.replace(/^\./, '').toUpperCase()
  })
  const unique = [...new Set(labels)]
  if (unique.length <= 2) return unique.join('、')
  return `${unique[0]} 等 ${unique.length} 种`
}

export function groupWorkflowInputs(inputs?: CozeWorkflowInputSpec[]): GroupedWorkflowInputs {
  const list = inputs ?? []
  const files = list.filter(isAttachmentInput)
  const requiredFiles = files.filter((i) => i.required)
  const optionalFiles = files.filter((i) => !i.required)
  const textInputs = list.filter(isTextInput)
  return { requiredFiles, optionalFiles, textInputs }
}

/** 工作流是否必须上传附件（inputs 为空时回退 needsAttachment）。 */
export function countRequiredAttachments(wf: CozeWorkflowItem): number {
  const { requiredFiles } = groupWorkflowInputs(wf.inputs)
  if (requiredFiles.length > 0) return requiredFiles.length
  return wf.needsAttachment ? 1 : 0
}

export function requiredAttachmentLabels(wf: CozeWorkflowItem): string[] {
  const { requiredFiles } = groupWorkflowInputs(wf.inputs)
  if (requiredFiles.length > 0) return requiredFiles.map(inputDisplayLabel)
  return wf.needsAttachment ? ['文档'] : []
}

export function workflowCardSubtitle(wf: CozeWorkflowItem): string {
  if (wf.description?.trim()) return wf.description.trim()
  if (wf.inputSummary?.trim()) return wf.inputSummary.trim()
  return '按下方要求准备材料，完成后发送'
}

export function workflowMenuDesc(wf: CozeWorkflowItem): string {
  if (wf.inputSummary) return wf.inputSummary
  if (wf.description) return `工作流 · ${wf.description}`
  return `工作流 ${wf.workflowId}`
}

/** 输入框短 placeholder，不重复横幅里的参数清单。 */
export function workflowPlaceholderHint(wf: CozeWorkflowItem): string {
  const { requiredFiles, optionalFiles, textInputs } = groupWorkflowInputs(wf.inputs)

  if (requiredFiles.length > 0 && textInputs.length === 0 && optionalFiles.length === 0) {
    return '上传必填文档后点击发送'
  }
  if (requiredFiles.length > 0) {
    return '补充说明（可选），上传必填文档后发送'
  }
  if (textInputs.some((i) => i.required)) {
    const label = inputDisplayLabel(textInputs.find((i) => i.required)!)
    return `请输入${label}`
  }
  if (textInputs.length > 0) {
    return '补充说明（可选）'
  }
  if (optionalFiles.length > 0) {
    return '输入说明或上传附件后发送'
  }
  return `已选「${wf.displayName}」，完成后发送`
}

export function workflowToastMessage(wf: CozeWorkflowItem): string {
  const { requiredFiles } = groupWorkflowInputs(wf.inputs)
  if (requiredFiles.length > 0) {
    const names = requiredFiles.map(inputDisplayLabel).join('、')
    return `已选择「${wf.displayName}」，请先上传：${names}`
  }
  if (wf.inputSummary) {
    return `已选择「${wf.displayName}」`
  }
  return `已选择「${wf.displayName}」，完成后发送`
}

/** @deprecated 横幅改用结构化卡片，保留供无 inputs 时的兜底。 */
export function workflowPendingMessage(wf: CozeWorkflowItem): string {
  if (wf.description?.trim()) {
    return `已选「${wf.displayName}」· ${wf.description.trim()}`
  }
  if (wf.inputSummary?.trim()) {
    return `已选「${wf.displayName}」· ${wf.inputSummary.trim()}`
  }
  return `已选工作流：${wf.displayName}`
}
