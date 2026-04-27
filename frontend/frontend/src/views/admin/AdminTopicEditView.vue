<template>
  <div class="grid gap-4">
    <div class="flex items-start justify-between gap-3">
      <div>
        <RouterLink class="text-sm text-slate-400 hover:text-slate-200" to="/admin/topics">← Vissza</RouterLink>
        <h1 class="text-xl font-semibold mt-1">Admin · {{ isNew ? 'Új téma' : `Téma #${topicId}` }}</h1>
        <p class="text-sm text-slate-400">Anyag (Markdown) + több feladat mentése egyben.</p>
      </div>
      <div class="flex gap-2">
        <button class="btn-ghost" @click="load" v-if="!isNew">Frissítés</button>
        <button class="btn" v-if="!isNew || createMode==='manual'" :disabled="saving" @click="save">
          {{ saving ? 'Mentés...' : 'Mentés' }}
        </button>
      </div>
    </div>

    <div v-if="loading" class="text-sm text-slate-400">Betöltés...</div>

    <div v-else class="grid gap-4">
      <!-- Create mode chooser (manual vs upload) -->
      <div v-if="isNew" class="card p-5">
        <div class="text-sm font-semibold">Téma létrehozása</div>
        <div class="text-xs text-slate-400 mt-1">Válassz: manuális szerkesztés vagy kész téma import (ZIP).</div>

        <div class="flex flex-wrap gap-2 mt-3">
          <button type="button" class="btn" :class="createMode === 'manual' ? '' : 'btn-ghost'" @click="createMode='manual'">
            Manuális
          </button>
          <button type="button" class="btn" :class="createMode === 'upload' ? '' : 'btn-ghost'" @click="createMode='upload'">
            Feltöltés (ZIP)
          </button>
        </div>

        <div v-if="createMode==='upload'" class="mt-4 grid gap-3">
          <div class="text-sm text-slate-300">Húzd be a ZIP-et vagy válaszd ki.</div>

          <div
            class="border border-dashed border-slate-700 rounded-2xl p-6 text-center bg-slate-950/30"
            @dragover.prevent
            @drop.prevent="onDrop"
          >
            <div class="text-sm text-slate-200">Drag & drop ZIP ide</div>
            <div class="text-xs text-slate-500 mt-1">Minimum: topic.json vagy topic.md + tasks/*.json (opcionális: tasks/*.md)</div>
            <div class="mt-3">
              <input type="file" accept=".zip" @change="onPick" />
            </div>
            <div v-if="importFileName" class="text-xs text-slate-400 mt-2">Kiválasztva: {{ importFileName }}</div>
          </div>

          <div class="flex items-center gap-2">
            <button class="btn" type="button" :disabled="importing || !importFile" @click="doImport">
              {{ importing ? 'Importálás...' : 'Importálás' }}
            </button>
            <div class="text-xs text-slate-500">Az import létrehozza a témát + feladatokat.</div>
          </div>

          <div v-if="importError" class="text-sm text-red-300 border border-red-900/50 bg-red-950/40 rounded-xl p-3">
            {{ importError }}
          </div>

          <div v-if="importSuccess" class="text-sm text-emerald-200 border border-emerald-900/40 bg-emerald-950/20 rounded-xl p-3">
            {{ importSuccess }}
          </div>
        </div>
      </div>

      <!-- Topic core -->
      <div class="card p-5" v-if="!isNew || createMode==='manual'">
        <div class="grid md:grid-cols-3 gap-3">
          <label class="grid gap-1">
            <span class="text-xs text-slate-400">Cím</span>
            <input class="input" v-model="form.title" placeholder="Pl. JOIN alapok" />
          </label>
          <label class="grid gap-1">
            <span class="text-xs text-slate-400">Slug (opcionális)</span>
            <input class="input" v-model="form.slug" placeholder="join-alapok" />
          </label>
          <label class="flex items-center gap-2 mt-6 md:mt-0">
            <input type="checkbox" v-model="form.isPublished" />
            <span class="text-sm">Publikált</span>
          </label>
        </div>

        <div class="grid lg:grid-cols-2 gap-3 mt-4">
          <div>
            <div class="text-xs text-slate-400 mb-1">Téma anyaga (Markdown)</div>
            <textarea class="textarea" rows="10" v-model="form.descriptionMarkdown" placeholder="# Elmélet\n\n- pont\n- pont"></textarea>
          </div>
          <div>
            <div class="text-xs text-slate-400 mb-1">Előnézet</div>
            <div class="border border-slate-800 rounded-xl p-3">
              <MarkdownView :source="form.descriptionMarkdown" />
            </div>
          </div>
        </div>
      </div>

      <!-- Tasks in topic -->
      <div class="card p-5" v-if="!isNew || createMode==='manual'">
        <div class="flex items-center justify-between mb-3">
          <div>
            <div class="text-sm font-semibold">Feladatok ebben a témában</div>
            <div class="text-xs text-slate-400">Új feladat: "+" · Törlés: kuka · Megnyitás: katt</div>
          </div>
          <button class="btn" type="button" @click="addTask">+ Új feladat</button>
        </div>

        <div v-if="!form.tasks.length" class="text-sm text-slate-400">Még nincs feladat.</div>

        <div v-else class="grid gap-3">
          <div v-for="(t, idx) in form.tasks" :key="t._key" class="border border-slate-800 rounded-xl overflow-hidden">
            <button
              type="button"
              class="w-full px-4 py-3 bg-slate-950/50 border-b border-slate-800 flex items-center justify-between text-left"
              @click="toggleOpen(t._key)"
              :aria-expanded="isOpen(t._key)"
            >
              <div class="flex items-center gap-3">
                <div class="text-xs text-slate-500">#{{ idx + 1 }}</div>
                <div class="font-semibold">{{ t.title || 'Új feladat' }}</div>
                <span class="badge" :class="t.isPublished ? 'border-emerald-600/40 text-emerald-200' : 'border-slate-700 text-slate-300'">
                  {{ t.isPublished ? 'Publikált' : 'Piszkozat' }}
                </span>
              </div>
              <div class="flex items-center gap-2">
                <span class="text-slate-500">{{ isOpen(t._key) ? '▾' : '▸' }}</span>
                <button
                  type="button"
                  class="btn-ghost text-red-300 hover:text-red-200"
                  title="Feladat törlése"
                  @click.stop="removeTask(t._key)"
                >
                  🗑
                </button>
              </div>
            </button>

            <div v-if="isOpen(t._key)" class="p-4 grid gap-3">
              <div class="grid md:grid-cols-3 gap-3">
                <label class="grid gap-1 md:col-span-2">
                  <span class="text-xs text-slate-400">Cím</span>
                  <input class="input" v-model="t.title" placeholder="Pl. INNER JOIN" />
                </label>
                <div class="grid gap-1">
                  <span class="text-xs text-slate-400">SQL mód</span>
                  <select class="input" v-model="t.sqlMode">
                    <option value="SelectOnly">SelectOnly</option>
                    <option value="SandboxSafe">SandboxSafe</option>
                  </select>
                </div>
              </div>

              <label class="flex items-center gap-2">
                <input type="checkbox" v-model="t.isPublished" />
                <span class="text-sm">Publikált</span>
              </label>

              <div class="grid lg:grid-cols-2 gap-3">
                <div>
                  <div class="text-xs text-slate-400 mb-1">Leírás (Markdown)</div>
                  <textarea class="textarea" rows="8" v-model="t.descriptionMarkdown" placeholder="Feladat leírás...\n\n- pont"></textarea>
                </div>
                <div>
                  <div class="text-xs text-slate-400 mb-1">Előnézet</div>
                  <div class="border border-slate-800 rounded-xl p-3">
                    <MarkdownView :source="t.descriptionMarkdown" />
                  </div>
                </div>
              </div>

              <div class="grid lg:grid-cols-2 gap-3">
                <div>
                  <div class="text-xs text-slate-400 mb-1">Starter SQL (opcionális)</div>
                  <textarea class="textarea font-mono" rows="6" v-model="t.starterSql" placeholder="SELECT ..."></textarea>
                </div>
                <div>
                  <div class="text-xs text-slate-400 mb-1">Seed SQL (DDL + adatok)</div>
                  <textarea class="textarea font-mono" rows="6" v-model="t.seedSql" placeholder="CREATE TABLE ...;\nINSERT ...; "></textarea>
                </div>
              </div>

              <div>
                <div class="text-xs text-slate-400 mb-1">Expected SQL (helyes megoldás)</div>
                <textarea class="textarea font-mono" rows="6" v-model="t.expectedSql" placeholder="SELECT ..."></textarea>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div v-if="error" class="text-sm text-red-300 border border-red-900/50 bg-red-950/40 rounded-xl p-3">
        {{ error }}
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { http } from '@/api/http'
import type { AdminTopicWithTasks, CreateIdResponse } from '@/types/api'
import MarkdownView from '@/components/MarkdownView.vue'

type UiTask = {
  _key: string
  id?: number | null
  title: string
  descriptionMarkdown: string
  starterSql: string
  seedSql: string
  expectedSql: string
  sqlMode: 'SelectOnly' | 'SandboxSafe'
  isPublished: boolean
}

const route = useRoute()
const router = useRouter()

// NOTE: /admin/topics/new is a dedicated route (no :id param), so route.params.id is undefined there.
// Avoid Number(undefined) => NaN, otherwise we'd call /api/admin/topics/NaN.
const isNew = computed(() => route.path.endsWith('/admin/topics/new') || route.path.endsWith('/topics/new') || route.params.id === 'new')
const topicId = computed(() => {
  const v = route.params.id
  if (typeof v !== 'string') return null
  const n = Number(v)
  return Number.isFinite(n) ? n : null
})

const loading = ref(false)
const saving = ref(false)
const error = ref('')

// New topic: manual vs upload
const createMode = ref<'manual' | 'upload'>('manual')
const importFile = ref<File | null>(null)
const importFileName = ref('')
const importing = ref(false)
const importError = ref('')
const importSuccess = ref('')

function onPick(ev: Event) {
  const input = ev.target as HTMLInputElement
  const f = input.files?.[0] ?? null
  importFile.value = f
  importFileName.value = f?.name ?? ''
  importError.value = ''
  importSuccess.value = ''
}

function onDrop(ev: DragEvent) {
  const f = ev.dataTransfer?.files?.[0] ?? null
  if (!f) return
  importFile.value = f
  importFileName.value = f.name
  importError.value = ''
  importSuccess.value = ''
}

async function doImport() {
  if (!importFile.value) return
  importing.value = true
  importError.value = ''
  importSuccess.value = ''
  try {
    const fd = new FormData()
    fd.append('file', importFile.value)
    const res = await http.post<CreateIdResponse>('/api/admin/topics/import', fd, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    const id = res.data.id
    importSuccess.value = `Sikeres import! (Téma #${id})`
    // Navigate to the created topic. This component is reused between routes,
    // so we also trigger a reload via the route watcher below.
    await router.push(`/admin/topics/${id}`)
  } catch (e: any) {
    importError.value = e?.response?.data ?? e?.response?.data?.message ?? 'Nem sikerült importálni a ZIP-et.'
  } finally {
    importing.value = false
  }
}
const open = ref<Record<string, boolean>>({})

const form = ref<{
  title: string
  slug: string
  descriptionMarkdown: string
  isPublished: boolean
  tasks: UiTask[]
}>({
  title: '',
  slug: '',
  descriptionMarkdown: '',
  isPublished: false,
  tasks: []
})

function newKey() {
  return Math.random().toString(36).slice(2)
}

function addTask() {
  const k = newKey()
  form.value.tasks.push({
    _key: k,
    id: null,
    title: '',
    descriptionMarkdown: '',
    starterSql: '',
    seedSql: '',
    expectedSql: '',
    sqlMode: 'SelectOnly',
    isPublished: false
  })
  open.value[k] = true
}

function removeTask(key: string) {
  if (!confirm('Biztos törlöd ezt a feladatot a témából?')) return
  form.value.tasks = form.value.tasks.filter((x) => x._key !== key)
  delete open.value[key]
}

function isOpen(key: string) {
  return open.value[key] === true
}

function toggleOpen(key: string) {
  open.value[key] = !isOpen(key)
}

async function load() {
  if (isNew.value || topicId.value == null) return
  error.value = ''
  loading.value = true
  try {
    const res = await http.get<AdminTopicWithTasks>(`/api/admin/topics/${topicId.value}`)
    const data = res.data
    form.value.title = data.title
    form.value.slug = data.slug
    form.value.descriptionMarkdown = data.descriptionMarkdown
    form.value.isPublished = data.isPublished
    form.value.tasks = (data.tasks || []).map((t) => ({
      _key: newKey(),
      id: t.id ?? null,
      title: t.title,
      descriptionMarkdown: t.descriptionMarkdown,
      starterSql: t.starterSql,
      seedSql: t.seedSql,
      expectedSql: t.expectedSql,
      sqlMode: t.sqlMode,
      isPublished: t.isPublished
    }))
    // open first one for convenience
    if (form.value.tasks[0]) open.value[form.value.tasks[0]._key] = true
  } catch (e: any) {
    error.value = e?.response?.data?.message || 'Nem sikerült betölteni a témát.'
  } finally {
    loading.value = false
  }
}

async function save() {
  error.value = ''
  saving.value = true
  try {
    if (!form.value.title.trim()) throw new Error('A téma címe kötelező.')

    if (isNew.value) {
      const createRes = await http.post<CreateIdResponse>('/api/admin/topics', {
        title: form.value.title,
        slug: form.value.slug,
        descriptionMarkdown: form.value.descriptionMarkdown,
        isPublished: form.value.isPublished
      })
      const newId = createRes.data.id
      // after create, immediately upsert tasks+topic in one go
      await http.put(`/api/admin/topics/${newId}`, {
        id: newId,
        title: form.value.title,
        slug: form.value.slug,
        descriptionMarkdown: form.value.descriptionMarkdown,
        isPublished: form.value.isPublished,
        tasks: form.value.tasks.map((t) => ({
          id: t.id ?? null,
          title: t.title,
          descriptionMarkdown: t.descriptionMarkdown,
          starterSql: t.starterSql,
          seedSql: t.seedSql,
          expectedSql: t.expectedSql,
          sqlMode: t.sqlMode,
          isPublished: t.isPublished
        }))
      })
      router.replace(`/admin/topics/${newId}`)
      return
    }

    if (topicId.value == null) throw new Error('Érvénytelen téma azonosító.')
    await http.put(`/api/admin/topics/${topicId.value}`, {
      id: topicId.value,
      title: form.value.title,
      slug: form.value.slug,
      descriptionMarkdown: form.value.descriptionMarkdown,
      isPublished: form.value.isPublished,
      tasks: form.value.tasks.map((t) => ({
        id: t.id ?? null,
        title: t.title,
        descriptionMarkdown: t.descriptionMarkdown,
        starterSql: t.starterSql,
        seedSql: t.seedSql,
        expectedSql: t.expectedSql,
        sqlMode: t.sqlMode,
        isPublished: t.isPublished
      }))
    })
    await load()
    alert('Mentve!')
  } catch (e: any) {
    error.value = e?.response?.data?.message || e?.message || 'Mentési hiba.'
  } finally {
    saving.value = false
  }
}

onMounted(load)

// IMPORTANT: this view is reused when navigating /admin/topics/:id.
// After import (or clicking another topic), we must reload the topic data.
watch(
  () => route.params.id,
  async () => {
    open.value = {}
    if (isNew.value) return
    await load()
  }
)
</script>
