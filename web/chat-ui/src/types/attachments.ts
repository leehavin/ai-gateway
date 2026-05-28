export interface UploadedFileMeta {
  fileId: string
  name: string
  contentType: string
  size: number
  url: string
  textPreview?: string
}

export interface ChatAttachmentRef {
  fileId: string
  name: string
}
