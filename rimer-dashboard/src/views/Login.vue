<script setup>
import { ref, reactive, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { store } from '../store'
import { getPublicTicker } from '../services/ticketApi'

const router = useRouter()

const mode = ref('login') // 'login' | 'register'

const form = reactive({ username: '', password: '', displayName: '', confirmPassword: '' })
const error = ref('')
const loading = ref(false)

const submit = async () => {
  error.value = ''
  loading.value = true

  await new Promise(r => setTimeout(r, 300)) // subtle UX delay

  try {
    if (mode.value === 'login') {
      const res = store.login(form.username, form.password)
      if (!res.ok) { error.value = res.error; return }
    } else {
      if (!form.username.trim() || !form.password.trim()) {
        error.value = 'Username and password are required'; return
      }
      if (form.password !== form.confirmPassword) {
        error.value = 'Passwords do not match'; return
      }
      const res = store.register(form.username, form.password, form.displayName)
      if (!res.ok) { error.value = res.error; return }
    }
    router.push(store.role === 'admin' ? '/admin' : '/user')
  } finally {
    loading.value = false
  }
}

const switchMode = () => {
  mode.value = mode.value === 'login' ? 'register' : 'login'
  error.value = ''
  Object.assign(form, { username: '', password: '', displayName: '', confirmPassword: '' })
}

// Ticker Logic
const tickers = ref([])
let fetchInterval = null

const loadTickers = async () => {
  try {
    const res = await getPublicTicker()
    if (res.data?.success) {
      tickers.value = res.data.data
    }
  } catch (e) {
    console.error('Failed to load public ticker', e)
  }
}

onMounted(() => {
  loadTickers()
  fetchInterval = setInterval(loadTickers, 30000)
})

onUnmounted(() => {
  if (fetchInterval) clearInterval(fetchInterval)
})
</script>

<template>
  <div class="min-h-screen bg-[#0a0f18] flex items-center justify-center p-4">

    <!-- Subtle background grid -->
    <div class="fixed inset-0 opacity-[0.03]"
      style="background-image: linear-gradient(rgba(255,255,255,.5) 1px, transparent 1px), linear-gradient(90deg, rgba(255,255,255,.5) 1px, transparent 1px); background-size: 40px 40px;"
    />

    <!-- Glow blobs -->
    <div class="fixed top-1/4 left-1/4 w-96 h-96 bg-blue-600/10 rounded-full blur-3xl pointer-events-none" />
    <div class="fixed bottom-1/4 right-1/4 w-96 h-96 bg-indigo-600/10 rounded-full blur-3xl pointer-events-none" />

    <div class="relative w-full max-w-md">
      <!-- Card -->
      <div class="bg-[#0d131f] border border-slate-800 rounded-2xl p-8 shadow-2xl">

        <!-- Logo -->
        <div class="text-center mb-8">
          <h1 class="text-2xl font-bold bg-gradient-to-r from-blue-400 to-indigo-400 bg-clip-text text-transparent">
            Rimer Platform
          </h1>
          <p class="text-xs text-slate-500 mt-1">Real-time monitoring &amp; request management</p>
        </div>

        <!-- Tabs -->
        <div class="flex rounded-xl bg-slate-900/60 p-1 mb-6">
          <button
            @click="mode = 'login'; error = ''"
            class="flex-1 py-2 text-xs font-bold rounded-lg transition-all duration-200"
            :class="mode === 'login' ? 'bg-blue-600 text-white shadow' : 'text-slate-400 hover:text-slate-300'"
          >Sign In</button>
          <button
            @click="mode = 'register'; error = ''"
            class="flex-1 py-2 text-xs font-bold rounded-lg transition-all duration-200"
            :class="mode === 'register' ? 'bg-blue-600 text-white shadow' : 'text-slate-400 hover:text-slate-300'"
          >Register</button>
        </div>

        <form @submit.prevent="submit" class="space-y-4">
          <!-- Display name (register only) -->
          <div v-show="mode === 'register'">
            <label class="block text-xs font-bold text-slate-400 uppercase tracking-widest mb-1.5">Display Name</label>
            <input
              v-model="form.displayName"
              type="text"
              placeholder="Your name"
              class="w-full bg-slate-900/60 border border-slate-700 rounded-xl px-4 py-3 text-sm text-slate-100 placeholder-slate-600 focus:outline-none focus:border-blue-500 transition-colors"
            />
          </div>

          <!-- Username -->
          <div>
            <label class="block text-xs font-bold text-slate-400 uppercase tracking-widest mb-1.5">Username</label>
            <input
              v-model="form.username"
              type="text"
              placeholder="username"
              autocomplete="username"
              required
              class="w-full bg-slate-900/60 border border-slate-700 rounded-xl px-4 py-3 text-sm text-slate-100 placeholder-slate-600 focus:outline-none focus:border-blue-500 transition-colors"
            />
          </div>

          <!-- Password -->
          <div>
            <label class="block text-xs font-bold text-slate-400 uppercase tracking-widest mb-1.5">Password</label>
            <input
              v-model="form.password"
              type="password"
              placeholder="••••••••"
              autocomplete="current-password"
              required
              class="w-full bg-slate-900/60 border border-slate-700 rounded-xl px-4 py-3 text-sm text-slate-100 placeholder-slate-600 focus:outline-none focus:border-blue-500 transition-colors"
            />
          </div>

          <!-- Confirm password (register only) -->
          <div v-show="mode === 'register'">
            <label class="block text-xs font-bold text-slate-400 uppercase tracking-widest mb-1.5">Confirm Password</label>
            <input
              v-model="form.confirmPassword"
              type="password"
              placeholder="••••••••"
              class="w-full bg-slate-900/60 border border-slate-700 rounded-xl px-4 py-3 text-sm text-slate-100 placeholder-slate-600 focus:outline-none focus:border-blue-500 transition-colors"
            />
          </div>

          <!-- Error -->
          <transition name="fade">
            <p v-if="error" class="text-xs text-rose-400 bg-rose-500/10 border border-rose-500/20 rounded-xl px-4 py-2.5">
              {{ error }}
            </p>
          </transition>

          <!-- Submit -->
          <button
            type="submit"
            :disabled="loading"
            class="w-full py-3 rounded-xl text-sm font-bold transition-all duration-200 bg-blue-600 hover:bg-blue-500 text-white shadow-lg shadow-blue-500/20 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <span v-if="loading" class="flex items-center justify-center gap-2">
              <span class="w-4 h-4 border-2 border-white/40 border-t-white rounded-full animate-spin" />
              {{ mode === 'login' ? 'Signing in…' : 'Creating account…' }}
            </span>
            <span v-else>{{ mode === 'login' ? 'Sign In' : 'Create Account' }}</span>
          </button>
        </form>

        <!-- Admin hint -->
        <p class="text-center text-[11px] text-slate-600 mt-5">
          Admin access: <code class="text-slate-500">admin / admin</code>
        </p>
      </div>
    </div>

    <!-- Public Ticker Widget -->
    <div v-if="tickers.length > 0" class="fixed bottom-6 left-6 z-50 w-72 md:w-80 pointer-events-none">
      <div class="bg-slate-900/80 backdrop-blur-md border border-slate-700/50 rounded-2xl p-4 shadow-2xl relative overflow-hidden">
        <!-- Decoration -->
        <div class="absolute left-0 top-0 bottom-0 w-1 bg-blue-500 rounded-l-2xl"></div>
        
        <div class="flex items-center gap-2 mb-3">
          <span class="w-1.5 h-1.5 rounded-full bg-blue-500 animate-pulse shadow-[0_0_8px_rgba(59,130,246,0.8)]"></span>
          <span class="text-[9px] font-black text-slate-400 uppercase tracking-widest">Canlı Sistem Akışı (Son Gelişmeler)</span>
        </div>
        
        <transition-group name="list" tag="div" class="space-y-3">
          <div v-for="(item, idx) in tickers" :key="item.timestamp + idx" class="border-l-2 border-slate-700/50 pl-3">
            <p class="text-xs font-medium text-slate-200 leading-relaxed">
              {{ item.message }}
            </p>
            <p class="text-[9px] font-medium text-slate-500 mt-1">
              {{ new Date(item.timestamp).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' }) }}
            </p>
          </div>
        </transition-group>
      </div>
    </div>
  </div>
</template>

<style scoped>
.fade-enter-active, .fade-leave-active { transition: opacity 0.2s ease; }
.fade-enter-from, .fade-leave-to { opacity: 0; }

.list-enter-active, .list-leave-active { transition: all 0.5s ease; }
.list-enter-from { opacity: 0; transform: translateY(-15px); }
.list-leave-to { opacity: 0; transform: translateY(15px); }
</style>
