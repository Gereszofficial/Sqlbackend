<template>
  <div class="min-h-screen relative">
    <header class="sticky top-0 z-30 border-b border-white/10 bg-[#07111f]/82 backdrop-blur-xl">
      <div class="mx-auto max-w-7xl px-4 py-4 flex flex-wrap items-center justify-between gap-4">
        <div class="flex items-center gap-3 min-w-0">
          <div class="h-11 w-11 rounded-2xl bg-slate-100 text-slate-950 grid place-items-center font-black shadow">SQL</div>
          <div class="min-w-0">
            <div class="text-base font-semibold leading-tight tracking-tight">SqlTrainer</div>
            <div class="text-xs text-slate-400 leading-tight truncate">egyszerűbb, gyorsabb, átláthatóbb kezelőfelület</div>
          </div>
        </div>

        <nav class="flex flex-wrap items-center gap-2 justify-end">
          <RouterLink v-if="auth.isStudent" class="btn-ghost" to="/student/topics">Témák</RouterLink>
          <RouterLink v-if="auth.isStudent" class="btn-ghost" to="/student/tasks">Feladatok</RouterLink>
          <RouterLink v-if="auth.isStudent" class="btn-ghost" to="/student/profile">Profil</RouterLink>
          <RouterLink v-if="auth.isAdmin" class="btn-ghost" to="/admin/tasks">Feladatok</RouterLink>
          <RouterLink v-if="auth.isAdmin" class="btn-ghost" to="/admin/topics">Témák</RouterLink>
          <RouterLink v-if="auth.isAdmin" class="btn-ghost" to="/admin/submissions">Beküldések</RouterLink>
          <RouterLink v-if="auth.isAdmin" class="btn-ghost" to="/admin/rank">Rank</RouterLink>

          <div class="h-7 w-px bg-white/10 mx-1 hidden md:block" />

          <div class="flex items-center gap-3 rounded-2xl border border-white/10 bg-white/[0.02] px-3 py-2 mr-1">
            <RouterLink v-if="auth.isStudent && progress.progress" class="flex items-center gap-2" to="/student/profile">
              <RankBadge :badge="progress.progress.badge" :size="26" />
              <div class="text-left leading-tight">
                <div class="text-[11px] text-slate-200">{{ progress.progress.badge.label }}</div>
                <div class="text-[11px] text-slate-500">{{ progress.progress.percent }}% kész</div>
              </div>
            </RouterLink>

            <div class="text-right leading-tight">
              <div class="text-xs text-slate-200">{{ auth.email || '—' }}</div>
              <div class="text-[11px] uppercase tracking-[0.16em] text-slate-500">{{ auth.role }}</div>
            </div>
          </div>

          <button class="btn" @click="logout">Kilépés</button>
        </nav>
      </div>
    </header>

    <main class="relative mx-auto max-w-7xl px-4 py-6 md:py-8">
      <RouterView />
    </main>

    <footer class="mx-auto max-w-7xl px-4 py-10 text-xs text-slate-500">
      SqlTrainer · Vue + ASP.NET · UI refresh
    </footer>
  </div>
</template>

<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'
import { useProgressStore } from '@/stores/progress'
import { useRouter } from 'vue-router'
import { onMounted, watch } from 'vue'
import RankBadge from '@/components/RankBadge.vue'
const auth = useAuthStore()
const progress = useProgressStore()
const router = useRouter()
async function logout() {
  await auth.logout()
  progress.clear()
  router.push('/login')
}

onMounted(() => {
  if (auth.isStudent) progress.fetchProgress({ notifyRankUp: false })
})

watch(
  () => auth.sessionNonce,
  () => {
    if (auth.isStudent) progress.fetchProgress({ notifyRankUp: false })
  }
)
</script>
