<script setup>
import { ref, computed, watch } from 'vue'

const props = defineProps({
  title: { type: String, required: true },
  value: { type: [String, Number], required: true },
  subtitle: { type: String, default: '' },
  icon: { type: String, default: '' },
  type: { type: String, default: 'default' }, // 'default' | 'mode'
  mode: { type: String, default: 'NORMAL' }  // for mode card
})

// Animate number on value change
const isUpdating = ref(false)
watch(() => props.value, () => {
  isUpdating.value = true
  setTimeout(() => { isUpdating.value = false }, 350)
})

const modeStyles = computed(() => {
  switch (props.mode) {
    case 'NORMAL':          return { badge: 'bg-emerald-500/15 text-emerald-400 border-emerald-500/25', dot: 'bg-emerald-400', glow: false }
    case 'HIGH LOAD':       return { badge: 'bg-amber-500/15 text-amber-400 border-amber-500/25', dot: 'bg-amber-400', glow: false }
    case 'PROTECTION MODE': return { badge: 'bg-rose-500/15 text-rose-400 border-rose-500/25', dot: 'bg-rose-400', glow: true }
    default:                return { badge: 'bg-slate-500/15 text-slate-400 border-slate-500/25', dot: 'bg-slate-400', glow: false }
  }
})

const cardAccent = computed(() => {
  if (props.type !== 'mode') return 'hover:border-blue-500/30'
  switch (props.mode) {
    case 'NORMAL': return 'hover:border-emerald-500/30'
    case 'HIGH LOAD': return 'hover:border-amber-500/30'
    case 'PROTECTION MODE': return 'hover:border-rose-500/30 border-rose-500/20'
    default: return 'hover:border-slate-500/30'
  }
})
</script>

<template>
  <div
    class="h-[120px] relative bg-white/5 dark:bg-[#0d131f] backdrop-blur-sm p-5 rounded-2xl border border-slate-200/50 dark:border-slate-800 shadow-sm transition-all duration-300 group cursor-default overflow-hidden"
    :class="[cardAccent, 'hover:-translate-y-1 hover:shadow-xl']"
  >
    <!-- Subtle inner glow top -->
    <div class="absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-white/10 to-transparent" />

    <!-- Pulse ring for protection mode -->
    <div
      v-if="type === 'mode' && mode === 'PROTECTION MODE'"
      class="absolute inset-0 rounded-2xl animate-protection-ring border-2 border-rose-500/30"
    />

    <!-- Icon -->
    <div v-if="icon" class="text-xl mb-3">{{ icon }}</div>

    <!-- Title -->
    <p class="text-xs font-bold uppercase tracking-widest text-slate-500 dark:text-slate-400 group-hover:text-blue-400 transition-colors duration-200">
      {{ title }}
    </p>

    <!-- Mode Card -->
    <template v-if="type === 'mode'">
      <div class="mt-3 flex items-center gap-2">
        <span
          class="inline-flex items-center gap-2 px-3 py-1.5 rounded-full text-xs font-bold border transition-all duration-500"
          :class="modeStyles.badge"
        >
          <span class="relative flex h-2 w-2">
            <span
              v-if="modeStyles.glow"
              class="animate-ping absolute inline-flex h-full w-full rounded-full opacity-75"
              :class="modeStyles.dot"
            />
            <span class="relative inline-flex rounded-full h-2 w-2" :class="modeStyles.dot" />
          </span>
          {{ value }}
        </span>
      </div>
    </template>

    <!-- Normal Metric Card -->
    <template v-else>
      <p
        class="text-3xl font-bold mt-2 text-slate-900 dark:text-white leading-none tabular-nums transition-all duration-300"
        :class="{ 'scale-[1.02] opacity-80': isUpdating }"
      >
        {{ value }}
      </p>
    </template>

    <p class="text-xs text-slate-400 dark:text-slate-500 mt-3 font-medium">{{ subtitle }}</p>
  </div>
</template>

<style scoped>
@keyframes protection-ring {
  0%, 100% { opacity: 0.3; transform: scale(1); }
  50% { opacity: 0.7; transform: scale(1.01); }
}
.animate-protection-ring {
  animation: protection-ring 2s ease-in-out infinite;
}
</style>
