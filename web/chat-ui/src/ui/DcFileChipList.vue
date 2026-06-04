<script setup lang="ts">
import type { ChatFileItem } from './types'

defineProps<{
  fileItems: ChatFileItem[]
}>()

const emit = defineEmits<{
  remove: [file: ChatFileItem]
}>()

function ext(name: string) {
  const i = name.lastIndexOf('.')
  return i >= 0 ? name.slice(i + 1).toLowerCase() : ''
}

function formatSize(bytes?: number) {
  if (bytes == null) return ''
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

function statusLabel(item: ChatFileItem) {
  if (item.status === 'uploading') return '上传中…'
  if (item.status === 'error') {
    const r = item.response as { message?: string } | undefined
    return r?.message || '上传失败'
  }
  return formatSize(item.size)
}
</script>

<template>
  <ul v-if="fileItems.length" class="dc-file-list">
    <li v-for="file in fileItems" :key="file.uid" :class="['dc-file-chip', `dc-file-chip--${file.status}`]">
      <span class="dc-file-ext">{{ ext(file.name) || 'file' }}</span>
      <div class="dc-file-meta">
        <span class="dc-file-name" :title="file.name">{{ file.name }}</span>
        <span class="dc-file-status">{{ statusLabel(file) }}</span>
      </div>
      <button
        type="button"
        class="dc-file-remove"
        title="移除"
        aria-label="移除附件"
        @click="emit('remove', file)"
      >
        <i class="icon-close"></i>
      </button>
    </li>
  </ul>
</template>

<style scoped>
.dc-file-list {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin: 0;
  padding: 0;
  list-style: none;
}

.dc-file-chip {
  display: flex;
  align-items: center;
  gap: 8px;
  max-width: 240px;
  padding: 6px 8px 6px 10px;
  border: 1px solid var(--dc-border);
  border-radius: var(--dc-radius-sm);
  background: #fff;
}

.dc-file-chip--error {
  border-color: rgba(220, 38, 38, 0.35);
  background: var(--dc-error-bg);
}

.dc-file-ext {
  flex-shrink: 0;
  min-width: 32px;
  padding: 4px 6px;
  border-radius: 4px;
  background: var(--dc-brand-soft);
  color: var(--dc-brand);
  font-size: 10px;
  font-weight: 700;
  text-transform: uppercase;
  text-align: center;
}

.dc-file-meta {
  min-width: 0;
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.dc-file-name {
  font-size: 12px;
  font-weight: 500;
  color: var(--dc-text);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.dc-file-status {
  font-size: 11px;
  color: var(--dc-text-muted);
}

.dc-file-chip--error .dc-file-status {
  color: var(--dc-error);
}

.dc-file-remove {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 24px;
  height: 24px;
  padding: 0;
  border: none;
  border-radius: 4px;
  background: transparent;
  color: var(--dc-text-muted);
  cursor: pointer;
}

.dc-file-remove:hover {
  background: #f1f5f9;
  color: var(--dc-text);
}

.dc-file-remove i {
  font-size: 12px;
}
</style>
