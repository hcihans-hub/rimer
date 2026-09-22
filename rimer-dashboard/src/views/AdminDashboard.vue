<script setup>
import { ref, onMounted, computed, watch } from 'vue'
import { useRouter } from 'vue-router'
import { auth, toastStore } from '../auth'
import { i18n, theme } from '../lang'
import * as api from '../services/ticketApi'
import LangSelector from '../components/LangSelector.vue'
import NotificationBell from '../components/NotificationBell.vue'
import UsersView from './Users.vue'
import DepartmentsView from './Departments.vue'
import SystemLogsView from './SystemLogs.vue'
import AdminAnnouncements from './AdminAnnouncements.vue'
import Pagination from '../components/Pagination.vue'
import AdminTabs from '../components/AdminTabs.vue'
import ReminderFormPopup from '../components/ReminderFormPopup.vue'
import ReminderBadge from '../components/ReminderBadge.vue'
import ChartsView from './ChartsDashboard.vue'

const router = useRouter()
const tab = ref('tickets')

// ── TICKETS ───────────────────────────────────────────────────────
const tickets = ref([])
const loadingTickets = ref(true)
const page = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)

const filterSearch = ref('')
const filterStatus = ref('')
const filterCategory = ref('')
const filterDepartmentId = ref('')
const hideClosed = ref(true)
const startDate = ref('')
const endDate = ref('')
let searchTimeout = null

const loadTickets = async () => {
  loadingTickets.value = true
  try {
    const r = await api.getAllTickets(page.value, pageSize.value, filterSearch.value, filterStatus.value, '', filterDepartmentId.value, filterCategory.value, hideClosed.value, startDate.value, endDate.value)
    tickets.value = r.data?.data?.items || []
    totalCount.value = r.data?.data?.totalCount || 0
  } catch { toastStore.add('Talepler yüklenemedi', 'error') }
  finally { loadingTickets.value = false }
}

const onSearchInput = () => {
  clearTimeout(searchTimeout)
  searchTimeout = setTimeout(() => { page.value = 1; loadTickets() }, 500)
}

watch([page, pageSize], loadTickets)
watch([filterStatus, filterCategory, filterDepartmentId, hideClosed, startDate, endDate], () => { page.value = 1; loadTickets() })

// ── TICKET DETAIL DRAWER ──────────────────────────────────────────
const drawerOpen = ref(false)
const drawerTicket = ref(null)
const drawerLoading = ref(false)
const showReminderPopup = ref(false)
const departments = ref([])
const transferDeptId = ref('')
const transferNote = ref('')
const transferring = ref(false)

const openDrawer = async (ticketId) => {
  drawerOpen.value = true
  drawerLoading.value = true
  drawerTicket.value = null
  transferDeptId.value = ''
  transferNote.value = ''
  try {
    const r = await api.getTicketById(ticketId)
    drawerTicket.value = r.data?.data || null
    if (!departments.value.length) {
      const dr = await api.getDepartments(1, 200, true)
      departments.value = dr.data?.data?.items || []
    }
  } catch {
    toastStore.error('Talep detayı yüklenemedi.')
    drawerOpen.value = false
  } finally {
    drawerLoading.value = false
  }
}

const closeDrawer = () => {
  drawerOpen.value = false
  drawerTicket.value = null
}

const doTransfer = async () => {
  if (!transferDeptId.value || !drawerTicket.value) return
  transferring.value = true
  try {
    await api.transferTicket(drawerTicket.value.id, transferDeptId.value, transferNote.value)
    toastStore.success('Talep başarıyla yönlendirildi.')
    await loadTickets()
    const id = drawerTicket.value.id
    transferDeptId.value = ''
    transferNote.value = ''
    await openDrawer(id)
  } catch (e) {
    toastStore.error(e.response?.data?.message || 'Yönlendirme başarısız.')
  } finally {
    transferring.value = false
  }
}

// ── CHART PERMISSIONS ─────────────────────────────────────────────
const permUserId = ref('')
const permKeys = ref([])
const loadingPerms = ref(false)
const savingPerms = ref(false)
const allCharts = [
  { key: 'totalTickets',           label: 'Toplam Talep Kartı',          icon: '📋' },
  { key: 'categoryDistribution',   label: 'Kategori Dağılımı',           icon: '🥧' },
  { key: 'departmentRanking',      label: 'Departman Sıralaması',         icon: '📊' },
  { key: 'statusSummary',          label: 'Durum Özeti',                  icon: '🍩' },
  { key: 'agingAnalysis',          label: 'Yaşlandırma Analizi',          icon: '⏰' },
  { key: 'departmentPerformance',  label: 'Birim Performans Analizi',     icon: '🚀' },
  { key: 'satisfactionByDepartment', label: 'Memnuniyet Dağılımı (Birim)', icon: '💚' },
  { key: 'complaintByDepartment',  label: 'Şikayet Dağılımı (Birim)',    icon: '🔴' },
  { key: 'staffTypeDistribution',  label: 'Personel Türü Dağılımı',      icon: '👥' },
]

const loadPerms = async () => {
  if (!permUserId.value) return
  loadingPerms.value = true
  try { const r = await api.getChartPermissions(permUserId.value); permKeys.value = r.data || [] }
  catch { permKeys.value = [] }
  finally { loadingPerms.value = false }
}

const allPermissions = ref([])
const permissionsSearchTerm = ref('')
const users = ref([])

const groupedPermissions = computed(() => {
  const map = {}
  allPermissions.value.forEach(p => {
    if (!map[p.userId]) {
      const u = users.value.find(u => u.id === p.userId)
      map[p.userId] = { userId: p.userId, fullName: u?.fullName || 'Bilinmiyor', email: u?.email || '—', permissions: [], dateObj: new Date(p.createdAt) }
    }
    const chartDef = allCharts.find(c => c.key === p.chartKey)
    map[p.userId].permissions.push({ key: p.chartKey, label: chartDef?.label || p.chartKey, icon: chartDef?.icon || '📊', date: p.createdAt })
  })
  let list = Object.values(map)
  list.sort((a, b) => a.fullName.localeCompare(b.fullName))
  if (permissionsSearchTerm.value) {
    const s = permissionsSearchTerm.value.toLowerCase()
    list = list.filter(x => x.fullName.toLowerCase().includes(s) || x.email.toLowerCase().includes(s))
  }
  return list
})

const loadAllPerms = async () => {
  try { const r = await api.getAllChartPermissions(); allPermissions.value = r.data || [] }
  catch { allPermissions.value = [] }
}

const togglePerm = (k) => { const i = permKeys.value.indexOf(k); i >= 0 ? permKeys.value.splice(i, 1) : permKeys.value.push(k) }
const selectAllPerms = () => { permKeys.value = allCharts.map(c => c.key) }
const clearAllPerms = () => { permKeys.value = [] }

const savePerms = async () => {
  if (!permUserId.value) return
  savingPerms.value = true
  try {
    await api.assignChartPermissions(permUserId.value, permKeys.value)
    toastStore.add(`${permKeys.value.length} izin başarıyla kaydedildi`, 'success')
    await loadPerms(); await loadAllPerms()
  } catch (err) {
    toastStore.add(err.response?.data?.message || 'Kaydetme başarısız', 'error')
  } finally { savingPerms.value = false }
}

// ── HELPERS ───────────────────────────────────────────────────────
const statusColors = {
  Submitted: 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300 border-slate-200 dark:border-slate-700',
  Reviewing: 'bg-amber-100 text-amber-700 dark:bg-amber-500/10 dark:text-amber-400 border-amber-200 dark:border-amber-800',
  WaitingDepartment: 'bg-purple-100 text-purple-700 dark:bg-purple-500/10 dark:text-purple-400 border-purple-200 dark:border-purple-800',
  Answered: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-400 border-emerald-200 dark:border-emerald-800',
  Closed: 'bg-slate-100 text-slate-500 dark:bg-slate-800/60 dark:text-slate-500 border-slate-200 dark:border-slate-700',
}
const statusLabels = { Submitted: 'Yeni', Reviewing: 'İnceleniyor', WaitingDepartment: 'Birimde', Answered: 'Cevaplandı', Closed: 'Kapatıldı' }
const getStatusColor = (s) => statusColors[s] || 'bg-slate-100 text-slate-700'
const getStatusLabel = (s) => statusLabels[s] || i18n.t[`status${s}`] || s
const getCategoryLabel = (c) => i18n.t[c] || i18n.t[`cat${c}`] || c

const getPriorityBadge = (p) => {
  const val = typeof p === 'string' ? ({ Low: 1, Normal: 2, Important: 3, Critical: 4 }[p] || 2) : (p || 2)
  switch (val) {
    case 1: return { label: 'Düşük',  color: 'bg-slate-200 text-slate-600 dark:bg-slate-700 dark:text-slate-300' }
    case 3: return { label: 'Önemli', color: 'bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400' }
    case 4: return { label: 'Yüksek', color: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400', pulse: true }
    default: return { label: 'Normal', color: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400' }
  }
}

const getDelayBadge = (createdAt, status) => {
  if (status === 'Closed' || status === 'Answered') return null
  const age = Math.floor((Date.now() - new Date(createdAt).getTime()) / 86400000)
  if (age >= 90) return { label: '90+ gün', color: 'bg-red-900 text-white', pulse: true }
  if (age >= 60) return { label: '60+ gün', color: 'bg-red-600 text-white' }
  if (age >= 45) return { label: '45+ gün', color: 'bg-orange-500 text-white' }
  if (age >= 30) return { label: '30+ gün', color: 'bg-yellow-500 text-yellow-900' }
  return null
}

const historyActionLabel = (a) => ({
  Created: 'Oluşturuldu', Transferred: 'Yönlendirildi', Assigned: 'Görevlendirildi',
  StatusChanged: 'Durum Değişti', TakenOwnership: 'İşleme Alındı',
  Closed: 'Kapatıldı', InternalStatusChanged: 'İç Durum Güncellendi'
}[a] || a)

const unifiedTimeline = computed(() => {
  if (!drawerTicket.value) return []
  const events = []
  
  events.push({
    id: 'create',
    action: 'Talep Oluşturuldu',
    detail: 'Talep sisteme kaydedildi.',
    timestamp: new Date(drawerTicket.value.createdAt),
    author: drawerTicket.value.creatorName || (drawerTicket.value.guestName ? drawerTicket.value.guestName + ' ' + drawerTicket.value.guestSurname : 'Sistem'),
    type: 'create'
  })
  
  if (drawerTicket.value.histories) {
    drawerTicket.value.histories.forEach(h => {
      if (h.action === 'Created') return
      
      let actionName = historyActionLabel(h.action)
      let finalAction = h.departmentName ? `${h.departmentName} — ${actionName}` : actionName
      
      let detail = ''
      if (h.action === 'Transferred') detail = `Başvuru yönlendirildi. ${h.departmentName ? 'Birim: ' + h.departmentName + ' ' : ''}${h.note ? 'Not: ' + h.note : ''}`
      else if (h.action === 'Assigned') detail = `İlgili personele atandı. ${h.newValue ? '(' + h.newValue + ')' : ''}`
      else if (h.action === 'StatusChanged') detail = `Durum güncellendi: ${h.newValue}`
      else if (h.action === 'InternalStatusChanged') detail = `İç durum güncellendi: ${h.newValue}`
      else if (h.action === 'TakenOwnership') detail = `İlgili personel başvuruyu işleme aldı.`
      else if (h.action === 'Closed') detail = `Başvuru sonuçlandırıldı.`
      else detail = `${h.oldValue || ''} -> ${h.newValue || ''}`
      
      events.push({
        id: h.id,
        action: finalAction,
        detail: detail,
        timestamp: new Date(h.changedAt),
        author: h.changedBy?.fullName || 'Sistem',
        type: 'history'
      })
    })
  }

  if (drawerTicket.value.replies) {
    drawerTicket.value.replies.forEach(r => {
      events.push({
        id: 'reply-' + r.id,
        action: 'Cevaplandı',
        detail: r.message,
        timestamp: new Date(r.createdAt),
        author: r.authorName || r.createdByName || '🏢 Kurum',
        type: 'reply'
      })
    })
  }
  
  events.sort((a, b) => a.timestamp - b.timestamp)
  return events
})

const logout = () => { auth.logout(); router.push('/login') }

const loadUsers = async () => {
  try { const r = await api.getAllUsers(1, 1000, false); users.value = r.data?.data?.items || [] }
  catch {}
}

watch(tab, (t) => {
  if (t === 'tickets' && !tickets.value.length) loadTickets()
  if (t === 'permissions') { if (!users.value.length) loadUsers(); loadAllPerms() }
})

onMounted(async () => {
  const savedTab = localStorage.getItem('admin_last_tab')
  if (savedTab && savedTab !== 'analytics') { tab.value = savedTab; localStorage.removeItem('admin_last_tab') }
  loadTickets()
  try {
    if (!departments.value.length) {
      const dr = await api.getDepartments(1, 200, true)
      departments.value = dr.data?.data?.items || []
    }
  } catch {}
})
</script>

<template>
<div class="min-h-screen bg-slate-50 dark:bg-slate-950 flex flex-col">

  <!-- HEADER -->
  <header class="h-16 bg-white dark:bg-slate-900 border-b border-slate-200 dark:border-slate-800 px-6 flex items-center justify-between sticky top-0 z-40 shadow-sm print:hidden no-print">
    <div class="flex items-center gap-3">
      <img :src="theme.dark ? '/logonight.png' : '/logo.png'" alt="Rimer Logo" class="h-10 w-auto object-contain" />
      <span class="px-2 py-0.5 text-[9px] font-black text-white rounded uppercase" :class="auth.isOperator ? 'bg-amber-500' : 'bg-blue-600'">
        {{ auth.isOperator ? 'Operatör' : 'Admin' }}
      </span>
    </div>
    <div class="flex items-center gap-3">
      <ReminderBadge />
      <NotificationBell />
      <LangSelector />
      <button @click="theme.toggle()" :title="theme.dark ? 'Açık Mod' : 'Koyu Mod'" class="w-9 h-9 rounded-lg flex items-center justify-center text-sm hover:bg-slate-100 dark:hover:bg-slate-800 transition">{{ theme.dark ? '☀️' : '🌙' }}</button>
      <button @click="logout" title="Çıkış Yap" class="w-9 h-9 rounded-lg bg-red-50 dark:bg-red-900/20 text-red-500 flex items-center justify-center hover:bg-red-100 transition">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"/></svg>
      </button>
    </div>
  </header>

  <!-- Body: sidebar + content -->
  <div class="flex flex-1 overflow-hidden">

    <!-- LEFT SIDEBAR NAV -->
    <AdminTabs v-model:activeTab="tab" class="print:hidden no-print" />

    <main class="flex-1 overflow-y-auto p-5">

    <!-- ═══ TICKETS ═══ -->
    <template v-if="tab === 'tickets'">
      <!-- FILTERS -->
      <div class="flex flex-wrap items-center gap-3 mb-6 bg-white dark:bg-slate-900 p-4 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm">
        <div class="flex-1 min-w-[200px] relative">
          <svg class="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/></svg>
          <input v-model="filterSearch" @input="onSearchInput" type="text" placeholder="Başvuru/Referans No veya Konu ara…"
            class="w-full pl-10 pr-4 py-2.5 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-sm text-slate-700 dark:text-slate-300 outline-none focus:ring-2 focus:ring-indigo-500 transition-all"/>
        </div>
        
        <select v-model="filterCategory" class="px-4 py-2.5 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-sm text-slate-700 dark:text-slate-300 outline-none focus:ring-2 focus:ring-indigo-500 transition-all">
          <option value="">Tüm Kategoriler</option>
          <option value="Complaint">Şikayet</option>
          <option value="Suggestion">Öneri</option>
          <option value="Request">Talep</option>
          <option value="Information">Bilgi Edinme</option>
        </select>

        <select v-model="filterDepartmentId" class="px-4 py-2.5 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-sm text-slate-700 dark:text-slate-300 outline-none focus:ring-2 focus:ring-indigo-500 transition-all">
          <option value="">Tüm Birimler</option>
          <option v-for="d in departments" :key="d.id" :value="d.id">{{ d.name }}</option>
        </select>

        <select v-model="filterStatus" class="px-4 py-2.5 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-sm text-slate-700 dark:text-slate-300 outline-none focus:ring-2 focus:ring-indigo-500 transition-all">
          <option value="">Tüm Durumlar</option>
          <option value="Submitted">Başvuru Alındı</option>
          <option value="Reviewing">İnceleniyor</option>
          <option value="WaitingDepartment">İlgili Birimde</option>
          <option value="Answered">Cevaplandı</option>
          <option value="Closed">Kapatıldı</option>
        </select>
        
        <label class="flex items-center gap-2 cursor-pointer px-4 py-2.5 bg-slate-50 dark:bg-slate-800 rounded-xl border border-slate-200 dark:border-slate-700 transition-colors hover:bg-slate-100 dark:hover:bg-slate-700">
          <input type="checkbox" v-model="hideClosed" class="w-4 h-4 rounded border-slate-300 text-indigo-600 focus:ring-indigo-500 cursor-pointer" />
          <span class="text-sm text-slate-700 dark:text-slate-300">Kapananları Gizle</span>
        </label>

        <div class="flex items-center gap-2 bg-slate-50 dark:bg-slate-800 px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 w-full md:w-auto overflow-x-auto">
          <input type="date" v-model="startDate" class="bg-transparent border-none outline-none text-sm text-slate-700 dark:text-slate-300 font-medium w-32" />
          <span class="text-slate-400">-</span>
          <input type="date" v-model="endDate" class="bg-transparent border-none outline-none text-sm text-slate-700 dark:text-slate-300 font-medium w-32" />
        </div>
      </div>

      <div class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl shadow-sm flex flex-col" style="height: calc(100vh - 220px);">
        <div v-if="loadingTickets" class="p-6 space-y-3">
          <div v-for="i in 5" :key="i" class="h-14 bg-slate-100 dark:bg-slate-800 rounded-xl animate-pulse" />
        </div>
        <div v-else class="flex-1 overflow-y-auto">
          <table class="w-full text-left">
            <thead class="sticky top-0 z-10 bg-slate-50 dark:bg-slate-800/90 backdrop-blur">
              <tr>
                <th class="px-4 py-3 text-[10px] font-bold text-slate-400 uppercase border-b border-slate-200 dark:border-slate-700">Başvuru / Referans No</th>
                <th class="px-4 py-3 text-[10px] font-bold text-slate-400 uppercase border-b border-slate-200 dark:border-slate-700">Kategori</th>
                <th class="px-4 py-3 text-[10px] font-bold text-slate-400 uppercase border-b border-slate-200 dark:border-slate-700">Konu</th>
                <th class="px-4 py-3 text-[10px] font-bold text-slate-400 uppercase border-b border-slate-200 dark:border-slate-700">Öncelik</th>
                <th class="px-4 py-3 text-[10px] font-bold text-slate-400 uppercase border-b border-slate-200 dark:border-slate-700">Durum</th>
                <th class="px-4 py-3 text-[10px] font-bold text-slate-400 uppercase border-b border-slate-200 dark:border-slate-700">Atanan Birim</th>
                <th class="px-4 py-3 text-[10px] font-bold text-slate-400 uppercase border-b border-slate-200 dark:border-slate-700">Tarih</th>
                <th class="px-4 py-3 text-[10px] font-bold text-slate-400 uppercase border-b border-slate-200 dark:border-slate-700">Gecikme</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
              <tr
                v-for="t in tickets" :key="t.id"
                @click="openDrawer(t.id)"
                class="hover:bg-indigo-50/50 dark:hover:bg-indigo-900/10 transition cursor-pointer group"
                :class="(t.priority === 4 || t.priority === 'Critical') ? 'border-l-4 border-l-red-500' : ''"
              >
                <td class="px-4 py-4 font-mono text-xs text-slate-500 whitespace-nowrap group-hover:text-indigo-600 dark:group-hover:text-indigo-400 transition">#{{ t.referenceNo || t.id.substring(0,8).toUpperCase() }}</td>
                <td class="px-4 py-4">
                  <span class="px-2 py-0.5 rounded bg-slate-100 dark:bg-slate-800 text-[10px] font-bold text-slate-600 dark:text-slate-300 uppercase tracking-tight">{{ getCategoryLabel(t.category) }}</span>
                </td>
                <td class="px-4 py-4">
                  <div class="text-sm text-slate-900 dark:text-white font-medium max-w-[220px] truncate group-hover:text-indigo-700 dark:group-hover:text-indigo-300 transition">{{ t.title }}</div>
                </td>
                <td class="px-4 py-4">
                  <span class="px-2 py-0.5 rounded text-[10px] font-black uppercase tracking-wider shadow-sm" :class="[getPriorityBadge(t.priority).color, getPriorityBadge(t.priority).pulse ? 'animate-pulse' : '']">{{ getPriorityBadge(t.priority).label }}</span>
                </td>
                <td class="px-4 py-4">
                  <span class="px-2.5 py-1 rounded-lg text-[10px] font-black uppercase tracking-wider border" :class="getStatusColor(t.status)">{{ getStatusLabel(t.status) }}</span>
                </td>
                <td class="px-4 py-4">
                  <span v-if="t.assignedDepartmentName || t.departmentName" class="text-[11px] font-semibold text-slate-600 dark:text-slate-300">{{ t.assignedDepartmentName || t.departmentName }}</span>
                  <span v-else class="inline-flex items-center gap-1 text-[10px] font-bold text-amber-600 dark:text-amber-400 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800/50 px-2 py-0.5 rounded-lg">⚡ Bekliyor</span>
                </td>
                <td class="px-4 py-4 text-xs text-slate-500 font-medium whitespace-nowrap">{{ new Date(t.createdAt).toLocaleDateString('tr-TR') }}</td>
                <td class="px-4 py-4">
                  <span v-if="getDelayBadge(t.createdAt, t.status)" class="px-2 py-0.5 rounded text-[10px] font-black" :class="[getDelayBadge(t.createdAt, t.status).color, getDelayBadge(t.createdAt, t.status).pulse ? 'animate-pulse' : '']">{{ getDelayBadge(t.createdAt, t.status).label }}</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <div class="p-4 border-t border-slate-200 dark:border-slate-800 shrink-0 bg-white dark:bg-slate-900 rounded-b-2xl z-20">
          <Pagination v-model:page="page" v-model:pageSize="pageSize" :totalCount="totalCount" />
        </div>
      </div>
    </template>

    <!-- ═══ USERS ═══ -->
    <template v-if="tab === 'users' && !auth.isOperator">
      <UsersView />
    </template>

    <!-- ═══ DEPARTMENTS ═══ -->
    <template v-if="tab === 'departments' && !auth.isOperator">
      <DepartmentsView />
    </template>

    <!-- ═══ ANNOUNCEMENTS ═══ -->
    <template v-if="tab === 'announcements' && !auth.isOperator">
      <AdminAnnouncements />
    </template>

    <!-- ═══ SYSTEM LOGS ═══ -->
    <template v-if="tab === 'logs' && !auth.isOperator">
      <SystemLogsView />
    </template>

    <!-- ═══ CHART PERMISSIONS ═══ -->
    <template v-if="tab === 'permissions' && !auth.isOperator">
      <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-5 mb-4 shadow-sm">
        <h4 class="text-sm font-bold text-slate-700 dark:text-slate-300 mb-3 uppercase">Kullanıcı Seçin</h4>
        <div class="flex gap-3">
          <select v-model="permUserId" @change="loadPerms" class="flex-1 px-4 py-3 rounded-xl border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-sm text-slate-700 dark:text-slate-300 outline-none focus:ring-2 focus:ring-indigo-500">
            <option value="">Bir kullanıcı seçin…</option>
            <option v-for="u in users" :key="u.id" :value="u.id">{{ u.fullName || 'İsimsiz' }} — {{ u.email }}</option>
          </select>
          <button v-if="permUserId" @click="permUserId = ''" class="px-5 py-3 rounded-xl border border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-400 font-bold text-sm hover:bg-slate-50 dark:hover:bg-slate-800 transition">İptal</button>
        </div>
      </div>
      <div v-if="permUserId" class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-5 shadow-sm">
        <div class="flex justify-between items-center mb-4">
          <h4 class="text-sm font-bold text-slate-700 dark:text-slate-300 uppercase">Grafik İzinleri <span class="text-indigo-500 font-mono">({{ permKeys.length }}/{{ allCharts.length }})</span></h4>
          <div class="flex gap-2">
            <button @click="selectAllPerms" class="px-3 py-1 rounded-lg text-[10px] font-bold bg-indigo-100 dark:bg-indigo-900/30 text-indigo-600 hover:bg-indigo-200 transition">Tümü</button>
            <button @click="clearAllPerms" class="px-3 py-1 rounded-lg text-[10px] font-bold bg-slate-100 dark:bg-slate-800 text-slate-500 hover:bg-slate-200 transition">Temizle</button>
          </div>
        </div>
        <div v-if="loadingPerms" class="space-y-2"><div v-for="i in 5" :key="i" class="h-14 bg-slate-100 dark:bg-slate-800 rounded-xl animate-pulse" /></div>
        <div v-else class="space-y-2">
          <label v-for="c in allCharts" :key="c.key" class="flex items-center gap-3 p-3.5 rounded-xl border cursor-pointer transition-all" :class="permKeys.includes(c.key) ? 'border-indigo-500 bg-indigo-50 dark:bg-indigo-900/20' : 'border-slate-200 dark:border-slate-700 hover:border-slate-300'">
            <input type="checkbox" :checked="permKeys.includes(c.key)" @change="togglePerm(c.key)" class="w-4 h-4 rounded text-indigo-600" />
            <span class="text-lg">{{ c.icon }}</span>
            <span class="text-sm font-medium text-slate-700 dark:text-slate-300">{{ c.label }}</span>
            <span class="ml-auto text-[9px] font-mono text-slate-400">{{ c.key }}</span>
          </label>
        </div>
        <button @click="savePerms" :disabled="savingPerms" class="mt-4 w-full py-3 rounded-xl bg-indigo-600 text-white text-sm font-bold hover:bg-indigo-700 transition disabled:opacity-50">{{ savingPerms ? 'Kaydediliyor…' : 'İzinleri Kaydet' }}</button>
      </div>

      <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm mt-6 overflow-hidden">
        <div class="px-5 py-4 border-b border-slate-200 dark:border-slate-800 flex justify-between items-center gap-4 flex-wrap">
          <h4 class="text-sm font-bold text-slate-700 dark:text-slate-300 uppercase whitespace-nowrap">Verilen Tüm Yetkiler</h4>
          <input v-model="permissionsSearchTerm" type="text" placeholder="Kullanıcı veya e-posta ara..." class="px-4 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm w-full max-w-sm outline-none focus:ring-2 focus:ring-indigo-500 transition" />
        </div>
        <div class="overflow-x-auto" style="max-height:400px">
          <table class="w-full text-left">
            <thead class="sticky top-0 z-10 bg-slate-50 dark:bg-slate-800/90 backdrop-blur shadow-sm">
              <tr>
                <th class="px-5 py-3 text-[10px] font-bold text-slate-400 uppercase border-b border-slate-200 dark:border-slate-700 w-1/3">Kullanıcı Bilgisi</th>
                <th class="px-5 py-3 text-[10px] font-bold text-slate-400 uppercase border-b border-slate-200 dark:border-slate-700">Yetkiler (Grafikler)</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
              <tr v-for="g in groupedPermissions" :key="g.userId" class="hover:bg-slate-50 dark:hover:bg-slate-800/50 transition">
                <td class="px-5 py-4 align-top">
                  <div class="text-sm font-bold text-slate-900 dark:text-white">{{ g.fullName }}</div>
                  <div class="text-xs text-slate-500 mt-0.5">{{ g.email }}</div>
                  <div class="text-[10px] font-mono text-slate-400 mt-2 uppercase tracking-tight">Son İşlem: {{ g.dateObj.toLocaleString('tr-TR') }}</div>
                </td>
                <td class="px-5 py-4 align-top">
                  <div class="flex flex-wrap gap-2">
                    <span v-for="p in g.permissions" :key="p.key" class="inline-flex items-center gap-1.5 px-2.5 py-1.5 rounded-lg bg-indigo-50 dark:bg-indigo-900/20 border border-indigo-100 dark:border-indigo-800/50 text-xs font-medium text-indigo-700 dark:text-indigo-300">
                      <span class="text-sm">{{ p.icon }}</span> {{ p.label }}
                    </span>
                  </div>
                </td>
              </tr>
              <tr v-if="groupedPermissions.length === 0">
                <td colspan="2" class="px-5 py-8 text-center text-sm text-slate-500">
                  <div class="text-4xl mb-3">🔍</div>
                  {{ allPermissions.length === 0 ? 'Henüz hiçbir kullanıcıya grafik yetkisi verilmemiş.' : 'Arama kriterinize uygun kullanıcı bulunamadı.' }}
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </template>

    <!-- ═══ ANALYTICS (inline, no route change) ═══ -->
    <template v-if="tab === 'analytics'">
      <ChartsView :embedded="true" />
    </template>

    </main>
  </div><!-- end body flex -->
</div>

<!-- ════════════════════════════════════════════════════════════ -->
<!-- TICKET DETAIL DRAWER                                        -->
<!-- ════════════════════════════════════════════════════════════ -->

<Transition name="fade">
  <div v-if="drawerOpen" @click="closeDrawer" class="fixed inset-0 bg-black/50 backdrop-blur-sm z-50" />
</Transition>

<Transition name="slide-right">
  <aside v-if="drawerOpen" class="fixed right-0 top-0 h-full w-full max-w-2xl bg-white dark:bg-slate-900 border-l border-slate-200 dark:border-slate-800 z-50 flex flex-col shadow-2xl">

    <!-- Drawer Header -->
    <div class="flex items-center justify-between px-6 py-4 border-b border-slate-200 dark:border-slate-800 flex-shrink-0 bg-slate-50 dark:bg-slate-800/50">
      <div class="flex items-center gap-3">
        <span class="text-lg font-black text-slate-900 dark:text-white">Talep Detayı</span>
        <span v-if="drawerTicket" class="font-mono text-xs text-slate-400">#{{ drawerTicket.referenceNo }}</span>
      </div>
      <div class="flex items-center gap-2">
        <button v-if="drawerTicket" @click="showReminderPopup = true" class="w-8 h-8 flex items-center justify-center rounded-lg bg-indigo-100 dark:bg-indigo-900/30 text-indigo-600 dark:text-indigo-400 hover:bg-indigo-200 dark:hover:bg-indigo-900/50 transition font-black text-lg shadow-sm tooltip-trigger relative" title="Hatırlatıcı Ekle">
          <span class="text-[14px]">ℹ️</span>
        </button>
        <button @click="closeDrawer" class="w-8 h-8 flex items-center justify-center rounded-lg bg-slate-200 dark:bg-slate-700 hover:bg-slate-300 dark:hover:bg-slate-600 transition text-slate-600 dark:text-slate-300 font-black text-lg">×</button>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="drawerLoading" class="flex-1 p-6 space-y-4">
      <div v-for="i in 6" :key="i" class="h-12 bg-slate-100 dark:bg-slate-800 rounded-xl animate-pulse" />
    </div>

    <!-- Content -->
    <div v-else-if="drawerTicket" class="flex-1 overflow-y-auto p-6 space-y-6">

      <!-- Status + Priority + Date -->
      <div class="flex flex-wrap items-center gap-3">
        <span class="px-3 py-1.5 rounded-xl text-xs font-black uppercase tracking-wider border" :class="getStatusColor(drawerTicket.status)">{{ getStatusLabel(drawerTicket.status) }}</span>
        <span class="px-3 py-1.5 rounded-xl text-xs font-black uppercase tracking-wider" :class="getPriorityBadge(drawerTicket.priority).color">{{ getPriorityBadge(drawerTicket.priority).label }}</span>
        <span class="ml-auto text-xs text-slate-400">{{ new Date(drawerTicket.createdAt).toLocaleString('tr-TR') }}</span>
      </div>

      <!-- Title + Description -->
      <div class="space-y-3">
        <div>
          <p class="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-1">Konu</p>
          <p class="text-base font-black text-slate-900 dark:text-white leading-snug">{{ drawerTicket.title }}</p>
        </div>
        <div class="bg-slate-50 dark:bg-slate-800/60 p-4 rounded-2xl border border-slate-200 dark:border-slate-700">
          <p class="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2">📝 Talep İçeriği</p>
          <p class="text-sm text-slate-700 dark:text-slate-300 leading-relaxed whitespace-pre-wrap">{{ drawerTicket.description }}</p>
        </div>
      </div>

      <!-- Talebe Cevap(lar) -->
      <div v-if="drawerTicket.replies && drawerTicket.replies.length" class="space-y-3">
        <p class="text-[10px] font-black text-emerald-600 dark:text-emerald-400 uppercase tracking-widest flex items-center gap-2">
          <span class="w-5 h-5 bg-emerald-100 dark:bg-emerald-900/40 rounded-full flex items-center justify-center text-[10px]">💬</span>
          Talebe Cevap
        </p>
        <div
          v-for="reply in drawerTicket.replies"
          :key="reply.id"
          class="bg-emerald-50 dark:bg-emerald-900/10 border border-emerald-200 dark:border-emerald-800/50 rounded-2xl p-4"
        >
          <p class="text-sm text-slate-700 dark:text-slate-300 leading-relaxed whitespace-pre-wrap mb-3">{{ reply.message }}</p>
          <div class="flex items-center justify-between pt-2 border-t border-emerald-100 dark:border-emerald-800/40">
            <span class="text-[10px] font-black text-emerald-700 dark:text-emerald-400 uppercase tracking-wider flex items-center gap-1">
              👤 {{ reply.authorName || reply.createdByName || '🏢 Kurum' }}
            </span>
            <time class="text-[10px] text-slate-400 font-mono">
              {{ new Date(reply.createdAt).toLocaleString('tr-TR') }}
            </time>
          </div>
        </div>
      </div>

      <!-- Attachment -->
      <div v-if="drawerTicket.attachmentUrl">
        <p class="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2">📎 Ek Dosya</p>
        <a :href="drawerTicket.attachmentUrl" target="_blank" class="inline-flex items-center gap-2 px-4 py-2 rounded-xl bg-indigo-50 dark:bg-indigo-900/20 border border-indigo-200 dark:border-indigo-800 text-sm font-bold text-indigo-600 dark:text-indigo-400 hover:bg-indigo-100 transition">📎 Eki Görüntüle</a>
      </div>

      <!-- Applicant -->
      <div class="bg-slate-50 dark:bg-slate-800/40 rounded-2xl border border-slate-200 dark:border-slate-700 p-4">
        <p class="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-3">👤 Başvuru Sahibi</p>
        <div v-if="drawerTicket.isExternal && drawerTicket.hidePersonalInfo" class="flex items-center gap-3 p-3 bg-slate-100 dark:bg-slate-700/50 rounded-xl border border-slate-200 dark:border-slate-600">
          <span class="text-2xl">🔒</span>
          <div>
            <p class="text-xs font-black text-slate-600 dark:text-slate-300 uppercase tracking-widest">Kimliği Gizli Başvuru</p>
            <p class="text-[11px] text-slate-400 mt-0.5">Başvuru sahibi gizlilik talebinde bulunmuştur.</p>
          </div>
        </div>
        <div v-else class="grid grid-cols-2 gap-3">
          <div>
            <p class="text-[9px] font-black text-slate-400 uppercase mb-0.5">Ad Soyad</p>
            <p class="text-sm font-bold text-slate-700 dark:text-slate-300">
              <template v-if="drawerTicket.isExternal">{{ drawerTicket.guestName }} {{ drawerTicket.guestSurname }}</template>
              <template v-else>{{ drawerTicket.creator?.fullName || '—' }}</template>
            </p>
          </div>
          <div>
            <p class="text-[9px] font-black text-slate-400 uppercase mb-0.5">E-Posta</p>
            <p class="text-sm font-bold text-slate-700 dark:text-slate-300 break-all">{{ drawerTicket.isExternal ? drawerTicket.guestEmail : (drawerTicket.creator?.email || '—') }}</p>
          </div>
          <div v-if="drawerTicket.isExternal ? drawerTicket.guestPhone : drawerTicket.creator?.phoneNumber">
            <p class="text-[9px] font-black text-slate-400 uppercase mb-0.5">Telefon</p>
            <p class="text-sm font-bold text-slate-700 dark:text-slate-300">{{ drawerTicket.isExternal ? drawerTicket.guestPhone : drawerTicket.creator?.phoneNumber }}</p>
          </div>
          <div v-if="drawerTicket.isExternal && drawerTicket.guestAddress" class="col-span-2">
            <p class="text-[9px] font-black text-slate-400 uppercase mb-0.5">Adres</p>
            <p class="text-sm font-bold text-slate-700 dark:text-slate-300 leading-relaxed">{{ drawerTicket.guestAddress }}</p>
          </div>
        </div>
      </div>

      <!-- Transfer Form -->
      <div v-if="drawerTicket.status !== 'Closed' && drawerTicket.status !== 'Answered'" class="bg-amber-50 dark:bg-amber-900/10 rounded-2xl border border-amber-200 dark:border-amber-800/50 p-4">
        <p class="text-[10px] font-black text-amber-600 dark:text-amber-400 uppercase tracking-widest mb-3">🔀 Birime Yönlendir</p>
        <div class="space-y-3">
          <select v-model="transferDeptId" class="w-full px-4 py-3 rounded-xl border border-amber-200 dark:border-amber-700 bg-white dark:bg-slate-800 text-sm font-medium text-slate-700 dark:text-slate-300 outline-none focus:ring-2 focus:ring-amber-400">
            <option value="">Birim seçin…</option>
            <option v-for="d in departments" :key="d.id" :value="d.id">{{ d.name }}</option>
          </select>
          <input v-model="transferNote" type="text" placeholder="Yönlendirme notu (opsiyonel)" class="w-full px-4 py-3 rounded-xl border border-amber-200 dark:border-amber-700 bg-white dark:bg-slate-800 text-sm font-medium text-slate-700 dark:text-slate-300 outline-none focus:ring-2 focus:ring-amber-400" />
          <button @click="doTransfer" :disabled="!transferDeptId || transferring" class="w-full py-3 rounded-xl bg-amber-500 hover:bg-amber-600 text-white font-black text-sm transition disabled:opacity-50 disabled:cursor-not-allowed">
            {{ transferring ? 'Yönlendiriliyor…' : 'Yönlendir →' }}
          </button>
        </div>
      </div>

      <!-- Unified Timeline -->
      <div v-if="unifiedTimeline && unifiedTimeline.length">
        <p class="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-3">📜 İşlem Geçmişi</p>
        <div class="relative space-y-4 before:absolute before:left-4 before:top-2 before:bottom-2 before:w-0.5 before:bg-slate-200 dark:before:bg-slate-700">
          <div
            v-for="event in unifiedTimeline"
            :key="event.id"
            class="relative pl-11"
          >
            <div class="absolute left-0 top-1 w-8 h-8 rounded-full flex items-center justify-center border-2 border-white dark:border-slate-900 z-10 shadow-sm"
              :class="event.type === 'create' ? 'bg-blue-500' : event.type === 'reply' ? 'bg-emerald-500' : event.action.includes('Yönlendirildi') ? 'bg-amber-500' : event.action.includes('Kapatıldı') ? 'bg-slate-500' : 'bg-indigo-500'">
              <span class="text-[10px] text-white">{{ event.type === 'create' ? '✓' : event.type === 'reply' ? '💬' : event.action.includes('Yönlendirildi') ? '→' : event.action.includes('Kapatıldı') ? '🔒' : '•' }}</span>
            </div>
            <div class="bg-slate-50 dark:bg-slate-800/60 border border-slate-100 dark:border-slate-700 rounded-xl p-3"
                 :class="{'border-emerald-200 dark:border-emerald-800 bg-emerald-50 dark:bg-emerald-900/10': event.type === 'reply'}">
              <div class="flex items-start justify-between gap-2">
                <div class="flex-1 min-w-0">
                  <p class="text-xs font-black text-slate-900 dark:text-white" :class="{'text-emerald-700 dark:text-emerald-400': event.type === 'reply'}">
                    {{ event.action }}
                  </p>
                  <p class="text-[11px] text-slate-500 dark:text-slate-400 mt-1" :class="{'italic': event.type !== 'reply', 'whitespace-pre-wrap font-medium text-slate-700 dark:text-slate-300': event.type === 'reply'}">
                    {{ event.detail }}
                  </p>
                  <p class="text-[10px] text-slate-400 mt-2 font-bold">{{ event.author }}</p>
                </div>
                <time class="text-[10px] text-slate-400 whitespace-nowrap flex-shrink-0">{{ event.timestamp.toLocaleString('tr-TR') }}</time>
              </div>
            </div>
          </div>
        </div>
      </div>

    </div>
  </aside>
</Transition>

<ReminderFormPopup 
  :show="showReminderPopup" 
  :ticketId="drawerTicket?.id" 
  @close="showReminderPopup = false"
  @created="loadTickets"
/>

</template>

<style scoped>
.fade-enter-active, .fade-leave-active { transition: opacity 0.25s ease; }
.fade-enter-from, .fade-leave-to { opacity: 0; }

.slide-right-enter-active, .slide-right-leave-active { transition: transform 0.3s cubic-bezier(0.16, 1, 0.3, 1); }
.slide-right-enter-from, .slide-right-leave-to { transform: translateX(100%); }
</style>
