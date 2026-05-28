<script setup lang="ts">
import { BRAND } from '../constants/brand'
import { formatHistoryTime } from '../utils/session'
import type { HistoryGroup } from '../composables/useHistory'

defineProps<{
  expanded: boolean
  searchKey: string
  groups: HistoryGroup[]
  activeId: string
}>()

const emit = defineEmits<{
  'update:searchKey': [value: string]
  select: [id: string]
  delete: [id: string]
  newChat: []
  collapse: []
}>()
</script>

<template>
  <aside :class="['history-panel', !expanded && 'collapsed']">
    <div class="sidebar-brand">
      <img
        class="brand-logo"
        :src="BRAND.logoIcon"
        :alt="BRAND.name"
        width="36"
        height="36"
      />
      <div class="brand-text">
        <span class="brand-name">{{ BRAND.name }}</span>
        <span class="brand-tag">{{ BRAND.tagline }}</span>
      </div>
      <button type="button" class="collapse-btn" title="收起侧栏" @click="emit('collapse')">
        <i class="icon-collapse-to-left"></i>
      </button>
    </div>

    <div class="history-toolbar">
      <div class="search-wrap">
        <i class="icon-search search-icon"></i>
        <input
          class="history-search"
          type="search"
          placeholder="搜索对话记录"
          :value="searchKey"
          @input="emit('update:searchKey', ($event.target as HTMLInputElement).value)"
        />
      </div>
      <button type="button" class="btn-new" title="新建对话" @click="emit('newChat')">
        <i class="icon-add"></i>
        <span>新对话</span>
      </button>
    </div>

    <div class="history-list-box dc-scroll">
      <template v-if="groups.length">
        <section v-for="(group, gi) in groups" :key="gi" class="history-group">
          <h4 class="group-title">{{ group.title }}</h4>
          <button
            v-for="item in group.items"
            :key="item.id"
            type="button"
            :class="['history-item', item.id === activeId && 'active']"
            @click="emit('select', item.id)"
          >
            <div class="item-top">
              <i class="icon-comment item-icon"></i>
              <span class="item-title">{{ item.title }}</span>
            </div>
            <p v-if="item.preview" class="item-preview">{{ item.preview }}</p>
            <div class="item-foot">
              <span class="item-time">{{ formatHistoryTime(item.updatedAt) }}</span>
              <span
                class="item-delete"
                title="删除会话"
                @click.stop="emit('delete', item.id)"
              >
                <i class="icon-delete"></i>
              </span>
            </div>
          </button>
        </section>
      </template>
      <div v-else class="history-empty">
        <i class="icon-comment empty-icon"></i>
        <p>暂无对话记录</p>
        <span>点击「新对话」开始</span>
      </div>
    </div>
  </aside>
</template>

<style scoped>
.history-panel {
  display: flex;
  flex-direction: column;
  width: var(--dc-sidebar-width);
  min-width: var(--dc-sidebar-width);
  height: 100%;
  padding: 16px 14px;
  background: var(--dc-bg-sidebar);
  backdrop-filter: blur(20px);
  border-right: 1px solid var(--dc-border);
  transition:
    width 0.28s ease,
    min-width 0.28s ease,
    opacity 0.28s ease,
    padding 0.28s ease;
}

.history-panel.collapsed {
  width: 0;
  min-width: 0;
  padding: 0;
  opacity: 0;
  overflow: hidden;
  border: none;
  pointer-events: none;
}

.sidebar-brand {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 16px;
  padding-bottom: 14px;
  border-bottom: 1px solid var(--dc-border);
}

.brand-logo {
  flex-shrink: 0;
  object-fit: contain;
}

.brand-text {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.brand-name {
  font-size: 16px;
  font-weight: 700;
  letter-spacing: -0.02em;
  color: var(--dc-text);
}

.brand-tag {
  font-size: 11px;
  color: var(--dc-text-muted);
}

.collapse-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 30px;
  height: 30px;
  border: none;
  border-radius: var(--dc-radius-sm);
  background: transparent;
  color: var(--dc-text-muted);
  cursor: pointer;
}

.collapse-btn:hover {
  background: #f1f5f9;
  color: var(--dc-brand);
}

.history-toolbar {
  display: flex;
  flex-direction: column;
  gap: 10px;
  margin-bottom: 12px;
}

.search-wrap {
  position: relative;
}

.search-icon {
  position: absolute;
  left: 12px;
  top: 50%;
  transform: translateY(-50%);
  font-size: 14px;
  color: var(--dc-text-muted);
  pointer-events: none;
}

.history-search {
  width: 100%;
  padding: 9px 12px 9px 36px;
  border: 1px solid var(--dc-border);
  border-radius: var(--dc-radius-pill);
  background: #fff;
  font-size: 13px;
  outline: none;
  transition: border-color 0.15s, box-shadow 0.15s;
}

.history-search:focus {
  border-color: rgba(94, 124, 224, 0.5);
  box-shadow: 0 0 0 3px rgba(94, 124, 224, 0.12);
}

.btn-new {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  width: 100%;
  padding: 9px 14px;
  border: none;
  border-radius: var(--dc-radius-pill);
  background: linear-gradient(135deg, var(--dc-brand) 0%, #6b8cff 100%);
  color: #fff;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  box-shadow: 0 4px 14px rgba(94, 124, 224, 0.35);
  transition: transform 0.12s, box-shadow 0.15s;
}

.btn-new:hover {
  transform: translateY(-1px);
  box-shadow: 0 6px 18px rgba(94, 124, 224, 0.4);
}

.btn-new i {
  font-size: 14px;
}

.history-list-box {
  flex: 1;
  overflow: auto;
  margin: 0 -4px;
  padding: 0 4px;
}

.history-group {
  margin-bottom: 16px;
}

.group-title {
  margin: 0 0 8px 4px;
  font-size: 11px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  color: var(--dc-text-muted);
}

.history-item {
  display: flex;
  flex-direction: column;
  gap: 6px;
  width: 100%;
  padding: 12px 12px 10px;
  margin-bottom: 6px;
  border: 1px solid transparent;
  border-radius: var(--dc-radius-md);
  background: rgba(255, 255, 255, 0.65);
  text-align: left;
  cursor: pointer;
  transition:
    background 0.15s,
    border-color 0.15s,
    box-shadow 0.15s;
}

.history-item:hover {
  background: #fff;
  border-color: var(--dc-border);
  box-shadow: var(--dc-shadow-sm);
}

.history-item.active {
  background: #fff;
  border-color: rgba(94, 124, 224, 0.35);
  box-shadow: 0 4px 16px rgba(94, 124, 224, 0.12);
}

.item-top {
  display: flex;
  align-items: center;
  gap: 8px;
  min-width: 0;
}

.item-icon {
  flex-shrink: 0;
  font-size: 14px;
  color: var(--dc-brand);
  opacity: 0.85;
}

.item-title {
  flex: 1;
  font-size: 13px;
  font-weight: 600;
  line-height: 1.35;
  color: var(--dc-text);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.item-preview {
  margin: 0;
  padding-left: 22px;
  font-size: 12px;
  line-height: 1.45;
  color: var(--dc-text-secondary);
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.item-foot {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding-left: 22px;
}

.item-time {
  font-size: 11px;
  color: var(--dc-text-muted);
}

.item-delete {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 24px;
  height: 24px;
  border-radius: 6px;
  color: var(--dc-text-muted);
  opacity: 0;
  transition: opacity 0.12s, background 0.12s, color 0.12s;
}

.history-item:hover .item-delete,
.history-item.active .item-delete {
  opacity: 1;
}

.item-delete:hover {
  background: var(--dc-error-bg);
  color: var(--dc-error);
}

.history-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 48px 16px;
  text-align: center;
  color: var(--dc-text-muted);
}

.empty-icon {
  font-size: 36px;
  opacity: 0.35;
}

.history-empty p {
  margin: 0;
  font-size: 14px;
  font-weight: 500;
  color: var(--dc-text-secondary);
}

.history-empty span {
  font-size: 12px;
}

@media screen and (max-width: 860px) {
  .history-panel {
    position: fixed;
    z-index: 100;
    left: 0;
    top: 0;
    height: 100vh;
    width: min(88vw, var(--dc-sidebar-width));
    min-width: min(88vw, var(--dc-sidebar-width));
    box-shadow: var(--dc-shadow-lg);
    transform: translateX(0);
    transition:
      transform 0.28s ease,
      opacity 0.28s ease,
      visibility 0.28s;
  }

  .history-panel.collapsed {
    transform: translateX(-100%);
    opacity: 0;
    visibility: hidden;
    pointer-events: none;
  }

  .collapse-btn {
    display: none;
  }
}
</style>
