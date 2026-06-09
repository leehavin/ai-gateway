<script lang="ts" setup>
import { ref, nextTick } from 'vue';
import { getRoleAgents, assignRoleAgents } from '#/api/agent/agentAccess';
import { $t } from '@vben/locales';

const reModalRef = ref();
const vxeTableRef = ref();
const tableData = ref<any[]>([]);
const roleId = ref<number>();

const show = (record: any) => {
  roleId.value = record.id;
  reModalRef.value.show(`${$t('agentAccess.assignAgents')} -> ${record.name}`);
  nextTick(() => {
    vxeTableRef.value?.clearCheckboxRow();
    getRoleAgents(record.id).then((data: any[]) => {
      tableData.value = data;
      nextTick(() => {
        const rows = data
          .filter((x) => x.assigned)
          .map((x) => vxeTableRef.value.getRowById(x.agentId));
        vxeTableRef.value?.setCheckboxRow(rows, true);
      });
    });
  });
};

const handleSubmit = async () => {
  const records = vxeTableRef.value.getCheckboxRecords() as any[];
  const agentIds = records.map((x) => x.agentId);
  await assignRoleAgents(roleId.value!, agentIds);
  reModalRef.value.close();
};

defineExpose({ show });
</script>

<template>
  <re-modal ref="reModalRef" @submit="handleSubmit">
    <vxe-table
      ref="vxeTableRef"
      :size="`mini`"
      :row-config="{ keyField: 'agentId' }"
      :data="tableData"
      :checkbox-config="{ highlight: true }"
      max-height="420"
    >
      <vxe-column type="checkbox" width="50" />
      <vxe-column field="agentId" :title="$t('agentAccess.table.agentId')" width="140" />
      <vxe-column field="displayName" :title="$t('agentAccess.table.displayName')" min-width="160" />
      <vxe-column field="provider" :title="$t('agentAccess.table.provider')" width="90" />
    </vxe-table>
  </re-modal>
</template>
