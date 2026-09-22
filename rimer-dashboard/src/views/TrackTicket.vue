<script setup>
import { ref } from 'vue'
import * as api from '../services/ticketApi'
import { theme, i18n } from '../lang'
import { toastStore } from '../auth'
import LangSelector from '../components/LangSelector.vue'

const form = ref({
  trackingCode: '',
  email: ''
})

const loading = ref(false)
const error = ref('')
const ticketInfo = ref(null)

const trackTicket = async () => {
  loading.value = true
  error.value = ''
  ticketInfo.value = null
  
  try {
    const res = await api.trackPublicTicket(form.value.trackingCode, form.value.email)
    
    if (res.data?.success) {
      ticketInfo.value = res.data.data
      toastStore.success(i18n.lang === 'tr' ? 'Başvuru detayları getirildi.' : 'Ticket details loaded.')
    } else {
      error.value = res.data?.message || 'Başvuru bulunamadı.'
      toastStore.error(error.value)
    }
  } catch (e) {
    error.value = e.response?.data?.message || 'Sunucu ile iletişim kurulamadı.'
    toastStore.error(error.value)
  } finally {
    loading.value = false
  }
}

const getCategoryLabel = (cat) => {
  if (!cat) return ''
  return i18n.t[cat] || cat
}

const getStatusLabel = (status) => {
  if (!status) return ''
  const key = 'status' + status
  return i18n.t[key] || status
}
</script>

<template>
  <div class="min-h-screen bg-slate-50 dark:bg-slate-950 flex flex-col">
    <!-- Header -->
    <header class="h-20 bg-white dark:bg-slate-900 border-b border-slate-200 dark:border-slate-800 px-6 flex items-center justify-between sticky top-0 z-50">
      <div class="flex items-center gap-4">
        <div class="relative h-16 w-40 flex-shrink-0 cursor-pointer" @click="$router.push('/login')">
          <img src="/logo.png" alt="Rimer Logo" class="absolute inset-0 w-full h-full object-contain transition-opacity duration-200" :class="theme.dark ? 'opacity-0' : 'opacity-100'" />
          <img src="/logonight.png" alt="Rimer Logo" class="absolute inset-0 w-full h-full object-contain transition-opacity duration-200" :class="theme.dark ? 'opacity-100' : 'opacity-0'" />
        </div>
      </div>
      
      <div class="flex items-center gap-4">
        <div class="flex items-center gap-2 bg-slate-50 dark:bg-slate-800 p-1.5 rounded-xl border border-slate-200 dark:border-slate-700">
          <LangSelector />
          <button @click="theme.toggle()" class="p-2 rounded-lg hover:bg-white dark:hover:bg-slate-700 transition-colors">
            {{ theme.dark ? '☀️' : '🌙' }}
          </button>
        </div>
      </div>
    </header>

    <main class="flex-1 p-4 md:p-8 flex justify-center items-start">
      <div class="w-full max-w-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-3xl shadow-xl overflow-hidden relative">
        
        <!-- Form -->
        <div v-if="!ticketInfo" class="p-8 md:p-12">
          <!-- Geri Butonu -->
          <button @click="$router.push('/login')" class="absolute top-6 left-6 flex items-center justify-center w-10 h-10 rounded-xl bg-slate-50 dark:bg-slate-800 text-slate-400 hover:text-slate-700 dark:hover:text-slate-200 transition-colors border border-slate-200 dark:border-slate-700 shadow-sm z-10" :title="i18n.lang === 'tr' ? 'Giriş Ekranına Dön' : 'Back to Login'">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/></svg>
          </button>

          <div class="text-center mb-10 pt-4">
            <h2 class="text-3xl font-black text-slate-900 dark:text-white uppercase mb-4">{{ i18n.t.publicTrack }}</h2>
            <p class="text-slate-500 dark:text-slate-400 max-w-md mx-auto">{{ i18n.t.publicTrackSub }}</p>
          </div>

          <form @submit.prevent="trackTicket" class="space-y-6">
            <div>
              <label class="block text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2">{{ i18n.t.trackingNo.toUpperCase() }}</label>
              <input v-model="form.trackingCode" type="text" placeholder="RIM-2026-000001" required 
                class="w-full px-5 py-4 rounded-2xl bg-slate-50 dark:bg-slate-800 border-2 border-transparent focus:border-blue-500 outline-none text-base font-bold text-slate-900 dark:text-white transition-all" />
            </div>

            <div>
              <label class="block text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2">{{ i18n.t.email.toUpperCase() }}</label>
              <input v-model="form.email" type="email" required 
                class="w-full px-5 py-4 rounded-2xl bg-slate-50 dark:bg-slate-800 border-2 border-transparent focus:border-blue-500 outline-none text-base font-bold text-slate-900 dark:text-white transition-all" />
            </div>

            <div v-if="error" class="p-4 bg-rose-50 dark:bg-rose-900/20 text-rose-600 rounded-xl text-sm font-bold border border-rose-100 dark:border-rose-900/50">
              ⚠️ {{ error }}
            </div>

            <button type="submit" :disabled="loading" 
              class="w-full py-5 bg-blue-600 hover:bg-blue-700 text-white rounded-2xl font-black text-sm shadow-xl shadow-blue-500/30 transition-all hover:-translate-y-0.5 active:scale-95 disabled:opacity-50">
              {{ loading ? i18n.t.waiting : i18n.t.trackButton }}
            </button>
          </form>

          <div class="mt-8 text-center">
            <router-link to="/apply" class="text-sm font-black text-blue-600 hover:text-blue-700 underline underline-offset-4">
              {{ i18n.t.publicApply.toUpperCase() }}
            </router-link>
          </div>
        </div>

        <!-- Result -->
        <div v-else class="animate-in fade-in duration-300">
          <div class="bg-slate-50 dark:bg-slate-800/50 p-6 md:p-8 border-b border-slate-200 dark:border-slate-800 flex flex-col md:flex-row justify-between items-center gap-4">
            <div>
              <h3 class="text-sm font-black text-slate-400 uppercase tracking-widest mb-1">{{ i18n.t.trackResult }}</h3>
              <p class="text-2xl font-black text-slate-900 dark:text-white">{{ ticketInfo.trackingCode }}</p>
            </div>
            <button @click="ticketInfo = null" class="px-6 py-2.5 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-xs font-black text-slate-600 dark:text-slate-300 hover:bg-slate-50 transition-colors">
              {{ i18n.t.newQuery.toUpperCase() }}
            </button>
          </div>
          
          <div class="p-8 md:p-10 space-y-10">
            <!-- Basic Info -->
            <div class="grid grid-cols-1 md:grid-cols-2 gap-8">
              <div class="md:col-span-2">
                <dt class="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-1">{{ i18n.t.subject.toUpperCase() }}</dt>
                <dd class="text-lg font-black text-slate-900 dark:text-white">{{ ticketInfo.title }}</dd>
              </div>

              <!-- DESCRIPTION -->
              <div class="md:col-span-2 bg-slate-50 dark:bg-slate-800/30 p-5 rounded-2xl border border-slate-200 dark:border-slate-800">
                <dt class="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2 flex items-center gap-2">📝 Talep İçeriği</dt>
                <dd class="text-sm font-medium text-slate-700 dark:text-slate-300 whitespace-pre-wrap leading-relaxed">{{ ticketInfo.description }}</dd>
              </div>

              <!-- LATEST REPLY -->
              <div v-if="ticketInfo.latestReply" class="md:col-span-2 bg-blue-50 dark:bg-blue-900/20 p-5 rounded-2xl border border-blue-100 dark:border-blue-800/50">
                <dt class="text-[10px] font-black text-blue-500 uppercase tracking-widest mb-2 flex items-center gap-2">🏢 Kurum Yanıtı</dt>
                <dd class="text-sm font-bold text-blue-900 dark:text-blue-100 whitespace-pre-wrap leading-relaxed">{{ ticketInfo.latestReply }}</dd>
              </div>
              
              <div>
                <dt class="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-1">{{ i18n.t.category.toUpperCase() }}</dt>
                <dd class="text-sm font-bold text-slate-700 dark:text-slate-300">{{ getCategoryLabel(ticketInfo.category) }}</dd>
              </div>
              
              <div>
                <dt class="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-1">{{ i18n.t.status.toUpperCase() }}</dt>
                <dd class="mt-1">
                  <span class="px-3 py-1 bg-blue-100 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 rounded-full text-xs font-black uppercase tracking-wider">
                    {{ getStatusLabel(ticketInfo.status) }}
                  </span>
                </dd>
              </div>
              
              <div>
                <dt class="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-1">{{ i18n.t.relevantDept.toUpperCase() }}</dt>
                <dd class="text-sm font-bold text-slate-700 dark:text-slate-300">{{ ticketInfo.department || i18n.t.noDeptAssigned }}</dd>
                <div v-if="ticketInfo.pastDepartments && ticketInfo.pastDepartments.length" class="mt-3">
                  <dt class="text-[9px] font-black text-slate-400 uppercase tracking-widest mb-1">Geçmiş Birimler</dt>
                  <dd class="text-[11px] font-medium text-slate-500 dark:text-slate-400 leading-relaxed">
                    {{ ticketInfo.pastDepartments.join(' → ') }}
                  </dd>
                </div>
              </div>
              
              <div>
                <dt class="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-1">{{ i18n.t.date.toUpperCase() }}</dt>
                <dd class="text-sm font-bold text-slate-700 dark:text-slate-300">{{ new Date(ticketInfo.createdAt).toLocaleString(i18n.lang === 'tr' ? 'tr-TR' : 'en-US') }}</dd>
              </div>
            </div>

            <!-- Personal Info (Conditional) -->
            <div v-if="!ticketInfo.hidePersonalInfo" class="bg-slate-50 dark:bg-slate-800/30 p-6 rounded-2xl border border-slate-200 dark:border-slate-800">
              <h4 class="text-xs font-black text-slate-400 uppercase tracking-widest mb-4 flex items-center gap-2">
                👤 {{ i18n.t.personalInfo.toUpperCase() }}
              </h4>
              <div class="grid grid-cols-1 md:grid-cols-2 gap-y-4 gap-x-8">
                <div>
                  <p class="text-[9px] font-black text-slate-400 uppercase mb-0.5">{{ i18n.t.fullName }}</p>
                  <p class="text-sm font-bold text-slate-900 dark:text-white">{{ ticketInfo.guestFirstName }} {{ ticketInfo.guestLastName }}</p>
                </div>
                <div v-if="ticketInfo.guestIdentityNumber">
                  <p class="text-[9px] font-black text-slate-400 uppercase mb-0.5">{{ i18n.t.idNumber }}</p>
                  <p class="text-sm font-bold text-slate-900 dark:text-white">{{ ticketInfo.guestIdentityNumber }}</p>
                </div>
                <div>
                  <p class="text-[9px] font-black text-slate-400 uppercase mb-0.5">{{ i18n.t.email }}</p>
                  <p class="text-sm font-bold text-slate-900 dark:text-white">{{ ticketInfo.guestEmail }}</p>
                </div>
                <div v-if="ticketInfo.guestPhone">
                  <p class="text-[9px] font-black text-slate-400 uppercase mb-0.5">TELEFON</p>
                  <p class="text-sm font-bold text-slate-900 dark:text-white">{{ ticketInfo.guestPhone }}</p>
                </div>
                <div class="md:col-span-2" v-if="ticketInfo.guestAddress">
                  <p class="text-[9px] font-black text-slate-400 uppercase mb-0.5">{{ i18n.t.address }}</p>
                  <p class="text-sm font-bold text-slate-900 dark:text-white leading-relaxed">{{ ticketInfo.guestAddress }}</p>
                </div>
              </div>
            </div>

            <!-- Timeline -->
            <div class="pt-6 border-t border-slate-100 dark:border-slate-800">
              <h4 class="text-md font-black text-slate-900 dark:text-white mb-6 uppercase tracking-tight">{{ i18n.t.timelineTitle }}</h4>
              <div class="space-y-8 relative before:absolute before:left-4 before:top-2 before:bottom-2 before:w-0.5 before:bg-slate-100 dark:before:bg-slate-800">
                <div v-for="(event, eventIdx) in ticketInfo.timeline" :key="eventIdx" class="relative pl-12">
                  <div class="absolute left-0 top-1 w-8 h-8 rounded-full bg-blue-600 flex items-center justify-center border-4 border-white dark:border-slate-900 shadow-sm z-10">
                    <span class="text-[10px] text-white">✓</span>
                  </div>
                  <div class="flex flex-col md:flex-row md:items-center justify-between gap-2">
                    <div>
                      <p class="text-sm font-black text-slate-900 dark:text-white">{{ event.action }}</p>
                      <p v-if="event.detail" class="text-xs font-medium text-slate-500 dark:text-slate-400 mt-1">{{ event.detail }}</p>
                    </div>
                    <time class="text-[10px] font-black text-slate-400 uppercase">{{ new Date(event.timestamp).toLocaleDateString(i18n.lang === 'tr' ? 'tr-TR' : 'en-US') }}</time>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

      </div>
    </main>
  </div>
</template>

<style scoped>
.animate-in { animation: slide-in 0.3s cubic-bezier(0.16, 1, 0.3, 1); }
@keyframes slide-in { from { opacity: 0; transform: translateY(8px); } to { opacity: 1; transform: translateY(0); } }
</style>
