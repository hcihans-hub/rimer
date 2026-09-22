<script setup>
import { toastStore } from '../auth'
</script>

<template>
  <!-- Max 3 toasts — click any → clear all — auto-dismiss 4s (handled in toastStore) -->
  <div class="fixed top-4 right-4 z-50 space-y-2 w-72">
    <div
      v-for="t in toastStore.toasts"
      :key="t.id"
      class="px-4 py-3 rounded-xl border text-xs font-semibold shadow-xl backdrop-blur-md cursor-pointer select-none"
      :class="{
        'bg-emerald-500/10 border-emerald-500/20 text-emerald-400': t.type === 'success',
        'bg-rose-500/10 border-rose-500/20 text-rose-400':          t.type === 'error',
        'bg-slate-800/90 border-slate-700 text-slate-300':          t.type === 'info',
      }"
      @click="toastStore.toasts = []"
    >{{ t.msg }}</div>
  </div>
</template>
