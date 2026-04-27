import axios from 'axios'
import { useAuthStore } from '@/stores/auth'
import router from '@/router'

const apiBase = import.meta.env.VITE_API_BASE || '/'

function readCookie(name: string): string {
  const prefix = `${name}=`
  const parts = document.cookie.split(';').map((v) => v.trim())
  const match = parts.find((part) => part.startsWith(prefix))
  return match ? decodeURIComponent(match.slice(prefix.length)) : ''
}

export const http = axios.create({
  baseURL: apiBase,
  withCredentials: true,
  headers: { 'Content-Type': 'application/json' }
})

http.interceptors.request.use((config) => {
  const method = (config.method || 'get').toUpperCase()
  const needsCsrf = ['POST', 'PUT', 'PATCH', 'DELETE'].includes(method)
  if (needsCsrf) {
    const csrf = readCookie('sqltrainer_csrf')
    if (csrf) {
      config.headers = config.headers ?? {}
      config.headers['X-CSRF-TOKEN'] = csrf
    }
  }
  return config
})

http.interceptors.response.use(
  (res) => res,
  async (err) => {
    const status = err?.response?.status
    if (status === 401) {
      const requestUrl = String(err?.config?.url || '')
      const isAuthBootstrapCheck = requestUrl.includes('/api/auth/me')

      const auth = useAuthStore()
      auth.applyUser(null)

      if (!isAuthBootstrapCheck && router.currentRoute.value.path !== '/login') {
        await router.push({ path: '/login', query: { r: router.currentRoute.value.fullPath } })
      }
    }
    return Promise.reject(err)
  }
)
