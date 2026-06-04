import hljs from 'highlight.js/lib/core'
import bash from 'highlight.js/lib/languages/bash'
import csharp from 'highlight.js/lib/languages/csharp'
import css from 'highlight.js/lib/languages/css'
import javascript from 'highlight.js/lib/languages/javascript'
import json from 'highlight.js/lib/languages/json'
import markdown from 'highlight.js/lib/languages/markdown'
import python from 'highlight.js/lib/languages/python'
import sql from 'highlight.js/lib/languages/sql'
import typescript from 'highlight.js/lib/languages/typescript'
import xml from 'highlight.js/lib/languages/xml'
import yaml from 'highlight.js/lib/languages/yaml'

const LANG_ALIASES: Record<string, string> = {
  ts: 'typescript',
  js: 'javascript',
  cs: 'csharp',
  'c#': 'csharp',
  sh: 'bash',
  shell: 'bash',
  yml: 'yaml',
  html: 'xml',
  vue: 'xml',
  md: 'markdown',
}

for (const [name, mod] of [
  ['bash', bash],
  ['csharp', csharp],
  ['css', css],
  ['javascript', javascript],
  ['json', json],
  ['markdown', markdown],
  ['python', python],
  ['sql', sql],
  ['typescript', typescript],
  ['xml', xml],
  ['yaml', yaml],
] as const) {
  hljs.registerLanguage(name, mod)
}

function resolveLanguage(lang: string): string | undefined {
  const key = lang.trim().toLowerCase()
  const normalized = LANG_ALIASES[key] ?? key
  return hljs.getLanguage(normalized) ? normalized : undefined
}

export function highlightCodeBlock(code: string, lang?: string): string {
  const trimmed = lang?.trim()
  if (trimmed) {
    const resolved = resolveLanguage(trimmed)
    if (resolved) {
      return hljs.highlight(code, { language: resolved }).value
    }
  }
  return hljs.highlightAuto(code).value
}
