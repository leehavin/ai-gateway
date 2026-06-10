<script lang="ts" setup>
import { ref, nextTick, watch, computed } from 'vue';
import type { VxeFormPropTypes } from 'vxe-pc-ui';
import { getSingle, submitData } from '#/api/agent/agent';
import { getList as getProviderAccounts } from '#/api/agent/providerAccount';
import {
  getWorkspaces,
  getBots,
} from '#/api/agent/cozeDiscovery';
import { $t } from '@vben/locales';
import { ElMessage } from 'element-plus';
import AgentWorkflowPanel from './AgentWorkflowPanel.vue';

const emits = defineEmits<{ (e: 'reload'): void }>();
const reDrawerRef = ref();
const formRef = ref();
const workflowPanelRef = ref<HTMLElement>();
const editId = ref<string>();
const viewMode = ref(false);

const providerOptions = [
  { label: 'Coze', value: 'coze' },
  { label: 'DB-GPT', value: 'dbgpt' },
  { label: 'OpenAI', value: 'openai' },
];

const chatModePresets: Record<string, string> = {
  coze: 'CozeAgent',
  dbgpt: 'DbGptData',
  openai: 'OpenAiChat',
};

const configHints: Record<string, string> = {
  dbgpt: '{"chatMode":"chat_data","datasourceId":""}',
  openai: '{"baseUrl":"","model":""}',
};

/** Coze 拉取工作流列表固定使用已发布线上版，不对客户暴露 */
const COZE_LIST_PUBLISH_STATUS = 'published_online';

type CozeFormConfig = {
  botId: string;
  workspaceId: string;
  autoSaveHistory: boolean;
  userIdPrefix: string;
};

const defaultCozeConfig = (): CozeFormConfig => ({
  botId: '',
  workspaceId: '',
  autoSaveHistory: true,
  userIdPrefix: 'user',
});

const parseCozeConfig = (json?: string): CozeFormConfig => {
  try {
    const o = JSON.parse(json || '{}');
    return {
      botId: o.botId ?? '',
      workspaceId: o.workspaceId ?? '',
      autoSaveHistory: o.autoSaveHistory !== false,
      userIdPrefix: o.userIdPrefix ?? 'user',
    };
  } catch {
    return defaultCozeConfig();
  }
};

const serializeCozeConfig = (c: CozeFormConfig) =>
  JSON.stringify({
    botId: c.botId.trim(),
    workspaceId: c.workspaceId.trim(),
    autoSaveHistory: c.autoSaveHistory,
    userIdPrefix: c.userIdPrefix.trim() || 'user',
    listPublishStatus: COZE_LIST_PUBLISH_STATUS,
  });

const accountOptions = ref<{ label: string; value: number }[]>([]);
const workspaceOptions = ref<{ label: string; value: string }[]>([]);
const botOptions = ref<{ label: string; value: string; description?: string }[]>([]);
const discoveryLoading = ref(false);

const loadAccounts = async (provider: string) => {
  if (!provider) {
    accountOptions.value = [];
    return;
  }
  const list = (await getProviderAccounts(provider)) as any[];
  accountOptions.value = list.map((x) => ({
    label: x.name,
    value: x.id,
  }));
};

const formatBotLabel = (bot: { name?: string; botId: string; isPublished?: boolean }) => {
  const base = bot.name ? `${bot.name} (${bot.botId})` : bot.botId;
  return bot.isPublished === false ? `${base} [${$t('agentManage.discovery.draft')}]` : base;
};

const loadWorkspaces = async (accountId?: number | null) => {
  workspaceOptions.value = [];
  botOptions.value = [];
  if (!accountId) return;
  discoveryLoading.value = true;
  try {
    const list = (await getWorkspaces(accountId)) as any[];
    workspaceOptions.value = list.map((x) => ({
      label: x.name ? `${x.name} (${x.id})` : x.id,
      value: x.id,
    }));
    if (!list.length) {
      ElMessage.warning($t('agentManage.discovery.emptyWorkspace'));
    }
  } catch (error: any) {
    ElMessage.error(error?.message ?? $t('agentManage.discovery.loadFailed'));
  } finally {
    discoveryLoading.value = false;
  }
};

const loadBots = async (accountId?: number | null, spaceId?: string) => {
  botOptions.value = [];
  if (!accountId || !spaceId) return;
  discoveryLoading.value = true;
  try {
    const list = (await getBots(accountId, spaceId)) as any[];
    botOptions.value = list.map((x) => ({
      label: formatBotLabel(x),
      value: x.botId,
      description: x.description,
    }));
    if (!list.length) {
      ElMessage.warning($t('agentManage.discovery.emptyBot'));
    }
  } catch (error: any) {
    ElMessage.error(error?.message ?? $t('agentManage.discovery.loadFailed'));
  } finally {
    discoveryLoading.value = false;
  }
};

const defaultFormData = () => ({
  id: '',
  displayName: '',
  provider: 'coze',
  chatMode: chatModePresets.coze,
  providerAccountId: null as number | null,
  model: '',
  configJson: '',
  cozeConfig: defaultCozeConfig(),
  placeholder: '',
  quickPromptsJson: '',
  systemPrompt: '',
  sortOrder: 0,
  enabled: true,
  remark: '',
});

const formData = ref(defaultFormData());

watch(
  () => formData.value.provider,
  (provider) => {
    if (!editId.value) {
      formData.value.chatMode = chatModePresets[provider] ?? '';
      formData.value.configJson = configHints[provider] ?? '{}';
      if (provider === 'coze') {
        formData.value.cozeConfig = defaultCozeConfig();
      }
      formData.value.providerAccountId = null;
    }
    workspaceOptions.value = [];
    botOptions.value = [];
    loadAccounts(provider);
  },
);

watch(
  () => formData.value.providerAccountId,
  async (accountId) => {
    if (formData.value.provider !== 'coze') return;
    if (!editId.value) {
      formData.value.cozeConfig.workspaceId = '';
      formData.value.cozeConfig.botId = '';
    }
    await loadWorkspaces(accountId);
    if (formData.value.cozeConfig.workspaceId) {
      await loadBots(accountId, formData.value.cozeConfig.workspaceId);
    }
  },
);

watch(
  () => formData.value.cozeConfig.workspaceId,
  async (spaceId) => {
    if (formData.value.provider !== 'coze') return;
    if (!editId.value) {
      formData.value.cozeConfig.botId = '';
    }
    await loadBots(formData.value.providerAccountId, spaceId);
  },
);

watch(
  () => formData.value.cozeConfig.botId,
  (botId) => {
    if (!botId || formData.value.provider !== 'coze') return;
    const bot = botOptions.value.find((x) => x.value === botId);
    if (bot && !formData.value.displayName) {
      formData.value.displayName = bot.label.split(' (')[0] ?? bot.label;
    }
  },
);

const baseFormItems = computed<VxeFormPropTypes.Items>(() => [
  {
    field: 'id',
    title: $t('agentManage.form.id'),
    span: 24,
    itemRender: {
      name: '$input',
      props: { placeholder: $t('agentManage.form.placeholder.id'), disabled: !!editId.value },
    },
  },
  {
    field: 'displayName',
    title: $t('agentManage.form.displayName'),
    span: 24,
    itemRender: {
      name: '$input',
      props: { placeholder: $t('agentManage.form.placeholder.displayName') },
    },
  },
  {
    field: 'provider',
    title: $t('agentManage.form.provider'),
    span: 12,
    itemRender: { name: '$select', options: providerOptions },
  },
  {
    field: 'chatMode',
    title: $t('agentManage.form.chatMode'),
    span: 12,
    itemRender: { name: '$input' },
  },
  {
    field: 'providerAccountId',
    title: $t('agentManage.form.providerAccount'),
    span: 24,
    itemRender: {
      name: '$select',
      options: accountOptions.value,
      props: {
        clearable: true,
        placeholder: $t('agentManage.form.placeholder.providerAccount'),
      },
    },
  },
]);

const cozeFormItems = computed<VxeFormPropTypes.Items>(() => [
  {
    field: 'cozeConfig.workspaceId',
    title: $t('agentManage.form.coze.workspace'),
    span: 24,
    itemRender: {
      name: '$select',
      options: workspaceOptions.value,
      props: {
        clearable: true,
        placeholder: $t('agentManage.form.placeholder.coze.workspace'),
        loading: discoveryLoading.value,
      },
    },
  },
  {
    field: 'cozeConfig.botId',
    title: $t('agentManage.form.coze.bot'),
    span: 24,
    itemRender: {
      name: '$select',
      options: botOptions.value,
      props: {
        clearable: true,
        placeholder: $t('agentManage.form.placeholder.coze.bot'),
        loading: discoveryLoading.value,
      },
    },
  },
  {
    field: 'cozeConfig.userIdPrefix',
    title: $t('agentManage.form.coze.userIdPrefix'),
    span: 12,
    itemRender: { name: '$input' },
  },
  {
    field: 'cozeConfig.autoSaveHistory',
    title: $t('agentManage.form.coze.autoSaveHistory'),
    span: 12,
    itemRender: {
      name: '$select',
      options: [
        { label: $t('common.enable'), value: true },
        { label: $t('common.disable'), value: false },
      ],
    },
  },
]);

const otherConfigItem = computed<VxeFormPropTypes.Items>(() => [
  {
    field: 'configJson',
    title: $t('agentManage.form.configJson'),
    span: 24,
    itemRender: {
      name: '$textarea',
      props: { rows: 6, placeholder: $t('agentManage.form.placeholder.configJson') },
    },
  },
]);

const tailFormItems = computed<VxeFormPropTypes.Items>(() => [
  ...(formData.value.provider !== 'coze'
    ? [{ field: 'model', title: $t('agentManage.form.model'), span: 24, itemRender: { name: '$input' } }]
    : []),
  {
    field: 'placeholder',
    title: $t('agentManage.form.placeholderText'),
    span: 24,
    itemRender: { name: '$input' },
  },
  {
    field: 'quickPromptsJson',
    title: $t('agentManage.form.quickPrompts'),
    span: 24,
    itemRender: {
      name: '$textarea',
      props: { rows: 2, placeholder: '["快捷问题1","快捷问题2"]' },
    },
  },
  {
    field: 'systemPrompt',
    title: $t('agentManage.form.systemPrompt'),
    span: 24,
    itemRender: { name: '$textarea', props: { rows: 3 } },
  },
  {
    field: 'sortOrder',
    title: $t('agentManage.form.sortOrder'),
    span: 12,
    itemRender: { name: '$input', props: { type: 'number', min: 0 } },
  },
  {
    field: 'enabled',
    title: $t('agentManage.form.enabled'),
    span: 12,
    itemRender: {
      name: '$select',
      options: [
        { label: $t('common.enable'), value: true },
        { label: $t('common.disable'), value: false },
      ],
    },
  },
  {
    field: 'remark',
    title: $t('agentManage.form.remark'),
    span: 24,
    itemRender: { name: '$textarea' },
  },
]);

const formItems = computed<VxeFormPropTypes.Items>(() => [
  ...baseFormItems.value,
  ...(formData.value.provider === 'coze' ? cozeFormItems.value : otherConfigItem.value),
  ...tailFormItems.value,
]);

const formRules = computed<VxeFormPropTypes.Rules>(() => {
  const rules: VxeFormPropTypes.Rules = {
    id: [{ required: true, message: $t('agentManage.form.validate.id') }],
    displayName: [{ required: true, message: $t('agentManage.form.validate.displayName') }],
    provider: [{ required: true, message: $t('agentManage.form.validate.provider') }],
    chatMode: [{ required: true, message: $t('agentManage.form.validate.chatMode') }],
  };
  if (formData.value.provider === 'coze') {
    rules.providerAccountId = [
      { required: true, message: $t('agentManage.form.validate.providerAccount') },
    ];
    rules['cozeConfig.workspaceId'] = [
      { required: true, message: $t('agentManage.form.validate.coze.workspace') },
    ];
    rules['cozeConfig.botId'] = [
      { required: true, message: $t('agentManage.form.validate.coze.bot') },
    ];
  } else {
    rules.configJson = [{ required: true, message: $t('agentManage.form.validate.configJson') }];
  }
  return rules;
});

const mapRecordToForm = (data: any) => ({
  ...data,
  cozeConfig:
    data.provider === 'coze' ? parseCozeConfig(data.configJson) : defaultCozeConfig(),
});

const hydrateCozeDiscovery = async () => {
  if (formData.value.provider !== 'coze' || !formData.value.providerAccountId) return;
  await loadWorkspaces(formData.value.providerAccountId);
  if (formData.value.cozeConfig.workspaceId) {
    await loadBots(
      formData.value.providerAccountId,
      formData.value.cozeConfig.workspaceId,
    );
  }
};

const buildSubmitPayload = () => {
  const { cozeConfig, ...rest } = formData.value;
  const payload: Record<string, unknown> = { ...rest };
  if (formData.value.provider === 'coze') {
    payload.configJson = serializeCozeConfig(cozeConfig);
  }
  return payload;
};

const showAddModal = () => {
  editId.value = undefined;
  viewMode.value = false;
  reDrawerRef.value.show($t('agentManage.add'));
  formData.value = defaultFormData();
  loadAccounts(formData.value.provider);
  nextTick(() => formRef.value?.clearValidate());
};

const showEditModal = (record: any) => {
  editId.value = record.id;
  viewMode.value = false;
  reDrawerRef.value.show(`${$t('agentManage.edit')} -> ${record.displayName}`);
  nextTick(() => {
    formRef.value?.clearValidate();
    getSingle(record.id).then(async (data: any) => {
      formData.value = mapRecordToForm(data);
      await loadAccounts(data.provider);
      await hydrateCozeDiscovery();
    });
  });
};

const showViewModal = (record: any) => {
  editId.value = record.id;
  viewMode.value = true;
  reDrawerRef.value.show(`${$t('agentManage.view')} -> ${record.displayName}`, true);
  nextTick(() => {
    formRef.value?.clearValidate();
    getSingle(record.id).then(async (data: any) => {
      formData.value = mapRecordToForm(data);
      await loadAccounts(data.provider);
      await hydrateCozeDiscovery();
    });
  });
};

const scrollToWorkflowPanel = () => {
  workflowPanelRef.value?.scrollIntoView({ behavior: 'smooth', block: 'start' });
};

const handleSubmit = async () => {
  const validate = await formRef.value?.validate();
  if (validate) return;
  reDrawerRef.value?.setSubmitting(true);
  try {
    const isNewCoze = !editId.value && formData.value.provider === 'coze';
    const result = await submitData(buildSubmitPayload(), editId.value);
    emits('reload');
    if (isNewCoze && typeof result === 'string') {
      editId.value = result;
      reDrawerRef.value?.setTitle(
        `${$t('agentManage.edit')} -> ${formData.value.displayName}`,
      );
      ElMessage.success($t('agentManage.coze.savedContinueWorkflow'));
      await nextTick();
      scrollToWorkflowPanel();
      return;
    }
    reDrawerRef.value.close();
  } finally {
    reDrawerRef.value?.setSubmitting(false);
  }
};

defineExpose({ showAddModal, showEditModal, showViewModal });
</script>

<template>
  <re-drawer ref="reDrawerRef" size="760px" @submit="handleSubmit">
    <el-alert
      v-if="formData.provider === 'coze' && !viewMode"
      type="info"
      :closable="false"
      show-icon
      class="mb-4"
    >
      <template #title>{{ $t('agentManage.form.coze.guideTitle') }}</template>
      <ul class="coze-guide-list">
        <li>{{ $t('agentManage.form.coze.guideBot') }}</li>
        <li>{{ $t('agentManage.form.coze.guideWorkflow') }}</li>
        <li>{{ $t('agentManage.form.coze.guideSkillTool') }}</li>
      </ul>
    </el-alert>
    <vxe-form
      ref="formRef"
      :data="formData"
      :items="formItems"
      :rules="formRules"
      :titleWidth="120"
      :titleColon="true"
      :titleAlign="`right`"
    />
    <div ref="workflowPanelRef">
      <AgentWorkflowPanel
        :agent-id="editId"
        :provider="formData.provider"
        :provider-account-id="formData.providerAccountId"
        :coze-config="formData.cozeConfig"
        :readonly="viewMode"
      />
    </div>
  </re-drawer>
</template>

<style scoped>
.coze-guide-list {
  margin: 0;
  padding-left: 1.1rem;
  font-size: 13px;
  line-height: 1.65;
}

.coze-guide-list li + li {
  margin-top: 0.35rem;
}
</style>
