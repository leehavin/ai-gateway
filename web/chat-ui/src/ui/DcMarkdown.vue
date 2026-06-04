<script setup lang="ts">
import { computed, nextTick, onUpdated, ref, toRef, watch } from 'vue'
import { useThrottledMarkdown } from './composables/useThrottledMarkdown'
import { enhanceCodeBlocks } from './markdown/enhanceCodeBlocks'
import { renderMarkdownHtml } from './markdown/render'
import 'highlight.js/styles/github.min.css'
import './markdown/markdown.css'

const props = withDefaults(
  defineProps<{
    content: string
    /** 流式输出中，启用节流与未闭合 fence 补全 */
    streaming?: boolean
  }>(),
  {
    streaming: false,
  }
)

const rootRef = ref<HTMLElement | null>(null)
const contentRef = toRef(props, 'content')
const streamingRef = toRef(props, 'streaming')
const displayed = useThrottledMarkdown(contentRef, streamingRef)

const html = computed(() =>
  renderMarkdownHtml(displayed.value, { streaming: props.streaming })
)

function runEnhancements() {
  if (rootRef.value) enhanceCodeBlocks(rootRef.value)
}

onUpdated(runEnhancements)
watch(html, () => nextTick(runEnhancements))
</script>

<template>
  <div ref="rootRef" class="dc-markdown" v-html="html" />
</template>
