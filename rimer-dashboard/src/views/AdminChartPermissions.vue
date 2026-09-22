<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { auth, toastStore } from '../auth'
import { theme } from '../lang'
import * as api from '../services/ticketApi'
import LangSelector from '../components/LangSelector.vue'

const router = useRouter()

const users = ref([])
const selectedUserId = ref('')
const selectedPerms = ref([])
const saving = ref(false)
const loadingUsers = ref(true)
const loadingPerms = ref(false)

const searchQuery = ref('')
const isDropdownOpen = ref(false)

const filteredUsers = computed(() => {
  if (!searchQuery.value) return users.value
  const q = searchQuery.value.toLowerCase()
  return users.value.filter(u => {
    const fullName = (u.fullName || '').toLowerCase()
    const email = (u.email || '').toLowerCase()
    return fullName.includes(q) || email.includes(q)
  })
})

const selectUser = (user) => {
  selectedUserId.value = user.id
  searchQuery.value = ''
  isDropdownOpen.value = false
  loadPerms()
}

const clearUserSelection = () => {
  selectedUserId.value = ''
  searchQuery.value = ''
  selectedPerms.value = []
  isDropdownOpen.value = true
}

const allCharts = [
  { key: 'totalTickets',          label: 'Toplam Talep Kartı',           icon: '📋', desc: 'KPI: toplam bilet sayısı' },
  { key: 'categoryDistribution',  label: 'Kategori Dağılımı',            icon: '🥧', desc: 'Pasta grafik: şikayet, öneri, talep…' },
  { key: 'departmentRanking',     label: 'Departman Sıralaması',         icon: '📊', desc: 'Yatay bar: en çok bilet alan birimler' },
  { key: 'statusSummary',         label: 'Durum Özeti',                  icon: '🍩', desc: 'Donut: çözüldü, işlemde, cevaplanmadı' },
  { key: 'agingAnalysis',         label: 'Yaşlandırma Analizi',          icon: '⏰', desc: 'Bar: 45, 60, 90+ gün açık biletler' },
  { key: 'departmentPerformance', label: 'Birim Performans Analizi',     icon: '🚀', desc: 'Birim bazlı çözüm oranları ve KPI' },
  { key: 'satisfactionByDepartment', label: 'Memnuniyet Dağılımı (Birim)', icon: '💚', desc: 'Teşekkür başvurularının birim dağılımı' },
  { key: 'complaintByDepartment',    label: 'Şikayet Dağılımı (Birim)',    icon: '🔴', desc: 'Şikayet başvurularının birim dağılımı' },
  { key: 'staffTypeDistribution',    label: 'Personel Türü Dağılımı',     icon: '👥', desc: 'İdari/Akademik/İşçi/Öğrenci başvuru analizi' },
]

const selectedUser = computed(() => users.value.find(u => u.id === selectedUserId.value))
const permCount = computed(() => selectedPerms.value.length)

// ── Load users ────────────────────────────────────────────────────
onMounted(async () => {
  try {
    const res = await api.getAllUsers()
    users.value = res.data?.data?.items || res.data?.data || res.data || []
  } catch {
    toastStore.add('Kullanıcılar yüklenemedi', 'error')
  } finally {
    loadingUsers.value = false
  }
})

// ── Load perms for selected user ──────────────────────────────────
const loadPerms = async () => {
  if (!selectedUserId.value) return
  loadingPerms.value = true
  try {
    const res = await api.getChartPermissions(selectedUserId.value)
    selectedPerms.value = res.data || []
  } catch {
    selectedPerms.value = []
  } finally {
    loadingPerms.value = false
  }
}

// ── Toggle / Select / Clear ───────────────────────────────────────
const toggle = (key) => {
  const i = selectedPerms.value.indexOf(key)
  if (i >= 0) selectedPerms.value.splice(i, 1)
  else selectedPerms.value.push(key)
}
const selectAll = () => { selectedPerms.value = allCharts.map(c => c.key) }
const clearAll = () => { selectedPerms.value = [] }

// ── Save ──────────────────────────────────────────────────────────
const save = async () => {
  if (!selectedUserId.value) return
  saving.value = true
  try {
    await api.assignChartPermissions(selectedUserId.value, selectedPerms.value)
    toastStore.add(`${permCount.value} izin başarıyla kaydedildi`, 'success')
    await loadPerms() // Reload to confirm persisted state
  } catch (err) {
    const msg = err.response?.data?.message || 'İzinler kaydedilemedi';
    toastStore.add(msg, 'error')
  } finally {
    saving.value = false
  }
}

const logout = () => { auth.logout(); router.push('/login') }
</script>

<template>
  <div class="min-h-screen bg-slate-50 dark:bg-slate-950 flex flex-col transition-colors duration-300">

    <!-- HEADER -->
    <header class="h-16 bg-white dark:bg-slate-900 border-b border-slate-200 dark:border-slate-800 px-6 flex items-center justify-between sticky top-0 z-40 shadow-sm">
      <div class="flex items-center gap-3">
        <button @click="router.push('/admin')" class="flex items-center gap-1.5 text-xs font-bold text-slate-500 hover:text-indigo-600 uppercase transition">
          <span class="text-base">←</span> Admin Panel
        </button>
        <div class="h-5 w-px bg-slate-200 dark:bg-slate-700" />
        <span class="text-xs font-bold text-indigo-500 uppercase tracking-widest">Grafik İzin Yönetimi</span>
      </div>
      <div class="flex items-center gap-3">
        <LangSelector />
        <button @click="theme.toggle()" class="w-9 h-9 rounded-lg flex items-center justify-center text-sm hover:bg-slate-100 dark:hover:bg-slate-800 transition">
          {{ theme.dark ? '☀️' : '🌙' }}
        </button>
        <button @click="logout" class="w-9 h-9 rounded-lg bg-red-50 dark:bg-red-900/20 text-red-500 flex items-center justify-center hover:bg-red-100 dark:hover:bg-red-900/40 transition">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"/></svg>
        </button>
      </div>
    </header>

    <main class="flex-1 p-4 md:p-6 lg:p-8 max-w-3xl mx-auto w-full">

      <!-- STEP 1: USER SELECT -->
      <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-6 mb-6 shadow-sm">
        <h3 class="text-sm font-bold text-slate-700 dark:text-slate-300 uppercase tracking-wider mb-4">1. Kullanıcı Seçin</h3>

        <div v-if="loadingUsers" class="h-12 bg-slate-100 dark:bg-slate-800 rounded-xl animate-pulse" />
        <div v-else class="relative">
          <!-- Eğer bir kullanıcı seçildiyse rozetini göster -->
          <div v-if="selectedUserId" class="w-full pl-4 pr-3 py-2.5 rounded-xl border border-indigo-200 dark:border-indigo-500/30 bg-indigo-50/50 dark:bg-indigo-900/10 flex items-center justify-between shadow-sm">
            <div class="flex-1 min-w-0">
              <p class="text-sm font-semibold text-slate-800 dark:text-slate-200 truncate">{{ selectedUser?.fullName || 'İsimsiz' }}</p>
              <p class="text-[10px] text-slate-500 truncate">{{ selectedUser?.email }}</p>
            </div>
            <button @click="clearUserSelection" class="p-1.5 ml-2 text-slate-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-lg transition-colors" title="Seçimi Temizle">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" /></svg>
            </button>
          </div>

          <!-- Seçim yoksa Arama Inputu göster -->
          <div v-else class="relative">
            <input 
              type="text" 
              v-model="searchQuery" 
              @focus="isDropdownOpen = true"
              @blur="() => setTimeout(() => isDropdownOpen = false, 200)"
              placeholder="İsim veya e-posta ile kullanıcı arayın..."
              class="w-full pl-4 pr-10 py-3 rounded-xl border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-sm text-slate-700 dark:text-slate-300 focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 outline-none transition placeholder-slate-400"
            />
            <div class="absolute right-3 top-3.5 text-slate-400 pointer-events-none">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" /></svg>
            </div>
          </div>

          <!-- Dropdown List -->
          <ul v-if="isDropdownOpen" class="absolute z-50 w-full mt-2 max-h-64 overflow-y-auto bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl shadow-xl overflow-hidden">
            <li 
              v-for="u in filteredUsers" :key="u.id"
              @click="selectUser(u)"
              class="px-4 py-2.5 hover:bg-slate-50 dark:hover:bg-slate-700/50 cursor-pointer flex flex-col sm:flex-row sm:items-center sm:justify-between border-b border-slate-100 dark:border-slate-700/50 last:border-0 transition-colors">
               <div>
                 <p class="text-sm font-semibold text-slate-800 dark:text-slate-200">{{ u.fullName || 'İsimsiz' }}</p>
                 <p class="text-[10px] text-slate-500 dark:text-slate-400">{{ u.email }}</p>
               </div>
               <span class="inline-block mt-1 sm:mt-0 px-2 py-0.5 rounded bg-slate-100 dark:bg-slate-700 text-[10px] text-slate-600 dark:text-slate-300 uppercase tracking-wider">{{ u.role }}</span>
            </li>
            <li v-if="filteredUsers.length === 0" class="px-4 py-3 text-sm text-slate-500 text-center">
              Arama kriterlerine uygun kullanıcı bulunamadı.
            </li>
          </ul>
        </div>
      </div>

      <!-- STEP 2: CHART PERMISSIONS -->
      <template v-if="selectedUserId">
        <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-6 shadow-sm">
          <div class="flex items-center justify-between mb-5">
            <div>
              <h3 class="text-sm font-bold text-slate-700 dark:text-slate-300 uppercase tracking-wider">2. Grafik İzinleri</h3>
              <p v-if="selectedUser" class="text-xs text-slate-400 mt-1">
                <span class="font-semibold text-indigo-500">{{ selectedUser.fullName || selectedUser.email }}</span>
                — <span class="font-mono">{{ permCount }}/{{ allCharts.length }}</span> izin
              </p>
            </div>
            <div class="flex gap-2">
              <button @click="selectAll" class="px-3 py-1.5 rounded-lg text-[10px] font-bold bg-indigo-100 dark:bg-indigo-900/30 text-indigo-600 hover:bg-indigo-200 dark:hover:bg-indigo-800/40 transition">Tümünü Seç</button>
              <button @click="clearAll" class="px-3 py-1.5 rounded-lg text-[10px] font-bold bg-slate-100 dark:bg-slate-800 text-slate-500 hover:bg-slate-200 dark:hover:bg-slate-700 transition">Temizle</button>
            </div>
          </div>

          <!-- Loading skeleton -->
          <div v-if="loadingPerms" class="space-y-3">
            <div v-for="i in 5" :key="i" class="h-16 bg-slate-100 dark:bg-slate-800 rounded-xl animate-pulse" />
          </div>

          <!-- Checkbox list -->
          <div v-else class="space-y-2">
            <label v-for="chart in allCharts" :key="chart.key"
              class="flex items-center gap-4 p-4 rounded-xl border cursor-pointer transition-all duration-200 group"
              :class="selectedPerms.includes(chart.key)
                ? 'border-indigo-500 bg-indigo-50 dark:bg-indigo-900/20 shadow-sm'
                : 'border-slate-200 dark:border-slate-700 hover:border-slate-300 dark:hover:border-slate-600'">
              <input type="checkbox" :checked="selectedPerms.includes(chart.key)" @change="toggle(chart.key)"
                class="w-5 h-5 rounded border-slate-300 text-indigo-600 focus:ring-indigo-500 flex-shrink-0" />
              <span class="text-xl flex-shrink-0">{{ chart.icon }}</span>
              <div class="min-w-0">
                <p class="text-sm font-semibold text-slate-700 dark:text-slate-300">{{ chart.label }}</p>
                <p class="text-[10px] text-slate-400 truncate">{{ chart.desc }}</p>
              </div>
              <span class="ml-auto text-[9px] font-mono text-slate-300 dark:text-slate-600 hidden sm:block">{{ chart.key }}</span>
            </label>
          </div>

          <!-- Save -->
          <button @click="save" :disabled="saving"
            class="mt-6 w-full py-3.5 rounded-xl font-bold text-sm text-white transition-all duration-200"
            :class="saving ? 'bg-slate-400 cursor-not-allowed' : 'bg-indigo-600 hover:bg-indigo-700 shadow-lg shadow-indigo-500/25 active:scale-[0.99]'">
            {{ saving ? 'Kaydediliyor…' : `İzinleri Kaydet (${permCount})` }}
          </button>
        </div>
      </template>
    </main>
  </div>
</template>

<style scoped>
* { transition: background-color 0.3s ease, border-color 0.3s ease, color 0.2s ease; }
</style>
