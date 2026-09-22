<template>
  <div v-if="show" class="fixed inset-0 z-[80] flex items-center justify-center p-4">
    <div class="fixed inset-0 bg-black/60 backdrop-blur-sm" @click="close"></div>
    <div class="relative w-full max-w-lg bg-white dark:bg-slate-900 rounded-3xl shadow-2xl border border-slate-200 dark:border-slate-700 overflow-hidden">

      <!-- Header -->
      <div class="px-6 py-5 border-b border-slate-100 dark:border-slate-800 flex items-center justify-between">
        <div>
          <h3 class="text-base font-black text-slate-900 dark:text-white">🔄 Talebi Yönlendir</h3>
          <p class="text-[11px] text-slate-500 mt-0.5">Talep başka bir birime atanacak ve mevcut birimden düşecektir.</p>
        </div>
        <button @click="close" class="w-8 h-8 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-400 hover:text-slate-600 transition flex items-center justify-center text-lg">✕</button>
      </div>

      <!-- Body -->
      <form @submit.prevent="submitTransfer" class="px-6 py-5 space-y-4">
        <!-- Birim Seç -->
        <div>
          <label class="block text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2">Hedef Birim</label>
          <!-- Arama Kutusu -->
          <div class="mb-3 relative">
            <span class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none text-slate-400">
              🔍
            </span>
            <input
              type="text"
              v-model="searchQuery"
              placeholder="Birim ara..."
              class="w-full bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl py-2 pl-9 pr-4 text-sm text-slate-900 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500/50"
            />
          </div>
          <!-- Yükleniyor -->
          <div v-if="loadingDepts" class="flex items-center gap-2 py-3 text-slate-400 text-sm">
            <span class="animate-spin">⏳</span> Birimler yükleniyor...
          </div>
          <!-- Boş uyarı -->
          <div v-else-if="activeDepartments.length === 0" class="py-3 text-sm text-amber-600 dark:text-amber-400 bg-amber-50 dark:bg-amber-900/10 rounded-xl px-4 border border-amber-200 dark:border-amber-800">
            <span v-if="searchQuery">⚠️ Aradığınız kriterlere uygun birim bulunamadı.</span>
            <span v-else>⚠️ Yönlendirilebilecek başka birim bulunamadı.</span>
          </div>
          <!-- Seçim listesi -->
          <div v-else class="space-y-1.5 max-h-52 overflow-y-auto rounded-xl border border-slate-200 dark:border-slate-700 divide-y divide-slate-100 dark:divide-slate-800">
            <label
              v-for="dept in activeDepartments"
              :key="dept.id"
              class="flex items-center gap-3 px-4 py-3 cursor-pointer transition-colors"
              :class="toDepartmentId === dept.id
                ? 'bg-indigo-50 dark:bg-indigo-900/20'
                : 'hover:bg-slate-50 dark:hover:bg-slate-800/50'"
            >
              <input
                type="radio"
                :value="dept.id"
                v-model="toDepartmentId"
                class="w-4 h-4 text-indigo-600 border-slate-300 focus:ring-indigo-500"
              />
              <span class="text-sm font-semibold text-slate-800 dark:text-slate-200">{{ dept.name }}</span>
              <span v-if="toDepartmentId === dept.id" class="ml-auto text-[10px] font-black text-indigo-600 dark:text-indigo-400 uppercase">Seçildi</span>
            </label>
          </div>
        </div>

        <!-- Not -->
        <div>
          <label class="block text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2">Yönlendirme Notu <span class="text-slate-300 normal-case font-normal">(isteğe bağlı)</span></label>
          <textarea
            v-model="note"
            rows="3"
            placeholder="Yönlendirme sebebini veya iletilmesi gereken bilgileri yazın..."
            class="w-full bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-2xl p-4 text-slate-900 dark:text-white text-sm leading-relaxed outline-none focus:ring-4 focus:ring-indigo-500/20 resize-none transition-all"
          ></textarea>
        </div>

        <!-- Footer -->
        <div class="flex justify-end gap-3 pt-2 border-t border-slate-100 dark:border-slate-800">
          <button
            type="button"
            @click="close"
            class="px-5 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-300 text-sm font-bold hover:bg-slate-50 dark:hover:bg-slate-800 transition"
          >
            İptal
          </button>
          <button
            type="submit"
            :disabled="loading || !toDepartmentId || loadingDepts"
            class="px-6 py-2.5 rounded-xl bg-amber-500 hover:bg-amber-600 text-white text-sm font-black shadow-lg shadow-amber-500/20 disabled:opacity-50 transition-all active:scale-95 flex items-center gap-2"
          >
            <span v-if="loading" class="animate-spin">⏳</span>
            {{ loading ? 'Yönlendiriliyor…' : 'Yönlendir' }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { getDepartments, transferTicket } from '../services/ticketApi'
import { toastStore } from '../auth'

const props = defineProps({
  show: Boolean,
  ticketId: String,
  currentDepartmentId: String
})

const emit = defineEmits(['close', 'transferred'])

const toDepartmentId = ref('')
const note = ref('')
const loading = ref(false)
const loadingDepts = ref(false)
const departments = ref([])
const searchQuery = ref('')

// Mevcut birimi ve arama metnini filtrele
const activeDepartments = computed(() => {
  const query = searchQuery.value.toLowerCase().trim()
  return departments.value.filter(d => 
    d.id !== props.currentDepartmentId &&
    (!query || d.name.toLowerCase().includes(query))
  )
})

const fetchDepartments = async () => {
  loadingDepts.value = true
  departments.value = []
  try {
    const res = await getDepartments(1, 100, true)
    if (res.data?.success) {
      departments.value = res.data.data.items || []
    }
  } catch (e) {
    toastStore.add('Birimler yüklenemedi', 'error')
  } finally {
    loadingDepts.value = false
  }
}

// Modal her açıldığında birimleri yeniden yükle
watch(() => props.show, (val) => {
  if (val) {
    toDepartmentId.value = ''
    note.value = ''
    searchQuery.value = ''
    fetchDepartments()
  }
})

const close = () => {
  toDepartmentId.value = ''
  note.value = ''
  searchQuery.value = ''
  emit('close')
}

const submitTransfer = async () => {
  if (!toDepartmentId.value) return
  loading.value = true
  try {
    const res = await transferTicket(props.ticketId, toDepartmentId.value, note.value)
    if (res.data?.success) {
      emit('transferred')
      close()
    } else {
      toastStore.add('Transfer başarısız: ' + (res.data?.message || 'Bilinmeyen hata'), 'error')
    }
  } catch (e) {
    toastStore.add(e.response?.data?.message || 'Transfer sırasında hata oluştu', 'error')
  } finally {
    loading.value = false
  }
}
</script>
