<script setup lang="ts">
const props = defineProps<{
  feedback?: 'up' | 'down'
  disabled?: boolean
}>()

const emit = defineEmits<{
  rate: [rating: 'up' | 'down']
}>()
</script>

<template>
  <div class="msg-feedback">
    <button
      type="button"
      :class="['fb-btn', feedback === 'up' && 'fb-btn--active']"
      title="有用"
      :disabled="disabled"
      @click="emit('rate', 'up')"
    >
      赞
    </button>
    <button
      type="button"
      :class="['fb-btn', feedback === 'down' && 'fb-btn--active-down']"
      title="无用"
      :disabled="disabled"
      @click="emit('rate', 'down')"
    >
      踩
    </button>
  </div>
</template>

<style scoped>
.msg-feedback {
  display: flex;
  gap: 4px;
  padding-top: 4px;
}

.fb-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  min-width: 28px;
  height: 28px;
  padding: 0 6px;
  border: 1px solid var(--dc-border);
  border-radius: var(--dc-radius-sm);
  background: #fff;
  color: var(--dc-text-muted);
  font-size: 11px;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.15s, color 0.15s, border-color 0.15s;
}

.fb-btn:hover:not(:disabled) {
  border-color: rgba(94, 124, 224, 0.4);
  color: var(--dc-brand);
}

.fb-btn--active {
  background: var(--dc-success-bg);
  border-color: rgba(22, 163, 74, 0.4);
  color: var(--dc-success);
}

.fb-btn--active-down {
  background: var(--dc-error-bg);
  border-color: rgba(220, 38, 38, 0.35);
  color: var(--dc-error);
}

.fb-btn:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}
</style>
