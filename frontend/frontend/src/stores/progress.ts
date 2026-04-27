import { defineStore } from 'pinia'
import { http } from '@/api/http'
import type { StudentProgress } from '@/types/api'
import { useAuthStore } from '@/stores/auth'
import { useToastStore } from '@/stores/toast'

function lsKey(email: string) {
  return `sqltrainer:lastBadge:${email}`
}

export const useProgressStore = defineStore('progress', {
  state: () => ({
    progress: null as StudentProgress | null,
    loading: false as boolean
  }),
  actions: {
    async fetchProgress(opts?: { notifyRankUp?: boolean }) {
      const auth = useAuthStore()
      if (!auth.isStudent || !auth.email) return

      this.loading = true
      try {
        const res = await http.get<StudentProgress>('/api/student/progress')
        const next = res.data

        const notify = opts?.notifyRankUp !== false
        if (notify) {
          const toast = useToastStore()
          const key = lsKey(auth.email)
          const prevKey = localStorage.getItem(key)
          if (prevKey && prevKey !== next.badge.key) {
            toast.push({
              kind: 'success',
              title: 'Rank up!',
              message: `Gratulálok! Új rang: ${next.badge.label} (${next.percent}%).`,
              ttlMs: 5200
            })
          }
          localStorage.setItem(key, next.badge.key)
        }

        this.progress = next
      } finally {
        this.loading = false
      }
    },
    clear() {
      this.progress = null
    }
  }
})
