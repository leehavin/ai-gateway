<script setup lang="ts">
import { BRAND } from '../constants/brand'
import type { DomainItem, GatewayHealth } from '../types'

defineProps<{
  title: string
  domains: DomainItem[]
  domainId: string
  loading: boolean
  error: string | null
  health: GatewayHealth | null
  showMenu: boolean
}>()

const emit = defineEmits<{
  'update:domainId': [value: string]
  refresh: []
  toggleMenu: []
  toggleHistory: []
}>()
</script>

<template>
  <header class="app-header">
    <div class="header-left">
      <button
        v-if="showMenu"
        type="button"
        class="dc-icon-btn"
        title="对话历史"
        @click="emit('toggleMenu')"
      >
        <i class="icon-list-view"></i>
      </button>
      <img
        class="header-logo"
        :src="BRAND.logoIcon"
        :alt="BRAND.name"
        width="32"
        height="32"
      />
      <div class="header-titles">
        <h1 class="header-title">{{ title }}</h1>
        <span class="header-sub">{{ BRAND.tagline }}</span>
      </div>
    </div>

    <div class="header-center">
      <label class="domain-field">
        <span class="domain-field-label">智能体</span>
        <select
          class="domain-select"
          :value="domainId"
          :disabled="loading || !!error"
          @change="emit('update:domainId', ($event.target as HTMLSelectElement).value)"
        >
          <option v-for="d in domains" :key="d.id" :value="d.id">{{ d.displayName }}</option>
        </select>
      </label>
    </div>

    <div class="header-right">
      <template v-if="loading">
        <span class="dc-badge dc-badge--muted">连接中…</span>
      </template>
      <template v-else-if="error">
        <span class="dc-badge dc-badge--error" :title="error">网关异常</span>
      </template>
      <template v-else-if="health">
        <span
          class="dc-badge"
          :class="health.dbgptReachable ? 'dc-badge--ok' : 'dc-badge--warn'"
          :title="health.dbgptBaseUrl ?? ''"
        >
          DB-GPT {{ health.dbgptReachable ? '在线' : '离线' }}
        </span>
        <span v-if="health.useMock" class="dc-badge dc-badge--warn">演示模式</span>
        <span class="dc-badge dc-badge--muted">{{ health.domainCount }} 领域</span>
      </template>
      <button
        v-if="showMenu"
        type="button"
        class="dc-icon-btn"
        title="对话历史"
        @click="emit('toggleHistory')"
      >
        <i class="icon-history"></i>
      </button>
      <button type="button" class="dc-icon-btn" title="刷新配置" @click="emit('refresh')">
        <i class="icon-refresh"></i>
      </button>
    </div>
  </header>
</template>

<style scoped>
.app-header {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 12px 20px;
  border-bottom: 1px solid var(--dc-border);
  background: rgba(255, 255, 255, 0.85);
  backdrop-filter: blur(12px);
  flex-shrink: 0;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 0;
}

.header-logo {
  flex-shrink: 0;
  width: 32px;
  height: 32px;
  object-fit: contain;
}

.header-titles {
  min-width: 0;
}

.header-title {
  margin: 0;
  font-size: 15px;
  font-weight: 600;
  line-height: 1.3;
  color: var(--dc-text);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.header-sub {
  display: block;
  font-size: 11px;
  color: var(--dc-text-muted);
  letter-spacing: 0.02em;
}

.header-center {
  flex: 1;
  display: flex;
  justify-content: center;
  min-width: 0;
}

.domain-field {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 4px 4px 4px 12px;
  background: var(--dc-brand-soft);
  border: 1px solid rgba(94, 124, 224, 0.2);
  border-radius: var(--dc-radius-pill);
}

.domain-field-label {
  font-size: 12px;
  font-weight: 500;
  color: var(--dc-brand);
  white-space: nowrap;
}

.domain-select {
  appearance: none;
  border: none;
  background: #fff
    url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='12' viewBox='0 0 12 12'%3E%3Cpath fill='%2371757f' d='M3 4.5 6 7.5 9 4.5'/%3E%3C/svg%3E")
    no-repeat right 10px center;
  padding: 6px 28px 6px 12px;
  border-radius: var(--dc-radius-pill);
  font-size: 13px;
  font-weight: 500;
  color: var(--dc-text);
  cursor: pointer;
  max-width: 220px;
  outline: none;
}

.domain-select:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-shrink: 0;
}

.dc-badge {
  display: inline-flex;
  align-items: center;
  padding: 3px 10px;
  border-radius: var(--dc-radius-pill);
  font-size: 11px;
  font-weight: 500;
  white-space: nowrap;
}

.dc-badge--ok {
  background: var(--dc-success-bg);
  color: var(--dc-success);
}

.dc-badge--warn {
  background: var(--dc-warn-bg);
  color: var(--dc-warn);
}

.dc-badge--error {
  background: var(--dc-error-bg);
  color: var(--dc-error);
}

.dc-badge--muted {
  background: #f1f5f9;
  color: var(--dc-text-secondary);
}

.dc-icon-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 34px;
  height: 34px;
  border: 1px solid var(--dc-border);
  border-radius: var(--dc-radius-sm);
  background: #fff;
  color: var(--dc-text-secondary);
  cursor: pointer;
  transition: border-color 0.15s, color 0.15s, background 0.15s;
}

.dc-icon-btn:hover {
  border-color: rgba(94, 124, 224, 0.45);
  color: var(--dc-brand);
  background: var(--dc-brand-soft);
}

.dc-icon-btn i {
  font-size: 16px;
}

@media (max-width: 900px) {
  .header-sub {
    display: none;
  }

  .header-center {
    justify-content: flex-end;
  }

  .dc-badge:not(.dc-badge--error) {
    display: none;
  }
}

@media (max-width: 640px) {
  .domain-field-label {
    display: none;
  }
}
</style>
