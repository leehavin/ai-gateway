<template>
  <div class="dc-chat-layout">
    <slot />
  </div>
</template>

<style scoped>
.dc-chat-layout {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
  gap: 0;
}
</style>
