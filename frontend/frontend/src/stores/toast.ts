import { defineStore } from 'pinia'

export type Toast = {
  id: string
  title: string
  message?: string
  kind?: 'success' | 'info' | 'warning'
  createdAt: number
}

function uid() {
  return Math.random().toString(16).slice(2) + Date.now().toString(16)
}

export const useToastStore = defineStore('toast', {
  state: () => ({
    toasts: [] as Toast[]
  }),
  actions: {
    push(t: Omit<Toast, 'id' | 'createdAt'> & { ttlMs?: number }) {
      const id = uid()
      const createdAt = Date.now()
      const toast: Toast = { id, createdAt, title: t.title, message: t.message, kind: t.kind }
      this.toasts.unshift(toast)

      const ttl = typeof t.ttlMs === 'number' ? t.ttlMs : 4200
      window.setTimeout(() => this.remove(id), ttl)
    },
    remove(id: string) {
      this.toasts = this.toasts.filter((x) => x.id !== id)
    },
    clear() {
      this.toasts = []
    }
  }
})
