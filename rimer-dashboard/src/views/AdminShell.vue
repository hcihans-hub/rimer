<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { store } from '../store'
import socket from '../services/socket'
import ToastContainer from '../components/ToastContainer.vue'
import Overview from './Overview.vue'
import SystemHealth from './SystemHealth.vue'
import QueueMonitor from './QueueMonitor.vue'
import EndpointAnalytics from './EndpointAnalytics.vue'
import AdminAnalytics from './AdminAnalytics.vue'

const router = useRouter()
const activePage = ref('Overview')
const isDark = ref(true)

// Admin nav items
const navItems = [
  { name: 'Overview',           icon: 'M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zM14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zM14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z' },
  { name: 'System Health',      icon: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 5.04M12 20.944V12' },
  { name: 'Queue Monitor',      icon: 'M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10' },
  { name: 'Analytics',          icon: 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z' },
  { name: 'All Requests',       icon: 'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2' },
]

const alertLevel = computed(() => {
  if (store.alerts.find(a => a.level === 'CRITICAL'))   return 'CRITICAL'
  if (store.alerts.find(a => a.level === 'HIGH LOAD'))  return 'HIGH LOAD'
  if (store.alerts.find(a => a.level === 'DROP SPIKE')) return 'DROP SPIKE'
  return null
})

const toggleDark = () => {
  isDark.value = !isDark.value
  document.documentElement.classList.toggle('dark', isDark.value)
}

const logout = () => { store.logout(); router.push('/login') }

onMounted(() => { document.documentElement.classList.add('dark') })
onUnmounted(() => {})
</script>

<template>
  <div class="flex h-screen bg-slate-50 dark:bg-[#0a0f18]">

    <!-- Sidebar -->
    <aside class="w-60 flex-shrink-0 border-r border-slate-800 bg-[#0d131f] flex flex-col z-20">
      <div class="p-5 border-b border-slate-800/50">
        <h1 class="text-lg font-bold bg-gradient-to-r from-blue-400 to-indigo-400 bg-clip-text text-transparent">Rimer</h1>
        <span class="text-[10px] text-rose-400 font-bold uppercase tracking-widest">Admin Console</span>
      </div>

      <nav class="flex-1 p-3 space-y-0.5 overflow-y-auto">
        <button
          v-for="item in navItems" :key="item.name"
          @click="activePage = item.name"
          class="w-full flex items-center gap-3 px-3 py-2.5 rounded-xl text-xs font-semibold transition-all duration-200"
          :class="activePage === item.name
            ? 'bg-blue-600/15 text-blue-400 border border-blue-500/20'
            : 'text-slate-400 hover:bg-slate-800/60 hover:text-slate-300 border border-transparent'"
        >
          <svg class="w-4 h-4 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="item.icon" />
          </svg>
          {{ item.name }}
        </button>
      </nav>

      <!-- User info -->
      <div class="p-4 border-t border-slate-800/50">
        <div class="flex items-center gap-3 mb-3">
          <div class="w-8 h-8 rounded-full bg-rose-500/20 flex items-center justify-center text-xs font-bold text-rose-400 flex-shrink-0">
            A
          </div>
          <div class="flex-1 min-w-0">
            <p class="text-xs font-semibold text-white truncate">{{ store.displayName }}</p>
            <p class="text-[10px] text-rose-400 font-bold uppercase tracking-wider">Admin</p>
          </div>
        </div>
        <button @click="logout" class="w-full py-2 text-xs font-semibold text-slate-500 hover:text-rose-400 transition-colors rounded-lg hover:bg-rose-500/5 border border-transparent hover:border-rose-500/20">
          Sign out
        </button>
      </div>
    </aside>

    <!-- Main -->
    <div class="flex-1 flex flex-col min-w-0 overflow-hidden">
      <!-- Topbar -->
      <header class="h-14 flex items-center justify-between px-6 bg-[#0d131f]/80 backdrop-blur-md border-b border-slate-800 z-10 flex-shrink-0">
        <h2 class="text-sm font-bold text-slate-200">{{ activePage }}</h2>
        <div class="flex items-center gap-3">
          <!-- Alert indicator -->
          <div
            v-show="alertLevel"
            class="flex items-center gap-2 px-3 py-1.5 rounded-lg border text-xs font-bold uppercase tracking-wider"
            :class="{
              'bg-rose-500/10 border-rose-500/20 text-rose-400':   alertLevel === 'CRITICAL',
              'bg-amber-500/10 border-amber-500/20 text-amber-400': alertLevel === 'HIGH LOAD',
              'bg-orange-500/10 border-orange-500/20 text-orange-400': alertLevel === 'DROP SPIKE',
            }"
          >
            <span class="relative flex h-1.5 w-1.5">
              <span class="animate-ping absolute inline-flex h-full w-full rounded-full opacity-75 bg-current" />
              <span class="relative inline-flex rounded-full h-1.5 w-1.5 bg-current" />
            </span>
            {{ alertLevel }}
          </div>
          <!-- Dark toggle -->
          <button @click="toggleDark" class="p-1.5 rounded-lg bg-slate-800 text-slate-400 hover:text-slate-200 transition-colors">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z" />
            </svg>
          </button>
        </div>
      </header>

      <!-- Page -->
      <main class="flex-1 overflow-y-auto p-6 bg-[#0a0f18]/50">
        <Overview          v-if="activePage === 'Overview'" />
        <SystemHealth      v-else-if="activePage === 'System Health'" />
        <QueueMonitor      v-else-if="activePage === 'Queue Monitor'" />
        <AdminAnalytics    v-else-if="activePage === 'Analytics'" />
        <AllRequestsView   v-else-if="activePage === 'All Requests'" />
      </main>
    </div>

    <ToastContainer />
  </div>
</template>

<script>
// Inline lightweight AllRequestsView to keep file count low
import { defineComponent, computed, h } from 'vue'
import { store } from '../store'

const AllRequestsView = defineComponent({
  name: 'AllRequestsView',
  setup() {
    const requests = computed(() => store.requests)
    const typeColor = (t) => ({ request: 'blue', complaint: 'rose', suggestion: 'amber' }[t] ?? 'slate')
    const statusColor = (s) => s === 'pending' ? 'text-amber-400' : 'text-emerald-400'
    return { requests, typeColor, statusColor }
  },
  template: `
    <div class="space-y-4">
      <div class="flex items-center justify-between">
        <h3 class="text-sm font-bold text-white">All User Submissions</h3>
        <span class="text-xs text-slate-500">{{ requests.length }} total</span>
      </div>
      <div v-if="requests.length === 0" class="flex flex-col items-center justify-center py-20 text-center">
        <span class="text-4xl mb-3">📭</span>
        <p class="text-sm text-slate-500">No submissions yet</p>
      </div>
      <div v-else class="space-y-3">
        <div v-for="r in requests" :key="r.id"
          class="bg-[#0d131f] border border-slate-800 rounded-xl p-4 hover:border-slate-700 transition-colors">
          <div class="flex items-start justify-between gap-4">
            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-2 mb-1.5">
                <span class="text-[10px] font-bold uppercase tracking-widest px-2 py-0.5 rounded-full"
                  :class="'bg-' + typeColor(r.type) + '-500/15 text-' + typeColor(r.type) + '-400'">{{ r.type }}</span>
                <span class="text-[10px] text-slate-500">from <span class="text-slate-400 font-semibold">{{ r.displayName || r.username }}</span></span>
              </div>
              <p class="text-sm text-slate-300 leading-snug">{{ r.message }}</p>
            </div>
            <div class="flex-shrink-0 text-right">
              <p :class="['text-[10px] font-bold uppercase', statusColor(r.status)]">{{ r.status }}</p>
              <p class="text-[10px] text-slate-600 mt-0.5">{{ new Date(r.createdAt).toLocaleString('tr-TR') }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})

export default { components: { AllRequestsView } }
</script>
