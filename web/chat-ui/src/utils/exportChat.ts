import type { ChatMessage, ChatSession } from '../types'
import { renderMarkdownHtml } from '../ui/markdown/render'
import { stripAttachmentNote } from './composerTokens'

function escapeHtml(text: string) {
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
}

function safeFilename(name: string) {
  return name.replace(/[<>:"/\\|?*]/g, '_').slice(0, 60) || 'chat'
}

function formatTime(ts = Date.now()) {
  const d = new Date(ts)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}${pad(d.getMonth() + 1)}${pad(d.getDate())}_${pad(d.getHours())}${pad(d.getMinutes())}`
}

export function sessionToMarkdown(
  session: ChatSession,
  meta: { title: string; domainName: string }
): string {
  const lines: string[] = [
    `# ${meta.title}`,
    '',
    `- 领域：${meta.domainName}`,
    `- 会话 ID：${session.id}`,
    `- 导出时间：${new Date().toLocaleString()}`,
    '',
    '---',
    '',
  ]

  for (const m of session.messages) {
    if (m.loading) continue
    const role = m.role === 'user' ? '用户' : '助手'
    lines.push(`## ${role}`, '')
    if (m.thinking?.trim()) {
      lines.push('<details><summary>思考过程</summary>', '', m.thinking.trim(), '', '</details>', '')
    }
    const body =
      m.role === 'user' ? stripAttachmentNote(m.content) : m.content
    lines.push(body.trim() || '（空）', '')
    if (m.citations?.length) {
      lines.push('### 参考来源', '')
      m.citations.forEach((c, i) => {
        lines.push(`${i + 1}. **${c.title}**${c.url ? ` (${c.url})` : ''}`)
        if (c.snippet) lines.push(`   ${c.snippet}`)
      })
      lines.push('')
    }
    if (m.feedback) {
      lines.push(`> 反馈：${m.feedback === 'up' ? '有用' : '无用'}`, '')
    }
    lines.push('---', '')
  }

  return lines.join('\n')
}

export function sessionToJson(
  session: ChatSession,
  meta: { title: string; domainId: string; domainName: string }
): string {
  return JSON.stringify(
    {
      exportedAt: new Date().toISOString(),
      title: meta.title,
      domainId: meta.domainId,
      domainName: meta.domainName,
      session,
    },
    null,
    2
  )
}

export function downloadTextFile(filename: string, content: string, mime: string) {
  const blob = new Blob([content], { type: `${mime};charset=utf-8` })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  a.rel = 'noopener'
  document.body.appendChild(a)
  a.click()
  document.body.removeChild(a)
  window.setTimeout(() => URL.revokeObjectURL(url), 0)
}

const PRINT_STYLES = `
  * { box-sizing: border-box; }
  body {
    font-family: "Segoe UI", "Microsoft YaHei", sans-serif;
    font-size: 13px;
    line-height: 1.55;
    color: #252b3a;
    margin: 24px 32px;
    max-width: 820px;
  }
  h1 { font-size: 20px; margin: 0 0 8px; }
  .meta { color: #71757f; font-size: 12px; margin-bottom: 20px; }
  .msg { margin: 16px 0; padding: 12px 14px; border-radius: 8px; page-break-inside: avoid; }
  .msg--user { background: #eef2ff; border: 1px solid #c7d2fe; }
  .msg--assistant { background: #f8fafc; border: 1px solid #e2e8f0; }
  .msg--error { background: #fef2f2; border: 1px solid #fecaca; }
  .msg h2 { font-size: 14px; margin: 0 0 8px; color: #5e7ce0; }
  .msg--user h2 { color: #4338ca; }
  .msg--error h2 { color: #dc2626; }
  .thinking {
    margin-bottom: 10px;
    padding: 8px 10px;
    background: #f1f5f9;
    border-radius: 6px;
    font-size: 12px;
    color: #475569;
    white-space: pre-wrap;
  }
  .user-text { margin: 0; white-space: pre-wrap; word-break: break-word; }
  .citations { margin-top: 10px; font-size: 12px; color: #475569; }
  .citations ol { margin: 6px 0 0; padding-left: 1.2em; }
  .feedback { margin-top: 8px; font-size: 11px; color: #71757f; }
  pre, code { font-family: Consolas, "Courier New", monospace; font-size: 12px; }
  pre { overflow-x: auto; padding: 10px; background: #1e293b; color: #e2e8f0; border-radius: 6px; }
  @media print {
    body { margin: 12mm 15mm; }
    .no-print { display: none; }
  }
`

function renderMessageBlock(m: ChatMessage): string {
  const roleLabel = m.role === 'user' ? '用户' : '助手'
  const roleClass =
    m.role === 'user' ? 'msg--user' : m.error ? 'msg--error' : 'msg--assistant'

  const parts: string[] = [`<section class="msg ${roleClass}"><h2>${roleLabel}</h2>`]

  if (m.thinking?.trim()) {
    parts.push(`<div class="thinking"><strong>思考过程</strong><br>${escapeHtml(m.thinking.trim())}</div>`)
  }

  if (m.role === 'user') {
    const text = stripAttachmentNote(m.content).trim() || '（空）'
    parts.push(`<pre class="user-text">${escapeHtml(text)}</pre>`)
  } else if (m.error) {
    const raw = m.content.trim()
    const err = raw.startsWith('[错误]') ? raw.slice(4).trim() : raw || '请求失败'
    parts.push(`<p>${escapeHtml(err)}</p>`)
  } else {
    const html = renderMarkdownHtml(m.content.trim() || '（空）')
    parts.push(`<div class="md-body">${html}</div>`)
  }

  if (m.citations?.length) {
    const items = m.citations
      .map((c, i) => {
        const title = escapeHtml(c.title)
        const url = c.url ? ` <a href="${escapeHtml(c.url)}">${escapeHtml(c.url)}</a>` : ''
        const snip = c.snippet ? `<br><span>${escapeHtml(c.snippet)}</span>` : ''
        return `<li><strong>${i + 1}. ${title}</strong>${url}${snip}</li>`
      })
      .join('')
    parts.push(`<div class="citations"><strong>参考来源</strong><ol>${items}</ol></div>`)
  }

  if (m.feedback) {
    const label = m.feedback === 'up' ? '有用' : '无用'
    parts.push(`<p class="feedback">反馈：${label}</p>`)
  }

  parts.push('</section>')
  return parts.join('')
}

/** 生成可打印 HTML（浏览器「另存为 PDF」） */
export function buildSessionPrintHtml(
  session: ChatSession,
  meta: { title: string; domainName: string }
): string {
  const blocks = session.messages
    .filter((m) => !m.loading)
    .map(renderMessageBlock)
    .join('')

  return `<!DOCTYPE html>
<html lang="zh-CN">
<head>
  <meta charset="utf-8" />
  <title>${escapeHtml(meta.title)}</title>
  <style>${PRINT_STYLES}</style>
</head>
<body>
  <h1>${escapeHtml(meta.title)}</h1>
  <p class="meta">
    领域：${escapeHtml(meta.domainName)} · 会话 ID：${escapeHtml(session.id)} ·
    导出：${escapeHtml(new Date().toLocaleString())}
  </p>
  ${blocks}
  <p class="no-print meta">在打印对话框中选择「另存为 PDF」或「Microsoft Print to PDF」。</p>
</body>
</html>`
}

/**
 * 通过系统打印对话框导出 PDF（零额外依赖，中文可靠）。
 * 需允许弹出窗口；WebView2/CefSharp 需启用脚本与弹窗。
 */
export function exportSessionPdf(
  session: ChatSession,
  meta: { title: string; domainName: string }
): void {
  const html = buildSessionPrintHtml(session, meta)
  const win = window.open('', '_blank', 'noopener,noreferrer')
  if (!win) {
    throw new Error('无法打开打印窗口，请允许本站弹出窗口后重试')
  }
  win.document.open()
  win.document.write(html)
  win.document.close()
  win.focus()
  const print = () => {
    win.print()
  }
  if (win.document.readyState === 'complete') {
    window.setTimeout(print, 250)
  } else {
    win.addEventListener('load', () => window.setTimeout(print, 250), { once: true })
  }
}

export function exportSessionMarkdown(
  session: ChatSession,
  meta: { title: string; domainName: string }
) {
  const md = sessionToMarkdown(session, meta)
  downloadTextFile(`${safeFilename(meta.title)}_${formatTime()}.md`, md, 'text/markdown')
}

export function exportSessionJson(
  session: ChatSession,
  meta: { title: string; domainId: string; domainName: string }
) {
  const json = sessionToJson(session, meta)
  downloadTextFile(`${safeFilename(meta.title)}_${formatTime()}.json`, json, 'application/json')
}
