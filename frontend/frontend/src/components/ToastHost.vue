<template>
  <div class="fixed top-4 right-4 z-50 grid gap-2 w-[min(360px,calc(100vw-2rem))]">
    <div
      v-for="t in toast.toasts"
      :key="t.id"
      class="toast-card"
      :class="toastClass(t.kind)"
      role="status"
      aria-live="polite"
    >
      <div class="flex items-start justify-between gap-3">
        <div>
          <div class="text-sm font-semibold">{{ t.title }}</div>
          <div v-if="t.message" class="text-xs text-slate-300 mt-0.5">{{ t.message }}</div>
        </div>
        <button class="toast-x" @click="toast.remove(t.id)" aria-label="Bezárás">✕</button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useToastStore } from '@/stores/toast'
const toast = useToastStore()

function toastClass(kind?: string) {
  if (kind === 'success') return 'toast-success'
  if (kind === 'warning') return 'toast-warning'
  return 'toast-info'
}
</script>

<style scoped>
.toast-card {
  border: 1px solid rgba(148, 163, 184, 0.18);
  background: rgba(2, 6, 23, 0.78);
  backdrop-filter: blur(10px);
  border-radius: 16px;
  padding: 12px 12px;
  box-shadow: 0 18px 40px rgba(0, 0, 0, 0.45);
}

.toast-info {
  border-color: rgba(56, 189, 248, 0.25);
}

.toast-success {
  border-color: rgba(52, 211, 153, 0.28);
}

.toast-warning {
  border-color: rgba(251, 191, 36, 0.28);
}

.toast-x {
  font-size: 12px;
  color: rgba(148, 163, 184, 0.8);
  padding: 4px 8px;
  border-radius: 10px;
}

.toast-x:hover {
  background: rgba(15, 23, 42, 0.7);
  color: rgba(226, 232, 240, 0.92);
}
</style>
