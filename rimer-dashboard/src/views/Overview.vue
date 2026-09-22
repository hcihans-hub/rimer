<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import RimerChart from '../components/RimerChart.vue'
import MetricCard from '../components/MetricCard.vue'
import SystemStory from '../components/SystemStory.vue'
import SystemActions from '../components/SystemActions.vue'
import SimulationToggle from '../components/SimulationToggle.vue'
import ActionPanel from '../components/ActionPanel.vue'
import socket from '../services/socket'
import { store } from '../store'

// ── Socket wiring ─────────────────────────────────────────────────────────────
// Throughput & queue series live here so chart update is isolated (no store re-render)
const throughputVals   = ref([])
const throughputLabels = ref([])
const queueVals        = ref([])
const queueLabels      = ref([])

const push = (labels, vals, label, val) => {
  labels.value = [...labels.value, label].slice(-30)
  vals.value   = [...vals.value,   val  ].slice(-30)
}

const handleMessage = (data) => {
  // 1. Update store metrics (reactive across all pages)
  store.updateMetrics(data)

  // 2. Push chart series (local — avoids store reactivity chain on chart)
  const now = new Date().toLocaleTimeString('tr-TR', {
    hour: '2-digit', minute: '2-digit', second: '2-digit'
  })
  push(throughputLabels, throughputVals, now, data.throughput  ?? 0)
  push(queueLabels,      queueVals,      now, data.queueDepth  ?? 0)

  // 3. Alert detection
  const depth   = data.queueDepth     || 0
  const dropped = data.droppedRequests || 0
  const ts      = Date.now()

  if (depth > 100_000) {
    store.addAlert({ id: 'crit', level: 'CRITICAL',  message: `Queue at ${depth.toLocaleString()} — system at risk` })
  } else if (depth > 80_000) {
    store.addAlert({ id: 'hi',   level: 'HIGH LOAD', message: `Queue at ${depth.toLocaleString()} — approaching limit` })
  }

  const delta = dropped - store.lastDroppedRequests
  if (delta > 500) {
    store.addAlert({ id: `drop_${ts}`, level: 'DROP SPIKE', message: `${delta.toLocaleString()} requests dropped` })
  }
  store.lastDroppedRequests = dropped
}

onMounted(() => {
  socket.onMessage(handleMessage)
  socket.onClose(() => {})
  socket.connect()
})
onUnmounted(() => socket.disconnect())

// ── Derived state ─────────────────────────────────────────────────────────────
const m           = computed(() => store.metrics)
const systemMode  = computed(() => m.value.systemMode ?? 'NORMAL')

const metrics = computed(() => [
  { id: 'total',   title: 'Total Requests',   icon: '📨', value: m.value.totalRequests.toLocaleString(),   subtitle: 'Global throughput' },
  { id: 'dropped', title: 'Dropped Requests', icon: '🗑️', value: m.value.droppedRequests.toLocaleString(), subtitle: 'Current shed rate' },
  { id: 'queue',   title: 'Queue Depth',      icon: '📊', value: m.value.queueDepth.toLocaleString(),      subtitle: 'Target: < 80k'     },
  { id: 'mode',    title: 'System Mode',      icon: '',   value: systemMode.value, subtitle: 'Engine state', type: 'mode' },
])

// ── Trend detection (last 5 queue points) ────────────────────────────────────
const queueTrend = computed(() => {
  const v = queueVals.value
  if (v.length < 5) return 'stable'
  const w = v.slice(-5), d = w[w.length - 1] - w[0]
  return d > 3000 ? 'rising' : d < -3000 ? 'falling' : 'stable'
})

// ── Recovery ETA ──────────────────────────────────────────────────────────────
const recoveryEta = computed(() => {
  const v = queueVals.value
  if (v.length < 5) return null
  let totalDrain = 0, n = 0
  const w = v.slice(-5)
  for (let i = 1; i < w.length; i++) {
    const d = w[i - 1] - w[i]
    if (d > 0) { totalDrain += d; n++ }
  }
  if (!n) return null
  const rate = totalDrain / n
  return rate > 0 ? (m.value.queueDepth / rate) * 2.5 : null
})
</script>

<template>
  <div class="space-y-5 animate-fade-in relative">

    <!-- Protection edge glow — v-show never removes DOM node -->
    <div v-show="systemMode === 'PROTECTION MODE'" class="fixed inset-0 pointer-events-none z-0 protection-edge-glow" />
    <div v-show="systemMode === 'HIGH LOAD'"       class="fixed inset-0 pointer-events-none z-0 high-load-tint" />

    <!-- Top bar — fixed height prevents jump -->
    <div class="flex items-center justify-between h-8">
      <div class="flex items-center gap-2">
        <span class="relative flex h-2 w-2">
          <span v-if="socket.isConnected" class="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75" />
          <span
            class="relative inline-flex rounded-full h-2 w-2 transition-colors duration-300"
            :class="socket.isConnected ? 'bg-emerald-500' : 'bg-rose-500'"
          />
        </span>
        <span class="text-xs text-slate-500 font-medium">
          {{ socket.isConnected ? 'Live' : 'Reconnecting…' }}
        </span>
      </div>
      <SimulationToggle />
    </div>

    <!-- ── Metric Cards — fixed height h-[120px] ── -->
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-5">
      <MetricCard
        v-for="metric in metrics"
        :key="metric.id"
        :title="metric.title"
        :value="metric.value"
        :subtitle="metric.subtitle"
        :icon="metric.icon"
        :type="metric.type || 'default'"
        :mode="systemMode"
      />
    </div>

    <!-- ── System Story + Actions — hard fixed height, never resizes ── -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-5 h-[220px]">
      <SystemStory
        :mode="systemMode"
        :queue-depth="m.queueDepth"
        :dropped-requests="m.droppedRequests"
      />
      <SystemActions
        :mode="systemMode"
        :recovery-eta="recoveryEta"
        :queue-trend="queueTrend"
      />
    </div>

    <!-- ── Charts — fixed h-[320px] ── -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-5">
      <div class="bg-white/5 dark:bg-[#0d131f] border border-slate-800 rounded-2xl p-6 relative overflow-hidden">
        <div class="absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-blue-500/20 to-transparent" />
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-sm font-bold text-white">Request Throughput</h3>
          <span class="text-xs text-slate-500 font-mono">RPS</span>
        </div>
        <!-- Fixed height — skeleton and real content same size -->
        <div class="h-[320px]">
          <RimerChart :chart-data="throughputVals" :chart-labels="throughputLabels" line-color="#3b82f6" />
        </div>
      </div>

      <div class="bg-white/5 dark:bg-[#0d131f] border border-slate-800 rounded-2xl p-6 relative overflow-hidden">
        <div class="absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-indigo-500/20 to-transparent" />
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-sm font-bold text-white">Queue Depth</h3>
          <span class="text-xs text-slate-500 font-mono">Jobs</span>
        </div>
        <div class="h-[320px]">
          <RimerChart :chart-data="queueVals" :chart-labels="queueLabels" line-color="#6366f1" />
        </div>
      </div>
    </div>

    <!-- ── Alert Feed + Controls ── -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-5">

      <!-- Alert Feed — fixed h-[300px] overflow-y-auto, never expands page -->
      <div class="lg:col-span-2 bg-white/5 dark:bg-[#0d131f] border border-slate-800 rounded-2xl p-5 flex flex-col">
        <div class="flex items-center justify-between mb-4 flex-shrink-0">
          <h3 class="text-sm font-bold text-white">Alert Feed</h3>
          <button
            v-show="store.alerts.length > 0"
            @click="store.clearAlerts()"
            class="text-[10px] text-slate-500 hover:text-slate-300 transition-colors px-2 py-1 rounded-lg hover:bg-slate-800"
          >Clear all</button>
        </div>

        <!-- Fixed height scroll container -->
        <div class="h-[300px] overflow-y-auto space-y-2 pr-1 relative">
          <!-- Empty state — v-show keeps height stable -->
          <div
            v-show="store.alerts.length === 0"
            class="absolute inset-0 flex flex-col items-center justify-center"
          >
            <span class="text-2xl mb-2">🟢</span>
            <p class="text-xs text-slate-500">No active alerts — system healthy</p>
          </div>

          <transition-group name="alert-slide">
            <div
              v-for="alert in store.alerts"
              :key="alert.id"
              class="flex items-start gap-3 p-3 rounded-xl border-l-4 cursor-pointer transition-all duration-200 hover:opacity-75"
              :class="{
                'bg-rose-500/5 border-rose-500':    alert.level === 'CRITICAL',
                'bg-amber-500/5 border-amber-500':  alert.level === 'HIGH LOAD',
                'bg-orange-500/5 border-orange-500': alert.level === 'DROP SPIKE',
                'bg-slate-800/50 border-slate-600': !['CRITICAL','HIGH LOAD','DROP SPIKE'].includes(alert.level),
              }"
              @click="store.removeAlert(alert.id)"
            >
              <span class="text-sm flex-shrink-0 mt-0.5">
                {{ alert.level === 'CRITICAL' ? '🔴' : alert.level === 'HIGH LOAD' ? '🟡' : '🟠' }}
              </span>
              <div class="flex-1 min-w-0">
                <p
                  class="text-[10px] font-bold uppercase tracking-widest"
                  :class="{
                    'text-rose-400':   alert.level === 'CRITICAL',
                    'text-amber-400':  alert.level === 'HIGH LOAD',
                    'text-orange-400': alert.level === 'DROP SPIKE',
                    'text-slate-400':  !['CRITICAL','HIGH LOAD','DROP SPIKE'].includes(alert.level),
                  }"
                >{{ alert.level }}</p>
                <p class="text-xs text-slate-400 leading-snug truncate">{{ alert.message }}</p>
              </div>
              <span class="text-[10px] text-slate-600 flex-shrink-0 mt-0.5">
                {{ alert.timestamp ? new Date(alert.timestamp).toLocaleTimeString('tr-TR',{hour:'2-digit',minute:'2-digit',second:'2-digit'}) : '' }}
              </span>
            </div>
          </transition-group>
        </div>
      </div>

      <!-- Action Panel -->
      <div class="lg:col-span-1">
        <ActionPanel />
      </div>
    </div>

  </div>
</template>

<style scoped>
.animate-fade-in { animation: fadeIn 0.5s ease-out forwards; }
@keyframes fadeIn { from{opacity:0;transform:translateY(10px)} to{opacity:1;transform:translateY(0)} }

.protection-edge-glow {
  box-shadow:
    inset 0 0 80px 20px rgba(239,68,68,0.08),
    inset 80px 0 80px -40px rgba(239,68,68,0.05),
    inset -80px 0 80px -40px rgba(239,68,68,0.05);
  animation: edge-pulse 2.5s ease-in-out infinite;
}
@keyframes edge-pulse { 0%,100%{opacity:.6} 50%{opacity:1} }

.high-load-tint {
  background: radial-gradient(ellipse at top, rgba(245,158,11,0.04) 0%, transparent 70%);
}

.alert-slide-enter-active { animation: alertIn 0.3s ease-out; }
.alert-slide-leave-active  { animation: alertIn 0.25s ease-in reverse; position: absolute; width: 100%; }
@keyframes alertIn {
  from { opacity: 0; transform: translateX(-12px); }
  to   { opacity: 1; transform: translateX(0); }
}
</style>
