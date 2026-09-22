<script setup>
import { computed } from 'vue'
import RimerChart from '../components/RimerChart.vue'
import { store } from '../store'

const m = computed(() => store.metrics)

// Derive priority breakdown from total queue depth (approximate)
const critical = computed(() => Math.round(m.value.queueDepth * 0.1))
const normal   = computed(() => Math.round(m.value.queueDepth * 0.6))
const low      = computed(() => Math.round(m.value.queueDepth * 0.3))

const utilizationPct = computed(() =>
  Math.min(100, Math.round(m.value.queueDepth / 1000))
)
</script>

<template>
  <div class="space-y-6 animate-fade-in">
    <!-- Priority cards — fixed height -->
    <div class="grid grid-cols-1 md:grid-cols-3 gap-5">
      <div
        v-for="(item, idx) in [
          { label: 'Critical Priority', value: critical, color: 'rose' },
          { label: 'Normal Priority',   value: normal,   color: 'blue' },
          { label: 'Low Priority',      value: low,      color: 'slate' },
        ]"
        :key="idx"
        class="h-[120px] bg-white/5 dark:bg-[#0d131f] p-5 rounded-2xl border border-slate-800 hover:-translate-y-1 hover:border-slate-700 transition-all duration-300 flex flex-col justify-between relative overflow-hidden"
      >
        <p class="text-xs font-bold uppercase tracking-widest text-slate-500">{{ item.label }}</p>
        <p
          class="text-3xl font-bold tabular-nums transition-colors duration-500"
          :class="{
            'text-rose-400':    item.color === 'rose',
            'text-blue-400':    item.color === 'blue',
            'text-slate-400':   item.color === 'slate',
          }"
        >{{ item.value.toLocaleString() }}</p>
        <p class="text-[10px] text-slate-600">Active items in buffer</p>
      </div>
    </div>

    <!-- Utilization bar -->
    <div class="bg-white/5 dark:bg-[#0d131f] border border-slate-800 rounded-2xl p-5">
      <div class="flex items-center justify-between mb-3">
        <h3 class="text-sm font-bold text-white">Queue Utilization</h3>
        <span
          class="text-xs font-mono font-bold"
          :class="utilizationPct > 80 ? 'text-rose-400' : utilizationPct > 50 ? 'text-amber-400' : 'text-emerald-400'"
        >{{ utilizationPct }}%</span>
      </div>
      <div class="w-full h-2 bg-slate-800 rounded-full overflow-hidden">
        <div
          class="h-full rounded-full transition-all duration-500"
          :class="utilizationPct > 80 ? 'bg-rose-500' : utilizationPct > 50 ? 'bg-amber-500' : 'bg-emerald-500'"
          :style="{ width: utilizationPct + '%' }"
        />
      </div>
      <div class="flex justify-between mt-1.5">
        <span class="text-[10px] text-slate-600">0</span>
        <span class="text-[10px] text-slate-600">100k cap</span>
      </div>
    </div>

    <!-- History chart -->
    <div class="bg-white/5 dark:bg-[#0d131f] border border-slate-800 rounded-2xl p-6 relative overflow-hidden">
      <div class="absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-indigo-500/20 to-transparent" />
      <div class="flex items-center justify-between mb-4">
        <div>
          <h3 class="text-sm font-bold text-white">Queue Saturation History</h3>
          <p class="text-xs text-slate-500 mt-0.5">Live WebSocket feed — last 30 intervals</p>
        </div>
        <p class="text-2xl font-bold font-mono text-indigo-400 tabular-nums">{{ m.queueDepth.toLocaleString() }}</p>
      </div>
      <div class="h-[350px]">
        <RimerChart :chart-data="m.queueHistory.values" :chart-labels="m.queueHistory.labels" line-color="#818cf8" />
      </div>
    </div>
  </div>
</template>

<style scoped>
.animate-fade-in { animation: fadeIn 0.5s ease-out forwards; }
@keyframes fadeIn { from{opacity:0;transform:translateY(10px)} to{opacity:1;transform:translateY(0)} }
</style>
