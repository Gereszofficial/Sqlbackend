<template>
  <div class="grid gap-4">
    <div class="flex items-end justify-between">
      <div>
        <h1 class="text-xl font-semibold">Admin · Feladatok</h1>
        <p class="text-sm text-slate-400">Feladatok létrehozása és publikálása.</p>
      </div>
      <div class="flex gap-2">
        <button class="btn-ghost" @click="load">Frissítés</button>
        <RouterLink class="btn" to="/admin/tasks/new">Új feladat</RouterLink>
      </div>
    </div>

    <div v-if="loading" class="text-sm text-slate-400">Betöltés...</div>

    <div v-else class="card overflow-hidden">
      <table class="min-w-full text-sm">
        <thead class="bg-slate-900/60">
          <tr>
            <th class="text-left px-4 py-3 text-slate-300">ID</th>
            <th class="text-left px-4 py-3 text-slate-300">Kategória</th>
            <th class="text-left px-4 py-3 text-slate-300">Cím</th>
            <th class="text-left px-4 py-3 text-slate-300">Állapot</th>
            <th class="text-right px-4 py-3 text-slate-300">Művelet</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="t in tasks" :key="t.id" class="border-t border-slate-800">
            <td class="px-4 py-3 text-slate-300">#{{ t.id }}</td>
            <td class="px-4 py-3 text-slate-300">{{ t.category || '—' }}</td>
            <td class="px-4 py-3">{{ t.title }}</td>
            <td class="px-4 py-3">
              <span class="badge" :class="t.isPublished ? 'border-emerald-600/40 text-emerald-200' : 'border-slate-700 text-slate-300'">
                {{ t.isPublished ? 'Publikált' : 'Piszkozat' }}
              </span>
            </td>
            <td class="px-4 py-3 text-right">
              <div class="flex justify-end gap-2">
                <RouterLink class="btn-ghost" :to="`/admin/tasks/${t.id}`">Szerkesztés</RouterLink>
                <button class="btn-ghost text-red-300 hover:text-red-200" @click="del(t.id)">Törlés</button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-if="error" class="text-sm text-red-300 border border-red-900/50 bg-red-950/40 rounded-xl p-3">
      {{ error }}
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { http } from '@/api/http'
import type { AdminTaskSummary } from '@/types/api'

const tasks = ref<AdminTaskSummary[]>([])
const loading = ref(false)
const error = ref('')

async function load() {
  error.value = ''
  loading.value = true
  try {
    const res = await http.get<AdminTaskSummary[]>('/api/admin/tasks')
    tasks.value = res.data
  } catch (e: any) {
    error.value = e?.response?.data?.message || 'Nem sikerült betölteni az admin feladatokat.'
  } finally {
    loading.value = false
  }
}

onMounted(load)

async function del(id: number) {
  if (!confirm(`Biztos törlöd a #${id} feladatot?`)) return
  error.value = ''
  try {
    await http.delete(`/api/admin/tasks/${id}`)
    await load()
  } catch (e: any) {
    error.value = e?.response?.data?.message || 'Törlési hiba.'
  }
}
</script>
