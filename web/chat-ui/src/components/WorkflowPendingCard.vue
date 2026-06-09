<script setup lang="ts">
import { computed, ref } from 'vue'
import type { CozeWorkflowInputSpec, CozeWorkflowItem } from '../types'
import {
  formatAcceptShort,
  groupWorkflowInputs,
  inputDisplayLabel,
  inputKind,
  workflowCardSubtitle,
} from '../providers/coze/workflowInputHint'

const props = defineProps<{
  workflow: CozeWorkflowItem
}>()

const emit = defineEmits<{
  cancel: []
}>()

const optionalOpen = ref(false)

const groups = computed(() => groupWorkflowInputs(props.workflow.inputs))
const subtitle = computed(() => workflowCardSubtitle(props.workflow))
const hasStructuredInputs = computed(
  () =>
    groups.value.requiredFiles.length > 0 ||
    groups.value.optionalFiles.length > 0 ||
    groups.value.textInputs.length > 0
)
const optionalItems = computed(() => [
  ...groups.value.textInputs,
  ...groups.value.optionalFiles,
])
const optionalSummary = computed(() => {
  const labels = optionalItems.value.map((s) => inputDisplayLabel(s))
  if (labels.length <= 2) return labels.join('、')
  return `${labels.slice(0, 2).join('、')} 等 ${labels.length} 项`
})
const needsUpload = computed(() => groups.value.requiredFiles.length > 0)

function pillMeta(spec: CozeWorkflowInputSpec): string {
  if (isTextInput(spec)) return '输入框'
  const short = formatAcceptShort(spec.accept)
  if (short) return short
  const kind = inputKind(spec)
  if (kind === 'image') return '图片'
  if (kind === 'audio') return '音频'
  return '文件'
}

function isTextInput(spec: CozeWorkflowInputSpec): boolean {
  const t = spec.type.toLowerCase()
  return t === 'string' || t === 'integer' || t === 'number' || t === 'boolean'
}

function onIconError(e: Event) {
  const img = e.target as HTMLImageElement
  img.style.display = 'none'
  img.nextElementSibling?.classList.remove('wf-icon-fallback--hidden')
}
</script>

<template>
  <div class="wf-card">
    <div class="wf-card-bar" aria-hidden="true" />

    <div class="wf-card-inner">
      <header class="wf-head">
        <div class="wf-head-main">
          <div class="wf-icon">
            <img
              v-if="workflow.iconUrl"
              :src="workflow.iconUrl"
              alt=""
              @error="onIconError"
            />
            <i
              :class="[
                'wf-icon-fallback',
                workflow.iconUrl ? 'wf-icon-fallback--hidden' : '',
                'icon-priority',
              ]"
            />
          </div>
          <div class="wf-head-text">
            <p class="wf-title">
              <span class="wf-title-name">{{ workflow.displayName }}</span>
              <span v-if="subtitle" class="wf-title-desc">{{ subtitle }}</span>
            </p>
            <p v-if="needsUpload" class="wf-head-tip">
              <i class="icon-add-file" aria-hidden="true" />
              必填项请用下方「附件」上传
            </p>
          </div>
        </div>
        <button type="button" class="wf-close" title="取消" @click="emit('cancel')">
          <i class="icon-close" />
        </button>
      </header>

      <div v-if="hasStructuredInputs" class="wf-rows">
        <div v-if="groups.requiredFiles.length" class="wf-row">
          <span class="wf-row-label wf-row-label--req">必填</span>
          <div class="wf-pills">
            <span
              v-for="spec in groups.requiredFiles"
              :key="spec.name"
              class="wf-pill wf-pill--req"
            >
              {{ inputDisplayLabel(spec) }}
              <em>{{ pillMeta(spec) }}</em>
            </span>
          </div>
        </div>

        <div v-if="optionalItems.length" class="wf-row wf-row--optional">
          <span class="wf-row-label">可选</span>
          <div class="wf-optional-wrap">
            <button
              type="button"
              class="wf-optional-toggle"
              :aria-expanded="optionalOpen"
              @click="optionalOpen = !optionalOpen"
            >
              <span>{{ optionalSummary }}</span>
              <span class="wf-chevron" :class="{ 'is-open': optionalOpen }" aria-hidden="true" />
            </button>
            <div class="wf-pills wf-pills--optional" :class="{ 'is-open': optionalOpen }">
              <span
                v-for="spec in optionalItems"
                :key="spec.name"
                class="wf-pill"
              >
                {{ inputDisplayLabel(spec) }}
                <em>{{ pillMeta(spec) }}</em>
              </span>
            </div>
          </div>
        </div>
      </div>

      <p v-else class="wf-fallback">输入后发送执行</p>
    </div>
  </div>
</template>

<style scoped>
.wf-card {
  position: relative;
  margin-bottom: 6px;
  border-radius: 10px;
  border: 1px solid var(--dc-border);
  background: var(--dc-bg-elevated);
  overflow: hidden;
}

.wf-card-bar {
  position: absolute;
  left: 0;
  top: 0;
  bottom: 0;
  width: 3px;
  background: var(--dc-brand);
}

.wf-card-inner {
  padding: 8px 10px 8px 12px;
}

.wf-head {
  display: flex;
  align-items: flex-start;
  gap: 8px;
}

.wf-head-main {
  display: flex;
  align-items: flex-start;
  gap: 8px;
  min-width: 0;
  flex: 1;
}

.wf-icon {
  flex-shrink: 0;
  width: 24px;
  height: 24px;
  border-radius: 6px;
  border: 1px solid var(--dc-border);
  background: #fff;
  overflow: hidden;
}

.wf-icon img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.wf-icon-fallback {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  height: 100%;
  color: var(--dc-brand);
  font-size: 13px;
}

.wf-icon-fallback--hidden {
  display: none;
}

.wf-head-text {
  min-width: 0;
  flex: 1;
}

.wf-title {
  margin: 0;
  font-size: 13px;
  line-height: 1.35;
  color: var(--dc-text);
}

.wf-title-name {
  font-weight: 600;
}

.wf-title-desc {
  color: var(--dc-text-secondary);
}

.wf-title-desc::before {
  content: '·';
  margin: 0 4px;
  color: var(--dc-text-muted);
}

.wf-head-tip {
  display: flex;
  align-items: center;
  gap: 4px;
  margin: 3px 0 0;
  font-size: 11px;
  color: var(--dc-warn);
  line-height: 1.3;
}

.wf-head-tip i {
  font-size: 12px;
  flex-shrink: 0;
}

.wf-close {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 26px;
  height: 26px;
  padding: 0;
  border: none;
  border-radius: 6px;
  background: transparent;
  color: var(--dc-text-muted);
  cursor: pointer;
}

.wf-close:hover {
  background: #f1f5f9;
  color: var(--dc-text-secondary);
}

.wf-close i {
  font-size: 14px;
}

.wf-rows {
  margin-top: 8px;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.wf-row {
  display: flex;
  align-items: flex-start;
  gap: 8px;
  min-width: 0;
}

.wf-row-label {
  flex-shrink: 0;
  width: 32px;
  padding-top: 4px;
  font-size: 11px;
  font-weight: 600;
  color: var(--dc-text-muted);
  line-height: 1.2;
}

.wf-row-label--req {
  color: var(--dc-warn);
}

.wf-pills {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
  min-width: 0;
  flex: 1;
}

.wf-pill {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  max-width: 100%;
  padding: 3px 8px;
  border-radius: var(--dc-radius-pill);
  border: 1px solid var(--dc-border);
  background: #f8fafc;
  font-size: 11px;
  color: var(--dc-text);
  line-height: 1.3;
}

.wf-pill em {
  font-style: normal;
  color: var(--dc-text-muted);
  font-size: 10px;
}

.wf-pill em::before {
  content: '·';
  margin-right: 2px;
}

.wf-pill--req {
  border-color: rgba(217, 119, 6, 0.28);
  background: var(--dc-warn-bg);
}

.wf-optional-wrap {
  min-width: 0;
  flex: 1;
}

.wf-optional-toggle {
  display: none;
  align-items: center;
  justify-content: space-between;
  gap: 6px;
  width: 100%;
  padding: 4px 8px;
  border: 1px solid var(--dc-border);
  border-radius: var(--dc-radius-pill);
  background: #f8fafc;
  font-size: 11px;
  color: var(--dc-text-secondary);
  cursor: pointer;
  text-align: left;
}

.wf-chevron {
  flex-shrink: 0;
  width: 0;
  height: 0;
  border-left: 4px solid transparent;
  border-right: 4px solid transparent;
  border-top: 5px solid var(--dc-text-muted);
  transition: transform 0.15s;
}

.wf-chevron.is-open {
  transform: rotate(180deg);
}

.wf-pills--optional {
  display: flex;
}

.wf-pills--optional:not(.is-open) {
  display: none;
}

.wf-fallback {
  margin: 6px 0 0;
  font-size: 11px;
  color: var(--dc-text-muted);
}

/* 手机：可选默认折叠，点开展开 */
@media (max-width: 639px) {
  .wf-optional-toggle {
    display: flex;
  }

  .wf-pills--optional:not(.is-open) {
    display: none;
  }
}

/* 桌面：可选始终展开，隐藏折叠按钮 */
@media (min-width: 640px) {
  .wf-optional-toggle {
    display: none;
  }

  .wf-pills--optional {
    display: flex !important;
  }

  .wf-title-desc::before {
    margin: 0 6px;
  }
}
</style>
