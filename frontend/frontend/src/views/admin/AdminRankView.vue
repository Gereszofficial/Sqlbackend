<template>
  <div class="page-shell">
    <section class="page-head">
      <div>
        <h1 class="page-title">Rank</h1>
        <p class="page-copy">
          Diákok haladása egyszerűbb, könnyebben olvasható nézetben.
          <span v-if="refreshMode === 'auto'">Automatikus frissítés: 5 mp.</span>
          <span v-else>Manuális frissítés.</span>
        </p>
      </div>

      <div class="flex items-center gap-2 flex-wrap justify-end">
        <div class="text-[11px] text-slate-500" v-if="generatedAt">
          Utolsó frissítés (UTC): {{ generatedAt }}
        </div>
        <button class="btn-ghost" @click="toggleRefreshMode" :disabled="loading">
          Frissítési mód: {{ refreshModeLabel }}
        </button>
        <button class="btn-ghost" @click="load" :disabled="loading">Frissítés</button>
      </div>
    </section>

    <div v-if="error" class="text-sm text-red-300 border border-red-900/50 bg-red-950/40 rounded-xl p-3">
      {{ error }}
    </div>

    <div v-if="loading" class="text-sm text-slate-400">Betöltés...</div>

    <div v-else class="grid gap-4">
      <div class="card p-5">
        <div class="grid gap-3 md:grid-cols-[1fr,auto] md:items-end">
          <div class="grid gap-3 sm:grid-cols-3">
            <div class="metric-tile">
              <div class="text-xs text-slate-400">Publikált feladatok</div>
              <div class="mt-1 text-2xl font-semibold">{{ tasks.length }}</div>
            </div>
            <div class="metric-tile">
              <div class="text-xs text-slate-400">Diákok</div>
              <div class="mt-1 text-2xl font-semibold">{{ students.length }}</div>
            </div>
            <div class="metric-tile">
              <div class="text-xs text-slate-400">Nézet</div>
              <div class="mt-1 text-sm font-semibold">Lenyitható részletek</div>
            </div>
          </div>

          <div class="flex items-center gap-2 flex-wrap justify-end">
            <input v-model="q" class="input w-64" placeholder="Keresés email alapján…" aria-label="Keresés email alapján" />
            <button class="btn-ghost" @click="expandAll" :disabled="studentsFiltered.length === 0">Nyit mind</button>
            <button class="btn-ghost" @click="collapseAll" :disabled="studentsFiltered.length === 0">Zár mind</button>
          </div>
        </div>
      </div>

      <div class="card p-0 overflow-hidden">
        <div class="overflow-auto">
          <table class="table-quiet min-w-[760px] w-full text-sm">
            <thead class="sticky top-0 bg-[#0a1323]">
              <tr class="border-b border-white/10">
                <th class="text-left px-4 py-3 whitespace-nowrap">#</th>
                <th class="text-left px-4 py-3 whitespace-nowrap">Diák</th>
                <th class="text-left px-4 py-3 whitespace-nowrap">Rank</th>
                <th class="text-left px-4 py-3 whitespace-nowrap">Kész</th>
                <th class="text-left px-4 py-3 whitespace-nowrap">%</th>
                <th class="text-right px-4 py-3 whitespace-nowrap">Részletek</th>
              </tr>
            </thead>

            <tbody>
              <template v-for="(s, idx) in studentsFiltered" :key="s.studentId">
                <tr class="border-b border-white/5 hover:bg-white/[0.02] transition">
                  <td class="px-4 py-3 text-slate-400">{{ idx + 1 }}</td>
                  <td class="px-4 py-3">
                    <div class="font-semibold text-slate-100">{{ s.email }}</div>
                    <div class="text-xs text-slate-500">ID: <span class="font-mono">{{ s.studentId }}</span></div>
                  </td>
                  <td class="px-4 py-3 whitespace-nowrap">
                    <div v-if="s.badge" class="flex items-center gap-2">
                      <RankBadge :badge="s.badge" :size="26" />
                      <span class="text-xs text-slate-200">{{ s.badge.label }}</span>
                    </div>
                    <span v-else class="text-xs text-slate-500">—</span>
                  </td>
                  <td class="px-4 py-3 whitespace-nowrap"><span class="badge">{{ s.completedCount }} / {{ s.totalTasks }}</span></td>
                  <td class="px-4 py-3 whitespace-nowrap text-slate-300">{{ s.percent }}%</td>
                  <td class="px-4 py-3 text-right">
                    <button class="btn-ghost" @click="toggleStudent(s.studentId)">
                      {{ opened[String(s.studentId)] ? 'Bezár' : 'Lenyit' }}
                      <span class="text-slate-400 ml-1" aria-hidden="true">{{ opened[String(s.studentId)] ? '▴' : '▾' }}</span>
                    </button>
                  </td>
                </tr>

                <tr v-show="opened[String(s.studentId)]" class="border-b border-white/5 bg-white/[0.015]">
                  <td :colspan="6" class="px-4 py-4">
                    <div class="grid gap-4">
                      <div class="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
                        <div>
                          <div class="text-sm font-semibold">Haladás</div>
                          <div class="text-xs text-slate-400">{{ s.completedCount }} / {{ s.totalTasks }} kész ({{ s.percent }}%)</div>
                        </div>
                        <div v-if="s.badge" class="flex items-center gap-2">
                          <RankBadge :badge="s.badge" :size="28" />
                          <span class="text-sm text-slate-200">{{ s.badge.label }}</span>
                        </div>
                      </div>

                      <div class="h-2 rounded-full bg-slate-950 overflow-hidden border border-white/10">
                        <div class="h-full bg-gradient-to-r from-sky-400 to-emerald-400" :style="{ width: `${s.percent}%` }" />
                      </div>

                      <div class="grid gap-3">
                        <div v-for="g in taskGroups" :key="g.name" class="card-soft overflow-hidden">
                          <div class="px-4 py-3 border-b border-white/10 bg-white/[0.02]">
                            <div class="text-sm font-semibold">{{ g.name }}</div>
                            <div class="text-xs text-slate-500">{{ g.items.length }} feladat</div>
                          </div>
                          <div class="p-4 flex flex-wrap gap-2">
                            <span
                              v-for="t in g.items"
                              :key="t.id"
                              class="inline-flex items-center gap-2 px-3 py-1 rounded-full border text-xs select-none"
                              :class="isDone(s, t.id) ? 'bg-emerald-500/15 text-emerald-200 border-emerald-600/30' : 'bg-slate-950/20 text-slate-300 border-white/10'"
                              :title="`${t.category} · ${t.title}`"
                            >
                              <span class="font-mono">#{{ t.id }}</span>
                              <span aria-hidden="true">{{ isDone(s, t.id) ? '✓' : '·' }}</span>
                            </span>
                          </div>
                        </div>

                        <div v-if="tasks.length === 0" class="text-sm text-slate-500">Nincs publikált feladat.</div>
                      </div>
                    </div>
                  </td>
                </tr>
              </template>

              <tr v-if="studentsFiltered.length === 0">
                <td class="px-4 py-6 text-slate-500" :colspan="6">Nincs megjeleníthető diák.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, onBeforeUnmount, ref } from 'vue'
import { http } from '@/api/http'
import type { RankResponse, RankTask, RankStudentRow } from '@/types/api'
import RankBadge from '@/components/RankBadge.vue'

const tasks = ref<RankTask[]>([])
const students = ref<RankStudentRow[]>([])
const loading = ref(false)
const error = ref('')
const generatedAt = ref('')
const q = ref('')

type RefreshMode = 'auto' | 'manual'
const refreshMode = ref<RefreshMode>('auto')
const refreshModeLabel = computed(() => (refreshMode.value === 'auto' ? 'Automatikus' : 'Manuális'))

let timer: any = null

function startAutoRefresh() {
  stopAutoRefresh()
  timer = setInterval(load, 5000)
}

function stopAutoRefresh() {
  if (timer) {
    clearInterval(timer)
    timer = null
  }
}

function toggleRefreshMode() {
  refreshMode.value = refreshMode.value === 'auto' ? 'manual' : 'auto'
  localStorage.setItem('rank_refresh_mode', refreshMode.value)
  if (refreshMode.value === 'auto') startAutoRefresh()
  else stopAutoRefresh()
}

function isDone(s: RankStudentRow, taskId: number) {
  return s.completedTaskIds?.includes(taskId)
}

const studentsFiltered = computed(() => {
  const needle = q.value.trim().toLowerCase()
  if (!needle) return students.value
  return students.value.filter(s => (s.email || '').toLowerCase().includes(needle))
})

const taskGroups = computed(() => {
  const map = new Map<string, RankTask[]>()
  for (const t of tasks.value) {
    const name = (t.category || '').trim() || 'Téma nélkül'
    if (!map.has(name)) map.set(name, [])
    map.get(name)!.push(t)
  }
  return [...map.entries()]
    .map(([name, items]) => ({ name, items: items.slice().sort((a, b) => a.id - b.id) }))
    .sort((a, b) => a.name.localeCompare(b.name))
})

const opened = ref<Record<string, boolean>>({})

function toggleStudent(studentId: number) {
  opened.value[String(studentId)] = !opened.value[String(studentId)]
}

function expandAll() {
  const next: Record<string, boolean> = { ...opened.value }
  for (const s of studentsFiltered.value) next[String(s.studentId)] = true
  opened.value = next
}

function collapseAll() {
  const next: Record<string, boolean> = { ...opened.value }
  for (const s of studentsFiltered.value) next[String(s.studentId)] = false
  opened.value = next
}

async function load() {
  error.value = ''
  loading.value = true
  try {
    const res = await http.get<RankResponse>('/api/admin/rank')
    tasks.value = res.data.tasks ?? []
    students.value = (res.data.students ?? []).slice().sort((a, b) => {
      if (b.percent !== a.percent) return b.percent - a.percent
      if (b.completedCount !== a.completedCount) return b.completedCount - a.completedCount
      return (a.email || '').localeCompare(b.email || '')
    })
    generatedAt.value = res.data.generatedAtUtc || ''
  } catch (e: any) {
    error.value = e?.response?.data?.message || 'Nem sikerült betölteni a ranglistát.'
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  const saved = localStorage.getItem('rank_refresh_mode')
  if (saved === 'manual' || saved === 'auto') refreshMode.value = saved
  load()
  if (refreshMode.value === 'auto') startAutoRefresh()
})

onBeforeUnmount(() => {
  stopAutoRefresh()
})
</script>
