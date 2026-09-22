<template>
  <div v-if="show" class="fixed inset-0 z-50 flex items-center justify-center overflow-x-hidden overflow-y-auto outline-none focus:outline-none">
    <div class="fixed inset-0 bg-black opacity-50" @click="close"></div>
    <div class="relative w-auto max-w-lg mx-auto my-6 z-50">
      <div class="relative flex flex-col w-full bg-gray-800 border border-gray-700 rounded-lg shadow-lg outline-none focus:outline-none text-white p-6">
        <div class="flex items-start justify-between border-b border-gray-700 pb-4 mb-4">
          <h3 class="text-xl font-semibold">Hatırlatıcı Oluştur</h3>
          <button @click="close" class="p-1 ml-auto bg-transparent border-0 text-gray-400 float-right text-3xl leading-none font-semibold outline-none focus:outline-none hover:text-white">
            <span class="bg-transparent h-6 w-6 text-2xl block outline-none focus:outline-none">×</span>
          </button>
        </div>
        
        <form @submit.prevent="submitReminder" class="space-y-4">
          <div class="flex gap-4">
            <div class="flex-1">
              <label class="block text-sm font-medium text-gray-300 mb-1">Tarih</label>
              <input 
                type="date"
                v-model="reminderDate" 
                required
                class="w-full bg-gray-900 border border-gray-600 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm text-white"
              />
            </div>
            <div class="flex-1">
              <label class="block text-sm font-medium text-gray-300 mb-1">Saat</label>
              <input 
                type="time"
                v-model="reminderTime" 
                required
                class="w-full bg-gray-900 border border-gray-600 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm text-white"
              />
            </div>
          </div>

          <div class="flex items-center justify-end pt-4 border-t border-gray-700 mt-6 gap-3">
            <button
              type="button"
              @click="close"
              class="px-4 py-2 text-sm font-medium text-gray-300 bg-gray-700 hover:bg-gray-600 rounded-md focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-offset-gray-800 focus:ring-gray-500 transition-colors"
            >
              İptal
            </button>
            <button
              type="submit"
              :disabled="loading || !reminderDate || !reminderTime"
              class="px-4 py-2 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 rounded-md disabled:opacity-50 disabled:cursor-not-allowed focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-offset-gray-800 focus:ring-blue-500 transition-colors"
            >
              <span v-if="loading">Oluşturuluyor...</span>
              <span v-else>Kaydet</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { createReminder } from '../services/ticketApi'

const props = defineProps({
  show: Boolean,
  ticketId: String
})

const emit = defineEmits(['close', 'created'])

const reminderDate = ref('')
const reminderTime = ref('')
const loading = ref(false)

const close = () => {
  reminderDate.value = ''
  reminderTime.value = ''
  emit('close')
}

const submitReminder = async () => {
  if (!reminderDate.value || !reminderTime.value) return

  loading.value = true
  try {
    // Convert local date/time to UTC ISO string
    const localDate = new Date(`${reminderDate.value}T${reminderTime.value}:00`)
    const utcDateStr = localDate.toISOString()

    const res = await createReminder(props.ticketId, utcDateStr)
    if (res.data?.success) {
      emit('created')
      close()
    } else {
      alert('Hatırlatıcı oluşturulamadı: ' + res.data?.message)
    }
  } catch (error) {
    console.error('Reminder creation failed', error)
    alert('Hatırlatıcı işlemi sırasında bir hata oluştu.')
  } finally {
    loading.value = false
  }
}
</script>
