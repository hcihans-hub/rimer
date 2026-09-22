<script setup>
import { ref, onMounted, onUnmounted, computed } from 'vue'
import { i18n } from '../lang'

const isOpen = ref(false)
const container = ref(null)

const toggle = () => isOpen.value = !isOpen.value
const close = (e) => {
  if (container.value && !container.value.contains(e.target)) {
    isOpen.value = false
  }
}

const select = (lang) => {
  i18n.set(lang)
  isOpen.value = false
}

onMounted(() => window.addEventListener('click', close))
onUnmounted(() => window.removeEventListener('click', close))

const options = [
  { code: 'tr', flag: '/langicon/turkey.png', key: 'langTr' },
  { code: 'en', flag: '/langicon/english.png', key: 'langEn' },
  { code: 'zh', flag: '/langicon/china.png', key: 'langZh' },
  { code: 'az', flag: '/langicon/azerbaijan.png', key: 'langAz' },
  { code: 'ru', flag: '/langicon/russia.png', key: 'langRu' },
  { code: 'kk', flag: '/langicon/kazakhstan.png', key: 'langKk' },
  { code: 'ar', flag: '/langicon/arabic.png', key: 'langAr' },
  { code: 'es', flag: '/langicon/espanol.png', key: 'langEs' },
  { code: 'de', flag: '/langicon/germany.png', key: 'langDe' },
  { code: 'ja', flag: '/langicon/japan.png', key: 'langJa' },
  { code: 'ur', flag: '/langicon/pakistan.png', key: 'langUr' },
  { code: 'ko', flag: '/langicon/kore.png', key: 'langKo' },
]

const current = computed(() => options.find(o => o.code === i18n.lang) ?? options[0])
</script>

<template>
  <div class="relative" ref="container">
    <button
      @click.stop="toggle"
      :title="i18n.t[current.key]"
      class="flex items-center gap-1.5 px-2 py-1.5 rounded-xl hover:bg-slate-100 dark:hover:bg-slate-800 transition-all"
    >
      <img :src="current.flag" :alt="current.code" class="w-6 h-6 object-contain" />
      <svg
        class="w-3.5 h-3.5 transition-transform duration-200 text-slate-400"
        :class="{ 'rotate-180': isOpen }"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
      >
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
      </svg>
    </button>

    <Transition
      enter-active-class="transition duration-100 ease-out"
      enter-from-class="transform scale-95 opacity-0"
      enter-to-class="transform scale-100 opacity-100"
      leave-active-class="transition duration-75 ease-in"
      leave-from-class="transform scale-100 opacity-100"
      leave-to-class="transform scale-95 opacity-0"
    >
      <div
        v-if="isOpen"
        class="absolute right-0 mt-2 p-2 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-xl z-50 flex flex-col gap-1"
      >
        <button
          v-for="opt in options"
          :key="opt.code"
          @click="select(opt.code)"
          :title="i18n.t[opt.key]"
          class="flex items-center justify-center p-2 rounded-xl transition-all"
          :class="i18n.lang === opt.code 
            ? 'bg-blue-50 dark:bg-blue-500/20 shadow-sm border border-blue-100 dark:border-blue-500/30' 
            : 'hover:bg-slate-50 dark:hover:bg-slate-800 border border-transparent'"
        >
          <img :src="opt.flag" :alt="opt.code" class="w-7 h-7 object-contain hover:scale-110 transition-transform" />
        </button>
      </div>
    </Transition>
  </div>
</template>
