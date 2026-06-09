<script lang="ts" setup>
import { ref, nextTick } from 'vue';
import type { VxeFormPropTypes } from 'vxe-pc-ui';
import { getSingle, submitData } from '#/api/agent/providerAccount';
import { $t } from '@vben/locales';

const emits = defineEmits<{ (e: 'reload'): void }>();
const reModalRef = ref();
const formRef = ref();

const providerOptions = [
  { label: 'Coze', value: 'coze' },
  { label: 'DB-GPT', value: 'dbgpt' },
  { label: 'OpenAI', value: 'openai' },
];

const defaultFormData = () => ({
  provider: 'coze',
  name: '',
  endpoint: '',
  apiKeyCiphertext: '',
  sortOrder: 0,
  enabled: true,
  remark: '',
});

const formData = ref(defaultFormData());
const formItems = ref<VxeFormPropTypes.Items>([
  {
    field: 'provider',
    title: $t('providerAccount.form.provider'),
    span: 24,
    itemRender: {
      name: '$select',
      options: providerOptions,
      props: { placeholder: $t('providerAccount.form.placeholder.provider') },
    },
  },
  {
    field: 'name',
    title: $t('providerAccount.form.name'),
    span: 24,
    itemRender: {
      name: '$input',
      props: { placeholder: $t('providerAccount.form.placeholder.name') },
    },
  },
  {
    field: 'endpoint',
    title: $t('providerAccount.form.endpoint'),
    span: 24,
    itemRender: {
      name: '$input',
      props: { placeholder: $t('providerAccount.form.placeholder.endpoint') },
    },
  },
  {
    field: 'apiKeyCiphertext',
    title: $t('providerAccount.form.apiKey'),
    span: 24,
    itemRender: {
      name: '$input',
      props: {
        type: 'password',
        placeholder: $t('providerAccount.form.placeholder.apiKey'),
      },
    },
  },
  {
    field: 'sortOrder',
    title: $t('providerAccount.form.sortOrder'),
    span: 12,
    itemRender: { name: '$input', props: { type: 'number', min: 0 } },
  },
  {
    field: 'enabled',
    title: $t('providerAccount.form.enabled'),
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
    title: $t('providerAccount.form.remark'),
    span: 24,
    itemRender: {
      name: '$textarea',
      props: { placeholder: $t('providerAccount.form.placeholder.remark') },
    },
  },
]);

const formRules = ref<VxeFormPropTypes.Rules>({
  provider: [{ required: true, message: $t('providerAccount.form.validate.provider') }],
  name: [{ required: true, message: $t('providerAccount.form.validate.name') }],
});

const showAddModal = () => {
  reModalRef.value.show($t('providerAccount.add'));
  formData.value = defaultFormData();
  nextTick(() => formRef.value?.clearValidate());
};

const showEditModal = (record: any) => {
  reModalRef.value.show(`${$t('providerAccount.edit')} -> ${record.name}`);
  nextTick(() => {
    formRef.value?.clearValidate();
    getSingle(record.id).then((data: any) => {
      const { configJson: _, ...rest } = data;
      formData.value = { ...rest, apiKeyCiphertext: '' };
    });
  });
};

const showViewModal = (record: any) => {
  reModalRef.value.show(`${$t('providerAccount.view')} -> ${record.name}`, true);
  nextTick(() => {
    formRef.value?.clearValidate();
    getSingle(record.id).then((data: any) => {
      const { configJson: _, ...rest } = data;
      formData.value = { ...rest, apiKeyCiphertext: '' };
    });
  });
};

const handleSubmit = async () => {
  const validate = await formRef.value?.validate();
  if (validate) return;
  const { configJson: _, ...payload } = formData.value;
  reModalRef.value?.setSubmitting(true);
  try {
    await submitData(payload);
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
