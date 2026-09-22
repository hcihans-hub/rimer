<template>
  <div class="space-y-4">
    <!-- Header & Filters -->
    <div class="flex flex-wrap justify-between items-center gap-3">
      <div class="flex flex-wrap items-center gap-3">
        <h3 class="text-lg font-bold text-slate-900 dark:text-white">Kullanıcılar</h3>
        <input v-model="searchTerm" @input="handleSearch" type="text" placeholder="Ad, e-posta ara..."
          class="px-3 py-1.5 rounded-lg border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm outline-none focus:ring-2 focus:ring-indigo-500 w-48"/>
        <select v-model="filterRole" @change="fetchData"
          class="px-3 py-1.5 rounded-lg border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm outline-none focus:ring-2 focus:ring-indigo-500">
          <option value="">Tüm Roller</option>
          <option value="Student">Öğrenci</option>
          <option value="UnitUser">Birim Kullanıcı</option>
          <option value="Staff">Personel</option>
          <option value="Operator">Operatör</option>
          <option value="Admin">Admin</option>
          <option value="Rector">Rektör</option>
        </select>
        <select v-model="filterDept" @change="fetchData"
          class="px-3 py-1.5 rounded-lg border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm outline-none focus:ring-2 focus:ring-indigo-500">
          <option value="">Tüm Birimler</option>
          <option v-for="d in departments" :key="d.id" :value="d.id">{{ d.name }}</option>
        </select>
        <label class="flex items-center gap-2 cursor-pointer">
          <input type="checkbox" v-model="onlyActive" @change="fetchData" class="w-4 h-4 text-indigo-600 rounded">
          <span class="text-sm text-slate-700 dark:text-slate-300">Sadece Aktif</span>
        </label>
      </div>
      <button @click="openAddModal" class="px-4 py-2 rounded-xl bg-indigo-600 text-white text-xs font-bold hover:bg-indigo-700 transition flex items-center gap-1.5">
        <span>+</span> Kullanıcı Ekle
      </button>
    </div>

    <!-- Table -->
    <div class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl shadow-sm flex flex-col" style="height: calc(100vh - 230px);">
      <div v-if="loading" class="p-6 space-y-3">
        <div v-for="i in pageSize" :key="i" class="h-14 bg-slate-100 dark:bg-slate-800 rounded-xl animate-pulse"/>
      </div>
      <div v-else class="flex-1 overflow-y-auto">
        <table class="w-full text-left relative">
          <thead class="sticky top-0 z-10 bg-slate-50 dark:bg-slate-800/90 backdrop-blur">
            <tr>
              <th class="px-4 py-3 text-[10px] font-bold text-slate-400 uppercase border-b border-slate-200 dark:border-slate-700">Ad Soyad</th>
              <th class="px-4 py-3 text-[10px] font-bold text-slate-400 uppercase border-b border-slate-200 dark:border-slate-700">E-posta</th>
              <th class="px-4 py-3 text-[10px] font-bold text-slate-400 uppercase border-b border-slate-200 dark:border-slate-700">Rol</th>
              <th class="px-4 py-3 text-[10px] font-bold text-slate-400 uppercase border-b border-slate-200 dark:border-slate-700">Birim</th>
              <th class="px-4 py-3 text-[10px] font-bold text-slate-400 uppercase border-b border-slate-200 dark:border-slate-700">Durum</th>
              <th class="px-4 py-3 text-[10px] font-bold text-slate-400 uppercase border-b border-slate-200 dark:border-slate-700">Son Giriş</th>
              <th class="px-4 py-3 text-[10px] font-bold text-slate-400 uppercase border-b border-slate-200 dark:border-slate-700">Oluşturulma</th>
              <th class="px-4 py-3 border-b border-slate-200 dark:border-slate-700"></th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
            <tr v-for="u in items" :key="u.id"
              :class="!u.isActive ? 'opacity-50 grayscale bg-slate-50 dark:bg-slate-800/20' : 'hover:bg-slate-50 dark:hover:bg-slate-800/50'"
              class="transition">
              <td class="px-4 py-3">
                <div class="flex items-center gap-2.5">
                  <div class="w-8 h-8 rounded-full bg-gradient-to-br from-indigo-500 to-purple-600 flex items-center justify-center text-white text-xs font-bold shrink-0">
                    {{ (u.fullName || '?')[0].toUpperCase() }}
                  </div>
                  <div>
                    <div class="text-sm font-semibold text-slate-900 dark:text-white leading-tight">{{ u.fullName }}</div>
                    <div v-if="u.personType && u.personType !== 'Other'" class="text-[10px] text-slate-400">{{ u.personType }}</div>
                  </div>
                </div>
              </td>
              <td class="px-4 py-3 text-sm text-slate-500 dark:text-slate-400">{{ u.email }}</td>
              <td class="px-4 py-3">
                <span :class="getRoleBadge(u.role)" class="px-2.5 py-1 rounded-lg text-[10px] font-bold uppercase">{{ getRoleLabel(u.role) }}</span>
              </td>
              <td class="px-4 py-3">
                <span v-if="u.departmentName" class="px-2.5 py-1 rounded-lg text-[10px] font-bold bg-blue-50 dark:bg-blue-900/20 text-blue-700 dark:text-blue-300 border border-blue-100 dark:border-blue-800">{{ u.departmentName }}</span>
                <span v-else class="text-xs text-slate-400">—</span>
              </td>
              <td class="px-4 py-3">
                <span :class="!u.isActive ? 'bg-red-100 text-red-600 dark:bg-red-900/30 dark:text-red-400' : u.isLockedOut ? 'bg-amber-100 text-amber-600 dark:bg-amber-900/30 dark:text-amber-400' : 'bg-emerald-100 text-emerald-600 dark:bg-emerald-900/30 dark:text-emerald-400'"
                  class="px-2.5 py-1 rounded-lg text-[10px] font-bold uppercase">
                  {{ !u.isActive ? 'Pasif' : u.isLockedOut ? 'Kilitli' : 'Aktif' }}
                </span>
              </td>
              <td class="px-4 py-3 text-xs text-slate-500">
                {{ u.lastLoginAt ? new Date(u.lastLoginAt).toLocaleString('tr-TR', { dateStyle: 'short', timeStyle: 'short' }) : '—' }}
              </td>
              <td class="px-4 py-3 text-xs text-slate-500">{{ new Date(u.createdAt).toLocaleDateString('tr-TR') }}</td>
              <td class="px-4 py-3 text-right">
                <div class="flex items-center justify-end gap-1.5">
                  <button @click="openEdit(u)" title="Düzenle"
                    class="p-1.5 rounded-lg text-slate-400 hover:text-indigo-500 hover:bg-indigo-50 dark:hover:bg-indigo-900/20 transition">
                    <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"/></svg>
                  </button>
                  <button @click="toggle(u)" :title="!u.isActive ? 'Aktif Et' : 'Pasif Et'"
                    :class="!u.isActive ? 'text-emerald-500 hover:bg-emerald-50 dark:hover:bg-emerald-900/20' : 'text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20'"
                    class="p-1.5 rounded-lg transition text-[10px] font-bold">
                    {{ !u.isActive ? '▶' : '■' }}
                  </button>
                </div>
              </td>
            </tr>
            <tr v-if="!items.length">
              <td colspan="8" class="px-6 py-12 text-center text-slate-400 text-sm">Kullanıcı bulunamadı.</td>
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
      <div class="bg-white dark:bg-slate-900 rounded-3xl border border-slate-200 dark:border-slate-800 p-6 w-full max-w-lg shadow-2xl">
        <div class="flex items-center justify-between mb-5">
          <h4 class="text-base font-bold text-slate-900 dark:text-white">
            {{ editingUser ? 'Kullanıcı Düzenle' : 'Yeni Kullanıcı' }}
          </h4>
          <button @click="closeModal" class="w-8 h-8 rounded-lg bg-slate-100 dark:bg-slate-800 flex items-center justify-center text-slate-500 hover:bg-slate-200 dark:hover:bg-slate-700 transition text-sm">✕</button>
        </div>

        <div class="space-y-3">
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-[10px] font-bold text-slate-500 uppercase mb-1">Ad</label>
              <input v-model="form.firstName" placeholder="Ad"
                class="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm text-slate-700 dark:text-slate-300 outline-none focus:ring-2 focus:ring-indigo-500"/>
            </div>
            <div>
              <label class="block text-[10px] font-bold text-slate-500 uppercase mb-1">Soyad</label>
              <input v-model="form.lastName" placeholder="Soyad"
                class="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm text-slate-700 dark:text-slate-300 outline-none focus:ring-2 focus:ring-indigo-500"/>
            </div>
            <div class="col-span-2 relative">
              <label class="block text-[10px] font-bold text-slate-500 uppercase mb-1">E-posta</label>
              <input v-model="form.email" type="email" placeholder="E-posta"
                class="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm text-slate-700 dark:text-slate-300 outline-none focus:ring-2 focus:ring-indigo-500"/>
              
              <!-- Şifre Yenileme Bağlantısı Butonu - hem yeni hem düzenleme modunda -->
              <div v-if="form.email" class="mt-3">
                <button @click="openResetConfirm" type="button"
                  class="w-full px-4 py-2.5 rounded-xl border border-blue-200 dark:border-blue-800 text-blue-600 dark:text-blue-400 font-bold text-xs hover:bg-blue-50 dark:hover:bg-blue-900/20 transition flex items-center justify-center gap-2">
                  <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"/></svg>
                  <span>Şifre Yenileme Bağlantısı Gönder</span>
                </button>
              </div>
            </div>
            <template v-if="!editingUser">
              <div>
                <label class="block text-[10px] font-bold text-slate-500 uppercase mb-1">Şifre</label>
                <input v-model="form.password" type="password" placeholder="Şifre"
                  class="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm text-slate-700 dark:text-slate-300 outline-none focus:ring-2 focus:ring-indigo-500"/>
              </div>
              <div>
                <label class="block text-[10px] font-bold text-slate-500 uppercase mb-1">Şifre Tekrar</label>
                <input v-model="form.confirmPassword" type="password" placeholder="Şifre Tekrar"
                  class="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm text-slate-700 dark:text-slate-300 outline-none focus:ring-2 focus:ring-indigo-500"/>
              </div>
            </template>
          </div>

          <!-- Department & Role -->
          <div>
            <label class="block text-[10px] font-bold text-slate-500 uppercase mb-1">Birim Ataması</label>
            <select v-model="form.departmentId"
              class="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm text-slate-700 dark:text-slate-300 outline-none focus:ring-2 focus:ring-indigo-500">
              <option :value="null">— Birim Yok —</option>
              <option v-for="d in departments" :key="d.id" :value="d.id">{{ d.name }}</option>
            </select>
            <p v-if="editingUser" class="text-[10px] text-slate-400 mt-1">Birim değişikliği eski talepleri etkilemez.</p>
          </div>

          <div>
            <label class="block text-[10px] font-bold text-slate-500 uppercase mb-1">{{ editingUser ? 'Rol' : 'Rol Seçimi' }}</label>
            <select v-model="form.role"
              class="w-full px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm text-slate-700 dark:text-slate-300 outline-none focus:ring-2 focus:ring-indigo-500">
              <option value="" disabled>— Rol Seçin —</option>
              <option value="Student">Öğrenci</option>
              <option value="UnitUser">Birim Kullanıcı</option>
              <option value="Staff">Personel</option>
              <option value="Operator">🔀 Operatör</option>
              <option value="Admin">Admin</option>
              <option value="Rector">Rektör</option>
            </select>
          </div>
        </div>

        <div class="flex flex-col sm:flex-row gap-3 mt-5">

          <button @click="closeModal" class="flex-1 px-4 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-400 font-bold text-sm hover:bg-slate-50 dark:hover:bg-slate-800 transition">İptal</button>
          <button @click="submit" :disabled="saving"
            class="flex-1 px-4 py-2.5 rounded-xl bg-indigo-600 text-white font-bold text-sm hover:bg-indigo-700 transition disabled:opacity-50 flex justify-center items-center gap-2">
            <svg v-if="saving" class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/></svg>
            {{ saving ? 'Kaydediliyor…' : 'Kaydet' }}
          </button>
        </div>
      </div>
    </div>
  </div>

  <!-- Reset Password Confirm Modal -->
  <div v-if="showResetConfirm" class="fixed inset-0 z-[60] flex items-center justify-center p-4">
    <!-- Backdrop with blur -->
    <div class="absolute inset-0 bg-slate-900/40 backdrop-blur-sm" @click="showResetConfirm = false"></div>
    
    <!-- Modal Content -->
    <div class="relative bg-white dark:bg-slate-900 w-full max-w-md rounded-3xl shadow-2xl overflow-hidden border border-slate-100 dark:border-slate-800 transform transition-all">
      <div class="p-8">
        <div class="w-14 h-14 rounded-full bg-blue-100 dark:bg-blue-900/30 flex items-center justify-center mb-6 mx-auto">
          <span class="text-3xl">📧</span>
        </div>
        
        <h3 class="text-xl font-bold text-slate-800 dark:text-white text-center mb-3">Şifre Yenileme Bağlantısı</h3>
        
        <p class="text-sm text-slate-500 dark:text-slate-400 text-center mb-8 leading-relaxed">
          <strong class="text-slate-700 dark:text-slate-300">{{ form.email }}</strong> adresine bir şifre yenileme bağlantısı gönderilecektir. Devam etmek istediğinize emin misiniz?
        </p>

        <div class="flex gap-4">
          <button @click="showResetConfirm = false" :disabled="sendingResetLink"
            class="flex-1 px-5 py-3 rounded-xl border border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-400 font-bold text-sm hover:bg-slate-50 dark:hover:bg-slate-800 transition disabled:opacity-50">
            İptal
          </button>
          <button @click="confirmResetLink" :disabled="sendingResetLink"
            class="flex-1 px-5 py-3 rounded-xl bg-blue-600 text-white font-bold text-sm hover:bg-blue-700 transition shadow-lg shadow-blue-500/30 disabled:opacity-50 flex justify-center items-center gap-2">
            <svg v-if="sendingResetLink" class="animate-spin h-5 w-5 text-white" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/></svg>
            {{ sendingResetLink ? 'Gönderiliyor…' : 'Evet, Gönder' }}
          </button>
        </div>
      </div>
    </div>
  </div>

  <!-- Confirm Deactivate Modal -->
    <div v-if="confirmDeactivateUser" class="fixed inset-0 z-[60] flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4">
      <div class="bg-white dark:bg-slate-900 rounded-3xl border border-slate-200 dark:border-slate-800 p-8 w-full max-w-sm shadow-2xl">
        <div class="w-16 h-16 rounded-full bg-red-100 dark:bg-red-900/30 flex items-center justify-center mx-auto mb-4">
          <span class="text-3xl">⚠️</span>
        </div>
        <h3 class="text-xl font-black text-slate-900 dark:text-white mb-3 text-center">Kullanıcı Pasife Alınacak</h3>
        <p class="text-sm text-slate-600 dark:text-slate-400 text-center mb-8 leading-relaxed">
          <strong class="text-red-600 dark:text-red-400 text-base block mb-2">{{ confirmDeactivateUser.fullName }}</strong>
          Bu kullanıcıyı pasife almak istediğinize emin misiniz?
        </p>
        <div class="flex gap-3">
          <button @click="confirmDeactivateUser = null" class="flex-1 px-4 py-3 rounded-xl border border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-400 font-bold text-sm hover:bg-slate-50 dark:hover:bg-slate-800 transition">İptal</button>
          <button @click="executeDeactivate" class="flex-1 px-4 py-3 rounded-xl bg-red-600 text-white font-bold text-sm hover:bg-red-700 transition">Pasife Al</button>
        </div>
      </div>
    </div>

    <!-- Confirm Activate Modal -->
    <div v-if="confirmActivateUser" class="fixed inset-0 z-[60] flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4">
      <div class="bg-white dark:bg-slate-900 rounded-3xl border border-slate-200 dark:border-slate-800 p-8 w-full max-w-sm shadow-2xl">
        <div class="w-16 h-16 rounded-full bg-emerald-100 dark:bg-emerald-900/30 flex items-center justify-center mx-auto mb-4">
          <span class="text-3xl">✅</span>
        </div>
        <h3 class="text-xl font-black text-slate-900 dark:text-white mb-3 text-center">Kullanıcı Aktif Edilecek</h3>
        <p class="text-sm text-slate-600 dark:text-slate-400 text-center mb-8 leading-relaxed">
          <strong class="text-emerald-600 dark:text-emerald-400 text-base block mb-2">{{ confirmActivateUser.fullName }}</strong>
          Bu kullanıcıyı aktif etmek istediğinize emin misiniz?
        </p>
        <div class="flex gap-3">
          <button @click="confirmActivateUser = null" class="flex-1 px-4 py-3 rounded-xl border border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-400 font-bold text-sm hover:bg-slate-50 dark:hover:bg-slate-800 transition">İptal</button>
          <button @click="executeActivate" class="flex-1 px-4 py-3 rounded-xl bg-emerald-600 text-white font-bold text-sm hover:bg-emerald-700 transition">Aktif Et</button>
        </div>
      </div>
    </div>

</template>

<script setup>
import { ref, onMounted, watch } from 'vue'
import { toastStore } from '../auth'
import { i18n } from '../lang'
import * as api from '../services/ticketApi'
import Pagination from '../components/Pagination.vue'

const items = ref([])
const departments = ref([])
const totalCount = ref(0)
const loading = ref(false)
const saving = ref(false)
const sendingResetLink = ref(false)
const showModal = ref(false)
const showResetConfirm = ref(false)
const confirmDeactivateUser = ref(null)
const confirmActivateUser = ref(null)
const editingUser = ref(null)
const onlyActive = ref(true)
const searchTerm = ref('')
const filterRole = ref('')
const filterDept = ref('')

const page = ref(1)
const pageSize = ref(10)

const defaultForm = () => ({ firstName: '', lastName: '', email: '', password: '', confirmPassword: '', departmentId: null, role: '' })
const form = ref(defaultForm())

const fetchData = async () => {
  loading.value = true
  try {
    const r = await api.getAllUsers(page.value, pageSize.value, onlyActive.value, searchTerm.value, filterRole.value, filterDept.value)
    items.value = r.data?.data?.items || []
    totalCount.value = r.data?.data?.totalCount || 0
  } catch {
    toastStore.add('Veriler yüklenemedi', 'error')
  } finally {
    loading.value = false
  }
}

const fetchDepartments = async () => {
  try {
    const r = await api.getDepartments(1, 200, true)
    departments.value = r.data?.data?.items || []
  } catch {}
}

let searchTimeout = null
const handleSearch = () => {
  clearTimeout(searchTimeout)
  searchTimeout = setTimeout(() => { page.value = 1; fetchData() }, 500)
}

watch([page, pageSize], fetchData)

const openAddModal = () => {
  editingUser.value = null
  form.value = defaultForm()
  showModal.value = true
}

const openEdit = (u) => {
  editingUser.value = u
  form.value = {
    firstName: u.firstName || '',
    lastName: u.lastName || '',
    email: u.email,
    password: '',
    confirmPassword: '',
    departmentId: u.departmentId || null,
    role: u.role || ''
  }
  showModal.value = true
}

const closeModal = () => {
  showModal.value = false
  editingUser.value = null
  form.value = defaultForm()
}

const openResetConfirm = () => {
  if (form.value.email) {
    showResetConfirm.value = true
  } else {
    toastStore.add('Lütfen önce bir e-posta adresi girin.', 'error')
  }
}

const confirmResetLink = async () => {
  sendingResetLink.value = true
  try {
    if (editingUser.value) {
      // Düzenleme modunda: kullanıcı ID üzerinden gönder
      await api.sendPasswordResetLink(editingUser.value.id)
    } else {
      // Yeni kullanıcı modunda: e-posta adresi üzerinden gönder
      await api.forgotPassword(form.value.email)
    }
    toastStore.add('Şifre yenileme bağlantısı gönderildi.', 'success')
    showResetConfirm.value = false
  } catch (e) {
    toastStore.add(e.response?.data?.message || 'Bağlantı gönderilemedi.', 'error')
  } finally {
    sendingResetLink.value = false
  }
}

const submit = async () => {
  if ((form.value.role === 'UnitUser' || form.value.role === 'Staff') && !form.value.departmentId) {
    toastStore.add('Birim yetkilisi veya Personel için birim seçimi zorunludur.', 'error')
    return
  }

  if (editingUser.value) {
    await submitEdit()
  } else {
    await submitAdd()
  }
}

const submitAdd = async () => {
  if (!form.value.firstName || !form.value.lastName || !form.value.email || !form.value.password) {
    toastStore.add('Ad, soyad, e-posta ve şifre zorunludur.', 'error'); return
  }
  if (form.value.password !== form.value.confirmPassword) {
    toastStore.add('Şifreler eşleşmiyor.', 'error'); return
  }
  saving.value = true
  try {
    if (!form.value.role) {
      toastStore.add('Lütfen bir rol seçin.', 'error'); saving.value = false; return
    }
    await api.createUser({
      firstName: form.value.firstName,
      lastName: form.value.lastName,
      email: form.value.email,
      password: form.value.password,
      role: form.value.role,
      departmentId: form.value.departmentId
    })

    toastStore.add('Kullanıcı oluşturuldu', 'success')
    closeModal()
    await fetchData()
  } catch (e) {
    let msg = e.response?.data?.message || 'Hata oluştu'
    if (msg.includes('Passwords must') || msg.includes('Password')) msg = 'Şifre gereksinimlerini karşılamıyor (min 6 karakter, büyük/küçük harf, rakam, özel karakter).'
    else if (msg.includes('already taken') || msg.includes('already')) msg = 'Bu e-posta adresi zaten kullanımda.'
    toastStore.add(msg, 'error')
  } finally {
    saving.value = false
  }
}

const submitEdit = async () => {
  if (!form.value.firstName && !form.value.lastName && !form.value.email) {
    toastStore.add('En az bir alan doldurulmalıdır.', 'error'); return
  }
  saving.value = true
  try {
    // Update profile + department
    await api.updateUser(editingUser.value.id, {
      firstName: form.value.firstName || undefined,
      lastName: form.value.lastName || undefined,
      email: form.value.email || undefined,
      departmentId: form.value.departmentId
    })

    // If role changed, also change role
    if (form.value.role) {
      await api.updateUserRole(editingUser.value.id, form.value.role)
    }

    toastStore.add('Kullanıcı güncellendi', 'success')
    closeModal()
    await fetchData()
  } catch (e) {
    toastStore.add(e.response?.data?.message || 'Güncelleme hatası', 'error')
  } finally {
    saving.value = false
  }
}

const toggle = async (u) => {
  if (!u.isActive) {
    confirmActivateUser.value = u
  } else {
    confirmDeactivateUser.value = u
  }
}

const executeActivate = async () => {
  if (!confirmActivateUser.value) return
  const u = confirmActivateUser.value
  try {
    await api.activateUser(u.id)
    toastStore.add('Aktif edildi', 'success')
    confirmActivateUser.value = null
    await fetchData()
  } catch (e) {
    toastStore.add(e.response?.data?.message || 'Hata', 'error')
  }
}

const executeDeactivate = async () => {
  if (!confirmDeactivateUser.value) return
  const u = confirmDeactivateUser.value
  try {
    await api.deactivateUser(u.id)
    toastStore.add('Pasif edildi', 'success')
    confirmDeactivateUser.value = null
    await fetchData()
  } catch (e) {
    toastStore.add(e.response?.data?.message || 'Hata', 'error')
  }
}

const getRoleLabel = (role) => {
  const map = { Student: 'Öğrenci', UnitUser: 'Birim', Staff: 'Personel', Operator: 'Operatör', Admin: 'Admin', Rector: 'Rektör' }
  return map[role] || role
}

const getRoleBadge = (role) => {
  const map = {
    Student:  'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300',
    UnitUser: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300',
    Staff:    'bg-violet-100 text-violet-700 dark:bg-violet-900/30 dark:text-violet-300',
    Operator: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-300',
    Admin:    'bg-rose-100 text-rose-700 dark:bg-rose-900/30 dark:text-rose-300',
    Rector:   'bg-teal-100 text-teal-700 dark:bg-teal-900/30 dark:text-teal-300',
  }
  return map[role] || 'bg-slate-100 text-slate-500'
}

onMounted(() => {
  fetchData()
  fetchDepartments()
})
</script>
