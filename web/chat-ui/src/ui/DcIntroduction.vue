<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(
  defineProps<{
    logoImg?: string
    logoWidth?: number | string
    logoHeight?: number | string
    title?: string
    subTitle?: string
    description?: string[]
  }>(),
  {
    logoWidth: 148,
    description: () => [],
  }
)

const logoStyle = computed(() => ({
  width: typeof props.logoWidth === 'number' ? `${props.logoWidth}px` : props.logoWidth,
  height: props.logoHeight
    ? typeof props.logoHeight === 'number'
      ? `${props.logoHeight}px`
      : props.logoHeight
    : 'auto',
}))
</script>

<template>
  <section class="dc-intro">
    <img v-if="logoImg" class="dc-intro-logo" :src="logoImg" :alt="title" :style="logoStyle" />
    <h1 v-if="title" class="dc-intro-title">{{ title }}</h1>
    <p v-if="subTitle" class="dc-intro-sub">{{ subTitle }}</p>
    <div v-if="description.length" class="dc-intro-desc">
      <p v-for="(line, i) in description" :key="i">{{ line }}</p>
    </div>
  </section>
</template>

<style scoped>
.dc-intro {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  gap: 12px;
}

.dc-intro-logo {
  object-fit: contain;
}

.dc-intro-title {
  margin: 0;
  font-size: 28px;
  font-weight: 700;
  letter-spacing: -0.02em;
  color: var(--dc-text);
}

.dc-intro-sub {
  margin: 0;
  font-size: 18px;
  font-weight: 500;
  color: var(--dc-text);
}

.dc-intro-desc {
  max-width: 560px;
  font-size: 14px;
  line-height: 1.65;
  color: var(--dc-text-secondary);
}

.dc-intro-desc p {
  margin: 0 0 6px;
}

.dc-intro-desc p:last-child {
  margin-bottom: 0;
}
</style>
