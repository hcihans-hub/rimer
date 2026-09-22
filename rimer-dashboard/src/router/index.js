import { createRouter, createWebHashHistory } from 'vue-router'
import { isAuthenticated, getUser } from '../auth'

import LoginPage      from '../views/LoginPage.vue'
import UserDashboard  from '../views/UserDashboard.vue'
import AdminDashboard from '../views/AdminDashboard.vue'
import DeptDashboard  from '../views/DeptDashboard.vue'
import TicketDetail   from '../views/TicketDetail.vue'
import RegisterPage   from '../views/RegisterPage.vue'
import ChartsDashboard from '../views/ChartsDashboard.vue'
import AdminChartPermissions from '../views/AdminChartPermissions.vue'
import ApplyTicket        from '../views/ApplyTicket.vue'
import TrackTicket        from '../views/TrackTicket.vue'
import ForgotPasswordPage from '../views/ForgotPasswordPage.vue'
import ResetPasswordPage from '../views/ResetPasswordPage.vue'

const routes = [
  { path: '/',             redirect: '/login' },
  { path: '/login',        component: LoginPage,      meta: { public: true } },
  { path: '/register',     component: RegisterPage,   meta: { public: true } },
  { path: '/forgot-password', component: ForgotPasswordPage, meta: { public: true } },
  { path: '/reset-password', component: ResetPasswordPage, meta: { public: true } },
  { path: '/apply',        component: ApplyTicket,    meta: { public: true } },
  { path: '/track',        component: TrackTicket,    meta: { public: true } },
  { path: '/dashboard',    component: UserDashboard,  meta: { auth: true } },
  { path: '/dept',         component: DeptDashboard,  meta: { auth: true, deptOnly: true } },
  { path: '/tickets/:id',  component: TicketDetail,   meta: { auth: true } },
  { path: '/admin',        component: AdminDashboard, meta: { auth: true, adminOnly: true } },
  { path: '/charts',       component: ChartsDashboard, meta: { auth: true } },
  { path: '/admin/chart-permissions', component: AdminChartPermissions, meta: { auth: true, adminOnly: true } },
]

const router = createRouter({ history: createWebHashHistory(), routes })

router.beforeEach((to) => {
  if (to.meta.public) {
    if (isAuthenticated()) {
      const { role } = getUser()
      const r = (role ?? '').toLowerCase()
      if (r === 'chartsrole' || r === 'rector') return '/charts'
      if (r === 'unituser') return '/dept'
      if (r === 'operator') return '/admin'
      return r === 'admin' || r === 'staff' ? '/admin' : '/dashboard'
    }
    return true
  }

  if (to.meta.auth) {
    if (!isAuthenticated()) return '/login'
    
    const { role } = getUser()
    const r = (role ?? '').toLowerCase()

    // chartsrole and rector can only access /charts
    if ((r === 'chartsrole' || r === 'rector') && to.path !== '/charts') {
      return '/charts'
    }

    // UnitUser can access /dept and /tickets/:id but not /admin or /dashboard
    if (to.path === '/dashboard' && r === 'unituser') {
      return '/dept'
    }

    // Operator goes to /admin, not /dashboard
    if (to.path === '/dashboard' && r === 'operator') {
      return '/admin'
    }

    // adminOnly: Admin, Staff and Operator can access
    if (to.meta.adminOnly) {
      if (r !== 'admin' && r !== 'staff' && r !== 'operator') {
        return r === 'unituser' ? '/dept' : '/dashboard'
      }
    }

    // UnitUser cannot access ticket detail page (actions are inline in dept panel)
    if (to.path.startsWith('/tickets/') && r === 'unituser') {
      return '/dept'
    }

    // Dept route only for unituser, admin, staff
    if (to.meta.deptOnly) {
      if (r !== 'unituser' && r !== 'admin' && r !== 'staff') return '/dashboard'
    }
  }

  return true
})

export default router

