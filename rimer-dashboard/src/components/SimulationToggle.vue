<script setup>
import { ref, onUnmounted } from 'vue'
import { store } from '../store'

const simMode = ref(false)
let simInterval = null
let simPhase = 0

const SIM_PHASES = ['NORMAL', 'NORMAL', 'HIGH LOAD', 'PROTECTION MODE', 'HIGH LOAD', 'NORMAL']

let simTotalRequests = 12000
let simDropped = 0

const runSimulation = () => {
  simPhase = (simPhase + 1) % SIM_PHASES.length
  const phase = SIM_PHASES[simPhase]

  let queueDepth = 0
  let throughput = 220

  if (phase === 'PROTECTION MODE') {
    queueDepth = 82000 + Math.floor(Math.random() * 12000)
    simDropped += 1200 + Math.floor(Math.random() * 800)
    throughput = 60 + Math.floor(Math.random() * 40)
    store.addAlert({ id: `sim_crit_${Date.now()}`, level: 'CRITICAL', message: `[SIM] Queue at ${queueDepth.toLocaleString()} — protection active` })
  } else if (phase === 'HIGH LOAD') {
    queueDepth = 52000 + Math.floor(Math.random() * 20000)
    simDropped += 100 + Math.floor(Math.random() * 200)
    throughput = 160 + Math.floor(Math.random() * 100)
    store.addAlert({ id: `sim_high_${Date.now()}`, level: 'HIGH LOAD', message: `[SIM] Queue at ${queueDepth.toLocaleString()} — approaching limit` })
  } else {
    queueDepth = Math.max(0, (store.metrics.queueDepth || 0) - 15000)
    throughput = 200 + Math.floor(Math.random() * 80)
  }

  simTotalRequests += throughput

  store.updateMetrics({
    totalRequests: simTotalRequests,
    droppedRequests: simDropped,
    queueDepth,
    throughput,
    systemMode: phase,
  })
}

const toggleSimulation = () => {
  simMode.value = !simMode.value
  if (simMode.value) {
    simPhase = 0
    simTotalRequests = store.metrics.totalRequests || 12000
    simDropped = store.metrics.droppedRequests || 0
    simInterval = setInterval(runSimulation, 3000)
    store.addAlert({ id: 'sim_start', level: 'DROP SPIKE', message: 'Simulation mode ON — generating synthetic metrics' })
  } else {
    clearInterval(simInterval)
    simInterval = null
  }
}

onUnmounted(() => { if (simInterval) clearInterval(simInterval) })
</script>

<template>
  <button
    @click="toggleSimulation"
    class="flex items-center gap-2 px-4 py-2 rounded-xl text-xs font-bold border transition-all duration-300"
    :class="simMode
      ? 'bg-violet-500/15 border-violet-500/30 text-violet-400 hover:bg-violet-500/25'
      : 'bg-slate-800/50 border-slate-700 text-slate-400 hover:border-slate-600 hover:text-slate-300'"
  >
    <span class="relative flex h-2 w-2">
      <span v-if="simMode" class="animate-ping absolute inline-flex h-full w-full rounded-full bg-violet-400 opacity-75" />
      <span
        class="relative inline-flex rounded-full h-2 w-2 transition-colors"
        :class="simMode ? 'bg-violet-400' : 'bg-slate-600'"
      />
    </span>
    {{ simMode ? 'SIM ON' : 'Simulation Mode' }}
  </button>
</template>
