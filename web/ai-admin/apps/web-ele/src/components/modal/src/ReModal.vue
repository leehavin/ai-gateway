<script setup lang="ts">
import { reactive, ref } from 'vue';
const emits = defineEmits<{
  (e: 'submit'): void;
}>();
const props = withDefaults(
  defineProps<{
    heigh?: number;
    width?: number;
  }>(),
  {
    heigh: 600,
    width: 800,
  },
);
const vxeModalRef = ref();
const submitting = ref(false);
const modalOptions = reactive({
  value: false,
  title: '',
  readonly: false,
});

const show = (title: string, readonly?: boolean) => {
  modalOptions.title = title;
  modalOptions.readonly = readonly ?? false;
  submitting.value = false;
  modalOptions.value = true;
};
const close = () => {
  modalOptions.value = false;
  submitting.value = false;
};
const setSubmitting = (value: boolean) => {
  submitting.value = value;
};
defineExpose({ show, close, setSubmitting });
</script>

<template>
  <vxe-modal
    ref="vxeModalRef"
    show-footer
    v-bind="$attrs"
    v-model="modalOptions.value"
    :width="props.width"
    :height="props.heigh"
    :title="modalOptions.title"
  >
    <template #default><slot name="default" /></template>
    <template #footer>
      <vxe-button
        v-if="!modalOptions.readonly"
        size="small"
        :content="$t(`common.cancel`)"
        @click="modalOptions.value = false"
      />
      <vxe-button
        v-if="!modalOptions.readonly"
        size="small"
        :status="`primary`"
        :content="$t(`common.save`)"
        :loading="submitting"
        :disabled="submitting"
        @click="emits('submit')"
      />
    </template>
  </vxe-modal>
</template>
