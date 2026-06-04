<script setup lang="ts">
import { computed, ref } from 'vue'

const props = withDefaults(
  defineProps<{
    content: string
    streaming?: boolean
  }>(),
  {
    streaming: false,
  }
)

const expanded = ref(true)

const label = computed(() =>
  props.streaming && props.content ? '思考中…' : props.content ? '思考过程' : '思考中…'
)
</script>

<template>
  <div v-if="content || streaming" class="dc-thinking">
    <button type="button" class="dc-thinking-toggle" @click="expanded = !expanded">
      <i :class="expanded ? 'icon-chevron-down-2' : 'icon-chevron-right-2'" />
      <span>{{ label }}</span>
    </button>
    <pre v-if="expanded" class="dc-thinking-body">{{ content || '…' }}</pre>
  </div>
</template>

<style scoped>
.dc-thinking {
  margin: 0 0 10px;
  border: 1px solid var(--dc-border);
  border-radius: var(--dc-radius-sm);
  background: #f8fafc;
  overflow: hidden;
}

.dc-thinking-toggle {
  display: flex;
  align-items: center;
  gap: 6px;
  width: 100%;
  padding: 8px 10px;
  border: none;
  background: transparent;
  color: var(--dc-text-secondary);
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
  text-align: left;
}

.dc-thinking-toggle:hover {
  background: #f1f5f9;
}

.dc-thinking-body {
  margin: 0;
  padding: 0 10px 10px;
  font-size: 12px;
  line-height: 1.5;
  color: var(--dc-text-secondary);
  white-space: pre-wrap;
  word-break: break-word;
  font-family: inherit;
}
</style>
