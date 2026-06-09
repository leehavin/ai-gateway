<script lang="ts" setup>
import { h, reactive, ref } from 'vue';
import { getPageList } from '#/api/system/role';
import { VxeButton } from 'vxe-pc-ui';
import AgentAssignModal from './AgentAssignModal.vue';
import ResourceAssignModal from './ResourceAssignModal.vue';
import { $t } from '@vben/locales';

const agentModalRef = ref();
const resourceModalRef = ref();
const reVxeGridRef = ref();

const columns = [
  { title: $t('agentAccess.columns.name'), field: 'name', minWidth: 140, treeNode: true },
  { title: $t('agentAccess.columns.description'), field: 'description', minWidth: 160 },
  {
    title: $t('agentAccess.columns.agents'),
    field: 'agents',
    align: 'center',
    width: 120,
    slots: {
      default: ({ row }: { row: any }) =>
        h(VxeButton, {
          size: 'small',
          mode: 'text',
          content: $t('agentAccess.assignAgents'),
          status: 'primary',
          onClick: () => agentModalRef.value.show(row),
        }),
    },
  },
  {
    title: $t('agentAccess.columns.resources'),
    field: 'resources',
    align: 'center',
    width: 120,
    slots: {
      default: ({ row }: { row: any }) =>
        h(VxeButton, {
          size: 'small',
          mode: 'text',
          content: $t('agentAccess.assignResources'),
          status: 'success',
          onClick: () => resourceModalRef.value.show(row),
        }),
    },
  },
];

const handleInitialFormParams = () => ({ name: '' });
const formData = reactive(handleInitialFormParams());
const formItems = [
  {
    field: 'name',
    title: $t('agentAccess.search.name'),
    span: 6,
    itemRender: {
      name: '$input',
      props: { placeholder: $t('agentAccess.search.name') },
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
</script>

<template>
  <re-page>
    <re-vxe-grid
      ref="reVxeGridRef"
      :request="getPageList"
      :columns="columns"
      :searchOptions="searchOptions"
    />
    <AgentAssignModal ref="agentModalRef" />
    <ResourceAssignModal ref="resourceModalRef" />
  </re-page>
</template>
