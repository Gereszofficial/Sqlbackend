import axios from 'axios'
import { useAuthStore } from '@/stores/auth'
import router from '@/router'

const API_BASE = import.meta.env.VITE_API_BASE

function readCookie(name: string): string {
  const prefix = `${name}=`
  const parts = document.cookie.split(';').map((v) => v.trim())
  const match = parts.find((part) => part.startsWith(prefix))
  return match ? decodeURIComponent(match.slice(prefix.length)) : ''
}

export function setCsrfToken(token: string) {
  if (token) {
    sessionStorage.setItem('sqltrainer_csrf', token)
    localStorage.setItem('sqltrainer_csrf', token)
  }
}

export function clearCsrfToken() {
  sessionStorage.removeItem('sqltrainer_csrf')
  localStorage.removeItem('sqltrainer_csrf')
}

export function setAccessToken(token: string) {
  if (token) {
    sessionStorage.setItem('sqltrainer_access_token', token)
    localStorage.setItem('sqltrainer_access_token', token)
  }
}

export function clearAccessToken() {
  sessionStorage.removeItem('sqltrainer_access_token')
  localStorage.removeItem('sqltrainer_access_token')
}

function getAccessToken(): string {
  return (
    sessionStorage.getItem('sqltrainer_access_token') ||
    localStorage.getItem('sqltrainer_access_token') ||
    ''
  )
}

function getCsrfToken(): string {
  return (
    sessionStorage.getItem('sqltrainer_csrf') ||
    localStorage.getItem('sqltrainer_csrf') ||
    readCookie('sqltrainer_csrf')
  )
}

export const http = axios.create({
  baseURL: API_BASE,
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json'
  }
})

http.interceptors.request.use((config) => {
  const token = getAccessToken()
  if (token) {
    config.headers = config.headers ?? {}
    config.headers.Authorization = `Bearer ${token}`
  }

  const method = (config.method || 'get').toUpperCase()
  const needsCsrf = ['POST', 'PUT', 'PATCH', 'DELETE'].includes(method)

  if (needsCsrf) {
    const csrf = getCsrfToken()
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
      const isAuthBootstrapCheck = requestUrl.includes('/auth/me')

      const auth = useAuthStore()
      auth.applyUser(null)
      clearCsrfToken()
      clearAccessToken()

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