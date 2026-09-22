<script setup>
import { computed } from 'vue'
import { store } from '../store'
import RimerChart from '../components/RimerChart.vue'

// ── Mock data helpers ─────────────────────────────────────────────────────────
const pad = (arr, target, seed = 0) => {
  const out = [...arr]
  while (out.length < target) out.unshift(Math.max(0, seed + Math.round((Math.random() - 0.4) * seed * 0.3)))
  return out.slice(-target)
}

// Requests per day (last 14 days) — live + mock fill
const dayLabels = Array.from({ length: 14 }, (_, i) => {
  const d = new Date(); d.setDate(d.getDate() - (13 - i))
  return d.toLocaleDateString('tr-TR', { day: '2-digit', month: 'short' })
})
const reqByDay = computed(() => {
  const base = store.requests.reduce((acc, r) => {
    const key = new Date(r.createdAt).toLocaleDateString('tr-TR', { day: '2-digit', month: 'short' })
    acc[key] = (acc[key] || 0) + 1
    return acc
  }, {})
  const real = dayLabels.map(l => base[l] || 0)
  // Fill with mock if empty
  const total = real.reduce((a, b) => a + b, 0)
  return total === 0 ? pad([], 14, 42) : real
})

// Requests per month (last 6 months)
const monthLabels = Array.from({ length: 6 }, (_, i) => {
  const d = new Date(); d.setMonth(d.getMonth() - (5 - i))
  return d.toLocaleDateString('tr-TR', { month: 'short', year: '2-digit' })
})
const reqByMonth = computed(() => {
  const base = store.requests.reduce((acc, r) => {
    const key = new Date(r.createdAt).toLocaleDateString('tr-TR', { month: 'short', year: '2-digit' })
    acc[key] = (acc[key] || 0) + 1
    return acc
  }, {})
  const real = monthLabels.map(l => base[l] || 0)
  const total = real.reduce((a, b) => a + b, 0)
  return total === 0 ? [320, 480, 390, 520, 610, 440] : real
})

// Queue depth history from live WS
const queueLabels  = computed(() => store.metrics.queueHistory.labels.length > 0 ? store.metrics.queueHistory.labels : Array.from({length:10},(_,i)=>`T-${10-i}`))
const queueValues  = computed(() => store.metrics.queueHistory.values.length > 0 ? store.metrics.queueHistory.values : [12000,18000,32000,55000,90000,110000,88000,60000,30000,8000])

// Throughput
const tpLabels = computed(() => store.metrics.throughputHistory.labels.length > 0 ? store.metrics.throughputHistory.labels : Array.from({length:10},(_,i)=>`T-${10-i}`))
const tpValues = computed(() => store.metrics.throughputHistory.values.length > 0 ? store.metrics.throughputHistory.values : [120,180,340,500,480,320,210,180,280,340])

// Summary stats
const totalRequests    = computed(() => store.requests.length || store.metrics.totalRequests || 0)
const pendingRequests  = computed(() => store.requests.filter(r => r.status === 'pending').length)
const uniqueUsers      = computed(() => new Set(store.requests.map(r => r.username)).size)
const typeBreakdown    = computed(() => {
  const counts = { request: 0, complaint: 0, suggestion: 0 }
  store.requests.forEach(r => { if (counts[r.type] !== undefined) counts[r.type]++ })
  return counts
})
</script>

<template>
  <div class="space-y-6">
    <!-- Summary stat cards -->
    <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
      <div v-for="card in [
        { label: 'Total Submissions', value: totalRequests, color: 'blue', icon: '📨' },
        { label: 'Pending',           value: pendingRequests, color: 'amber', icon: '⏳' },
        { label: 'Unique Users',      value: uniqueUsers,    color: 'emerald', icon: '👥' },
        { label: 'Queue Depth (live)',value: store.metrics.queueDepth.toLocaleString(), color: 'indigo', icon: '📊' },
      ]" :key="card.label"
        class="h-[110px] bg-[#0d131f] border border-slate-800 rounded-2xl p-4 flex flex-col justify-between hover:border-slate-700 transition-colors"
      >
        <div class="flex items-center justify-between">
          <p class="text-[10px] font-bold uppercase tracking-widest text-slate-500">{{ card.label }}</p>
          <span class="text-base">{{ card.icon }}</span>
        </div>
        <p class="text-2xl font-bold text-white tabular-nums">{{ card.value }}</p>
      </div>
    </div>

    <!-- Type breakdown -->
    <div class="grid grid-cols-3 gap-4">
      <div v-for="(count, type) in typeBreakdown" :key="type"
        class="bg-[#0d131f] border border-slate-800 rounded-2xl p-4 text-center hover:border-slate-700 transition-colors"
      >
        <p class="text-[10px] font-bold uppercase tracking-widest text-slate-500 mb-1">{{ type }}s</p>
        <p class="text-xl font-bold"
          :class="{ 'text-blue-400': type === 'request', 'text-rose-400': type === 'complaint', 'text-amber-400': type === 'suggestion' }"
        >{{ count }}</p>
      </div>
    </div>

    <!-- Charts row 1 -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-5">
      <div class="bg-[#0d131f] border border-slate-800 rounded-2xl p-5 relative overflow-hidden">
        <div class="absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-blue-500/25 to-transparent" />
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-xs font-bold text-white uppercase tracking-wider">Requests / Day</h3>
          <span class="text-[10px] text-slate-500">Last 14 days</span>
        </div>
        <div class="h-[220px]">
          <RimerChart :chart-data="reqByDay" :chart-labels="dayLabels" line-color="#3b82f6" />
        </div>
      </div>

      <div class="bg-[#0d131f] border border-slate-800 rounded-2xl p-5 relative overflow-hidden">
        <div class="absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-indigo-500/25 to-transparent" />
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-xs font-bold text-white uppercase tracking-wider">Requests / Month</h3>
          <span class="text-[10px] text-slate-500">Last 6 months</span>
        </div>
        <div class="h-[220px]">
          <RimerChart :chart-data="reqByMonth" :chart-labels="monthLabels" line-color="#6366f1" />
        </div>
      </div>
    </div>

    <!-- Charts row 2 -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-5">
      <div class="bg-[#0d131f] border border-slate-800 rounded-2xl p-5 relative overflow-hidden">
        <div class="absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-violet-500/25 to-transparent" />
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-xs font-bold text-white uppercase tracking-wider">Queue Depth (Live)</h3>
          <span class="text-[10px] text-slate-500 font-mono">Jobs</span>
        </div>
        <div class="h-[220px]">
          <RimerChart :chart-data="queueValues" :chart-labels="queueLabels" line-color="#8b5cf6" />
        </div>
      </div>

      <div class="bg-[#0d131f] border border-slate-800 rounded-2xl p-5 relative overflow-hidden">
        <div class="absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-emerald-500/25 to-transparent" />
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-xs font-bold text-white uppercase tracking-wider">Throughput (Live)</h3>
          <span class="text-[10px] text-slate-500 font-mono">RPS</span>
        </div>
        <div class="h-[220px]">
          <RimerChart :chart-data="tpValues" :chart-labels="tpLabels" line-color="#10b981" />
        </div>
      </div>
    </div>
  </div>
</template>
