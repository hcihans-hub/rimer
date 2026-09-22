<script setup>
import { ref, reactive, onMounted, onUnmounted, watch } from 'vue'
import { i18n } from '../lang'

const isOpen = ref(false)

// State for all 14 features
const state = reactive({
  screenReader: false,
  lineSpacing: false,
  highlightLinks: false,
  largeText: false,
  alignText: false,
  largeCursor: false,
  readingGuide: false,
  readingMask: false,
  dyslexiaFriendly: false,
  contrast: false,
  dimming: false,
  lowSaturation: false,
  highSaturation: false,
  hideImages: false
})

// Feature definitions for rendering buttons
const features = [
  { key: 'screenReader', icon: '🎧', label: 'Ekran Okuyucu' },
  { key: 'lineSpacing', icon: '⇕', label: 'Satır Aralığı' },
  { key: 'highlightLinks', icon: '🔗', label: 'Bağlantı Vurgula' },
  { key: 'largeText', icon: 'T', label: 'Büyük Metin' },
  { key: 'alignText', icon: '≡', label: 'Metni Hizala' },
  { key: 'largeCursor', icon: '➤', label: 'Büyük İmleç' },
  { key: 'readingGuide', icon: '👁', label: 'Okuma Kılavuzu' },
  { key: 'readingMask', icon: '⬛', label: 'Okuma Maskesi' },
  { key: 'dyslexiaFriendly', icon: 'Aa', label: 'Disleksi Dostu' },
  { key: 'contrast', icon: '🌓', label: 'Kontrast' },
  { key: 'dimming', icon: '💡', label: 'Solgunlaştırma' },
  { key: 'lowSaturation', icon: '💧', label: 'Düşük Doygunluk' },
  { key: 'highSaturation', icon: '🔥', label: 'Yüksek Doygunluk' },
  { key: 'hideImages', icon: '🖼', label: 'Resimleri Gizle' }
]

// Load state from localStorage
onMounted(() => {
  const savedState = localStorage.getItem('rim_a11y')
  if (savedState) {
    Object.assign(state, JSON.parse(savedState))
  }
  applyAllFeatures()
  
  window.addEventListener('keydown', handleKeydown)
  window.addEventListener('mousemove', handleMousemove)
})

onUnmounted(() => {
  window.removeEventListener('keydown', handleKeydown)
  window.removeEventListener('mousemove', handleMousemove)
  stopSpeaking()
})

const handleKeydown = (e) => {
  if (e.ctrlKey && e.key.toLowerCase() === 'y') {
    e.preventDefault()
    isOpen.value = !isOpen.value
  }
}

// Mouse position for reading guide & mask
const mouseY = ref(0)
const handleMousemove = (e) => {
  if (state.readingGuide || state.readingMask) {
    mouseY.value = e.clientY
  }
}

// Toggle feature
const toggleFeature = (key) => {
  // Handle mutually exclusive features
  if (key === 'lowSaturation' && !state.lowSaturation) state.highSaturation = false
  if (key === 'highSaturation' && !state.highSaturation) state.lowSaturation = false

  state[key] = !state[key]
  
  if (key === 'screenReader') {
    if (state.screenReader) readPage()
    else stopSpeaking()
  } else {
    applyAllFeatures()
  }
  
  localStorage.setItem('rim_a11y', JSON.stringify(state))
}

const clearAll = () => {
  Object.keys(state).forEach(k => state[k] = false)
  stopSpeaking()
  applyAllFeatures()
  localStorage.setItem('rim_a11y', JSON.stringify(state))
}

// Apply features to DOM
const applyAllFeatures = () => {
  const b = document.body
  const h = document.documentElement
  
  b.classList.toggle('a11y-line-spacing', state.lineSpacing)
  b.classList.toggle('a11y-highlight-links', state.highlightLinks)
  h.classList.toggle('a11y-large-text', state.largeText)
  b.classList.toggle('a11y-align-text', state.alignText)
  b.classList.toggle('a11y-large-cursor', state.largeCursor)
  b.classList.toggle('a11y-dyslexia', state.dyslexiaFriendly)
  b.classList.toggle('high-contrast', state.contrast)
  b.classList.toggle('a11y-dimming', state.dimming)
  b.classList.toggle('a11y-low-saturation', state.lowSaturation)
  b.classList.toggle('a11y-high-saturation', state.highSaturation)
  b.classList.toggle('a11y-hide-images', state.hideImages)
}

// --- TTS Logic ---
let synth = window.speechSynthesis
const speak = (text) => {
  if (!text || !synth) return
  synth.cancel()
  const msg = new SpeechSynthesisUtterance(text)
  msg.lang = i18n.lang === 'tr' ? 'tr-TR' : 'en-US'
  msg.onend = () => state.screenReader = false
  synth.speak(msg)
}

const stopSpeaking = () => {
  if (synth) synth.cancel()
}

const readPage = () => {
  const content = document.querySelector('main')?.innerText || document.body.innerText
  if (content) speak(content)
}

</script>

<template>
  <div class="fixed bottom-6 right-6 z-[9999] flex flex-col items-end gap-3 no-print">
    
    <!-- Accessibility Menu (New Design) -->
    <div v-if="isOpen" class="bg-white dark:bg-slate-900 border-2 border-slate-200 dark:border-slate-700 rounded-2xl shadow-2xl p-5 w-[380px] flex flex-col gap-5 animate-in slide-in-from-bottom-4 duration-200">
      
      <!-- Header -->
      <div class="flex flex-col items-center border-b border-slate-100 dark:border-slate-800 pb-4 relative">
        <div class="flex items-center gap-2 mb-1">
          <span class="text-xl">♿</span>
          <h3 class="font-black text-slate-800 dark:text-slate-200 text-lg tracking-tight">
            Erişilebilirlik
          </h3>
        </div>
        <div class="flex items-center gap-2">
          <span class="text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest">Kontrol Paneli</span>
          <div class="flex items-center gap-1 bg-slate-100 dark:bg-slate-800 px-2 py-0.5 rounded-md border border-slate-200 dark:border-slate-700">
            <span class="text-[9px] font-bold text-slate-500 dark:text-slate-400">Ctrl</span>
            <span class="text-[9px] font-bold text-slate-400">+</span>
            <span class="text-[9px] font-bold text-slate-500 dark:text-slate-400">Y</span>
          </div>
        </div>
        
        <button @click="isOpen = false" class="absolute right-0 top-0 w-7 h-7 flex items-center justify-center rounded-full bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-700 text-slate-400 hover:bg-rose-50 hover:border-rose-200 hover:text-rose-500 dark:hover:bg-rose-500/10 dark:hover:border-rose-500/30 dark:hover:text-rose-400 transition-all">
          <span class="text-base font-bold leading-none mb-[2px]">&times;</span>
        </button>
      </div>

      <!-- Feature Grid -->
      <div class="grid grid-cols-2 gap-3 overflow-y-auto max-h-[55vh] pr-2 
                  scrollbar-thin scrollbar-thumb-slate-300 dark:scrollbar-thumb-slate-600 scrollbar-track-transparent pb-1">
        <button 
          v-for="feat in features" 
          :key="feat.key"
          @click="toggleFeature(feat.key)"
          :class="[
            state[feat.key] 
              ? 'bg-gradient-to-r from-indigo-500 to-blue-500 dark:from-blue-600 dark:to-indigo-600 text-white shadow-[0_0_12px_rgba(99,102,241,0.4)] dark:shadow-[0_0_12px_rgba(59,130,246,0.5)] border-transparent scale-[0.98]' 
              : 'bg-slate-50 dark:bg-slate-800/50 text-slate-700 dark:text-slate-300 border-slate-200 dark:border-slate-700 hover:bg-white dark:hover:bg-slate-800 hover:border-indigo-300 dark:hover:border-blue-500/50 hover:shadow-[0_0_15px_rgba(99,102,241,0.2)] dark:hover:shadow-[0_0_15px_rgba(59,130,246,0.3)] hover:text-indigo-600 dark:hover:text-blue-400'
          ]"
          class="flex items-center gap-3 p-3 rounded-xl font-bold text-[12px] transition-all duration-300 border text-left group"
        >
          <span class="text-lg w-5 text-center shrink-0 transition-transform group-hover:scale-110">{{ feat.icon }}</span>
          <span class="leading-tight tracking-wide">{{ feat.label }}</span>
        </button>
      </div>

      <!-- Reset Button -->
      <button 
        @click="clearAll"
        class="w-full flex items-center justify-center gap-2 p-3 rounded-xl font-bold text-sm transition-all duration-300
               bg-rose-50 dark:bg-rose-500/10 text-rose-600 dark:text-rose-400 border border-rose-100 dark:border-rose-500/20
               hover:bg-rose-500 dark:hover:bg-rose-600 hover:text-white dark:hover:text-white hover:shadow-[0_0_15px_rgba(244,63,94,0.3)] dark:hover:shadow-[0_0_15px_rgba(225,29,72,0.4)] hover:border-transparent group"
      >
        <span class="text-lg group-hover:rotate-12 transition-transform">🗑</span>
        Erişilebilirlik Ayarlarını Temizle
      </button>

    </div>

    <!-- Toggle Button -->
    <button @click="isOpen = !isOpen" 
      class="w-16 h-16 rounded-full bg-white/60 dark:bg-[#1e3a5f] text-blue-700 dark:text-white shadow-xl hover:scale-110 active:scale-95 transition-all flex items-center justify-center text-3xl border-4 border-white/80 dark:border-slate-900 backdrop-blur-md focus:ring-4 ring-blue-500/50 outline-none"
      title="Erişilebilirlik Menüsü (Ctrl + y)">
      ♿
    </button>

    <!-- Reading Guide Line -->
    <div v-if="state.readingGuide" 
         class="fixed left-0 right-0 h-1 bg-red-500/80 z-[10000] pointer-events-none transition-transform duration-75"
         :style="{ top: mouseY + 'px' }">
    </div>

    <!-- Reading Mask -->
    <div v-if="state.readingMask" class="fixed inset-0 z-[9998] pointer-events-none">
      <div class="absolute top-0 left-0 right-0 bg-black/70 transition-all duration-75" :style="{ height: Math.max(0, mouseY - 50) + 'px' }"></div>
      <!-- Clear slit -->
      <div class="absolute left-0 right-0 h-[100px] bg-transparent transition-all duration-75" :style="{ top: Math.max(0, mouseY - 50) + 'px' }"></div>
      <div class="absolute bottom-0 left-0 right-0 bg-black/70 transition-all duration-75" :style="{ top: (mouseY + 50) + 'px' }"></div>
    </div>
  </div>
</template>

<style>
/* 1. Line Spacing */
body.a11y-line-spacing, body.a11y-line-spacing * {
  line-height: 2 !important;
  letter-spacing: 0.05em !important;
  word-spacing: 0.1em !important;
}

/* 2. Highlight Links */
body.a11y-highlight-links a, 
body.a11y-highlight-links button {
  text-decoration: underline !important;
  text-decoration-color: #ff0 !important;
  text-decoration-thickness: 3px !important;
  background-color: rgba(255,255,0,0.15) !important;
  color: inherit;
}

/* 3. Large Text */
html.a11y-large-text {
  font-size: 130% !important;
}

/* 4. Align Text (Left align everything to prevent justified text reading issues) */
body.a11y-align-text, body.a11y-align-text * {
  text-align: left !important;
}

/* 5. Large Cursor */
body.a11y-large-cursor, body.a11y-large-cursor * {
  cursor: url('data:image/svg+xml;utf8,<svg xmlns="http://www.w3.org/2000/svg" width="32" height="32" viewBox="0 0 24 24"><path fill="%23000" stroke="%23fff" stroke-width="1.5" d="M5.5 3.21V20.8c0 .45.54.67.85.35l4.86-4.86a.5.5 0 0 1 .35-.15h6.87c.45 0 .67-.54.35-.85L5.5 3.21z"/></svg>'), auto !important;
}

/* 6. Dyslexia Friendly (Comic Sans or specific fonts) */
body.a11y-dyslexia, body.a11y-dyslexia * {
  font-family: 'Comic Sans MS', 'OpenDyslexic', 'Segoe Print', sans-serif !important;
  letter-spacing: 0.1em !important;
}

/* 7. High Contrast (Already present, keeping existing rules) */
.high-contrast {
  background-color: black !important;
  color: white !important;
}
.high-contrast * {
  background-color: transparent !important;
  color: white !important;
  border-color: white !important;
  text-shadow: none !important;
  box-shadow: none !important;
}
.high-contrast button, 
.high-contrast a, 
.high-contrast input, 
.high-contrast select {
  background-color: black !important;
  color: yellow !important;
  border: 2px solid yellow !important;
}
.high-contrast button:focus,
.high-contrast input:focus {
  outline: 4px solid yellow !important;
}

/* 8. Dimming (Solgunlaştırma) */
body.a11y-dimming {
  filter: brightness(0.65) contrast(1.1) !important;
}

/* 9. Low Saturation (Düşük Doygunluk) */
body.a11y-low-saturation {
  filter: saturate(0.25) !important;
}

/* 10. High Saturation (Yüksek Doygunluk) */
body.a11y-high-saturation {
  filter: saturate(2) !important;
}

/* 11. Hide Images */
body.a11y-hide-images img, 
body.a11y-hide-images svg, 
body.a11y-hide-images video {
  visibility: hidden !important;
}

/* Avoid filter conflicts: if multiple are applied, we combine them */
body.a11y-dimming.a11y-low-saturation { filter: brightness(0.65) saturate(0.25) !important; }
body.a11y-dimming.a11y-high-saturation { filter: brightness(0.65) saturate(2) !important; }

/* Accessibility Focus States for general usage */
button:focus, a:focus, input:focus {
  outline: 3px solid #3b82f6 !important;
  outline-offset: 2px;
}
</style>
