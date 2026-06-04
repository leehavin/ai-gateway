<script setup lang="ts">
import { nextTick, onMounted, onUnmounted, ref, watch } from 'vue'

const props = withDefaults(
  defineProps<{
    autoScroll?: boolean
    showScrollArrow?: boolean
  }>(),
  {
    autoScroll: true,
    showScrollArrow: false,
  }
)

const rootRef = ref<HTMLElement | null>(null)
const showArrow = ref(false)
let mo: MutationObserver | null = null

function isNearBottom(el: HTMLElement, threshold = 80) {
  return el.scrollHeight - el.scrollTop - el.clientHeight < threshold
}

function scrollToBottom(smooth = false) {
  const el = rootRef.value
  if (!el) return
  el.scrollTo({ top: el.scrollHeight, behavior: smooth ? 'smooth' : 'auto' })
  showArrow.value = false
}

function onScroll() {
  const el = rootRef.value
  if (!el || !props.showScrollArrow) return
  showArrow.value = !isNearBottom(el)
}

function maybeAutoScroll() {
  if (!props.autoScroll) return
  const el = rootRef.value
  if (!el) return
  if (isNearBottom(el, 120)) scrollToBottom(false)
}

defineExpose({ scrollToBottom })

onMounted(() => {
  const el = rootRef.value
  if (!el) return
  mo = new MutationObserver(() => {
    nextTick(maybeAutoScroll)
  })
  mo.observe(el, { childList: true, subtree: true, characterData: true })
  nextTick(maybeAutoScroll)
})

onUnmounted(() => mo?.disconnect())

watch(
  () => props.autoScroll,
  () => nextTick(maybeAutoScroll)
)
</script>

<template>
  <div ref="rootRef" class="dc-chat-scroll dc-scroll" @scroll="onScroll">
    <slot />
    <button
      v-if="showScrollArrow && showArrow"
      type="button"
      class="dc-scroll-arrow"
      title="回到底部"
      aria-label="回到底部"
      @click="scrollToBottom(true)"
    >
      <i class="icon-down"></i>
    </button>
  </div>
</template>

<style scoped>
.dc-chat-scroll {
  position: relative;
  flex: 1;
  min-height: 0;
  overflow-y: auto;
  overflow-x: hidden;
}

.dc-scroll-arrow {
  position: sticky;
  bottom: 16px;
  left: 50%;
  transform: translateX(-50%);
  z-index: 2;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  margin: -44px auto 8px;
  border: 1px solid var(--dc-border);
  border-radius: 50%;
  background: #fff;
  color: var(--dc-brand);
  box-shadow: var(--dc-shadow-md);
  cursor: pointer;
  transition: box-shadow 0.15s, background 0.15s;
}

.dc-scroll-arrow:hover {
  background: var(--dc-brand-soft);
  box-shadow: var(--dc-shadow-lg);
}

.dc-scroll-arrow i {
  font-size: 16px;
}
</style>
