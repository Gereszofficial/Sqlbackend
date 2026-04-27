<template>
  <span
    class="schema-key-icon"
    :class="toneClass"
    aria-hidden="true"
    :title="title || 'Kulcs mező'"
  >
    <svg viewBox="0 0 24 24" fill="currentColor" class="w-3.5 h-3.5">
      <path d="M14 3a7 7 0 0 0-5.566 11.246L2 20.68V22h1.32l1.5-1.5 1.06 1.06 1.5-1.5 1.06 1.06 2.12-2.12A7 7 0 1 0 14 3Zm0 2.2A4.8 4.8 0 1 1 9.2 10 4.81 4.81 0 0 1 14 5.2Zm0 2.2A2.6 2.6 0 1 0 16.6 10 2.6 2.6 0 0 0 14 7.4Z"/>
    </svg>
  </span>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  tone?: 'gold' | 'gray'
  title?: string
}>()

const toneClass = computed(() => (props.tone === 'gold' ? 'schema-key-icon--gold' : 'schema-key-icon--gray'))
</script>
