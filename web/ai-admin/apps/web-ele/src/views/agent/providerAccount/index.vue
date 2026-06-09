<script lang="ts" setup>
import { reactive, ref } from 'vue';
import { getPageList, deleteData } from '#/api/agent/providerAccount';
import CreateModal from './CreateModal.vue';
import { deleteConfirm } from '#/components/modal';
import { $t } from '@vben/locales';
import type { CommonOperationType } from '#/components/grid';

const reVxeGridRef = ref();
const columns = [
  { type: 'checkbox', title: '', width: 60, align: 'center' },
  { title: $t('providerAccount.columns.name'), field: 'name', minWidth: 140 },
  { title: $t('providerAccount.columns.provider'), field: 'provider', width: 100 },
  { title: $t('providerAccount.columns.endpoint'), field: 'endpoint', minWidth: 180 },
  {
    title: $t('providerAccount.columns.hasApiKey'),
    field: 'hasApiKey',
    width: 100,
    formatter: ({ cellValue }: { cellValue: boolean }) =>
      cellValue ? '✓' : '-',
  },
  { title: $t('providerAccount.columns.sortOrder'), field: 'sortOrder', width: 80 },
  {
    title: $t('providerAccount.columns.enabled'),
    field: 'enabled',
    width: 80,
    formatter: ({ cellValue }: { cellValue: boolean }) =>
      cellValue ? $t('common.enable') : $t('common.disable'),
  },
  { title: $t('providerAccount.columns.remark'), field: 'remark', minWidth: 160 },
];

const providerOptions = [
  { label: 'Coze', value: 'coze' },
  { label: 'DB-GPT', value: 'dbgpt' },
  { label: 'OpenAI', value: 'openai' },
];

const handleInitialFormParams = () => ({
  name: '',
  provider: '',
});
const formData = reactive(handleInitialFormParams());
const formItems = [
  {
    field: 'name',
    title: $t('providerAccount.search.name'),
    span: 6,
    itemRender: {
      name: '$input',
      props: { placeholder: $t('providerAccount.search.name') },
    },
  },
  {
    field: 'provider',
    title: $t('providerAccount.search.provider'),
    span: 6,
    itemRender: {
      name: '$select',
      options: providerOptions,
      props: { placeholder: $t('providerAccount.search.provider'), clearable: true },
    },
  },
];

const handleSearch = () => reVxeGridRef.value.loadData();
const searchOptions = {
  formData,
  formItems,
  submit: handleSearch,
  reset: handleInitialFormParams,
};

const createModalRef = ref();
const commonOperation: CommonOperationType = {
  add: {
    permissionCode: 'agent.provider.add',
    handleClick: () => createModalRef.value.showAddModal(),
  },
  view: {
    permissionCode: 'agent.provider.view',
    handleClick: (record: any) => createModalRef.value.showViewModal(record),
  },
  edit: {
    permissionCode: 'agent.provider.edit',
    handleClick: (record: any) => createModalRef.value.showEditModal(record),
  },
  delete: {
    permissionCode: 'agent.provider.delete',
    handleClick: async (record: any) => {
      if (await deleteConfirm()) {
        await deleteData(record.id);
        handleSearch();
      }
    },
  },
};
</script>

<template>
  <re-page>
    <re-vxe-grid
      ref="reVxeGridRef"
      :request="getPageList"
      :commonOperation="commonOperation"
      :columns="columns"
      :searchOptions="searchOptions"
    />
    <CreateModal ref="createModalRef" @reload="handleSearch" />
  </re-page>
</template>
