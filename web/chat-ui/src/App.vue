<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { ApiUnauthorizedError } from './api/http'
import AppHeader from './components/AppHeader.vue'
import LoginView from './components/LoginView.vue'
import { useAuth } from './composables/useAuth'
import AssistantMessage from './components/AssistantMessage.vue'
import ChatParamsPanel from './components/ChatParamsPanel.vue'
import ChatComposer from './components/ChatComposer.vue'
import WorkflowPendingCard from './components/WorkflowPendingCard.vue'
import HistoryPanel from './components/HistoryPanel.vue'
import MessageActions from './components/MessageActions.vue'
import { useAttachments } from './composables/useAttachments'
import { useChat } from './composables/useChat'
import { useChatParameters } from './composables/useChatParameters'
import { useDomains } from './composables/useDomains'
import { useCozeResources } from './providers'
import { useHistory } from './composables/useHistory'
import { exportSessionJson, exportSessionMarkdown, exportSessionPdf } from './utils/exportChat'
import { BRAND } from './constants/brand'
import { useLayout } from './composables/useLayout'
import { useQuoteReply } from './composables/useQuoteReply'
import {
  parseAttachmentNamesFromContent,
  stripAttachmentNote,
} from './utils/composerTokens'
import type { ChatMessage } from './types'
import type { ChatGenerationParameters } from './types/chatParams'
import {
  DcChatFooter,
  DcChatLayout,
  DcChatScroll,
  DcFileChipList,
  DcIntroduction,
  DcMessageBubble,
  DcPromptChips,
  type PromptItem,
} from './ui'

const {
  checking: authChecking,
  loggingIn: authLoggingIn,
  error: authError,
  needsLogin,
  isEmbedded,
  isAuthenticated,
  displayName,
  login: authLogin,
  logout: authLogout,
  verifySession,
} = useAuth()

const canLoadData = computed(() => isAuthenticated.value && !authChecking.value)
const { domains, cozeBots, health, loading, error, domainId, refresh } = useDomains(canLoadData)
const history = useHistory(domainId)
const noAgents = computed(() => !loading.value && !error.value && domains.value.length === 0)
const {
  searchKey,
  asideExpanded,
  groupedList,
  activeId: historyActiveId,
  toggleAside,
} = history
const activeDomain = computed(() => domains.value.find((d) => d.id === domainId.value))
const isCozeDomain = computed(() => activeDomain.value?.provider === 'coze')
const {
  workflows: cozeWorkflows,
  loading: cozeWorkflowsLoading,
  error: cozeWorkflowsError,
  refresh: refreshCozeWorkflows,
} = useCozeResources(domainId, isCozeDomain)

function onRefreshCozeWorkflows(reason: 'open' | 'manual' = 'open') {
  refreshCozeWorkflows({
    silent: reason !== 'manual',
    force: reason === 'manual',
  })
}

const slashMenuContext = computed(() => ({
  domains: domains.value,
  domainId: domainId.value,
  activeProvider: activeDomain.value?.provider,
  cozeBots: cozeBots.value,
  cozeWorkflows: cozeWorkflows.value,
  cozeWorkflowsLoading: cozeWorkflowsLoading.value,
  cozeWorkflowsError: cozeWorkflowsError.value,
}))
const attachments = useAttachments()
const { fileItems, hasUploading, getDropContainer, onHiddenFileChange, openFilePicker, addFiles } =
  attachments

function bindFileInput(el: unknown) {
  attachments.fileInputEl.value = el instanceof HTMLInputElement ? el : null
}

function bindDropZone(el: unknown) {
  attachments.dropZoneEl.value = el instanceof HTMLElement ? el : null
}

const {
  params: chatParams,
  panelOpen: paramsPanelOpen,
  applyParams: applyChatParams,
  resetDefaults: resetChatParams,
  togglePanel: toggleParamsPanel,
  closePanel: closeParamsPanel,
} = useChatParameters(domainId)

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
  markPreserveInputOnNextDomainSwitch,
  stopGenerating,
  regenerateAssistant,
  startEditUserMessage,
  cancelEditUserMessage,
  copyMessage,
  setMessageFeedback,
  canRegenerateAssistant,
  canEditUserMessage,
  isEditing,
  toast,
  showToast,
  composerFocusNonce,
  hasHistory,
  providerBanners,
  runCozeWorkflow,
} = useChat(domainId, activeDomain, history, attachments, chatParams, cozeWorkflows)

const { historyList, renameSessionTitle, togglePin } = history

const { quotePreview, applyQuote, clearQuotePreview } = useQuoteReply(
  inputValue,
  () => composerFocusNonce.value++
)

function userMessageText(msg: ChatMessage) {
  return stripAttachmentNote(msg.content)
}

function userMessageAttachments(msg: ChatMessage): { name: string }[] {
  if (msg.attachments?.length) return msg.attachments.map((a) => ({ name: a.name }))
  return parseAttachmentNamesFromContent(msg.content).map((name) => ({ name }))
}

function userMessageHasBody(msg: ChatMessage) {
  return !!userMessageText(msg).trim() || userMessageAttachments(msg).length > 0
}

function onQuoteAssistant(msg: ChatMessage) {
  const sel = window.getSelection()?.toString().trim()
  if (sel) applyQuote(sel)
  else if (msg.content.trim()) applyQuote(msg.content.trim().slice(0, 400))
}

function onComposerDomainChange(id: string) {
  markPreserveInputOnNextDomainSwitch()
  domainId.value = id
}

function onComposerNewChat() {
  const firstCoze = domains.value.find((d) => d.provider === 'coze')
  if (firstCoze && firstCoze.id !== domainId.value) {
    domainId.value = firstCoze.id
  }
  newConversation()
}

function onRunCozeWorkflow(workflowId: string) {
  runCozeWorkflow(workflowId)
}

const contentRef = ref<{ scrollToBottom?: () => void } | null>(null)

watch(
  () => session.value.messages.map((m) => m.content).join('|'),
  () => contentRef.value?.scrollToBottom?.()
)

function handleSubmit(text?: string) {
  void onSubmit(typeof text === 'string' ? text : inputValue.value)
}

function onRemoveFile(file: { uid: number }) {
  fileItems.value = fileItems.value.filter((f) => f.uid !== file.uid)
}

function onSelectHistory(id: string) {
  void loadSessionById(id)
}

async function onDeleteHistory(id: string) {
  applySession(await history.removeSession(id))
}

const headerTitle = computed(() => activeDomain.value?.displayName ?? 'DataChat')
const { isNarrow } = useLayout()

const canExportSession = computed(
  () => session.value.messages.some((m) => !m.loading && (m.content.trim() || m.thinking?.trim()))
)

const exportMeta = computed(() => {
  const meta = historyList.value.find((s) => s.id === session.value.id)
  const firstUser = session.value.messages.find((m) => m.role === 'user' && m.content.trim())
  const title =
    meta?.title?.trim() ||
    (firstUser ? firstUser.content.trim().slice(0, 48) : 'DataChat 对话')
  return {
    title,
    domainId: domainId.value,
    domainName: activeDomain.value?.displayName ?? domainId.value,
  }
})

function onExportMarkdown() {
  if (!canExportSession.value) return
  exportSessionMarkdown(session.value, exportMeta.value)
  showToast('已导出 Markdown')
}

function onExportJson() {
  if (!canExportSession.value) return
  exportSessionJson(session.value, exportMeta.value)
  showToast('已导出 JSON')
}

function onExportPdf() {
  if (!canExportSession.value) return
  try {
    exportSessionPdf(session.value, exportMeta.value)
    showToast('请在打印窗口中选择「另存为 PDF」')
  } catch (e) {
    showToast(e instanceof Error ? e.message : '导出 PDF 失败')
  }
}

function onApplyChatParams(v: ChatGenerationParameters) {
  applyChatParams(v)
  closeParamsPanel()
  showToast('生成参数已保存')
}

async function loadWorkspace() {
  try {
    await refresh()
    if (domainId.value) await history.refresh()
  } catch (e) {
    if (e instanceof ApiUnauthorizedError) {
      authLogout()
      await verifySession()
    }
  }
}

watch(canLoadData, (ready) => {
  if (ready) void loadWorkspace()
}, { immediate: true })

async function onLogin(username: string, password: string) {
  try {
    await authLogin(username, password)
    await loadWorkspace()
  } catch {
    /* error shown on LoginView */
  }
}

async function onLogout() {
  authLogout()
  await verifySession()
}
</script>

<template>
  <div v-if="authChecking" class="auth-loading">正在验证登录…</div>

  <LoginView
    v-else-if="needsLogin"
    :loading="authLoggingIn"
    :error="authError"
    @submit="onLogin"
  />

  <div v-else-if="isEmbedded && !isAuthenticated" class="auth-loading">
    等待宿主系统注入用户信息…
  </div>

  <div v-else-if="loading && domains.length === 0" class="auth-loading">
    正在加载智能体…
  </div>

  <div v-else-if="noAgents" class="auth-loading empty-agents">
    <p>当前账号未分配可用智能体，请联系管理员在「智能体授权」中配置。</p>
    <button type="button" class="empty-agents-logout" @click="onLogout">退出登录</button>
  </div>

  <div v-else class="app-shell">
    <HistoryPanel
      :expanded="asideExpanded"
      :search-key="searchKey"
      :groups="groupedList"
      :active-id="historyActiveId"
      @update:search-key="(v) => (searchKey = v)"
      @select="onSelectHistory"
      @delete="onDeleteHistory"
      @rename="(id, title) => renameSessionTitle(id, title)"
      @pin="(id) => togglePin(id)"
      @new-chat="newConversation"
      @collapse="toggleAside"
    />

    <div v-if="isNarrow && asideExpanded" class="history-backdrop" @click="toggleAside()" />

    <button
      v-if="!asideExpanded && !isNarrow"
      type="button"
      class="history-expand-rail"
      title="展开对话历史"
      aria-label="展开对话历史"
      @click="toggleAside()"
    >
      <i class="icon-list-view"></i>
      <span class="history-expand-label">历史</span>
    </button>

    <div :ref="bindDropZone" class="chat-stage">
      <AppHeader
        :title="headerTitle"
        :domains="domains"
        :domain-id="domainId"
        :active-provider="activeDomain?.provider"
        :loading="loading"
        :error="error"
        :health="health"
        :user-name="displayName"
        :show-menu="isNarrow || !asideExpanded"
        :can-export="canExportSession"
        :params-open="paramsPanelOpen"
        @update:domain-id="(v) => (domainId = v)"
        @refresh="loadWorkspace"
        @logout="onLogout"
        @toggle-menu="toggleAside"
        @toggle-history="toggleAside"
        @open-params="toggleParamsPanel"
        @export-markdown="onExportMarkdown"
        @export-json="onExportJson"
        @export-pdf="onExportPdf"
        @coze-new-chat="onComposerNewChat"
      />

      <DcChatLayout class="chat-main">
        <DcChatScroll
          v-if="startPage"
          ref="contentRef"
          class="chat-body intro-pane"
          :auto-scroll="false"
        >
          <div class="welcome-card">
            <DcIntroduction
              :logo-img="BRAND.logo"
              :logo-width="148"
              :title="BRAND.name"
              :sub-title="greetText"
              :description="description"
            />
            <DcPromptChips
              v-if="introPrompt.list.length"
              :list="introPrompt.list"
              :direction="introPrompt.direction"
              class="intro-prompt"
              @item-click="(e: PromptItem) => handleSubmit(e.label)"
            />
            <p v-if="error" class="alert-banner">
              <i class="icon-info-o"></i>
              服务暂时不可用，请
              <button class="link-btn" @click="refresh">点击重试</button>
              或联系管理员。
            </p>
          </div>
        </DcChatScroll>

        <DcChatScroll
          v-else
          ref="contentRef"
          class="chat-body messages-pane"
          auto-scroll
          show-scroll-arrow
        >
          <div class="messages-inner">
            <div v-if="session.messages.length === 0" class="compose-empty-hint">
              <template v-if="!hasHistory">
                <img class="compose-logo" :src="BRAND.logo" :alt="BRAND.name" width="120" />
                <h2 class="compose-greet">{{ greetText }}</h2>
                <p v-if="description[0]" class="compose-desc">{{ description[0] }}</p>
                <DcPromptChips
                  v-if="introPrompt.list.length"
                  :list="introPrompt.list"
                  :direction="introPrompt.direction"
                  class="compose-prompts"
                  @item-click="(e: PromptItem) => handleSubmit(e.label)"
                />
              </template>
              <p v-else class="compose-ready">新对话已就绪，在下方输入问题即可开始。</p>
              <p v-if="error" class="compose-empty-warn">
                <i class="icon-info-o"></i>
                服务暂时不可用，请
                <button class="link-btn" @click="refresh">点击重试</button>
                或联系管理员。
              </p>
            </div>
            <template v-for="(msg, msgIdx) in session.messages" :key="msg.id">
              <div v-if="msg.role === 'user'" class="message-row message-row--user">
                <div class="user-message-wrap">
                  <DcMessageBubble
                    :content="userMessageText(msg)"
                    :attachments="userMessageAttachments(msg)"
                    align="right"
                    :avatar-config="{ imgSrc: BRAND.userAvatar }"
                  />
                  <MessageActions
                    v-if="userMessageHasBody(msg)"
                    placement="toolbar"
                    :actions="['copy', 'edit']"
                    :disabled="!canEditUserMessage(msg)"
                    @copy="copyMessage(msg.id)"
                    @edit="startEditUserMessage(msg.id)"
                  />
                </div>
              </div>
              <AssistantMessage
                v-else
                :content="msg.content"
                :thinking="msg.thinking"
                :citations="msg.citations"
                :loading="msg.loading"
                :error="msg.error"
                :feedback="msg.feedback"
                :can-regenerate="canRegenerateAssistant(msg, msgIdx)"
                show-actions
                @copy="copyMessage(msg.id)"
                @quote="onQuoteAssistant(msg)"
                @regenerate="regenerateAssistant(msg.id)"
                @feedback="(r) => setMessageFeedback(msg.id, r)"
              />
            </template>
          </div>
        </DcChatScroll>

        <div v-if="!startPage && hasHistory" class="quick-bar">
          <DcPromptChips
            v-if="simplePrompt.length"
            :list="simplePrompt"
            direction="horizontal"
            @item-click="(e: PromptItem) => handleSubmit(e.label)"
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

        <DcChatFooter class="composer-sender">
          <input
            :ref="bindFileInput"
            type="file"
            class="hidden-file-input"
            multiple
            accept=".txt,.md,.csv,.json,.pdf,.png,.jpg,.jpeg,.webp,.gif,.doc,.docx,.xls,.xlsx"
            @change="onHiddenFileChange"
          />
          <div class="composer-shell">
            <div v-if="fileItems.length" class="file-list-wrap">
              <DcFileChipList :file-items="fileItems" @remove="onRemoveFile" />
            </div>
            <div v-if="quotePreview" class="quote-banner">
              <span>已引用：{{ quotePreview }}</span>
              <button type="button" class="quote-banner-clear" @click="clearQuotePreview">清除</button>
            </div>
            <template v-for="(banner, bi) in providerBanners" :key="bi">
              <div
                v-if="banner.kind === 'workflow-interrupt'"
                class="workflow-banner"
              >
                <span>
                  工作流等待回复
                  <template v-if="banner.nodeTitle">（{{ banner.nodeTitle }}）</template>
                </span>
                <button type="button" class="workflow-banner-cancel" @click="banner.onCancel">
                  取消
                </button>
              </div>
              <WorkflowPendingCard
                v-else-if="banner.kind === 'workflow-pending'"
                :workflow="banner.workflow"
                @cancel="banner.onCancel"
              />
            </template>
            <div v-if="isEditing" class="edit-banner">
              <span>正在编辑消息，发送后将从此处重新生成</span>
              <button type="button" class="edit-banner-cancel" @click="cancelEditUserMessage">
                取消
              </button>
            </div>
            <div class="composer-card">
              <ChatComposer
                v-model="inputValue"
                :placeholder="placeholder"
                :max-length="2000"
                :disabled="sending || !!error || hasUploading"
                :slash-menu-context="slashMenuContext"
                :get-drop-container="getDropContainer"
                :focus-nonce="composerFocusNonce"
                @submit="() => handleSubmit()"
                @update:domain-id="onComposerDomainChange"
                @new-chat="onComposerNewChat"
                @run-workflow="onRunCozeWorkflow"
                @refresh-workflows="onRefreshCozeWorkflows"
                @pick-files="openFilePicker"
                @drop-files="(files) => addFiles(files)"
                @paste-files="(files) => addFiles(files)"
              />
              <div v-if="sending" class="composer-stop-row">
                <button type="button" class="btn-stop" @click="stopGenerating">停止生成</button>
              </div>
            </div>
            <p class="composer-disclaimer">
              内容由 AI 生成，仅供参考
            </p>
          </div>
        </DcChatFooter>
      </DcChatLayout>
    </div>

    <ChatParamsPanel
      :open="paramsPanelOpen"
      :params="chatParams"
      @close="closeParamsPanel"
      @reset="resetChatParams"
      @apply="onApplyChatParams"
    />

    <Transition name="toast-fade">
      <div v-if="toast" class="app-toast" role="status">{{ toast }}</div>
    </Transition>
  </div>
</template>

<style scoped>
.auth-loading {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  height: 100vh;
  color: var(--dc-text-secondary);
  font-size: 14px;
}

.empty-agents {
  flex-direction: column;
  gap: 16px;
  padding: 24px;
  text-align: center;
}

.empty-agents-logout {
  padding: 8px 20px;
  border: 1px solid var(--dc-border);
  border-radius: var(--dc-radius-pill);
  background: #fff;
  color: var(--dc-text);
  font-size: 13px;
  cursor: pointer;
}

.empty-agents-logout:hover {
  border-color: rgba(94, 124, 224, 0.45);
}

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

.history-expand-rail {
  position: fixed;
  left: 0;
  top: 50%;
  z-index: 95;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  padding: 10px 6px;
  border: 1px solid var(--dc-border);
  border-left: none;
  border-radius: 0 var(--dc-radius-md) var(--dc-radius-md) 0;
  background: #fff;
  color: var(--dc-brand);
  box-shadow: var(--dc-shadow-md);
  cursor: pointer;
  transform: translateY(-50%);
  transition: background 0.15s, box-shadow 0.15s;
}

.history-expand-rail:hover {
  background: var(--dc-brand-soft);
  box-shadow: var(--dc-shadow-lg);
}

.history-expand-rail i {
  font-size: 18px;
}

.history-expand-label {
  font-size: 11px;
  font-weight: 600;
  writing-mode: vertical-rl;
  letter-spacing: 0.08em;
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

.alert-banner .link-btn {
  display: inline;
  padding: 0;
  border: none;
  background: none;
  color: var(--dc-error);
  font-size: inherit;
  font-weight: 600;
  text-decoration: underline;
  cursor: pointer;
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

.message-row {
  display: flex;
  align-items: flex-start;
  gap: 8px;
  width: 100%;
}

.message-row--user {
  justify-content: flex-end;
}

.user-message-wrap {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  max-width: min(88%, 720px);
  gap: 4px;
}

.user-message-wrap :deep(.dc-bubble-row) {
  margin: 4px 0 0;
  width: 100%;
}

.edit-banner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 8px;
  padding: 8px 12px;
  border-radius: var(--dc-radius-md);
  background: var(--dc-brand-soft);
  border: 1px solid rgba(94, 124, 224, 0.25);
  font-size: 12px;
  color: var(--dc-brand);
}

.edit-banner-cancel {
  flex-shrink: 0;
  padding: 4px 10px;
  border: 1px solid var(--dc-border);
  border-radius: var(--dc-radius-pill);
  background: #fff;
  font-size: 12px;
  cursor: pointer;
}

.edit-banner-cancel:hover {
  border-color: rgba(94, 124, 224, 0.4);
}

.workflow-banner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 8px;
  padding: 8px 12px;
  border-radius: var(--dc-radius-md);
  background: #fff7ed;
  border: 1px solid rgba(234, 88, 12, 0.25);
  font-size: 12px;
  color: #c2410c;
}

.workflow-banner-cancel {
  flex-shrink: 0;
  padding: 4px 10px;
  border: 1px solid var(--dc-border);
  border-radius: var(--dc-radius-pill);
  background: #fff;
  font-size: 12px;
  cursor: pointer;
}

.workflow-banner-cancel:hover {
  border-color: rgba(94, 124, 224, 0.4);
}

.quote-banner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 8px;
  padding: 8px 12px;
  border-radius: var(--dc-radius-md);
  background: #f0fdf4;
  border: 1px solid rgba(22, 163, 74, 0.25);
  font-size: 12px;
  color: #166534;
}

.quote-banner-clear {
  flex-shrink: 0;
  padding: 4px 10px;
  border: 1px solid var(--dc-border);
  border-radius: var(--dc-radius-pill);
  background: #fff;
  font-size: 12px;
  cursor: pointer;
}

.app-toast {
  position: fixed;
  bottom: 24px;
  left: 50%;
  transform: translateX(-50%);
  z-index: 300;
  padding: 10px 18px;
  border-radius: var(--dc-radius-pill);
  background: rgba(37, 43, 58, 0.92);
  color: #fff;
  font-size: 13px;
  box-shadow: var(--dc-shadow-lg);
  pointer-events: none;
}

.toast-fade-enter-active,
.toast-fade-leave-active {
  transition: opacity 0.2s, transform 0.2s;
}

.toast-fade-enter-from,
.toast-fade-leave-to {
  opacity: 0;
  transform: translateX(-50%) translateY(8px);
}

.compose-empty-hint {
  margin: 32px auto 24px;
  max-width: var(--dc-chat-max);
  width: 100%;
  text-align: center;
  color: var(--dc-text-secondary);
  font-size: 14px;
  line-height: 1.6;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
}

.compose-logo {
  object-fit: contain;
}

.compose-greet {
  margin: 0;
  font-size: 22px;
  font-weight: 600;
  color: var(--dc-text);
}

.compose-desc {
  margin: 0;
  max-width: 520px;
  color: var(--dc-text-secondary);
}

.compose-prompts {
  width: 100%;
  max-width: 720px;
}

.compose-ready {
  margin: 24px 0 0;
  font-size: 15px;
  color: var(--dc-text-secondary);
}

.compose-empty-warn {
  color: var(--dc-error);
  font-size: 13px;
}

.compose-empty-warn .link-btn {
  display: inline;
  padding: 0;
  border: none;
  background: none;
  color: var(--dc-error);
  font-size: inherit;
  font-weight: 600;
  text-decoration: underline;
  cursor: pointer;
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

.hidden-file-input {
  position: absolute;
  width: 0;
  height: 0;
  opacity: 0;
  pointer-events: none;
}

.composer-stop-row {
  display: flex;
  justify-content: center;
  margin-top: 8px;
}

.btn-stop {
  padding: 6px 16px;
  border: 1px solid rgba(220, 38, 38, 0.35);
  border-radius: var(--dc-radius-pill);
  background: var(--dc-error-bg);
  color: var(--dc-error);
  font-size: 12px;
  font-weight: 600;
  cursor: pointer;
}

.btn-stop:hover {
  background: #fee2e2;
}

.composer-disclaimer {
  margin: 10px 0 0;
  text-align: center;
  font-size: 11px;
  color: var(--dc-text-muted);
  letter-spacing: 0.02em;
}

@media screen and (max-width: 860px) {
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
