<script setup>
import { ref, onMounted, computed, watch } from 'vue'
import { useRouter } from 'vue-router'
import { auth, toastStore } from '../auth'
import { i18n, theme } from '../lang'
import * as api from '../services/ticketApi'
import * as XLSX from 'xlsx'
import LangSelector from '../components/LangSelector.vue'
import NotificationBell from '../components/NotificationBell.vue'
import ReminderBadge from '../components/ReminderBadge.vue'
import Pagination from '../components/Pagination.vue'
import TransferModal from '../components/TransferModal.vue'
import ReminderFormPopup from '../components/ReminderFormPopup.vue'

const router = useRouter()
const user = auth.user

// ── Data ──────────────────────────────────────────────────────────
const tickets = ref([])
const reminders = ref([])
const loading = ref(true)
const page = ref(1)
const pageSize = ref(10)
const totalCount = ref(0)
const searchTerm = ref('')
const filterStatus = ref('')
const filterOnlyMine = ref(false)
const hideClosed = ref(true)
const startDate = ref('')
const endDate = ref('')

// ── Action state ──────────────────────────────────────────────────
// Modal: Cevap Ver
const replyModal = ref({ show: false, ticketId: null, ticketTitle: '', ticketDescription: '', requesterName: '', requesterEmail: '', requesterPhone: '', requesterTc: '', requesterTitle: '', requesterAddress: '', requesterInstitution: '', attachmentUrl: '', message: '', saving: false })
// Modal: Yönlendir
const transferModal = ref({ show: false, ticketId: null, currentDepartmentId: null })
// Modal: Kapat
const closeTicketModal = ref({ show: false, ticketId: null, referenceNo: '', title: '' })
const showUnansweredModal = ref(false)
// Per-row loading flags
const actionLoading = ref({}) // { [ticketId]: true|false }

const setActionLoading = (id, val) => {
  actionLoading.value = { ...actionLoading.value, [id]: val }
}

const stats = ref({
  total: 0,
  newTickets: 0,
  reviewing: 0,
  myAssigned: 0,
  critical: 0,
  delayed45: 0,
  reminderCount: 0
})

// ── Helpers ───────────────────────────────────────────────────────
const getTicketAge = (createdAt) =>
  Math.floor((Date.now() - new Date(createdAt).getTime()) / 86400000)

const getDelayBadge = (createdAt, status) => {
  if (status === 'Closed' || status === 'Answered') return null
  const age = getTicketAge(createdAt)
  if (age >= 90) return { label: '90+ gün', color: 'bg-red-900 text-white', pulse: true }
  if (age >= 60) return { label: '60+ gün', color: 'bg-red-600 text-white' }
  if (age >= 45) return { label: '45+ gün', color: 'bg-orange-500 text-white' }
  if (age >= 30) return { label: '30+ gün', color: 'bg-yellow-500 text-yellow-900' }
  return null
}

const getPriorityBadge = (p) => {
  const val = typeof p === 'string' ? { Low:1, Normal:2, Important:3, Critical:4 }[p] || 2 : (p || 2)
  switch(val) {
    case 1: return { label: 'Düşük', color: 'bg-slate-200 text-slate-600 dark:bg-slate-700 dark:text-slate-300' }
    case 3: return { label: 'Önemli', color: 'bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400' }
    case 4: return { label: 'Yüksek', color: 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400', pulse: true }
    default: return { label: 'Normal', color: 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400' }
  }
}

const getStatusColor = (s) => ({
  Submitted: 'bg-blue-100 text-blue-700 dark:bg-blue-500/10 dark:text-blue-400',
  Reviewing: 'bg-amber-100 text-amber-700 dark:bg-amber-500/10 dark:text-amber-400',
  WaitingDepartment: 'bg-purple-100 text-purple-700 dark:bg-purple-500/10 dark:text-purple-400',
  Answered: 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/10 dark:text-emerald-400',
  Closed: 'bg-slate-100 text-slate-500 dark:bg-slate-800 dark:text-slate-400',
}[s] || 'bg-slate-100 text-slate-700')

const getStatusLabel = (s) => i18n.t[`status${s}`] || s

const getCategoryLabel = (c) => i18n.t[c] || i18n.t[`cat${c}`] || c

const isActive = (t) => t.status !== 'Closed'

// ── Load ──────────────────────────────────────────────────────────
const loadTickets = async () => {
  loading.value = true
  try {
    const r = await api.getMyTickets(
      page.value,
      pageSize.value,
      searchTerm.value,
      filterStatus.value || undefined,
      filterOnlyMine.value ? user.id : undefined,
      user.departmentId || undefined,
      hideClosed.value,
      startDate.value,
      endDate.value
    )
    tickets.value = r.data?.data?.items || []
    totalCount.value = r.data?.data?.totalCount || 0

    // Fetch accurate global dashboard stats
    const sr = await api.getMyStats()
    if (sr.data?.data) {
      stats.value = sr.data.data
      stats.value.reminderCount = reminders.value.length
    }
  } catch { toastStore.add('Talepler yüklenemedi', 'error') }
  finally { loading.value = false }
}

const loadReminders = async () => {
  try {
    const r = await api.getReminders()
    reminders.value = r.data?.data || []
    if (stats.value) stats.value.reminderCount = reminders.value.length
  } catch { reminders.value = [] }
}

const dismissReminder = async (id) => {
  try {
    await api.dismissReminder(id)
    reminders.value = reminders.value.filter(r => r.id !== id)
    if (stats.value) stats.value.reminderCount = reminders.value.length
    toastStore.add('Hatırlatıcı kapatıldı', 'success')
  } catch { toastStore.add('Hata oluştu', 'error') }
}

let searchTimeout = null
const handleSearch = () => {
  clearTimeout(searchTimeout)
  searchTimeout = setTimeout(() => { page.value = 1; loadTickets() }, 500)
}

watch([page, pageSize, hideClosed, startDate, endDate], loadTickets)

// ── Client-side sort / filter ─────────────────────────────────────
const sortByRef = ref('desc') // 'asc' = eskiden yeniye | 'desc' = yeniden eskiye (varsayılan)
const filterCategory = ref('')
const filterPriority = ref('')  // '' | '1' | '2' | '3' | '4'
const filterDelay = ref('')     // '' | '30' | '45' | '60' | '90'
const filterOwner = ref('')     // '' | 'assigned' | 'unassigned'
const filterStatusCl = ref('') // client-side status filter

const toggleSortRef = () => {
  // null → asc → desc → null (null = varsayılan, en yeni önce)
  if (sortByRef.value === null) sortByRef.value = 'asc'
  else if (sortByRef.value === 'asc') sortByRef.value = 'desc'
  else sortByRef.value = null
}

const displayedTickets = computed(() => {
  let list = [...tickets.value]

  // Category filter
  if (filterCategory.value !== '') {
    const filterVal = parseInt(filterCategory.value)
    list = list.filter(t => normalizeCategory(t.category) === filterVal)
  }

  // Priority filter
  if (filterPriority.value !== '') {
    const pv = parseInt(filterPriority.value)
    list = list.filter(t => {
      const val = typeof t.priority === 'string'
        ? ({ Low:1, Normal:2, Important:3, Critical:4 }[t.priority] || 2)
        : (t.priority || 2)
      return val === pv
    })
  }

  // Status filter (client-side)
  if (filterStatusCl.value !== '') {
    list = list.filter(t => t.status === filterStatusCl.value)
  }

  // Delay filter
  if (filterDelay.value !== '') {
    const minDays = parseInt(filterDelay.value)
    list = list.filter(t => {
      if (t.status === 'Closed' || t.status === 'Answered') return false
      return getTicketAge(t.createdAt) >= minDays
    })
  }

  // Owner filter
  if (filterOwner.value === 'assigned') list = list.filter(t => !!t.assignedTo)
  else if (filterOwner.value === 'unassigned') list = list.filter(t => !t.assignedTo)

  // Sort by reference no — her zaman aktif
  // 'desc' = en yüksek ref no önce (en yeni talep başta) — varsayılan
  // 'asc'  = en düşük ref no önce (en eski talep başta)
  if (sortByRef.value === 'asc') {
    list.sort((a, b) => (a.referenceNo || '').localeCompare(b.referenceNo || '', undefined, { numeric: true }))
  } else {
    list.sort((a, b) => (b.referenceNo || '').localeCompare(a.referenceNo || '', undefined, { numeric: true }))
  }

  return list
})

// ── DRAWER ───────────────────────────────────────────────────────
const drawerOpen = ref(false)
const drawerTicket = ref(null)
const drawerLoading = ref(false)
const showReminderPopup = ref(false)

const openDrawer = async (ticketId) => {
  drawerOpen.value = true
  drawerLoading.value = true
  drawerTicket.value = null
  try {
    const r = await api.getTicketById(ticketId)
    drawerTicket.value = r.data?.data || null
  } catch {
    toastStore.add('Talep detayı yüklenemedi.', 'error')
    drawerOpen.value = false
  } finally {
    drawerLoading.value = false
  }
}

const closeDrawer = () => {
  drawerOpen.value = false
  drawerTicket.value = null
}

const historyActionLabel = (a) => ({
  Created: 'Oluşturuldu', Transferred: 'Yönlendirildi', Assigned: 'Görevlendirildi',
  StatusChanged: 'Durum Değişti', TakenOwnership: 'İşleme Alındı',
  Closed: 'Kapatıldı', InternalStatusChanged: 'İç Durum Güncellendi'
}[a] || a)

const unifiedTimeline = computed(() => {
  if (!drawerTicket.value) return []
  const events = []
  
  events.push({
    id: 'creation',
    action: 'Talep Oluşturuldu',
    detail: 'Talep sisteme kaydedildi.',
    timestamp: new Date(drawerTicket.value.createdAt),
    author: drawerTicket.value.creatorName || (drawerTicket.value.guestName ? drawerTicket.value.guestName + ' ' + drawerTicket.value.guestSurname : 'Sistem'),
    type: 'create'
  })
  
  if (drawerTicket.value.histories) {
    drawerTicket.value.histories.forEach(h => {
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
        action: finalAction,
        detail: detail,
        timestamp: new Date(h.changedAt),
        author: h.changedBy?.fullName || 'Sistem',
        type: 'history'
      })
    })
  }

  if (drawerTicket.value.replies) {
    drawerTicket.value.replies.forEach(r => {
      events.push({
        id: 'reply-' + r.id,
        action: 'Cevaplandı',
        detail: r.message,
        timestamp: new Date(r.createdAt),
        author: r.authorName || r.createdByName || '🏢 Kurum',
        type: 'reply'
      })
    })
  }
  
  events.sort((a, b) => a.timestamp - b.timestamp)
  return events
})

// ── AKSIYON 1: Talebi Üzerime Al ─────────────────────────────────
const takeOwnership = async (ticket) => {
  setActionLoading(ticket.id, true)
  try {
    const res = await api.takeOwnership(ticket.id)
    if (res.data?.success) {
      toastStore.add('Talep üzerinize alındı', 'success')
      await loadTickets()
      if (drawerOpen.value && drawerTicket.value?.id === ticket.id) await openDrawer(ticket.id)
    } else {
      toastStore.add(res.data?.message || 'Hata oluştu', 'error')
    }
  } catch (e) {
    toastStore.add(e.response?.data?.message || 'Hata oluştu', 'error')
  } finally {
    setActionLoading(ticket.id, false)
  }
}

// ── AKSIYON 2: Cevap Ver ─────────────────────────────────────────
const getRequesterInfo = (ticket) => {
  if (ticket.hidePersonalInfo) return { name: 'Gizli Kimlik', email: '', phone: '', tc: '', title: '', address: '', institution: '' }
  if (ticket.isExternal) return { 
    name: `${ticket.guestName || ''} ${ticket.guestSurname || ''}`.trim() || 'Dış Kullanıcı', 
    email: ticket.guestEmail || '',
    phone: ticket.guestPhone || '',
    tc: ticket.guestIdentityNumber || '',
    title: ticket.guestTitle || '',
    address: ticket.guestAddress || '',
    institution: ticket.institutionName || ''
  }
  if (ticket.creator) return { 
    name: ticket.creator.fullName || `${ticket.creator.firstName || ''} ${ticket.creator.lastName || ''}`.trim() || 'İç Kullanıcı', 
    email: ticket.creator.email || '',
    phone: '',
    tc: '',
    title: '',
    address: '',
    institution: ticket.institutionName || ''
  }
  return { name: 'Bilinmeyen Kullanıcı', email: '', phone: '', tc: '', title: '', address: '', institution: '' }
}

const openReplyModal = (ticket) => {
  const requester = getRequesterInfo(ticket)
  replyModal.value = {
    show: true,
    ticketId: ticket.id,
    ticketTitle: ticket.title,
    ticketDescription: ticket.description || '',
    requesterName: requester.name,
    requesterEmail: requester.email,
    requesterPhone: requester.phone,
    requesterTc: requester.tc,
    requesterTitle: requester.title,
    requesterAddress: requester.address,
    requesterInstitution: requester.institution,
    attachmentUrl: ticket.attachmentUrl || '',
    message: '',
    saving: false
  }
}

const closeReplyModal = () => {
  replyModal.value = { show: false, ticketId: null, ticketTitle: '', ticketDescription: '', requesterName: '', requesterEmail: '', requesterPhone: '', requesterTc: '', requesterTitle: '', requesterAddress: '', requesterInstitution: '', attachmentUrl: '', message: '', saving: false }
}

const submitReply = async () => {
  if (!replyModal.value.message.trim()) return
  replyModal.value.saving = true
  // ticketId'yi modal kapanmadan ÖNCE kaydet
  const repliedTicketId = replyModal.value.ticketId
  const repliedMessage = replyModal.value.message
  try {
    const res = await api.replyTicket(repliedTicketId, repliedMessage)
    if (res.data?.success) {
      toastStore.add('Cevap başarıyla gönderildi.', 'success')
      closeReplyModal() // Bu ticketId'yi sıfırlar, ama biz zaten kopyaladık
      // Çekmece açıksa ve bu taleple ilgiliyse ANİNDE güncelle
      if (drawerOpen.value && drawerTicket.value?.id === repliedTicketId) {
        drawerTicket.value.status = 'Answered'
        // Cevabı da listeye ekle (UI'da hemen göster)
        if (!drawerTicket.value.replies) drawerTicket.value.replies = []
        drawerTicket.value.replies.push({
          id: 'temp-' + Date.now(),
          message: repliedMessage,
          createdAt: new Date().toISOString(),
          authorName: user.fullName || user.name || 'Siz'
        })
        // Arka planda sunucudan güncel veriyi çek (sessizce)
        api.getTicketById(repliedTicketId).then(r => {
          if (r.data?.data && drawerTicket.value?.id === repliedTicketId) {
            drawerTicket.value = r.data.data
          }
        }).catch(() => {})
      }
      await loadTickets()
    } else {
      toastStore.add(res.data?.message || 'Hata oluştu', 'error')
    }
  } catch (e) {
    toastStore.add(e.response?.data?.message || 'Hata oluştu', 'error')
  } finally {
    replyModal.value.saving = false
  }
}

// ── AKSIYON 3: Yönlendir ─────────────────────────────────────────
const openTransferModal = (ticket) => {
  transferModal.value = { show: true, ticketId: ticket.id, currentDepartmentId: ticket.assignedDepartmentId || ticket.departmentId }
}

const onTransferred = () => {
  transferModal.value = { show: false, ticketId: null, currentDepartmentId: null }
  toastStore.add('Talep başarıyla yönlendirildi', 'success')
  loadTickets() // Talep artık bu birimde görünmeyecek
  if (drawerOpen.value) closeDrawer()
}

const closeTransferModal = () => {
  transferModal.value = { show: false, ticketId: null, currentDepartmentId: null }
}

// ── AKSIYON 4: Kapat ─────────────────────────────────────────────
const openCloseModal = (ticket) => {
  if (ticket.status !== 'Answered') {
    showUnansweredModal.value = true
    return
  }
  closeTicketModal.value = { show: true, ticketId: ticket.id, referenceNo: ticket.referenceNo, title: ticket.title }
}

const confirmCloseTicket = async () => {
  const tId = closeTicketModal.value.ticketId
  closeTicketModal.value = { show: false, ticketId: null, referenceNo: '', title: '' }
  setActionLoading(tId, true)
  try {
    const res = await api.closeTicket(tId)
    if (res.data?.success) {
      toastStore.add('Talep kapatıldı.', 'success')
      // Drawer açıksa kapat
      closeDrawer()
      await loadTickets()
    } else {
      toastStore.add(res.data?.message || 'Hata oluştu', 'error')
    }
  } catch (e) {
    toastStore.add(e.response?.data?.message || 'Hata oluştu', 'error')
  } finally {
    setActionLoading(tId, false)
  }
}

const logout = () => { auth.logout(); router.push('/login') }

// ── SIDEBAR NAV ───────────────────────────────────────────
const activeTab = ref('tickets') // 'tickets' | 'reports'

// ── REPORTS ───────────────────────────────────────────────
const allReportTickets = ref([])
const isReportLoading = ref(false)

const rptSearch = ref('')
const rptCategory = ref('')
const rptStatus = ref('')
const rptDelay = ref('')
const rptStartDate = ref('')
const rptEndDate = ref('')
const rptPageSize = ref(20) // Preview limit
const rptPage = ref(1) // Current preview page

const loadAllReportTickets = async () => {
  if (isReportLoading.value) return
  isReportLoading.value = true
  try {
    // Tüm kayıtları alacak kadar büyük bir pageSize gönderilir (Örn: 10000)
    // Sadece atananları (filterOnlyMine) dikkate almıyoruz, departmanın tüm taleplerini alıyoruz
    const r = user.role === 'Admin' || user.role === 'DepartmentManager' 
      ? await api.getAllTickets(1, 10000, '', '', '', user.departmentId)
      : await api.getAllTickets(1, 10000, '', '', '', user.departmentId) // DepartmentManager da dahil
    allReportTickets.value = r.data?.data?.items || []
  } catch (e) {
    toastStore.add('Rapor verileri yüklenirken hata oluştu', 'error')
  } finally {
    isReportLoading.value = false
  }
}

watch(activeTab, (val) => {
  if (val === 'reports') {
    loadAllReportTickets()
  }
})



const normalizeCategory = (c) => {
  if (c === null || c === undefined) return -1
  const s = String(c).toLowerCase()
  if (s === '0' || s === 'complaint') return 0
  if (s === '1' || s === 'suggestion') return 1
  if (s === '2' || s === 'request') return 2
  if (s === '3' || s === 'opinion') return 3
  if (s === '4' || s === 'information') return 4
  return -1
}

const categoryLabel = (c) => {
  const norm = normalizeCategory(c)
  const map = { 0: 'Şikayet', 1: 'Öneri', 2: 'Talep', 3: 'Görüş', 4: 'Bilgi' }
  return map[norm] || '-'
}

const priorityLabel = (p) => {
  const val = typeof p === 'string' ? { Low:1, Normal:2, Important:3, Critical:4 }[p] || 2 : (p || 2)
  return ['', 'Düşük', 'Normal', 'Önemli', 'Yüksek'][val] || 'Normal'
}

const statusLabel = (s) => ({
  Submitted: 'Başvuru Alındı',
  Reviewing: 'İnceleniyor',
  WaitingDepartment: 'İlgili Birimde',
  Answered: 'Cevaplandı',
  Closed: 'Kapatıldı'
}[s] || s)

const reportRows = computed(() => {
  let list = [...allReportTickets.value]

  if (rptSearch.value) {
    const q = rptSearch.value.toLowerCase()
    list = list.filter(t => (t.title || '').toLowerCase().includes(q) || (t.referenceNo || '').toLowerCase().includes(q))
  }
  if (rptCategory.value !== '') {
    const filterVal = parseInt(rptCategory.value)
    list = list.filter(t => normalizeCategory(t.category) === filterVal)
  }
  if (rptStatus.value !== '') {
    list = list.filter(t => t.status === rptStatus.value)
  }
  if (rptStartDate.value) {
    const from = new Date(rptStartDate.value)
    list = list.filter(t => new Date(t.createdAt) >= from)
  }
  if (rptEndDate.value) {
    const to = new Date(rptEndDate.value)
    to.setHours(23, 59, 59)
    list = list.filter(t => new Date(t.createdAt) <= to)
  }
  if (rptDelay.value !== '') {
    const minDays = parseInt(rptDelay.value)
    list = list.filter(t => {
      if (t.status === 'Closed' || t.status === 'Answered') return false
      return getTicketAge(t.createdAt) >= minDays
    })
  }
  // Sort newest first
  list.sort((a, b) => (b.referenceNo || '').localeCompare(a.referenceNo || '', undefined, { numeric: true }))
  return list
})

const buildTableData = () => reportRows.value.map(t => ({
  'Ref No': t.referenceNo || t.id?.substring(0,8).toUpperCase(),
  'Konu': t.title,
  'Kategori': categoryLabel(t.category),
  'Öncelik': priorityLabel(t.priority),
  'Durum': statusLabel(t.status),
  'Sahip': t.assignedTo?.fullName || 'Atanmamış',
  'Tarih': new Date(t.createdAt).toLocaleDateString('tr-TR'),
  'Gecikme (gün)': (t.status === 'Closed' || t.status === 'Answered') ? '-' : String(getTicketAge(t.createdAt)),
}))

const exportXlsx = () => {
  const data = buildTableData()
  if (!data.length) { toastStore.add('Dışa aktaracak veri yok', 'error'); return }
  const ws = XLSX.utils.json_to_sheet(data)
  const wb = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(wb, ws, 'Talepler')
  XLSX.writeFile(wb, `rimer-rapor-${new Date().toISOString().slice(0,10)}.xlsx`)
  toastStore.add('Excel dosyası indirildi', 'success')
}

const exportPdf = async () => {
  const data = buildTableData()
  if (!data.length) { toastStore.add('Dışa aktaracak veri yok', 'error'); return }
  const html2pdf = (await import('html2pdf.js')).default
  const cols = Object.keys(data[0])
  const rows = data.map(r => cols.map(c => r[c]))
  const tableHtml = `
    <div style="font-family:Arial,sans-serif; padding:20px">
      <h2 style="color:#1e293b;margin-bottom:4px">RİMER — Talep Raporu</h2>
      <p style="color:#64748b;font-size:12px;margin-bottom:16px">
        ${user.fullName || user.name} &bull;
        ${new Date().toLocaleDateString('tr-TR')} &bull;
        ${data.length} talep
        ${rptStartDate.value ? '&bull; ' + rptStartDate.value : ''}
        ${rptEndDate.value ? '→ ' + rptEndDate.value : ''}
      </p>
      <table style="width:100%;border-collapse:collapse;font-size:10px">
        <thead>
          <tr style="background:#4f46e5;color:#fff">
            ${cols.map(c => `<th style="padding:6px 8px;text-align:left;white-space:nowrap">${c}</th>`).join('')}
          </tr>
        </thead>
        <tbody>
          ${rows.map((r, i) => `
            <tr style="background:${i%2===0?'#f8fafc':'#fff'}">
              ${r.map(v => `<td style="padding:5px 8px;border-bottom:1px solid #e2e8f0">${v}</td>`).join('')}
            </tr>`).join('')}
        </tbody>
      </table>
    </div>`
  const el = document.createElement('div')
  el.innerHTML = tableHtml
  html2pdf().set({
    margin: 8,
    filename: `rimer-rapor-${new Date().toISOString().slice(0,10)}.pdf`,
    html2canvas: { scale: 2 },
    jsPDF: { unit: 'mm', format: 'a4', orientation: 'landscape' }
  }).from(el).save()
  toastStore.add('PDF hazırlanıyor…', 'success')
}

onMounted(() => { loadTickets(); loadReminders() })
</script>

<template>
<div class="h-screen bg-slate-50 dark:bg-slate-950 flex flex-col overflow-hidden">
  <!-- HEADER -->
  <header class="h-16 bg-white dark:bg-slate-900 border-b border-slate-200 dark:border-slate-800 px-6 flex items-center justify-between sticky top-0 z-40 shadow-sm">
    <div class="flex items-center gap-3">
      <div class="relative h-10 w-28 flex-shrink-0 cursor-pointer" @click="router.push('/dept')">
        <img src="/logo.png" alt="Rimer Logo" class="absolute inset-0 w-full h-full object-contain transition-opacity duration-200" :class="theme.dark ? 'opacity-0' : 'opacity-100'" />
        <img src="/logonight.png" alt="Rimer Logo" class="absolute inset-0 w-full h-full object-contain transition-opacity duration-200" :class="theme.dark ? 'opacity-100' : 'opacity-0'" />
      </div>
      <span class="px-2 py-0.5 bg-purple-600 text-[9px] font-black text-white rounded uppercase">Birim Paneli</span>
    </div>
    <div class="flex items-center gap-3">
      <ReminderBadge />
      <NotificationBell />
      <LangSelector />
      <button @click="theme.toggle()" :title="theme.dark ? 'Açık Mod' : 'Koyu Mod'" class="w-9 h-9 rounded-lg flex items-center justify-center text-sm hover:bg-slate-100 dark:hover:bg-slate-800 transition">{{ theme.dark ? '☀️' : '🌙' }}</button>
      <div class="text-right hidden sm:block">
        <p class="text-xs font-bold text-slate-900 dark:text-white">{{ user.fullName || user.name }}</p>
        <p class="text-[9px] text-slate-500 uppercase">{{ user.role }}</p>
      </div>
      <button @click="logout" class="w-9 h-9 rounded-lg bg-red-50 dark:bg-red-900/20 text-red-500 flex items-center justify-center hover:bg-red-100 transition">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"/></svg>
      </button>
    </div>
  </header>

  <!-- BODY: Sidebar + Content -->
  <div class="flex flex-1 overflow-hidden">

    <!-- LEFT SIDEBAR -->
    <aside class="w-56 shrink-0 bg-white dark:bg-slate-900 border-r border-slate-200 dark:border-slate-800 flex flex-col py-6 px-3 gap-1 shadow-sm">
      <p class="text-[9px] font-black text-slate-400 uppercase px-3 mb-2 tracking-widest">Menü</p>

      <!-- Talepler -->
      <button
        @click="activeTab = 'tickets'"
        class="flex items-center gap-3 w-full px-3 py-2.5 rounded-xl text-sm font-semibold transition-all"
        :class="activeTab === 'tickets'
          ? 'bg-indigo-50 dark:bg-indigo-900/30 text-indigo-700 dark:text-indigo-300 shadow-sm'
          : 'text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800'"
      >
        <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"/>
        </svg>
        Talepler
        <span v-if="totalCount" class="ml-auto text-[10px] bg-indigo-100 dark:bg-indigo-900/50 text-indigo-600 dark:text-indigo-300 rounded-full px-2 py-0.5 font-black">{{ totalCount }}</span>
      </button>

      <!-- Raporlar -->
      <button
        @click="activeTab = 'reports'"
        class="flex items-center gap-3 w-full px-3 py-2.5 rounded-xl text-sm font-semibold transition-all"
        :class="activeTab === 'reports'
          ? 'bg-emerald-50 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-300 shadow-sm'
          : 'text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800'"
      >
        <svg class="w-4 h-4 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
        </svg>
        Raporlar
      </button>

      <div class="flex-1"></div>
      <div class="px-3 pt-4 border-t border-slate-100 dark:border-slate-800">
        <p class="text-[9px] text-slate-400 font-bold uppercase">{{ user.fullName || user.name }}</p>
        <p class="text-[9px] text-slate-400 mt-0.5">{{ user.role }}</p>
      </div>
    </aside>

    <!-- MAIN CONTENT -->
    <main class="flex-1 overflow-y-auto p-4 md:p-6 bg-slate-50 dark:bg-slate-950">

    <!-- ═══ TALEPLER EKRANI ═══ -->
    <div v-show="activeTab === 'tickets'">
    <div class="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-3 mb-6">
      <div class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl p-4 shadow-sm">
        <div class="text-3xl font-black text-blue-600">{{ stats.newTickets }}</div>
        <div class="text-[10px] font-bold text-slate-500 uppercase mt-1">Yeni Talepler</div>
      </div>
      <div class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl p-4 shadow-sm">
        <div class="text-3xl font-black text-amber-600">{{ stats.reviewing }}</div>
        <div class="text-[10px] font-bold text-slate-500 uppercase mt-1">İşlemdeki</div>
      </div>
      <div class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl p-4 shadow-sm">
        <div class="text-3xl font-black text-indigo-600">{{ stats.myAssigned }}</div>
        <div class="text-[10px] font-bold text-slate-500 uppercase mt-1">Bana Atanan</div>
      </div>
      <div class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl p-4 shadow-sm">
        <div class="text-3xl font-black text-red-600" :class="stats.critical > 0 ? 'animate-pulse' : ''">{{ stats.critical }}</div>
        <div class="text-[10px] font-bold text-slate-500 uppercase mt-1">Yüksek</div>
      </div>
      <div class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl p-4 shadow-sm">
        <div class="text-3xl font-black text-orange-600">{{ stats.delayed45 }}</div>
        <div class="text-[10px] font-bold text-slate-500 uppercase mt-1">45+ Gün Geciken</div>
      </div>
      <div class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl p-4 shadow-sm">
        <div class="text-3xl font-black text-purple-600">{{ stats.reminderCount }}</div>
        <div class="text-[10px] font-bold text-slate-500 uppercase mt-1">Hatırlatıcılar</div>
      </div>
    </div>

    <!-- REMINDERS BAR -->
    <div v-if="reminders.length" class="mb-6 bg-amber-50 dark:bg-amber-900/10 border border-amber-200 dark:border-amber-800 rounded-2xl p-4">
      <h4 class="text-xs font-black text-amber-700 dark:text-amber-400 uppercase mb-3">⏰ Aktif Hatırlatıcılar</h4>
      <div class="flex flex-wrap gap-2">
        <div v-for="r in reminders" :key="r.id" class="flex items-center gap-2 bg-white dark:bg-slate-900 border border-amber-200 dark:border-amber-800 rounded-xl px-3 py-2 text-xs">
          <span class="font-bold text-slate-700 dark:text-slate-300">#{{ r.ticketReferenceNo }}</span>
          <span class="text-slate-500">{{ r.ticketTitle }}</span>
          <span class="text-amber-600 font-mono text-[10px]">{{ new Date(r.reminderAt).toLocaleDateString('tr-TR') }}</span>
          <button @click="dismissReminder(r.id)" class="ml-1 text-slate-400 hover:text-red-500 transition">✕</button>
        </div>
      </div>
    </div>

    <!-- FILTERS -->
    <div class="flex flex-wrap items-center gap-3 mb-6 bg-white dark:bg-slate-900 p-4 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm">
      <!-- Arama -->
      <div class="flex-1 min-w-[200px] relative">
        <svg class="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/></svg>
        <input v-model="searchTerm" @input="handleSearch" type="text" placeholder="Talep no veya konu ara…"
          class="w-full pl-10 pr-4 py-2.5 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-sm text-slate-700 dark:text-slate-300 outline-none focus:ring-2 focus:ring-indigo-500 transition-all"/>
      </div>
      
      <!-- Sadece bana atananlar -->
      <div class="flex items-center gap-2 px-4 py-2 bg-slate-50 dark:bg-slate-800 rounded-xl border border-slate-200 dark:border-slate-700">
        <span class="text-[10px] font-black text-slate-500 uppercase">Sadece Bana Atananlar</span>
        <button 
          @click="filterOnlyMine = !filterOnlyMine; page = 1; loadTickets()"
          class="w-10 h-5 rounded-full relative transition-colors"
          :class="filterOnlyMine ? 'bg-indigo-600' : 'bg-slate-300 dark:bg-slate-600'"
        >
          <div class="absolute top-1 w-3 h-3 bg-white rounded-full transition-all" :class="filterOnlyMine ? 'left-6' : 'left-1'"></div>
        </button>
      </div>

      <!-- Tarih aralığı -->
      <div class="flex items-center gap-2 bg-slate-50 dark:bg-slate-800 px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 w-full md:w-auto overflow-x-auto">
        <input type="date" v-model="startDate" @change="page = 1; loadTickets()" class="bg-transparent border-none outline-none text-xs text-slate-700 dark:text-slate-300 font-medium w-28" />
        <span class="text-slate-400 text-xs">-</span>
        <input type="date" v-model="endDate" @change="page = 1; loadTickets()" class="bg-transparent border-none outline-none text-xs text-slate-700 dark:text-slate-300 font-medium w-28" />
      </div>

      <!-- Kapananları gizle -->
      <label class="flex items-center gap-2 cursor-pointer px-4 py-2 bg-slate-50 dark:bg-slate-800 rounded-xl border border-slate-200 dark:border-slate-700 transition-colors hover:bg-slate-100 dark:hover:bg-slate-700">
        <input type="checkbox" v-model="hideClosed" class="w-4 h-4 rounded border-slate-300 text-indigo-600 focus:ring-indigo-500 cursor-pointer" />
        <span class="text-[10px] font-black text-slate-500 uppercase">Kapananları Gizle</span>
      </label>
    </div>

    <!-- TICKET TABLE -->
    <div class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl shadow-sm flex flex-col">
      <div v-if="loading" class="p-6 space-y-3"><div v-for="i in 5" :key="i" class="h-14 bg-slate-100 dark:bg-slate-800 rounded-xl animate-pulse"/></div>
      <div v-else class="overflow-x-auto">
        <table class="w-full text-left">
          <thead class="bg-slate-50 dark:bg-slate-800/90">
            <tr>
              <!-- Başvuru No: tıklanabilir sort (her zaman aktif, varsayılan: en yeni üstte) -->
              <th
                @click="toggleSortRef"
                class="px-4 py-3 text-[10px] font-bold text-slate-400 uppercase border-b border-slate-200 dark:border-slate-700 cursor-pointer select-none hover:text-indigo-500 dark:hover:text-indigo-400 transition-colors group"
                :title="sortByRef === 'asc' ? 'Şu an: Eskiden Yeniye — Tıkla: Yeniden Eskiye' : 'Şu an: Yeniden Eskiye (Varsayılan) — Tıkla: Eskiden Yeniye'"
              >
                <span class="flex items-center gap-1">
                  Başvuru / Referans No
                  <span class="text-indigo-500 dark:text-indigo-400 transition-all">
                    {{ sortByRef === 'asc' ? '↑ Eski→Yeni' : '↓ Yeni→Eski' }}
                  </span>
                </span>
              </th>

              <!-- Konu: sabit -->
              <th class="px-4 py-3 text-[10px] font-bold text-slate-400 uppercase border-b border-slate-200 dark:border-slate-700">Konu</th>

              <!-- Kategori: inline filtre -->
              <th class="px-3 py-2 border-b border-slate-200 dark:border-slate-700">
                <div class="flex flex-col gap-1">
                  <span class="text-[10px] font-bold text-slate-400 uppercase">Kategori</span>
                  <select
                    v-model="filterCategory"
                    @click.stop
                    class="text-[10px] font-bold rounded-lg px-1.5 py-1 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-300 outline-none focus:ring-1 focus:ring-purple-400 cursor-pointer transition-all"
                    :class="filterCategory ? 'border-purple-400 text-purple-600 dark:text-purple-400' : ''"
                  >
                    <option value="">Tamamı</option>
                    <option value="0">⚠️ Şikayet</option>
                    <option value="1">💡 Öneri</option>
                    <option value="2">🛠️ Talep</option>
                    <option value="3">⭐ Görüş</option>
                    <option value="4">❓ Bilgi</option>
                  </select>
                </div>
              </th>

              <!-- Öncelik: inline filtre -->
              <th class="px-3 py-2 border-b border-slate-200 dark:border-slate-700">
                <div class="flex flex-col gap-1">
                  <span class="text-[10px] font-bold text-slate-400 uppercase">Öncelik</span>
                  <select
                    v-model="filterPriority"
                    @click.stop
                    class="text-[10px] font-bold rounded-lg px-1.5 py-1 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-300 outline-none focus:ring-1 focus:ring-indigo-400 cursor-pointer transition-all"
                    :class="filterPriority ? 'border-indigo-400 text-indigo-600 dark:text-indigo-400' : ''"
                  >
                    <option value="">Tamamı</option>
                    <option value="1">Düşük</option>
                    <option value="2">Normal</option>
                    <option value="3">Önemli</option>
                    <option value="4">Yüksek</option>
                  </select>
                </div>
              </th>

              <!-- Durum: inline filtre (client-side) -->
              <th class="px-3 py-2 border-b border-slate-200 dark:border-slate-700">
                <div class="flex flex-col gap-1">
                  <span class="text-[10px] font-bold text-slate-400 uppercase">Durum</span>
                  <select
                    v-model="filterStatusCl"
                    @click.stop
                    class="text-[10px] font-bold rounded-lg px-1.5 py-1 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-300 outline-none focus:ring-1 focus:ring-blue-400 cursor-pointer transition-all"
                    :class="filterStatusCl ? 'border-blue-400 text-blue-600 dark:text-blue-400' : ''"
                  >
                    <option value="">Tamamı</option>
                    <option value="Submitted">Başvuru Alındı</option>
                    <option value="Reviewing">İnceleniyor</option>
                    <option value="WaitingDepartment">İlgili Birimde</option>
                    <option value="Answered">Cevaplandı</option>
                    <option value="Closed">Kapatıldı</option>
                  </select>
                </div>
              </th>

              <!-- Sahip: inline filtre -->
              <th class="px-3 py-2 border-b border-slate-200 dark:border-slate-700">
                <div class="flex flex-col gap-1">
                  <span class="text-[10px] font-bold text-slate-400 uppercase">Sahip</span>
                  <select
                    v-model="filterOwner"
                    @click.stop
                    class="text-[10px] font-bold rounded-lg px-1.5 py-1 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-300 outline-none focus:ring-1 focus:ring-emerald-400 cursor-pointer transition-all"
                    :class="filterOwner ? 'border-emerald-400 text-emerald-600 dark:text-emerald-400' : ''"
                  >
                    <option value="">Tamamı</option>
                    <option value="assigned">👤 Atanmış</option>
                    <option value="unassigned">— Atanmamış</option>
                  </select>
                </div>
              </th>

              <!-- Tarih: sabit -->
              <th class="px-4 py-3 text-[10px] font-bold text-slate-400 uppercase border-b border-slate-200 dark:border-slate-700">Tarih</th>

              <!-- Gecikme: inline filtre -->
              <th class="px-3 py-2 border-b border-slate-200 dark:border-slate-700">
                <div class="flex flex-col gap-1">
                  <span class="text-[10px] font-bold text-slate-400 uppercase">Gecikme</span>
                  <select
                    v-model="filterDelay"
                    @click.stop
                    class="text-[10px] font-bold rounded-lg px-1.5 py-1 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-300 outline-none focus:ring-1 focus:ring-orange-400 cursor-pointer transition-all"
                    :class="filterDelay ? 'border-orange-400 text-orange-600 dark:text-orange-400' : ''"
                  >
                    <option value="">Tamamı</option>
                    <option value="30">30+ Gün</option>
                    <option value="45">45+ Gün</option>
                    <option value="60">60+ Gün</option>
                    <option value="90">90+ Gün</option>
                  </select>
                </div>
              </th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
            <tr 
              v-for="t in displayedTickets" 
              :key="t.id" 
              @click="openDrawer(t.id)"
              class="hover:bg-slate-50 dark:hover:bg-slate-800/30 transition-colors cursor-pointer"
              :class="[
                (t.priority === 4 || t.priority === 'Critical') ? 'border-l-4 border-l-red-500' : '',
                !isActive(t) ? 'opacity-60' : ''
              ]"
            >
              <!-- Başvuru No -->
              <td class="px-4 py-3 font-mono text-xs text-slate-500 whitespace-nowrap">#{{ t.referenceNo || t.id.substring(0,8).toUpperCase() }}</td>

              <!-- Konu -->
              <td class="px-4 py-3">
                <div class="text-sm font-semibold text-slate-900 dark:text-white max-w-[180px] truncate" :title="t.title">{{ t.title }}</div>
              </td>

              <!-- Kategori -->
              <td class="px-4 py-3">
                <span class="px-2 py-0.5 rounded bg-slate-100 dark:bg-slate-800 text-[10px] font-bold text-slate-600 dark:text-slate-300 uppercase tracking-tight whitespace-nowrap">
                  {{ getCategoryLabel(t.category) }}
                </span>
              </td>

              <!-- Öncelik -->
              <td class="px-4 py-3">
                <span class="px-2 py-0.5 rounded text-[10px] font-black uppercase tracking-wider" :class="[getPriorityBadge(t.priority).color, getPriorityBadge(t.priority).pulse ? 'animate-pulse' : '']">
                  {{ getPriorityBadge(t.priority).label }}
                </span>
              </td>

              <!-- Durum -->
              <td class="px-4 py-3 whitespace-nowrap">
                <span class="px-2.5 py-1 rounded-lg text-[10px] font-black uppercase tracking-wider border" :class="getStatusColor(t.status)">
                  {{ getStatusLabel(t.status) }}
                </span>
              </td>

              <!-- Sahip -->
              <td class="px-4 py-3 whitespace-nowrap">
                <div v-if="t.assignedTo" class="inline-flex items-center gap-1.5 px-2 py-1 bg-indigo-50 dark:bg-indigo-900/30 border border-indigo-100 dark:border-indigo-800/50 rounded-lg text-[10px] font-bold text-indigo-700 dark:text-indigo-300">
                  <span>👤</span> {{ t.assignedTo.fullName }}
                </div>
                <span v-else class="text-[10px] font-bold text-slate-400 italic">Atanmamış</span>
              </td>

              <!-- Tarih -->
              <td class="px-4 py-3 text-xs text-slate-500 font-medium whitespace-nowrap">
                {{ new Date(t.createdAt).toLocaleDateString('tr-TR') }}
              </td>

              <!-- Gecikme -->
              <td class="px-4 py-3">
                <span v-if="getDelayBadge(t.createdAt, t.status)" class="px-2 py-0.5 rounded text-[10px] font-black whitespace-nowrap" :class="[getDelayBadge(t.createdAt, t.status).color, getDelayBadge(t.createdAt, t.status).pulse ? 'animate-pulse' : '']">
                  {{ getDelayBadge(t.createdAt, t.status).label }}
                </span>
              </td>
            </tr>

            <tr v-if="!loading && displayedTickets.length === 0">
              <td colspan="8" class="px-4 py-16 text-center text-sm text-slate-500">
                <div class="text-4xl mb-3">📭</div>
                <span v-if="filterCategory || filterOwner || filterDelay">Seçili filtrelere uyan talep bulunamadı.</span>
                <span v-else>Biriminize atanmış talep bulunmuyor.</span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <div class="p-4 border-t border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 rounded-b-2xl">
        <Pagination v-model:page="page" v-model:pageSize="pageSize" :totalCount="totalCount" />
      </div>
    </div>
    </div> <!-- end Talepler ekranı -->

    <!-- ═══ RAPORLAR EKRANI ═══ -->
    <div v-show="activeTab === 'reports'">
      <!-- Başlık + Export Butonları -->
      <div class="flex flex-wrap items-center justify-between gap-4 mb-6">
        <div>
          <h2 class="text-xl font-black text-slate-900 dark:text-white">Rapor Oluştur</h2>
          <p class="text-xs text-slate-500 mt-0.5">Filtreleri uygulayın, ardından xlsx veya PDF olarak indirin</p>
        </div>
        <div class="flex items-center gap-3">
          <button @click="exportXlsx" class="flex items-center gap-2 px-5 py-2.5 bg-emerald-600 hover:bg-emerald-700 text-white rounded-xl font-bold text-sm shadow-lg shadow-emerald-600/20 transition-all active:scale-95">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"/></svg>
            Excel (.xlsx)
          </button>
          <button @click="exportPdf" class="flex items-center gap-2 px-5 py-2.5 bg-rose-600 hover:bg-rose-700 text-white rounded-xl font-bold text-sm shadow-lg shadow-rose-600/20 transition-all active:scale-95">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z"/></svg>
            PDF
          </button>
        </div>
      </div>

      <!-- Filtreler -->
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 mb-6 bg-white dark:bg-slate-900 p-5 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm">
        <!-- Konu arama -->
        <div class="relative">
          <label class="block text-[10px] font-black text-slate-500 uppercase mb-1.5">Konu / Ref No</label>
          <div class="relative">
            <svg class="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/></svg>
            <input v-model="rptSearch" type="text" placeholder="Konu veya referans no…" class="w-full pl-10 pr-4 py-2.5 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-sm text-slate-700 dark:text-slate-300 outline-none focus:ring-2 focus:ring-indigo-400" />
          </div>
        </div>

        <!-- Kategori -->
        <div>
          <label class="block text-[10px] font-black text-slate-500 uppercase mb-1.5">Kategori</label>
          <select v-model="rptCategory" class="w-full px-4 py-2.5 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-sm text-slate-700 dark:text-slate-300 outline-none focus:ring-2 focus:ring-purple-400">
            <option value="">Tüm Kategoriler</option>
            <option value="0">⚠️ Şikayet</option>
            <option value="1">💡 Öneri</option>
            <option value="2">🛠️ Talep</option>
            <option value="3">⭐ Görüş</option>
            <option value="4">❓ Bilgi</option>
          </select>
        </div>

        <!-- Durum -->
        <div>
          <label class="block text-[10px] font-black text-slate-500 uppercase mb-1.5">Durum</label>
          <select v-model="rptStatus" class="w-full px-4 py-2.5 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-sm text-slate-700 dark:text-slate-300 outline-none focus:ring-2 focus:ring-blue-400">
            <option value="">Tüm Durumlar</option>
            <option value="Submitted">Başvuru Alındı</option>
            <option value="Reviewing">İnceleniyor</option>
            <option value="WaitingDepartment">İlgili Birimde</option>
            <option value="Answered">Cevaplandı</option>
            <option value="Closed">Kapatıldı</option>
          </select>
        </div>

        <!-- Tarih aralığı -->
        <div>
          <label class="block text-[10px] font-black text-slate-500 uppercase mb-1.5">Başlangıç Tarihi</label>
          <input type="date" v-model="rptStartDate" class="w-full px-4 py-2.5 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-sm text-slate-700 dark:text-slate-300 outline-none focus:ring-2 focus:ring-indigo-400" />
        </div>
        <div>
          <label class="block text-[10px] font-black text-slate-500 uppercase mb-1.5">Bitiş Tarihi</label>
          <input type="date" v-model="rptEndDate" class="w-full px-4 py-2.5 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-sm text-slate-700 dark:text-slate-300 outline-none focus:ring-2 focus:ring-indigo-400" />
        </div>

        <!-- Gecikme -->
        <div>
          <label class="block text-[10px] font-black text-slate-500 uppercase mb-1.5">Min. Gecikme</label>
          <select v-model="rptDelay" class="w-full px-4 py-2.5 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-sm text-slate-700 dark:text-slate-300 outline-none focus:ring-2 focus:ring-orange-400">
            <option value="">Gecikme Filtresi Yok</option>
            <option value="30">30+ Gün Geciken</option>
            <option value="45">45+ Gün Geciken</option>
            <option value="60">60+ Gün Geciken</option>
            <option value="90">90+ Gün Geciken</option>
          </select>
        </div>
      </div>

      <!-- Önizleme Tablosu -->
      <div v-if="isReportLoading" class="flex flex-col items-center justify-center p-12 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl shadow-sm">
        <span class="text-4xl animate-spin mb-4">⏳</span>
        <p class="text-sm font-bold text-slate-600 dark:text-slate-400">Rapor verileri hazırlanıyor, lütfen bekleyin...</p>
      </div>
      <div v-else class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-2xl shadow-sm overflow-hidden">
        <div class="px-5 py-3 border-b border-slate-100 dark:border-slate-800 flex items-center justify-between">
          <span class="text-xs font-black text-slate-500 uppercase flex items-center gap-2">
            Önizleme 
            <select v-model.number="rptPageSize" class="bg-slate-100 dark:bg-slate-800 border-none rounded text-[10px] font-bold text-slate-600 dark:text-slate-300 py-1 pl-2 pr-6 outline-none cursor-pointer hover:bg-slate-200 dark:hover:bg-slate-700 transition">
              <option :value="10">10 Göster</option>
              <option :value="20">20 Göster</option>
              <option :value="50">50 Göster</option>
              <option :value="100">100 Göster</option>
            </select>
          </span>
          <span class="text-xs font-bold text-indigo-600 dark:text-indigo-400">{{ reportRows.length }} talep bulundu</span>
        </div>
        <div class="overflow-x-auto">
          <table class="w-full text-left text-xs">
            <thead class="bg-slate-50 dark:bg-slate-800/80">
              <tr>
                <th class="px-4 py-2.5 text-[10px] font-black text-slate-400 uppercase">Ref No</th>
                <th class="px-4 py-2.5 text-[10px] font-black text-slate-400 uppercase">Konu</th>
                <th class="px-4 py-2.5 text-[10px] font-black text-slate-400 uppercase">Kategori</th>
                <th class="px-4 py-2.5 text-[10px] font-black text-slate-400 uppercase">Durum</th>
                <th class="px-4 py-2.5 text-[10px] font-black text-slate-400 uppercase">Sahip</th>
                <th class="px-4 py-2.5 text-[10px] font-black text-slate-400 uppercase">Tarih</th>
                <th class="px-4 py-2.5 text-[10px] font-black text-slate-400 uppercase">Gecikme</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
              <tr v-for="t in reportRows.slice((rptPage - 1) * rptPageSize, rptPage * rptPageSize)" :key="t.id" class="hover:bg-slate-50 dark:hover:bg-slate-800/30 transition-colors">
                <td class="px-4 py-2.5 font-mono text-slate-500 whitespace-nowrap">#{{ t.referenceNo }}</td>
                <td class="px-4 py-2.5 font-semibold text-slate-800 dark:text-slate-200 max-w-[160px] truncate" :title="t.title">{{ t.title }}</td>
                <td class="px-4 py-2.5"><span class="px-2 py-0.5 rounded bg-slate-100 dark:bg-slate-800 font-bold text-slate-600 dark:text-slate-300 uppercase text-[9px]">{{ categoryLabel(t.category) }}</span></td>
                <td class="px-4 py-2.5 whitespace-nowrap"><span class="px-2 py-0.5 rounded-lg font-black text-[9px] uppercase" :class="getStatusColor(t.status)">{{ statusLabel(t.status) }}</span></td>
                <td class="px-4 py-2.5 text-slate-500 whitespace-nowrap">{{ t.assignedTo?.fullName || '—' }}</td>
                <td class="px-4 py-2.5 text-slate-500 whitespace-nowrap">{{ new Date(t.createdAt).toLocaleDateString('tr-TR') }}</td>
                <td class="px-4 py-2.5">
                  <span v-if="getDelayBadge(t.createdAt, t.status)" class="px-1.5 py-0.5 rounded font-black text-[9px] whitespace-nowrap" :class="[getDelayBadge(t.createdAt, t.status).color, getDelayBadge(t.createdAt, t.status).pulse ? 'animate-pulse' : '']">{{ getDelayBadge(t.createdAt, t.status).label }}</span>
                  <span v-else class="text-slate-400 text-[9px]">—</span>
                </td>
              </tr>
              <tr v-if="reportRows.length === 0">
                <td colspan="7" class="px-4 py-10 text-center text-sm text-slate-400">
                  <div class="text-3xl mb-2">📊</div>
                  Seçili filtrelere uygun talep bulunamadı.
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <!-- Raporlar Pagination -->
        <div v-if="reportRows.length > 0" class="p-4 border-t border-slate-100 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-900/50">
          <Pagination v-model:page="rptPage" v-model:pageSize="rptPageSize" :totalCount="reportRows.length" />
        </div>
      </div>
    </div> <!-- end Raporlar ekranı -->

    </main>
  </div>

<!-- ════════════════════════════════════════════════════════════ -->
<!-- TICKET DETAIL DRAWER                                        -->
<!-- ════════════════════════════════════════════════════════════ -->

<Transition name="fade">
  <div v-if="drawerOpen" @click="closeDrawer" class="fixed inset-0 bg-black/50 backdrop-blur-sm z-[60]" />
</Transition>

<Transition name="slide-right">
  <aside v-if="drawerOpen" class="fixed right-0 top-0 h-full w-full max-w-2xl bg-white dark:bg-slate-900 border-l border-slate-200 dark:border-slate-800 z-[70] flex flex-col shadow-2xl">

    <!-- Drawer Header -->
    <div class="flex items-center justify-between px-6 py-4 border-b border-slate-200 dark:border-slate-800 flex-shrink-0 bg-slate-50 dark:bg-slate-800/50">
      <div class="flex items-center gap-3">
        <span class="text-lg font-black text-slate-900 dark:text-white">Talep Detayı</span>
        <span v-if="drawerTicket" class="font-mono text-xs text-slate-400">#{{ drawerTicket.referenceNo }}</span>
      </div>
      <div class="flex items-center gap-2">
        <button v-if="drawerTicket" @click="showReminderPopup = true" class="w-8 h-8 flex items-center justify-center rounded-lg bg-indigo-100 dark:bg-indigo-900/30 text-indigo-600 dark:text-indigo-400 hover:bg-indigo-200 dark:hover:bg-indigo-900/50 transition font-black text-lg shadow-sm tooltip-trigger relative" title="Hatırlatıcı Ekle">
          <span class="text-[14px]">ℹ️</span>
        </button>
        <button @click="closeDrawer" class="w-8 h-8 flex items-center justify-center rounded-lg bg-slate-200 dark:bg-slate-700 hover:bg-slate-300 dark:hover:bg-slate-600 transition text-slate-600 dark:text-slate-300 font-black text-lg">×</button>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="drawerLoading" class="flex-1 p-6 space-y-4">
      <div v-for="i in 6" :key="i" class="h-12 bg-slate-100 dark:bg-slate-800 rounded-xl animate-pulse" />
    </div>

    <!-- Content -->
    <div v-else-if="drawerTicket" class="flex-1 overflow-y-auto p-6 space-y-6">

      <!-- Status + Priority + Date -->
      <div class="flex flex-wrap items-center gap-3">
        <span class="px-3 py-1.5 rounded-xl text-xs font-black uppercase tracking-wider border" :class="getStatusColor(drawerTicket.status)">{{ getStatusLabel(drawerTicket.status) }}</span>
        <span class="px-3 py-1.5 rounded-xl text-xs font-black uppercase tracking-wider" :class="getPriorityBadge(drawerTicket.priority).color">{{ getPriorityBadge(drawerTicket.priority).label }}</span>
        <span class="ml-auto text-xs text-slate-400">{{ new Date(drawerTicket.createdAt).toLocaleString('tr-TR') }}</span>
      </div>

      <!-- Title + Description -->
      <div class="space-y-3">
        <div>
          <p class="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-1">Konu</p>
          <p class="text-base font-black text-slate-900 dark:text-white leading-snug">{{ drawerTicket.title }}</p>
        </div>
        <div class="bg-slate-50 dark:bg-slate-800/60 p-4 rounded-2xl border border-slate-200 dark:border-slate-700">
          <p class="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2">📝 Talep İçeriği</p>
          <p class="text-sm text-slate-700 dark:text-slate-300 leading-relaxed whitespace-pre-wrap">{{ drawerTicket.description }}</p>
        </div>
      </div>

      <!-- Talebe Cevap(lar) -->
      <div v-if="drawerTicket.replies && drawerTicket.replies.length" class="space-y-3">
        <p class="text-[10px] font-black text-emerald-600 dark:text-emerald-400 uppercase tracking-widest flex items-center gap-2">
          <span class="w-5 h-5 bg-emerald-100 dark:bg-emerald-900/40 rounded-full flex items-center justify-center text-[10px]">💬</span>
          Talebe Cevap
        </p>
        <div
          v-for="reply in drawerTicket.replies"
          :key="reply.id"
          class="bg-emerald-50 dark:bg-emerald-900/10 border border-emerald-200 dark:border-emerald-800/50 rounded-2xl p-4"
        >
          <p class="text-sm text-slate-700 dark:text-slate-300 leading-relaxed whitespace-pre-wrap mb-3">{{ reply.message }}</p>
          <div class="flex items-center justify-between pt-2 border-t border-emerald-100 dark:border-emerald-800/40">
            <span class="text-[10px] font-black text-emerald-700 dark:text-emerald-400 uppercase tracking-wider flex items-center gap-1">
              👤 {{ reply.authorName || reply.createdByName || '🏢 Kurum' }}
            </span>
            <time class="text-[10px] text-slate-400 font-mono">
              {{ new Date(reply.createdAt).toLocaleString('tr-TR') }}
            </time>
          </div>
        </div>
      </div>

      <!-- Attachment -->
      <div v-if="drawerTicket.attachmentUrl">
        <p class="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2">📎 Ek Dosya</p>
        <a :href="drawerTicket.attachmentUrl" target="_blank" class="inline-flex items-center gap-2 px-4 py-2 rounded-xl bg-indigo-50 dark:bg-indigo-900/20 border border-indigo-200 dark:border-indigo-800 text-sm font-bold text-indigo-600 dark:text-indigo-400 hover:bg-indigo-100 transition">📎 Eki Görüntüle</a>
      </div>

      <!-- Applicant -->
      <div class="bg-slate-50 dark:bg-slate-800/40 rounded-2xl border border-slate-200 dark:border-slate-700 p-4">
        <p class="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-3">👤 Başvuru Sahibi</p>
        <div v-if="drawerTicket.isExternal && drawerTicket.hidePersonalInfo" class="flex items-center gap-3 p-3 bg-slate-100 dark:bg-slate-700/50 rounded-xl border border-slate-200 dark:border-slate-600">
          <span class="text-2xl">🔒</span>
          <div>
            <p class="text-xs font-black text-slate-600 dark:text-slate-300 uppercase tracking-widest">Kimliği Gizli Başvuru</p>
            <p class="text-[11px] text-slate-400 mt-0.5">Başvuru sahibi gizlilik talebinde bulunmuştur.</p>
          </div>
        </div>
        <div v-else class="grid grid-cols-2 gap-3">
          <div>
            <p class="text-[9px] font-black text-slate-400 uppercase mb-0.5">Ad Soyad</p>
            <p class="text-sm font-bold text-slate-700 dark:text-slate-300">
              <template v-if="drawerTicket.isExternal">{{ drawerTicket.guestName }} {{ drawerTicket.guestSurname }}</template>
              <template v-else>{{ drawerTicket.creator?.fullName || '—' }}</template>
            </p>
          </div>
          <div>
            <p class="text-[9px] font-black text-slate-400 uppercase mb-0.5">E-Posta</p>
            <p class="text-sm font-bold text-slate-700 dark:text-slate-300 break-all">{{ drawerTicket.isExternal ? drawerTicket.guestEmail : (drawerTicket.creator?.email || '—') }}</p>
          </div>
          <div v-if="drawerTicket.isExternal ? drawerTicket.guestPhone : drawerTicket.creator?.phoneNumber">
            <p class="text-[9px] font-black text-slate-400 uppercase mb-0.5">Telefon</p>
            <p class="text-sm font-bold text-slate-700 dark:text-slate-300">{{ drawerTicket.isExternal ? drawerTicket.guestPhone : drawerTicket.creator?.phoneNumber }}</p>
          </div>
          <div v-if="drawerTicket.isExternal && drawerTicket.guestAddress" class="col-span-2">
            <p class="text-[9px] font-black text-slate-400 uppercase mb-0.5">Adres</p>
            <p class="text-sm font-bold text-slate-700 dark:text-slate-300 leading-relaxed">{{ drawerTicket.guestAddress }}</p>
          </div>
        </div>
      </div>

      <!-- Unified Timeline -->
      <div v-if="unifiedTimeline && unifiedTimeline.length">
        <p class="text-[10px] font-black text-slate-400 uppercase tracking-widest mb-3">📜 İşlem Geçmişi</p>
        <div class="relative space-y-4 before:absolute before:left-4 before:top-2 before:bottom-2 before:w-0.5 before:bg-slate-200 dark:before:bg-slate-700">
          <div
            v-for="event in unifiedTimeline"
            :key="event.id"
            class="relative pl-11"
          >
            <div class="absolute left-0 top-1 w-8 h-8 rounded-full flex items-center justify-center border-2 border-white dark:border-slate-900 z-10 shadow-sm"
              :class="event.type === 'create' ? 'bg-blue-500' : event.type === 'reply' ? 'bg-emerald-500' : event.action.includes('Yönlendirildi') ? 'bg-amber-500' : event.action.includes('Kapatıldı') ? 'bg-slate-500' : 'bg-indigo-500'">
              <span class="text-[10px] text-white">{{ event.type === 'create' ? '✓' : event.type === 'reply' ? '💬' : event.action.includes('Yönlendirildi') ? '→' : event.action.includes('Kapatıldı') ? '🔒' : '•' }}</span>
            </div>
            <div class="bg-slate-50 dark:bg-slate-800/60 border border-slate-100 dark:border-slate-700 rounded-xl p-3"
                 :class="{'border-emerald-200 dark:border-emerald-800 bg-emerald-50 dark:bg-emerald-900/10': event.type === 'reply'}">
              <div class="flex items-start justify-between gap-2">
                <div class="flex-1 min-w-0">
                  <p class="text-xs font-black text-slate-900 dark:text-white" :class="{'text-emerald-700 dark:text-emerald-400': event.type === 'reply'}">
                    {{ event.action }}
                  </p>
                  <p class="text-[11px] text-slate-500 dark:text-slate-400 mt-1" :class="{'italic': event.type !== 'reply', 'whitespace-pre-wrap font-medium text-slate-700 dark:text-slate-300': event.type === 'reply'}">
                    {{ event.detail }}
                  </p>
                  <p class="text-[10px] text-slate-400 mt-2 font-bold">{{ event.author }}</p>
                </div>
                <time class="text-[10px] text-slate-400 whitespace-nowrap flex-shrink-0">{{ event.timestamp.toLocaleString('tr-TR') }}</time>
              </div>
            </div>
          </div>
        </div>
      </div>

    </div>

    <!-- Drawer Footer Actions -->
    <div class="p-6 border-t border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-800/50 flex flex-wrap gap-3 mt-auto shrink-0">

      <!-- Üzerime Al: Kapalı veya Cevaplandı değilse ve sahip değilse -->
      <button
        v-if="drawerTicket && drawerTicket.status !== 'Closed' && drawerTicket.status !== 'Answered' && (!drawerTicket.assignedTo || drawerTicket.assignedTo.id !== user.id)"
        @click="takeOwnership(drawerTicket)"
        :disabled="actionLoading[drawerTicket?.id]"
        class="flex-1 min-w-[120px] inline-flex items-center justify-center gap-2 px-4 py-3 rounded-xl bg-indigo-600 hover:bg-indigo-700 text-white text-xs font-black transition-all active:scale-95 disabled:opacity-50 shadow-sm"
      >
        <span v-if="actionLoading[drawerTicket?.id]" class="animate-spin">⏳</span>
        <span v-else>✋</span>
        Üzerime Al
      </button>

      <!-- Cevap Ver: Kapalı veya zaten Cevaplandı değilse -->
      <button
        v-if="drawerTicket && drawerTicket.status !== 'Closed' && drawerTicket.status !== 'Answered'"
        @click="openReplyModal(drawerTicket)"
        :disabled="actionLoading[drawerTicket?.id]"
        class="flex-1 min-w-[120px] inline-flex items-center justify-center gap-2 px-4 py-3 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-black transition-all active:scale-95 disabled:opacity-50 shadow-sm"
      >
        💬 Cevap Ver
      </button>

      <!-- Yönlendir: Kapalı veya Cevaplandı değilse -->
      <button
        v-if="drawerTicket && drawerTicket.status !== 'Closed' && drawerTicket.status !== 'Answered'"
        @click="openTransferModal(drawerTicket)"
        :disabled="actionLoading[drawerTicket?.id]"
        class="flex-1 min-w-[120px] inline-flex items-center justify-center gap-2 px-4 py-3 rounded-xl bg-amber-500 hover:bg-amber-600 text-white text-xs font-black transition-all active:scale-95 disabled:opacity-50 shadow-sm"
      >
        🔄 Yönlendir
      </button>

      <!-- Kapat: Yalnızca Cevaplandı ise göster -->
      <button
        v-if="drawerTicket && drawerTicket.status === 'Answered'"
        @click="openCloseModal(drawerTicket)"
        :disabled="actionLoading[drawerTicket?.id]"
        class="flex-1 min-w-[120px] inline-flex items-center justify-center gap-2 px-4 py-3 rounded-xl bg-rose-500 hover:bg-rose-600 text-white text-xs font-black transition-all active:scale-95 disabled:opacity-50 shadow-sm"
      >
        🔒 Kapat
      </button>

      <!-- Hiçbir buton görünmüyorsa (Kapalı talep) -->
      <span
        v-if="drawerTicket && drawerTicket.status === 'Closed'"
        class="w-full text-center px-4 py-2 bg-slate-200 dark:bg-slate-700 text-slate-500 dark:text-slate-400 rounded-lg text-xs font-black uppercase tracking-wider"
      >
        Bu Talep Kapatılmıştır
      </span>

    </div>

  </aside>
</Transition>

<!-- ═══ MODAL: CEVAP VER ═══ -->
<Transition name="modal">
  <div v-if="replyModal.show" class="fixed inset-0 z-[80] flex items-center justify-center p-4">
    <div class="fixed inset-0 bg-black/60 backdrop-blur-sm" @click="closeReplyModal"></div>
    <div class="relative w-full max-w-lg bg-white dark:bg-slate-900 rounded-3xl shadow-2xl border border-slate-200 dark:border-slate-700 overflow-hidden">
      <!-- Header -->
      <div class="px-6 py-5 border-b border-slate-100 dark:border-slate-800 flex items-center justify-between">
        <div>
          <h3 class="text-base font-black text-slate-900 dark:text-white">💬 Cevap Ver</h3>
          <p class="text-[11px] text-slate-500 mt-0.5 truncate max-w-[300px]">{{ replyModal.ticketTitle }}</p>
        </div>
        <button @click="closeReplyModal" class="w-8 h-8 rounded-lg hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-400 hover:text-slate-600 transition flex items-center justify-center text-lg">✕</button>
      </div>

      <!-- Body -->
      <div class="px-6 py-5 space-y-4">
        <!-- Talep Sahibi Bilgisi -->
        <div class="flex flex-col gap-2 px-4 py-3 bg-slate-50 dark:bg-slate-800/50 rounded-2xl border border-slate-200 dark:border-slate-700">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-xl bg-indigo-100 dark:bg-indigo-900/50 text-indigo-600 dark:text-indigo-400 flex items-center justify-center text-lg shadow-sm">
              👤
            </div>
            <div>
              <div class="text-sm font-bold text-slate-900 dark:text-white">
                {{ replyModal.requesterName }}
                <span v-if="replyModal.requesterTitle" class="text-[10px] ml-1 font-medium bg-slate-200 dark:bg-slate-700 px-1.5 py-0.5 rounded text-slate-600 dark:text-slate-300">{{ replyModal.requesterTitle }}</span>
              </div>
              <div class="flex flex-wrap items-center gap-x-3 gap-y-1 mt-0.5">
                <div v-if="replyModal.requesterEmail" class="text-[11px] font-medium text-slate-500 flex items-center gap-1">
                  ✉️ {{ replyModal.requesterEmail }}
                </div>
                <div v-if="replyModal.requesterPhone" class="text-[11px] font-medium text-slate-500 flex items-center gap-1">
                  📞 {{ replyModal.requesterPhone }}
                </div>
                <div v-if="replyModal.requesterTc" class="text-[11px] font-medium text-slate-500 flex items-center gap-1">
                  💳 {{ replyModal.requesterTc }}
                </div>
              </div>
            </div>
          </div>
          
          <div v-if="replyModal.requesterInstitution || replyModal.requesterAddress" class="mt-2 pt-2 border-t border-slate-200 dark:border-slate-700 text-[11px] text-slate-600 dark:text-slate-400 space-y-1">
            <div v-if="replyModal.requesterInstitution">
              <span class="font-bold text-slate-500 dark:text-slate-500">Kurum:</span> {{ replyModal.requesterInstitution }}
            </div>
            <div v-if="replyModal.requesterAddress">
              <span class="font-bold text-slate-500 dark:text-slate-500">Adres:</span> {{ replyModal.requesterAddress }}
            </div>
          </div>
        </div>

        <!-- Konu ve Mesaj -->
        <div v-if="replyModal.ticketDescription" class="rounded-2xl border border-amber-200 dark:border-amber-800/60 bg-amber-50 dark:bg-amber-900/10 overflow-hidden">
          <div class="px-4 py-2 border-b border-amber-200 dark:border-amber-800/60 flex items-center gap-2">
            <span class="text-amber-600 dark:text-amber-400 text-xs">📩</span>
            <span class="text-[10px] font-black text-amber-700 dark:text-amber-400 uppercase tracking-widest">Gelen Talep Mesajı</span>
          </div>
          <div class="px-4 py-3 text-sm text-amber-900 dark:text-amber-200 leading-relaxed whitespace-pre-wrap max-h-32 overflow-y-auto">
            {{ replyModal.ticketDescription }}
          </div>
        </div>

        <!-- Ek (Varsa) -->
        <div v-if="replyModal.attachmentUrl" class="flex items-center gap-3 px-4 py-3 bg-blue-50 dark:bg-blue-900/10 rounded-2xl border border-blue-200 dark:border-blue-800/50">
          <div class="w-8 h-8 rounded-lg bg-blue-100 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 flex items-center justify-center">
            📎
          </div>
          <div class="flex-1">
            <div class="text-xs font-bold text-blue-900 dark:text-blue-400">Ek Dosya</div>
            <a :href="'http://localhost:5000' + replyModal.attachmentUrl" target="_blank" class="text-[11px] text-blue-600 dark:text-blue-500 hover:underline">Görüntülemek için tıklayın</a>
          </div>
        </div>

        <!-- Yanıt alanı -->
        <div>
          <label class="block text-[10px] font-black text-slate-400 uppercase tracking-widest mb-2">Yanıt Metni</label>
          <textarea
            v-model="replyModal.message"
            rows="5"
            placeholder="Başvuru sahibine iletilecek resmi yanıtınızı yazın..."
            class="w-full bg-slate-50 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-2xl p-4 text-slate-900 dark:text-white text-sm leading-relaxed outline-none focus:ring-4 focus:ring-blue-500/20 resize-none transition-all"
            :disabled="replyModal.saving"
            autofocus
          ></textarea>
          <p class="text-[10px] text-slate-400 mt-2">Bu yanıt kaydedildiğinde talep durumu <strong>Cevaplandı</strong> olarak güncellenecektir.</p>
        </div>
      </div>

      <!-- Footer -->
      <div class="px-6 py-4 border-t border-slate-100 dark:border-slate-800 flex justify-end gap-3">
        <button @click="closeReplyModal" class="px-5 py-2.5 rounded-xl border border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-300 text-sm font-bold hover:bg-slate-50 dark:hover:bg-slate-800 transition">İptal</button>
        <button
          @click="submitReply"
          :disabled="replyModal.saving || !replyModal.message.trim()"
          class="px-6 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-sm font-black shadow-lg shadow-blue-500/20 disabled:opacity-50 transition-all active:scale-95 flex items-center gap-2"
        >
          <span v-if="replyModal.saving" class="animate-spin">⏳</span>
          {{ replyModal.saving ? 'Kaydediliyor…' : 'Kaydet' }}
        </button>
      </div>
    </div>
  </div>
</Transition>

<!-- ═══ MODAL: YÖNLENDIR ═══ -->
<TransferModal
  :show="transferModal.show"
  :ticketId="transferModal.ticketId"
  :currentDepartmentId="transferModal.currentDepartmentId"
  @close="closeTransferModal"
  @transferred="onTransferred"
/>

<!-- ═══ MODAL: KAPAT ═══ -->
<Transition name="modal">
  <div v-if="closeTicketModal.show" class="fixed inset-0 z-[80] flex items-center justify-center p-4">
    <div class="fixed inset-0 bg-slate-900/40 backdrop-blur-sm" @click="closeTicketModal.show = false"></div>
    <div class="relative w-full max-w-md bg-white dark:bg-slate-900 rounded-3xl shadow-2xl border border-slate-200 dark:border-slate-800 p-8">
      <div class="w-16 h-16 bg-rose-100 dark:bg-rose-900/30 text-rose-600 dark:text-rose-400 rounded-full flex items-center justify-center mb-6 shadow-sm mx-auto">
        <svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" /></svg>
      </div>
      <h3 class="text-xl font-black text-slate-900 dark:text-white mb-2 text-center">Talebi Kapat</h3>
      <p class="text-sm text-slate-600 dark:text-slate-400 mb-8 leading-relaxed text-center">
        <strong>#{{ closeTicketModal.referenceNo }}</strong> referans numaralı bu talebi kapatmak istediğinize emin misiniz? İşlem tamamlandığında vatandaş veya ilgili öğrenci başvurusunun sonuçlandığına dair bilgilendirilecektir.
      </p>
      <div class="flex gap-4">
        <button @click="closeTicketModal.show = false" type="button" class="flex-1 py-3 px-4 rounded-xl border-2 border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-300 font-bold hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors">
          İptal
        </button>
        <button @click="confirmCloseTicket" type="button" class="flex-1 py-3 px-4 rounded-xl bg-rose-600 hover:bg-rose-700 text-white font-black shadow-lg shadow-rose-500/30 flex items-center justify-center gap-2 transition-all active:scale-95">
          Evet, Kapat
        </button>
      </div>
    </div>
  </div>
</Transition>

<!-- ═══ MODAL: CEVAP VERİLMEDİ UYARISI ═══ -->
<Transition name="modal">
  <div v-if="showUnansweredModal" class="fixed inset-0 z-[80] flex items-center justify-center p-4">
    <div class="fixed inset-0 bg-slate-900/40 backdrop-blur-sm" @click="showUnansweredModal = false"></div>
    <div class="relative w-full max-w-md bg-white dark:bg-slate-900 rounded-3xl shadow-2xl border border-slate-200 dark:border-slate-800 p-8">
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
</Transition>

<ReminderFormPopup 
  :show="showReminderPopup" 
  :ticketId="drawerTicket?.id" 
  @close="showReminderPopup = false"
  @created="loadReminders(); loadTickets()"
/>

</div><!-- end root div -->
</template>

<style scoped>
.fade-enter-active, .fade-leave-active { transition: opacity 0.25s ease; }
.fade-enter-from, .fade-leave-to { opacity: 0; }

.slide-right-enter-active, .slide-right-leave-active { transition: transform 0.3s cubic-bezier(0.16, 1, 0.3, 1); }
.slide-right-enter-from, .slide-right-leave-to { transform: translateX(100%); }

.modal-enter-active, .modal-leave-active {
  transition: all 0.2s ease;
}
.modal-enter-from, .modal-leave-to {
  opacity: 0;
  transform: scale(0.95);
}
.modal-enter-active .relative, .modal-leave-active .relative { transition: transform 0.2s ease; }
.modal-enter-from .relative { transform: scale(0.95); }
.modal-leave-to .relative { transform: scale(0.95); }
</style>
