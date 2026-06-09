<script lang="ts" setup>
import { ref, nextTick, onMounted, computed } from 'vue';
import { getRoleResources, assignRoleResources } from '#/api/agent/agentAccess';
import { getList as getAgents } from '#/api/agent/agent';
import { $t } from '@vben/locales';

const reModalRef = ref();
const vxeTableRef = ref();
const allData = ref<any[]>([]);
const roleId = ref<number>();
const filterAgentId = ref('');
const agentOptions = ref<{ label: string; value: string }[]>([]);
const assignedMap = ref<Map<number, boolean>>(new Map());

const tableData = computed(() =>
  filterAgentId.value
    ? allData.value.filter((x) => x.agentId === filterAgentId.value)
    : allData.value,
);

onMounted(async () => {
  const list = (await getAgents()) as any[];
  agentOptions.value = [
    { label: $t('common.all'), value: '' },
    ...list.map((x) => ({ label: `${x.displayName} (${x.id})`, value: x.id })),
  ];
});

const syncCheckboxRows = () => {
  nextTick(() => {
    vxeTableRef.value?.clearCheckboxRow();
    const rows = tableData.value
      .filter((x) => assignedMap.value.get(x.resourceRowId))
      .map((x) => vxeTableRef.value.getRowById(x.resourceRowId));
    vxeTableRef.value?.setCheckboxRow(rows, true);
  });
};

const loadResources = async () => {
  if (!roleId.value) return;
  const data = (await getRoleResources(roleId.value, undefined, 'workflow')) as any[];
  allData.value = data;
  assignedMap.value = new Map(data.map((x) => [x.resourceRowId, !!x.assigned]));
  syncCheckboxRows();
};

const show = (record: any) => {
  roleId.value = record.id;
  filterAgentId.value = '';
  reModalRef.value.show(`${$t('agentAccess.assignResources')} -> ${record.name}`);
  nextTick(() => loadResources());
};

const handleFilterChange = () => syncCheckboxRows();

const handleCheckboxChange = () => {
  const records = vxeTableRef.value.getCheckboxRecords() as any[];
  const checkedIds = new Set(records.map((x) => x.resourceRowId));
  for (const row of tableData.value) {
    assignedMap.value.set(row.resourceRowId, checkedIds.has(row.resourceRowId));
  }
};

const handleSubmit = async () => {
  handleCheckboxChange();
  const resourceRowIds = [...assignedMap.value.entries()]
    .filter(([, assigned]) => assigned)
    .map(([id]) => id);
  await assignRoleResources(roleId.value!, resourceRowIds);
  reModalRef.value.close();
};

defineExpose({ show });
</script>

<template>
  <re-modal ref="reModalRef" @submit="handleSubmit">
    <div class="mb-3 flex items-center gap-2">
      <span>{{ $t('agentAccess.filterAgent') }}</span>
      <vxe-select
        v-model="filterAgentId"
        :options="agentOptions"
        style="width: 280px"
        @change="handleFilterChange"
      />
    </div>
    <vxe-table
      ref="vxeTableRef"
      :size="`mini`"
      :row-config="{ keyField: 'resourceRowId' }"
      :data="tableData"
      :checkbox-config="{ highlight: true }"
      max-height="400"
      @checkbox-change="handleCheckboxChange"
      @checkbox-all="handleCheckboxChange"
    >
      <vxe-column type="checkbox" width="50" />
      <vxe-column field="agentId" :title="$t('agentAccess.table.agentId')" width="120" />
      <vxe-column field="resourceType" :title="$t('agentAccess.table.resourceType')" width="90" />
      <vxe-column field="externalId" :title="$t('agentAccess.table.externalId')" min-width="140" />
      <vxe-column field="displayName" :title="$t('agentAccess.table.displayName')" min-width="140" />
    </vxe-table>
  </re-modal>
</template>
