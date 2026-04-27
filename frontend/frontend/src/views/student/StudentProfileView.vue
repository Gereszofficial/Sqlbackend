<template>
  <div class="page-shell">
    <section class="page-head">
      <div class="flex items-center gap-4">
        <RankBadge v-if="progress.progress" :badge="progress.progress.badge" :size="64" />
        <div>
          <div class="badge mb-2">Saját profil</div>
          <h1 class="page-title">Haladásod</h1>
          <p class="page-copy mt-2">Itt látod a rangodat, a készültségi százalékot és mennyi feladatot oldottál meg eddig.</p>
        </div>
      </div>
      <button class="btn-ghost" @click="refresh" :disabled="progress.loading">{{ progress.loading ? '...' : 'Frissítés' }}</button>
    </section>

    <div class="card p-6" v-if="progress.progress">
      <div class="grid gap-4 md:grid-cols-3">
        <div class="metric-tile">
          <div class="text-xs text-slate-400">Rang</div>
          <div class="mt-1 text-2xl font-semibold">{{ progress.progress.badge.label }}</div>
          <div class="mt-1 text-xs text-slate-500">{{ auth.email }}</div>
        </div>
        <div class="metric-tile">
          <div class="text-xs text-slate-400">Teljesítés</div>
          <div class="mt-1 text-2xl font-semibold">{{ progress.progress.percent }}%</div>
          <div class="mt-1 text-xs text-slate-500">Összesített készültség</div>
        </div>
        <div class="metric-tile">
          <div class="text-xs text-slate-400">Megoldott feladatok</div>
          <div class="mt-1 text-2xl font-semibold">{{ progress.progress.completedCount }} / {{ progress.progress.totalTasks }}</div>
          <div class="mt-1 text-xs text-slate-500">Helyesen beküldött feladatok</div>
        </div>
      </div>

      <div class="h-3 rounded-full bg-slate-950 overflow-hidden border border-white/10 mt-6">
        <div class="h-full rounded-full bg-gradient-to-r from-sky-400 to-emerald-400" :style="{ width: `${progress.progress.percent}%` }" />
      </div>

      <div class="mt-4 text-sm text-slate-400">
        Helyes beküldés után a rang és a százalék automatikusan frissül.
      </div>
    </div>

    <div v-else class="text-sm text-slate-400">Nincs progress adat.</div>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue'
import { useProgressStore } from '@/stores/progress'
import { useAuthStore } from '@/stores/auth'
import RankBadge from '@/components/RankBadge.vue'

const progress = useProgressStore()
const auth = useAuthStore()

async function refresh() {
  await progress.fetchProgress({ notifyRankUp: false })
}

onMounted(() => {
  refresh()
})
</script>
