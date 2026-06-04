<script setup lang="ts">
export interface CitationItem {
  title: string
  url?: string
  snippet?: string
}

defineProps<{
  items: CitationItem[]
}>()
</script>

<template>
  <div v-if="items.length" class="dc-citations">
    <p class="dc-citations-title">参考来源</p>
    <ul class="dc-citations-list">
      <li v-for="(c, i) in items" :key="i" class="dc-citation-item">
        <a
          v-if="c.url"
          class="dc-citation-link"
          :href="c.url"
          target="_blank"
          rel="noopener noreferrer"
        >
          {{ c.title }}
        </a>
        <span v-else class="dc-citation-link">{{ c.title }}</span>
        <p v-if="c.snippet" class="dc-citation-snippet">{{ c.snippet }}</p>
      </li>
    </ul>
  </div>
</template>

<style scoped>
.dc-citations {
  margin-top: 10px;
  padding-top: 10px;
  border-top: 1px dashed var(--dc-border);
}

.dc-citations-title {
  margin: 0 0 8px;
  font-size: 11px;
  font-weight: 600;
  color: var(--dc-text-muted);
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.dc-citations-list {
  margin: 0;
  padding: 0;
  list-style: none;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.dc-citation-item {
  padding: 8px 10px;
  border-radius: var(--dc-radius-sm);
  background: var(--dc-brand-soft);
  border: 1px solid rgba(94, 124, 224, 0.15);
}

.dc-citation-link {
  font-size: 13px;
  font-weight: 600;
  color: var(--dc-brand);
  text-decoration: none;
}

.dc-citation-link:hover {
  text-decoration: underline;
}

.dc-citation-snippet {
  margin: 4px 0 0;
  font-size: 12px;
  line-height: 1.45;
  color: var(--dc-text-secondary);
}
</style>
