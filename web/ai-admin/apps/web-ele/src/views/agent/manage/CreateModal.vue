<script lang="ts" setup>
import { ref, nextTick, watch, computed } from 'vue';
import type { VxeFormPropTypes } from 'vxe-pc-ui';
import { getSingle, submitData } from '#/api/agent/agent';
import { getList as getProviderAccounts } from '#/api/agent/providerAccount';
import { $t } from '@vben/locales';

const emits = defineEmits<{ (e: 'reload'): void }>();
const reModalRef = ref();
const formRef = ref();
const editId = ref<string>();

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

const listPublishStatusOptions = [
  { label: 'published_online', value: 'published_online' },
  { label: 'published_draft', value: 'published_draft' },
  { label: 'all', value: 'all' },
];

type CozeFormConfig = {
  botId: string;
  workspaceId: string;
  autoSaveHistory: boolean;
  userIdPrefix: string;
  listPublishStatus: string;
};

const defaultCozeConfig = (): CozeFormConfig => ({
  botId: '',
  workspaceId: '',
  autoSaveHistory: true,
  userIdPrefix: 'user',
  listPublishStatus: 'published_online',
});

const parseCozeConfig = (json?: string): CozeFormConfig => {
  try {
    const o = JSON.parse(json || '{}');
    return {
      botId: o.botId ?? '',
      workspaceId: o.workspaceId ?? '',
      autoSaveHistory: o.autoSaveHistory !== false,
      userIdPrefix: o.userIdPrefix ?? 'user',
      listPublishStatus: o.listPublishStatus ?? 'published_online',
    };
  } catch {
    return defaultCozeConfig();
  }
};

const serializeCozeConfig = (c: CozeFormConfig) =>
  JSON.stringify({
    botId: c.botId.trim(),
    ...(c.workspaceId.trim() ? { workspaceId: c.workspaceId.trim() } : {}),
    autoSaveHistory: c.autoSaveHistory,
    userIdPrefix: c.userIdPrefix.trim() || 'user',
    listPublishStatus: c.listPublishStatus || 'published_online',
  });

const accountOptions = ref<{ label: string; value: number }[]>([]);

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
    loadAccounts(provider);
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
      props: { clearable: true, placeholder: $t('agentManage.form.placeholder.providerAccount') },
    },
  },
]);

const cozeFormItems = computed<VxeFormPropTypes.Items>(() => [
  {
    field: 'cozeConfig.botId',
    title: $t('agentManage.form.coze.botId'),
    span: 24,
    itemRender: {
      name: '$input',
      props: { placeholder: $t('agentManage.form.placeholder.coze.botId') },
    },
  },
  {
    field: 'cozeConfig.workspaceId',
    title: $t('agentManage.form.coze.workspaceId'),
    span: 24,
    itemRender: {
      name: '$input',
      props: { placeholder: $t('agentManage.form.placeholder.coze.workspaceId') },
    },
  },
  {
    field: 'cozeConfig.userIdPrefix',
    title: $t('agentManage.form.coze.userIdPrefix'),
    span: 12,
    itemRender: { name: '$input' },
  },
  {
    field: 'cozeConfig.listPublishStatus',
    title: $t('agentManage.form.coze.listPublishStatus'),
    span: 12,
    itemRender: { name: '$select', options: listPublishStatusOptions },
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
    rules['cozeConfig.botId'] = [
      { required: true, message: $t('agentManage.form.validate.coze.botId') },
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
  reModalRef.value.show($t('agentManage.add'));
  formData.value = defaultFormData();
  loadAccounts(formData.value.provider);
  nextTick(() => formRef.value?.clearValidate());
};

const showEditModal = (record: any) => {
  editId.value = record.id;
  reModalRef.value.show(`${$t('agentManage.edit')} -> ${record.displayName}`);
  nextTick(() => {
    formRef.value?.clearValidate();
    getSingle(record.id).then((data: any) => {
      formData.value = mapRecordToForm(data);
      loadAccounts(data.provider);
    });
  });
};

const showViewModal = (record: any) => {
  editId.value = record.id;
  reModalRef.value.show(`${$t('agentManage.view')} -> ${record.displayName}`, true);
  nextTick(() => {
    formRef.value?.clearValidate();
    getSingle(record.id).then((data: any) => {
      formData.value = mapRecordToForm(data);
      loadAccounts(data.provider);
    });
  });
};

const handleSubmit = async () => {
  const validate = await formRef.value?.validate();
  if (validate) return;
  reModalRef.value?.setSubmitting(true);
  try {
    await submitData(buildSubmitPayload(), editId.value);
    emits('reload');
    reModalRef.value.close();
  } finally {
    reModalRef.value?.setSubmitting(false);
  }
};

defineExpose({ showAddModal, showEditModal, showViewModal });
</script>

<template>
  <re-modal ref="reModalRef" @submit="handleSubmit">
    <vxe-form
      ref="formRef"
      :data="formData"
      :items="formItems"
      :rules="formRules"
      :titleWidth="120"
      :titleColon="true"
      :titleAlign="`right`"
    />
  </re-modal>
</template>
