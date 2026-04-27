import { defineStore } from 'pinia'
import { http } from '@/api/http'
import type { AuthResponse, CurrentUser } from '@/types/api'

type Role = 'Student' | 'Admin' | 'Unknown'

function normalizeRole(raw?: string): Role {
  if (!raw) return 'Unknown'
  const r = raw.toLowerCase()
  if (r.includes('admin') || r.includes('teacher')) return 'Admin'
  if (r.includes('student')) return 'Student'
  return 'Unknown'
}

export const useAuthStore = defineStore('auth', {
  state: () => ({
    role: (localStorage.getItem('sqltrainer_role') as Role) || 'Unknown',
    email: localStorage.getItem('sqltrainer_email') || '',
    sessionChecked: false,
    sessionNonce: 0
  }),
  getters: {
    isAuthed: (s) => s.role !== 'Unknown' && !!s.email,
    isAdmin: (s) => s.role === 'Admin',
    isStudent: (s) => s.role === 'Student'
  },
  actions: {
    applyUser(user?: CurrentUser | null) {
      this.role = normalizeRole(user?.role)
      this.email = user?.email ?? ''
      this.sessionChecked = true
      this.sessionNonce++
      if (this.role === 'Unknown' || !this.email) {
        localStorage.removeItem('sqltrainer_role')
        localStorage.removeItem('sqltrainer_email')
        return
      }
      localStorage.setItem('sqltrainer_role', this.role)
      localStorage.setItem('sqltrainer_email', this.email)
    },
    async ensureSession(force = false) {
      if (this.sessionChecked && !force) return this.isAuthed
      try {
        const res = await http.get<CurrentUser>('/api/auth/me')
        this.applyUser(res.data)
        return true
      } catch {
        this.applyUser(null)
        return false
      }
    },
    async login(email: string, password: string) {
      const res = await http.post<AuthResponse>('/api/auth/login', { email, password })
      this.applyUser(res.data.user)
    },
    async register(email: string, password: string) {
      const res = await http.post<AuthResponse>('/api/auth/register', { email, password })
      this.applyUser(res.data.user)
    },
    async logout() {
      try {
        await http.post('/api/auth/logout')
      } catch {
        // ignore logout transport errors and clear local state anyway
      }
      this.applyUser(null)
    }
  }
})
