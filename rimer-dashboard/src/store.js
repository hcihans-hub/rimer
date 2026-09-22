import { reactive } from 'vue'

// ── Auth store (localStorage-backed) ─────────────────────────────────────────
const USERS_KEY = 'rimer_users'
const SESSION_KEY = 'rimer_session'

// Seed admin + any registered users
const loadUsers = () => {
  const saved = localStorage.getItem(USERS_KEY)
  const list = saved ? JSON.parse(saved) : []
  // Ensure admin always exists
  if (!list.find(u => u.username === 'admin')) {
    list.unshift({ username: 'admin', password: 'admin', role: 'admin', displayName: 'Admin User' })
  }
  return list
}

const saveUsers = (users) => {
  localStorage.setItem(USERS_KEY, JSON.stringify(users.filter(u => u.username !== 'admin')))
}

const loadSession = () => {
  const s = localStorage.getItem(SESSION_KEY)
  return s ? JSON.parse(s) : null
}

// ── Request store (in-memory + localStorage) ─────────────────────────────────
const REQUESTS_KEY = 'rimer_requests'
const loadRequests = () => {
  const s = localStorage.getItem(REQUESTS_KEY)
  return s ? JSON.parse(s) : []
}
const saveRequests = (reqs) => localStorage.setItem(REQUESTS_KEY, JSON.stringify(reqs))

// ── WebSocket metrics store ───────────────────────────────────────────────────
export const store = reactive({
  // ── Auth ──────────────────────────────────────────────────────────────────
  session: loadSession(),  // { username, role, displayName }

  get isLoggedIn() { return !!this.session },
  get role() { return this.session?.role ?? null },
  get isAdmin() { return this.session?.role === 'admin' },
  get displayName() { return this.session?.displayName ?? this.session?.username ?? '' },

  login(username, password) {
    const users = loadUsers()
    const user = users.find(u => u.username === username && u.password === password)
    if (!user) return { ok: false, error: 'Invalid username or password' }
    const session = { username: user.username, role: user.role, displayName: user.displayName || user.username }
    this.session = session
    localStorage.setItem(SESSION_KEY, JSON.stringify(session))
    return { ok: true }
  },

  register(username, password, displayName) {
    const users = loadUsers()
    if (users.find(u => u.username === username)) return { ok: false, error: 'Username already taken' }
    const newUser = { username, password, role: 'user', displayName: displayName || username }
    users.push(newUser)
    saveUsers(users)
    return this.login(username, password)
  },

  logout() {
    this.session = null
    localStorage.removeItem(SESSION_KEY)
  },

  // ── Requests ───────────────────────────────────────────────────────────────
  requests: loadRequests(),

  submitRequest(type, message) {
    const req = {
      id: `req_${Date.now()}`,
      username: this.session?.username,
      displayName: this.session?.displayName,
      type,
      message,
      status: 'pending',
      createdAt: new Date().toISOString(),
    }
    this.requests.unshift(req)
    saveRequests(this.requests)
    return req
  },

  userRequests() {
    return this.requests.filter(r => r.username === this.session?.username)
  },

  // ── Alerts ─────────────────────────────────────────────────────────────────
  alerts: [],
  lastDroppedRequests: 0,
  samplingRate: 50,
  protectionPending: false,

  addAlert(alert) {
    if (this.alerts.find(a => a.id === alert.id)) return
    this.alerts.unshift({ ...alert, timestamp: new Date() })
    if (this.alerts.length > 3) this.alerts.length = 3
    setTimeout(() => this.removeAlert(alert.id), 3000)
  },
  removeAlert(id) { this.alerts = this.alerts.filter(a => a.id !== id) },
  clearAlerts() { this.alerts = [] },

  // ── Live metrics (WebSocket) ───────────────────────────────────────────────
  metrics: {
    totalRequests: 0,
    droppedRequests: 0,
    queueDepth: 0,
    throughput: 0,
    systemMode: 'NORMAL',
    queueHistory:      { labels: [], values: [] },
    throughputHistory: { labels: [], values: [] },
  },

  get protectionMode() {
    const m = this.metrics.systemMode
    return m === 'PROTECTION' || m === 'PROTECTION MODE'
  },

  updateMetrics(data) {
    const now = new Date().toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit', second: '2-digit' })
    if (data.totalRequests   !== undefined) this.metrics.totalRequests   = data.totalRequests
    if (data.droppedRequests !== undefined) this.metrics.droppedRequests = data.droppedRequests
    if (data.queueDepth      !== undefined) this.metrics.queueDepth      = data.queueDepth
    if (data.throughput      !== undefined) this.metrics.throughput      = data.throughput
    if (data.systemMode      !== undefined) this.metrics.systemMode      = data.systemMode
    const push = (s, v) => { s.labels = [...s.labels, now].slice(-30); s.values = [...s.values, v].slice(-30) }
    if (data.throughput !== undefined) push(this.metrics.throughputHistory, data.throughput)
    if (data.queueDepth !== undefined) push(this.metrics.queueHistory,     data.queueDepth)
  },
})
