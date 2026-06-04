<script setup lang="ts">
import { ref } from 'vue'
import { BRAND } from '../constants/brand'
import { formatHistoryTime } from '../utils/session'
import type { HistoryGroup } from '../composables/useHistory'
import type { SessionMeta } from '../types'

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
  rename: [id: string, title: string]
  pin: [id: string]
  newChat: []
  collapse: []
}>()

const renamingId = ref('')
const renameDraft = ref('')

function startRename(item: SessionMeta) {
  renamingId.value = item.id
  renameDraft.value = item.title
}

function commitRename(id: string) {
  const title = renameDraft.value.trim()
  if (title) emit('rename', id, title)
  renamingId.value = ''
}

function cancelRename() {
  renamingId.value = ''
}
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
      <button
        type="button"
        class="collapse-btn"
        title="收起侧栏（可点左侧「历史」或顶栏菜单再次展开）"
        aria-label="收起对话历史侧栏"
        @click="emit('collapse')"
      >
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
          <div
            v-for="item in group.items"
            :key="item.id"
            :class="['history-item-wrap', item.id === activeId && 'active']"
          >
            <button
              type="button"
              class="history-item"
              @click="emit('select', item.id)"
              @dblclick.stop="startRename(item)"
            >
              <div class="item-header">
                <div class="item-top">
                  <i
                    :class="['item-icon', item.pinned ? 'icon-star' : 'icon-comment']"
                    :style="item.pinned ? { color: '#d97706' } : undefined"
                  />
                  <input
                    v-if="renamingId === item.id"
                    v-model="renameDraft"
                    class="item-rename-input"
                    type="text"
                    maxlength="80"
                    @click.stop
                    @keydown.enter.prevent="commitRename(item.id)"
                    @keydown.esc.prevent="cancelRename"
                    @blur="commitRename(item.id)"
                  />
                  <span v-else class="item-title" :title="item.title">{{ item.title }}</span>
                </div>
                <div class="item-actions" @click.stop>
                  <button
                    type="button"
                    class="item-action"
                    :title="item.pinned ? '取消置顶' : '置顶'"
                    @click.stop="emit('pin', item.id)"
                  >
                    <i :class="item.pinned ? 'icon-star' : 'icon-priority'" />
                  </button>
                  <button
                    type="button"
                    class="item-action"
                    title="重命名（或双击标题）"
                    @click.stop="startRename(item)"
                  >
                    <i class="icon-info-o" />
                  </button>
                  <button
                    type="button"
                    class="item-action item-action--danger"
                    title="删除会话"
                    @click.stop="emit('delete', item.id)"
                  >
                    <i class="icon-delete" />
                  </button>
                </div>
              </div>
              <p v-if="item.preview" class="item-preview">{{ item.preview }}</p>
              <div class="item-foot">
                <span class="item-time">{{ formatHistoryTime(item.updatedAt) }}</span>
              </div>
            </button>
          </div>
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

.history-item-wrap {
  margin-bottom: 6px;
  border-radius: var(--dc-radius-md);
}

.history-item-wrap.active .history-item {
  border-color: rgba(94, 124, 224, 0.35);
  box-shadow: 0 4px 16px rgba(94, 124, 224, 0.12);
}

.history-item {
  display: flex;
  flex-direction: column;
  gap: 6px;
  width: 100%;
  padding: 12px 12px 10px;
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

.history-item-wrap:hover .history-item {
  background: #fff;
  border-color: var(--dc-border);
  box-shadow: var(--dc-shadow-sm);
}

.item-header {
  display: flex;
  align-items: flex-start;
  gap: 6px;
  min-width: 0;
}

.item-top {
  display: flex;
  align-items: center;
  gap: 8px;
  flex: 1;
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

.item-rename-input {
  flex: 1;
  min-width: 0;
  padding: 4px 8px;
  border: 1px solid var(--dc-brand);
  border-radius: var(--dc-radius-sm);
  font-size: 13px;
  font-weight: 600;
  outline: none;
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
  padding-left: 22px;
}

.item-time {
  font-size: 11px;
  color: var(--dc-text-muted);
}

.item-actions {
  display: flex;
  flex-shrink: 0;
  gap: 2px;
  max-width: 0;
  overflow: hidden;
  opacity: 0;
  pointer-events: none;
  transition:
    max-width 0.2s ease,
    opacity 0.12s ease;
}

.history-item-wrap:hover .item-actions,
.history-item-wrap.active .item-actions,
.history-item-wrap:focus-within .item-actions {
  max-width: 88px;
  opacity: 1;
  pointer-events: auto;
}

.item-action {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 24px;
  height: 24px;
  padding: 0;
  border: none;
  border-radius: 6px;
  background: rgba(255, 255, 255, 0.95);
  color: var(--dc-text-muted);
  cursor: pointer;
  box-shadow: var(--dc-shadow-sm);
}

.item-action:hover {
  color: var(--dc-brand);
  background: var(--dc-brand-soft);
}

.item-action--danger:hover {
  color: var(--dc-error);
  background: var(--dc-error-bg);
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
