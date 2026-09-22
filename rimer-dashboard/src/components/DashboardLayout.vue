<script setup>
import { ref, computed, onMounted } from 'vue'
import { store } from '../store'

const props = defineProps({
  modelValue: {
    type: String,
    default: 'Overview'
  }
})

const emit = defineEmits(['update:modelValue'])

const isDark = ref(true)

const menuItems = [
  { name: 'Overview', icon: 'M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zM14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zM14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z' },
  { name: 'System Health', icon: 'M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 5.04M12 20.944a11.955 11.955 0 01-8.618-5.04M12 20.944a11.952 11.952 0 008.618-5.04M12 20.944V12' },
  { name: 'Queue Monitor', icon: 'M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10' },
  { name: 'Endpoint Analytics', icon: 'M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z' },
]

// STEP 2: Compute highest alert level for topbar indicator
const alertLevel = computed(() => {
  if (store.alerts.find(a => a.level === 'CRITICAL')) return 'CRITICAL'
  if (store.alerts.find(a => a.level === 'HIGH LOAD')) return 'HIGH LOAD'
  if (store.alerts.find(a => a.level === 'DROP SPIKE')) return 'DROP SPIKE'
  return null
})

const toggleDarkMode = () => {
  isDark.value = !isDark.value
  updateTheme()
}

const updateTheme = () => {
  if (isDark.value) {
    document.documentElement.classList.add('dark')
  } else {
    document.documentElement.classList.remove('dark')
  }
}

onMounted(() => {
  updateTheme()
})
</script>

<template>
  <div class="flex h-screen bg-slate-50 dark:bg-[#0a0f18] transition-colors duration-300">
    <!-- Sidebar -->
    <aside class="w-64 border-r border-slate-200 dark:border-slate-800 bg-white dark:bg-[#0d131f] flex flex-col z-20">
      <div class="p-6 border-b border-slate-100 dark:border-slate-800/50">
        <h1 class="text-xl font-bold bg-gradient-to-r from-blue-600 to-indigo-600 bg-clip-text text-transparent">
          Rimer Dashboard
        </h1>
      </div>
      
      <nav class="flex-1 p-4 space-y-1">
        <button
          v-for="item in menuItems"
          :key="item.name"
          @click="emit('update:modelValue', item.name)"
          class="w-full flex items-center space-x-3 px-4 py-2.5 rounded-lg transition-all duration-200 text-sm font-medium"
          :class="[
            modelValue === item.name 
              ? 'bg-blue-50 text-blue-600 dark:bg-blue-600/10 dark:text-blue-400' 
              : 'text-slate-600 dark:text-slate-400 hover:bg-slate-50 dark:hover:bg-slate-800/50'
          ]"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" :d="item.icon" />
          </svg>
          <span>{{ item.name }}</span>
        </button>
      </nav>

      <!-- Profile Section -->
      <div class="p-4 border-t border-slate-100 dark:border-slate-800/50">
        <div class="flex items-center space-x-3 px-4 py-2 opacity-80">
          <div class="w-8 h-8 rounded-full bg-slate-200 dark:bg-slate-700 flex items-center justify-center overflow-hidden">
            <svg class="w-5 h-5 text-slate-500" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M10 9a3 3 0 100-6 3 3 0 000 6zm-7 9a7 7 0 1114 0H3z" clip-rule="evenodd" />
            </svg>
          </div>
          <div class="flex-1 min-w-0">
            <p class="text-xs font-semibold text-slate-900 dark:text-white truncate">Admin User</p>
            <p class="text-[10px] text-slate-500 truncate">System Engineer</p>
          </div>
        </div>
      </div>
    </aside>

    <!-- Main Content Wrapper -->
    <div class="flex-1 flex flex-col min-w-0 overflow-hidden">
      <!-- Topbar -->
      <header class="h-16 flex items-center justify-between px-8 bg-white/80 dark:bg-[#0d131f]/80 backdrop-blur-md border-b border-slate-200 dark:border-slate-800 z-10">
        <h2 class="text-lg font-semibold text-slate-800 dark:text-slate-100">{{ modelValue }}</h2>
        
        <div class="flex items-center space-x-4">
          <!-- Alert Indicator -->
          <div v-if="alertLevel" class="flex items-center space-x-2 px-3 py-1.5 rounded-lg border text-xs font-bold uppercase tracking-wider"
               :class="{
                 'bg-rose-500/10 border-rose-500/20 text-rose-500': alertLevel === 'CRITICAL',
                 'bg-amber-500/10 border-amber-500/20 text-amber-500': alertLevel === 'HIGH LOAD',
                 'bg-orange-500/10 border-orange-500/20 text-orange-500': alertLevel === 'DROP SPIKE',
               }">
            <span class="relative flex h-2 w-2">
              <span class="animate-ping absolute inline-flex h-full w-full rounded-full opacity-75"
                    :class="{
                      'bg-rose-400': alertLevel === 'CRITICAL',
                      'bg-amber-400': alertLevel === 'HIGH LOAD',
                      'bg-orange-400': alertLevel === 'DROP SPIKE',
                    }"></span>
              <span class="relative inline-flex rounded-full h-2 w-2"
                    :class="{
                      'bg-rose-500': alertLevel === 'CRITICAL',
                      'bg-amber-500': alertLevel === 'HIGH LOAD',
                      'bg-orange-500': alertLevel === 'DROP SPIKE',
                    }"></span>
            </span>
            <span>{{ alertLevel }}</span>
          </div>

          <button 
            @click="toggleDarkMode"
            class="p-2 rounded-lg bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-700 transition-colors"
          >
            <svg v-if="isDark" class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 3v1m0 18v1m9-9h1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z" />
            </svg>
            <svg v-else class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z" />
            </svg>
          </button>
        </div>
      </header>

      <!-- Page Content -->
      <main class="flex-1 overflow-y-auto p-8 bg-slate-50 dark:bg-[#0a0f18]/50">
        <slot></slot>
      </main>
    </div>
  </div>
</template>

<style>
/* Custom transitions for a premium feel */
.transition-colors {
  transition-property: background-color, border-color, color, fill, stroke;
  transition-timing-function: cubic-bezier(0.4, 0, 0.2, 1);
  transition-duration: 300ms;
}
</style>
