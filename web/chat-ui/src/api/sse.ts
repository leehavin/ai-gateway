export interface SseDelta {
  text?: string
  done?: boolean
  error?: string
}

export function parseSseLine(line: string): SseDelta | null {
  const trimmed = line.trim()
  if (!trimmed.startsWith('data:')) return null
  const data = trimmed.slice(5).trim()
  if (data === '[DONE]') return { done: true }
  try {
    const j = JSON.parse(data) as Record<string, unknown>
    if (typeof j.error === 'string') return { error: j.error }
    if (typeof j.delta === 'string') return { text: j.delta }
    const choices = j.choices as Array<{ delta?: { content?: string } }> | undefined
    const fromChoice = choices?.[0]?.delta?.content
    if (typeof fromChoice === 'string') return { text: fromChoice }
    if (typeof j.content === 'string') return { text: j.content }
  } catch {
    /* ignore malformed chunk */
  }
  return null
}

export async function readSseStream(
  body: ReadableStream<Uint8Array> | null,
  onDelta: (text: string) => void,
  signal?: AbortSignal
): Promise<void> {
  if (!body) throw new Error('响应体为空')
  const reader = body.getReader()
  const decoder = new TextDecoder()
  let buf = ''
  while (true) {
    if (signal?.aborted) throw new DOMException('Aborted', 'AbortError')
    const { done, value } = await reader.read()
    if (done) break
    buf += decoder.decode(value, { stream: true })
    const lines = buf.split('\n')
    buf = lines.pop() ?? ''
    for (const line of lines) {
      const p = parseSseLine(line)
      if (!p) continue
      if (p.error) throw new Error(p.error)
      if (p.done) return
      if (p.text) onDelta(p.text)
    }
  }
}
