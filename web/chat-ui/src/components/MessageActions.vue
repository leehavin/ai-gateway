<script setup lang="ts">
export type MessageAction = 'copy' | 'edit' | 'regenerate' | 'quote'

withDefaults(
  defineProps<{
    actions: MessageAction[]
    disabled?: boolean
    /** side：助手消息旁；toolbar：用户消息气泡下方 */
    placement?: 'side' | 'toolbar'
  }>(),
  { placement: 'side' }
)

const emit = defineEmits<{
  copy: []
  edit: []
  regenerate: []
  quote: []
}>()
</script>

<template>
  <div :class="['msg-actions', placement === 'toolbar' && 'msg-actions--toolbar']">
    <button
      v-if="actions.includes('copy')"
      type="button"
      class="msg-action-btn"
      title="复制"
      :disabled="disabled"
      @click="emit('copy')"
    >
      复制
    </button>
    <button
      v-if="actions.includes('edit')"
      type="button"
      class="msg-action-btn"
      title="编辑并重新发送"
      :disabled="disabled"
      @click="emit('edit')"
    >
      编辑
    </button>
    <button
      v-if="actions.includes('quote')"
      type="button"
      class="msg-action-btn"
      title="引用到输入框"
      :disabled="disabled"
      @click="emit('quote')"
    >
      引用
    </button>
    <button
      v-if="actions.includes('regenerate')"
      type="button"
      class="msg-action-btn"
      title="重新生成"
      :disabled="disabled"
      @click="emit('regenerate')"
    >
      <i class="icon-refresh"></i>
    </button>
  </div>
</template>

<style scoped>
.msg-actions {
  display: flex;
  align-items: center;
  gap: 4px;
  opacity: 0;
  transition: opacity 0.15s;
  flex-shrink: 0;
  padding-top: 6px;
  pointer-events: none;
}

.msg-actions--toolbar {
  padding-top: 0;
  justify-content: flex-end;
  width: 100%;
}

.message-row:hover .msg-actions,
.message-row:focus-within .msg-actions,
.user-message-wrap:hover .msg-actions,
.user-message-wrap:focus-within .msg-actions {
  opacity: 1;
  pointer-events: auto;
}

.msg-action-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  min-width: 28px;
  height: 28px;
  padding: 0 8px;
  font-size: 11px;
  font-weight: 500;
  border: 1px solid var(--dc-border);
  border-radius: var(--dc-radius-sm);
  background: #fff;
  color: var(--dc-text-secondary);
  cursor: pointer;
  box-shadow: var(--dc-shadow-sm);
  transition: color 0.15s, border-color 0.15s, background 0.15s;
}

.msg-action-btn:hover:not(:disabled) {
  color: var(--dc-brand);
  border-color: rgba(94, 124, 224, 0.4);
  background: var(--dc-brand-soft);
}

.msg-action-btn:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

.msg-action-btn i {
  font-size: 14px;
}
</style>
