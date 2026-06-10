import { computed, ref } from 'vue'
import { uploadFile } from '../api/files'
import type { ChatAttachmentRef, UploadedFileMeta } from '../types/attachments'
import {
  ATTACHMENT_ACCEPT,
  ATTACHMENT_MAX_COUNT,
  ATTACHMENT_MAX_SIZE_MB,
  type ChatFileItem,
} from '../ui'

function parseUploadResponse(raw: unknown): UploadedFileMeta | null {
  if (!raw || typeof raw !== 'object') return null
  const r = raw as UploadedFileMeta
  if (!r.fileId || !r.name) return null
  return r
}

const acceptSet = new Set(
  ATTACHMENT_ACCEPT.split(',').map((s) => s.trim().toLowerCase().replace(/^\./, ''))
)

function isAccepted(file: File): boolean {
  const ext = file.name.includes('.') ? file.name.split('.').pop()?.toLowerCase() : ''
  return !!ext && acceptSet.has(ext)
}

export function useAttachments() {
  const fileItems = ref<ChatFileItem[]>([])

  const readyAttachments = computed((): ChatAttachmentRef[] =>
    fileItems.value
      .filter((f) => f.status === 'success')
      .map((f) => {
        const meta = parseUploadResponse(f.response)
        return meta ? { fileId: meta.fileId, name: meta.name } : null
      })
      .filter((x): x is ChatAttachmentRef => x !== null)
  )

  const hasUploading = computed(() => fileItems.value.some((f) => f.status === 'uploading'))

  function clearAttachments() {
    fileItems.value = []
  }

  /** 宿主已通过 Gateway 上传的文件，直接挂到输入区（fileId 来自 /v1/files/upload 或 register）。 */
  function setPreuploadedFiles(refs: ChatAttachmentRef[]) {
    const next: ChatFileItem[] = refs.map((ref, i) => ({
      uid: Date.now() + i + Math.random(),
      name: ref.name,
      size: 0,
      status: 'success' as const,
      response: {
        fileId: ref.fileId,
        name: ref.name,
        contentType: 'application/octet-stream',
        size: 0,
        url: `/v1/files/${ref.fileId}`,
      },
    }))
    fileItems.value = next
  }

  function formatAttachmentNote(refs: ChatAttachmentRef[]): string {
    if (!refs.length) return ''
    return '\n\n📎 ' + refs.map((a) => a.name).join('、')
  }

  function getDropContainer(): HTMLElement {
    return dropZoneEl.value ?? document.body
  }

  const dropZoneEl = ref<HTMLElement | null>(null)
  const fileInputEl = ref<HTMLInputElement | null>(null)

  function openFilePicker() {
    fileInputEl.value?.click()
  }

  async function addFiles(files: File[]) {
    const maxBytes = ATTACHMENT_MAX_SIZE_MB * 1024 * 1024
    for (const file of files) {
      if (fileItems.value.length >= ATTACHMENT_MAX_COUNT) break
      if (!isAccepted(file)) continue
      if (file.size > maxBytes) continue

      const uid = Date.now() + Math.random()
      const item: ChatFileItem = {
        uid,
        name: file.name,
        size: file.size,
        status: 'uploading',
      }
      fileItems.value = [...fileItems.value, item]
      try {
        const meta = await uploadFile(file)
        fileItems.value = fileItems.value.map((f) =>
          f.uid === uid ? { ...f, status: 'success', response: meta } : f
        )
      } catch (err) {
        fileItems.value = fileItems.value.map((f) =>
          f.uid === uid
            ? {
                ...f,
                status: 'error',
                response: { message: err instanceof Error ? err.message : String(err) },
              }
            : f
        )
      }
    }
  }

  async function onHiddenFileChange(e: Event) {
    const input = e.target as HTMLInputElement
    const files = input.files
    if (!files?.length) return
    await addFiles(Array.from(files))
    input.value = ''
  }

  return {
    fileItems,
    readyAttachments,
    hasUploading,
    clearAttachments,
    setPreuploadedFiles,
    formatAttachmentNote,
    dropZoneEl,
    fileInputEl,
    openFilePicker,
    onHiddenFileChange,
    addFiles,
    getDropContainer,
  }
}
