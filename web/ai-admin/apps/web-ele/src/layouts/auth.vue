<script lang="ts" setup>
import { AuthPageLayout } from '@vben/layouts';

import { $t } from '#/locales';
</script>

<template>
  <AuthPageLayout
    class="dc-auth-page"
    :copyright="false"
    app-name=""
    logo=""
    slogan-image="/logo.svg"
    :page-description="$t('authentication.pageDesc')"
    :page-title="$t('authentication.pageTitle')"
  >
    <!-- 自定义工具栏 -->
    <!-- <template #toolbar></template> -->
  </AuthPageLayout>
</template>

<style scoped>
.dc-auth-page :deep(img.animate-float) {
  width: auto !important;
  max-width: 120px;
  height: 5rem !important;
}

.dc-auth-page :deep(.text-1xl) {
  margin-top: 1.25rem;
  font-size: 1.375rem;
  font-weight: 600;
  letter-spacing: 0.02em;
}

.dc-auth-page :deep(.dark\:text-muted-foreground) {
  max-width: 24rem;
  margin-top: 0.75rem;
  font-size: 0.875rem;
  line-height: 1.7;
  color: hsl(var(--muted-foreground));
  text-align: center;
}
</style>
