<template>
  <div class="login-shell">
    <div class="w-full max-w-md card p-6 lg:p-8">
      <div class="flex items-center gap-3 mb-8">
        <div class="h-12 w-12 rounded-2xl bg-slate-100 text-slate-950 grid place-items-center font-black text-lg">SQL</div>
        <div>
          <div class="text-xl font-semibold">Regisztráció</div>
          <div class="text-sm text-slate-400">Hozz létre egy új diák fiókot</div>
        </div>
      </div>

      <form class="space-y-4" @submit.prevent="onSubmit">
        <div>
          <label class="text-xs text-slate-400">Email</label>
          <input class="input mt-2" v-model="email" type="email" autocomplete="email" required />
        </div>
        <div>
          <label class="text-xs text-slate-400">Jelszó</label>
          <input class="input mt-2" v-model="password" type="password" autocomplete="new-password" required />
          <div class="text-[11px] text-slate-500 mt-2">Ajánlott: legalább 6 karakter és legyen benne szám is.</div>
        </div>

        <div v-if="error" class="text-sm text-red-300 border border-red-900/50 bg-red-950/40 rounded-xl p-3">
          {{ error }}
        </div>

        <button class="btn w-full" :disabled="loading">
          {{ loading ? 'Létrehozás...' : 'Regisztráció' }}
        </button>

        <div class="text-sm text-slate-400 mt-2">
          Van már fiókod? <RouterLink class="hover:text-slate-200 transition" to="/login">Belépés</RouterLink>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useRouter } from 'vue-router'

const auth = useAuthStore()
const router = useRouter()

const email = ref('')
const password = ref('')
const error = ref('')
const loading = ref(false)

async function onSubmit() {
  error.value = ''
  loading.value = true
  try {
    await auth.register(email.value.trim(), password.value)
    router.push(auth.isAdmin ? '/admin/tasks' : '/student/tasks')
  } catch (e: any) {
    error.value = e?.response?.data?.message || 'Sikertelen regisztráció.'
  } finally {
    loading.value = false
  }
}
</script>
