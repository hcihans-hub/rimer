<script setup>
import { ref, onMounted, onUnmounted, watch } from 'vue'
import * as api from '../services/ticketApi'
import { auth, toastStore } from '../auth'
import { reminderStore } from '../reminderStore'

const triggeredReminder = ref(null)
let pollInterval = null

const checkTriggers = () => {
  if (triggeredReminder.value) return // zaten gösteriliyor
  
  const now = new Date()
  for (const rem of reminderStore.activeReminders.value) {
    const remTime = new Date(rem.reminderAt)
    if (remTime <= now && !rem.isDismissed) {
      triggeredReminder.value = rem
      break
    }
  }
}

const dismiss = async () => {
  if (!triggeredReminder.value) return
  try {
    await api.dismissReminder(triggeredReminder.value.id)
    triggeredReminder.value = null
    await reminderStore.load()
  } catch (e) {
    toastStore.add('Kapatma işlemi başarısız', 'error')
  }
}

const snooze = async (hours) => {
  if (!triggeredReminder.value) return
  const snoozeUntil = new Date()
  snoozeUntil.setHours(snoozeUntil.getHours() + hours)
  
  try {
    await api.snoozeReminder(triggeredReminder.value.id, snoozeUntil.toISOString())
    triggeredReminder.value = null
    await reminderStore.load()
  } catch (e) {
    toastStore.add('Erteleme başarısız', 'error')
  }
}

onMounted(() => {
  reminderStore.init()
  pollInterval = setInterval(() => {
    if (auth.isLoggedIn) checkTriggers()
  }, 10000) // 10 saniyede bir saati kontrol et
})

onUnmounted(() => {
  clearInterval(pollInterval)
  reminderStore.destroy()
})
</script>

<template>
  <Transition name="bounce">
    <div v-if="triggeredReminder" class="fixed top-20 right-6 z-50 w-full max-w-sm bg-white dark:bg-slate-900 rounded-2xl shadow-2xl border border-indigo-200 dark:border-indigo-800/50 overflow-hidden">
      <div class="bg-indigo-600 px-4 py-3 flex items-center justify-between">
        <div class="flex items-center gap-2">
          <span class="text-xl">⏰</span>
          <span class="text-white font-bold text-sm uppercase tracking-wider">Hatırlatma</span>
        </div>
        <button @click="dismiss" class="text-white/80 hover:text-white transition">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path></svg>
        </button>
      </div>
      <div class="p-5">
        <p class="text-[11px] font-black text-slate-400 uppercase mb-1">Talep No: #{{ triggeredReminder.ticketReferenceNo }}</p>
        <p class="text-sm font-bold text-slate-800 dark:text-slate-200 mb-3"><span class="italic text-indigo-600 dark:text-indigo-400 font-black">"{{ triggeredReminder.ticketTitle }}"</span> konulu talebiniz için hatırlatma mesajınızdır.</p>
        
        <div v-if="triggeredReminder.note" class="mb-4 p-3 bg-amber-50 dark:bg-amber-900/10 border-l-4 border-amber-500 rounded-r-xl">
          <p class="text-xs font-semibold text-amber-800 dark:text-amber-400">📝 Notunuz:</p>
          <p class="text-sm text-slate-700 dark:text-slate-300 mt-1 italic break-words">{{ triggeredReminder.note }}</p>
        </div>

        <div class="grid grid-cols-2 gap-2">
          <button @click="dismiss" class="px-4 py-2 bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300 rounded-xl text-xs font-bold transition">
            Kapat
          </button>
          <div class="relative group">
            <button class="w-full px-4 py-2 bg-indigo-50 dark:bg-indigo-900/20 hover:bg-indigo-100 dark:hover:bg-indigo-900/40 text-indigo-600 dark:text-indigo-400 rounded-xl text-xs font-bold transition border border-indigo-200 dark:border-indigo-800/50">
              Ertele ▾
            </button>
            <div class="absolute bottom-full left-0 w-full mb-1 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl shadow-lg opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all z-10 py-1 overflow-hidden">
              <button @click="snooze(1)" class="w-full text-left px-4 py-2 text-xs font-medium hover:bg-slate-50 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300">1 Saat</button>
              <button @click="snooze(3)" class="w-full text-left px-4 py-2 text-xs font-medium hover:bg-slate-50 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300">3 Saat</button>
              <button @click="snooze(24)" class="w-full text-left px-4 py-2 text-xs font-medium hover:bg-slate-50 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300">1 Gün</button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </Transition>
</template>

<style scoped>
.bounce-enter-active {
  animation: bounce-in 0.5s;
}
.bounce-leave-active {
  animation: bounce-in 0.5s reverse;
}
@keyframes bounce-in {
  0% { transform: scale(0.9) translateY(-20px); opacity: 0; }
  50% { transform: scale(1.02); }
  100% { transform: scale(1) translateY(0); opacity: 1; }
}
</style>
