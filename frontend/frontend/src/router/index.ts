import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const routes = [
  { path: '/', redirect: '/student/tasks' },
  { path: '/login', component: () => import('@/views/LoginView.vue') },
  { path: '/register', component: () => import('@/views/RegisterView.vue') },

  {
    path: '/student',
    component: () => import('@/views/layouts/AppLayout.vue'),
    meta: { requiresAuth: true, role: 'Student' },
    children: [
      { path: 'topics', component: () => import('@/views/student/StudentTopicsView.vue') },
      { path: 'topics/:id', component: () => import('@/views/student/StudentTopicDetailView.vue') },
      { path: 'tasks', component: () => import('@/views/student/StudentTasksView.vue') },
      { path: 'tasks/:id', component: () => import('@/views/student/StudentTaskDetailView.vue') },
      { path: 'profile', component: () => import('@/views/student/StudentProfileView.vue') }
    ]
  },
  {
    path: '/admin',
    component: () => import('@/views/layouts/AppLayout.vue'),
    meta: { requiresAuth: true, role: 'Admin' },
    children: [
      { path: 'topics', component: () => import('@/views/admin/AdminTopicsView.vue') },
      { path: 'topics/new', component: () => import('@/views/admin/AdminTopicEditView.vue') },
      { path: 'topics/:id', component: () => import('@/views/admin/AdminTopicEditView.vue') },
      { path: 'tasks', component: () => import('@/views/admin/AdminTasksView.vue') },
      { path: 'tasks/new', component: () => import('@/views/admin/AdminTaskEditView.vue') },
      { path: 'tasks/:id', component: () => import('@/views/admin/AdminTaskEditView.vue') },
      { path: 'submissions', component: () => import('@/views/admin/AdminSubmissionsView.vue') },
      { path: 'rank', component: () => import('@/views/admin/AdminRankView.vue') }
    ]
  },

  { path: '/:pathMatch(.*)*', component: () => import('@/views/NotFoundView.vue') }
]

const router = createRouter({ history: createWebHistory(), routes })

router.beforeEach(async (to) => {
  const auth = useAuthStore()
  const requiresAuth = to.matched.some((r) => (r.meta as any)?.requiresAuth)

  if (requiresAuth || auth.sessionChecked === false) {
    await auth.ensureSession()
  }

  if (!requiresAuth) return true
  if (!auth.isAuthed) return { path: '/login', query: { r: to.fullPath } }

  const role = (to.matched.find((r) => (r.meta as any)?.role)?.meta as any)?.role as 'Admin' | 'Student' | undefined
  if (role === 'Admin' && !auth.isAdmin) return { path: '/student/tasks' }
  if (role === 'Student' && !auth.isStudent && auth.isAdmin) return { path: '/admin/tasks' }
  return true
})

export default router
