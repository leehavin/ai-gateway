<script setup lang="ts">
import { ref, watch } from 'vue'
import {
  DEFAULT_CHAT_PARAMS,
  normalizeChatParams,
  type ChatGenerationParameters,
} from '../types/chatParams'

const props = defineProps<{
  open: boolean
  params: ChatGenerationParameters
}>()

const emit = defineEmits<{
  close: []
  reset: []
  apply: [value: ChatGenerationParameters]
}>()

const draft = ref<ChatGenerationParameters>({ ...DEFAULT_CHAT_PARAMS })

watch(
  () => props.open,
  (open) => {
    if (open) draft.value = { ...props.params }
  }
)

watch(
  () => props.params,
  (p) => {
    if (props.open) draft.value = { ...p }
  }
)

function patch(partial: Partial<ChatGenerationParameters>) {
  draft.value = normalizeChatParams({ ...draft.value, ...partial })
}

function onApply() {
  emit('apply', normalizeChatParams(draft.value))
}
</script>

<template>
  <div v-if="open" class="params-backdrop" @click.self="emit('close')">
    <aside class="params-panel" role="dialog" aria-label="生成参数">
      <header class="params-head">
        <h3>生成参数</h3>
        <button type="button" class="params-close" aria-label="关闭" @click="emit('close')">
          <i class="icon-close"></i>
        </button>
      </header>
      <p class="params-hint">
        调节后请点击「应用」才会生效并保存；关闭面板未应用时不会改动当前对话参数。
      </p>

      <label class="params-field">
        <span class="params-label">Temperature {{ draft.temperature.toFixed(1) }}</span>
        <input
          type="range"
          min="0"
          max="2"
          step="0.1"
          :value="draft.temperature"
          @input="patch({ temperature: Number(($event.target as HTMLInputElement).value) })"
        />
      </label>

      <label class="params-field">
        <span class="params-label">Top P {{ draft.topP.toFixed(2) }}</span>
        <input
          type="range"
          min="0"
          max="1"
          step="0.05"
          :value="draft.topP"
          @input="patch({ topP: Number(($event.target as HTMLInputElement).value) })"
        />
      </label>

      <label class="params-field">
        <span class="params-label">Max tokens</span>
        <input
          type="number"
          class="params-number"
          min="256"
          max="8192"
          step="256"
          :value="draft.maxTokens"
          @change="patch({ maxTokens: Number(($event.target as HTMLInputElement).value) })"
        />
      </label>

      <div class="params-foot">
        <button type="button" class="btn-secondary" @click="emit('reset')">恢复默认</button>
        <button type="button" class="btn-primary" @click="onApply">应用</button>
      </div>
      <p class="params-defaults">
        默认：T={{ DEFAULT_CHAT_PARAMS.temperature }} · TopP={{ DEFAULT_CHAT_PARAMS.topP }} ·
        {{ DEFAULT_CHAT_PARAMS.maxTokens }} tokens
      </p>
    </aside>
  </div>
</template>

<style scoped>
.params-backdrop {
  position: fixed;
  inset: 0;
  z-index: 250;
  background: rgba(15, 23, 42, 0.35);
  display: flex;
  justify-content: flex-end;
}

.params-panel {
  width: min(360px, 92vw);
  height: 100%;
  padding: 20px 18px;
  background: #fff;
  box-shadow: var(--dc-shadow-lg);
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.params-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.params-head h3 {
  margin: 0;
  font-size: 16px;
}

.params-close {
  border: none;
  background: transparent;
  cursor: pointer;
  color: var(--dc-text-muted);
}

.params-hint {
  margin: 0;
  font-size: 12px;
  line-height: 1.5;
  color: var(--dc-text-secondary);
}

.params-field {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.params-label {
  font-size: 13px;
  font-weight: 500;
}

.params-number {
  padding: 8px 10px;
  border: 1px solid var(--dc-border);
  border-radius: var(--dc-radius-sm);
  font-size: 14px;
}

.params-foot {
  display: flex;
  gap: 10px;
  margin-top: auto;
}

.btn-secondary,
.btn-primary {
  flex: 1;
  padding: 9px 14px;
  border-radius: var(--dc-radius-pill);
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
}

.btn-secondary {
  border: 1px solid var(--dc-border);
  background: #fff;
  color: var(--dc-text);
}

.btn-primary {
  border: none;
  background: var(--dc-brand);
  color: #fff;
}

.params-defaults {
  margin: 0;
  font-size: 11px;
  color: var(--dc-text-muted);
  text-align: center;
}
</style>
