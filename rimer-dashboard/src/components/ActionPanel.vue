<script setup>
import { ref, computed } from 'vue'
import api from '../services/api'
import { store } from '../store'

const feedbackMsg = ref('')
const feedbackType = ref('success')
let feedbackTimer = null

const showFeedback = (msg, type = 'success') => {
  feedbackMsg.value = msg
  feedbackType.value = type
  clearTimeout(feedbackTimer)
  feedbackTimer = setTimeout(() => { feedbackMsg.value = '' }, 3000)
}

// ── Protection Mode ──────────────────────────────────────────────────────────
// Derived from WS systemMode — single source of truth
const isProtectionActive = computed(() => store.protectionMode)

const toggleProtection = async () => {
  if (store.protectionPending) return
  store.protectionPending = true
  try {
    await api.postProtectionMode(!isProtectionActive.value)
    // Do NOT update local state — wait for system_mode_change via WebSocket
    showFeedback(`Protection Mode request sent. Waiting for confirmation…`)
  } catch {
    showFeedback('Failed to toggle protection mode', 'error')
  } finally {
    store.protectionPending = false
  }
}

// ── Emergency Drop ────────────────────────────────────────────────────────────
const emergencyPending = ref(false)

const emergencyDrop = async () => {
  emergencyPending.value = true
  try {
    await api.postEmergencyDrop()
    showFeedback('Emergency drop executed — queue flushed')
  } catch {
    showFeedback('Emergency drop failed', 'error')
  } finally {
    emergencyPending.value = false
  }
}

// ── Sampling Rate ─────────────────────────────────────────────────────────────
const samplingPending = ref(false)

const samplingLabel = computed(() => {
  const r = store.samplingRate
  if (r >= 80) return 'High acceptance — most requests pass through'
  if (r >= 40) return 'Balanced — some low priority requests dropped'
  return 'Low acceptance — heavy load shedding active'
})

const updateSampling = async () => {
  samplingPending.value = true
  try {
    await api.postSamplingRate(store.samplingRate)
    showFeedback(`Sampling rate set to ${store.samplingRate}%`)
  } catch {
    showFeedback('Failed to update sampling rate', 'error')
  } finally {
    samplingPending.value = false
  }
}
</script>

<template>
  <div class="h-full bg-white/5 dark:bg-[#0d131f] border border-slate-200/50 dark:border-slate-800 rounded-2xl p-5 relative overflow-hidden flex flex-col gap-4">
    <!-- Glass shimmer -->
    <div class="absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-white/8 to-transparent" />

    <div class="flex items-center justify-between">
      <p class="text-xs font-bold uppercase tracking-widest text-slate-400">System Controls</p>
      <span class="text-[10px] text-slate-600 uppercase tracking-widest">Admin</span>
    </div>

    <!-- Feedback -->
    <transition name="fade">
      <div
        v-if="feedbackMsg"
        class="px-3 py-2 rounded-xl text-xs font-semibold border"
        :class="feedbackType === 'error'
          ? 'bg-rose-500/10 text-rose-400 border-rose-500/20'
          : 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20'"
      >{{ feedbackMsg }}</div>
    </transition>

    <!-- Group: Protection -->
    <div class="space-y-1.5">
      <p class="text-[10px] text-slate-600 uppercase tracking-widest font-bold">Circuit Breaker</p>
      <div class="flex items-center justify-between p-3.5 rounded-xl bg-slate-900/60 border border-slate-800 hover:border-slate-700 transition-colors">
        <div>
          <p class="text-xs font-semibold text-slate-300">Protection Mode</p>
          <p class="text-[11px] mt-0.5 text-slate-500">
            State: <span
              class="font-bold transition-colors duration-300"
              :class="isProtectionActive ? 'text-rose-400' : 'text-emerald-400'"
            >{{ isProtectionActive ? 'ACTIVE' : 'INACTIVE' }}</span>
            <span v-if="store.protectionPending" class="text-amber-400 ml-1">(updating…)</span>
          </p>
        </div>
        <button
          @click="toggleProtection"
          :disabled="store.protectionPending"
          class="relative w-11 h-6 rounded-full transition-all duration-300 focus:outline-none disabled:opacity-40 flex-shrink-0"
          :class="isProtectionActive ? 'bg-rose-500' : 'bg-slate-700'"
        >
          <span v-if="store.protectionPending" class="absolute inset-0 flex items-center justify-center">
            <span class="w-3 h-3 border border-white border-t-transparent rounded-full animate-spin" />
          </span>
          <span
            v-else
            class="absolute top-0.5 left-0.5 w-5 h-5 bg-white rounded-full shadow-md transition-transform duration-300"
            :class="isProtectionActive ? 'translate-x-5' : 'translate-x-0'"
          />
        </button>
      </div>
    </div>

    <!-- Group: Sampling -->
    <div class="space-y-1.5">
      <p class="text-[10px] text-slate-600 uppercase tracking-widest font-bold">Observability</p>
      <div class="p-3.5 rounded-xl bg-slate-900/60 border border-slate-800 hover:border-slate-700 transition-colors">
        <div class="flex items-center justify-between mb-2">
          <p class="text-xs font-semibold text-slate-300">Sampling Rate</p>
          <span class="text-xs font-mono font-bold text-blue-400 bg-blue-500/10 px-2 py-0.5 rounded-lg">{{ store.samplingRate }}%</span>
        </div>
        <input
          type="range" min="0" max="100" step="5"
          v-model.number="store.samplingRate"
          @change="updateSampling"
          :disabled="samplingPending"
          class="w-full h-1 bg-slate-700 rounded-full appearance-none cursor-pointer accent-blue-500 disabled:opacity-40"
        />
        <p class="text-[10px] text-slate-500 mt-2 leading-snug">{{ samplingLabel }}</p>
        <p class="text-[10px] text-slate-600 mt-0.5">Lower = more data dropped under load</p>
      </div>
    </div>

    <!-- Group: Emergency -->
    <div class="space-y-1.5">
      <p class="text-[10px] text-slate-600 uppercase tracking-widest font-bold">Danger Zone</p>
      <button
        @click="emergencyDrop"
        :disabled="emergencyPending"
        class="w-full py-2.5 px-4 rounded-xl text-xs font-bold transition-all duration-200 border
               bg-rose-500/8 text-rose-400 border-rose-500/20
               hover:bg-rose-500 hover:text-white hover:border-rose-500 hover:shadow-lg hover:shadow-rose-500/20
               disabled:opacity-40 disabled:cursor-not-allowed disabled:hover:bg-rose-500/8 disabled:hover:text-rose-400 disabled:hover:border-rose-500/20"
      >
        <span v-if="emergencyPending" class="flex items-center justify-center gap-2">
          <span class="w-3 h-3 border border-current border-t-transparent rounded-full animate-spin" />
          Executing…
        </span>
        <span v-else>⚠ Emergency Drop All Queues</span>
      </button>
    </div>
  </div>
</template>

<style scoped>
.fade-enter-active, .fade-leave-active { transition: opacity 0.25s ease; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
</style>
