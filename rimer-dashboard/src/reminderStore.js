import { ref, watch } from 'vue'
import * as api from './services/ticketApi'
import { auth } from './auth'

export const reminderStore = {
  activeReminders: ref([]),
  loading: ref(false),
  fetchInterval: null,

  async load() {
    if (!auth.isLoggedIn) return
    this.loading.value = true
    try {
      const res = await api.getReminders()
      this.activeReminders.value = res.data?.data || []
    } catch (e) {
      // ignore
    } finally {
      this.loading.value = false
    }
  },

  init() {
    if (auth.isLoggedIn) this.load()
    this.fetchInterval = setInterval(() => {
      if (auth.isLoggedIn) this.load()
    }, 60000) // Her dakika güncelle
    
    watch(() => auth.isLoggedIn, (newVal) => {
      if (newVal) this.load()
      else this.activeReminders.value = []
    })
  },

  destroy() {
    clearInterval(this.fetchInterval)
  }
}
