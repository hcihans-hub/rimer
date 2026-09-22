<template>
  <div class="space-y-4">
    <!-- Header & Filters -->
    <div class="flex flex-wrap justify-between items-center gap-3">
      <div class="flex flex-wrap items-center gap-3">
        <h3 class="text-lg font-bold text-slate-900 dark:text-white">Birimler</h3>
        <input v-model="searchTerm" @input="handleSearch" type="text" placeholder="Birim ara..."
          class="px-3 py-1.5 rounded-lg border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm outline-none focus:ring-2 focus:ring-indigo-500 w-48"/>
        <label class="flex items-center gap-2 cursor-pointer">
          <input type="checkbox" v-model="onlyActive" @change="fetchData" class="w-4 h-4 text-indigo-600 rounded">
          <span class="text-sm text-slate-700 dark:text-slate-300">Sadece Aktif</span>
        </label>
      </div>
      <button @click="openAddModal"
        class="px-4 py-2 rounded-xl bg-indigo-600 text-white text-xs font-bold hover:bg-indigo-700 transition flex items-center gap-1.5">
        <span>+</span> Birim Ekle
      </button>
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
              <th class="px-4 py-3 text-[10px] font-bold text-slate-400 uppercase border-b border-slate-200 dark:border-slate-700">Birim Adı</th>
              <th class="px-4 py-3 text-[10px] font-bold text-slate-400 uppercase border-b border-slate-200 dark:border-slate-700">Durum</th>
              <th class="px-4 py-3 text-[10px] font-bold text-slate-400 uppercase border-b border-slate-200 dark:border-slate-700">Kullanıcı</th>
              <th class="px-4 py-3 text-[10px] font-bold text-slate-400 uppercase border-b border-slate-200 dark:border-slate-700">Aktif Talep</th>
              <th class="px-4 py-3 text-[10px] font-bold text-slate-400 uppercase border-b border-slate-200 dark:border-slate-700">Oluşturulma</th>
              <th class="px-4 py-3 border-b border-slate-200 dark:border-slate-700"></th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
            <tr v-for="d in items" :key="d.id"
              :class="!d.isActive ? 'opacity-50 grayscale bg-slate-50 dark:bg-slate-800/20' : 'hover:bg-slate-50 dark:hover:bg-slate-800/50'"
              class="transition">
              <td class="px-4 py-3">
                <div class="text-sm font-semibold text-slate-900 dark:text-white">{{ d.name }}</div>
                <div v-if="d.description" class="text-xs text-slate-400 mt-0.5 truncate max-w-[180px]">{{ d.description }}</div>
              </td>

              <td class="px-4 py-3">
                <span :class="!d.isActive ? 'bg-red-100 text-red-600 dark:bg-red-900/30 dark:text-red-400' : 'bg-emerald-100 text-emerald-600 dark:bg-emerald-900/30 dark:text-emerald-400'"
                  class="px-2.5 py-1 rounded-lg text-[10px] font-bold uppercase">
                  {{ d.isActive ? 'Aktif' : 'Pasif' }}
                </span>
              </td>
              <td class="px-4 py-3">
                <div class="flex items-center gap-1.5">
                  <span class="text-sm font-bold text-slate-700 dark:text-slate-200">{{ d.userCount }}</span>
                  <span class="text-[10px] text-slate-400">kullanıcı</span>
                </div>
              </td>
              <td class="px-4 py-3">
                <div class="flex items-center gap-1.5">
                  <span :class="d.activeTicketCount > 0 ? 'text-amber-600 dark:text-amber-400 font-bold' : 'text-slate-500'"
                    class="text-sm">{{ d.activeTicketCount }}</span>
                  <span class="text-[10px] text-slate-400">talep</span>
                </div>
              </td>
              <td class="px-4 py-3 text-xs text-slate-500">{{ new Date(d.createdAt).toLocaleDateString('tr-TR') }}</td>
              <td class="px-4 py-3 text-right">
                <div class="flex items-center justify-end gap-1.5">
                  <button @click="openEdit(d)" title="Düzenle"
                    class="p-1.5 rounded-lg text-slate-400 hover:text-indigo-500 hover:bg-indigo-50 dark:hover:bg-indigo-900/20 transition">
                    <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"/></svg>
                  </button>
                  <button @click="toggle(d)"
                    :class="!d.isActive ? 'text-emerald-500 hover:bg-emerald-50 dark:hover:bg-emerald-900/20' : 'text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20'"
                    class="p-1.5 rounded-lg transition text-[10px] font-bold">
                    {{ !d.isActive ? '▶' : '■' }}
                  </button>
                </div>
              </td>
            </tr>
            <tr v-if="!items.length">
              <td colspan="6" class="px-6 py-12 text-center text-slate-400 text-sm">Birim bulunamadı.</td>
            </tr>
          </tbody>
        </table>
      </div>
      <div class="p-4 border-t border-slate-200 dark:border-slate-800 shrink-0 bg-white dark:bg-slate-900 rounded-b-2xl z-20">
        <Pagination v-model:page="page" v-model:pageSize="pageSize" :totalCount="totalCount" />
      </div>
    </div>

    <!-- Add/Edit Modal -->
    <div v-if="showModal" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4">
      <div class="bg-white dark:bg-slate-900 rounded-3xl border border-slate-200 dark:border-slate-800 p-6 w-full max-w-md shadow-2xl">
        <div class="flex items-center justify-between mb-5">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-2xl bg-indigo-100 dark:bg-indigo-900/30 flex items-center justify-center text-indigo-600 text-xl">🏢</div>
            <h4 class="text-base font-bold text-slate-900 dark:text-white">{{ editingDept ? 'Birim Düzenle' : 'Yeni Birim' }}</h4>
          </div>
          <button @click="closeModal" class="w-8 h-8 rounded-lg bg-slate-100 dark:bg-slate-800 flex items-center justify-center text-slate-500 hover:bg-slate-200 transition text-sm">✕</button>
        </div>

        <div class="space-y-3">
          <div>
            <label class="block text-[10px] font-bold text-slate-500 uppercase mb-1">Birim Adı *</label>
            <input v-model="form.name" placeholder="Birim adını girin"
              class="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm text-slate-700 dark:text-slate-300 outline-none focus:ring-2 focus:ring-indigo-500"/>
          </div>
          <div>
            <label class="block text-[10px] font-bold text-slate-500 uppercase mb-1">Açıklama</label>
            <input v-model="form.description" placeholder="Açıklama (opsiyonel)"
              class="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm text-slate-700 dark:text-slate-300 outline-none focus:ring-2 focus:ring-indigo-500"/>
          </div>

        </div>

        <div class="flex gap-3 mt-5">
          <button @click="closeModal" class="flex-1 px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-400 font-bold text-sm hover:bg-slate-50 dark:hover:bg-slate-800 transition">İptal</button>
          <button @click="submit" :disabled="saving"
            class="flex-1 px-4 py-2.5 rounded-xl bg-indigo-600 text-white font-bold text-sm hover:bg-indigo-700 transition disabled:opacity-50 flex justify-center items-center gap-2">
            <svg v-if="saving" class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/></svg>
            {{ saving ? 'Kaydediliyor…' : editingDept ? 'Güncelle' : 'Oluştur' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Confirm Deactivate Modal -->
    <div v-if="confirmDeactivateDept" class="fixed inset-0 z-[60] flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4">
      <div class="bg-white dark:bg-slate-900 rounded-3xl border border-slate-200 dark:border-slate-800 p-8 w-full max-w-sm shadow-2xl">
        <div class="w-16 h-16 rounded-full bg-red-100 dark:bg-red-900/30 flex items-center justify-center mx-auto mb-4">
          <span class="text-3xl">⚠️</span>
        </div>
        <h3 class="text-xl font-black text-slate-900 dark:text-white mb-3 text-center">Birim Pasife Alınacak</h3>
        <p class="text-sm text-slate-600 dark:text-slate-400 text-center mb-8 leading-relaxed">
          <strong class="text-red-600 dark:text-red-400 text-base block mb-2">{{ confirmDeactivateDept.name }}</strong>
          Bu birimi pasife almak istediğinize emin misiniz?
        </p>
        <div class="flex gap-3">
          <button @click="confirmDeactivateDept = null" class="flex-1 px-4 py-3 rounded-xl border border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-400 font-bold text-sm hover:bg-slate-50 dark:hover:bg-slate-800 transition">İptal</button>
          <button @click="executeDeactivate" class="flex-1 px-4 py-3 rounded-xl bg-red-600 text-white font-bold text-sm hover:bg-red-700 transition">Pasife Al</button>
        </div>
      </div>
    </div>

    <!-- Confirm Activate Modal -->
    <div v-if="confirmActivateDept" class="fixed inset-0 z-[60] flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4">
      <div class="bg-white dark:bg-slate-900 rounded-3xl border border-slate-200 dark:border-slate-800 p-8 w-full max-w-sm shadow-2xl">
        <div class="w-16 h-16 rounded-full bg-emerald-100 dark:bg-emerald-900/30 flex items-center justify-center mx-auto mb-4">
          <span class="text-3xl">✅</span>
        </div>
        <h3 class="text-xl font-black text-slate-900 dark:text-white mb-3 text-center">Birim Aktif Edilecek</h3>
        <p class="text-sm text-slate-600 dark:text-slate-400 text-center mb-8 leading-relaxed">
          <strong class="text-emerald-600 dark:text-emerald-400 text-base block mb-2">{{ confirmActivateDept.name }}</strong>
          Bu birimi aktif etmek istediğinize emin misiniz?
        </p>
        <div class="flex gap-3">
          <button @click="confirmActivateDept = null" class="flex-1 px-4 py-3 rounded-xl border border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-400 font-bold text-sm hover:bg-slate-50 dark:hover:bg-slate-800 transition">İptal</button>
          <button @click="executeActivate" class="flex-1 px-4 py-3 rounded-xl bg-emerald-600 text-white font-bold text-sm hover:bg-emerald-700 transition">Aktif Et</button>
        </div>
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
const saving = ref(false)
const showModal = ref(false)
const editingDept = ref(null)
const onlyActive = ref(true)
const searchTerm = ref('')
const confirmDeactivateDept = ref(null)
const confirmActivateDept = ref(null)

const page = ref(1)
const pageSize = ref(10)

const defaultForm = () => ({ name: '', description: '' })
const form = ref(defaultForm())

const fetchData = async () => {
  loading.value = true
  try {
    const r = await api.getDepartments(page.value, pageSize.value, onlyActive.value, searchTerm.value)
    items.value = r.data?.data?.items || []
    totalCount.value = r.data?.data?.totalCount || 0
  } catch {
    toastStore.add('Veriler yüklenemedi', 'error')
  } finally {
    loading.value = false
  }
}

let searchTimeout = null
const handleSearch = () => {
  clearTimeout(searchTimeout)
  searchTimeout = setTimeout(() => { page.value = 1; fetchData() }, 500)
}

watch([page, pageSize], fetchData)

const openAddModal = () => {
  editingDept.value = null
  form.value = defaultForm()
  showModal.value = true
}

const openEdit = (d) => {
  editingDept.value = d
  form.value = {
    name: d.name,
    description: d.description || ''
  }
  showModal.value = true
}

const closeModal = () => {
  showModal.value = false
  editingDept.value = null
  form.value = defaultForm()
}

const submit = async () => {
  if (!form.value.name?.trim()) {
    toastStore.add('Birim adı zorunludur.', 'error'); return
  }
  saving.value = true
  try {
    if (editingDept.value) {
      await api.updateDepartment(editingDept.value.id, {
        name: form.value.name.trim(),
        description: form.value.description?.trim() || undefined
      })
      toastStore.add('Birim güncellendi', 'success')
    } else {
      await api.createDepartment(form.value.name.trim(), form.value.description?.trim() || '')
      toastStore.add('Birim oluşturuldu', 'success')
    }
    closeModal()
    await fetchData()
  } catch (e) {
    let msg = e.response?.data?.message || 'Hata oluştu'
    if (msg.includes('already exists')) msg = 'Bu isme sahip bir birim zaten sistemde kayıtlı.'
    toastStore.add(msg, 'error')
  } finally {
    saving.value = false
  }
}

const toggle = async (d) => {
  if (!d.isActive) {
    confirmActivateDept.value = d
  } else {
    confirmDeactivateDept.value = d
  }
}

const executeActivate = async () => {
  if (!confirmActivateDept.value) return
  const d = confirmActivateDept.value
  try {
    await api.activateDepartment(d.id)
    toastStore.add('Aktif edildi', 'success')
    confirmActivateDept.value = null
    await fetchData()
  } catch (e) {
    toastStore.add(e.response?.data?.message || 'Hata', 'error')
  }
}

const executeDeactivate = async () => {
  if (!confirmDeactivateDept.value) return
  const d = confirmDeactivateDept.value
  try {
    await api.deactivateDepartment(d.id)
    toastStore.add('Pasif edildi', 'success')
    confirmDeactivateDept.value = null
    await fetchData()
  } catch (e) {
    toastStore.add(e.response?.data?.message || 'Hata', 'error')
  }
}



onMounted(fetchData)
</script>
