<script setup>
import { computed } from 'vue'
import { reminderStore } from '../reminderStore'

const activeCount = computed(() => {
  const now = new Date()
  return reminderStore.activeReminders.value.filter(r => !r.isDismissed && new Date(r.reminderAt) > now).length
})

const totalCount = computed(() => {
  return reminderStore.activeReminders.value.filter(r => !r.isDismissed).length
})
</script>

<template>
  <div class="relative flex items-center justify-center p-2.5 rounded-xl bg-white dark:bg-slate-900 transition-all cursor-pointer group tooltip-trigger hover:bg-slate-100 dark:hover:bg-slate-800" title="Aktif Hatırlatıcılar">
    <span class="text-lg group-hover:scale-110 transition-transform">⏰</span>
    
    <span v-if="totalCount > 0" class="absolute -top-1 -right-1 min-w-[20px] h-5 rounded-full bg-indigo-500 border-2 border-white dark:border-slate-900 flex items-center justify-center text-[10px] font-black text-white px-1 shadow-sm">
      {{ totalCount }}
    </span>
    
    <!-- pt-2 köprüsü: fare ile dropdown arasındaki boşluğu kapatır, hover state korunur -->
    <div class="absolute right-0 top-full pt-2 w-64 opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all z-50">
      <div class="bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl shadow-xl overflow-hidden">
        <div class="bg-indigo-50 dark:bg-indigo-900/30 px-4 py-2 border-b border-indigo-100 dark:border-indigo-800">
          <h4 class="text-xs font-black text-indigo-800 dark:text-indigo-300 uppercase tracking-wider">Hatırlatıcılar ({{ totalCount }})</h4>
        </div>
        <div class="max-h-60 overflow-y-auto">
          <div v-if="totalCount === 0" class="p-4 text-center text-xs text-slate-500 dark:text-slate-400">
            Aktif hatırlatıcı bulunmuyor.
          </div>
          <div v-for="rem in reminderStore.activeReminders.value" :key="rem.id" class="p-3 border-b border-slate-100 dark:border-slate-700 last:border-0 hover:bg-slate-50 dark:hover:bg-slate-700/50 transition">
            <div class="text-[10px] font-black text-slate-400 mb-1">#{{ rem.ticketReferenceNo }}</div>
            <div class="text-xs font-bold text-slate-700 dark:text-slate-300 mb-1 line-clamp-1">{{ rem.ticketTitle }}</div>
            <div v-if="rem.note" class="text-[10px] text-slate-500 dark:text-slate-400 mb-1 line-clamp-2 italic">📝 {{ rem.note }}</div>
            <div class="text-[10px] font-medium" :class="new Date(rem.reminderAt) <= new Date() ? 'text-red-500' : 'text-indigo-500'">
              {{ new Date(rem.reminderAt).toLocaleString('tr-TR') }}
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
