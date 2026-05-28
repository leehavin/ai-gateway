import { computed, ref } from 'vue'
import type { FileItem } from '@matechat/core/Attachment/attachment-types'
import type { ChatAttachmentRef, UploadedFileMeta } from '../types/attachments'

function parseUploadResponse(raw: unknown): UploadedFileMeta | null {
  if (!raw || typeof raw !== 'object') return null
  const r = raw as UploadedFileMeta
  if (!r.fileId || !r.name) return null
  return r
}

export function useAttachments() {
  const fileItems = ref<FileItem[]>([])

  const uploadOptions = computed(() => ({
    uri: '/v1/files/upload',
    method: 'POST' as const,
    authToken: `Bearer ${import.meta.env.VITE_DATACHAT_TOKEN || 'demo-token'}`,
    authTokenHeader: 'Authorization',
    responseType: 'json' as const,
  }))

  const readyAttachments = computed((): ChatAttachmentRef[] =>
    fileItems.value
      .filter((f: FileItem) => f.status === 'success')
      .map((f: FileItem) => {
        const meta = parseUploadResponse(f.response)
        return meta ? { fileId: meta.fileId, name: meta.name } : null
      })
      .filter((x: ChatAttachmentRef | null): x is ChatAttachmentRef => x !== null)
  )

  const hasUploading = computed(() =>
    fileItems.value.some((f: FileItem) => f.status === 'uploading')
  )

  function clearAttachments() {
    fileItems.value = []
  }

  function formatAttachmentNote(refs: ChatAttachmentRef[]): string {
    if (!refs.length) return ''
    return '\n\n📎 ' + refs.map((a) => a.name).join('、')
  }

  function getDropContainer(): HTMLElement {
    return dropZoneEl.value ?? document.body
  }

  const dropZoneEl = ref<HTMLElement | null>(null)

  return {
    fileItems,
    uploadOptions,
    readyAttachments,
    hasUploading,
    clearAttachments,
    formatAttachmentNote,
    dropZoneEl,
    getDropContainer,
  }
}
