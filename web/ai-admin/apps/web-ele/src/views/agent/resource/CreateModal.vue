<script lang="ts" setup>
import { ref, nextTick, onMounted } from 'vue';
import type { VxeFormPropTypes } from 'vxe-pc-ui';
import { getSingle, submitData } from '#/api/agent/agentResource';
import { getList as getAgents } from '#/api/agent/agent';
import { $t } from '@vben/locales';

const emits = defineEmits<{ (e: 'reload'): void }>();
const reModalRef = ref();
const formRef = ref();

const agentOptions = ref<{ label: string; value: string }[]>([]);
const resourceTypeOptions = [
  { label: 'workflow', value: 'workflow' },
  { label: 'tool', value: 'tool' },
  { label: 'skill', value: 'skill' },
];

onMounted(async () => {
  const list = (await getAgents()) as any[];
  agentOptions.value = list.map((x) => ({
    label: `${x.displayName} (${x.id})`,
    value: x.id,
  }));
});

const defaultFormData = () => ({
  agentId: '',
  resourceType: 'workflow',
  externalId: '',
  displayName: '',
  description: '',
  configJson: '{"inputParameter":"BOT_USER_INPUT","inputHint":""}',
  sortOrder: 0,
  enabled: true,
  remark: '',
});

const formData = ref(defaultFormData());
const formItems = ref<VxeFormPropTypes.Items>([
  {
    field: 'agentId',
    title: $t('agentResource.form.agent'),
    span: 24,
    itemRender: {
      name: '$select',
      options: agentOptions,
      props: { placeholder: $t('agentResource.form.placeholder.agent') },
    },
  },
  {
    field: 'resourceType',
    title: $t('agentResource.form.resourceType'),
    span: 12,
    itemRender: { name: '$select', options: resourceTypeOptions },
  },
  {
    field: 'externalId',
    title: $t('agentResource.form.externalId'),
    span: 12,
    itemRender: {
      name: '$input',
      props: { placeholder: $t('agentResource.form.placeholder.externalId') },
    },
  },
  {
    field: 'displayName',
    title: $t('agentResource.form.displayName'),
    span: 24,
    itemRender: { name: '$input' },
  },
  {
    field: 'description',
    title: $t('agentResource.form.description'),
    span: 24,
    itemRender: { name: '$textarea', props: { rows: 2 } },
  },
  {
    field: 'configJson',
    title: $t('agentResource.form.configJson'),
    span: 24,
    itemRender: { name: '$textarea', props: { rows: 4 } },
  },
  {
    field: 'sortOrder',
    title: $t('agentResource.form.sortOrder'),
    span: 12,
    itemRender: { name: '$input', props: { type: 'number', min: 0 } },
  },
  {
    field: 'enabled',
    title: $t('agentResource.form.enabled'),
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
    title: $t('agentResource.form.remark'),
    span: 24,
    itemRender: { name: '$textarea' },
  },
]);

const formRules = ref<VxeFormPropTypes.Rules>({
  agentId: [{ required: true, message: $t('agentResource.form.validate.agent') }],
  resourceType: [{ required: true, message: $t('agentResource.form.validate.resourceType') }],
  externalId: [{ required: true, message: $t('agentResource.form.validate.externalId') }],
});

const showAddModal = () => {
  reModalRef.value.show($t('agentResource.add'));
  formData.value = defaultFormData();
  nextTick(() => formRef.value?.clearValidate());
};

const showEditModal = (record: any) => {
  reModalRef.value.show(`${$t('agentResource.edit')} -> ${record.displayName ?? record.externalId}`);
  nextTick(() => {
    formRef.value?.clearValidate();
    getSingle(record.id).then((data: any) => {
      formData.value = data;
    });
  });
};

const showViewModal = (record: any) => {
  reModalRef.value.show(`${$t('agentResource.view')} -> ${record.displayName ?? record.externalId}`, true);
  nextTick(() => {
    formRef.value?.clearValidate();
    getSingle(record.id).then((data: any) => {
      formData.value = data;
    });
  });
};

const handleSubmit = async () => {
  const validate = await formRef.value?.validate();
  if (validate) return;
  reModalRef.value?.setSubmitting(true);
  try {
    await submitData(formData.value);
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
      :titleWidth="110"
      :titleColon="true"
      :titleAlign="`right`"
    />
  </re-modal>
</template>
