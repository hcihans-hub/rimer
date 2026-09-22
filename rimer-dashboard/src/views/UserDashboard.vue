<script setup>
import { ref, onMounted, computed, watch } from 'vue'
import { useRouter } from 'vue-router'
import { auth, toastStore } from '../auth'
import { i18n, theme } from '../lang'
import * as api from '../services/ticketApi'
import LangSelector from '../components/LangSelector.vue'
import NotificationBell from '../components/NotificationBell.vue'
import ReminderBadge from '../components/ReminderBadge.vue'
import Pagination from '../components/Pagination.vue'
import NewTicketForm from '../components/NewTicketForm.vue'

const router = useRouter()
const user = auth.user
const tickets = ref([])
const loading = ref(true)

const page = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)

const placeholderText = computed(() => {
  return i18n.lang === 'tr' 
    ? 'Talep numarası veya konu başlığına göre hızlıca arama yapın...' 
    : 'Quickly search by ticket number or subject...'
})

const CATEGORY_MAP = {
  request: { label_tr: "Destek Talebi", label_en: "Support Request", color: "bg-sky-100 text-sky-700", icon: "🛠️" },
  complaint: { label_tr: "Şikayet", label_en: "Complaint", color: "bg-red-100 text-red-700", icon: "⚠️" },
  suggestion: { label_tr: "Öneri", label_en: "Suggestion", color: "bg-green-100 text-green-700", icon: "💡" },
  info: { label_tr: "Bilgi Edinme", label_en: "Info Request", color: "bg-gray-100 text-gray-700", icon: "ℹ️" },
  thanks: { label_tr: "Teşekkür", label_en: "Thanks", color: "bg-yellow-100 text-yellow-700", icon: "⭐" },
  question: { label_tr: "Soru", label_en: "Question", color: "bg-purple-100 text-purple-700", icon: "❓" }
}

const STATUS_MAP = {
  Open: "bg-gray-100 text-gray-600",
  InProgress: "bg-yellow-100 text-yellow-700",
  Closed: "bg-green-100 text-green-700",
  Submitted: "bg-blue-100 text-blue-700",
  Reviewing: "bg-amber-100 text-amber-700",
  WaitingDepartment: "bg-purple-100 text-purple-700",
  Answered: "bg-emerald-100 text-emerald-700"
}

const normalizeCat = (cat) => {
  const c = String(cat).toLowerCase()
  if (c.includes('complaint') || c === '0') return 'complaint'
  if (c.includes('suggestion') || c === '1') return 'suggestion'
  if (c.includes('support') || c.includes('request') || c === '2') return 'request'
  if (c.includes('appreciation') || c.includes('thanks') || c === '3') return 'thanks'
  if (c.includes('question') || c === '4') return 'question'
  if (c.includes('info') || c === '5') return 'info'
  return 'info'
}

const tCategory = (cat) => {
  const key = normalizeCat(cat)
  return i18n.lang === 'tr' ? CATEGORY_MAP[key].label_tr : CATEGORY_MAP[key].label_en
}

const tStatus = (status) => {
  return i18n.t[`status${status}`] || status
}

const searchTerm = ref('')
const hideClosed = ref(true)
const startDate = ref('')
const endDate = ref('')

const showNewForm = ref(false)

const handleTicketCreated = () => {
  showNewForm.value = false
  page.value = 1
  loadTickets()
}

const loadTickets = async () => {
  loading.value = true
  try {
    const res = await api.getMyTickets(page.value, pageSize.value, searchTerm.value, '', '', '', hideClosed.value, startDate.value, endDate.value)
    tickets.value = res.data?.data?.items || []
    totalCount.value = res.data?.data?.totalCount || 0
  } catch (e) {
    toastStore.add('Failed to load tickets', 'error')
  } finally {
    loading.value = false
  }
}

let searchTimeout = null
const handleSearch = () => {
  clearTimeout(searchTimeout)
  searchTimeout = setTimeout(() => {
    page.value = 1
    loadTickets()
  }, 500)
}

watch([page, pageSize, hideClosed, startDate, endDate], loadTickets)

const logout = () => {
  auth.logout()
  router.push('/login')
}

onMounted(loadTickets)
</script>

<template>
  <div class="min-h-screen bg-slate-50 dark:bg-slate-950 flex flex-col">
    <header class="h-20 bg-white dark:bg-slate-900 border-b border-slate-200 dark:border-slate-800 px-6 flex items-center justify-between sticky top-0 z-40">
      <div class="flex items-center gap-4">
        <div class="relative h-16 w-40 flex-shrink-0 cursor-pointer" @click="router.push('/dashboard')">
          <img src="/logo.png" alt="Rimer Logo" class="absolute inset-0 w-full h-full object-contain transition-opacity duration-200" :class="theme.dark ? 'opacity-0' : 'opacity-100'" />
          <img src="/logonight.png" alt="Rimer Logo" class="absolute inset-0 w-full h-full object-contain transition-opacity duration-200" :class="theme.dark ? 'opacity-100' : 'opacity-0'" />
        </div>
        <div class="h-6 w-px bg-slate-200 dark:bg-slate-800" />
        <span class="text-sm font-medium text-slate-500 dark:text-slate-400">{{ i18n.t.myTickets }}</span>
        
        <button v-if="['admin', 'staff'].includes((user?.role || '').toLowerCase())" 
                @click="router.push('/admin')" 
                class="ml-4 px-4 py-2 bg-slate-800 hover:bg-slate-700 text-white rounded-lg text-xs font-bold transition-colors shadow-sm flex items-center gap-2">
          <span>⚙️</span> Admin Paneline Geç
        </button>
      </div>

      <div class="flex items-center gap-4">
        <LangSelector />
        <button @click="theme.toggle()" :title="theme.dark ? 'Açık Mod' : 'Koyu Mod'" class="p-2.5 rounded-xl text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800 transition-all">
          <span v-if="theme.dark">☀️</span><span v-else>🌙</span>
        </button>
        <div class="h-8 w-px bg-slate-200 dark:bg-slate-800" />
        <ReminderBadge />
        <NotificationBell />
        <div class="flex items-center gap-3 pl-2">
          <div class="text-right hidden sm:block">
            <p class="text-sm font-bold text-slate-900 dark:text-white">{{ user.fullName }}</p>
            <p class="text-[10px] text-slate-500 font-medium uppercase tracking-tighter">{{ user.role }}</p>
          </div>
          <button @click="logout" class="p-2.5 rounded-xl bg-rose-500/10 text-rose-500 hover:bg-rose-500 hover:text-white transition-all">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" /></svg>
          </button>
        </div>
      </div>
    </header>

    <main class="flex-1 p-6 max-w-7xl mx-auto w-full">
      <div class="flex items-center justify-between mb-8">
        <div>
          <h2 class="text-3xl font-black text-slate-900 dark:text-white">{{ i18n.t.myTickets }}</h2>
          <p class="text-slate-500 dark:text-slate-400 mt-1">{{ i18n.t.myTicketsSub }}</p>
          <div v-if="auth.isRector" class="mt-2 inline-flex items-center gap-1.5 px-3 py-1 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-full">
            <span class="w-1.5 h-1.5 rounded-full bg-amber-500 animate-pulse"></span>
            <span class="text-[10px] font-black text-amber-700 dark:text-amber-400 uppercase tracking-widest">Sadece Okuma Modu (Rektör)</span>
          </div>
        </div>
        <div class="flex gap-3">
          <button v-if="auth.isRector" @click="router.push('/charts')" class="px-6 py-3 bg-indigo-600 hover:bg-indigo-700 text-white rounded-2xl font-bold shadow-lg shadow-indigo-500/25 transition-all active:scale-95 flex items-center gap-2">
            <span>📈</span> Analytics
          </button>
          <button v-if="auth.isStudent && !showNewForm" @click="showNewForm = true" class="px-6 py-3 bg-blue-600 hover:bg-blue-700 text-white rounded-2xl font-bold shadow-lg shadow-blue-500/25 transition-all active:scale-95 flex items-center gap-2">
            <span class="text-xl">+</span> {{ i18n.t.newTicket }}
          </button>
        </div>
      </div>

      <NewTicketForm 
        v-if="showNewForm" 
        @cancel="showNewForm = false" 
        @created="handleTicketCreated" 
      />

      <!-- Search Bar & Filters -->
      <div class="mb-6 flex flex-col md:flex-row gap-4 items-center">
        <div class="relative flex-1 w-full">
          <input 
            v-model="searchTerm" 
            @input="handleSearch"
            type="text" 
            :placeholder="placeholderText"
            class="w-full pl-12 pr-6 py-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 text-slate-900 dark:text-white focus:ring-2 focus:ring-blue-500/50 outline-none transition-all shadow-sm"
          />
          <svg class="w-5 h-5 absolute left-4 top-1/2 -translate-y-1/2 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
        </div>
        <div class="flex items-center gap-2 bg-white dark:bg-slate-900 px-3 py-2.5 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm w-full md:w-auto overflow-x-auto">
          <input type="date" v-model="startDate" class="bg-transparent border-none outline-none text-sm text-slate-700 dark:text-slate-300 font-medium w-32" />
          <span class="text-slate-400">-</span>
          <input type="date" v-model="endDate" class="bg-transparent border-none outline-none text-sm text-slate-700 dark:text-slate-300 font-medium w-32" />
        </div>
        <label class="flex items-center gap-2 cursor-pointer whitespace-nowrap bg-white dark:bg-slate-900 px-4 py-4 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm transition-colors hover:bg-slate-50 dark:hover:bg-slate-800">
          <input type="checkbox" v-model="hideClosed" class="w-5 h-5 rounded border-slate-300 text-blue-600 focus:ring-blue-500" />
          <span class="text-sm font-bold text-slate-700 dark:text-slate-300">{{ i18n.lang === 'tr' ? 'Kapananları Gizle' : 'Hide Closed' }}</span>
        </label>
      </div>

      <div v-if="loading" class="space-y-4">
        <div v-for="i in 5" :key="i" class="h-20 bg-slate-200 dark:bg-slate-800 animate-pulse rounded-2xl w-full" />
      </div>

      <div v-else-if="tickets.length === 0" class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-3xl p-12 text-center">
        <div class="text-6xl mb-4">📭</div>
        <h3 class="text-xl font-bold text-slate-900 dark:text-white mb-2">{{ i18n.t.noTickets }}</h3>
        <p class="text-slate-500 dark:text-slate-400 mb-6">Need help? Create a new ticket above.</p>
      </div>

      <div v-else class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-3xl shadow-sm overflow-hidden flex flex-col">
        <div class="overflow-x-auto flex-1">
          <table class="w-full text-left border-collapse">
            <thead>
              <tr class="bg-slate-50 dark:bg-slate-800/50">
                <th class="px-6 py-4 text-xs font-bold text-slate-400 uppercase tracking-widest border-b border-slate-100 dark:border-slate-800">{{ i18n.t.ticketNo }}</th>
                <th class="px-6 py-4 text-xs font-bold text-slate-400 uppercase tracking-widest border-b border-slate-100 dark:border-slate-800">{{ i18n.t.category }}</th>
                <th class="px-6 py-4 text-xs font-bold text-slate-400 uppercase tracking-widest border-b border-slate-100 dark:border-slate-800">{{ i18n.t.subject }}</th>
                <th class="px-6 py-4 text-xs font-bold text-slate-400 uppercase tracking-widest border-b border-slate-100 dark:border-slate-800">{{ i18n.t.status }}</th>
                <th class="px-6 py-4 text-xs font-bold text-slate-400 uppercase tracking-widest border-b border-slate-100 dark:border-slate-800">{{ i18n.t.date }}</th>
                <th class="px-6 py-4 text-xs font-bold text-slate-400 uppercase tracking-widest border-b border-slate-100 dark:border-slate-800"></th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
              <tr v-for="item in tickets" :key="item.id" class="hover:bg-slate-50 dark:hover:bg-slate-800/50 transition-colors">
                <td class="px-6 py-5 font-mono text-xs text-slate-500 dark:text-slate-400">#{{ item.referenceNo }}</td>
                <td class="px-6 py-5">
                  <span :class="CATEGORY_MAP[normalizeCat(item.category)].color + ' px-2 py-1 rounded text-xs font-medium inline-flex items-center gap-1.5 hover:opacity-80 transition cursor-default'">
                    <span>{{ CATEGORY_MAP[normalizeCat(item.category)].icon }}</span>
                    <span>{{ tCategory(item.category) }}</span>
                  </span>
                </td>
                <td class="px-6 py-5">
                  <span class="text-sm text-slate-900 dark:text-white font-medium">{{ item.title }}</span>
                  <div class="text-[10px] text-slate-500 mt-1 uppercase">{{ item.assignedDepartmentName || item.departmentName || 'Birim Atanmadı' }}</div>
                </td>
                <td class="px-6 py-5">
                  <span :class="STATUS_MAP[item.status] + ' px-2 py-1 rounded text-xs font-medium inline-flex items-center hover:opacity-80 transition cursor-default'">
                    {{ tStatus(item.status) }}
                  </span>
                </td>
                <td class="px-6 py-5 text-sm text-slate-500 dark:text-slate-400 whitespace-nowrap">
                  {{ new Date(item.createdAt).toLocaleDateString(i18n.lang === 'tr' ? 'tr-TR' : 'en-GB') }}
                </td>
                <td class="px-6 py-5 text-right">
                  <button @click="router.push(`/tickets/${item.id}`)" class="p-2 text-slate-400 hover:text-blue-500 hover:bg-blue-500/10 rounded-lg transition-all">
                    <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" /></svg>
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <div class="p-4 border-t border-slate-200 dark:border-slate-800 shrink-0 bg-white dark:bg-slate-900 rounded-b-3xl">
          <Pagination v-model:page="page" v-model:pageSize="pageSize" :totalCount="totalCount" />
        </div>
      </div>
    </main>
  </div>
</template>
