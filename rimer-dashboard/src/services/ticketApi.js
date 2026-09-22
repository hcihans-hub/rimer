import axios from 'axios'
import { getToken } from '../auth'

const http = axios.create({
  baseURL: 'http://localhost:5000/api',
  timeout: 30000,
})

http.interceptors.request.use(cfg => {
  const token = getToken()
  if (token) cfg.headers.Authorization = `Bearer ${token}`
  return cfg
})

http.interceptors.response.use(
  res => res,
  error => {
    if (error.response && error.response.status === 401) {
      localStorage.removeItem('rimer_token')
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)

// ── Auth ──────────────────────────────────────────────────────────────────────
export const login = (email, password) =>
  http.post('/auth/login', { email, password })

export const register = (firstName, lastName, email, password, confirmPassword) =>
  http.post('/auth/register', { firstName, lastName, email, password, confirmPassword })

export const forgotPassword = (email) =>
  http.post('/auth/forgot-password', { email })

export const resetPassword = (email, token, newPassword) =>
  http.post('/auth/reset-password', { email, token, newPassword })

export const logout = () => http.post('/auth/logout')

// ── Tickets (User) ────────────────────────────────────────────────────────────
export const createTicket = (formData) =>
  http.post('/tickets', formData, {
    headers: { 'Content-Type': 'multipart/form-data' }
  })

export const getMyTickets = (page = 1, pageSize = 20, searchTerm = '', status = '', assignedToId = '', departmentId = '', excludeClosed = false, startDate = '', endDate = '') =>
  http.get('/tickets', { params: { 
    page, pageSize, searchTerm, 
    status: status || undefined, 
    assignedToId: assignedToId || undefined,
    departmentId: departmentId || undefined,
    excludeClosed,
    startDate: startDate || undefined,
    endDate: endDate || undefined
  } })

export const getTicketById = (id) =>
  http.get(`/tickets/${id}`)

export const getMyStats = () =>
  http.get('/tickets/my-stats')

// ── Tickets (Admin) ───────────────────────────────────────────────────────────
export const getAllTickets = (page = 1, pageSize = 50, searchTerm = '', status = '', assignedToId = '', departmentId = '', category = '', excludeClosed = false, startDate = '', endDate = '') =>
  http.get('/tickets', { params: { 
    page, pageSize, searchTerm, 
    status: status || undefined, 
    assignedToId: assignedToId || undefined,
    departmentId: departmentId || undefined,
    category: category || undefined,
    excludeClosed,
    startDate: startDate || undefined,
    endDate: endDate || undefined
  } })

export const updateTicketStatus = (id, status, note = '') =>
  http.put(`/tickets/${id}/status`, { status, note })

export const replyTicket = (id, message) =>
  http.post(`/tickets/${id}/reply`, { message })

export const transferTicket = (id, toDepartmentId, note = '') =>
  http.post(`/tickets/${id}/transfer`, { toDepartmentId, note })

export const closeTicket = (id) =>
  http.post(`/tickets/${id}/close`)

export const takeOwnership = (id) =>
  http.post(`/tickets/${id}/take-ownership`)

export const updateInternalStatus = (id, internalStatus) =>
  http.put(`/tickets/${id}/internal-status`, internalStatus, { headers: { 'Content-Type': 'application/json' } })

export const updatePriority = (id, priority) =>
  http.put(`/tickets/${id}/priority`, priority, { headers: { 'Content-Type': 'application/json' } })

export const createReminder = (id, reminderAt, note = null) =>
  http.post(`/tickets/${id}/reminders`, { reminderAt, note })

export const getReminders = () =>
  http.get('/tickets/reminders')

export const dismissReminder = (reminderId) =>
  http.put(`/tickets/reminders/${reminderId}/dismiss`)

export const snoozeReminder = (reminderId, snoozeUntil) =>
  http.put(`/tickets/reminders/${reminderId}/snooze`, `"${snoozeUntil}"`, { headers: { 'Content-Type': 'application/json' } })

// ── Analytics (Permission-based) ──────────────────────────────────────────────
export const getCharts = (startDate = null, endDate = null) =>
  http.get('/charts', { params: { startDate, endDate } })

// ── Chart Permissions (Admin) ─────────────────────────────────────────────────
export const getAllChartPermissions = () =>
  http.get('/admin/chart-permissions')

export const getChartPermissions = (userId) =>
  http.get(`/admin/chart-permissions/${userId}`)

export const assignChartPermissions = (userId, chartKeys) =>
  http.post('/admin/chart-permissions', { userId, chartKeys })

// ── Users (Admin) ─────────────────────────────────────────────────────────────
export const getAllUsers = (page = 1, pageSize = 20, onlyActive = true, searchTerm = '', role = '', departmentId = '') =>
  http.get('/users', { params: { page, pageSize, onlyActive, search: searchTerm, role: role || undefined, departmentId: departmentId || undefined } })

export const getUserById = (id) =>
  http.get(`/users/${id}`)

export const createUser = (data) =>
  http.post('/users', data)

export const updateUserRole = (id, role) =>
  http.put(`/users/${id}/role`, { role })

export const lockUser = (id) =>
  http.put(`/users/${id}/lock`)

export const unlockUser = (id) =>
  http.put(`/users/${id}/unlock`)

export const deleteUser = (id) =>
  http.delete(`/users/${id}`)

export const activateUser = (id) =>
  http.put(`/users/${id}/activate`)

export const deactivateUser = (id) =>
  http.put(`/users/${id}/deactivate`)

export const sendPasswordResetLink = (id) =>
  http.post(`/users/${id}/send-reset-link`)

export const updateUser = (id, data) =>
  http.put(`/users/${id}`, data)

// ── Departments (Admin) ───────────────────────────────────────────────────────
export const getDepartments = (page = 1, pageSize = 20, onlyActive = true, searchTerm = '') =>
  http.get('/departments', { params: { page, pageSize, onlyActive, search: searchTerm } })

export const createDepartment = (name, description = '') =>
  http.post('/departments', { name, description })

export const activateDepartment = (id) =>
  http.put(`/departments/${id}/activate`)

export const deactivateDepartment = (id) =>
  http.put(`/departments/${id}/deactivate`)

export const updateDepartment = (id, data) =>
  http.put(`/departments/${id}`, data)

// ── Admin Logs ────────────────────────────────────────────────────────────────
export const getSystemLogs = (page = 1, pageSize = 20, filters = {}) =>
  http.get('/admin/logs', { params: { page, pageSize, ...filters } })

// ── Public (External) ─────────────────────────────────────────────────────────
export const getPublicDepartments = () =>
  http.get('/public/departments')

export const submitPublicTicket = (formData) =>
  http.post('/public/apply', formData)

export const trackPublicTicket = (trackingCode, email) =>
  http.post('/public/track', { trackingCode, email })

export const getPublicTicker = () =>
  http.get('/public/ticker')