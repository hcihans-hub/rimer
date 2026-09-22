<script setup>
import { computed } from 'vue'

const props = defineProps({
  mode: { type: String, default: 'NORMAL' },
  queueDepth: { type: Number, default: 0 },
  droppedRequests: { type: Number, default: 0 }
})

const story = computed(() => {
  switch (props.mode) {
    case 'PROTECTION MODE':
      return {
        icon: '🛡️',
        title: 'Protection Mode Active',
        message: 'System is shedding load to stay alive. Non-critical requests are being dropped to preserve API availability. The circuit breaker will auto-reset within 60 seconds.',
        color: 'rose',
        pulse: true
      }
    case 'HIGH LOAD':
      return {
        icon: '⚡',
        title: 'High Load Detected',
        message: 'System is under high load. Low priority requests may be dropped. Normal priority traffic continues. Queue depth is approaching threshold.',
        color: 'amber',
        pulse: false
      }
    default:
      return {
        icon: '✅',
        title: 'System Operating Normally',
        message: 'All systems nominal. Requests are being processed within expected latency bounds. Queue depth is healthy and all traffic is being accepted.',
        color: 'emerald',
        pulse: false
      }
  }
})
</script>

<template>
  <div
    class="h-[220px] relative overflow-hidden rounded-2xl border backdrop-blur-md p-5 transition-colors duration-500"
    :class="{
      'bg-rose-500/5 border-rose-500/30':   story.color === 'rose',
      'bg-amber-500/5 border-amber-500/30':  story.color === 'amber',
      'bg-emerald-500/5 border-emerald-500/30': story.color === 'emerald',
    }"
  >
    <!-- Pulse background for protection mode -->
    <div
      v-if="story.pulse"
      class="absolute inset-0 rounded-2xl animate-protection-pulse bg-rose-500/5"
    />

    <div class="relative flex items-start gap-4">
      <!-- Icon -->
      <div
        class="flex-shrink-0 w-11 h-11 rounded-xl flex items-center justify-center text-xl transition-all duration-500"
        :class="{
          'bg-rose-500/15': story.color === 'rose',
          'bg-amber-500/15': story.color === 'amber',
          'bg-emerald-500/15': story.color === 'emerald',
        }"
      >
        <span>{{ story.icon }}</span>
      </div>

      <!-- Text -->
      <div class="flex-1 min-w-0">
        <div class="flex items-center gap-2 mb-1">
          <span
            class="text-xs font-bold uppercase tracking-widest"
            :class="{
              'text-rose-400': story.color === 'rose',
              'text-amber-400': story.color === 'amber',
              'text-emerald-400': story.color === 'emerald',
            }"
          >System Story</span>
          <span
            v-if="story.pulse"
            class="relative flex h-2 w-2"
          >
            <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-rose-400 opacity-75" />
            <span class="relative inline-flex rounded-full h-2 w-2 bg-rose-500" />
          </span>
        </div>
        <p
          class="text-sm font-semibold mb-1 transition-all duration-300"
          :class="{
            'text-rose-300': story.color === 'rose',
            'text-amber-300': story.color === 'amber',
            'text-emerald-300': story.color === 'emerald',
          }"
        >{{ story.title }}</p>
        <p class="text-xs text-slate-400 leading-relaxed transition-all duration-500">
          {{ story.message }}
        </p>
      </div>

      <!-- Live stats -->
      <div class="flex-shrink-0 text-right hidden sm:block">
        <p class="text-[10px] text-slate-500 uppercase tracking-wider mb-1">Queue</p>
        <p
          class="text-sm font-bold font-mono transition-colors duration-300"
          :class="{
            'text-rose-400': story.color === 'rose',
            'text-amber-400': story.color === 'amber',
            'text-emerald-400': story.color === 'emerald',
          }"
        >{{ queueDepth.toLocaleString() }}</p>
      </div>
    </div>
  </div>
</template>

<style scoped>
@keyframes protection-pulse {
  0%, 100% { opacity: 0.3; }
  50% { opacity: 0.8; }
}
.animate-protection-pulse {
  animation: protection-pulse 2s ease-in-out infinite;
}
</style>
