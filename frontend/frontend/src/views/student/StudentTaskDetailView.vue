<template>
  <div class="grid gap-4">
    <div class="flex items-start justify-between gap-3">
      <div>
        <RouterLink class="text-sm text-slate-400 hover:text-slate-200" to="/student/tasks">← Vissza</RouterLink>
        <div class="flex items-center gap-2 mt-1">
          <h1 class="text-xl font-semibold">{{ task?.title || 'Feladat' }}</h1>
          <span
            v-if="isCompleted"
            class="inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs bg-emerald-500/15 text-emerald-200 border border-emerald-600/30"
            title="Helyesen megoldva és elküldve"
          >
            ✓ Kész
          </span>
        </div>
        <p class="text-sm text-slate-400">Írd meg az SQL megoldást, futtasd tesztként, majd küldd be.</p>
      </div>
      <button class="btn-ghost" @click="load">Frissítés</button>
    </div>

    <div v-if="loading" class="text-sm text-slate-400">Betöltés...</div>

    <div
      v-if="!loading && submitNotice"
      ref="submitNoticeEl"
      class="rounded-2xl border border-emerald-600/30 bg-emerald-500/10 px-4 py-3 text-sm text-emerald-100"
    >
      <div class="font-semibold">Köszönöm a beküldést!</div>
      <div class="mt-1 text-emerald-100/80">{{ submitNotice }}</div>
    </div>

    <div v-if="!loading" class="task-detail-layout">
      <div class="card p-0 overflow-hidden task-panel task-panel--schema">
        <div class="flex items-center justify-between px-5 py-4 border-b border-slate-800">
          <button
            class="text-sm font-semibold flex items-center gap-2 hover:text-slate-200"
            type="button"
            @click="toggleSchema"
            :aria-expanded="schemaOpen"
          >
            <span>Database Schema</span>
            <span class="text-slate-500">{{ schemaOpen ? '▾' : '▸' }}</span>
          </button>

          <button
            class="btn-ghost"
            @click="refreshSchema"
            :disabled="schemaLoading"
            :title="schemaOpen ? 'Séma frissítése' : 'Séma betöltése és frissítése'"
          >
            {{ schemaLoading ? '...' : 'Frissítés' }}
          </button>
        </div>

        <div v-if="schemaOpen" class="p-5">
          <div v-if="schemaLoading">
            <div class="text-sm text-slate-400">Séma betöltése...</div>
          </div>

          <div v-else>
            <div v-if="schemaError" class="text-sm text-red-300 border border-red-900/50 bg-red-950/40 rounded-xl p-3">
              {{ schemaError }}
            </div>

            <div v-else-if="!schema?.tables?.length" class="text-sm text-slate-400">Nincs séma adat.</div>

            <div v-else class="grid md:grid-cols-2 gap-3">
              <div v-for="t in schema!.tables" :key="t.name" class="border border-slate-800 rounded-xl overflow-hidden">
                <button
                  type="button"
                  class="w-full px-4 py-2 bg-slate-950/50 border-b border-slate-800 flex items-center justify-between text-left"
                  @click="toggleSchemaTable(t.name)"
                  :aria-expanded="isSchemaTableOpen(t.name)"
                  :title="isSchemaTableOpen(t.name) ? 'Bezárás' : 'Megnyitás'"
                >
                  <div class="flex items-center gap-2">
                    <div class="text-sm font-semibold">{{ t.name }}</div>
                    <span class="text-slate-500">{{ isSchemaTableOpen(t.name) ? '▾' : '▸' }}</span>
                  </div>
                  <span class="badge">{{ t.columns.length }} oszlop</span>
                </button>

                <div v-if="isSchemaTableOpen(t.name)" class="overflow-auto max-h-44">
                  <table class="min-w-full text-xs">
                    <thead class="bg-slate-950/30 sticky top-0">
                      <tr>
                        <th class="text-left px-3 py-2 text-slate-300">Oszlop</th>
                        <th class="text-left px-3 py-2 text-slate-300">Típus</th>
                        <th class="text-left px-3 py-2 text-slate-300">Kulcs</th>
                        <th class="text-left px-3 py-2 text-slate-300">Null</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="c in t.columns" :key="c.name" class="border-t border-slate-800">
                        <td class="px-3 py-2 text-slate-100 font-mono">
                          <span class="inline-flex items-center gap-2">
                            <SchemaKeyIcon
                              v-if="hasAnyKey(c)"
                              :tone="c.isPrimaryKey ? 'gold' : 'gray'"
                              :title="getKeyTitle(c)"
                            />
                            <span>{{ c.name }}</span>
                          </span>
                        </td>
                        <td class="px-3 py-2 text-slate-200">{{ c.dataType }}</td>
                        <td class="px-3 py-2 text-slate-200">
                          <span v-if="c.isPrimaryKey || c.isForeignKey || c.isIndexed" class="inline-flex items-center gap-2">
                            <SchemaKeyIcon
                              v-if="hasAnyKey(c)"
                              :tone="c.isPrimaryKey ? 'gold' : 'gray'"
                              :title="getKeyTitle(c)"
                            />
                            <span>{{ getKeyLabel(c) }}</span>
                          </span>
                          <span v-else class="text-slate-500">—</span>
                        </td>
                        <td class="px-3 py-2 text-slate-400">{{ c.isNullable ? 'YES' : 'NO' }}</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div class="card p-0 overflow-hidden task-panel task-panel--description">
        <div class="flex items-center justify-between px-5 py-4 border-b border-slate-800">
          <div class="text-sm font-semibold">Feladat leírása</div>
          <button class="icon-pill" type="button" @click="descriptionOpen = !descriptionOpen" :title="descriptionOpen ? 'Bezárás' : 'Megnyitás'">
            {{ descriptionOpen ? '—' : '+' }}
          </button>
        </div>
        <div v-if="descriptionOpen" class="p-5 task-description-body">
          <MarkdownView :source="task?.descriptionMarkdown || ''" />
        </div>
      </div>

      <div class="card p-0 overflow-hidden task-panel task-panel--preview">
        <div class="flex items-center justify-between px-5 py-4 border-b border-slate-800">
          <div class="text-sm font-semibold">Táblák adatai</div>
          <button class="btn-ghost" @click="loadPreview" :disabled="previewLoading">{{ previewLoading ? '...' : 'Frissítés' }}</button>
        </div>

        <div class="p-5" v-if="previewLoading">
          <div class="text-sm text-slate-400">Adatok betöltése...</div>
        </div>

        <div class="p-5" v-else>
          <div v-if="previewError" class="text-sm text-red-300 border border-red-900/50 bg-red-950/40 rounded-xl p-3">
            {{ previewError }}
          </div>

          <div v-else-if="!preview?.tables?.length" class="text-sm text-slate-400">Nincs adat előnézet.</div>

          <div v-else class="grid gap-4">
            <div class="preview-tabs">
              <button
                v-for="t in preview!.tables"
                :key="t.name"
                type="button"
                class="preview-tab"
                :class="{ 'preview-tab--active': activePreviewTable === t.name }"
                @click="setActivePreviewTable(t.name)"
              >
                <span>{{ t.name }}</span>
                <span class="badge">{{ t.rows.length }} sor</span>
              </button>
            </div>

            <div v-if="activePreview" class="border border-slate-800 rounded-2xl overflow-hidden">
              <div class="px-4 py-3 border-b border-slate-800 bg-slate-950/40 flex items-center justify-between gap-3">
                <div class="text-sm font-semibold">{{ activePreview.name }}</div>
                <span class="badge">{{ activePreview.columns.length }} oszlop</span>
              </div>

              <div class="overflow-auto max-h-[220px]">
                <table class="min-w-full text-xs">
                  <thead class="bg-slate-950/30 sticky top-0">
                    <tr>
                      <th v-for="c in activePreview.columns" :key="c.name" class="text-left px-3 py-2 text-slate-300">
                        <span class="inline-flex items-center gap-2 whitespace-nowrap">
                          <SchemaKeyIcon
                            v-if="hasAnyKey(c)"
                            :tone="c.isPrimaryKey ? 'gold' : 'gray'"
                            :title="getKeyTitle(c)"
                          />
                          <span>{{ c.name }}</span>
                        </span>
                      </th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="(r, idx) in activePreview.rows" :key="idx" class="border-t border-slate-800">
                      <td v-for="c in activePreview.columns" :key="c.name" class="px-3 py-2 text-slate-100">{{ r[c.name] }}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div class="card p-5 task-panel task-panel--editor">
        <div class="flex items-center justify-between mb-2">
          <div class="text-sm font-semibold">SQL Editor</div>
          <span class="badge">Max 500 sor / 3s timeout</span>
        </div>

        <SqlMonacoEditor v-model="sql" :schema="schema?.tables" />

        <div class="flex flex-wrap gap-2 mt-3">
          <button class="btn-ghost" :disabled="busy || !sql.trim()" @click="run">Futtatás</button>
          <button class="btn" :disabled="busy || !sql.trim()" @click="submit">Beküldés</button>
          <button class="btn-ghost" :disabled="expectedLoading" @click="toggleExpectedPanel">
            {{ expectedLoading ? 'Betöltés...' : showExpectedPanel ? 'Elvárt eredmény bezárása' : 'Elvárt eredmény' }}
          </button>
        </div>

        <div v-if="error" class="text-sm text-red-300 border border-red-900/50 bg-red-950/40 rounded-xl p-3 mt-3">
          {{ error }}
        </div>

        <div v-if="showExpectedPanel" class="mt-4 border border-slate-800 rounded-2xl overflow-hidden">
          <div class="px-4 py-3 border-b border-slate-800 bg-slate-950/40 flex items-center justify-between gap-3">
            <div class="text-sm font-semibold">Elvárt eredmény</div>
            <span class="badge">{{ expectedColumns.length }} oszlop</span>
          </div>

          <div v-if="expectedError" class="p-4 text-sm text-red-300 bg-red-950/40 border-t border-red-900/50">
            {{ expectedError }}
          </div>

          <div v-else-if="!expectedRows.length" class="p-4 text-sm text-slate-400">
            Nincs megjeleníthető elvárt eredmény.
          </div>

          <div v-else class="overflow-auto max-h-[180px]">
            <table class="min-w-full text-xs">
              <thead class="bg-slate-900 sticky top-0">
                <tr>
                  <th v-for="c in expectedColumns" :key="c" class="text-left px-3 py-2 text-slate-300">{{ c }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(r, idx) in expectedRows" :key="idx" class="border-t border-slate-800">
                  <td v-for="c in expectedColumns" :key="c" class="px-3 py-2 text-slate-100">{{ r[c] }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <div v-if="result" class="mt-4">
          <div class="text-sm font-semibold mb-2">Eredmény</div>
          <div class="text-xs text-slate-400 mb-2" v-if="result.message">{{ result.message }}</div>

          <div v-if="result.rows && result.columns" class="overflow-auto max-h-[180px] border border-slate-800 rounded-xl">
            <table class="min-w-full text-xs">
              <thead class="bg-slate-900 sticky top-0">
                <tr>
                  <th v-for="c in result.columns" :key="c" class="text-left px-3 py-2 text-slate-300">{{ c }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(r, idx) in result.rows" :key="idx" class="border-t border-slate-800">
                  <td v-for="c in result.columns" :key="c" class="px-3 py-2 text-slate-100">{{ r[c] }}</td>
                </tr>
              </tbody>
            </table>
          </div>

          <div class="mt-3" v-if="result.matches !== undefined">
            <span
              class="badge"
              :class="result.matches ? 'border-emerald-600/40 text-emerald-200' : 'border-rose-600/40 text-rose-200'"
            >
              {{ result.matches ? '✓ Megfelelő eredmény' : '✕ Eltérés az elvárttól' }}
            </span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed, watch, nextTick } from 'vue'
import { useRoute } from 'vue-router'
import { http } from '@/api/http'
import type { TaskDetails, RunResult, TaskSchemaResponse, TaskPreviewResponse, TaskSummary, TablePreview, ExpectedResultResponse } from '@/types/api'
import SqlMonacoEditor from '@/components/SqlMonacoEditor.vue'
import MarkdownView from '@/components/MarkdownView.vue'
import SchemaKeyIcon from '@/components/SchemaKeyIcon.vue'
import { useProgressStore } from '@/stores/progress'

const progressStore = useProgressStore()

const route = useRoute()
const id = computed(() => Number(route.params.id))

const task = ref<TaskDetails | null>(null)
const loading = ref(false)
const busy = ref(false)
const error = ref('')
const sql = ref('')
const result = ref<RunResult | null>(null)
const schema = ref<TaskSchemaResponse | null>(null)
const schemaLoading = ref(false)
const schemaError = ref('')
const schemaOpen = ref(false)

const preview = ref<TaskPreviewResponse | null>(null)
const previewLoading = ref(false)
const previewError = ref('')
const descriptionOpen = ref(true)
const selectedPreviewTable = ref('')

const expectedRows = ref<any[]>([])
const expectedColumns = ref<string[]>([])
const expectedLoading = ref(false)
const expectedError = ref('')
const showExpectedPanel = ref(false)
const submitNotice = ref('')
const submitNoticeEl = ref<HTMLElement | null>(null)

const schemaTableOpen = ref<Record<string, boolean>>({})

function isSchemaTableOpen(name: string) {
  return schemaTableOpen.value[name] === true
}
function toggleSchemaTable(name: string) {
  schemaTableOpen.value[name] = !isSchemaTableOpen(name)
}

const activePreviewTable = computed(() => {
  const tables = preview.value?.tables || []
  if (!tables.length) return ''
  return selectedPreviewTable.value && tables.some(t => t.name === selectedPreviewTable.value)
    ? selectedPreviewTable.value
    : tables[0].name
})

const activePreview = computed<TablePreview | null>(() => {
  const tables = preview.value?.tables || []
  return tables.find(t => t.name === activePreviewTable.value) || null
})

function setActivePreviewTable(name: string) {
  selectedPreviewTable.value = name
}

async function toggleExpectedPanel() {
  showExpectedPanel.value = !showExpectedPanel.value
  if (showExpectedPanel.value && !expectedRows.value.length && !expectedLoading.value) {
    await loadExpectedResult()
  }
}

async function loadExpectedResult() {
  expectedError.value = ''
  expectedLoading.value = true
  try {
    const res = await http.get<ExpectedResultResponse>(`/api/student/tasks/${id.value}/expected-result`)
    expectedRows.value = res.data?.rows || []
    expectedColumns.value = toColumns(expectedRows.value)
  } catch (e: any) {
    expectedError.value = e?.response?.data?.message || 'Nem sikerült betölteni az elvárt eredményt.'
  } finally {
    expectedLoading.value = false
  }
}

async function scrollToSubmitFeedback() {
  await nextTick()
  submitNoticeEl.value?.scrollIntoView({ behavior: 'smooth', block: 'center' })
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

// Kész-e a feladat (Submitted + helyes) — a listából kérjük le, hogy ne nyúljunk a backendhez.
const isCompleted = ref(false)

async function refreshCompletion() {
  try {
    const res = await http.get<TaskSummary[]>('/api/student/tasks')
    const me = res.data.find(x => x.id === id.value)
    isCompleted.value = !!me?.isCompleted
  } catch {
    // ha nem megy, marad a korábbi állapot
  }
}

function safeParseRows(json?: string | null): any[] {
  if (!json) return []
  try {
    const v = JSON.parse(json)
    return Array.isArray(v) ? v : []
  } catch {
    return []
  }
}

function toColumns(rows: any[]): string[] {
  if (!rows?.length) return []
  const first = rows[0]
  if (first && typeof first === 'object') return Object.keys(first)
  return []
}

function hasAnyKey(column: { isPrimaryKey?: boolean; isForeignKey?: boolean; isIndexed?: boolean }) {
  return !!(column.isPrimaryKey || column.isForeignKey || column.isIndexed)
}

function getKeyLabel(column: { isPrimaryKey?: boolean; isForeignKey?: boolean; isIndexed?: boolean }) {
  if (column.isPrimaryKey) return 'Primary key'
  if (column.isForeignKey) return 'Foreign key'
  if (column.isIndexed) return 'Index'
  return '—'
}

function getKeyTitle(column: { isPrimaryKey?: boolean; isForeignKey?: boolean; isIndexed?: boolean }) {
  if (column.isPrimaryKey) return 'Elsődleges kulcs'
  if (column.isForeignKey) return 'Idegen kulcs'
  if (column.isIndexed) return 'Indexelt mező'
  return 'Kulcs mező'
}

async function load() {
  error.value = ''
  submitNotice.value = ''
  loading.value = true
  try {
    const res = await http.get<TaskDetails>(`/api/student/tasks/${id.value}`)
    task.value = res.data
    if (!sql.value.trim()) sql.value = task.value.starterSql || ''
    refreshCompletion()
  } catch (e: any) {
    error.value = e?.response?.data?.message || 'Nem sikerült betölteni a feladatot.'
  } finally {
    loading.value = false
  }
}

async function loadSchema() {
  schemaError.value = ''
  schemaLoading.value = true
  try {
    const res = await http.get<TaskSchemaResponse>(`/api/student/tasks/${id.value}/schema`)
    schema.value = res.data

    // Alapértelmezés: MINDEN tábla zárt.
    // Több táblás feladatnál így marad átlátható; a felhasználó nyitja meg gombbal/táblánként.
    const current = schemaTableOpen.value
    const tables = schema.value?.tables || []
    const hasAny = tables.some(t => typeof current[t.name] === 'boolean')
    if (!hasAny) {
      const next: Record<string, boolean> = {}
      for (const t of tables) next[t.name] = false
      schemaTableOpen.value = next
    }
  } catch (e: any) {
    schemaError.value = e?.response?.data?.message || 'Nem sikerült betölteni a sémát.'
  } finally {
    schemaLoading.value = false
  }
}

function toggleSchema() {
  schemaOpen.value = !schemaOpen.value
  if (schemaOpen.value && !schema.value && !schemaLoading.value) {
    loadSchema()
  }
}

function refreshSchema() {
  // Ha frissítesz, nyissuk is ki, hogy lásd az eredményt.
  schemaOpen.value = true
  loadSchema()
}

async function loadPreview() {
  previewError.value = ''
  previewLoading.value = true
  try {
    const res = await http.get<TaskPreviewResponse>(`/api/student/tasks/${id.value}/preview`)
    preview.value = res.data

    const tables = preview.value?.tables || []
    if (tables.length && !tables.some(t => t.name === selectedPreviewTable.value)) {
      selectedPreviewTable.value = tables[0].name
    }
  } catch (e: any) {
    previewError.value = e?.response?.data?.message || 'Nem sikerült betölteni a táblák adatait.'
  } finally {
    previewLoading.value = false
  }
}

async function run() {
  result.value = null
  error.value = ''
  submitNotice.value = ''
  busy.value = true
  try {
    // Backend: RunSqlResponse { ok, isCorrect, message, studentResultJson, expectedResultJson }
    const res = await http.post<any>(`/api/student/tasks/${id.value}/run`, { sql: sql.value })
    const rows = safeParseRows(res.data?.studentResultJson)
    const expectedRows = safeParseRows(res.data?.expectedResultJson)
    result.value = {
      ok: !!res.data?.ok,
      message: res.data?.message,
      rows,
      columns: toColumns(rows),
      expectedRows,
      expectedColumns: toColumns(expectedRows),
      matches: res.data?.isCorrect ?? undefined
    }
  } catch (e: any) {
    error.value = e?.response?.data?.message || 'Futtatási hiba.'
  } finally {
    busy.value = false
  }
}

async function submit() {
  result.value = null
  error.value = ''
  submitNotice.value = ''
  busy.value = true
  try {
    // Backend: SubmitResponse { submissionId, isCorrect, message }
    const res = await http.post<any>(`/api/student/tasks/${id.value}/submit`, { sql: sql.value })
    result.value = {
      ok: true,
      message: res.data?.message,
      matches: res.data?.isCorrect ?? undefined
    }
    submitNotice.value = res.data?.message || 'A megoldásod rögzítve lett.'

    // Ha helyes, azonnali visszajelzés: zöld pipa + kész státusz (reallife)
    if (res.data?.isCorrect === true) {
      isCompleted.value = true
      // Rank badge automatikus frissítés + rank-up értesítő
      await progressStore.fetchProgress({ notifyRankUp: true })
    } else {
      // ha nem helyes, frissítjük biztos ami biztos (pl. már korábban kész volt)
      await refreshCompletion()
    }

    await scrollToSubmitFeedback()
  } catch (e: any) {
    error.value = e?.response?.data?.message || 'Beküldési hiba.'
  } finally {
    busy.value = false
  }
}

onMounted(async () => {
  await load()
  // Krumpli gépbarát: ne rendereljünk azonnal mindent.
  // Az előnézet marad, de késleltetve töltjük, hogy az UI hamarabb "felálljon".
  const idle = (cb: () => void) => {
    const w = window as any
    if (typeof w.requestIdleCallback === 'function') w.requestIdleCallback(cb)
    else setTimeout(cb, 150)
  }

  idle(() => {
    // Sémát csak akkor töltünk, ha a felhasználó kinyitja (vagy frissít).
    loadPreview()
  })
})

watch(
  () => id.value,
  async () => {
    await load()
    // ID váltáskor reseteljük a sémát, és csak akkor töltjük újra, ha ki van nyitva.
    schema.value = null
    schemaError.value = ''
    schemaTableOpen.value = {}

    preview.value = null
    previewError.value = ''
    selectedPreviewTable.value = ''
    expectedRows.value = []
    expectedColumns.value = []
    expectedError.value = ''
    showExpectedPanel.value = false
    submitNotice.value = ''

    const idle = (cb: () => void) => {
      const w = window as any
      if (typeof w.requestIdleCallback === 'function') w.requestIdleCallback(cb)
      else setTimeout(cb, 150)
    }

    idle(() => {
      if (schemaOpen.value) loadSchema()
      loadPreview()
    })
  }
)

// (a schema + preview is frissül id váltáskor is)
</script>
