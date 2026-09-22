<script setup>
import { ref, onMounted } from 'vue'
import { announcementApi } from '../api/announcementApi'
import { toastStore } from '../auth'

const announcements = ref([])
const loading = ref(true)

const showForm = ref(false)
const form = ref({
  id: null,
  title: '',
  content: '',
  publishedAt: '',
  expiresAt: '',
  isActive: true,
  targetAudience: 'All',
  targetDepartmentId: null
})

const fetchAnnouncements = async () => {
  loading.value = true
  try {
    const res = await announcementApi.getAllAdmin()
    if (res.data.success) {
      announcements.value = res.data.data
    }
  } catch (error) {
    toastStore.error('Duyurular yüklenirken hata oluştu')
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  fetchAnnouncements()
})

const resetForm = () => {
  form.value = {
    id: null,
    title: '',
    content: '',
    publishedAt: new Date().toISOString().slice(0,16),
    expiresAt: new Date(Date.now() + 7*24*60*60*1000).toISOString().slice(0,16),
    isActive: true,
    targetAudience: 'All',
    targetDepartmentId: null
  }
  showForm.value = true
}

const editAnnouncement = (a) => {
  form.value = {
    id: a.id,
    title: a.title,
    content: a.content,
    publishedAt: new Date(a.publishedAt).toISOString().slice(0,16),
    expiresAt: new Date(a.expiresAt).toISOString().slice(0,16),
    isActive: a.isActive,
    targetAudience: a.targetAudience,
    targetDepartmentId: a.targetDepartmentId
  }
  showForm.value = true
}

const saveAnnouncement = async () => {
  try {
    const payload = {
      title: form.value.title,
      content: form.value.content,
      publishedAt: new Date(form.value.publishedAt).toISOString(),
      expiresAt: new Date(form.value.expiresAt).toISOString(),
      isActive: form.value.isActive,
      targetAudience: form.value.targetAudience,
      targetDepartmentId: form.value.targetDepartmentId ? form.value.targetDepartmentId : null
    }

    if (form.value.id) {
      await announcementApi.update(form.value.id, payload)
      toastStore.success('Duyuru başarıyla güncellendi')
    } else {
      await announcementApi.create(payload)
      toastStore.success('Duyuru başarıyla oluşturuldu')
    }
    showForm.value = false
    fetchAnnouncements()
  } catch (error) {
    console.error('Duyuru kaydetme hatası:', error.response?.data || error)
    toastStore.error(error.response?.data?.message || 'Kaydetme hatası')
  }
}

const confirmModal = ref({ show: false, message: '', action: null })
const openConfirm = (message, action) => {
  confirmModal.value = { show: true, message, action }
}
const executeConfirm = () => {
  if (confirmModal.value.action) confirmModal.value.action()
  confirmModal.value.show = false
}
const cancelConfirm = () => {
  confirmModal.value.show = false
}

const deleteAnnouncement = (id) => {
  openConfirm('Duyuruyu silmek istediğinize emin misiniz?', async () => {
    try {
      await announcementApi.delete(id)
      toastStore.success('Duyuru silindi')
      fetchAnnouncements()
    } catch (error) {
      toastStore.error('Silme hatası')
    }
  })
}

const toggleStatus = (id) => {
  openConfirm('Duyuru durumunu (aktif/pasif) değiştirmek istediğinize emin misiniz?', async () => {
    try {
      await announcementApi.toggleStatus(id)
      toastStore.success('Durum güncellendi')
      fetchAnnouncements()
    } catch (error) {
      toastStore.error('Durum güncellenirken hata oluştu')
    }
  })
}
</script>

<template>
  <div class="space-y-6">
    <div class="flex justify-between items-center bg-white dark:bg-slate-900 p-6 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800">
      <div>
        <h2 class="text-xl font-black text-slate-800 dark:text-white">Duyuru Yönetimi</h2>
        <p class="text-sm text-slate-500 mt-1">Sistem üzerindeki tüm duyuruları buradan yönetebilirsiniz.</p>
      </div>
      <button @click="resetForm" class="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white font-bold rounded-xl transition-all">
        + Yeni Duyuru
      </button>
    </div>

    <!-- Form Popup -->
    <div v-if="showForm" class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/50 backdrop-blur-sm">
      <div class="bg-white dark:bg-slate-900 rounded-3xl w-full max-w-2xl max-h-[90vh] overflow-y-auto border border-slate-200 dark:border-slate-800 shadow-2xl">
        <div class="sticky top-0 bg-white/80 dark:bg-slate-900/80 backdrop-blur-md p-6 border-b border-slate-100 dark:border-slate-800 flex justify-between items-center z-10">
          <h3 class="text-xl font-black text-slate-800 dark:text-white">{{ form.id ? 'Duyuruyu Düzenle' : 'Yeni Duyuru Oluştur' }}</h3>
          <button @click="showForm = false" class="text-slate-400 hover:text-red-500 transition-colors">
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path></svg>
          </button>
        </div>

        <form @submit.prevent="saveAnnouncement" class="p-6 space-y-5">
          <div>
            <label class="block text-sm font-bold text-slate-700 dark:text-slate-300 mb-1">Duyuru Konusu</label>
            <input v-model="form.title" type="text" required class="w-full bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-700 rounded-xl px-4 py-3 text-sm focus:ring-2 focus:ring-indigo-500" placeholder="Örn: Sistem Bakımı" maxlength="150" />
          </div>

          <div>
            <label class="block text-sm font-bold text-slate-700 dark:text-slate-300 mb-1">Duyuru İçeriği</label>
            <textarea v-model="form.content" required rows="4" class="w-full bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-700 rounded-xl px-4 py-3 text-sm focus:ring-2 focus:ring-indigo-500" placeholder="Detaylar..." maxlength="1000"></textarea>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-bold text-slate-700 dark:text-slate-300 mb-1">Yayın Tarihi</label>
              <input v-model="form.publishedAt" type="datetime-local" required class="w-full bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-700 rounded-xl px-4 py-3 text-sm" />
            </div>
            <div>
              <label class="block text-sm font-bold text-slate-700 dark:text-slate-300 mb-1">Bitiş Tarihi</label>
              <input v-model="form.expiresAt" type="datetime-local" required class="w-full bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-700 rounded-xl px-4 py-3 text-sm" />
            </div>
          </div>

          <div>
            <label class="block text-sm font-bold text-slate-700 dark:text-slate-300 mb-1">Hedef Kitle</label>
            <select v-model="form.targetAudience" required class="w-full bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-700 rounded-xl px-4 py-3 text-sm">
              <option value="All">Tüm Kullanıcılar</option>
              <option value="Student">Sadece Öğrenciler</option>
              <option value="Academician">Sadece Akademisyenler</option>
              <option value="Staff">Sadece Personel</option>
              <option value="Department">Birim Bazlı (Departman)</option>
            </select>
          </div>

          <div v-if="form.targetAudience === 'Department'">
            <label class="block text-sm font-bold text-slate-700 dark:text-slate-300 mb-1">Birim ID (Test İçin Manuel Giriniz veya Liste Kullanınız)</label>
            <input v-model="form.targetDepartmentId" type="text" placeholder="Birim GUID'si" class="w-full bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-700 rounded-xl px-4 py-3 text-sm" />
          </div>

          <div class="flex items-center gap-3 pt-2">
            <input type="checkbox" v-model="form.isActive" id="activeToggle" class="w-5 h-5 text-indigo-600 rounded" />
            <label for="activeToggle" class="text-sm font-bold text-slate-700 dark:text-slate-300">Aktif Mi?</label>
          </div>

          <div class="flex justify-end gap-3 pt-4 border-t border-slate-100 dark:border-slate-800">
            <button type="button" @click="showForm = false" class="px-5 py-2.5 text-slate-600 dark:text-slate-400 font-bold hover:bg-slate-100 dark:hover:bg-slate-800 rounded-xl transition">İptal</button>
            <button type="submit" class="px-5 py-2.5 bg-indigo-600 hover:bg-indigo-700 text-white font-bold rounded-xl shadow-lg shadow-indigo-500/25 transition">Kaydet</button>
          </div>
        </form>
      </div>
    </div>

    <!-- Confirm Popup -->
    <div v-if="confirmModal.show" class="fixed inset-0 z-[60] flex items-center justify-center p-4 bg-slate-900/50 backdrop-blur-sm">
      <div class="bg-white dark:bg-slate-900 rounded-2xl w-full max-w-sm p-6 border border-slate-200 dark:border-slate-800 shadow-2xl text-center">
        <div class="w-16 h-16 bg-amber-100 dark:bg-amber-900/30 text-amber-500 rounded-full flex items-center justify-center mx-auto mb-4">
          <svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"/></svg>
        </div>
        <h3 class="text-lg font-bold text-slate-800 dark:text-white mb-2">Onay Gerekiyor</h3>
        <p class="text-sm text-slate-500 dark:text-slate-400 mb-6">{{ confirmModal.message }}</p>
        <div class="flex gap-3 justify-center">
          <button @click="cancelConfirm" class="px-5 py-2 text-slate-600 dark:text-slate-300 font-bold bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 rounded-xl transition">İptal</button>
          <button @click="executeConfirm" class="px-5 py-2 text-white font-bold bg-indigo-600 hover:bg-indigo-700 rounded-xl shadow-lg shadow-indigo-500/25 transition">Evet, Onaylıyorum</button>
        </div>
      </div>
    </div>

    <!-- Table -->
    <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 overflow-hidden">
      <div v-if="loading" class="p-8 text-center text-slate-500">Yükleniyor...</div>
      <div v-else-if="announcements.length === 0" class="p-8 text-center text-slate-500">Kayıtlı duyuru bulunamadı.</div>
      <table v-else class="w-full text-left text-sm whitespace-nowrap">
        <thead class="bg-slate-50 dark:bg-slate-800/50 text-slate-500 dark:text-slate-400 font-bold uppercase text-[10px] tracking-wider">
          <tr>
            <th class="px-6 py-4">Konu</th>
            <th class="px-6 py-4">Yayın Tarihi</th>
            <th class="px-6 py-4">Bitiş Tarihi</th>
            <th class="px-6 py-4">Hedef Kitle</th>
            <th class="px-6 py-4">Durum</th>
            <th class="px-6 py-4 text-right">İşlemler</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-100 dark:divide-slate-800/50">
          <tr v-for="a in announcements" :key="a.id" class="hover:bg-slate-50/50 dark:hover:bg-slate-800/20 transition-colors">
            <td class="px-6 py-4 font-bold text-slate-800 dark:text-white">{{ a.title }}</td>
            <td class="px-6 py-4 text-slate-500">{{ new Date(a.publishedAt).toLocaleString() }}</td>
            <td class="px-6 py-4 text-slate-500">{{ new Date(a.expiresAt).toLocaleString() }}</td>
            <td class="px-6 py-4 text-slate-500">{{ a.targetAudience }}</td>
            <td class="px-6 py-4">
              <button @click="toggleStatus(a.id)" :class="a.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-rose-100 text-rose-700'" class="px-3 py-1 rounded-full text-xs font-bold transition hover:opacity-80">
                {{ a.isActive ? 'Aktif' : 'Pasif' }}
              </button>
            </td>
            <td class="px-6 py-4 text-right space-x-2">
              <button @click="editAnnouncement(a)" class="text-indigo-600 hover:text-indigo-800 font-bold text-xs bg-indigo-50 px-3 py-1.5 rounded-lg">Düzenle</button>
              <button @click="deleteAnnouncement(a.id)" class="text-rose-600 hover:text-rose-800 font-bold text-xs bg-rose-50 px-3 py-1.5 rounded-lg">Sil</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
