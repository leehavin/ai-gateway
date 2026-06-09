<script setup lang="ts">
import { nextTick, ref, toRef, watch } from 'vue'
import { Button } from 'vue-devui/button'
import 'vue-devui/button/style.css'
import { DcAttachmentTrigger } from '../ui'
import { useComposerCommands, type CommandMenuItem } from '../composables/useComposerCommands'
import { getProviderBySlashPrefix, type SlashMenuContext } from '../providers'
import ComposerCommandMenu from './ComposerCommandMenu.vue'

const props = defineProps<{
  modelValue: string
  placeholder: string
  disabled: boolean
  maxLength: number
  slashMenuContext: SlashMenuContext
  getDropContainer: () => HTMLElement
  focusNonce?: number
}>()

const emit = defineEmits<{
  'update:modelValue': [value: string]
  'drop-files': [files: File[]]
  'paste-files': [files: File[]]
  submit: []
  'update:domainId': [id: string]
  'new-chat': []
  'pick-files': []
  'run-workflow': [workflowId: string]
  'refresh-workflows': [reason?: 'open' | 'manual']
}>()

const textareaRef = ref<HTMLTextAreaElement | null>(null)

const inputRef = ref(props.modelValue)
watch(
  () => props.modelValue,
  (v) => {
    if (v !== inputRef.value) inputRef.value = v
  }
)

const {
  activeIndex,
  trigger,
  menuOpen,
  menuItems,
  groupedMenuItems,
  slashMenuTitle,
  setCursor,
  clampActiveIndex,
  moveActive,
  flatItemAt,
  applyMentionInsert,
  removeTriggerText,
  resetActiveIndex,
} = useComposerCommands(inputRef, toRef(props, 'slashMenuContext'))

watch(menuItems, () => {
  resetActiveIndex()
  clampActiveIndex()
})

watch(
  () => {
    const t = trigger.value
    if (!t || t.kind !== 'slash') return false
    return getProviderBySlashPrefix(t.query)?.id === 'coze'
  },
  (active, wasActive) => {
    if (active && !wasActive) emit('refresh-workflows', 'open')
  }
)

function syncInput(v: string) {
  inputRef.value = v
  emit('update:modelValue', v)
}

function updateCursor() {
  const el = textareaRef.value
  if (el) setCursor(el.selectionStart ?? inputRef.value.length)
}

function onInput(e: Event) {
  const el = e.target as HTMLTextAreaElement
  syncInput(el.value)
  setCursor(el.selectionStart ?? el.value.length)
  nextTick(() => clampActiveIndex())
}

function autoResize() {
  const el = textareaRef.value
  if (!el) return
  el.style.height = 'auto'
  el.style.height = `${Math.min(el.scrollHeight, 160)}px`
}

watch(inputRef, () => nextTick(autoResize))

watch(
  () => props.focusNonce,
  () => nextTick(focusTextarea)
)

function focusTextarea() {
  textareaRef.value?.focus()
}

function insertSlashPrefix(prefix?: string, replaceTrigger = false) {
  // 从菜单选中时保留 /coze 无尾空格，便于继续展开子命令
  const token = prefix ? `/${prefix}${replaceTrigger ? '' : ' '}` : '/'
  let base = inputRef.value

  if (replaceTrigger && trigger.value) {
    base = inputRef.value.slice(0, trigger.value.start) + inputRef.value.slice(trigger.value.end)
  } else if (!prefix) {
    const t = trigger.value
    if (t?.kind === 'slash' && !t.query) {
      nextTick(() => {
        const el = textareaRef.value
        if (el) {
          const pos = el.selectionStart ?? inputRef.value.length
          el.selectionStart = el.selectionEnd = pos
          setCursor(pos)
          focusTextarea()
        }
      })
      return
    }
  }

  const needsSpace = base.length > 0 && !/\s$/.test(base)
  syncInput(`${base}${needsSpace ? ' ' : ''}${token}`)
  nextTick(() => {
    const el = textareaRef.value
    if (el) {
      const pos = inputRef.value.length
      el.selectionStart = el.selectionEnd = pos
      setCursor(pos)
      focusTextarea()
    }
  })
}

function applyItem(item: CommandMenuItem) {
  if (!item.action) return
  if (item.action === 'switch-domain' && item.domainId) {
    const nextInput =
      trigger.value?.kind === 'mention'
        ? applyMentionInsert(item)
        : removeTriggerText()
    syncInput(nextInput)
    emit('update:domainId', item.domainId)
  } else if (item.action === 'upload') {
    syncInput(removeTriggerText())
    emit('pick-files')
  } else if (item.action === 'new-chat') {
    syncInput(removeTriggerText())
    emit('new-chat')
  } else if (item.action === 'run-workflow' && item.workflowId) {
    syncInput(removeTriggerText())
    emit('run-workflow', item.workflowId)
  } else if (item.action === 'refresh-workflows') {
    syncInput(removeTriggerText())
    emit('refresh-workflows', 'manual')
  } else if (item.action === 'insert-mention') {
    const t = trigger.value
    const next = t
      ? inputRef.value.slice(0, t.start) + '@' + inputRef.value.slice(t.end)
      : `${inputRef.value}@`
    syncInput(next)
  } else if (item.action === 'insert-slash-prefix') {
    insertSlashPrefix(item.slashPrefix, true)
  } else if (item.action === 'fill-prompt' && item.promptText) {
    const base = removeTriggerText().trimEnd()
    syncInput(base ? `${base} ${item.promptText}` : item.promptText)
  }
  nextTick(() => {
    updateCursor()
    autoResize()
    focusTextarea()
  })
}

function onPaste(e: ClipboardEvent) {
  const files: File[] = []
  const items = e.clipboardData?.items
  if (items) {
    for (const item of items) {
      if (item.kind === 'file') {
        const f = item.getAsFile()
        if (f) files.push(f)
      }
    }
  }
  if (!files.length && e.clipboardData?.files?.length) {
    files.push(...Array.from(e.clipboardData.files))
  }
  if (files.length) {
    e.preventDefault()
    emit('paste-files', files)
  }
}

function onKeydown(e: KeyboardEvent) {
  updateCursor()
  if (menuOpen.value && menuItems.value.length) {
    if (e.key === 'ArrowDown') {
      e.preventDefault()
      moveActive(1)
      return
    }
    if (e.key === 'ArrowUp') {
      e.preventDefault()
      moveActive(-1)
      return
    }
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      const item = flatItemAt(activeIndex.value)
      if (item) applyItem(item)
      return
    }
    if (e.key === 'Escape') {
      e.preventDefault()
      syncInput(removeTriggerText())
      return
    }
  }
  if (e.key === 'Enter' && !e.shiftKey) {
    e.preventDefault()
    emit('submit')
  }
}
</script>

<template>
  <div class="chat-composer">
    <ComposerCommandMenu
      :open="!!trigger && menuItems.length > 0"
      :kind="trigger?.kind ?? 'mention'"
      :slash-title="slashMenuTitle"
      :groups="groupedMenuItems"
      :flat-items="menuItems"
      :active-index="activeIndex"
      @select="(item) => applyItem(item)"
    />
    <div class="composer-input-wrap">
      <textarea
        ref="textareaRef"
        class="composer-textarea"
        :value="inputRef"
        :placeholder="placeholder"
        :disabled="disabled"
        :maxlength="maxLength"
        rows="1"
        @input="onInput"
        @keydown="onKeydown"
        @click="updateCursor"
        @keyup="updateCursor"
        @paste="onPaste"
      />
    </div>
    <div class="input-foot-wrapper">
      <div class="input-foot-left">
        <DcAttachmentTrigger
          :disabled="disabled"
          :get-drop-container="getDropContainer"
          @pick="emit('pick-files')"
          @drop="(files: File[]) => emit('drop-files', files)"
        >
          <button
            type="button"
            class="foot-tool attach-tool"
            title="上传附件（Coze / DB-GPT 经网关转发）"
            :disabled="disabled"
          >
            <i class="icon-add-file"></i>
            <span class="foot-tool-label">附件</span>
          </button>
        </DcAttachmentTrigger>
        <button
          type="button"
          class="foot-tool foot-tool--slash"
          title="打开命令菜单（/coze、/dbgpt 等，也可直接输入 /）"
          :disabled="disabled"
          @click="insertSlashPrefix()"
        >
          <span class="slash-trigger">/</span>
        </button>
        <span class="input-foot-dividing-line" aria-hidden="true"></span>
        <span class="input-foot-maxlength">{{ inputRef.length }} / {{ maxLength }}</span>
      </div>
      <div class="input-foot-right">
        <Button
          icon="op-clearup"
          shape="round"
          size="sm"
          :disabled="!inputRef || disabled"
          @click="syncInput('')"
        >
          清空
        </Button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.chat-composer {
  position: relative;
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 10px 12px 8px;
}

.composer-input-wrap {
  min-height: 44px;
}

.composer-textarea {
  width: 100%;
  min-height: 44px;
  max-height: 160px;
  padding: 8px 4px;
  border: none;
  outline: none;
  resize: none;
  font: inherit;
  font-size: 14px;
  line-height: 1.55;
  color: var(--dc-text);
  background: transparent;
}

.composer-textarea:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.composer-textarea::placeholder {
  color: var(--dc-text-muted);
}

.input-foot-wrapper {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex: 1;
  width: 100%;
  min-width: 0;
  gap: 12px;
}

.input-foot-left {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
}

.input-foot-right {
  display: flex;
  align-items: center;
  flex-shrink: 0;
}

.foot-tool {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 4px 6px;
  border: none;
  border-radius: var(--dc-radius-sm);
  background: transparent;
  color: var(--dc-text);
  font-size: 12px;
  cursor: pointer;
  transition: color 0.15s, background 0.15s;
}

.foot-tool:hover:not(:disabled) {
  color: var(--dc-brand);
  background: var(--dc-brand-soft);
}

.foot-tool:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

.foot-tool i {
  font-size: 16px;
  color: var(--dc-brand);
}

.foot-tool--slash .slash-trigger {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 22px;
  height: 22px;
  border-radius: 6px;
  background: var(--dc-brand-soft);
  color: var(--dc-brand);
  font-size: 14px;
  font-weight: 700;
  line-height: 1;
}

.foot-tool-label {
  white-space: nowrap;
}

.input-foot-dividing-line {
  width: 1px;
  height: 14px;
  background: var(--dc-border-strong);
  flex-shrink: 0;
}

.input-foot-maxlength {
  font-size: 12px;
  color: var(--dc-text-muted);
  font-variant-numeric: tabular-nums;
  white-space: nowrap;
}

@media screen and (max-width: 860px) {
  .foot-tool-label {
    display: none;
  }
}
</style>
