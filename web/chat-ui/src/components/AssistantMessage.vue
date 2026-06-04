<script setup lang="ts">
import { computed } from 'vue'
import { BRAND } from '../constants/brand'
import { DcCitationList, DcMessageBubble, DcThinkingBlock } from '../ui'
import type { ChatCitation } from '../types'
import MessageActions, { type MessageAction } from './MessageActions.vue'
import MessageFeedback from './MessageFeedback.vue'

const props = defineProps<{
  content: string
  thinking?: string
  citations?: ChatCitation[]
  loading?: boolean
  error?: boolean
  feedback?: 'up' | 'down'
  canRegenerate?: boolean
  showActions?: boolean
}>()

const emit = defineEmits<{
  regenerate: []
  copy: []
  quote: []
  feedback: [rating: 'up' | 'down']
}>()

const displayText = computed(() => {
  if (props.loading && !props.content) return '正在思考…'
  return props.content
})

const errorText = computed(() => {
  if (!props.error) return ''
  const raw = props.content.trim()
  if (raw.startsWith('[错误]')) return raw.slice(4).trim()
  return raw || '请求失败'
})

/** 成功回复：悬停显示复制 / 引用 / 重新生成 */
const hoverActions = computed((): MessageAction[] => {
  if (props.error || props.loading || !props.showActions) return []
  if (!props.content.trim() && !props.thinking?.trim()) return []
  const list: MessageAction[] = ['copy', 'quote']
  if (props.canRegenerate) list.push('regenerate')
  return list
})

const showHoverToolbar = computed(() => hoverActions.value.length > 0)

const showThinking = computed(
  () => !!(props.thinking?.trim() || (props.loading && !props.content))
)
</script>

<template>
  <div
    :class="['message-row', 'message-row--assistant', error && 'message-row--error']"
  >
    <div class="assistant-body">
      <DcThinkingBlock
        v-if="showThinking && !error"
        :content="thinking ?? ''"
        :streaming="loading && !content"
      />
      <DcMessageBubble
        v-if="!error"
        :content="displayText"
        :loading="loading && !thinking"
        markdown
        :avatar-config="{ imgSrc: BRAND.logoIcon }"
      />
      <div v-else class="error-bubble">
        <div class="error-bubble-head">
          <img class="error-avatar" :src="BRAND.logoIcon" alt="" width="28" height="28" />
          <span class="error-title">回复失败</span>
        </div>
        <p class="error-body">{{ errorText }}</p>
        <button
          v-if="canRegenerate"
          type="button"
          class="error-retry-btn"
          :disabled="loading"
          @click="emit('regenerate')"
        >
          <i class="icon-refresh"></i>
          重新生成
        </button>
      </div>
      <DcCitationList v-if="!error && citations?.length" :items="citations" />
    </div>
    <MessageActions
      v-if="showHoverToolbar"
      :actions="hoverActions"
      @quote="emit('quote')"
      :disabled="loading"
      @copy="emit('copy')"
      @regenerate="emit('regenerate')"
    />
    <MessageFeedback
      v-if="showHoverToolbar"
      :feedback="feedback"
      :disabled="loading"
      @rate="(r) => emit('feedback', r)"
    />
  </div>
</template>

<style scoped>
.message-row {
  display: flex;
  align-items: flex-start;
  gap: 8px;
  width: 100%;
}

.message-row--assistant {
  flex-wrap: wrap;
}

.assistant-body {
  flex: 1;
  min-width: 0;
  max-width: min(88%, 720px);
}

.message-row--error .error-bubble {
  max-width: 100%;
}

.error-bubble {
  padding: 12px 14px;
  border-radius: var(--dc-radius-md);
  background: var(--dc-error-bg);
  border: 1px solid rgba(220, 38, 38, 0.2);
}

.error-bubble-head {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 8px;
}

.error-avatar {
  width: 28px;
  height: 28px;
  object-fit: contain;
}

.error-title {
  font-size: 13px;
  font-weight: 600;
  color: var(--dc-error);
}

.error-body {
  margin: 0;
  font-size: 13px;
  line-height: 1.55;
  color: #7f1d1d;
  white-space: pre-wrap;
  word-break: break-word;
}

.error-retry-btn {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  margin-top: 10px;
  padding: 6px 14px;
  border: 1px solid rgba(220, 38, 38, 0.35);
  border-radius: var(--dc-radius-pill);
  background: #fff;
  color: var(--dc-error);
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
}

.error-retry-btn:hover:not(:disabled) {
  background: #fff5f5;
}

.error-retry-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.error-retry-btn i {
  font-size: 14px;
}
</style>
