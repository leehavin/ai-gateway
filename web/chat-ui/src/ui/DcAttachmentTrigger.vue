<script setup lang="ts">
import { onMounted, onUnmounted, ref } from 'vue'
import { ATTACHMENT_ACCEPT } from './types'

const props = withDefaults(
  defineProps<{
    disabled?: boolean
    draggable?: boolean
    dropPlaceholder?: string
    accept?: string
    getDropContainer?: () => HTMLElement
  }>(),
  {
    disabled: false,
    draggable: true,
    dropPlaceholder: '松开鼠标即可上传文件',
    accept: ATTACHMENT_ACCEPT,
  }
)

const emit = defineEmits<{
  pick: []
  drop: [files: File[]]
}>()

const dragging = ref(false)
let dropEl: HTMLElement | null = null

function onDragEnter(e: DragEvent) {
  if (!props.draggable || props.disabled) return
  if (!e.dataTransfer?.types.includes('Files')) return
  e.preventDefault()
  dragging.value = true
}

function onDragOver(e: DragEvent) {
  if (!props.draggable || props.disabled) return
  if (!e.dataTransfer?.types.includes('Files')) return
  e.preventDefault()
  e.dataTransfer.dropEffect = 'copy'
}

function onDragLeave(e: DragEvent) {
  if (e.currentTarget === dropEl && !dropEl?.contains(e.relatedTarget as Node)) {
    dragging.value = false
  }
}

function onDrop(e: DragEvent) {
  if (!props.draggable || props.disabled) return
  e.preventDefault()
  dragging.value = false
  const files = Array.from(e.dataTransfer?.files ?? [])
  if (files.length) emit('drop', files)
}

function onClick() {
  if (!props.disabled) emit('pick')
}

function bindDropTarget() {
  dropEl = props.getDropContainer?.() ?? null
  if (!dropEl) return
  dropEl.addEventListener('dragenter', onDragEnter)
  dropEl.addEventListener('dragover', onDragOver)
  dropEl.addEventListener('dragleave', onDragLeave)
  dropEl.addEventListener('drop', onDrop)
}

function unbindDropTarget() {
  if (!dropEl) return
  dropEl.removeEventListener('dragenter', onDragEnter)
  dropEl.removeEventListener('dragover', onDragOver)
  dropEl.removeEventListener('dragleave', onDragLeave)
  dropEl.removeEventListener('drop', onDrop)
  dropEl = null
}

onMounted(bindDropTarget)
onUnmounted(unbindDropTarget)
</script>

<template>
  <div class="dc-attach-trigger">
    <div
      v-if="dragging"
      class="dc-attach-overlay"
      aria-hidden="true"
    >
      <p>{{ dropPlaceholder }}</p>
    </div>
    <div class="dc-attach-slot" @click="onClick">
      <slot />
    </div>
  </div>
</template>

<style scoped>
.dc-attach-trigger {
  position: relative;
  display: inline-flex;
}

.dc-attach-slot {
  display: inline-flex;
}

.dc-attach-overlay {
  position: fixed;
  inset: 0;
  z-index: 200;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(94, 124, 224, 0.12);
  border: 2px dashed var(--dc-brand);
  pointer-events: none;
}

.dc-attach-overlay p {
  margin: 0;
  padding: 12px 20px;
  border-radius: var(--dc-radius-md);
  background: #fff;
  color: var(--dc-brand);
  font-size: 15px;
  font-weight: 600;
  box-shadow: var(--dc-shadow-lg);
}
</style>
