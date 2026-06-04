<script setup lang="ts">
import type { PromptDirection, PromptItem } from './types'

const props = withDefaults(
  defineProps<{
    list: PromptItem[]
    direction?: PromptDirection
  }>(),
  {
    list: () => [],
    direction: 'horizontal',
  }
)

const emit = defineEmits<{
  'item-click': [item: PromptItem]
}>()
</script>

<template>
  <div
    v-if="list.length"
    :class="['dc-prompt', direction === 'vertical' ? 'dc-prompt--vertical' : 'dc-prompt--horizontal']"
  >
    <button
      v-for="item in list"
      :key="String(item.value)"
      type="button"
      class="dc-prompt-chip"
      :title="item.desc || item.label"
      @click="emit('item-click', item)"
    >
      <i
        v-if="item.iconConfig?.name"
        :class="item.iconConfig.name"
        :style="{ color: item.iconConfig.color || 'var(--dc-brand)' }"
      />
      <span class="dc-prompt-label">{{ item.label }}</span>
    </button>
  </div>
</template>

<style scoped>
.dc-prompt {
  display: flex;
  gap: 10px;
}

.dc-prompt--horizontal {
  flex-wrap: wrap;
  justify-content: center;
}

.dc-prompt--vertical {
  flex-direction: column;
  align-items: stretch;
}

.dc-prompt-chip {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 10px 14px;
  border: 1px solid var(--dc-border);
  border-radius: var(--dc-radius-md);
  background: #fff;
  color: var(--dc-text);
  font-size: 13px;
  line-height: 1.4;
  text-align: left;
  cursor: pointer;
  box-shadow: var(--dc-shadow-sm);
  transition: border-color 0.15s, box-shadow 0.15s, background 0.15s;
}

.dc-prompt-chip:hover {
  border-color: rgba(94, 124, 224, 0.45);
  background: var(--dc-brand-soft);
  box-shadow: 0 4px 16px rgba(94, 124, 224, 0.12);
}

.dc-prompt-chip i {
  font-size: 16px;
  flex-shrink: 0;
}

.dc-prompt-label {
  flex: 1;
  min-width: 0;
}

.dc-prompt--horizontal .dc-prompt-chip {
  flex: 1 1 calc(50% - 6px);
  min-width: 140px;
  max-width: 100%;
}

@media screen and (max-width: 640px) {
  .dc-prompt--horizontal .dc-prompt-chip {
    flex: 1 1 100%;
  }
}
</style>
