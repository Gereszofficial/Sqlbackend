import axios from 'axios'
import { useAuthStore } from '@/stores/auth'
import router from '@/router'

// DEBUG - látni fogod a console-ban
const API_BASE = 'https://acceptable-benevolence-production-9080.up.railway.app'
console.log('API BASE FIX ACTIVE:', API_BASE)

// cookie olvasó
function readCookie(name: string): string {
  const prefix = `${name}=`
  const parts = document.cookie.split(';').map((v) => v.trim())
  const match = parts.find((part) => part.startsWith(prefix))
  return match ? decodeURIComponent(match.slice(prefix.length)) : ''
}

// axios instance
export const http = axios.create({
  baseURL: API_BASE,
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json'
  }
})

// request interceptor (CSRF)
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

// response interceptor (auth)
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
        await router.push({
          path: '/login',
          query: { r: router.currentRoute.value.fullPath }
        })
      }
    }

    return Promise.reject(err)
  }
)