<script setup lang="ts">
import type { CommandMenuItem } from '../composables/useComposerCommands'

defineProps<{
  open: boolean
  kind: 'mention' | 'slash'
  slashTitle?: string
  groups: { category: string; items: CommandMenuItem[] }[]
  flatItems: CommandMenuItem[]
  activeIndex: number
}>()

const emit = defineEmits<{
  select: [item: CommandMenuItem, index: number]
}>()

function globalIndex(flatItems: CommandMenuItem[], item: CommandMenuItem): number {
  return flatItems.indexOf(item)
}
</script>

<template>
  <div v-if="open && flatItems.length" class="cmd-menu" role="listbox">
    <div class="cmd-menu-head">
      <span v-if="kind === 'mention'">选择智能体</span>
      <span v-else>{{ slashTitle ?? '命令' }}</span>
      <span class="cmd-hint">↑↓ 选择 · Enter 确认 · Esc 关闭</span>
    </div>
    <div class="cmd-menu-body dc-scroll">
      <template v-for="group in groups" :key="group.category">
        <div class="cmd-group-label">{{ group.category }}</div>
        <button
          v-for="item in group.items"
          :key="item.id"
          type="button"
          class="cmd-item"
          :class="{ active: globalIndex(flatItems, item) === activeIndex }"
          role="option"
          @mousedown.prevent="emit('select', item, globalIndex(flatItems, item))"
        >
          <span class="cmd-item-label">{{ item.label }}</span>
          <span v-if="item.desc" class="cmd-item-desc">{{ item.desc }}</span>
        </button>
      </template>
    </div>
  </div>
</template>

<style scoped>
.cmd-menu {
  position: absolute;
  left: 0;
  right: 0;
  bottom: calc(100% + 6px);
  z-index: 40;
  background: #fff;
  border: 1px solid var(--dc-border);
  border-radius: var(--dc-radius-md);
  box-shadow: var(--dc-shadow-lg);
  overflow: hidden;
}

.cmd-menu-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 12px;
  font-size: 12px;
  font-weight: 600;
  color: var(--dc-text);
  border-bottom: 1px solid var(--dc-border);
  background: var(--dc-brand-soft);
}

.cmd-hint {
  font-weight: 400;
  color: var(--dc-text-muted);
}

.cmd-menu-body {
  max-height: 240px;
  overflow-y: auto;
  padding: 4px 0;
}

.cmd-group-label {
  padding: 6px 12px 4px;
  font-size: 11px;
  font-weight: 600;
  color: var(--dc-text-muted);
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.cmd-item {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 2px;
  width: 100%;
  padding: 8px 12px;
  border: none;
  background: transparent;
  text-align: left;
  cursor: pointer;
  transition: background 0.12s;
}

.cmd-item:hover,
.cmd-item.active {
  background: var(--dc-brand-soft);
}

.cmd-item-label {
  font-size: 13px;
  font-weight: 500;
  color: var(--dc-text);
}

.cmd-item-desc {
  font-size: 11px;
  color: var(--dc-text-muted);
  line-height: 1.35;
}
</style>
