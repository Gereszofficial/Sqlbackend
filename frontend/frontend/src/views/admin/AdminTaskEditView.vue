<template>
  <div class="grid gap-4">
    <div class="flex items-start justify-between gap-3">
      <div>
        <RouterLink class="text-sm text-slate-400 hover:text-slate-200" to="/admin/tasks">← Vissza</RouterLink>
        <h1 class="text-xl font-semibold mt-1">{{ isNew ? 'Új feladat' : `Feladat #${taskId}` }}</h1>
        <p class="text-sm text-slate-400">
          Profi feladat-szerkesztő: kategória, kezdő SQL, seed import, SQL mód (SELECT-only vs sandbox safe).
        </p>
      </div>
      <div class="flex gap-2">
        <button class="btn" :disabled="saving" @click="save">{{ saving ? 'Mentés...' : 'Mentés' }}</button>
      </div>
    </div>

    <div class="grid lg:grid-cols-2 gap-4">
      <!-- Meta / leírás -->
      <div class="card p-5 space-y-3">
        <div class="grid md:grid-cols-2 gap-3">
          <div>
            <label class="text-xs text-slate-400">Kategória (pl. Törpék)</label>
            <input class="input mt-1" v-model="form.category" placeholder="Törpék" />
          </div>
          <div>
            <label class="text-xs text-slate-400">SQL mód</label>
            <select class="input mt-1" v-model="form.sqlMode">
              <option value="SelectOnly">SelectOnly (csak SELECT)</option>
              <option value="SandboxSafe">SandboxSafe (DDL/DML engedélyezett, veszélyes tiltva)</option>
            </select>
          </div>
        </div>

        <div>
          <label class="text-xs text-slate-400">Cím</label>
          <input class="input mt-1" v-model="form.title" />
        </div>
        <div>
          <label class="text-xs text-slate-400">Leírás (Markdown)</label>
          <textarea class="input mt-1 min-h-[160px]" v-model="form.descriptionMarkdown"></textarea>
        </div>
        <div class="flex items-center gap-2">
          <input id="pub" type="checkbox" v-model="form.isPublished" class="h-4 w-4" />
          <label for="pub" class="text-sm">Publikált</label>
        </div>
      </div>

      <!-- SQL editor részek -->
      <div class="card p-5 space-y-4">
        <div>
          <div class="flex items-center justify-between gap-3">
            <label class="text-xs text-slate-400">Kezdő SQL (ezt látja a diák alapból)</label>
            <button class="btn-ghost" @click="setDefaultStarter">Alap sablon</button>
          </div>
          <textarea class="input mt-1 min-h-[120px] font-mono text-xs" spellcheck="false" v-model="form.starterSql"></textarea>
        </div>

        <div>
          <div class="flex items-center justify-between gap-3">
            <label class="text-xs text-slate-400">Seed SQL (DDL + adatok)</label>
            <div class="flex items-center gap-2">
              <label class="btn-ghost cursor-pointer">
                <input class="hidden" type="file" accept=".sql,.txt" @change="onFileImport($event, 'seed')" />
                Import .sql
              </label>
              <button class="btn-ghost" @click="form.seedSql = ''">Törlés</button>
            </div>
          </div>
          <textarea class="input mt-1 min-h-[200px] font-mono text-xs" spellcheck="false" v-model="form.seedSql"></textarea>
        </div>

        <div>
          <div class="flex items-center justify-between gap-3">
            <label class="text-xs text-slate-400">Expected SQL (helyes megoldás, referencia)</label>
            <div class="flex items-center gap-2">
              <label class="btn-ghost cursor-pointer">
                <input class="hidden" type="file" accept=".sql,.txt" @change="onFileImport($event, 'expected')" />
                Import .sql
              </label>
              <button class="btn-ghost" @click="form.expectedSql = ''">Törlés</button>
            </div>
          </div>
          <textarea class="input mt-1 min-h-[160px] font-mono text-xs" spellcheck="false" v-model="form.expectedSql"></textarea>
        </div>
      </div>
    </div>

    <div v-if="error" class="text-sm text-red-300 border border-red-900/50 bg-red-950/40 rounded-xl p-3">{{ error }}</div>
    <div v-if="ok" class="text-sm text-emerald-200 border border-emerald-900/40 bg-emerald-950/30 rounded-xl p-3">Mentve ✅</div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { http } from '@/api/http'
import type { AdminTaskCreate, CreateIdResponse } from '@/types/api'

const route = useRoute()
const router = useRouter()

const taskId = computed(() => Number(route.params.id))
const isNew = computed(() => route.path.endsWith('/new'))

const form = reactive<AdminTaskCreate>({
  category: '',
  title: '',
  descriptionMarkdown: '',
  starterSql: '',
  seedSql: '',
  expectedSql: '',
  sqlMode: 'SelectOnly',
  isPublished: false
})

const error = ref('')
const ok = ref(false)
const saving = ref(false)

onMounted(async () => {
  if (isNew.value) {
    setDefaultStarter()
    return
  }
  try {
    const res = await http.get<AdminTaskCreate>(`/api/admin/tasks/${taskId.value}`)
    Object.assign(form, res.data)
    if (!form.starterSql) setDefaultStarter()
  } catch (e: any) {
    error.value = e?.response?.data?.message || 'Nem sikerült betölteni.'
  }
})

function setDefaultStarter() {
  if (form.starterSql?.trim()) return
  form.starterSql = "SELECT\n  *\nFROM \n  -- írd ide a táblát\n;\n"
}

async function onFileImport(ev: Event, target: 'seed' | 'expected') {
  const input = ev.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file) return
  try {
    const text = await file.text()
    if (target === 'seed') form.seedSql = text
    else form.expectedSql = text
  } catch {
    error.value = 'Nem sikerült beolvasni a fájlt.'
  } finally {
    // ugyanaz a file újra kiválasztható legyen
    input.value = ''
  }
}

function toSqlModeValue(v: string) {
  // backend enum: 0=SelectOnly, 1=SandboxSafe
  return v === "SandboxSafe" ? 1 : 0
}

async function save() {
  error.value = ""
  ok.value = false
  saving.value = true

  // FLAT payload – ez kell a backend AdminTaskUpsert-nek
  const payload = {
    category: form.category,
    title: form.title,
    descriptionMarkdown: form.descriptionMarkdown,
    starterSql: form.starterSql,
    seedSql: form.seedSql,
    expectedSql: form.expectedSql,
    sqlMode: toSqlModeValue(form.sqlMode),
    isPublished: form.isPublished,
  }

  try {
    if (isNew.value) {
      const res = await http.post<CreateIdResponse>("/api/admin/tasks", payload)
      ok.value = true
      router.push(`/admin/tasks/${res.data.id}`)
    } else {
      await http.put(`/api/admin/tasks/${taskId.value}`, payload)
      ok.value = true
    }
  } catch (e: any) {
    error.value = e?.response?.data?.message || "Mentési hiba."
  } finally {
    saving.value = false
    setTimeout(() => (ok.value = false), 2000)
  }
}

</script>
