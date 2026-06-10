<script setup lang="ts">
import { reactive, ref } from 'vue';
import { ElDrawer } from 'element-plus';

const emits = defineEmits<{
  (e: 'submit'): void;
}>();

const props = withDefaults(
  defineProps<{
    size?: string | number;
  }>(),
  {
    size: '720px',
  },
);

const submitting = ref(false);
const drawerOptions = reactive({
  value: false,
  title: '',
  readonly: false,
});

const show = (title: string, readonly?: boolean) => {
  drawerOptions.title = title;
  drawerOptions.readonly = readonly ?? false;
  submitting.value = false;
  drawerOptions.value = true;
};

const close = () => {
  drawerOptions.value = false;
  submitting.value = false;
};

const setSubmitting = (value: boolean) => {
  submitting.value = value;
};

const setTitle = (title: string) => {
  drawerOptions.title = title;
};

defineExpose({ show, close, setSubmitting, setTitle });
</script>

<template>
  <el-drawer
    v-model="drawerOptions.value"
    :title="drawerOptions.title"
    direction="rtl"
    :size="props.size"
    :close-on-click-modal="false"
    append-to-body
    destroy-on-close
    class="re-drawer"
  >
    <slot />
    <template #footer>
      <div class="re-drawer__footer">
        <vxe-button
          v-if="!drawerOptions.readonly"
          size="small"
          :content="$t('common.cancel')"
          @click="drawerOptions.value = false"
        />
        <vxe-button
          v-if="!drawerOptions.readonly"
          size="small"
          status="primary"
          :content="$t('common.save')"
          :loading="submitting"
          :disabled="submitting"
          @click="emits('submit')"
        />
        <vxe-button
          v-else
          size="small"
          :content="$t('common.back')"
          @click="drawerOptions.value = false"
        />
      </div>
    </template>
  </el-drawer>
</template>

<style scoped>
.re-drawer__footer {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
}
</style>
