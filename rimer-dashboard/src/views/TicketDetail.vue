<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { auth, toastStore } from '../auth'
import { i18n, theme } from '../lang'
import * as api from '../services/ticketApi'
import LangSelector from '../components/LangSelector.vue'
import NotificationBell from '../components/NotificationBell.vue'
import ReminderBadge from '../components/ReminderBadge.vue'
import TransferModal from '../components/TransferModal.vue'
import ReminderFormPopup from '../components/ReminderFormPopup.vue'

const route = useRoute()
const router = useRouter()
const user = auth.user

const ticket = ref(null)
const loading = ref(true)
const updating = ref(false)

const showTransferModal = ref(false)
const showReminderModal = ref(false)
const showCloseModal = ref(false)
const showUnansweredModal = ref(false)
const replyMessage = ref('')

const loadTicket = async () => {
  loading.value = true
  try {
    const res = await api.getTicketById(route.params.id)
    if (!res.data?.success) throw new Error()
    ticket.value = res.data.data
  } catch (e) {
    const msg = e.response?.data?.message || 'Talep bulunamadı veya erişim yetkiniz yok'
    toastStore.add(msg, 'error')
    router.push('/dashboard')
  } finally {
    loading.value = false
  }
}

const sendReply = async () => {
  if (!replyMessage.value.trim()) return
  updating.value = true
  try {
    const res = await api.replyTicket(ticket.value.id, replyMessage.value)
    if (res.data?.success) {
      toastStore.add('Yanıt gönderildi', 'success')
      replyMessage.value = ''
      await loadTicket()
    } else {
      toastStore.add('Hata oluştu', 'error')
    }
  } catch (e) {
    toastStore.add('Hata oluştu', 'error')
  } finally {
    updating.value = false
  }
}

const closeTicketAction = async () => {
  showCloseModal.value = false
  updating.value = true
  try {
    const res = await api.closeTicket(ticket.value.id)
    if (res.data?.success) {
      toastStore.add('Talep kapatıldı', 'success')
      await loadTicket()
    } else {
      toastStore.add('Hata oluştu', 'error')
    }
  } catch (e) {
    toastStore.add('Hata oluştu', 'error')
  } finally {
    updating.value = false
  }
}

const handleTransferred = () => {
  toastStore.add('Talep başarıyla transfer edildi.', 'success')
  router.push('/dashboard')
}

const takeOwnershipAction = async () => {
  updating.value = true
  try {
    const res = await api.takeOwnership(ticket.value.id)
    if (res.data?.success) {
      toastStore.add('Talebi üzerinize aldınız.', 'success')
      await loadTicket()
    } else toastStore.add('Hata oluştu', 'error')
  } catch (e) { toastStore.add('Hata oluştu', 'error') }
  finally { updating.value = false }
}

const updatePriorityAction = async (val) => {
  updating.value = true
  try {
    const res = await api.updatePriority(ticket.value.id, val)
    if (res.data?.success) {
      toastStore.add('Öncelik güncellendi.', 'success')
      await loadTicket()
    } else toastStore.add('Hata oluştu', 'error')
  } catch (e) { toastStore.add('Hata oluştu', 'error') }
  finally { updating.value = false }
}

const updateInternalStatusAction = async (val) => {
  updating.value = true
  try {
    const res = await api.updateInternalStatus(ticket.value.id, val)
    if (res.data?.success) {
      toastStore.add('İç durum güncellendi.', 'success')
      await loadTicket()
    } else toastStore.add('Hata oluştu', 'error')
  } catch (e) { toastStore.add('Hata oluştu', 'error') }
  finally { updating.value = false }
}

const handleReminderCreated = () => {
  toastStore.add('Hatırlatıcı oluşturuldu.', 'success')
}

onMounted(loadTicket)

const getStatusColor = (status) => {
  switch (status) {
    case 'Open': return 'bg-blue-100 text-blue-700 dark:bg-blue-500/10 dark:text-blue-400 border-blue-200 dark:border-blue-500/20'
    case 'Submitted': return 'bg-blue-100 text-blue-700 dark:bg-blue-500/10 dark:text-blue-400 border-blue-200 dark:border-blue-500/20'
    case 'InProgress': return 'bg-amber-100 text-amber-700 dark:bg-amber-500/10 dark:text-amber-400 border-amber-200 dark:border-amber-500/20'
    case 'Reviewing': return 'bg-amber-100 text-amber-700 dark:bg-amber-500/10 dark:text-amber-400 border-amber-200 dark:border-amber-500/20'
    case 'WaitingDepartment': return 'bg-purple-100 text-purple-700 dark:bg-purple-500/10 dark:text-purple-400 border-purple-200 dark:border-purple-500/20'
    case 'Answered': return 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-400 border-emerald-200 dark:border-emerald-500/20'
    case 'Closed': return 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-400 border-emerald-200 dark:border-emerald-500/20'
    default: return 'bg-slate-100 text-slate-700 dark:bg-slate-500/10 dark:text-slate-400'
  }
}

const getStatusLabel = (status) => i18n.t[`status${status}`] || status
const getCategoryLabel = (cat) => i18n.t[cat] || i18n.t[`cat${cat}`] || cat

const getPriorityBadge = (p) => {
  const val = typeof p === 'string' ? { Low:1, Normal:2, Important:3, Critical:4 }[p] || 2 : (p || 2)
  switch(val) {
    case 1: return { label: 'Düşük', color: 'bg-slate-200 text-slate-600 dark:bg-slate-700 dark:text-slate-300' }
    case 3: return { label: 'Önemli', color: 'bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400' }
    case 4: return { label: 'Yüksek', color: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400', pulse: true }
    default: return { label: 'Normal', color: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400' }
  }
}
const getDelayBadge = (createdAt, status) => {
  if (!createdAt || status === 'Closed' || status === 'Answered') return null
  const age = Math.floor((Date.now() - new Date(createdAt).getTime()) / 86400000)
  if (age >= 90) return { label: '90+ gün gecikmiş', color: 'bg-red-900 text-white', pulse: true }
  if (age >= 60) return { label: '60+ gün gecikmiş', color: 'bg-red-600 text-white' }
  if (age >= 45) return { label: '45+ gün gecikmiş', color: 'bg-orange-500 text-white' }
  if (age >= 30) return { label: '30+ gün gecikmiş', color: 'bg-yellow-500 text-yellow-900' }
  return null
}

const historyActionLabel = (a) => ({
  Created: 'Oluşturuldu', Transferred: 'Yönlendirildi', Assigned: 'Görevlendirildi',
  StatusChanged: 'Durum Değişti', TakenOwnership: 'İşleme Alındı',
  Closed: 'Kapatıldı', InternalStatusChanged: 'İç Durum Güncellendi'
}[a] || a)

const timeline = computed(() => {
  if (!ticket.value) return []
  const events = []
  
  events.push({
    id: 'creation',
    type: 'create',
    date: new Date(ticket.value.createdAt),
    author: ticket.value.creatorName || (ticket.value.guestName ? ticket.value.guestName + ' ' + ticket.value.guestSurname : 'Sistem'),
    action: 'Talep Oluşturuldu',
    text: 'Talep sisteme kaydedildi.'
  })
  
  if (ticket.value.histories) {
    ticket.value.histories.forEach(h => {
      if (h.action === 'Created') return
      
      let actionName = historyActionLabel(h.action)
      let finalAction = h.departmentName ? `${h.departmentName} — ${actionName}` : actionName
      
      let detail = ''
      if (h.action === 'Transferred') detail = `Başvuru yönlendirildi. ${h.departmentName ? 'Birim: ' + h.departmentName + ' ' : ''}${h.note ? 'Not: ' + h.note : ''}`
      else if (h.action === 'Assigned') detail = `İlgili personele atandı. ${h.newValue ? '(' + h.newValue + ')' : ''}`
      else if (h.action === 'StatusChanged') detail = `Durum güncellendi: ${h.newValue}`
      else if (h.action === 'InternalStatusChanged') detail = `İç durum güncellendi: ${h.newValue}`
      else if (h.action === 'TakenOwnership') detail = `İlgili personel başvuruyu işleme aldı.`
      else if (h.action === 'Closed') detail = `Başvuru sonuçlandırıldı.`
      else detail = `${h.oldValue || ''} -> ${h.newValue || ''}`

      events.push({
        id: h.id,
        type: h.action === 'Transferred' ? 'transfer' : 'history',
        date: new Date(h.changedAt),
        author: h.changedByName || 'Sistem',
        action: finalAction,
        text: detail
      })
    })
  }
  
  if (ticket.value.replies) {
    ticket.value.replies.forEach(r => {
      events.push({
        id: 'reply-' + r.id,
        type: 'reply',
        date: new Date(r.createdAt),
        author: r.createdByName || r.authorName || '🏢 Kurum',
        action: 'Cevaplandı',
        text: r.message
      })
    })
  }
  
  events.sort((a, b) => a.date - b.date)
  return events
})

const isClosed = computed(() => ticket.value?.status === 'Closed')
const canAct = computed(() => {
  if (!user || !user.role) return false;
  const r = user.role.toLowerCase();
  return ['unituser', 'admin', 'staff'].includes(r) && !auth.isRector;
})
</script>

<template>
  <div class="min-h-screen bg-slate-50 dark:bg-slate-950 flex flex-col font-outfit">
    <header class="h-16 bg-white/80 dark:bg-slate-900/80 backdrop-blur border-b border-slate-200 dark:border-slate-800 px-6 flex items-center justify-between sticky top-0 z-40">
      <div class="flex items-center gap-4">
        <button @click="router.back()" class="p-2 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-500 transition-colors">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" /></svg>
        </button>
        <div class="relative h-10 w-28 flex-shrink-0 cursor-pointer" @click="router.push('/dashboard')">
          <img src="/logo.png" alt="Rimer Logo" class="absolute inset-0 w-full h-full object-contain transition-opacity duration-200" :class="theme.dark ? 'opacity-0' : 'opacity-100'" />
          <img src="/logonight.png" alt="Rimer Logo" class="absolute inset-0 w-full h-full object-contain transition-opacity duration-200" :class="theme.dark ? 'opacity-100' : 'opacity-0'" />
        </div>
      </div>

      <div class="flex items-center gap-4">
        <LangSelector />
        <button @click="theme.toggle()" class="w-10 h-10 rounded-xl bg-slate-50 dark:bg-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-100 transition-all flex items-center justify-center">
          <span v-if="theme.dark">☀️</span><span v-else>🌙</span>
        </button>
        <div class="h-6 w-px bg-slate-200 dark:bg-slate-800" />
        <ReminderBadge />
        <NotificationBell />
      </div>
    </header>

    <main v-if="loading" class="flex-1 flex items-center justify-center">
      <div class="flex flex-col items-center gap-4">
        <div class="animate-spin rounded-full h-12 w-12 border-4 border-blue-600 border-t-transparent"></div>
        <p class="text-xs font-bold text-slate-400 uppercase tracking-widest">Yükleniyor...</p>
      </div>
    </main>

    <main v-else-if="ticket" class="flex-1 p-6 md:p-8 max-w-[1600px] mx-auto w-full">
      <!-- TOP ACTION BAR -->
      <div class="flex flex-wrap items-center justify-between gap-6 mb-8 bg-white dark:bg-slate-900 p-6 rounded-3xl border border-slate-200 dark:border-slate-800 shadow-sm">
        <div class="flex-1 min-w-[300px]">
          <div class="flex items-center gap-3 mb-2 flex-wrap">
            <span class="px-2.5 py-1 bg-slate-100 dark:bg-slate-800 text-[10px] font-black text-slate-500 dark:text-slate-400 rounded-lg uppercase tracking-widest">
              #{{ ticket.referenceNo }}
            </span>
            <span class="px-3 py-1 rounded-lg text-[10px] font-black uppercase tracking-wider border shadow-sm" :class="getStatusColor(ticket.status)">
              {{ getStatusLabel(ticket.status) }}
            </span>
            <span class="px-3 py-1 rounded-lg text-[10px] font-black uppercase tracking-wider shadow-sm" :class="[getPriorityBadge(ticket.priority).color, getPriorityBadge(ticket.priority).pulse ? 'animate-pulse' : '']">
              {{ getPriorityBadge(ticket.priority).label }}
            </span>
          </div>
          <h1 class="text-3xl font-black text-slate-900 dark:text-white leading-tight">
             {{ ticket.title }}
          </h1>
          <p v-if="ticket.assignedTo" class="text-xs text-slate-500 mt-2 font-medium">
             Sorumlu: <span class="text-indigo-600 dark:text-indigo-400 font-bold uppercase">{{ ticket.assignedTo.fullName }}</span>
          </p>
        </div>

        <div class="flex items-center gap-3" v-if="canAct && !isClosed">
          <button v-if="!ticket.assignedTo || ticket.assignedTo?.id !== user.id" @click="takeOwnershipAction" class="px-5 py-3 bg-indigo-600 hover:bg-indigo-700 text-white rounded-2xl text-sm font-black shadow-lg shadow-indigo-500/20 transition-all active:scale-95 flex items-center gap-2">
            ✋ Sahiplen
          </button>
          <button @click="showReminderModal = true" class="px-5 py-3 bg-purple-600 hover:bg-purple-700 text-white rounded-2xl text-sm font-black shadow-lg shadow-purple-500/20 transition-all active:scale-95 flex items-center gap-2">
            ⏰ Hatırlatıcı
          </button>
          <button v-if="ticket.status !== 'Answered' && (!ticket.assignedTo || ticket.assignedTo.id === user.id)" @click="showTransferModal = true" class="px-5 py-3 bg-amber-500 hover:bg-amber-600 text-white rounded-2xl text-sm font-black shadow-lg shadow-amber-500/20 transition-all active:scale-95 flex items-center gap-2">
            🔄 Transfer
          </button>
          <button v-if="ticket.status === 'Answered' || (!ticket.assignedTo || ticket.assignedTo.id === user.id)" @click="ticket?.status === 'Answered' || (ticket?.replies && ticket.replies.length > 0) ? showCloseModal = true : showUnansweredModal = true" class="px-5 py-3 bg-rose-500 hover:bg-rose-600 text-white rounded-2xl text-sm font-black shadow-lg shadow-rose-500/20 transition-all active:scale-95 flex items-center gap-2">
            🔒 Kapat
          </button>
        </div>
      </div>

      <div v-if="auth.isRector" class="mb-6 bg-amber-50 dark:bg-amber-900/10 border border-amber-200 dark:border-amber-800 rounded-2xl p-4 text-amber-700 dark:text-amber-400 font-bold text-center uppercase tracking-tighter text-xs">
        ⚠️ Sadece Okuma Modu (Rektör) — İşlem Yapılamaz
      </div>

      <!-- TWO COLUMN GRID -->
      <div class="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
        
        <!-- LEFT COLUMN: CONTENT & REPLIES (8 cols) -->
        <div class="lg:col-span-8 space-y-6">
          
          <!-- MAIN CONTENT -->
          <div class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-3xl shadow-sm p-8">
            <div class="flex items-center gap-2 mb-6">
               <span class="w-1.5 h-6 rounded-full bg-indigo-500"></span>
               <h3 class="text-lg font-black text-slate-900 dark:text-white uppercase tracking-tight">Talep Detayı</h3>
            </div>
            
            <p class="text-slate-700 dark:text-slate-300 whitespace-pre-wrap leading-relaxed text-xl font-medium mb-8">{{ ticket.description }}</p>
            
            <div v-if="ticket.attachmentUrl" class="mt-8 pt-8 border-t border-slate-100 dark:border-slate-800">
               <dt class="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-4">EKLENEN DOSYALAR</dt>
               <a :href="ticket.attachmentUrl" target="_blank" class="inline-flex items-center gap-3 px-6 py-3 bg-slate-50 dark:bg-slate-800 hover:bg-slate-100 border border-slate-200 dark:border-slate-700 rounded-2xl text-sm font-black transition-all active:scale-95 group">
                  <span class="group-hover:animate-bounce">📄</span> Dosyayı Görüntüle
               </a>
            </div>
          </div>

          <!-- REPLY SECTION -->
          <div v-if="canAct && !isClosed && ticket.status !== 'Answered' && (!ticket.assignedTo || ticket.assignedTo.id === user.id)" class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-3xl shadow-sm p-8">
            <div class="flex items-center justify-between mb-6">
               <h3 class="text-lg font-black text-slate-900 dark:text-white uppercase flex items-center gap-2">
                 <span class="w-8 h-8 rounded-lg bg-blue-50 dark:bg-blue-900/30 flex items-center justify-center text-sm">💬</span> 
                 Resmi Yanıt Yaz
               </h3>
               <span class="text-[10px] font-bold text-slate-400 uppercase">Vatandaşa iletilecek</span>
            </div>
            <textarea
              v-model="replyMessage"
              rows="6"
              class="w-full bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-2xl p-6 text-slate-900 dark:text-white outline-none focus:ring-4 focus:ring-blue-500/20 resize-none mb-6 transition-all text-sm leading-relaxed"
              placeholder="Vatandaşa/Öğrenciye iletilecek resmi yanıtınızı buraya yazın..."
              :disabled="updating"
            ></textarea>
            <div class="flex justify-end">
              <button
                @click="sendReply"
                :disabled="updating || !replyMessage.trim()"
                class="px-10 py-4 bg-blue-600 hover:bg-blue-700 text-white font-black rounded-2xl shadow-xl shadow-blue-500/30 disabled:opacity-50 transition-all active:scale-95 flex items-center gap-2"
              >
                <span v-if="updating" class="animate-spin text-xl">⏳</span>
                {{ updating ? 'GÖNDERİLİYOR...' : 'RESMİ YANITI GÖNDER' }}
              </button>
            </div>
          </div>

          <div v-if="isClosed" class="bg-emerald-50 dark:bg-emerald-900/10 border border-emerald-200 dark:border-emerald-800 rounded-3xl p-12 text-center shadow-inner">
            <div class="w-20 h-20 bg-emerald-500 rounded-full flex items-center justify-center mx-auto mb-6 shadow-lg shadow-emerald-500/40">
               <span class="text-3xl text-white">✓</span>
            </div>
            <h3 class="text-xl font-black text-emerald-900 dark:text-emerald-400 uppercase tracking-tight">BU TALEP KAPATILMIŞTIR</h3>
            <p class="text-emerald-700 dark:text-emerald-500/80 mt-2 font-medium">Başvuru süreci başarıyla sonuçlandırıldı ve arşive kaldırıldı.</p>
          </div>
        </div>

        <!-- RIGHT COLUMN: METADATA & ACTIONS (4 cols) -->
        <div class="lg:col-span-4 space-y-6">
          
          <!-- TICKET METADATA CARD -->
          <div class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-3xl shadow-sm p-6">
             <h4 class="text-xs font-black text-slate-400 uppercase tracking-widest mb-6">Talep Bilgileri</h4>
             <div class="space-y-4">
                <div class="flex justify-between items-center py-2 border-b border-slate-50 dark:border-slate-800">
                   <span class="text-xs font-bold text-slate-500 uppercase">Durum</span>
                   <span class="px-2.5 py-1 rounded-lg text-[10px] font-black uppercase tracking-wider border shadow-sm" :class="getStatusColor(ticket.status)">
                      {{ getStatusLabel(ticket.status) }}
                   </span>
                </div>
                <div class="flex justify-between items-center py-2 border-b border-slate-50 dark:border-slate-800">
                   <span class="text-xs font-bold text-slate-500 uppercase">Öncelik</span>
                   <span class="px-3 py-1 rounded-lg text-[10px] font-black uppercase tracking-wider shadow-sm" :class="[getPriorityBadge(ticket.priority).color, getPriorityBadge(ticket.priority).pulse ? 'animate-pulse' : '']">
                      {{ getPriorityBadge(ticket.priority).label }}
                   </span>
                </div>
                <div class="flex justify-between items-center py-2 border-b border-slate-50 dark:border-slate-800">
                   <span class="text-xs font-bold text-slate-500 uppercase">Kategori</span>
                   <span class="text-xs font-black text-slate-700 dark:text-slate-300 text-right">{{ getCategoryLabel(ticket.category) }}</span>
                </div>
                <div class="flex justify-between items-center py-2 border-b border-slate-50 dark:border-slate-800">
                   <span class="text-xs font-bold text-slate-500 uppercase">Sorumlu</span>
                   <span class="text-xs font-black text-indigo-600 dark:text-indigo-400">{{ ticket.assignedTo?.fullName || 'Atanmamış' }}</span>
                </div>
                <div class="flex justify-between items-center py-2">
                   <span class="text-xs font-bold text-slate-500 uppercase">Oluşturma</span>
                   <span class="text-xs font-black text-slate-700 dark:text-slate-300">{{ new Date(ticket.createdAt).toLocaleDateString('tr-TR') }}</span>
                </div>
                <div v-if="getDelayBadge(ticket.createdAt, ticket.status)" class="mt-4 p-3 bg-red-50 dark:bg-red-900/20 border border-red-100 dark:border-red-900/30 rounded-xl animate-pulse">
                   <div class="flex items-center gap-2">
                      <span class="text-red-600 dark:text-red-400 font-black text-[10px] uppercase tracking-wider">⚠️ SLA GECİKMESİ</span>
                   </div>
                   <p class="text-[10px] text-red-700 dark:text-red-400 font-bold mt-1">{{ getDelayBadge(ticket.createdAt, ticket.status).label }}</p>
                </div>
             </div>
          </div>

          <!-- APPLICANT INFO CARD -->
          <div class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-3xl shadow-sm p-6">
             <h4 class="text-xs font-black text-slate-400 uppercase tracking-widest mb-6 flex items-center gap-2">👤 Başvuru Sahibi</h4>
             
             <div v-if="ticket.isExternal && ticket.hidePersonalInfo" class="bg-slate-50 dark:bg-slate-800/50 p-4 rounded-xl border border-slate-100 dark:border-slate-700 text-center">
                <span class="text-sm font-black text-slate-500 dark:text-slate-400 uppercase tracking-widest">KİMLİĞİ GİZLİ BAŞVURU</span>
             </div>
             
             <div v-else class="space-y-4">
                <div class="flex flex-col gap-1 py-2 border-b border-slate-50 dark:border-slate-800">
                   <span class="text-[10px] font-black text-slate-400 uppercase tracking-widest">Ad Soyad</span>
                   <span class="text-sm font-bold text-slate-900 dark:text-white">
                      <template v-if="ticket.isExternal">
                         {{ ticket.guestName }} {{ ticket.guestSurname }}
                      </template>
                      <template v-else-if="ticket.creator">
                         {{ ticket.creator.fullName }}
                      </template>
                      <template v-else>
                         Bilinmiyor
                      </template>
                   </span>
                </div>
                <div class="flex flex-col gap-1 py-2 border-b border-slate-50 dark:border-slate-800">
                   <span class="text-[10px] font-black text-slate-400 uppercase tracking-widest">E-Posta</span>
                   <span class="text-sm font-bold text-slate-900 dark:text-white">{{ ticket.isExternal ? ticket.guestEmail : (ticket.creator?.email || 'Belirtilmemiş') }}</span>
                </div>
                <div class="flex flex-col gap-1 py-2 border-b border-slate-50 dark:border-slate-800">
                   <span class="text-[10px] font-black text-slate-400 uppercase tracking-widest">Telefon</span>
                   <span class="text-sm font-bold text-slate-900 dark:text-white">{{ ticket.isExternal ? (ticket.guestPhone || 'Belirtilmemiş') : (ticket.creator?.phoneNumber || 'Belirtilmemiş') }}</span>
                </div>
                <div class="flex flex-col gap-1 py-2" v-if="ticket.isExternal && ticket.guestAddress">
                   <span class="text-[10px] font-black text-slate-400 uppercase tracking-widest">Adres</span>
                   <span class="text-sm font-bold text-slate-900 dark:text-white leading-relaxed">{{ ticket.guestAddress }}</span>
                </div>
             </div>
          </div>

          <!-- OPERATIONAL CONTROLS -->
          <div class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-3xl shadow-sm p-6" v-if="canAct && !isClosed">
            <h4 class="text-xs font-black text-slate-400 uppercase tracking-widest mb-6">Yönetim Paneli</h4>
            <div class="space-y-4">
               <div>
                  <label class="text-[9px] font-black text-slate-400 uppercase tracking-widest block mb-2">Birim İçi Durum</label>
                  <select 
                    :value="ticket.internalStatus" 
                    @change="updateInternalStatusAction($event.target.value)"
                    class="w-full bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl px-4 py-3 text-xs font-black outline-none focus:ring-2 focus:ring-blue-500/20 transition-all cursor-pointer"
                  >
                    <option value="Unread">Okunmadı</option>
                    <option value="Read">Okundu</option>
                    <option value="Pending">İşlem Bekliyor</option>
                    <option value="WaitingExternal">Dış Kurum Bekleniyor</option>
                    <option value="Escalated">Üst Birime İletildi</option>
                  </select>
               </div>
               <div>
                  <label class="text-[9px] font-black text-slate-400 uppercase tracking-widest block mb-2">Öncelik Değiştir</label>
                  <select 
                    :value="ticket.priority" 
                    @change="updatePriorityAction($event.target.value)"
                    class="w-full bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-xl px-4 py-3 text-xs font-black outline-none focus:ring-2 focus:ring-indigo-500/20 transition-all cursor-pointer"
                  >
                    <option :value="1">🟢 Düşük</option>
                    <option :value="2">🔵 Normal</option>
                    <option :value="3">🟠 Önemli</option>
                    <option :value="4">🔴 Yüksek</option>
                  </select>
               </div>
            </div>
          </div>

          <!-- TIMELINE -->
          <div class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-3xl shadow-sm overflow-hidden flex flex-col max-h-[500px]">
            <div class="p-5 border-b border-slate-100 dark:border-slate-800 flex items-center justify-between bg-slate-50/50 dark:bg-slate-800/30">
              <h3 class="text-xs font-black text-slate-900 dark:text-white uppercase tracking-tight">Geçmiş</h3>
              <span class="px-2 py-0.5 bg-white dark:bg-slate-900 rounded-lg text-[9px] font-bold text-slate-400 border border-slate-200 dark:border-slate-700">{{ timeline.length }}</span>
            </div>
            <div class="p-6 overflow-y-auto flex-1">
              <div class="space-y-6 relative">
                <div class="absolute left-2.5 top-2 bottom-2 w-px bg-slate-100 dark:bg-slate-800" />
                <div v-for="event in timeline" :key="event.id" class="relative pl-8">
                  <div class="absolute left-0 top-1 w-5 h-5 rounded-full border-4 border-white dark:border-slate-900 shadow-sm"
                       :class="{
                         'bg-blue-500': event.type === 'create',
                         'bg-emerald-500': event.type === 'reply',
                         'bg-amber-500': event.type === 'transfer',
                         'bg-slate-500': event.type === 'history'
                       }" />
                  <div class="flex flex-col mb-1">
                    <span class="text-[10px] font-black text-slate-900 dark:text-white uppercase">{{ event.action }}</span>
                    <span class="text-[9px] text-slate-400 font-bold uppercase">{{ event.date.toLocaleString('tr-TR') }} - {{ event.author }}</span>
                  </div>
                  <div class="mt-1 p-2.5 rounded-xl border text-[11px] leading-relaxed"
                       :class="event.type === 'reply' ? 'bg-blue-50 dark:bg-blue-900/10 border-blue-100 dark:border-blue-900 text-blue-800 dark:text-blue-300 font-medium whitespace-pre-wrap' : 'bg-slate-50 dark:bg-slate-800/50 border-slate-100 dark:border-slate-800 text-slate-600 dark:text-slate-400 italic'">
                    {{ event.text }}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </main>

    <!-- Modals -->
    <TransferModal 
      v-if="ticket"
      :show="showTransferModal" 
      :ticketId="ticket.id"
      :currentDepartmentId="ticket.assignedDepartmentId"
      @close="showTransferModal = false"
      @transferred="handleTransferred"
    />

    <ReminderFormPopup
      v-if="ticket"
      :show="showReminderModal"
      :ticketId="ticket.id"
      @close="showReminderModal = false"
      @created="handleReminderCreated"
    />

    <!-- Close Ticket Modal -->
    <div v-if="showCloseModal" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 backdrop-blur-sm p-4">
      <div class="bg-white dark:bg-slate-900 rounded-3xl border border-slate-200 dark:border-slate-800 p-8 w-full max-w-md shadow-2xl animate-in fade-in zoom-in-95 duration-200">
        <div class="w-16 h-16 bg-rose-100 dark:bg-rose-900/30 text-rose-600 dark:text-rose-400 rounded-full flex items-center justify-center mb-6 shadow-sm mx-auto">
          <svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" /></svg>
        </div>
        <h3 class="text-xl font-black text-slate-900 dark:text-white mb-2 text-center">Talebi Kapat</h3>
        <p class="text-sm text-slate-600 dark:text-slate-400 mb-8 leading-relaxed text-center">
          <strong>#{{ ticket?.referenceNo }}</strong> referans numaralı bu talebi kapatmak istediğinize emin misiniz? İşlem tamamlandığında vatandaş veya ilgili öğrenci başvurusunun sonuçlandığına dair bilgilendirilecektir.
        </p>
        <div class="flex gap-4">
          <button @click="showCloseModal = false" type="button" class="flex-1 py-3 px-4 rounded-xl border-2 border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-300 font-bold hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors">
            İptal
          </button>
          <button @click="closeTicketAction" :disabled="updating" type="button" class="flex-1 py-3 px-4 rounded-xl bg-rose-600 hover:bg-rose-700 text-white font-black shadow-lg shadow-rose-500/30 flex items-center justify-center gap-2 transition-all active:scale-95 disabled:opacity-50">
            {{ updating ? 'Kapatılıyor...' : 'Evet, Kapat' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Cevap Verilmedi Uyarısı Modal -->
    <div v-if="showUnansweredModal" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 backdrop-blur-sm p-4">
      <div class="bg-white dark:bg-slate-900 rounded-3xl border border-slate-200 dark:border-slate-800 p-8 w-full max-w-md shadow-2xl animate-in fade-in zoom-in-95 duration-200">
        <div class="w-16 h-16 bg-orange-100 dark:bg-orange-900/30 text-orange-600 dark:text-orange-400 rounded-full flex items-center justify-center mb-6 shadow-sm mx-auto">
          <svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" /></svg>
        </div>
        <h3 class="text-xl font-black text-slate-900 dark:text-white mb-2 text-center">İşlem Gerçekleştirilemez</h3>
        <p class="text-sm text-slate-600 dark:text-slate-400 mb-8 leading-relaxed text-center">
          Bu talebe henüz <strong>cevap verilmedi</strong>. Cevap verilmeyen talepler kapatılamaz. Lütfen önce talebi yanıtlayın.
        </p>
        <div class="flex justify-center">
          <button @click="showUnansweredModal = false" type="button" class="w-full py-3 px-4 rounded-xl bg-orange-500 hover:bg-orange-600 text-white font-black shadow-lg shadow-orange-500/30 transition-all active:scale-95">
            Tamam, Anladım
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
