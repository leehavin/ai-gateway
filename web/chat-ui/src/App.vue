<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { Button } from 'vue-devui/button'
import 'vue-devui/button/style.css'
import AppHeader from './components/AppHeader.vue'
import HistoryPanel from './components/HistoryPanel.vue'
import { useAttachments } from './composables/useAttachments'
import { useChat } from './composables/useChat'
import { useDomains } from './composables/useDomains'
import { useHistory } from './composables/useHistory'
import { BRAND } from './constants/brand'
import { useLayout } from './composables/useLayout'

const { domains, health, loading, error, domainId, refresh } = useDomains()
const history = useHistory(domainId)
const {
  searchKey,
  asideExpanded,
  groupedList,
  activeId: historyActiveId,
  toggleAside,
} = history
const activeDomain = computed(() => domains.value.find((d) => d.id === domainId.value))
const attachments = useAttachments()
const { fileItems, uploadOptions, hasUploading, getDropContainer } = attachments

function bindDropZone(el: unknown) {
  attachments.dropZoneEl.value = el instanceof HTMLElement ? el : null
}

const {
  session,
  inputValue,
  sending,
  startPage,
  greetText,
  description,
  introPrompt,
  simplePrompt,
  placeholder,
  newConversation,
  loadSessionById,
  applySession,
  onSubmit,
} = useChat(domainId, activeDomain, history, attachments)

const contentRef = ref<{ scrollToBottom?: () => void } | null>(null)

watch(
  () => session.value.messages.map((m) => m.content).join('|'),
  () => contentRef.value?.scrollToBottom?.()
)

function handleSubmit(text?: string) {
  void onSubmit(typeof text === 'string' ? text : inputValue.value)
}

function onRemoveFile(file: { uid?: number }) {
  fileItems.value = fileItems.value.filter((f: { uid?: number }) => f.uid !== file.uid)
}

function onSelectHistory(id: string) {
  loadSessionById(id)
}

function onDeleteHistory(id: string) {
  applySession(history.removeSession(id))
}

const headerTitle = computed(() => activeDomain.value?.displayName ?? 'DataChat')
const { isNarrow } = useLayout()
</script>

<template>
  <div class="app-shell">
    <HistoryPanel
      :expanded="asideExpanded"
      :search-key="searchKey"
      :groups="groupedList"
      :active-id="historyActiveId"
      @update:search-key="(v) => (searchKey = v)"
      @select="onSelectHistory"
      @delete="onDeleteHistory"
      @new-chat="newConversation"
      @collapse="toggleAside"
    />

    <div v-if="isNarrow && asideExpanded" class="history-backdrop" @click="toggleAside()" />

    <div :ref="bindDropZone" class="chat-stage">
      <AppHeader
        :title="headerTitle"
        :domains="domains"
        :domain-id="domainId"
        :loading="loading"
        :error="error"
        :health="health"
        :show-menu="isNarrow"
        @update:domain-id="(v) => (domainId = v)"
        @refresh="refresh"
        @toggle-menu="toggleAside"
        @toggle-history="toggleAside"
      />

      <McLayout class="chat-main">
        <McLayoutContent
          v-if="startPage"
          ref="contentRef"
          class="chat-body intro-pane dc-scroll"
          :auto-scroll="false"
        >
          <div class="welcome-card">
            <McIntroduction
              :logo-img="BRAND.logo"
              :logo-width="148"
              :title="BRAND.name"
              :sub-title="greetText"
              :description="description"
            />
            <McPrompt
              v-if="introPrompt.list.length"
              :list="introPrompt.list"
              :direction="introPrompt.direction"
              class="intro-prompt"
              @item-click="(e: { label: string }) => handleSubmit(e.label)"
            />
            <p v-if="error" class="alert-banner">
              <i class="icon-info-o"></i>
              无法连接 Gateway：{{ error }}。请先运行
              <code>dotnet run</code>（DataChat.Gateway :5080）
            </p>
          </div>
        </McLayoutContent>

        <McLayoutContent
          v-else
          ref="contentRef"
          class="chat-body messages-pane dc-scroll"
          auto-scroll
          show-scroll-arrow
        >
          <div class="messages-inner">
            <template v-for="msg in session.messages" :key="msg.id">
              <McBubble
                v-if="msg.role === 'user'"
                :content="msg.content"
                align="right"
                :avatar-config="{ imgSrc: 'https://matechat.gitcode.com/png/demo/userAvatar.svg' }"
              />
              <McBubble
                v-else
                :content="msg.loading && !msg.content ? '正在思考…' : msg.content"
                :loading="msg.loading"
                :avatar-config="{ imgSrc: BRAND.logoIcon }"
              />
            </template>
          </div>
        </McLayoutContent>

        <div v-if="!startPage" class="quick-bar">
          <McPrompt
            v-if="simplePrompt.length"
            :list="simplePrompt"
            direction="horizontal"
            @item-click="(e: { label: string }) => handleSubmit(e.label)"
          />
          <button
            type="button"
            class="quick-new"
            title="新建对话"
            :disabled="sending"
            @click="newConversation"
          >
            <i class="icon-add"></i>
          </button>
        </div>

        <McLayoutSender class="composer-sender">
          <div class="composer-shell">
            <div v-if="fileItems.length" class="file-list-wrap">
              <McFileList :file-items="fileItems" context="input" @remove="onRemoveFile" />
            </div>
            <div class="composer-card">
              <McInput
                :value="inputValue"
                :placeholder="placeholder"
                :max-length="2000"
                :show-count="false"
                :disabled="sending || !!error || hasUploading"
                @change="(e: string) => (inputValue = e)"
                @submit="() => handleSubmit()"
              >
                <template #extra>
                  <div class="input-foot-wrapper">
                    <div class="input-foot-left">
                      <McAttachment
                        v-model="fileItems"
                        :upload-options="uploadOptions"
                        :max-count="5"
                        :max-size="10"
                        accept=".txt,.md,.csv,.json,.pdf,.png,.jpg,.jpeg,.webp,.doc,.docx,.xls,.xlsx"
                        :multiple="true"
                        :draggable="true"
                        :get-drop-container="getDropContainer"
                        drop-placeholder="松开鼠标即可上传文件"
                      >
                        <button
                          type="button"
                          class="foot-tool attach-tool"
                          title="上传附件"
                          :disabled="sending || !!error"
                        >
                          <i class="icon-add-file"></i>
                          <span class="foot-tool-label">附件</span>
                        </button>
                      </McAttachment>
                      <span class="input-foot-dividing-line" aria-hidden="true"></span>
                      <span class="input-foot-maxlength">{{ inputValue.length }} / 2000</span>
                    </div>
                    <div class="input-foot-right">
                      <Button
                        icon="op-clearup"
                        shape="round"
                        size="sm"
                        :disabled="!inputValue || sending"
                        @click="inputValue = ''"
                      >
                        清空
                      </Button>
                    </div>
                  </div>
                </template>
              </McInput>
            </div>
            <p class="composer-disclaimer">
              AI 生成内容可能有误，请谨慎甄别 · 会话数据仅保存在本机浏览器
            </p>
          </div>
        </McLayoutSender>
      </McLayout>
    </div>

  </div>
</template>

<style scoped>
.app-shell {
  display: flex;
  width: 100%;
  height: 100vh;
  overflow: hidden;
  background: var(--dc-bg);
}

.history-backdrop {
  position: fixed;
  inset: 0;
  z-index: 90;
  background: rgba(15, 23, 42, 0.4);
  backdrop-filter: blur(2px);
}

.chat-stage {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  height: 100%;
  background: var(--dc-bg-chat);
  box-shadow: inset 1px 0 0 var(--dc-border);
}

.chat-main {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
  gap: 0;
  background: transparent;
}

.chat-body {
  flex: 1;
  min-height: 0;
}

.intro-pane {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px 20px 32px;
}

.welcome-card {
  width: 100%;
  max-width: var(--dc-chat-max);
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 20px;
}

.intro-prompt {
  width: 100%;
}

.alert-banner {
  display: flex;
  align-items: flex-start;
  gap: 8px;
  width: 100%;
  margin: 0;
  padding: 12px 16px;
  border-radius: var(--dc-radius-md);
  background: var(--dc-error-bg);
  border: 1px solid rgba(220, 38, 38, 0.15);
  font-size: 13px;
  line-height: 1.5;
  color: var(--dc-error);
}

.alert-banner code {
  padding: 1px 6px;
  border-radius: 4px;
  background: rgba(255, 255, 255, 0.7);
  font-size: 12px;
}

.messages-pane {
  padding: 8px 0 16px;
}

.messages-inner {
  max-width: var(--dc-chat-max);
  margin: 0 auto;
  padding: 0 24px;
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.quick-bar {
  display: flex;
  align-items: center;
  gap: 10px;
  max-width: var(--dc-chat-max);
  width: 100%;
  margin: 0 auto;
  padding: 0 24px 8px;
}

.quick-new {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  border: 1px solid var(--dc-border);
  border-radius: 50%;
  background: #fff;
  color: var(--dc-brand);
  cursor: pointer;
  box-shadow: var(--dc-shadow-sm);
  transition: border-color 0.15s, box-shadow 0.15s;
}

.quick-new:hover:not(:disabled) {
  border-color: rgba(94, 124, 224, 0.4);
  box-shadow: 0 4px 12px rgba(94, 124, 224, 0.15);
}

.quick-new:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.composer-sender {
  flex-shrink: 0;
  padding: 0 20px 16px;
}

.composer-shell {
  max-width: var(--dc-chat-max);
  margin: 0 auto;
}

.file-list-wrap {
  margin-bottom: 8px;
  padding: 8px 12px;
  background: rgba(255, 255, 255, 0.7);
  border: 1px solid var(--dc-border);
  border-radius: var(--dc-radius-md);
}

.composer-card {
  padding: 4px;
  background: #fff;
  border: 1px solid var(--dc-border);
  border-radius: var(--dc-radius-lg);
  box-shadow: var(--dc-shadow-md);
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

.composer-disclaimer {
  margin: 10px 0 0;
  text-align: center;
  font-size: 11px;
  color: var(--dc-text-muted);
  letter-spacing: 0.02em;
}

@media screen and (max-width: 860px) {
  .foot-tool-label {
    display: none;
  }

  .messages-inner,
  .quick-bar {
    padding-left: 16px;
    padding-right: 16px;
  }

  .composer-sender {
    padding-left: 12px;
    padding-right: 12px;
  }
}
</style>
