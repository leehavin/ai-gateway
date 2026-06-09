<script lang="ts" setup>
import { reactive, ref } from 'vue';
import { getPageList, deleteData } from '#/api/agent/agentResource';
import CreateModal from './CreateModal.vue';
import { deleteConfirm } from '#/components/modal';
import { $t } from '@vben/locales';
import type { CommonOperationType } from '#/components/grid';

const reVxeGridRef = ref();
const columns = [
  { type: 'checkbox', title: '', width: 60, align: 'center' },
  { title: $t('agentResource.columns.agent'), field: 'agentDisplayName', minWidth: 120 },
  { title: $t('agentResource.columns.agentId'), field: 'agentId', minWidth: 110 },
  { title: $t('agentResource.columns.resourceType'), field: 'resourceType', width: 100 },
  { title: $t('agentResource.columns.externalId'), field: 'externalId', minWidth: 160 },
  { title: $t('agentResource.columns.displayName'), field: 'displayName', minWidth: 140 },
  { title: $t('agentResource.columns.sortOrder'), field: 'sortOrder', width: 80 },
  {
    title: $t('agentResource.columns.enabled'),
    field: 'enabled',
    width: 80,
    formatter: ({ cellValue }: { cellValue: boolean }) =>
      cellValue ? $t('common.enable') : $t('common.disable'),
  },
];

const resourceTypeOptions = [
  { label: 'workflow', value: 'workflow' },
  { label: 'tool', value: 'tool' },
  { label: 'skill', value: 'skill' },
];

const handleInitialFormParams = () => ({
  agentId: '',
  displayName: '',
  resourceType: '',
});
const formData = reactive(handleInitialFormParams());
const formItems = [
  {
    field: 'agentId',
    title: $t('agentResource.search.agentId'),
    span: 6,
    itemRender: {
      name: '$input',
      props: { placeholder: $t('agentResource.search.agentId') },
    },
  },
  {
    field: 'displayName',
    title: $t('agentResource.search.displayName'),
    span: 6,
    itemRender: {
      name: '$input',
      props: { placeholder: $t('agentResource.search.displayName') },
    },
  },
  {
    field: 'resourceType',
    title: $t('agentResource.search.resourceType'),
    span: 6,
    itemRender: {
      name: '$select',
      options: resourceTypeOptions,
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
    permissionCode: 'agent.resource.add',
    handleClick: () => createModalRef.value.showAddModal(),
  },
  view: {
    permissionCode: 'agent.resource.view',
    handleClick: (record: any) => createModalRef.value.showViewModal(record),
  },
  edit: {
    permissionCode: 'agent.resource.edit',
    handleClick: (record: any) => createModalRef.value.showEditModal(record),
  },
  delete: {
    permissionCode: 'agent.resource.delete',
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
