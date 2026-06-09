<script lang="ts" setup>
import { ref, watch, computed } from 'vue';
import {
  getAgentResources,
  submitAgentResource,
  deleteAgentResource,
  syncCozeWorkflows,
} from '#/api/agent/agent';
import { getWorkflows } from '#/api/agent/cozeDiscovery';
import { deleteConfirm } from '#/components/modal';
import { $t } from '@vben/locales';
import { ElMessage } from 'element-plus';

const props = defineProps<{
  agentId?: string;
  provider?: string;
  providerAccountId?: number | null;
  cozeConfig?: {
    workspaceId?: string;
    botId?: string;
  };
  readonly?: boolean;
}>();

const resources = ref<any[]>([]);
const loading = ref(false);
const syncing = ref(false);
const workflowOptions = ref<{ label: string; value: string }[]>([]);
const selectedWorkflowId = ref('');
const adding = ref(false);

const showPanel = computed(
  () => !!props.agentId && props.provider === 'coze',
);

const loadResources = async () => {
  if (!props.agentId) {
    resources.value = [];
    return;
  }
  loading.value = true;
  try {
    resources.value = (await getAgentResources(props.agentId, 'workflow')) as any[];
  } finally {
    loading.value = false;
  }
};

const loadWorkflowOptions = async () => {
  workflowOptions.value = [];
  selectedWorkflowId.value = '';
  if (!props.providerAccountId || !props.cozeConfig?.workspaceId) return;
  try {
    const list = (await getWorkflows(
      props.providerAccountId,
      props.cozeConfig.workspaceId,
      props.cozeConfig.botId,
      'published_online',
    )) as any[];
    const existing = new Set(resources.value.map((x) => x.externalId));
    workflowOptions.value = list
      .filter((x) => !existing.has(x.workflowId))
      .map((x) => ({
        label: `${x.name} (${x.workflowId})`,
        value: x.workflowId,
      }));
  } catch {
    workflowOptions.value = [];
  }
};

watch(
  () => [props.agentId, props.provider],
  () => loadResources(),
  { immediate: true },
);

watch(
  () => [props.providerAccountId, props.cozeConfig?.workspaceId, resources.value.length],
  () => loadWorkflowOptions(),
);

const handleSync = async () => {
  if (!props.agentId) return;
  syncing.value = true;
  try {
    const added = (await syncCozeWorkflows(props.agentId)) as number;
    await loadResources();
    ElMessage.success(
      added > 0
        ? $t('agentManage.workflows.syncSuccess', { count: added })
        : $t('agentManage.workflows.syncEmpty'),
    );
  } finally {
    syncing.value = false;
  }
};

const handleAdd = async () => {
  if (!props.agentId || !selectedWorkflowId.value) return;
  const wf = workflowOptions.value.find((x) => x.value === selectedWorkflowId.value);
  adding.value = true;
  try {
    await submitAgentResource({
      agentId: props.agentId,
      resourceType: 'workflow',
      externalId: selectedWorkflowId.value,
      displayName: wf?.label.split(' (')[0] ?? selectedWorkflowId.value,
      configJson: '{"inputParameter":"BOT_USER_INPUT","inputHint":""}',
      sortOrder: resources.value.length,
      enabled: true,
    });
    selectedWorkflowId.value = '';
    await loadResources();
    ElMessage.success($t('agentManage.workflows.addSuccess'));
  } finally {
    adding.value = false;
  }
};

const handleToggleEnabled = async (row: any) => {
  await submitAgentResource({
    ...row,
    enabled: !row.enabled,
  });
  await loadResources();
};

const handleDelete = async (row: any) => {
  if (!(await deleteConfirm())) return;
  await deleteAgentResource(row.id);
  await loadResources();
};
</script>

<template>
  <div v-if="showPanel" class="mt-4 border-t pt-4">
    <div class="mb-2 flex items-center justify-between">
      <span class="font-medium">{{ $t('agentManage.workflows.title') }}</span>
      <span class="text-xs text-gray-500">{{ $t('agentManage.workflows.hint') }}</span>
    </div>
    <div v-if="!readonly" class="mb-3 flex flex-wrap items-center gap-2">
      <vxe-button status="primary" :loading="syncing" @click="handleSync">
        {{ $t('agentManage.workflows.sync') }}
      </vxe-button>
      <vxe-select
        v-if="workflowOptions.length"
        v-model="selectedWorkflowId"
        :options="workflowOptions"
        :placeholder="$t('agentManage.workflows.addPlaceholder')"
        style="width: 280px"
        clearable
        transfer
      />
      <vxe-button
        v-if="workflowOptions.length"
        :disabled="!selectedWorkflowId"
        :loading="adding"
        @click="handleAdd"
      >
        {{ $t('agentManage.workflows.add') }}
      </vxe-button>
    </div>
    <vxe-table :data="resources" :loading="loading" :size="`mini`" max-height="320">
      <vxe-column field="displayName" :title="$t('agentManage.workflows.columns.name')" min-width="140" />
      <vxe-column field="externalId" :title="$t('agentManage.workflows.columns.id')" min-width="160" />
      <vxe-column field="enabled" :title="$t('agentManage.workflows.columns.enabled')" width="80">
        <template #default="{ row }">
          {{ row.enabled ? $t('common.enable') : $t('common.disable') }}
        </template>
      </vxe-column>
      <vxe-column v-if="!readonly" :title="$t('common.operation')" width="140">
        <template #default="{ row }">
          <vxe-button type="text" @click="handleToggleEnabled(row)">
            {{ row.enabled ? $t('common.disable') : $t('common.enable') }}
          </vxe-button>
          <vxe-button type="text" status="danger" @click="handleDelete(row)">
            {{ $t('common.del') }}
          </vxe-button>
        </template>
      </vxe-column>
    </vxe-table>
    <p v-if="!resources.length && !loading" class="mt-2 text-xs text-gray-500">
      {{ $t('agentManage.workflows.empty') }}
    </p>
  </div>
</template>
