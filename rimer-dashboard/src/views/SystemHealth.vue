<script setup>
import { computed } from 'vue'
import RimerChart from '../components/RimerChart.vue'
import { store } from '../store'

// All data from shared store — no polling
const m = computed(() => store.metrics)

const normalizeMode = (v) => {
  if (!v) return 'NORMAL'
  if (v === 'HIGH_LOAD'  || v === 'HIGH LOAD')      return 'HIGH LOAD'
  if (v === 'PROTECTION' || v === 'PROTECTION MODE') return 'PROTECTION MODE'
  return 'NORMAL'
}

const mode = computed(() => normalizeMode(m.value.systemMode))

const modeLabel = computed(() => ({
  NORMAL:           { text: 'Healthy',    cls: 'bg-emerald-500/15 text-emerald-400 border-emerald-500/25' },
  'HIGH LOAD':      { text: 'High Load',  cls: 'bg-amber-500/15 text-amber-400 border-amber-500/25' },
  'PROTECTION MODE':{ text: 'Protection', cls: 'bg-rose-500/15 text-rose-400 border-rose-500/25' },
})[mode.value] ?? { text: mode.value, cls: 'bg-slate-500/15 text-slate-400 border-slate-500/25' })

const services = computed(() => [
  { name: 'API Gateway',      status: mode.value === 'PROTECTION MODE' ? 'Degraded' : 'Healthy', latency: Math.round(m.value.throughput > 0 ? 1200 / m.value.throughput : 0) },
  { name: 'Audit Queue',      status: m.value.queueDepth > 80_000 ? 'Degraded' : 'Healthy',       latency: Math.round(m.value.queueDepth / 1000) },
  { name: 'Background Worker',status: m.value.queueDepth > 50_000 ? 'Degraded' : 'Healthy',       latency: 0 },
  { name: 'Database',         status: 'Healthy',                                                    latency: 4 },
])
</script>

<template>
  <div class="space-y-6 animate-fade-in">
    <!-- Top stat cards -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-5">
      <!-- Throughput -->
      <div class="h-[120px] bg-white/5 dark:bg-[#0d131f] p-5 rounded-2xl border border-slate-800 hover:border-slate-700 hover:-translate-y-1 transition-all duration-300 flex flex-col justify-between">
        <div class="flex items-center justify-between">
          <p class="text-xs font-bold uppercase tracking-widest text-slate-500">Throughput</p>
          <span class="text-xs font-mono text-blue-400">RPS</span>
        </div>
        <p class="text-3xl font-bold text-white tabular-nums">{{ m.throughput.toLocaleString() }}</p>
      </div>

      <!-- Queue Depth -->
      <div class="h-[120px] bg-white/5 dark:bg-[#0d131f] p-5 rounded-2xl border border-slate-800 hover:border-slate-700 hover:-translate-y-1 transition-all duration-300 flex flex-col justify-between">
        <div class="flex items-center justify-between">
          <p class="text-xs font-bold uppercase tracking-widest text-slate-500">Queue Depth</p>
          <span class="text-xs font-mono text-indigo-400">Jobs</span>
        </div>
        <p class="text-3xl font-bold text-white tabular-nums">{{ m.queueDepth.toLocaleString() }}</p>
      </div>

      <!-- System Mode -->
      <div class="h-[120px] bg-white/5 dark:bg-[#0d131f] p-5 rounded-2xl border border-slate-800 hover:border-slate-700 hover:-translate-y-1 transition-all duration-300 flex flex-col justify-between">
        <p class="text-xs font-bold uppercase tracking-widest text-slate-500">System Mode</p>
        <span
          class="inline-flex items-center px-3 py-1.5 rounded-full text-xs font-bold border transition-all duration-500 self-start"
          :class="modeLabel.cls"
        >{{ modeLabel.text }}</span>
      </div>
    </div>

    <!-- Charts side by side -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-5">
      <div class="bg-white/5 dark:bg-[#0d131f] border border-slate-800 rounded-2xl p-6 relative overflow-hidden">
        <div class="absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-blue-500/20 to-transparent" />
        <h3 class="text-sm font-bold text-white mb-4">CPU Load Factor</h3>
        <div class="h-[280px]">
          <RimerChart :chart-data="m.throughputHistory.values" :chart-labels="m.throughputHistory.labels" line-color="#3b82f6" />
        </div>
      </div>

      <div class="bg-white/5 dark:bg-[#0d131f] border border-slate-800 rounded-2xl p-6 relative overflow-hidden">
        <div class="absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-indigo-500/20 to-transparent" />
        <h3 class="text-sm font-bold text-white mb-4">Queue Pressure</h3>
        <div class="h-[280px]">
          <RimerChart :chart-data="m.queueHistory.values" :chart-labels="m.queueHistory.labels" line-color="#818cf8" />
        </div>
      </div>
    </div>

    <!-- Service list -->
    <div class="bg-white/5 dark:bg-[#0d131f] border border-slate-800 rounded-2xl p-6">
      <h3 class="text-sm font-bold text-white mb-4">Service Health</h3>
      <div class="space-y-2">
        <div
          v-for="svc in services"
          :key="svc.name"
          class="flex items-center justify-between p-3 rounded-xl bg-slate-900/50 border border-slate-800/50"
        >
          <div class="flex items-center gap-3">
            <span
              class="w-2 h-2 rounded-full flex-shrink-0 transition-colors duration-500"
              :class="svc.status === 'Healthy' ? 'bg-emerald-500' : 'bg-rose-500'"
            />
            <span class="text-sm text-slate-300 font-medium">{{ svc.name }}</span>
          </div>
          <div class="flex items-center gap-3">
            <span class="text-xs font-mono text-slate-500">{{ svc.latency }}ms</span>
            <span
              class="text-[10px] font-bold uppercase px-2 py-0.5 rounded-full"
              :class="svc.status === 'Healthy'
                ? 'bg-emerald-500/15 text-emerald-400'
                : 'bg-rose-500/15 text-rose-400'"
            >{{ svc.status }}</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.animate-fade-in { animation: fadeIn 0.5s ease-out forwards; }
@keyframes fadeIn { from{opacity:0;transform:translateY(10px)} to{opacity:1;transform:translateY(0)} }
</style>
