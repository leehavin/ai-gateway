export interface ChatGenerationParameters {
  temperature: number
  topP: number
  maxTokens: number
}

export const DEFAULT_CHAT_PARAMS: ChatGenerationParameters = {
  temperature: 0.7,
  topP: 1,
  maxTokens: 2048,
}

function clamp(n: number, min: number, max: number) {
  return Math.min(max, Math.max(min, n))
}

/** 滑块/数字框输入归一化，避免 NaN 进入 stream 请求 */
export function normalizeChatParams(
  partial: Partial<ChatGenerationParameters>
): ChatGenerationParameters {
  const t = Number(partial.temperature)
  const p = Number(partial.topP)
  const m = Number(partial.maxTokens)
  return {
    temperature: clamp(Number.isFinite(t) ? t : DEFAULT_CHAT_PARAMS.temperature, 0, 2),
    topP: clamp(Number.isFinite(p) ? p : DEFAULT_CHAT_PARAMS.topP, 0, 1),
    maxTokens: Math.round(
      clamp(Number.isFinite(m) ? m : DEFAULT_CHAT_PARAMS.maxTokens, 256, 8192)
    ),
  }
}
