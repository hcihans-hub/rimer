import { reactive } from 'vue'
import axios from 'axios'

// ── JWT helpers ───────────────────────────────────────────────────────────────

/**
 * Decode JWT payload without any library.
 * Returns null if token is missing or malformed.
 */
function decodeJwt(token) {
  if (!token) return null
  try {
    const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')
    const json = decodeURIComponent(
      atob(base64)
        .split('')
        .map(c => '%' + c.charCodeAt(0).toString(16).padStart(2, '0'))
        .join('')
    )
    return JSON.parse(json)
  } catch {
    return null
  }
}

/**
 * ASP.NET Core Identity uses long-form ClaimTypes URIs inside the JWT.
 * Extract role and name regardless of which key format is present.
 */
function extractFromPayload(payload) {
  if (!payload) return { role: null, name: null, email: null }
  
  // ASP.NET Core Identity standard role claim URI
  const roleClaim = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
  const nameClaim = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'
  const idClaim = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'
  
  return {
    id:    payload['id']       ?? payload[idClaim]   ?? payload['sub'] ?? null,
    role:  payload['role']     ?? payload[roleClaim] ?? null,
    name:  payload['fullName'] ?? payload[nameClaim] ?? payload['name'] ?? null,
    email: payload['email']    ?? payload['sub']      ?? null,
    departmentId: payload['departmentId'] ?? null
  }
}

// ── Token storage ─────────────────────────────────────────────────────────────
const TOKEN_KEY = 'rimer_token'

export function getToken()        { return localStorage.getItem(TOKEN_KEY) }
export function setToken(token)   { localStorage.setItem(TOKEN_KEY, token) }
export function clearToken()      { localStorage.removeItem(TOKEN_KEY) }
export function isAuthenticated() { return !!getToken() }

export function getUser() {
  const payload = decodeJwt(getToken())
  return extractFromPayload(payload)
}

// ── Reactive auth store ───────────────────────────────────────────────────────
export const auth = reactive({
  _token: getToken(),

  get token()       { return this._token },
  get isLoggedIn()  { return !!this._token },

  get user()        { return getUser() },
  get role()        { return this.user.role ?? '' },
  get displayName() { return this.user.name ?? this.user.email ?? '' },

  // Admin if role is Admin or Staff (backend uses "Admin" string)
  get isAdmin()     {
    const r = this.role.toLowerCase()
    return r === 'admin' || r === 'staff'
  },

  get isRector()    {
    return this.role.toLowerCase() === 'rector'
  },

  get isUnitUser()  {
    return this.role.toLowerCase() === 'unituser'
  },

  get isStudent()   {
    return this.role.toLowerCase() === 'student'
  },

  get isOperator()  {
    return this.role.toLowerCase() === 'operator'
  },

  /** Call with the raw token string returned by backend. */
  setSession(token) {
    setToken(token)
    this._token = token
  },

  async login(email, password) {
    try {
      const res = await axios.post('http://localhost:5000/api/Auth/login', { email, password })
      if (res.data?.data?.token) {
        this.setSession(res.data.data.token)
        return true
      }
      return false
    } catch (e) {
      console.error('Login error:', e)
      throw e
    }
  },

  async register(firstName, lastName, email, password, confirmPassword) {
    try {
      const res = await axios.post('http://localhost:5000/api/Auth/register', { 
        firstName,
        lastName,
        email, 
        password,
        confirmPassword: confirmPassword || password 
      })
      if (res.data?.data?.token) {
        this.setSession(res.data.data.token)
        return true
      }
      return false
    } catch (e) {
      console.error('Register error:', e)
      throw e
    }
  },

  logout() {
    clearToken()
    this._token = null
  },
})


// ── Toast store (max 3, auto-dismiss 4s) ─────────────────────────────────────
export const toastStore = reactive({
  toasts: [],
  add(msg, type = 'info') {
    const id = Date.now()
    this.toasts.unshift({ id, msg, type })
    if (this.toasts.length > 3) this.toasts.length = 3
    setTimeout(() => this.remove(id), 4000)
  },
  remove(id) { this.toasts = this.toasts.filter(t => t.id !== id) },
  success(msg) { this.add(msg, 'success') },
  error(msg)   { this.add(msg, 'error') },
})
