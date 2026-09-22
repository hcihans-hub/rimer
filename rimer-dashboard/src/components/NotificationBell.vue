<template>
  <div class="relative inline-block text-left" v-click-outside="closePanel">
    <button
      @click="togglePanel"
      class="relative p-2 rounded-lg text-slate-600 dark:text-slate-400 hover:text-indigo-600 dark:hover:text-white hover:bg-slate-100 dark:hover:bg-slate-800 focus:outline-none transition-colors"
      aria-label="Bildirimler"
      title="Bildirimler"
    >
      <svg class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
      </svg>
      <span
        v-if="unreadCount > 0"
        class="absolute top-1 right-1 block h-2.5 w-2.5 rounded-full bg-red-500 ring-2 ring-white dark:ring-slate-900"
      ></span>
    </button>

    <div
      v-if="isOpen"
      class="origin-top-right absolute right-0 mt-2 w-80 rounded-xl shadow-2xl bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 ring-1 ring-black/5 z-50 overflow-hidden"
    >
      <div class="p-3 border-b border-slate-200 dark:border-slate-700 flex justify-between items-center bg-slate-50 dark:bg-slate-900/50">
        <h3 class="text-sm font-bold text-slate-800 dark:text-white">Bildirimler</h3>
        <button
          v-if="unreadCount > 0"
          @click="markAllRead"
          class="text-xs text-indigo-600 dark:text-blue-400 hover:text-indigo-800 dark:hover:text-blue-300 font-semibold"
        >
          Tümünü okundu say
        </button>
      </div>

      <div class="max-h-96 overflow-y-auto">
        <div v-if="loading" class="p-4 text-center text-slate-400 text-sm">
          Yükleniyor...
        </div>
        <div v-else-if="notifications.length === 0" class="p-4 text-center text-slate-400 dark:text-slate-500 text-sm">
          Bildirim bulunmuyor.
        </div>
        <ul v-else class="divide-y divide-slate-100 dark:divide-slate-700">
          <li
            v-for="notification in notifications"
            :key="notification.id"
            @click="handleNotificationClick(notification)"
            class="p-4 hover:bg-slate-50 dark:hover:bg-slate-700 cursor-pointer transition-colors"
            :class="{ 'bg-indigo-50/60 dark:bg-slate-700/50': !notification.isRead }"
          >
            <div class="flex items-start">
              <div class="flex-1 min-w-0">
                <p class="text-sm text-slate-700 dark:text-slate-200" :class="{ 'font-bold': !notification.isRead }">
                  {{ notification.message }}
                </p>
                <p class="mt-1 text-xs text-slate-400 dark:text-slate-500">
                  {{ new Date(notification.createdAt).toLocaleString() }}
                </p>
              </div>
              <div v-if="!notification.isRead" class="ml-2 flex-shrink-0">
                <span class="inline-block h-2 w-2 rounded-full bg-indigo-500"></span>
              </div>
            </div>
          </li>
        </ul>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { notificationApi } from '../services/notificationApi'

const router = useRouter()
const isOpen = ref(false)
const notifications = ref([])
const unreadCount = ref(0)
const loading = ref(false)

let pollInterval = null

const fetchUnreadCount = async () => {
  try {
    const res = await notificationApi.getUnreadCount()
    if (res.success) {
      unreadCount.value = res.data
    }
  } catch (error) {
    console.error('Failed to fetch unread count:', error)
  }
}

const fetchNotifications = async () => {
  loading.value = true
  try {
    const res = await notificationApi.getMyNotifications()
    if (res.success) {
      notifications.value = res.data.slice(0, 5) // Show max 5 recent
    }
  } catch (error) {
    console.error('Failed to fetch notifications:', error)
  } finally {
    loading.value = false
  }
}

const togglePanel = async () => {
  isOpen.value = !isOpen.value
  if (isOpen.value) {
    await fetchNotifications()
  }
}

const closePanel = () => {
  isOpen.value = false
}

const markAllRead = async () => {
  try {
    await notificationApi.markAllRead()
    unreadCount.value = 0
    notifications.value.forEach(n => n.isRead = true)
  } catch (error) {
    console.error('Failed to mark all read:', error)
  }
}

const handleNotificationClick = async (notification) => {
  if (!notification.isRead) {
    try {
      await notificationApi.markRead(notification.id)
      notification.isRead = true
      unreadCount.value = Math.max(0, unreadCount.value - 1)
    } catch (error) {
      console.error('Failed to mark read:', error)
    }
  }

  isOpen.value = false

  if (notification.ticketId) {
    router.push(`/tickets/${notification.ticketId}`)
  }
}

// Click outside directive (simple implementation for this component)
const vClickOutside = {
  mounted(el, binding) {
    el.clickOutsideEvent = (event) => {
      if (!(el === event.target || el.contains(event.target))) {
        binding.value()
      }
    }
    document.body.addEventListener('click', el.clickOutsideEvent)
  },
  unmounted(el) {
    document.body.removeEventListener('click', el.clickOutsideEvent)
  }
}

onMounted(() => {
  fetchUnreadCount()
  pollInterval = setInterval(fetchUnreadCount, 30000) // Poll every 30s
})

onUnmounted(() => {
  if (pollInterval) {
    clearInterval(pollInterval)
  }
})
</script>
