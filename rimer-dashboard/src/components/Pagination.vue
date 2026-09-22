<template>
  <div class="flex items-center justify-between mt-4">
    <div class="text-xs text-slate-500">
      Toplam: <span class="font-bold text-slate-700 dark:text-slate-300">{{ totalCount }}</span>
    </div>
    <div class="flex items-center gap-2">
      <select 
        :value="pageSize" 
        @change="$emit('update:pageSize', Number($event.target.value))"
        class="text-xs px-2 py-1.5 rounded-lg border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-700 dark:text-slate-300 outline-none"
      >
        <option :value="10">10 / sayfa</option>
        <option :value="20">20 / sayfa</option>
        <option :value="50">50 / sayfa</option>
        <option :value="100">100 / sayfa</option>
      </select>
      <button 
        :disabled="page <= 1" 
        @click="$emit('update:page', page - 1)"
        class="px-3 py-1.5 rounded-lg border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-700 dark:text-slate-300 disabled:opacity-50 text-xs font-bold transition hover:bg-slate-50 dark:hover:bg-slate-700"
      >
        Önceki
      </button>
      <span class="text-xs font-bold text-slate-700 dark:text-slate-300 px-2">{{ page }} / {{ totalPages }}</span>
      <button 
        :disabled="page >= totalPages" 
        @click="$emit('update:page', page + 1)"
        class="px-3 py-1.5 rounded-lg border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-slate-700 dark:text-slate-300 disabled:opacity-50 text-xs font-bold transition hover:bg-slate-50 dark:hover:bg-slate-700"
      >
        Sonraki
      </button>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  page: { type: Number, required: true },
  pageSize: { type: Number, required: true },
  totalCount: { type: Number, required: true }
})

const emit = defineEmits(['update:page', 'update:pageSize'])

const totalPages = computed(() => Math.ceil(props.totalCount / props.pageSize) || 1)
</script>
