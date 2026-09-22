<script setup>
import { theme } from './lang'
import { onMounted } from 'vue'
import ToastContainer from './components/ToastContainer.vue'
import AccessibilityControls from './components/AccessibilityControls.vue'
import GlobalReminderPopup from './components/GlobalReminderPopup.vue'
import AnnouncementBanner from './components/AnnouncementBanner.vue'

onMounted(() => {
  theme.init()
})
</script>

<template>
  <div class="min-h-screen bg-slate-50 dark:bg-slate-950 transition-colors duration-300">
    <AnnouncementBanner />
    <RouterView />
    <ToastContainer />
    <AccessibilityControls />
    <GlobalReminderPopup />
  </div>
</template>

<style>
/* Global Transitions */
.fade-enter-active, .fade-leave-active { transition: opacity 0.2s ease; }
.fade-enter-from, .fade-leave-to { opacity: 0; }

/* Scrollbar Styling */
::-webkit-scrollbar { width: 8px; }
::-webkit-scrollbar-track { background: transparent; }
::-webkit-scrollbar-thumb {
  background: #cbd5e1;
  border-radius: 4px;
}
.dark ::-webkit-scrollbar-thumb {
  background: #334155;
}
::-webkit-scrollbar-thumb:hover {
  background: #94a3b8;
}
</style>
