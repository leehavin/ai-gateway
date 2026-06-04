import MarkdownIt from 'markdown-it'
import DOMPurify from 'dompurify'
import { highlightCodeBlock } from './highlight'
import { prepareStreamingMarkdown } from './prepareStream'

const md = new MarkdownIt({
  html: false,
  linkify: true,
  breaks: true,
  highlight(code: string, lang: string) {
    return highlightCodeBlock(code, lang)
  },
})

export interface RenderMarkdownOptions {
  /** 流式生成中：补全未闭合代码块 */
  streaming?: boolean
}

/** 助手消息 Markdown → 安全 HTML */
export function renderMarkdownHtml(source: string, options?: RenderMarkdownOptions): string {
  const prepared = prepareStreamingMarkdown(source || '', !!options?.streaming)
  const raw = md.render(prepared)
  return DOMPurify.sanitize(raw, {
    USE_PROFILES: { html: true },
    ADD_ATTR: ['target', 'rel', 'class'],
  })
}
