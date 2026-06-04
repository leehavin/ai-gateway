/** 流式输出时补全未闭合的 ```，避免 Markdown 解析抖动 */
export function prepareStreamingMarkdown(source: string, streaming: boolean): string {
  if (!streaming || !source) return source
  const fences = source.match(/```/g)
  if (fences && fences.length % 2 === 1) {
    return `${source}\n\`\`\``
  }
  return source
}
