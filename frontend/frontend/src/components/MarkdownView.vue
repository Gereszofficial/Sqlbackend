<template>
  <div class="markdown text-sm leading-relaxed" v-html="html"></div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import MarkdownIt from 'markdown-it'
import DOMPurify from 'dompurify'

const props = defineProps<{ source: string }>()

const md = new MarkdownIt({
  html: false,
  linkify: true,
  breaks: true
})

const html = computed(() => {
  const raw = md.render(props.source || '')
  // Basic XSS protection
  return DOMPurify.sanitize(raw)
})
</script>

<style scoped>
.markdown :deep(h1),
.markdown :deep(h2),
.markdown :deep(h3) {
  font-weight: 700;
  margin: 0.8rem 0 0.4rem;
}
.markdown :deep(p) {
  margin: 0.5rem 0;
}
.markdown :deep(code) {
  font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", "Courier New", monospace;
  font-size: 0.9em;
  padding: 0.1rem 0.35rem;
  border: 1px solid rgba(148, 163, 184, 0.2);
  border-radius: 0.5rem;
  background: rgba(2, 6, 23, 0.35);
}
.markdown :deep(pre) {
  padding: 0.75rem;
  border: 1px solid rgba(148, 163, 184, 0.2);
  border-radius: 0.75rem;
  background: rgba(2, 6, 23, 0.35);
  overflow: auto;
}
.markdown :deep(pre code) {
  padding: 0;
  border: 0;
  background: transparent;
}
.markdown :deep(a) {
  color: rgb(56, 189, 248);
  text-decoration: underline;
}
.markdown :deep(ul),
.markdown :deep(ol) {
  padding-left: 1.25rem;
  margin: 0.5rem 0;
}
.markdown :deep(blockquote) {
  border-left: 3px solid rgba(148, 163, 184, 0.35);
  padding-left: 0.75rem;
  margin: 0.75rem 0;
  color: rgba(226, 232, 240, 0.85);
}
</style>
