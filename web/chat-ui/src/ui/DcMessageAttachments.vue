<script setup lang="ts">
defineProps<{
  items: { name: string }[]
  /** 与上方正文拉开间距 */
  spaced?: boolean
}>()

function ext(name: string) {
  const i = name.lastIndexOf('.')
  return i >= 0 ? name.slice(i + 1).toLowerCase() : 'file'
}
</script>

<template>
  <ul
    v-if="items.length"
    :class="['dc-msg-attach-list', spaced && 'dc-msg-attach-list--spaced']"
  >
    <li v-for="(item, i) in items" :key="`${item.name}-${i}`" class="dc-msg-attach-chip">
      <span class="dc-msg-attach-icon" aria-hidden="true">
        <i class="icon-add-file"></i>
      </span>
      <span class="dc-msg-attach-ext">{{ ext(item.name) }}</span>
      <span class="dc-msg-attach-name" :title="item.name">{{ item.name }}</span>
    </li>
  </ul>
</template>

<style scoped>
.dc-msg-attach-list {
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin: 0;
  padding: 0;
  list-style: none;
}

.dc-msg-attach-list--spaced {
  margin-top: 10px;
}

.dc-msg-attach-chip {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 10px;
  border-radius: var(--dc-radius-sm);
  background: rgba(255, 255, 255, 0.92);
  border: 1px solid rgba(94, 124, 224, 0.18);
  max-width: 280px;
}

.dc-msg-attach-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border-radius: 6px;
  background: var(--dc-brand-soft);
  color: var(--dc-brand);
  flex-shrink: 0;
}

.dc-msg-attach-icon i {
  font-size: 14px;
}

.dc-msg-attach-ext {
  flex-shrink: 0;
  min-width: 28px;
  padding: 2px 6px;
  border-radius: 4px;
  background: #f1f5f9;
  color: var(--dc-text-secondary);
  font-size: 10px;
  font-weight: 700;
  text-transform: uppercase;
  text-align: center;
}

.dc-msg-attach-name {
  min-width: 0;
  flex: 1;
  font-size: 13px;
  font-weight: 500;
  color: var(--dc-text);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
