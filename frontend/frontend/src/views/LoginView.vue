<template>
  <div class="login-shell">
    <div class="login-grid">
      <section class="hero-card flex flex-col justify-between min-h-[620px]">
        <div class="relative z-10">
          <div class="badge mb-4">SqlTrainer · clean session</div>
          <h1 class="text-4xl font-semibold tracking-tight max-w-xl">Tanulj SQL-t egy letisztultabb, gyorsabb és látványosabb felületen.</h1>
          <p class="mt-4 text-sm text-slate-300 max-w-lg">
            Belépés után azonnal folytathatod a témákat, feladatokat és a haladásodat. A háttérben futó SQL animáció csak vizuális elem, könnyű és folyamatos.
          </p>

          <div class="mt-8 grid sm:grid-cols-3 gap-3 max-w-2xl">
            <div class="metric-tile">
              <div class="text-xs text-slate-400">Diák nézet</div>
              <div class="mt-1 text-sm font-semibold">Egyszerűbb feladatlista</div>
            </div>
            <div class="metric-tile">
              <div class="text-xs text-slate-400">Rank</div>
              <div class="mt-1 text-sm font-semibold">Valós idejű haladás</div>
            </div>
            <div class="metric-tile">
              <div class="text-xs text-slate-400">UI</div>
              <div class="mt-1 text-sm font-semibold">Tisztább, prémiumabb</div>
            </div>
          </div>
        </div>

        <div class="sql-stage rounded-[28px] mt-8">
          <div class="sql-header">
            <div class="sql-dots"><span></span><span></span><span></span></div>
            <div>live-sql-preview.sql</div>
          </div>
          <pre class="sql-code" v-html="animatedCode"></pre>
        </div>
      </section>

      <section class="card p-6 lg:p-8 flex flex-col justify-center min-h-[620px]">
        <div class="flex items-center gap-3 mb-8">
          <div class="h-12 w-12 rounded-2xl bg-slate-100 text-slate-950 grid place-items-center font-black text-lg">SQL</div>
          <div>
            <div class="text-xl font-semibold">Belépés</div>
            <div class="text-sm text-slate-400">Jelentkezz be és folytasd a megkezdett feladatokat</div>
          </div>
        </div>

        <form class="space-y-4" @submit.prevent="onSubmit">
          <div>
            <label class="text-xs text-slate-400">Email</label>
            <input class="input mt-2" v-model="email" type="email" autocomplete="email" required />
          </div>
          <div>
            <label class="text-xs text-slate-400">Jelszó</label>
            <input class="input mt-2" v-model="password" type="password" autocomplete="current-password" required />
          </div>

          <div v-if="error" class="text-sm text-red-300 border border-red-900/50 bg-red-950/40 rounded-xl p-3">
            {{ error }}
          </div>

          <button class="btn w-full" :disabled="loading">
            {{ loading ? 'Beléptetés...' : 'Belépés' }}
          </button>

          <div class="flex items-center justify-between gap-3 text-sm text-slate-400 pt-2">
            <RouterLink class="hover:text-slate-200 transition" to="/register">Regisztráció</RouterLink>
            <button type="button" class="hover:text-slate-200 transition" @click="showHelp = !showHelp">HTTPS tipp</button>
          </div>

          <div v-if="showHelp" class="rounded-2xl border border-white/10 bg-white/[0.02] p-3 text-xs text-slate-400 leading-relaxed">
            Ha az API self-signed HTTPS-t használ, először nyisd meg a <span class="badge">/swagger</span> oldalt és fogadd el a böngésző figyelmeztetését.
          </div>
        </form>
      </section>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useRoute, useRouter } from 'vue-router'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()

const email = ref('')
const password = ref('')
const error = ref('')
const loading = ref(false)
const showHelp = ref(false)

const snippets = [
`SELECT t.title, t.category,\n       CASE WHEN s.IsCorrect = 1 THEN 'kész' ELSE 'folyamatban' END AS status\nFROM Tasks t\nLEFT JOIN Submissions s ON s.TaskId = t.Id\nWHERE t.IsPublished = 1\nORDER BY t.Category, t.Title;`,
`SELECT c.name, o.product_name, o.price\nFROM customers c\nJOIN orders o ON o.customer_id = c.id\nWHERE o.price > 10000\nORDER BY o.price DESC;`,
`SELECT topic, COUNT(*) AS feladat_db\nFROM student_progress\nGROUP BY topic\nHAVING COUNT(*) > 0\nORDER BY feladat_db DESC;`
]

const animatedText = ref('')
let snippetIndex = 0
let charIndex = 0
let pauseTimer: number | null = null
let tickTimer: number | null = null

function escapeHtml(text: string) {
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
}

const animatedCode = computed(() => {
  const html = escapeHtml(animatedText.value)
    .replace(/\b(SELECT|FROM|LEFT JOIN|JOIN|WHERE|ORDER BY|GROUP BY|HAVING|CASE|WHEN|THEN|ELSE|END|AS|COUNT)\b/g, '<span class="kw">$1</span>')
    .replace(/'([^']*)'/g, "<span class=\"str\">'$1'</span>")
    .replace(/\b(Tasks|Submissions|customers|orders|student_progress|title|category|price|topic|status|product_name|name|customer_id|IsCorrect|TaskId|Id|IsPublished)\b/g, '<span class="id">$1</span>')
    .replace(/\b(1|10000|0)\b/g, '<span class="num">$1</span>')
  return `${html}<span class="sql-caret">|</span>`
})

function stopAnimation() {
  if (pauseTimer) window.clearTimeout(pauseTimer)
  if (tickTimer) window.clearTimeout(tickTimer)
}

function nextFrame() {
  const current = snippets[snippetIndex]
  if (charIndex < current.length) {
    animatedText.value = current.slice(0, charIndex + 1)
    charIndex += 1
    tickTimer = window.setTimeout(nextFrame, 34)
    return
  }

  pauseTimer = window.setTimeout(() => {
    snippetIndex = (snippetIndex + 1) % snippets.length
    charIndex = 0
    animatedText.value = ''
    tickTimer = window.setTimeout(nextFrame, 250)
  }, 1300)
}

onMounted(() => {
  nextFrame()
})

onBeforeUnmount(() => {
  stopAnimation()
})

async function onSubmit() {
  error.value = ''
  loading.value = true
  try {
    await auth.login(email.value.trim(), password.value)
    const redirect = (route.query.r as string) || (auth.isAdmin ? '/admin/tasks' : '/student/tasks')
    router.push(redirect)
  } catch (e: any) {
    error.value = e?.response?.data?.message || 'Sikertelen belépés. Ellenőrizd az adatokat.'
  } finally {
    loading.value = false
  }
}
</script>
