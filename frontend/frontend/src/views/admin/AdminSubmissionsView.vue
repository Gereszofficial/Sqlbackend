<template>
  <div class="grid gap-4">
    <div class="flex items-end justify-between">
      <div>
        <h1 class="text-xl font-semibold">Beküldések</h1>
        <p class="text-sm text-slate-400">Diák beküldések listája, részletes nézettel és kézi értékeléssel.</p>
      </div>

      <div class="flex items-center gap-2">
        <select class="input" style="min-width: 240px" v-model.number="filterTaskId">
          <option :value="0">Összes feladat</option>
          <option v-for="t in tasks" :key="t.id" :value="t.id">#{{ t.id }} · {{ t.category || '—' }} · {{ t.title }}</option>
        </select>
        <button class="btn-ghost" @click="load">Frissítés</button>
      </div>
    </div>

    <div v-if="loading" class="text-sm text-slate-400">Betöltés...</div>

    <div v-else class="card overflow-hidden">
      <table class="min-w-full text-sm">
        <thead class="bg-slate-900/60">
          <tr>
            <th class="text-left px-4 py-3 text-slate-300">ID</th>
            <th class="text-left px-4 py-3 text-slate-300">Diák</th>
            <th class="text-left px-4 py-3 text-slate-300">Feladat</th>
            <th class="text-left px-4 py-3 text-slate-300">Automata</th>
            <th class="text-left px-4 py-3 text-slate-300">Értékelés</th>
            <th class="text-right px-4 py-3 text-slate-300">Művelet</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="s in submissions" :key="s.id" class="border-t border-slate-800">
            <td class="px-4 py-3 text-slate-300">#{{ s.id }}</td>
            <td class="px-4 py-3 text-slate-300">{{ s.studentEmail }}</td>
            <td class="px-4 py-3">#{{ s.taskId }} · {{ s.taskTitle }}</td>
            <td class="px-4 py-3">
              <span
                class="badge"
                :class="s.isCorrect ? 'border-emerald-600/40 text-emerald-200' : 'border-amber-600/40 text-amber-200'"
              >
                {{ s.isCorrect ? 'Helyes' : 'Nem jó / N/A' }}
              </span>
            </td>
            <td class="px-4 py-3">
              <span class="badge" v-if="s.score !== null && s.score !== undefined">Score: {{ s.score }}</span>
              <span class="text-slate-500" v-else>—</span>
            </td>
            <td class="px-4 py-3 text-right">
              <button class="btn-ghost" @click="openDetails(s.id)">Megnyitás</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-if="error" class="text-sm text-red-300 border border-red-900/50 bg-red-950/40 rounded-xl p-3">{{ error }}</div>

    <!-- Modal (egyszerű, dependency nélkül) -->
    <div
  v-if="detailsOpen"
  class="admin-submissions-overlay"
  @click.self="closeDetails"
>
  <div class="admin-submissions-modal">

        <div class="flex items-start justify-between gap-3">
          <div>
            <div class="text-sm text-slate-400">Beküldés</div>
            <div class="text-xl font-semibold">#{{ details?.id }} · {{ details?.taskTitle }}</div>
            <div class="text-xs text-slate-400 mt-1">{{ details?.studentEmail }} · {{ fmt(details?.submittedAtUtc || '') }}</div>
          </div>
          <button class="btn-ghost" @click="closeDetails">Bezárás ✕</button>
        </div>

        <!-- Tab navigáció: kompaktabb, átláthatóbb -->
        <div class="mt-4 flex flex-wrap gap-2">
          <button class="btn-tab" :class="activeTab === 'sql' ? 'btn-tab--active' : ''" @click="activeTab = 'sql'">SQL</button>
          <button class="btn-tab" :class="activeTab === 'auto' ? 'btn-tab--active' : ''" @click="activeTab = 'auto'">Automata</button>
          <button class="btn-tab" :class="activeTab === 'json' ? 'btn-tab--active' : ''" @click="activeTab = 'json'">JSON</button>
          <button class="btn-tab" :class="activeTab === 'review' ? 'btn-tab--active' : ''" @click="activeTab = 'review'">Értékelés</button>
        </div>

        <!-- SQL -->
        <div v-show="activeTab === 'sql'" class="mt-4 grid lg:grid-cols-2 gap-4">
          <div class="panel">
            <div class="flex items-center justify-between">
              <div class="text-xs text-slate-400">Beküldött SQL</div>
              <button class="btn-ghost" @click="copy(details?.sql || '')">Másolás</button>
            </div>
            <pre class="code-block mt-2">{{ details?.sql }}</pre>
          </div>

          <div class="panel">
            <div class="text-xs text-slate-400">Gyors infó</div>
            <div class="mt-2 flex flex-wrap gap-2">
              <span class="badge" :class="details?.isCorrect ? 'border-emerald-600/40 text-emerald-200' : 'border-amber-600/40 text-amber-200'">
                {{ details?.isCorrect ? 'Helyes' : 'Nem jó / N/A' }}
              </span>
              <span class="badge border-slate-700 text-slate-300">Status: {{ details?.status }}</span>
              <span class="badge border-slate-700 text-slate-300" v-if="details?.score !== null && details?.score !== undefined">Score: {{ details?.score }}</span>
            </div>
            <div class="text-xs text-slate-400 mt-3">A részleteket a többi fülön találod.</div>
          </div>
        </div>

        <!-- Automata -->
        <div v-show="activeTab === 'auto'" class="mt-4 panel">
          <div class="text-xs text-slate-400">Automata eredmény</div>
          <div class="mt-2 flex flex-wrap gap-2">
            <span class="badge" :class="details?.isCorrect ? 'border-emerald-600/40 text-emerald-200' : 'border-amber-600/40 text-amber-200'">
              {{ details?.isCorrect ? 'Helyes' : 'Nem jó / N/A' }}
            </span>
            <span class="badge border-slate-700 text-slate-300">Status: {{ details?.status }}</span>
            <span class="badge border-slate-700 text-slate-300" v-if="details?.score !== null && details?.score !== undefined">Score: {{ details?.score }}</span>
          </div>
          <div v-if="details?.runnerMessage" class="code-block mt-3">{{ details.runnerMessage }}</div>
        </div>

        <!-- JSON -->
        <div v-show="activeTab === 'json'" class="mt-4 grid lg:grid-cols-2 gap-4">
          <div class="panel">
            <div class="text-xs text-slate-400 mb-2">Diák eredmény (JSON)</div>
            <pre class="code-block">{{ prettyJson(details?.studentResultJson) }}</pre>
          </div>
          <div class="panel">
            <div class="text-xs text-slate-400 mb-2">Elvárt eredmény (JSON)</div>
            <pre class="code-block">{{ prettyJson(details?.expectedResultJson) }}</pre>
          </div>
        </div>

        <!-- Kézi értékelés -->
        <div v-show="activeTab === 'review'" class="mt-4 panel p-4">
          <div class="text-xs text-slate-400 mb-2">Kézi értékelés</div>
          <div class="flex flex-col md:flex-row gap-2">
            <input class="input" style="max-width: 140px" type="number" min="0" max="100" v-model.number="reviewDraft.score" />
            <input class="input" placeholder="Komment" v-model="reviewDraft.comment" />
            <button class="btn" @click="saveReview" :disabled="savingReview">{{ savingReview ? 'Mentés...' : 'Mentés' }}</button>
          </div>
          <div v-if="details?.review" class="text-xs text-slate-400 mt-2">
            Utolsó értékelés: Score {{ details.review.score }} · {{ fmt(details.review.reviewedAtUtc) }}
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { http } from '@/api/http'
import type { SubmissionDto, SubmissionDetailsDto, ReviewRequest, TaskSummary } from '@/types/api'

const submissions = ref<SubmissionDto[]>([])
const tasks = ref<TaskSummary[]>([])

const loading = ref(false)
const error = ref('')
const filterTaskId = ref<number>(0)

const detailsOpen = ref(false)
const details = ref<SubmissionDetailsDto | null>(null)
const activeTab = ref<'sql' | 'auto' | 'json' | 'review'>('sql')
const reviewDraft = ref<ReviewRequest>({ score: 100, comment: 'Rendben ✅' })
const savingReview = ref(false)

function fmt(utc: string) {
  try {
    return new Date(utc).toLocaleString()
  } catch {
    return utc
  }
}

function prettyJson(raw?: string | null) {
  if (!raw) return '—'
  try {
    return JSON.stringify(JSON.parse(raw), null, 2)
  } catch {
    return raw
  }
}

async function copy(text: string) {
  try {
    await navigator.clipboard.writeText(text)
  } catch {
    // ignore
  }
}

async function loadTasks() {
  // admin task lista kell a filter dropdownhoz
  // (a TaskSummary itt elég: id/title/category)
  const res = await http.get<any[]>('/api/admin/tasks')
  tasks.value = res.data.map((x) => ({ id: x.id, title: x.title, category: x.category }))
}

async function load() {
  error.value = ''
  loading.value = true
  try {
    const q = filterTaskId.value ? `?taskId=${filterTaskId.value}` : ''
    const res = await http.get<SubmissionDto[]>(`/api/admin/submissions${q}`)
    submissions.value = res.data
  } catch (e: any) {
    error.value = e?.response?.data?.message || 'Nem sikerült betölteni a beküldéseket.'
  } finally {
    loading.value = false
  }
}

async function openDetails(id: number) {
  error.value = ''
  try {
    const res = await http.get<SubmissionDetailsDto>(`/api/admin/submissions/${id}`)
    details.value = res.data
    const d = res.data
    reviewDraft.value = {
      score: d.review?.score ?? 100,
      comment: d.review?.comment ?? 'Rendben ✅'
    }
    detailsOpen.value = true
    activeTab.value = 'sql'
  } catch (e: any) {
    error.value = e?.response?.data?.message || 'Nem sikerült megnyitni a beküldést.'
  }
}

function closeDetails() {
  detailsOpen.value = false
  details.value = null
}

async function saveReview() {
  if (!details.value) return
  savingReview.value = true
  error.value = ''
  try {
    await http.post(`/api/admin/submissions/${details.value.id}/review`, reviewDraft.value)
    await openDetails(details.value.id)
    await load()
  } catch (e: any) {
    error.value = e?.response?.data?.message || 'Mentési hiba.'
  } finally {
    savingReview.value = false
  }
}

watch(filterTaskId, load)

onMounted(async () => {
  try {
    await loadTasks()
  } catch {
    // ha nem jön be, attól még a beküldések menjenek
  }
  await load()
})
</script>
