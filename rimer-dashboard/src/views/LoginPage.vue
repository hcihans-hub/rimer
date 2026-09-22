<script setup>
import { ref, reactive, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { auth, toastStore } from '../auth'
import { i18n, theme } from '../lang'
import LangSelector from '../components/LangSelector.vue'
import * as api from '../services/ticketApi'

const colors = [
  'bg-white', 'bg-white', 'bg-white', 'bg-slate-100', // Çoğunluk beyaz/parlak
  'bg-blue-300', 'bg-indigo-300',                     // Mavi yıldızlar
  'bg-orange-300', 'bg-rose-300',                     // Kırmızı/Turuncu yıldızlar
  'bg-amber-100'                                      // Sarımsı
]

// Astronomik takımyıldız merkezleri (yıldız kümesi yoğunlukları)
const clusters = [
  { cx: 78, cy: 15 },  // Büyük Ayı (Ursa Major) bölgesi
  { cx: 55, cy: 12 },  // Küçük Ayı (Ursa Minor) bölgesi  
  { cx: 26, cy: 62 },  // Orion (Avcı) bölgesi
  { cx: 45, cy: 8 },   // Cassiopeia bölgesi
  { cx: 82, cy: 50 },  // Kuğu (Cygnus) bölgesi
  { cx: 12, cy: 35 },  // Aslan (Leo) bölgesi
  { cx: 52, cy: 82 },  // Akrep (Scorpius) bölgesi
]

// Takımyıldız çapa yıldızları (sabit, parlak, gerçek konumlarda)
const constellationAnchors = [
  // Büyük Ayı (Big Dipper) - kepçe şekli
  { top: '16%', left: '68%', size: '3.5px', color: 'bg-white', shadow: '0 0 10px rgba(255,255,255,0.9)', isTwinkling: false, duration: '0s', delay: '0s', baseOpacity: 1 },
  { top: '14%', left: '72%', size: '3px', color: 'bg-white', shadow: '0 0 6px rgba(255,255,255,0.8)', isTwinkling: true, duration: '4s', delay: '1s', baseOpacity: 0.95 },
  { top: '17%', left: '76%', size: '2.5px', color: 'bg-white', shadow: '0 0 5px rgba(255,255,255,0.7)', isTwinkling: false, duration: '0s', delay: '0s', baseOpacity: 0.9 },
  { top: '14%', left: '78%', size: '2.8px', color: 'bg-white', shadow: '0 0 5px rgba(255,255,255,0.7)', isTwinkling: true, duration: '5s', delay: '2s', baseOpacity: 0.9 },
  { top: '12%', left: '82%', size: '2.5px', color: 'bg-white', shadow: 'none', isTwinkling: false, duration: '0s', delay: '0s', baseOpacity: 0.85 },
  { top: '10%', left: '86%', size: '3px', color: 'bg-white', shadow: '0 0 6px rgba(255,255,255,0.8)', isTwinkling: false, duration: '0s', delay: '0s', baseOpacity: 0.95 },
  { top: '9%', left: '90%', size: '2.5px', color: 'bg-white', shadow: 'none', isTwinkling: true, duration: '3s', delay: '0.5s', baseOpacity: 0.85 },
  // Küçük Ayı + Kutup Yıldızı (Polaris)
  { top: '6%', left: '55%', size: '4.5px', color: 'bg-amber-100', shadow: '0 0 14px rgba(255,255,255,1)', isTwinkling: false, duration: '0s', delay: '0s', baseOpacity: 1 }, // ⭐ Polaris
  { top: '11%', left: '57%', size: '2px', color: 'bg-white', shadow: 'none', isTwinkling: true, duration: '4s', delay: '1s', baseOpacity: 0.7 },
  { top: '13%', left: '60%', size: '2px', color: 'bg-white', shadow: 'none', isTwinkling: false, duration: '0s', delay: '0s', baseOpacity: 0.65 },
  { top: '18%', left: '59%', size: '1.8px', color: 'bg-white', shadow: 'none', isTwinkling: true, duration: '5s', delay: '3s', baseOpacity: 0.6 },
  // Orion (Avcı) - omuz, kemer, ayak
  { top: '54%', left: '30%', size: '3px', color: 'bg-blue-300', shadow: '0 0 8px rgba(147,197,253,0.8)', isTwinkling: false, duration: '0s', delay: '0s', baseOpacity: 0.95 }, // Bellatrix
  { top: '61%', left: '24%', size: '2.5px', color: 'bg-white', shadow: '0 0 4px rgba(255,255,255,0.6)', isTwinkling: false, duration: '0s', delay: '0s', baseOpacity: 0.9 }, // Kemer
  { top: '61%', left: '26.5%', size: '2.8px', color: 'bg-white', shadow: '0 0 5px rgba(255,255,255,0.7)', isTwinkling: false, duration: '0s', delay: '0s', baseOpacity: 0.95 },
  { top: '61%', left: '29%', size: '2.5px', color: 'bg-white', shadow: '0 0 4px rgba(255,255,255,0.6)', isTwinkling: false, duration: '0s', delay: '0s', baseOpacity: 0.9 },
  { top: '69%', left: '30%', size: '3.8px', color: 'bg-blue-300', shadow: '0 0 12px rgba(147,197,253,0.9)', isTwinkling: false, duration: '0s', delay: '0s', baseOpacity: 1 }, // Rigel
  // Cassiopeia (W şekli)
  { top: '8%', left: '38%', size: '2.8px', color: 'bg-white', shadow: '0 0 6px rgba(255,255,255,0.8)', isTwinkling: true, duration: '4s', delay: '0s', baseOpacity: 0.9 },
  { top: '5%', left: '42%', size: '2.5px', color: 'bg-white', shadow: '0 0 4px rgba(255,255,255,0.6)', isTwinkling: false, duration: '0s', delay: '0s', baseOpacity: 0.85 },
  { top: '9%', left: '45%', size: '3px', color: 'bg-white', shadow: '0 0 7px rgba(255,255,255,0.8)', isTwinkling: true, duration: '3.5s', delay: '1.5s', baseOpacity: 0.9 },
  { top: '4%', left: '48%', size: '2.5px', color: 'bg-white', shadow: 'none', isTwinkling: false, duration: '0s', delay: '0s', baseOpacity: 0.8 },
  { top: '7%', left: '51%', size: '2.8px', color: 'bg-white', shadow: '0 0 5px rgba(255,255,255,0.7)', isTwinkling: true, duration: '5s', delay: '2s', baseOpacity: 0.85 },
]

const stars = Array.from({ length: 600 }, () => {
  let left, top;
  // %35 ihtimalle bir takımyıldız bölgesine yakın
  if (Math.random() < 0.35) {
    const cluster = clusters[Math.floor(Math.random() * clusters.length)]
    const offsetX = (Math.random() - 0.5) * 20
    const offsetY = (Math.random() - 0.5) * 20
    left = (cluster.cx + offsetX) + '%'
    top = (cluster.cy + offsetY) + '%'
  } else {
    // Serbest dağınık (field)
    left = Math.random() * 100 + '%'
    top = Math.random() * 100 + '%'
  }

  const color = colors[Math.floor(Math.random() * colors.length)]
  const sizeVal = Math.random() < 0.9 ? (Math.random() * 1.5 + 0.5) : (Math.random() * 2 + 2.5)
  const size = sizeVal + 'px'
  
  let shadow = 'none'
  const isBright = sizeVal > 2
  if (isBright) {
    shadow = color.includes('blue') ? '0 0 8px rgba(147,197,253,0.9)' 
           : color.includes('rose') ? '0 0 8px rgba(253,164,175,0.9)'
           : '0 0 8px rgba(255,255,255,0.9)'
  }

  return {
    top, left, size, color, shadow,
    isTwinkling: !isBright,
    duration: Math.random() * 3 + 2 + 's',
    delay: Math.random() * 5 + 's',
    baseOpacity: isBright ? 1 : (Math.random() * 0.8 + 0.2)
  }
});

// Takımyıldız çapa yıldızlarını ana yıldız dizisine ekle
const allStars = [...stars, ...constellationAnchors]


const router = useRouter()
const form = reactive({ email: '', password: '' })
const loading = ref(false)

// Ticker Logic
const tickers = ref([])
let fetchInterval = null

const loadTickers = async () => {
  try {
    const res = await api.getPublicTicker()
    if (res.data?.success) {
      tickers.value = res.data.data
    }
  } catch (e) {
    console.error('Failed to load public ticker', e)
  }
}

onMounted(() => {
  loadTickers()
  fetchInterval = setInterval(loadTickers, 30000)
})

onUnmounted(() => {
  if (fetchInterval) clearInterval(fetchInterval)
})

const login = async () => {
  if (!form.email || !form.password) return
  loading.value = true
  try {
    const success = await auth.login(form.email, form.password)
    if (success) {
      const user = auth.user
      const role = (user.role ?? '').toLowerCase()

      if (role === 'chartsrole') {
        router.push('/charts')
      } else if (role === 'admin' || role === 'staff') {
        router.push('/admin')
      } else {
        // Check if user has any chart permissions
        try {
          const res = await api.getCharts()
          const charts = res.data?.charts
          if (charts && Object.keys(charts).length > 0) {
            router.push('/charts')
          } else {
            router.push('/dashboard')
          }
        } catch {
          router.push('/dashboard')
        }
      }
    } else {
      toastStore.add(i18n.t.invalidCreds, 'error')
    }
  } catch (e) {
    toastStore.add(i18n.t.connError, 'error')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="min-h-screen w-full flex items-center justify-center p-4 py-12 sm:p-6 lg:p-8 relative overflow-x-hidden overflow-y-auto bg-sky-200 dark:bg-[#020617] transition-colors duration-500">
    
    <!-- COMMUNICATION EVOLUTION THEME BACKGROUND -->
    <div class="absolute inset-0 z-0 overflow-hidden pointer-events-none">
      
      <!-- TWINKLING STARS (Dark Mode Only) -->
      <div class="absolute inset-0 z-0 opacity-0 dark:opacity-100 transition-opacity duration-1000 pointer-events-none">
        <div v-for="(star, i) in allStars" :key="'star-'+i"
             class="absolute rounded-full"
             :class="[star.color, star.isTwinkling ? 'animate-twinkle' : '']"
             :style="{ 
               top: star.top, 
               left: star.left, 
               width: star.size, 
               height: star.size, 
               boxShadow: star.shadow,
               animationDuration: star.isTwinkling ? star.duration : 'auto',
               animationDelay: star.isTwinkling ? star.delay : '0s',
               opacity: star.baseOpacity
             }">
        </div>

        <!-- THE MOON (Realistic Photo, transparent PNG) -->
        <div class="absolute top-28 left-16 md:top-[108px] md:left-[72px] w-16 h-16 md:w-20 md:h-20 select-none pointer-events-none">
          <!-- Yıldızları maskeleyen dolu daire (arka plan rengi) -->
          <div class="absolute inset-0 rounded-full" style="background: #020617;"></div>
          <!-- Moon image (mix-blend-mode yok, normal render) -->
          <img src="/moon.png" alt="Moon"
               class="absolute inset-0 w-full h-full"
               style="opacity: 1;" />
        </div>

        <!-- SIRIUS STAR (Turkish flag style, in the crescent opening) -->
        <div class="absolute select-none pointer-events-none"
             style="top: 133px; left: 118px; transform: translate(-50%, -50%);
                    @media (min-width: 768px) { top: 138px; left: 146px; }">
        </div>
        <div class="absolute select-none pointer-events-none md:hidden"
             style="top: 133px; left: 118px; transform: translate(-50%, -50%);">
          <!-- Glow layers -->
          <div class="absolute rounded-full" style="width:9px;height:9px;top:50%;left:50%;transform:translate(-50%,-50%);background:radial-gradient(circle,rgba(255,255,255,0.95) 0%,rgba(200,225,255,0.5) 40%,transparent 70%);filter:blur(2px);"></div>
          <div class="absolute rounded-full" style="width:17px;height:17px;top:50%;left:50%;transform:translate(-50%,-50%);background:radial-gradient(circle,rgba(200,220,255,0.25) 0%,transparent 65%);filter:blur(4px);"></div>
          <!-- Core star dot -->
          <div class="absolute rounded-full bg-white" style="width:2px;height:2px;top:50%;left:50%;transform:translate(-50%,-50%);box-shadow:0 0 3px 1px rgba(255,255,255,1),0 0 6px 2px rgba(200,225,255,0.8),0 0 10px 3px rgba(180,210,255,0.4);"></div>
        </div>
        <!-- md+ version -->
        <div class="absolute select-none pointer-events-none hidden md:block"
             style="top: 138px; left: 146px; transform: translate(-50%, -50%);">
          <div class="absolute rounded-full" style="width:10px;height:10px;top:50%;left:50%;transform:translate(-50%,-50%);background:radial-gradient(circle,rgba(255,255,255,0.95) 0%,rgba(200,225,255,0.5) 40%,transparent 70%);filter:blur(2px);"></div>
          <div class="absolute rounded-full" style="width:20px;height:20px;top:50%;left:50%;transform:translate(-50%,-50%);background:radial-gradient(circle,rgba(200,220,255,0.25) 0%,transparent 65%);filter:blur(5px);"></div>
          <!-- Core star dot -->
          <div class="absolute rounded-full bg-white" style="width:2.5px;height:2.5px;top:50%;left:50%;transform:translate(-50%,-50%);box-shadow:0 0 3px 1px rgba(255,255,255,1),0 0 7px 2px rgba(200,225,255,0.8),0 0 12px 4px rgba(180,210,255,0.4);"></div>
        </div>



        <!-- SHOOTING STARS (Perfect Trajectory & Rare Spawns) -->
        <!-- Meteor 1: Klasik, Sağdan sola çapraz ok gibi süzülen -->
        <div class="absolute w-[150px] h-[2px] bg-gradient-to-r from-transparent via-white to-white opacity-0 animate-shooting-star-1" style="top: -10%; left: 90%; border-radius: 50%; filter: drop-shadow(0 0 6px rgba(255,255,255,1));"></div>
        <!-- Meteor 2: Sol üstten gelip ortada (atmosferde) eriyen -->
        <div class="absolute w-[100px] h-[2px] bg-gradient-to-r from-transparent via-blue-200 to-white opacity-0 animate-shooting-star-2" style="top: -10%; left: 40%; border-radius: 50%; filter: drop-shadow(0 0 6px rgba(255,255,255,0.8));"></div>

        <!-- SATELLITES -->
        <!-- Satellite 1: Soldan sağa yavaş (hızı artırıldı) -->
        <div class="absolute animate-satellite-orbit-1 flex items-center justify-center opacity-80">
          <div class="w-3 h-5 bg-slate-600/80 border border-slate-500 rounded-sm"></div>
          <div class="w-2.5 h-2.5 bg-slate-100 rounded-full mx-1 relative shadow-[0_0_15px_rgba(255,255,255,1)]">
            <div class="absolute inset-0 bg-white rounded-full animate-[pulse_1s_ease-in-out_infinite]"></div>
          </div>
          <div class="w-3 h-5 bg-slate-600/80 border border-slate-500 rounded-sm"></div>
        </div>

        <!-- Satellite 2: Sağ üstten sol alta çapraz, farklı hız -->
        <div class="absolute animate-satellite-orbit-2 flex items-center justify-center opacity-75">
          <div class="w-3 h-4 bg-slate-700/80 border border-slate-600 rounded-sm"></div>
          <div class="w-2 h-2 bg-slate-100 rounded-full mx-1 relative shadow-[0_0_12px_rgba(255,255,255,1)]">
            <div class="absolute inset-0 bg-white rounded-full animate-[pulse_2s_ease-in-out_infinite]"></div>
          </div>
          <div class="w-3 h-4 bg-slate-700/80 border border-slate-600 rounded-sm"></div>
        </div>

        <!-- Satellite 3: Aşağıdan yukarıya farklı bir açıyla -->
        <div class="absolute animate-satellite-orbit-3 flex items-center justify-center opacity-70">
          <div class="w-2 h-4 bg-slate-500/80 border border-slate-400 rounded-sm"></div>
          <div class="w-2 h-2 bg-slate-100 rounded-full mx-0.5 relative shadow-[0_0_12px_rgba(255,255,255,1)]">
            <div class="absolute inset-0 bg-white rounded-full animate-[pulse_1.5s_ease-in-out_infinite]"></div>
          </div>
          <div class="w-2 h-4 bg-slate-500/80 border border-slate-400 rounded-sm"></div>
        </div>
      </div>

      <!-- THE SUN (Light Mode Only) -->
      <div class="absolute inset-0 z-0 opacity-100 dark:opacity-0 transition-opacity duration-1000 pointer-events-none overflow-hidden">
        
        <!-- CLOUDS -->
        <div class="absolute top-[8%] left-[-20%] w-48 text-white/80 animate-[wind_40s_linear_infinite]">
          <svg viewBox="0 0 24 24" fill="currentColor"><path d="M17.5 19c2.485 0 4.5-2.015 4.5-4.5S19.985 10 17.5 10c-.3 0-.594.03-.88.087a6.002 6.002 0 0 0-11.238-1.077A5.002 5.002 0 0 0 5 19h12.5z"/></svg>
        </div>
        <div class="absolute top-[20%] left-[-20%] w-32 text-white/60 animate-[wind_30s_linear_infinite]" style="animation-delay: -15s">
          <svg viewBox="0 0 24 24" fill="currentColor"><path d="M17.5 19c2.485 0 4.5-2.015 4.5-4.5S19.985 10 17.5 10c-.3 0-.594.03-.88.087a6.002 6.002 0 0 0-11.238-1.077A5.002 5.002 0 0 0 5 19h12.5z"/></svg>
        </div>
        <div class="absolute top-[5%] left-[-20%] w-64 text-white/40 animate-[wind_60s_linear_infinite]" style="animation-delay: -35s">
          <svg viewBox="0 0 24 24" fill="currentColor"><path d="M17.5 19c2.485 0 4.5-2.015 4.5-4.5S19.985 10 17.5 10c-.3 0-.594.03-.88.087a6.002 6.002 0 0 0-11.238-1.077A5.002 5.002 0 0 0 5 19h12.5z"/></svg>
        </div>

      </div>

      <!-- Light Mode: light blue/gray, Dark Mode: subtle slate -->
      <div class="absolute inset-0 text-blue-300/50 dark:text-slate-700/50 transition-colors duration-500 pointer-events-none">
        
        <!-- REALISTIC RADIO TOWER (Bottom Right) -->
        <div class="absolute bottom-0 right-[4%] opacity-90 flex flex-col items-center">
          <!-- Broadcasting Waves (Slower and Thinner) -->
          <div class="absolute top-[5px] left-1/2 -translate-x-1/2 flex items-center justify-center">
            <div class="absolute w-[80px] h-[80px] rounded-full border border-blue-400/20 dark:border-indigo-400/20 animate-broadcast" style="animation-delay: 0s;"></div>
            <div class="absolute w-[80px] h-[80px] rounded-full border border-indigo-400/15 dark:border-blue-400/15 animate-broadcast" style="animation-delay: 8s;"></div>
            <div class="absolute w-[80px] h-[80px] rounded-full border border-blue-300/10 dark:border-indigo-300/10 animate-broadcast" style="animation-delay: 16s;"></div>
          </div>
          
          <!-- Realistic Steel Tower SVG (Scaled down by half) -->
          <svg class="w-20 h-32 relative z-10 text-slate-800 dark:text-slate-400 opacity-60" viewBox="0 0 100 150">
            <!-- Main Structure -->
            <polygon points="35,150 65,150 52,20 48,20" fill="currentColor" />
            <!-- Cross Beams (X) -->
            <g stroke="currentColor" stroke-width="1.5" opacity="0.9">
              <line x1="38" y1="130" x2="62" y2="110" />
              <line x1="62" y1="130" x2="38" y2="110" />
              <line x1="41" y1="110" x2="59" y2="90" />
              <line x1="59" y1="110" x2="41" y2="90" />
              <line x1="43" y1="90" x2="57" y2="70" />
              <line x1="57" y1="90" x2="43" y2="70" />
              <line x1="45" y1="70" x2="55" y2="50" />
              <line x1="55" y1="70" x2="45" y2="50" />
              <line x1="47" y1="50" x2="53" y2="30" />
              <line x1="53" y1="50" x2="47" y2="30" />
              <line x1="38" y1="130" x2="62" y2="130" />
              <line x1="41" y1="110" x2="59" y2="110" />
              <line x1="43" y1="90" x2="57" y2="90" />
              <line x1="45" y1="70" x2="55" y2="70" />
              <line x1="47" y1="50" x2="53" y2="50" />
              <line x1="48" y1="30" x2="52" y2="30" />
            </g>
            <!-- Antenna Pole -->
            <line x1="50" y1="20" x2="50" y2="0" stroke="currentColor" stroke-width="2.5" />
            <!-- Red Signal Light -->
            <circle cx="50" cy="0" r="4" fill="#ef4444" class="animate-morse-blink" style="filter: drop-shadow(0 0 5px rgba(239, 68, 68, 1));" />
          </svg>
        </div>
      </div>

      <div class="absolute inset-0 bg-[radial-gradient(ellipse_at_center,transparent_40%,rgba(248,250,252,1)_100%)] dark:bg-[radial-gradient(ellipse_at_center,transparent_40%,rgba(2,6,23,1)_100%)] transition-colors duration-500"></div>
      <div class="absolute top-[-10%] left-[-10%] w-[40%] h-[40%] rounded-full bg-blue-600/10 blur-[120px] animate-pulse" />
      <div class="absolute bottom-[-10%] right-[-10%] w-[50%] h-[50%] rounded-full bg-indigo-600/10 blur-[120px] animate-pulse" style="animation-delay: 2s" />
      <div class="absolute top-[20%] right-[10%] w-[30%] h-[30%] rounded-full bg-purple-600/5 blur-[100px]" />
    </div>

    <!-- Top Navigation Bar -->
    <div class="absolute top-8 left-8 right-8 flex items-center justify-between z-20">
      <div class="flex items-center gap-4">
        <div class="relative h-16 w-40 flex-shrink-0 cursor-pointer" @click="router.push('/dashboard')">
          <img src="/logo.png" alt="Rimer Logo" class="absolute inset-0 w-full h-full object-contain transition-opacity duration-200" :class="theme.dark ? 'opacity-0' : 'opacity-100'" />
          <img src="/logonight.png" alt="Rimer Logo" class="absolute inset-0 w-full h-full object-contain transition-opacity duration-200" :class="theme.dark ? 'opacity-100' : 'opacity-0'" />
        </div>
      </div>
      
      <div class="flex items-center gap-4 bg-white/50 dark:bg-slate-900/50 backdrop-blur-md p-1.5 rounded-2xl border border-white dark:border-slate-800 shadow-xl shadow-slate-200/50 dark:shadow-none">
        <LangSelector />
        <div class="w-px h-6 bg-slate-200 dark:bg-slate-800" />
        <button 
          @click="theme.toggle()" 
          class="p-2 rounded-xl text-slate-600 dark:text-slate-400 hover:bg-white dark:hover:bg-slate-800 hover:text-blue-600 dark:hover:text-blue-400 transition-all duration-300"
        >
          <span v-if="theme.dark" class="text-lg">☀️</span>
          <span v-else class="text-lg">🌙</span>
        </button>
      </div>
    </div>

    <!-- Login Card -->
    <div class="w-full max-w-[440px] relative z-10 group my-auto">
      <!-- Glow effect removed by user request -->
      
      <div class="relative bg-transparent border-none rounded-[30px] shadow-none p-5 sm:p-6 transition-all duration-500">
        
        <div class="text-center mb-4">
          <h1 class="text-2xl sm:text-3xl font-black text-slate-900 dark:text-white mb-1 tracking-tight">
            {{ i18n.t.welcomeBack }}
          </h1>
          <p class="text-slate-500 dark:text-slate-400 font-medium">
            {{ i18n.t.loginDesc }}
          </p>
        </div>

        <form @submit.prevent="login" class="space-y-4">
          <div class="space-y-2">
            <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-[0.2em] ml-1">
              {{ i18n.t.email }}
            </label>
            <div class="relative group/input">
              <div class="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none text-slate-400 group-focus-within/input:text-blue-500 transition-colors z-10">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 12a4 4 0 10-8 0 4 4 0 008 0zm0 0v1.5a2.5 2.5 0 005 0V12a9 9 0 10-9 9m4.5-1.206a8.959 8.959 0 01-4.5 1.206" /></svg>
              </div>
              <input 
                v-model="form.email" 
                type="email" 
                required
                class="w-full pl-12 pr-5 py-3 rounded-2xl bg-white/30 dark:bg-slate-800/30 backdrop-blur-md border border-white/40 dark:border-slate-700/50 text-slate-900 dark:text-white focus:ring-4 focus:ring-blue-500/10 focus:border-blue-500 outline-none transition-all duration-300 placeholder-slate-500 dark:placeholder-slate-400"
                placeholder="name@example.com"
              />
            </div>
          </div>

          <div class="space-y-2">
            <div class="flex items-center justify-between ml-1">
              <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-[0.2em]">
                {{ i18n.t.password }}
              </label>
              <button type="button" @click="router.push('/forgot-password')" class="text-[11px] font-bold text-blue-600 dark:text-blue-400 hover:underline uppercase tracking-wider">
                {{ i18n.t.forgotPassword }}
              </button>
            </div>
            <div class="relative group/input">
              <div class="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none text-slate-400 group-focus-within/input:text-blue-500 transition-colors z-10">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" /></svg>
              </div>
              <input 
                v-model="form.password" 
                type="password" 
                required
                class="w-full pl-12 pr-5 py-3 rounded-2xl bg-white/30 dark:bg-slate-800/30 backdrop-blur-md border border-white/40 dark:border-slate-700/50 text-slate-900 dark:text-white focus:ring-4 focus:ring-blue-500/10 focus:border-blue-500 outline-none transition-all duration-300 placeholder-slate-500 dark:placeholder-slate-400"
                placeholder="••••••••"
              />
            </div>
          </div>

          <button 
            type="submit" 
            :disabled="loading"
            class="w-full relative group/btn overflow-hidden py-3 bg-slate-900 dark:bg-white text-white dark:text-slate-900 rounded-2xl font-bold text-base shadow-xl active:scale-[0.98] transition-all duration-300 disabled:opacity-50 disabled:pointer-events-none flex items-center justify-center gap-3 mt-4"
          >
            <!-- Animated background on hover -->
            <div class="absolute inset-0 bg-gradient-to-r from-blue-600 to-indigo-600 opacity-0 group-hover/btn:opacity-100 transition-opacity duration-500" />
            <span v-if="loading" class="relative z-10 w-5 h-5 border-2 border-current/30 border-t-current rounded-full animate-spin" />
            <span class="relative z-10 transition-colors duration-500 group-hover/btn:text-white">
              {{ loading ? i18n.t.sending : i18n.t.login }}
            </span>
          </button>
        </form>

        <div class="mt-4 text-center">
          <p class="text-sm text-slate-500 dark:text-slate-400">
            {{ i18n.t.noAccount }} 
            <router-link to="/register" class="text-blue-600 dark:text-blue-400 font-bold hover:underline ml-1">
              {{ i18n.t.register }}
            </router-link>
          </p>
        </div>

        <div class="mt-4 pt-4 border-t border-slate-100 dark:border-slate-800 text-center">
          <p class="text-[10px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-widest mb-2">{{ i18n.t.visitorAction }}</p>
          <div class="flex flex-col sm:flex-row justify-center gap-3">
            <router-link to="/apply" class="flex-1 py-2 px-4 rounded-xl border border-white/30 dark:border-slate-700/50 bg-white/10 dark:bg-slate-800/20 backdrop-blur-md hover:bg-white/20 dark:hover:bg-slate-800/40 hover:border-white/50 text-slate-900 dark:text-white text-sm font-bold transition-all flex items-center justify-center gap-2 group">
              <span class="text-blue-600 dark:text-blue-400 group-hover:scale-110 transition-transform">📝</span>
              {{ i18n.t.newApplication }}
            </router-link>
            <router-link to="/track" class="flex-1 py-2 px-4 rounded-xl border border-white/30 dark:border-slate-700/50 bg-white/10 dark:bg-slate-800/20 backdrop-blur-md hover:bg-white/20 dark:hover:bg-slate-800/40 hover:border-white/50 text-slate-900 dark:text-white text-sm font-bold transition-all flex items-center justify-center gap-2 group">
              <span class="text-indigo-600 dark:text-indigo-400 group-hover:scale-110 transition-transform">🔍</span>
              {{ i18n.t.trackApplication }}
            </router-link>
          </div>
        </div>
      </div>
      
      <!-- Footer element moved outside -->
    </div>

    <!-- Footer Info (Absolutely positioned at the bottom) -->
    <div class="absolute bottom-6 left-0 right-0 w-full text-center z-10 pointer-events-none">
      <p class="text-xs text-slate-400 dark:text-slate-500 font-medium tracking-wide">
        {{ i18n.t.copyright }}
      </p>
    </div>

    <!-- Public Ticker Widget -->
    <div v-if="tickers.length > 0" class="fixed bottom-6 left-6 z-50 w-72 md:w-80 pointer-events-none">
      <div class="bg-transparent border-none rounded-2xl p-4 relative overflow-hidden transition-colors duration-500">
        <!-- Decoration -->
        <div class="absolute left-0 top-0 bottom-0 w-1 bg-[#1e3a5f]/40 rounded-l-2xl"></div>
        
        <div class="flex items-center gap-2 mb-3">
          <span class="w-1.5 h-1.5 rounded-full bg-blue-500 animate-pulse shadow-[0_0_8px_rgba(59,130,246,0.8)]"></span>
          <span class="text-[9px] font-black text-slate-500 dark:text-slate-400 uppercase tracking-widest">Canlı Sistem Akışı (Son Gelişmeler)</span>
        </div>
        
        <transition-group name="list" tag="div" class="space-y-3">
          <div v-for="(item, idx) in tickers" :key="item.timestamp + idx" class="border-l-2 border-slate-200 dark:border-slate-700/50 pl-3">
            <p class="text-xs font-bold text-slate-700 dark:text-slate-200 leading-relaxed">
              {{ item.message }}
            </p>
            <p class="text-[9px] font-medium text-slate-400 dark:text-slate-500 mt-1">
              {{ new Date(item.timestamp).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' }) }}
            </p>
          </div>
        </transition-group>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* Smooth transition for theme colors */
* { transition: background-color 0.5s ease, border-color 0.5s ease, color 0.3s ease; }

@keyframes twinkle {
  0%, 100% { opacity: 1; transform: scale(1); }
  50% { opacity: 0.2; transform: scale(0.9); }
}
.animate-twinkle {
  animation: twinkle 3s ease-in-out infinite;
}

@keyframes pulse {
  0%, 100% { opacity: 0.1; transform: scale(1); }
  50% { opacity: 0.15; transform: scale(1.05); }
}

@keyframes wind {
  0% { transform: translateX(-20vw); }
  100% { transform: translateX(120vw); }
}

@keyframes shooting-star-1 {
  0% { transform: rotate(145deg) translateX(0); opacity: 1; }
  3% { transform: rotate(145deg) translateX(3500px); opacity: 0; }
  100% { transform: rotate(145deg) translateX(3500px); opacity: 0; }
}
.animate-shooting-star-1 {
  animation: shooting-star-1 45s linear infinite; /* Her 45 saniyede 1 kez nadir düşer */
}

@keyframes shooting-star-2 {
  0% { transform: rotate(115deg) translateX(0); opacity: 1; }
  2% { transform: rotate(115deg) translateX(1200px); opacity: 0; } /* Ortada atmosferde yanar */
  100% { transform: rotate(115deg) translateX(1200px); opacity: 0; }
}
.animate-shooting-star-2 {
  animation: shooting-star-2 55s linear infinite;
  animation-delay: 15s;
}

@keyframes satellite-orbit-1 {
  0% { transform: translate(-20vw, 20vh) rotate(15deg) scale(0.6); }
  100% { transform: translate(120vw, 40vh) rotate(25deg) scale(0.6); }
}
.animate-satellite-orbit-1 {
  animation: satellite-orbit-1 35s linear infinite; /* Hızlandırıldı (Eskiden 55s'ydi) */
}

@keyframes satellite-orbit-2 {
  0% { transform: translate(120vw, 15vh) rotate(-35deg) scale(0.5); }
  100% { transform: translate(-20vw, 70vh) rotate(-25deg) scale(0.5); }
}
.animate-satellite-orbit-2 {
  animation: satellite-orbit-2 45s linear infinite; 
  animation-delay: 8s;
}

@keyframes satellite-orbit-3 {
  0% { transform: translate(30vw, 120vh) rotate(-65deg) scale(0.4); }
  100% { transform: translate(80vw, -20vh) rotate(-85deg) scale(0.4); }
}
.animate-satellite-orbit-3 {
  animation: satellite-orbit-3 28s linear infinite;
  animation-delay: 15s;
}
.animate-pulse {
  animation: pulse 8s ease-in-out infinite;
}

@keyframes broadcast {
  0% { transform: scale(0.1); opacity: 0.8; }
  100% { transform: scale(25); opacity: 0; }
}

.animate-broadcast {
  animation: broadcast 24s linear infinite;
}

.list-enter-active, .list-leave-active { transition: all 0.5s ease; }
.list-enter-from { opacity: 0; transform: translateY(-15px); }
.list-leave-to { opacity: 0; transform: translateY(15px); }

.animate-morse-blink {
  animation: morse-blink 42.3s linear infinite;
}

@keyframes morse-blink {
  0.00% { opacity: 1; }
  0.29% { opacity: 1; }
  0.30% { opacity: 0; }
  1.19% { opacity: 0; }
  1.20% { opacity: 1; }
  1.49% { opacity: 1; }
  1.50% { opacity: 0; }
  1.79% { opacity: 0; }
  1.80% { opacity: 1; }
  2.69% { opacity: 1; }
  2.70% { opacity: 0; }
  3.59% { opacity: 0; }
  3.60% { opacity: 1; }
  3.89% { opacity: 1; }
  3.90% { opacity: 0; }
  4.19% { opacity: 0; }
  4.20% { opacity: 1; }
  5.10% { opacity: 1; }
  5.11% { opacity: 0; }
  5.40% { opacity: 0; }
  5.41% { opacity: 1; }
  5.70% { opacity: 1; }
  5.71% { opacity: 0; }
  6.00% { opacity: 0; }
  6.01% { opacity: 1; }
  6.30% { opacity: 1; }
  6.31% { opacity: 0; }
  7.20% { opacity: 0; }
  7.21% { opacity: 1; }
  7.50% { opacity: 1; }
  7.51% { opacity: 0; }
  7.80% { opacity: 0; }
  7.81% { opacity: 1; }
  8.70% { opacity: 1; }
  8.71% { opacity: 0; }
  9.00% { opacity: 0; }
  9.01% { opacity: 1; }
  9.30% { opacity: 1; }
  9.31% { opacity: 0; }
  9.60% { opacity: 0; }
  9.61% { opacity: 1; }
  9.90% { opacity: 1; }
  9.91% { opacity: 0; }
  10.80% { opacity: 0; }
  10.81% { opacity: 1; }
  11.10% { opacity: 1; }
  11.11% { opacity: 0; }
  11.40% { opacity: 0; }
  11.41% { opacity: 1; }
  12.30% { opacity: 1; }
  12.31% { opacity: 0; }
  13.20% { opacity: 0; }
  13.21% { opacity: 1; }
  13.50% { opacity: 1; }
  13.51% { opacity: 0; }
  13.80% { opacity: 0; }
  13.81% { opacity: 1; }
  14.10% { opacity: 1; }
  14.11% { opacity: 0; }
  14.40% { opacity: 0; }
  14.41% { opacity: 1; }
  14.70% { opacity: 1; }
  14.71% { opacity: 0; }
  15.01% { opacity: 0; }
  15.02% { opacity: 1; }
  15.31% { opacity: 1; }
  15.32% { opacity: 0; }
  16.21% { opacity: 0; }
  16.22% { opacity: 1; }
  17.11% { opacity: 1; }
  17.12% { opacity: 0; }
  17.41% { opacity: 0; }
  17.42% { opacity: 1; }
  18.31% { opacity: 1; }
  18.32% { opacity: 0; }
  18.61% { opacity: 0; }
  18.62% { opacity: 1; }
  18.91% { opacity: 1; }
  18.92% { opacity: 0; }
  19.21% { opacity: 0; }
  19.22% { opacity: 1; }
  19.51% { opacity: 1; }
  19.52% { opacity: 0; }
  19.81% { opacity: 0; }
  19.82% { opacity: 1; }
  20.71% { opacity: 1; }
  20.72% { opacity: 0; }
  21.01% { opacity: 0; }
  21.02% { opacity: 1; }
  21.91% { opacity: 1; }
  21.92% { opacity: 0; }
  24.01% { opacity: 0; }
  24.02% { opacity: 1; }
  24.91% { opacity: 1; }
  24.92% { opacity: 0; }
  25.22% { opacity: 0; }
  25.23% { opacity: 1; }
  25.52% { opacity: 1; }
  25.53% { opacity: 0; }
  25.82% { opacity: 0; }
  25.83% { opacity: 1; }
  26.72% { opacity: 1; }
  26.73% { opacity: 0; }
  27.02% { opacity: 0; }
  27.03% { opacity: 1; }
  27.92% { opacity: 1; }
  27.93% { opacity: 0; }
  28.82% { opacity: 0; }
  28.83% { opacity: 1; }
  29.12% { opacity: 1; }
  29.13% { opacity: 0; }
  29.42% { opacity: 0; }
  29.43% { opacity: 1; }
  30.32% { opacity: 1; }
  30.33% { opacity: 0; }
  31.22% { opacity: 0; }
  31.23% { opacity: 1; }
  31.52% { opacity: 1; }
  31.53% { opacity: 0; }
  31.82% { opacity: 0; }
  31.83% { opacity: 1; }
  32.72% { opacity: 1; }
  32.73% { opacity: 0; }
  33.02% { opacity: 0; }
  33.03% { opacity: 1; }
  33.92% { opacity: 1; }
  33.93% { opacity: 0; }
  34.22% { opacity: 0; }
  34.23% { opacity: 1; }
  34.52% { opacity: 1; }
  34.53% { opacity: 0; }
  35.43% { opacity: 0; }
  35.44% { opacity: 1; }
  36.33% { opacity: 1; }
  36.34% { opacity: 0; }
  37.23% { opacity: 0; }
  37.24% { opacity: 1; }
  37.53% { opacity: 1; }
  37.54% { opacity: 0; }
  37.83% { opacity: 0; }
  37.84% { opacity: 1; }
  38.13% { opacity: 1; }
  38.14% { opacity: 0; }
  39.03% { opacity: 0; }
  39.04% { opacity: 1; }
  39.93% { opacity: 1; }
  39.94% { opacity: 0; }
  40.23% { opacity: 0; }
  40.24% { opacity: 1; }
  40.53% { opacity: 1; }
  40.54% { opacity: 0; }
  40.83% { opacity: 0; }
  40.84% { opacity: 1; }
  41.73% { opacity: 1; }
  41.74% { opacity: 0; }
  42.63% { opacity: 0; }
  42.64% { opacity: 1; }
  42.93% { opacity: 1; }
  42.94% { opacity: 0; }
  43.23% { opacity: 0; }
  43.24% { opacity: 1; }
  44.13% { opacity: 1; }
  44.14% { opacity: 0; }
  44.43% { opacity: 0; }
  44.44% { opacity: 1; }
  44.73% { opacity: 1; }
  44.74% { opacity: 0; }
  45.04% { opacity: 0; }
  45.05% { opacity: 1; }
  45.34% { opacity: 1; }
  45.35% { opacity: 0; }
  46.24% { opacity: 0; }
  46.25% { opacity: 1; }
  46.54% { opacity: 1; }
  46.55% { opacity: 0; }
  46.84% { opacity: 0; }
  46.85% { opacity: 1; }
  47.74% { opacity: 1; }
  47.75% { opacity: 0; }
  48.64% { opacity: 0; }
  48.65% { opacity: 1; }
  48.94% { opacity: 1; }
  48.95% { opacity: 0; }
  49.24% { opacity: 0; }
  49.25% { opacity: 1; }
  50.14% { opacity: 1; }
  50.15% { opacity: 0; }
  50.44% { opacity: 0; }
  50.45% { opacity: 1; }
  50.74% { opacity: 1; }
  50.75% { opacity: 0; }
  51.64% { opacity: 0; }
  51.65% { opacity: 1; }
  51.94% { opacity: 1; }
  51.95% { opacity: 0; }
  52.24% { opacity: 0; }
  52.25% { opacity: 1; }
  52.54% { opacity: 1; }
  52.55% { opacity: 0; }
  53.44% { opacity: 0; }
  53.45% { opacity: 1; }
  54.34% { opacity: 1; }
  54.35% { opacity: 0; }
  54.64% { opacity: 0; }
  54.65% { opacity: 1; }
  54.94% { opacity: 1; }
  54.95% { opacity: 0; }
  55.85% { opacity: 0; }
  55.86% { opacity: 1; }
  56.15% { opacity: 1; }
  56.16% { opacity: 0; }
  56.45% { opacity: 0; }
  56.46% { opacity: 1; }
  56.75% { opacity: 1; }
  56.76% { opacity: 0; }
  57.65% { opacity: 0; }
  57.66% { opacity: 1; }
  58.55% { opacity: 1; }
  58.56% { opacity: 0; }
  58.85% { opacity: 0; }
  58.86% { opacity: 1; }
  59.75% { opacity: 1; }
  59.76% { opacity: 0; }
  60.05% { opacity: 0; }
  60.06% { opacity: 1; }
  60.35% { opacity: 1; }
  60.36% { opacity: 0; }
  60.65% { opacity: 0; }
  60.66% { opacity: 1; }
  60.95% { opacity: 1; }
  60.96% { opacity: 0; }
  61.85% { opacity: 0; }
  61.86% { opacity: 1; }
  62.15% { opacity: 1; }
  62.16% { opacity: 0; }
  62.45% { opacity: 0; }
  62.46% { opacity: 1; }
  62.75% { opacity: 1; }
  62.76% { opacity: 0; }
  64.85% { opacity: 0; }
  64.86% { opacity: 1; }
  65.76% { opacity: 1; }
  65.77% { opacity: 0; }
  66.06% { opacity: 0; }
  66.07% { opacity: 1; }
  66.96% { opacity: 1; }
  66.97% { opacity: 0; }
  67.26% { opacity: 0; }
  67.27% { opacity: 1; }
  67.56% { opacity: 1; }
  67.57% { opacity: 0; }
  68.46% { opacity: 0; }
  68.47% { opacity: 1; }
  69.36% { opacity: 1; }
  69.37% { opacity: 0; }
  69.66% { opacity: 0; }
  69.67% { opacity: 1; }
  70.56% { opacity: 1; }
  70.57% { opacity: 0; }
  70.86% { opacity: 0; }
  70.87% { opacity: 1; }
  71.76% { opacity: 1; }
  71.77% { opacity: 0; }
  72.06% { opacity: 0; }
  72.07% { opacity: 1; }
  72.36% { opacity: 1; }
  72.37% { opacity: 0; }
  73.26% { opacity: 0; }
  73.27% { opacity: 1; }
  73.56% { opacity: 1; }
  73.57% { opacity: 0; }
  73.86% { opacity: 0; }
  73.87% { opacity: 1; }
  74.76% { opacity: 1; }
  74.77% { opacity: 0; }
  75.07% { opacity: 0; }
  75.08% { opacity: 1; }
  75.37% { opacity: 1; }
  75.38% { opacity: 0; }
  76.27% { opacity: 0; }
  76.28% { opacity: 1; }
  77.17% { opacity: 1; }
  77.18% { opacity: 0; }
  77.47% { opacity: 0; }
  77.48% { opacity: 1; }
  78.37% { opacity: 1; }
  78.38% { opacity: 0; }
  79.27% { opacity: 0; }
  79.28% { opacity: 1; }
  79.57% { opacity: 1; }
  79.58% { opacity: 0; }
  80.47% { opacity: 0; }
  80.48% { opacity: 1; }
  81.37% { opacity: 1; }
  81.38% { opacity: 0; }
  81.67% { opacity: 0; }
  81.68% { opacity: 1; }
  81.97% { opacity: 1; }
  81.98% { opacity: 0; }
  82.27% { opacity: 0; }
  82.28% { opacity: 1; }
  83.17% { opacity: 1; }
  83.18% { opacity: 0; }
  84.07% { opacity: 0; }
  84.08% { opacity: 1; }
  84.97% { opacity: 1; }
  84.98% { opacity: 0; }
  85.88% { opacity: 0; }
  85.89% { opacity: 1; }
  86.18% { opacity: 1; }
  86.19% { opacity: 0; }
  87.08% { opacity: 0; }
  87.09% { opacity: 1; }
  87.98% { opacity: 1; }
  87.99% { opacity: 0; }
  88.28% { opacity: 0; }
  88.29% { opacity: 1; }
  88.58% { opacity: 1; }
  88.59% { opacity: 0; }
  88.88% { opacity: 0; }
  88.89% { opacity: 1; }
  89.18% { opacity: 1; }
  89.19% { opacity: 0; }
  90.08% { opacity: 0; }
  90.09% { opacity: 1; }
  90.38% { opacity: 1; }
  90.39% { opacity: 0; }
  90.68% { opacity: 0; }
  90.69% { opacity: 1; }
  90.98% { opacity: 1; }
  90.99% { opacity: 0; }
  91.88% { opacity: 0; }
  91.89% { opacity: 1; }
  92.18% { opacity: 1; }
  92.19% { opacity: 0; }
  92.48% { opacity: 0; }
  92.49% { opacity: 1; }
  93.38% { opacity: 1; }
  93.39% { opacity: 0; }
  93.68% { opacity: 0; }
  93.69% { opacity: 1; }
  93.98% { opacity: 1; }
  93.99% { opacity: 0; }
  99.99% { opacity: 0; }
  100% { opacity: 0; }
}
</style>
