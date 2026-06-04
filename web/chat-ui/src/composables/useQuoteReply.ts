import { ref, type Ref } from 'vue'

export function useQuoteReply(inputValue: Ref<string>, onFocus: () => void) {
  const quotePreview = ref('')

  function applyQuote(text: string) {
    const trimmed = text.trim().slice(0, 500)
    if (!trimmed) return
    const block = trimmed.split('\n').map((l) => `> ${l}`).join('\n')
    const prefix = inputValue.value.trim() ? `${inputValue.value.trim()}\n\n` : ''
    inputValue.value = `${prefix}${block}\n\n`
    quotePreview.value = trimmed.length > 80 ? `${trimmed.slice(0, 80)}…` : trimmed
    onFocus()
  }

  function clearQuotePreview() {
    quotePreview.value = ''
  }

  return { quotePreview, applyQuote, clearQuotePreview }
}
