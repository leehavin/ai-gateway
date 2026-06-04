import { getAuthToken } from '../bridge/hostAuth'
import type { UploadedFileMeta } from '../types/attachments'

const gatewayUrl = () => {
  const env = import.meta.env.VITE_GATEWAY_URL
  const base = env === undefined || env === '' ? '' : env
  return base.replace(/\/$/, '')
}

export async function uploadFile(file: File): Promise<UploadedFileMeta> {
  const form = new FormData()
  form.append('file', file, file.name)
  const t = getAuthToken()
  const res = await fetch(`${gatewayUrl()}/v1/files/upload`, {
    method: 'POST',
    headers: t ? { Authorization: `Bearer ${t}` } : {},
    body: form,
  })
  if (!res.ok) {
    const text = await res.text()
    let message = `上传失败 ${res.status}`
    try {
      const j = JSON.parse(text) as { message?: string }
      if (j.message) message = j.message
    } catch {
      if (text) message += `: ${text.slice(0, 120)}`
    }
    throw new Error(message)
  }
  return res.json() as Promise<UploadedFileMeta>
}
