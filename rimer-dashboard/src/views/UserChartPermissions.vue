<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { auth, toastStore } from '../auth'
import { theme } from '../lang'
import * as api from '../services/ticketApi'

const router = useRouter()

const users = ref([])
const selectedUserId = ref('')
const selectedPerms = ref([])
const saving = ref(false)
const loadingUsers = ref(true)
const loadingPerms = ref(false)

const allCharts = [
  { key: 'totalTickets', label: 'Toplam Talep Kartı', icon: '📋' },
  { key: 'categoryDistribution', label: 'Kategori Dağılımı (Pasta)', icon: '🥧' },
  { key: 'departmentRanking', label: 'Departman Sıralaması (Bar)', icon: '📊' },
  { key: 'statusSummary', label: 'Durum Özeti (Donut)', icon: '🍩' },
  { key: 'agingAnalysis', label: 'Yaşlandırma Analizi (Bar)', icon: '⏰' },
  { key: 'departmentPerformance', label: 'Birim Performans Analizi', icon: '🚀' },
  { key: 'satisfactionByDepartment', label: 'Memnuniyet Dağılımı (Birim)', icon: '💚' },
  { key: 'complaintByDepartment', label: 'Şikayet Dağılımı (Birim)', icon: '🔴' },
  { key: 'staffTypeDistribution', label: 'Personel Türü Dağılımı', icon: '👥' },
]

// ── Load users ────────────────────────────────────────────────────
onMounted(async () => {
  try {
    const res = await api.getAllUsers()
    users.value = res.data?.data || res.data || []
  } catch (e) {
    toastStore.add('Kullanıcılar yüklenemedi', 'error')
  } finally {
    loadingUsers.value = false
  }
})

// ── Load permissions for selected user ────────────────────────────
const loadPerms = async () => {
  if (!selectedUserId.value) return
  loadingPerms.value = true
  try {
    const res = await api.getChartPermissions(selectedUserId.value)
    selectedPerms.value = res.data || []
  } catch (e) {
    toastStore.add('İzinler yüklenemedi', 'error')
    selectedPerms.value = []
  } finally {
    loadingPerms.value = false
  }
}

// ── Toggle chart permission ───────────────────────────────────────
const toggleChart = (key) => {
  if (selectedPerms.value.includes(key)) {
    selectedPerms.value = selectedPerms.value.filter(k => k !== key)
  } else {
    selectedPerms.value.push(key)
  }
}

const selectAll = () => { selectedPerms.value = allCharts.map(c => c.key) }
const clearAll = () => { selectedPerms.value = [] }

// ── Save ──────────────────────────────────────────────────────────
const save = async () => {
  if (!selectedUserId.value) return
  saving.value = true
  try {
    await api.assignChartPermissions(selectedUserId.value, selectedPerms.value)
    toastStore.add('İzinler başarıyla güncellendi', 'success')
    await loadPerms() // Reload to confirm persisted state
  } catch (err) {
    const msg = err.response?.data?.message || 'İzinler kaydedilemedi';
    toastStore.add(msg, 'error')
  } finally {
    saving.value = false
  }
}

const selectedUserName = () => {
  const u = users.value.find(u => u.id === selectedUserId.value)
  return u ? (u.fullName || u.email) : ''
}
</script>

<template>
  <div class="min-h-screen bg-slate-50 dark:bg-slate-950 flex flex-col transition-colors duration-300">

    <!-- HEADER -->
    <header class="h-16 bg-white dark:bg-slate-900 border-b border-slate-200 dark:border-slate-800 px-6 flex items-center justify-between sticky top-0 z-40 shadow-sm">
      <div class="flex items-center gap-3">
        <button @click="router.push('/dashboard')" class="text-xs font-bold text-slate-500 hover:text-indigo-600 uppercase flex items-center gap-1">
          <span class="text-base">←</span> Admin Panel
        </button>
        <div class="h-5 w-px bg-slate-200 dark:bg-slate-700" />
        <span class="text-xs font-bold text-slate-500 dark:text-slate-400 uppercase tracking-widest">Grafik İzin Yönetimi</span>
      </div>
      <button @click="theme.toggle()" class="w-9 h-9 rounded-lg flex items-center justify-center text-sm hover:bg-slate-100 dark:hover:bg-slate-800 transition">
        {{ theme.dark ? '☀️' : '🌙' }}
      </button>
    </header>

    <main class="flex-1 p-4 md:p-6 lg:p-8 max-w-3xl mx-auto w-full">

      <!-- USER SELECT -->
      <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-6 mb-6 shadow-sm">
        <h3 class="text-sm font-bold text-slate-700 dark:text-slate-300 uppercase tracking-wider mb-4">Kullanıcı Seçin</h3>

        <div v-if="loadingUsers" class="h-12 bg-slate-100 dark:bg-slate-800 rounded-xl animate-pulse" />
        <select v-else v-model="selectedUserId" @change="loadPerms"
          class="w-full px-4 py-3 rounded-xl border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-sm text-slate-700 dark:text-slate-300 focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 outline-none transition">
          <option value="" disabled>Bir kullanıcı seçin…</option>
          <option v-for="u in users" :key="u.id" :value="u.id">
            {{ u.fullName || 'İsimsiz' }} — {{ u.email }}
          </option>
        </select>
      </div>

      <!-- PERMISSIONS -->
      <div v-if="selectedUserId" class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-6 shadow-sm">
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-sm font-bold text-slate-700 dark:text-slate-300 uppercase tracking-wider">
            Grafik İzinleri
            <span class="text-indigo-500 ml-1">{{ selectedUserName() }}</span>
          </h3>
          <div class="flex gap-2">
            <button @click="selectAll" class="px-3 py-1 rounded-lg text-[10px] font-bold bg-indigo-100 dark:bg-indigo-900/30 text-indigo-600 hover:bg-indigo-200 transition">Tümünü Seç</button>
            <button @click="clearAll" class="px-3 py-1 rounded-lg text-[10px] font-bold bg-slate-100 dark:bg-slate-800 text-slate-500 hover:bg-slate-200 transition">Temizle</button>
          </div>
        </div>

        <div v-if="loadingPerms" class="space-y-3">
          <div v-for="i in 5" :key="i" class="h-14 bg-slate-100 dark:bg-slate-800 rounded-xl animate-pulse" />
        </div>

        <div v-else class="space-y-2">
          <label v-for="chart in allCharts" :key="chart.key"
            class="flex items-center gap-4 p-4 rounded-xl border cursor-pointer transition-all duration-200"
            :class="selectedPerms.includes(chart.key)
              ? 'border-indigo-500 bg-indigo-50 dark:bg-indigo-900/20 shadow-sm'
              : 'border-slate-200 dark:border-slate-700 hover:border-slate-300 dark:hover:border-slate-600'">
            <input type="checkbox" :checked="selectedPerms.includes(chart.key)" @change="toggleChart(chart.key)"
              class="w-5 h-5 rounded border-slate-300 text-indigo-600 focus:ring-indigo-500" />
            <span class="text-xl">{{ chart.icon }}</span>
            <div>
              <p class="text-sm font-semibold text-slate-700 dark:text-slate-300">{{ chart.label }}</p>
              <p class="text-[10px] text-slate-400 font-mono">{{ chart.key }}</p>
            </div>
          </label>
        </div>

        <!-- SAVE -->
        <button @click="save" :disabled="saving"
          class="mt-6 w-full py-3 rounded-xl font-bold text-sm text-white transition-all duration-200"
          :class="saving ? 'bg-slate-400 cursor-not-allowed' : 'bg-indigo-600 hover:bg-indigo-700 shadow-lg shadow-indigo-500/25'">
          {{ saving ? 'Kaydediliyor…' : 'İzinleri Kaydet' }}
        </button>

        <!-- Summary -->
        <p class="mt-3 text-center text-xs text-slate-400">
          {{ selectedPerms.length }} / {{ allCharts.length }} grafik izni atandı
        </p>
      </div>
    </main>
  </div>
</template>

<style scoped>
* { transition: background-color 0.3s ease, border-color 0.3s ease, color 0.2s ease; }
</style>
