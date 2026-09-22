<template>
  <div class="space-y-4">
    <div class="flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
      <h3 class="text-lg font-bold text-slate-900 dark:text-white">Sistem Logları</h3>
      <div class="flex items-center gap-2">
        <button @click="resetFilters" class="px-4 py-2 rounded-xl bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300 text-xs font-bold hover:bg-slate-200 dark:hover:bg-slate-700 transition">
          Filtreleri Temizle
        </button>
        <button @click="fetchData" class="px-4 py-2 rounded-xl bg-indigo-600 text-white text-xs font-bold hover:bg-indigo-700 transition">
          Yenile
        </button>
      </div>
    </div>

    <!-- Filters -->
    <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-4 shadow-sm">
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <!-- Action Filter -->
        <div class="space-y-1">
          <label class="text-[10px] font-bold text-slate-400 uppercase ml-1">İşlem</label>
          <input v-model="filters.action" placeholder="Örn: Create, Update..." 
            class="w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm outline-none focus:ring-2 focus:ring-indigo-500 transition"/>
        </div>
        <!-- User Filter -->
        <div class="space-y-1">
          <label class="text-[10px] font-bold text-slate-400 uppercase ml-1">Kullanıcı</label>
          <input v-model="filters.userName" placeholder="İsim veya E-posta..." 
            class="w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm outline-none focus:ring-2 focus:ring-indigo-500 transition"/>
        </div>
        <!-- Details Filter -->
        <div class="space-y-1">
          <label class="text-[10px] font-bold text-slate-400 uppercase ml-1">Detay / Hedef</label>
          <input v-model="filters.details" placeholder="ID veya Konu..." 
            class="w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm outline-none focus:ring-2 focus:ring-indigo-500 transition"/>
        </div>
        <!-- Date Filters -->
        <div class="grid grid-cols-2 gap-2">
          <div class="space-y-1">
            <label class="text-[10px] font-bold text-slate-400 uppercase ml-1">Başlangıç</label>
            <input v-model="filters.startDate" type="date"
              class="w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm outline-none focus:ring-2 focus:ring-indigo-500 transition"/>
          </div>
          <div class="space-y-1">
            <label class="text-[10px] font-bold text-slate-400 uppercase ml-1">Bitiş</label>
            <input v-model="filters.endDate" type="date"
              class="w-full px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm outline-none focus:ring-2 focus:ring-indigo-500 transition"/>
          </div>
        </div>
      </div>
    </div>

    <!-- Table -->
    <div class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl shadow-sm flex flex-col" style="height: calc(100vh - 200px);">
      <div v-if="loading" class="p-6 space-y-3">
        <div v-for="i in pageSize" :key="i" class="h-14 bg-slate-100 dark:bg-slate-800 rounded-xl animate-pulse"/>
      </div>
      <div v-else class="flex-1 overflow-y-auto">
        <table class="w-full text-left relative">
          <thead class="sticky top-0 z-10 bg-slate-50 dark:bg-slate-800/90 backdrop-blur">
            <tr>
              <th class="px-5 py-3 text-[10px] font-bold text-slate-400 uppercase border-b border-slate-200 dark:border-slate-700">İşlem</th>
            <th class="px-5 py-3 text-[10px] font-bold text-slate-400 uppercase border-b border-slate-200 dark:border-slate-700">Kullanıcı</th>
            <th class="px-5 py-3 text-[10px] font-bold text-slate-400 uppercase border-b border-slate-200 dark:border-slate-700">Varlık</th>
            <th class="px-5 py-3 text-[10px] font-bold text-slate-400 uppercase border-b border-slate-200 dark:border-slate-700">Detay / Hedef</th>
            <th class="px-5 py-3 text-[10px] font-bold text-slate-400 uppercase border-b border-slate-200 dark:border-slate-700">Tarih</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
          <tr v-for="l in items" :key="l.id" class="hover:bg-slate-50 dark:hover:bg-slate-800/50 transition">
            <td class="px-5 py-4">
              <span class="px-2.5 py-1 rounded-lg text-[10px] font-bold uppercase bg-blue-100 text-blue-600 dark:bg-blue-900/30 dark:text-blue-400">
                {{ l.action }}
              </span>
            </td>
            <td class="px-5 py-4 text-xs">
              <div class="font-semibold text-slate-900 dark:text-white truncate max-w-[150px]">{{ l.userName || 'Bilinmiyor' }}</div>
              <div class="font-mono text-[9px] text-slate-400 truncate max-w-[150px]">ID: {{ l.userId }}</div>
            </td>
            <td class="px-5 py-4 text-sm font-semibold text-slate-900 dark:text-white">{{ l.entityName || l.entity || '—' }}</td>
            <td class="px-5 py-4 text-xs text-slate-600 dark:text-slate-300 max-w-[250px]">
              <div v-if="l.details" class="font-medium text-slate-900 dark:text-white mb-1">{{ l.details }}</div>
              <div class="font-mono text-[10px] text-slate-400 truncate">ID: {{ l.entityId || '—' }}</div>
            </td>
            <td class="px-5 py-4 text-xs text-slate-500">
              {{ new Date(l.timestamp).toLocaleString('tr-TR') }}
            </td>
          </tr>
        </tbody>
      </table>
      </div>
      
      <div class="p-4 border-t border-slate-200 dark:border-slate-800 shrink-0 bg-white dark:bg-slate-900 rounded-b-2xl z-20">
        <Pagination v-model:page="page" v-model:pageSize="pageSize" :totalCount="totalCount" />
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, watch } from 'vue'
import { toastStore } from '../auth'
import * as api from '../services/ticketApi'
import Pagination from '../components/Pagination.vue'

const items = ref([])
const totalCount = ref(0)
const loading = ref(false)

const page = ref(1)
const pageSize = ref(10)

const filters = ref({
  action: '',
  userName: '',
  details: '',
  startDate: '',
  endDate: ''
})

const fetchData = async () => {
  loading.value = true
  try {
    const r = await api.getSystemLogs(page.value, pageSize.value, filters.value)
    items.value = r.data?.data?.items || []
    totalCount.value = r.data?.data?.totalCount || 0
  } catch {
    toastStore.add('Loglar yüklenemedi', 'error')
  } finally {
    loading.value = false
  }
}

const resetFilters = () => {
  filters.value = {
    action: '',
    userName: '',
    details: '',
    startDate: '',
    endDate: ''
  }
  page.value = 1
  fetchData()
}

watch([page, pageSize], fetchData)
watch(filters, () => {
  page.value = 1
  fetchData()
}, { deep: true })

onMounted(fetchData)
</script>
