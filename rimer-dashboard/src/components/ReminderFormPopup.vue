<script setup>
import { ref } from 'vue'
import * as api from '../services/ticketApi'
import { toastStore } from '../auth'

const props = defineProps({
  ticketId: String,
  show: Boolean
})
const emit = defineEmits(['close', 'created'])

const selectedType = ref('1d') // varsayılan 1 gün
const customDate = ref('')
const note = ref('')
const saving = ref(false)

const create = async () => {
  saving.value = true
  let reminderAt = new Date()
  
  if (selectedType.value === '1h') reminderAt.setHours(reminderAt.getHours() + 1)
  else if (selectedType.value === '1d') reminderAt.setDate(reminderAt.getDate() + 1)
  else if (selectedType.value === '3d') reminderAt.setDate(reminderAt.getDate() + 3)
  else if (selectedType.value === '1w') reminderAt.setDate(reminderAt.getDate() + 7)
  else if (selectedType.value === 'custom') {
    if (!customDate.value) {
      toastStore.add('Lütfen tarih seçin', 'error')
      saving.value = false
      return
    }
    reminderAt = new Date(customDate.value)
    
    // Geçmiş tarih kontrolü
    if (reminderAt <= new Date()) {
      toastStore.add('Hatırlatıcı zamanı geçmiş olamaz.', 'error')
      saving.value = false
      return
    }

    const maxDate = new Date()
    maxDate.setDate(maxDate.getDate() + 7)
    if (reminderAt > maxDate) {
      toastStore.add('En fazla 1 hafta sonraya hatırlatıcı kurabilirsiniz.', 'error')
      saving.value = false
      return
    }
  }

  try {
    await api.createReminder(props.ticketId, reminderAt.toISOString(), note.value)
    toastStore.add('Hatırlatıcı eklendi', 'success')
    note.value = ''
    emit('created')
    emit('close')
  } catch (e) {
    toastStore.add('Hatırlatıcı eklenemedi', 'error')
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <Transition name="fade">
    <div v-if="show" class="fixed inset-0 bg-black/50 backdrop-blur-sm z-[60] flex items-center justify-center p-4">
      <div class="bg-white dark:bg-slate-900 rounded-2xl w-full max-w-sm shadow-2xl overflow-hidden border border-slate-200 dark:border-slate-800">
        <div class="px-5 py-4 border-b border-slate-200 dark:border-slate-800 flex justify-between items-center bg-slate-50 dark:bg-slate-800/50">
          <h3 class="font-bold text-slate-800 dark:text-slate-200 flex items-center gap-2">
            <span class="text-indigo-500">ℹ️</span> Hatırlatıcı Ekle
          </h3>
          <button @click="emit('close')" class="text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 transition text-xl font-bold">×</button>
        </div>
        <div class="p-5 space-y-4">
          <div>
            <label class="text-xs font-bold text-slate-500 uppercase tracking-wider mb-2 block">Süre Seçin (Maks. 1 Hafta)</label>
            <select v-model="selectedType" class="w-full px-4 py-3 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm focus:ring-2 focus:ring-indigo-500 outline-none text-slate-700 dark:text-slate-300">
              <option value="1h">1 Saat Sonra</option>
              <option value="1d">1 Gün Sonra</option>
              <option value="3d">3 Gün Sonra</option>
              <option value="1w">1 Hafta Sonra</option>
              <option value="custom">Özel Tarih & Saat</option>
            </select>
          </div>
          
          <div v-if="selectedType === 'custom'">
            <label class="text-xs font-bold text-slate-500 uppercase tracking-wider mb-2 block">Tarih & Saat</label>
            <input type="datetime-local" v-model="customDate" class="w-full px-4 py-3 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm focus:ring-2 focus:ring-indigo-500 outline-none text-slate-700 dark:text-slate-300" />
          </div>

          <div>
            <label class="text-xs font-bold text-slate-500 uppercase tracking-wider mb-2 block">Not (İsteğe Bağlı)</label>
            <textarea v-model="note" maxlength="250" rows="3" placeholder="Hatırlatıcı için kısa bir not ekleyebilirsiniz..." class="w-full px-4 py-3 rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm focus:ring-2 focus:ring-indigo-500 outline-none text-slate-700 dark:text-slate-300 resize-none"></textarea>
            <div class="text-right text-[10px] text-slate-400 mt-1">{{ note.length }}/250</div>
          </div>

          <button @click="create" :disabled="saving" class="w-full py-3 mt-2 rounded-xl bg-indigo-600 hover:bg-indigo-700 text-white font-bold text-sm transition disabled:opacity-50">
            {{ saving ? 'Ekleniyor...' : 'Hatırlatıcı Ekle' }}
          </button>
        </div>
      </div>
    </div>
  </Transition>
</template>

<style scoped>
.fade-enter-active, .fade-leave-active { transition: opacity 0.2s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
</style>
