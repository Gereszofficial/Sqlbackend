<template>
  <div class="page-shell">
    <section class="page-head">
      <div>
        <div class="badge mb-3">Diák nézet</div>
        <h1 class="page-title">Feladatok</h1>
        <p class="page-copy">Tisztábban csoportosítva látod a témákat, a készültséget és a következő feladatot. Minden funkció ugyanaz maradt, csak a felület lett egyszerűbb.</p>
      </div>
      <div class="flex items-center gap-3">
        <span v-if="progress" :class="rankBadgeClass(progress.badge?.tone)" class="badge">{{ progress.badge?.label }}</span>
        <button class="btn-ghost" @click="load">Frissítés</button>
      </div>
    </section>

    <div class="card p-5">
      <div class="grid gap-3 md:grid-cols-4">
        <div class="metric-tile md:col-span-2">
          <div class="flex items-center justify-between gap-3">
            <div>
              <div class="text-xs text-slate-400">Összes teljesítés</div>
              <div class="mt-1 text-2xl font-semibold">{{ overall.done }} / {{ overall.total }}</div>
            </div>
            <div class="text-2xl font-semibold">{{ overall.percent }}%</div>
          </div>
          <div class="mt-4 h-2.5 rounded-full bg-slate-950/80 overflow-hidden border border-white/10">
            <div class="h-full rounded-full bg-gradient-to-r from-sky-400 to-emerald-400" :style="{ width: `${overall.percent}%` }" />
          </div>
        </div>
        <div class="metric-tile">
          <div class="text-xs text-slate-400">Témás feladatok</div>
          <div class="mt-1 text-2xl font-semibold">{{ topicOverall.done }}/{{ topicOverall.total }}</div>
        </div>
        <div class="metric-tile">
          <div class="text-xs text-slate-400">Önálló feladatok</div>
          <div class="mt-1 text-2xl font-semibold">{{ standaloneOverall.done }}/{{ standaloneOverall.total }}</div>
        </div>
      </div>
    </div>

    <div v-if="loading" class="text-sm text-slate-400">Betöltés...</div>
    <div v-else class="grid gap-5">
      <section class="card p-5">
        <div class="flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between mb-4">
          <div>
            <div class="section-title">Témák szerint</div>
            <div class="section-subtitle">Nyisd le a témát és válassz feladatot. A pipák a helyesen beküldött megoldásokat jelzik.</div>
          </div>
          <span class="badge">{{ topicGroups.length }} téma</span>
        </div>

        <div v-if="topicGroups.length === 0" class="text-sm text-slate-500">Nincs publikált témás feladat.</div>

        <div v-else class="grid gap-3">
          <div v-for="group in topicGroups" :key="group.key" class="card-soft overflow-hidden">
            <button class="w-full flex items-center justify-between gap-4 px-5 py-4 text-left" @click="toggle(group.key)">
              <div>
                <div class="text-base font-semibold">{{ group.title }}</div>
                <div class="text-xs text-slate-400 mt-1">{{ group.done }} kész · {{ group.items.length }} összes feladat</div>
              </div>
              <div class="flex items-center gap-2">
                <span class="badge">{{ group.percent }}%</span>
                <span class="text-slate-400 text-lg" aria-hidden="true">{{ open[group.key] ? '▴' : '▾' }}</span>
              </div>
            </button>

            <div v-show="open[group.key]" class="px-5 pb-5 soft-divider pt-4">
              <div class="grid sm:grid-cols-2 xl:grid-cols-3 gap-3">
                <RouterLink v-for="t in group.items" :key="t.id" class="link-card" :to="`/student/tasks/${t.id}`">
                  <div class="flex items-start justify-between gap-3">
                    <div>
                      <div class="text-xs uppercase tracking-[0.18em] text-slate-500">Feladat #{{ t.id }}</div>
                      <div class="mt-2 text-base font-semibold text-slate-50">{{ t.title }}</div>
                    </div>
                    <span v-if="t.isCompleted" class="inline-flex items-center justify-center h-8 w-8 rounded-full bg-emerald-500/15 text-emerald-200 border border-emerald-600/30">✓</span>
                  </div>
                  <div class="mt-4 flex items-center justify-between text-xs text-slate-400">
                    <span class="badge">Témás</span>
                    <span>{{ t.isCompleted ? 'Kész' : 'Megnyitás →' }}</span>
                  </div>
                </RouterLink>
              </div>
            </div>
          </div>
        </div>
      </section>

      <section class="card p-5">
        <div class="flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between mb-4">
          <div>
            <div class="section-title">Önálló feladatok</div>
            <div class="section-subtitle">Ezek a feladatok nincsenek témához kötve, bármikor megoldhatod őket.</div>
          </div>
          <span class="badge">{{ standaloneOverall.done }}/{{ standaloneOverall.total }} kész</span>
        </div>

        <div v-if="standaloneTasks.length === 0" class="text-sm text-slate-500">Nincs publikált önálló feladat.</div>
        <div v-else class="grid sm:grid-cols-2 xl:grid-cols-3 gap-3">
          <RouterLink v-for="t in standaloneTasks" :key="t.id" class="link-card" :to="`/student/tasks/${t.id}`">
            <div class="flex items-start justify-between gap-3">
              <div>
                <div class="text-xs uppercase tracking-[0.18em] text-slate-500">Önálló feladat</div>
                <div class="mt-2 text-base font-semibold text-slate-50">{{ t.title }}</div>
              </div>
              <span v-if="t.isCompleted" class="inline-flex items-center justify-center h-8 w-8 rounded-full bg-emerald-500/15 text-emerald-200 border border-emerald-600/30">✓</span>
            </div>
            <div class="mt-4 flex items-center justify-between text-xs text-slate-400">
              <span class="badge">#{{ t.id }}</span>
              <span>{{ t.isCompleted ? 'Kész' : 'Megnyitás →' }}</span>
            </div>
          </RouterLink>
        </div>
      </section>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { http } from '@/api/http'
import type { TaskSummary, StudentProgress } from '@/types/api'

const loading = ref(false)
const tasks = ref<TaskSummary[]>([])
const progress = ref<StudentProgress | null>(null)
const open = ref<Record<string, boolean>>({})

function rankBadgeClass(tone?: string) {
  switch (tone) {
    case 'emerald': return 'border-emerald-600/40 text-emerald-200'
    case 'sky': return 'border-sky-600/40 text-sky-200'
    case 'violet': return 'border-violet-600/40 text-violet-200'
    case 'amber': return 'border-amber-600/40 text-amber-200'
    default: return 'border-white/10 text-slate-200'
  }
}

const topicGroups = computed(() => {
  const map = new Map<string, { key: string; title: string; items: TaskSummary[]; done: number; percent: number }>()
  for (const t of tasks.value.filter(x => x.topicId)) {
    const key = `topic-${t.topicId}`
    if (!map.has(key)) map.set(key, { key, title: t.topicTitle || 'Téma', items: [], done: 0, percent: 0 })
    map.get(key)!.items.push(t)
  }
  const list = Array.from(map.values()).map(g => {
    const done = g.items.filter(x => x.isCompleted).length
    const percent = g.items.length ? Math.round((done / g.items.length) * 100) : 0
    return { ...g, done, percent }
  })
  for (const g of list) if (!(g.key in open.value)) open.value[g.key] = false
  return list
})

const standaloneTasks = computed(() => tasks.value.filter(x => !x.topicId))

const overall = computed(() => {
  const total = tasks.value.length
  const done = tasks.value.filter(x => x.isCompleted).length
  const percent = total ? Math.round((done / total) * 100) : 0
  return { total, done, percent }
})

const topicOverall = computed(() => {
  const all = tasks.value.filter(x => x.topicId)
  const total = all.length
  const done = all.filter(x => x.isCompleted).length
  return { total, done }
})

const standaloneOverall = computed(() => {
  const total = standaloneTasks.value.length
  const done = standaloneTasks.value.filter(x => x.isCompleted).length
  return { total, done }
})

function toggle(key: string) {
  open.value[key] = !open.value[key]
}

async function load() {
  loading.value = true
  try {
    const [tasksRes, progressRes] = await Promise.all([
      http.get<TaskSummary[]>('/api/student/tasks'),
      http.get<StudentProgress>('/api/student/progress')
    ])
    tasks.value = tasksRes.data
    progress.value = progressRes.data
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>
