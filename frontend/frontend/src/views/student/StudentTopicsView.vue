<template>
  <div class="page-shell">
    <section class="page-head">
      <div>
        <div class="badge mb-3">Tanulási útvonal</div>
        <h1 class="page-title">Témák</h1>
        <p class="page-copy">Fejezetenként tudsz haladni. Minden témánál rögtön látod, hogy kész van-e, és egy kattintással megnyithatod a kapcsolódó feladatokat.</p>
      </div>
      <button class="btn-ghost" @click="load">Frissítés</button>
    </section>

    <div v-if="loading" class="text-sm text-slate-400">Betöltés...</div>

    <div v-else class="grid md:grid-cols-2 xl:grid-cols-3 gap-4">
      <RouterLink v-for="t in topics" :key="t.id" :to="`/student/topics/${t.id}`" class="link-card">
        <div class="flex items-start justify-between gap-3">
          <div>
            <div class="text-xs uppercase tracking-[0.18em] text-slate-500">Téma</div>
            <div class="mt-2 text-xl font-semibold">{{ t.title }}</div>
          </div>
          <span class="badge" :class="t.isCompleted ? 'border-emerald-600/40 text-emerald-200' : 'border-white/10 text-slate-300'">
            {{ t.isCompleted ? '✓ Kész' : 'Nyitott' }}
          </span>
        </div>
        <div class="mt-4 text-sm text-slate-400">Olvasd el az anyagot, majd indítsd a hozzá tartozó feladatokat.</div>
      </RouterLink>
    </div>

    <div v-if="error" class="text-sm text-red-300 border border-red-900/50 bg-red-950/40 rounded-xl p-3">
      {{ error }}
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { http } from '@/api/http'
import type { TopicSummary } from '@/types/api'

const topics = ref<TopicSummary[]>([])
const loading = ref(false)
const error = ref('')

async function load() {
  error.value = ''
  loading.value = true
  try {
    const res = await http.get<TopicSummary[]>('/api/student/topics')
    topics.value = res.data
  } catch (e: any) {
    error.value = e?.response?.data?.message || 'Nem sikerült betölteni a témákat.'
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>
