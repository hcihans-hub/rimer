<script setup>
import { computed } from 'vue'

const props = defineProps({
  mode: { type: String, default: 'NORMAL' },
  recoveryEta: { type: Number, default: null },
  queueTrend: { type: String, default: 'stable' }
})

const config = computed(() => {
  switch (props.mode) {
    case 'PROTECTION MODE':
      return {
        color: 'rose',
        label: 'Immediate Actions Required',
        icon: '🛡️',
        actions: [
          { icon: '📉', text: 'Increase sampling rate to reduce enqueue volume', priority: 'high' },
          { icon: '🗑️', text: 'Trigger emergency drop to flush stuck jobs', priority: 'high' },
          { icon: '⏸️', text: 'Reduce or pause incoming traffic at load balancer', priority: 'medium' },
          { icon: '⏱️', text: 'Wait for 60s fail-safe auto-reset if queue drains', priority: 'low' },
        ]
      }
    case 'HIGH LOAD':
      return {
        color: 'amber',
        label: 'Recommended Actions',
        icon: '⚡',
        actions: [
          { icon: '👁️', text: 'Monitor queue depth trend closely', priority: 'medium' },
          { icon: '📊', text: 'Check drop rate — if rising, pre-enable protection', priority: 'medium' },
          { icon: '⚙️', text: 'Prepare to scale worker count if load sustains', priority: 'low' },
          { icon: '🟢', text: 'System still accepting normal priority traffic', priority: 'none' },
        ]
      }
    default:
      return {
        color: 'emerald',
        label: 'System Stable',
        icon: '✅',
        actions: [
          { icon: '🟢', text: 'All systems nominal — no action required', priority: 'none' },
          { icon: '📋', text: 'Review periodic audit log reports', priority: 'none' },
          { icon: '📈', text: 'Monitor throughput for unexpected spikes', priority: 'none' },
          { icon: '💡', text: 'Adjust sampling rate as needed', priority: 'none' },
        ]
      }
  }
})

const priorityClass = (p) => ({
  high:   'text-rose-400 bg-rose-500/8',
  medium: 'text-amber-400 bg-amber-500/8',
  low:    'text-slate-400 bg-slate-700/30',
  none:   'text-emerald-400 bg-emerald-500/8',
})[p] ?? 'text-slate-400'

const borderColor = computed(() => ({
  rose:    'border-rose-500/25',
  amber:   'border-amber-500/25',
  emerald: 'border-emerald-500/25',
})[config.value.color])

const bgColor = computed(() => ({
  rose:    'bg-rose-500/5',
  amber:   'bg-amber-500/5',
  emerald: 'bg-emerald-500/5',
})[config.value.color])

const headerColor = computed(() => ({
  rose:    'text-rose-400',
  amber:   'text-amber-400',
  emerald: 'text-emerald-400',
})[config.value.color])

const etaDisplay = computed(() => {
  if (props.recoveryEta === null || props.recoveryEta <= 0) return '—'
  if (props.recoveryEta < 60) return `~${Math.round(props.recoveryEta)}s`
  const m = Math.floor(props.recoveryEta / 60)
  const s = Math.round(props.recoveryEta % 60)
  return `~${m}m ${s}s`
})

const trendText = computed(() =>
  props.queueTrend === 'rising'  ? 'Queue rising rapidly ↑' :
  props.queueTrend === 'falling' ? 'System recovering ↓'    : 'Queue stable →'
)

const trendColor = computed(() =>
  props.queueTrend === 'rising'  ? 'text-rose-400' :
  props.queueTrend === 'falling' ? 'text-emerald-400' : 'text-slate-500'
)
</script>

<template>
  <!--
    FIXED HEIGHT — overflow:hidden.
    Content changes (mode, ETA, trend) only swap text/color; never resize the box.
  -->
  <div
    class="h-[220px] relative overflow-hidden rounded-2xl border backdrop-blur-sm p-5 transition-colors duration-500"
    :class="[bgColor, borderColor]"
  >
    <!-- Top shimmer line -->
    <div class="absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-white/8 to-transparent" />

    <!-- Header row — always same height -->
    <div class="flex items-center gap-3 mb-3">
      <div class="w-9 h-9 rounded-xl flex items-center justify-center text-base flex-shrink-0" :class="bgColor">
        {{ config.icon }}
      </div>
      <div class="flex-1 min-w-0">
        <p class="text-[10px] font-bold uppercase tracking-widest text-slate-500">System Actions</p>
        <p class="text-xs font-semibold mt-0.5 transition-colors duration-300" :class="headerColor">
          {{ config.label }}
        </p>
      </div>

      <!-- ETA + Trend always rendered, just update text — no layout shift -->
      <div class="flex-shrink-0 text-right">
        <p class="text-[10px] text-slate-500 uppercase tracking-wider">Recovery</p>
        <p class="text-xs font-bold font-mono text-blue-400 transition-all duration-300">{{ etaDisplay }}</p>
        <p class="text-[10px] font-semibold transition-colors duration-300 mt-0.5" :class="trendColor">
          {{ trendText }}
        </p>
      </div>
    </div>

    <!-- Action list: fixed 4 slots, no v-if, only text/color swap -->
    <ul class="space-y-1.5 overflow-hidden">
      <li
        v-for="action in config.actions"
        :key="action.text"
        class="flex items-center gap-2.5 px-2.5 py-1.5 rounded-lg transition-colors duration-300"
        :class="priorityClass(action.priority)"
      >
        <span class="text-xs flex-shrink-0">{{ action.icon }}</span>
        <p class="text-xs leading-tight truncate">{{ action.text }}</p>
      </li>
    </ul>
  </div>
</template>
