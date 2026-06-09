<script lang="ts" setup>
import { reactive, ref } from 'vue';
import { getPageList, deleteData } from '#/api/agent/agent';
import CreateModal from './CreateModal.vue';
import { deleteConfirm } from '#/components/modal';
import { $t } from '@vben/locales';
import type { CommonOperationType } from '#/components/grid';

const reVxeGridRef = ref();
const columns = [
  { type: 'checkbox', title: '', width: 60, align: 'center' },
  { title: $t('agentManage.columns.id'), field: 'id', minWidth: 120 },
  { title: $t('agentManage.columns.displayName'), field: 'displayName', minWidth: 140 },
  { title: $t('agentManage.columns.provider'), field: 'provider', width: 90 },
  { title: $t('agentManage.columns.chatMode'), field: 'chatMode', width: 110 },
  {
    title: $t('agentManage.columns.providerAccount'),
    field: 'providerAccountName',
    minWidth: 120,
  },
  { title: $t('agentManage.columns.model'), field: 'model', minWidth: 120 },
  { title: $t('agentManage.columns.sortOrder'), field: 'sortOrder', width: 80 },
  {
    title: $t('agentManage.columns.enabled'),
    field: 'enabled',
    width: 80,
    formatter: ({ cellValue }: { cellValue: boolean }) =>
      cellValue ? $t('common.enable') : $t('common.disable'),
  },
];

const providerOptions = [
  { label: 'Coze', value: 'coze' },
  { label: 'DB-GPT', value: 'dbgpt' },
  { label: 'OpenAI', value: 'openai' },
];

const handleInitialFormParams = () => ({
  displayName: '',
  provider: '',
  enabled: undefined as boolean | undefined,
});
const formData = reactive(handleInitialFormParams());
const formItems = [
  {
    field: 'displayName',
    title: $t('agentManage.search.displayName'),
    span: 6,
    itemRender: {
      name: '$input',
      props: { placeholder: $t('agentManage.search.displayName') },
    },
  },
  {
    field: 'provider',
    title: $t('agentManage.search.provider'),
    span: 6,
    itemRender: {
      name: '$select',
      options: providerOptions,
      props: { placeholder: $t('agentManage.search.provider'), clearable: true },
    },
  },
  {
    field: 'enabled',
    title: $t('agentManage.search.enabled'),
    span: 6,
    itemRender: {
      name: '$select',
      options: [
        { label: $t('common.enable'), value: true },
        { label: $t('common.disable'), value: false },
      ],
      props: { clearable: true },
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
    permissionCode: 'agent.manage.add',
    handleClick: () => createModalRef.value.showAddModal(),
  },
  view: {
    permissionCode: 'agent.manage.view',
    handleClick: (record: any) => createModalRef.value.showViewModal(record),
  },
  edit: {
    permissionCode: 'agent.manage.edit',
    handleClick: (record: any) => createModalRef.value.showEditModal(record),
  },
  delete: {
    permissionCode: 'agent.manage.delete',
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
