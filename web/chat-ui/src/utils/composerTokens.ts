/** 发送前去掉输入框中的 @智能体 标记（领域已由 domainId 决定）。 */
export function stripMentionTokens(message: string): string {
  let s = message.trim()
  // 支持显示名含空格、括号等：重复剥离行首 @… 段（以空格结尾）
  while (/^@[^\n]+?\s+/u.test(s)) {
    s = s.replace(/^@[^\n]+?\s+/u, '').trimStart()
  }
  return s.replace(/\s+@[^\n]+?\s+/gu, ' ').trim()
}

/** 去掉 /coze 等斜杠命令残留。 */
export function stripSlashCommandTokens(message: string): string {
  return message
    .replace(/^\/coze(?:\s+\S+)?\s*/i, '')
    .replace(/^\/\s*/, '')
    .trim()
}

/** 去掉展示用附件后缀（📎 文件名），API 用 attachments 字段传递。 */
export function stripAttachmentNote(content: string): string {
  return content.replace(/\n\n📎 [^\n]+$/u, '').trim()
}

/** 从旧版正文末尾解析附件名（兼容无 attachments 字段的历史消息）。 */
export function parseAttachmentNamesFromContent(content: string): string[] {
  const m = content.match(/\n\n📎 ([^\n]+)$/u)
  if (!m?.[1]) return []
  return m[1]
    .split('、')
    .map((s) => s.trim())
    .filter(Boolean)
}

export function normalizeOutgoingMessage(message: string): string {
  return stripSlashCommandTokens(stripMentionTokens(message))
}

/** 发往网关的用户消息正文（无 @、无 /coze、无 📎 行）。 */
export function toApiUserContent(displayContent: string): string {
  return normalizeOutgoingMessage(stripAttachmentNote(displayContent))
}
