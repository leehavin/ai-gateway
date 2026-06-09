<script lang="ts" setup>
import { ref, nextTick, reactive, h } from "vue";
import { VxeFormPropTypes, VxeFormInstance, VxeModalInstance } from "vxe-pc-ui";
import { getSingle, submitData } from "@/api/system/role";
import { ReRoleTreeSelect } from "@/components/ReRoleTreeSelect";
const emits = defineEmits<{ (e: "reload"): void }>();
const vxeModalRef = ref<VxeModalInstance>();
const modalOptions = reactive<{
  modalValue: boolean;
  modalTitle: string;
  canSubmit: boolean;
}>({
  modalValue: false,
  modalTitle: "",
  canSubmit: true
});

const showModal = (title: string, canSubmit?: boolean): void => {
  modalOptions.modalTitle = title;
  modalOptions.modalValue = true;
  modalOptions.canSubmit = canSubmit ?? true;
};

interface AddRoleInput {
  name: string;
  parentId: number | null;
  description: string;
  remark: string;
}
const formRef = ref<VxeFormInstance>();
const defaultFormData = () => {
  return {
    name: "",
    parentId: null,
    description: "",
    remark: ""
  };
};
const formData = ref<AddRoleInput>(defaultFormData());
const formItems = ref<VxeFormPropTypes.Items>([
  {
    field: "name",
    title: "角色名称",
    span: 24,
    itemRender: {
      name: "$input",
      props: { placeholder: "请输入角色名称" }
    }
  },
  {
    field: "parentId",
    title: "父级角色",
    span: 24,
    slots: {
      default: ({ data }) => [
        h(ReRoleTreeSelect, {
          modelValue: data.parentId,
          onNodeClick(nodeData: Recordable) {
            formData.value.parentId = nodeData.id;
            formRef.value.validateField("parentId");
          }
        })
      ]
    }
  },
  {
    field: "description",
    title: "角色描述",
    span: 24,
    itemRender: {
      name: "$textarea",
      props: { placeholder: "请输入角色描述" }
    }
  },
  {
    field: "remark",
    title: "备注",
    span: 24,
    itemRender: {
      name: "$textarea",
      props: { placeholder: "请输入备注" }
    }
  }
]);
const formRules = ref<VxeFormPropTypes.Rules>({
  name: [{ required: true, message: "请输入角色名" }],
  parentId: [{ required: true, message: "请选择父级角色" }]
});

const showAddModal = () => {
  showModal(`添加角色`);
  formData.value = defaultFormData();
  nextTick(() => {
    formRef.value.clearValidate();
  });
};
const showEditModal = (record: Recordable) => {
  showModal(`编辑角色->${record.name}`);
  nextTick(() => {
    formRef.value.clearValidate();
    getSingle(record.id).then((data: any) => {
      formData.value = data;
    });
  });
};
const showViewModal = (record: Recordable) => {
  showModal(`查看角色->${record.name}`, false);
  nextTick(() => {
    formRef.value.clearValidate();
    getSingle(record.id).then((data: any) => {
      formData.value = data;
    });
  });
};
const handleSubmit = async () => {
  const validate = await formRef.value.validate();
  if (!validate) {
    submitData(formData.value).then(() => {
      modalOptions.modalValue = false;
      emits("reload");
    });
  }
};

defineExpose({ showAddModal, showEditModal, showViewModal });
</script>
<template>
  <vxe-modal
    ref="vxeModalRef"
    v-model="modalOptions.modalValue"
    width="600"
    height="500"
    showFooter
    :title="modalOptions.modalTitle"
  >
    <template #default>
      <vxe-form
        ref="formRef"
        :data="formData"
        :items="formItems"
        :rules="formRules"
        :titleWidth="100"
        :titleColon="true"
        :titleAlign="`right`"
      />
    </template>
    <template #footer>
      <vxe-button content="关闭" @click="modalOptions.modalValue = false" />
      <vxe-button
        v-if="modalOptions.canSubmit"
        status="primary"
        content="确定"
        @click="handleSubmit"
      />
    </template>
  </vxe-modal>
</template>
