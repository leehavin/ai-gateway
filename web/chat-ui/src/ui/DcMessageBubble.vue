<script setup lang="ts">
import { computed } from 'vue'
import DcMarkdown from './DcMarkdown.vue'
import DcMessageAttachments from './DcMessageAttachments.vue'
import type { AvatarConfig, BubbleAlign } from './types'

const props = withDefaults(
  defineProps<{
    content: string
    align?: BubbleAlign
    loading?: boolean
    markdown?: boolean
    avatarConfig?: AvatarConfig
    /** 用户消息中的附件（与正文分开展示） */
    attachments?: { name: string }[]
  }>(),
  {
    align: 'left',
    loading: false,
    markdown: false,
    attachments: () => [],
  }
)

const isRight = computed(() => props.align === 'right')

const avatarSrc = computed(() => props.avatarConfig?.imgSrc)
const avatarLetter = computed(() => {
  const n = props.avatarConfig?.name?.trim()
  return n ? n.charAt(0).toUpperCase() : '?'
})

const displayText = computed(() => props.content.trim())
const hasAttachments = computed(() => (props.attachments?.length ?? 0) > 0)
const showBody = computed(
  () => props.loading || !!displayText.value || hasAttachments.value
)
</script>

<template>
  <div :class="['dc-bubble-row', isRight && 'dc-bubble-row--right']">
    <div v-if="!isRight" class="dc-bubble-avatar">
      <img v-if="avatarSrc" :src="avatarSrc" alt="" class="dc-bubble-avatar-img" />
      <span v-else class="dc-bubble-avatar-fallback">{{ avatarLetter }}</span>
    </div>
    <div :class="['dc-bubble', isRight ? 'dc-bubble--user' : 'dc-bubble--assistant']">
      <div v-if="loading && !content" class="dc-bubble-loading" aria-live="polite">
        <span class="dc-dot" />
        <span class="dc-dot" />
        <span class="dc-dot" />
      </div>
      <template v-else-if="showBody">
        <DcMarkdown
          v-if="markdown && displayText"
          :content="displayText"
          :streaming="loading"
        />
        <p v-else-if="displayText" class="dc-bubble-plain">{{ displayText }}</p>
        <DcMessageAttachments
          v-if="hasAttachments"
          :items="attachments"
          :spaced="!!displayText"
        />
      </template>
    </div>
    <div v-if="isRight" class="dc-bubble-avatar">
      <img v-if="avatarSrc" :src="avatarSrc" alt="" class="dc-bubble-avatar-img" />
      <span v-else class="dc-bubble-avatar-fallback dc-bubble-avatar-fallback--user">{{ avatarLetter }}</span>
    </div>
  </div>
</template>

<style scoped>
.dc-bubble-row {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  margin: 8px 0;
  max-width: 100%;
}

.dc-bubble-row--right {
  flex-direction: row-reverse;
}

.dc-bubble-avatar {
  flex-shrink: 0;
  width: 32px;
  height: 32px;
}

.dc-bubble-avatar-img {
  width: 32px;
  height: 32px;
  border-radius: 8px;
  object-fit: contain;
  background: #fff;
  border: 1px solid var(--dc-border);
}

.dc-bubble-avatar-fallback {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  border-radius: 8px;
  background: var(--dc-brand-soft);
  color: var(--dc-brand);
  font-size: 13px;
  font-weight: 600;
}

.dc-bubble-avatar-fallback--user {
  background: #e2e8f0;
  color: #475569;
}

.dc-bubble {
  max-width: min(88%, 720px);
  padding: 12px 16px;
  border-radius: var(--dc-radius-md);
  font-size: 14px;
  line-height: 1.55;
}

.dc-bubble--assistant {
  background: #fff;
  border: 1px solid var(--dc-border);
  box-shadow: var(--dc-shadow-sm);
}

.dc-bubble--user {
  background: #fff;
  color: var(--dc-text);
  border: 1px solid rgba(94, 124, 224, 0.2);
  box-shadow: 0 2px 12px rgba(94, 124, 224, 0.08);
  border-top-right-radius: 4px;
}

.dc-bubble-plain {
  margin: 0;
  white-space: pre-wrap;
  word-break: break-word;
}

.dc-bubble--user .dc-bubble-plain {
  color: var(--dc-text);
}

.dc-bubble-loading {
  display: flex;
  align-items: center;
  gap: 5px;
  padding: 4px 0;
}

.dc-dot {
  width: 7px;
  height: 7px;
  border-radius: 50%;
  background: var(--dc-text-muted);
  animation: dc-bounce 1.2s ease-in-out infinite;
}

.dc-dot:nth-child(2) {
  animation-delay: 0.15s;
}

.dc-dot:nth-child(3) {
  animation-delay: 0.3s;
}

@keyframes dc-bounce {
  0%,
  80%,
  100% {
    transform: translateY(0);
    opacity: 0.45;
  }
  40% {
    transform: translateY(-5px);
    opacity: 1;
  }
}
</style>
