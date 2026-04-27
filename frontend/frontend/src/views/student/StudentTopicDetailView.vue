<template>
  <div class="page-shell">
    <section class="page-head">
      <div>
        <RouterLink class="text-sm text-slate-400 hover:text-slate-200" to="/student/topics">← Vissza a témákhoz</RouterLink>
        <h1 class="page-title mt-3">{{ topic?.title || 'Téma' }}</h1>
        <p class="page-copy mt-2">Olvasd át a tananyagot, majd jobb oldalon válassz egy feladatot. A kész feladatoknál pipa jelenik meg.</p>
      </div>
      <div class="flex items-center gap-3">
        <span class="badge">{{ topic?.tasks?.length || 0 }} feladat</span>
        <button class="btn-ghost" @click="load">Frissítés</button>
      </div>
    </section>

    <div v-if="loading" class="text-sm text-slate-400">Betöltés...</div>

    <div v-else class="grid xl:grid-cols-[1.45fr,0.95fr] gap-4">
      <div class="card p-6">
        <div class="flex items-center justify-between mb-4">
          <div>
            <div class="section-title">Tananyag</div>
            <div class="section-subtitle">Leírás és magyarázat a témához.</div>
          </div>
          <span class="badge">{{ topic?.slug || 'tema' }}</span>
        </div>
        <MarkdownView :source="topic?.descriptionMarkdown || ''" />
      </div>

      <div class="card p-5">
        <div class="flex items-center justify-between mb-3">
          <div>
            <div class="section-title">Kapcsolódó feladatok</div>
            <div class="section-subtitle">Válassz egy feladatot a folytatáshoz.</div>
          </div>
          <span class="badge">{{ topic?.tasks?.length || 0 }} db</span>
        </div>

        <div v-if="!topic?.tasks?.length" class="text-sm text-slate-400">Nincs feladat ebben a témában.</div>

        <div v-else class="grid gap-3">
          <RouterLink v-for="t in topic!.tasks" :key="t.id" :to="`/student/tasks/${t.id}`" class="link-card !rounded-2xl !p-4">
            <div class="flex items-center justify-between gap-3">
              <div>
                <div class="text-xs text-slate-500 uppercase tracking-[0.16em]">Feladat #{{ t.id }}</div>
                <div class="mt-1 text-sm font-semibold">{{ t.title }}</div>
              </div>
              <span class="badge" :class="t.isCompleted ? 'border-emerald-600/40 text-emerald-200' : 'border-white/10 text-slate-300'">
                {{ t.isCompleted ? '✓ Kész' : 'Megnyitás' }}
              </span>
            </div>
          </RouterLink>
        </div>
      </div>
    </div>

    <div v-if="error" class="text-sm text-red-300 border border-red-900/50 bg-red-950/40 rounded-xl p-3">
      {{ error }}
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { http } from '@/api/http'
import type { TopicDetails } from '@/types/api'
import MarkdownView from '@/components/MarkdownView.vue'

const route = useRoute()
const id = computed(() => Number(route.params.id))

const topic = ref<TopicDetails | null>(null)
const loading = ref(false)
const error = ref('')

async function load() {
  error.value = ''
  loading.value = true
  try {
    const res = await http.get<TopicDetails>(`/api/student/topics/${id.value}`)
    topic.value = res.data
  } catch (e: any) {
    error.value = e?.response?.data?.message || 'Nem sikerült betölteni a témát.'
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>
