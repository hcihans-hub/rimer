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
  { top: '14%', left: '72%', size: '3px', color: 'bg-white', shadow: '0 0 6px rgba(255,255,255,0.8)', isTwinkling: false, duration: '0.2s', delay: '1s', baseOpacity: 0.95 },
  { top: '17%', left: '76%', size: '2.5px', color: 'bg-white', shadow: '0 0 5px rgba(255,255,255,0.7)', isTwinkling: false, duration: '0s', delay: '0s', baseOpacity: 0.9 },
  { top: '14%', left: '78%', size: '2.8px', color: 'bg-white', shadow: '0 0 5px rgba(255,255,255,0.7)', isTwinkling: false, duration: '0.3s', delay: '2s', baseOpacity: 0.9 },
  { top: '12%', left: '82%', size: '2.5px', color: 'bg-white', shadow: 'none', isTwinkling: false, duration: '0s', delay: '0s', baseOpacity: 0.85 },
  { top: '10%', left: '86%', size: '3px', color: 'bg-white', shadow: '0 0 6px rgba(255,255,255,0.8)', isTwinkling: false, duration: '0s', delay: '0s', baseOpacity: 0.95 },
  { top: '9%', left: '90%', size: '2.5px', color: 'bg-white', shadow: 'none', isTwinkling: false, duration: '0.15s', delay: '0.5s', baseOpacity: 0.85 },
  // Küçük Ayı + Kutup Yıldızı (Polaris)
  { top: '6%', left: '55%', size: '4.5px', color: 'bg-amber-100', shadow: '0 0 14px rgba(255,255,255,1)', isTwinkling: false, duration: '0s', delay: '0s', baseOpacity: 1 }, // ⭐ Polaris
  { top: '11%', left: '57%', size: '2px', color: 'bg-white', shadow: 'none', isTwinkling: false, duration: '0.2s', delay: '1s', baseOpacity: 0.7 },
  { top: '13%', left: '60%', size: '2px', color: 'bg-white', shadow: 'none', isTwinkling: false, duration: '0s', delay: '0s', baseOpacity: 0.65 },
  { top: '18%', left: '59%', size: '1.8px', color: 'bg-white', shadow: 'none', isTwinkling: false, duration: '0.3s', delay: '3s', baseOpacity: 0.6 },
  // Orion (Avcı) - omuz, kemer, ayak
  { top: '54%', left: '30%', size: '3px', color: 'bg-blue-300', shadow: '0 0 8px rgba(147,197,253,0.8)', isTwinkling: false, duration: '0s', delay: '0s', baseOpacity: 0.95 }, // Bellatrix
  { top: '61%', left: '24%', size: '2.5px', color: 'bg-white', shadow: '0 0 4px rgba(255,255,255,0.6)', isTwinkling: false, duration: '0s', delay: '0s', baseOpacity: 0.9 }, // Kemer
  { top: '61%', left: '26.5%', size: '2.8px', color: 'bg-white', shadow: '0 0 5px rgba(255,255,255,0.7)', isTwinkling: false, duration: '0s', delay: '0s', baseOpacity: 0.95 },
  { top: '61%', left: '29%', size: '2.5px', color: 'bg-white', shadow: '0 0 4px rgba(255,255,255,0.6)', isTwinkling: false, duration: '0s', delay: '0s', baseOpacity: 0.9 },
  { top: '69%', left: '30%', size: '3.8px', color: 'bg-blue-300', shadow: '0 0 12px rgba(147,197,253,0.9)', isTwinkling: false, duration: '0s', delay: '0s', baseOpacity: 1 }, // Rigel
  // Cassiopeia (W şekli)
  { top: '8%', left: '38%', size: '2.8px', color: 'bg-white', shadow: '0 0 6px rgba(255,255,255,0.8)', isTwinkling: false, duration: '0.2s', delay: '0s', baseOpacity: 0.9 },
  { top: '5%', left: '42%', size: '2.5px', color: 'bg-white', shadow: '0 0 4px rgba(255,255,255,0.6)', isTwinkling: false, duration: '0s', delay: '0s', baseOpacity: 0.85 },
  { top: '9%', left: '45%', size: '3px', color: 'bg-white', shadow: '0 0 7px rgba(255,255,255,0.8)', isTwinkling: false, duration: '0.25s', delay: '1.5s', baseOpacity: 0.9 },
  { top: '4%', left: '48%', size: '2.5px', color: 'bg-white', shadow: 'none', isTwinkling: false, duration: '0s', delay: '0s', baseOpacity: 0.8 },
  { top: '7%', left: '51%', size: '2.8px', color: 'bg-white', shadow: '0 0 5px rgba(255,255,255,0.7)', isTwinkling: false, duration: '0.3s', delay: '2s', baseOpacity: 0.85 },
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
    isTwinkling: sizeVal <= 0.9 && Math.random() < 0.5,
    duration: Math.random() * 0.25 + 0.15 + 's',
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

        <!-- ANDROMEDA GALAKSISI (M31, küçük, soluk) -->
        <div class="absolute pointer-events-none select-none" style="top:32%;right:12%;width:120px;height:45px;transform:rotate(-35deg);">
          <div class="absolute" style="inset:-15px;background:radial-gradient(ellipse at 50% 50%,rgba(220,210,200,0.06) 0%,rgba(200,190,180,0.03) 40%,transparent 70%);filter:blur(8px);"></div>
          <div class="absolute" style="inset:0;background:radial-gradient(ellipse at 50% 50%,rgba(255,245,230,0.1) 0%,rgba(220,210,190,0.05) 45%,transparent 75%);filter:blur(5px);"></div>
          <div class="absolute" style="top:50%;left:50%;transform:translate(-50%,-50%);width:18px;height:8px;background:radial-gradient(ellipse at 50% 50%,rgba(255,250,235,0.45) 0%,rgba(255,240,210,0.2) 50%,transparent 80%);filter:blur(2px);border-radius:50%;"></div>
          <div class="absolute rounded-full" style="top:50%;left:50%;transform:translate(-50%,-50%);width:2px;height:2px;background:rgba(255,252,240,0.7);box-shadow:0 0 4px 1px rgba(255,245,220,0.4);"></div>
        </div>

        <!-- UZAK GALAKSI 1 (Yatay, çok soluk) -->
        <div class="absolute pointer-events-none select-none" style="top:65%;left:20%;width:90px;height:30px;transform:rotate(15deg);">
          <div class="absolute" style="inset:-10px;background:radial-gradient(ellipse at 50% 50%,rgba(180,210,255,0.05) 0%,transparent 70%);filter:blur(6px);"></div>
          <div class="absolute" style="inset:0;background:radial-gradient(ellipse at 50% 50%,rgba(220,235,255,0.08) 0%,transparent 75%);filter:blur(4px);"></div>
          <div class="absolute" style="top:50%;left:50%;transform:translate(-50%,-50%);width:12px;height:5px;background:radial-gradient(ellipse at 50% 50%,rgba(255,255,255,0.3) 0%,transparent 80%);filter:blur(1px);border-radius:50%;"></div>
          <div class="absolute rounded-full" style="top:50%;left:50%;transform:translate(-50%,-50%);width:1.5px;height:1.5px;background:rgba(255,255,255,0.6);box-shadow:0 0 3px 1px rgba(220,235,255,0.3);"></div>
        </div>

        <!-- UZAK GALAKSI 2 (Dikeyimsi, soluk, küçük) -->
        <div class="absolute pointer-events-none select-none" style="top:20%;left:40%;width:60px;height:20px;transform:rotate(75deg);">
          <div class="absolute" style="inset:-8px;background:radial-gradient(ellipse at 50% 50%,rgba(255,200,200,0.05) 0%,transparent 70%);filter:blur(5px);"></div>
          <div class="absolute" style="inset:0;background:radial-gradient(ellipse at 50% 50%,rgba(255,230,230,0.07) 0%,transparent 75%);filter:blur(3px);"></div>
          <div class="absolute" style="top:50%;left:50%;transform:translate(-50%,-50%);width:8px;height:4px;background:radial-gradient(ellipse at 50% 50%,rgba(255,255,255,0.25) 0%,transparent 80%);filter:blur(1px);border-radius:50%;"></div>
          <div class="absolute rounded-full" style="top:50%;left:50%;transform:translate(-50%,-50%);width:1px;height:1px;background:rgba(255,255,255,0.5);"></div>
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
        <!-- Satellite 1: Soldan sağa yavaş -->
        <div class="absolute animate-satellite-orbit-1 flex items-center justify-center opacity-75">
          <div class="w-[2px] h-[2px] bg-white rounded-full relative" style="box-shadow: 0 0 2px 1px rgba(255,255,255,0.6), 0 0 5px 1px rgba(255,255,255,0.3);">
            <div class="absolute inset-0 bg-white rounded-full animate-[pulse_1s_ease-in-out_infinite]" style="box-shadow: 0 0 6px 2px rgba(255,255,255,0.2);"></div>
          </div>
        </div>

        <!-- Satellite 2: Sağ üstten sol alta çapraz -->
        <div class="absolute animate-satellite-orbit-2 flex items-center justify-center opacity-60">
          <div class="w-[1.5px] h-[1.5px] bg-white rounded-full relative" style="box-shadow: 0 0 1px 1px rgba(255,255,255,0.5), 0 0 3px 1px rgba(255,255,255,0.2);">
            <div class="absolute inset-0 bg-white rounded-full animate-[pulse_1.5s_ease-in-out_infinite]" style="box-shadow: 0 0 4px 1px rgba(255,255,255,0.1);"></div>
          </div>
        </div>

        <!-- Satellite 3: Aşağıdan yukarıya farklı bir açıyla -->
        <div class="absolute animate-satellite-orbit-3 flex items-center justify-center opacity-70">
          <div class="w-[2px] h-[2px] bg-white rounded-full relative" style="box-shadow: 0 0 2px 1px rgba(255,255,255,0.6), 0 0 4px 1px rgba(255,255,255,0.3);">
            <div class="absolute inset-0 bg-white rounded-full animate-[pulse_1.2s_ease-in-out_infinite]" style="box-shadow: 0 0 5px 2px rgba(255,255,255,0.2);"></div>
          </div>
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
            <circle cx="50" cy="0" r="4" fill="#f97316" class="animate-morse-blink" style="filter: drop-shadow(0 0 7px rgba(251, 146, 60, 1)) drop-shadow(0 0 14px rgba(249, 115, 22, 0.8));" />
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
  50% { opacity: 0.5; transform: scale(0.95); }
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
  animation: morse-blink 66.8s linear infinite;
}

@keyframes morse-blink {
  0.00% { opacity: 1; }
  0.18% { opacity: 0; }
  0.36% { opacity: 1; }
  0.90% { opacity: 0; }
  1.08% { opacity: 1; }
  1.62% { opacity: 0; }
  1.80% { opacity: 1; }
  1.97% { opacity: 0; }
  2.15% { opacity: 1; }
  2.33% { opacity: 0; }
  2.87% { opacity: 1; }
  3.05% { opacity: 0; }
  3.23% { opacity: 1; }
  3.41% { opacity: 0; }
  3.59% { opacity: 1; }
  4.13% { opacity: 0; }
  4.31% { opacity: 1; }
  4.85% { opacity: 0; }
  5.39% { opacity: 1; }
  5.57% { opacity: 0; }
  5.75% { opacity: 1; }
  6.28% { opacity: 0; }
  6.46% { opacity: 1; }
  7.00% { opacity: 0; }
  7.18% { opacity: 1; }
  7.36% { opacity: 0; }
  7.90% { opacity: 1; }
  8.08% { opacity: 0; }
  8.26% { opacity: 1; }
  8.44% { opacity: 0; }
  8.62% { opacity: 1; }
  8.80% { opacity: 0; }
  8.98% { opacity: 1; }
  9.16% { opacity: 0; }
  9.69% { opacity: 1; }
  9.87% { opacity: 0; }
  10.41% { opacity: 1; }
  10.59% { opacity: 0; }
  10.77% { opacity: 1; }
  10.95% { opacity: 0; }
  11.13% { opacity: 1; }
  11.31% { opacity: 0; }
  11.85% { opacity: 1; }
  12.03% { opacity: 0; }
  12.21% { opacity: 1; }
  12.39% { opacity: 0; }
  12.93% { opacity: 1; }
  13.46% { opacity: 0; }
  13.64% { opacity: 1; }
  14.18% { opacity: 0; }
  14.36% { opacity: 1; }
  14.54% { opacity: 0; }
  14.72% { opacity: 1; }
  14.90% { opacity: 0; }
  16.16% { opacity: 1; }
  16.70% { opacity: 0; }
  16.88% { opacity: 1; }
  17.06% { opacity: 0; }
  17.24% { opacity: 1; }
  17.77% { opacity: 0; }
  18.31% { opacity: 1; }
  18.49% { opacity: 0; }
  18.67% { opacity: 1; }
  18.85% { opacity: 0; }
  19.39% { opacity: 1; }
  19.93% { opacity: 0; }
  20.11% { opacity: 1; }
  20.65% { opacity: 0; }
  20.83% { opacity: 1; }
  21.01% { opacity: 0; }
  21.18% { opacity: 1; }
  21.36% { opacity: 0; }
  21.54% { opacity: 1; }
  22.08% { opacity: 0; }
  22.26% { opacity: 1; }
  22.80% { opacity: 0; }
  24.06% { opacity: 1; }
  24.24% { opacity: 0; }
  24.42% { opacity: 1; }
  24.96% { opacity: 0; }
  25.49% { opacity: 1; }
  25.67% { opacity: 0; }
  25.85% { opacity: 1; }
  26.39% { opacity: 0; }
  26.57% { opacity: 1; }
  26.75% { opacity: 0; }
  26.93% { opacity: 1; }
  27.11% { opacity: 0; }
  27.65% { opacity: 1; }
  27.83% { opacity: 0; }
  28.01% { opacity: 1; }
  28.55% { opacity: 0; }
  28.73% { opacity: 1; }
  28.90% { opacity: 0; }
  29.08% { opacity: 1; }
  29.26% { opacity: 0; }
  29.80% { opacity: 1; }
  29.98% { opacity: 0; }
  30.16% { opacity: 1; }
  30.70% { opacity: 0; }
  31.24% { opacity: 1; }
  31.42% { opacity: 0; }
  31.60% { opacity: 1; }
  31.78% { opacity: 0; }
  31.96% { opacity: 1; }
  32.14% { opacity: 0; }
  32.32% { opacity: 1; }
  32.50% { opacity: 0; }
  33.75% { opacity: 1; }
  33.93% { opacity: 0; }
  34.11% { opacity: 1; }
  34.29% { opacity: 0; }
  34.47% { opacity: 1; }
  34.65% { opacity: 0; }
  34.83% { opacity: 1; }
  35.01% { opacity: 0; }
  35.55% { opacity: 1; }
  35.73% { opacity: 0; }
  36.27% { opacity: 1; }
  36.45% { opacity: 0; }
  36.62% { opacity: 1; }
  37.16% { opacity: 0; }
  37.34% { opacity: 1; }
  37.52% { opacity: 0; }
  38.78% { opacity: 1; }
  38.96% { opacity: 0; }
  39.14% { opacity: 1; }
  39.68% { opacity: 0; }
  39.86% { opacity: 1; }
  40.39% { opacity: 0; }
  40.57% { opacity: 1; }
  40.75% { opacity: 0; }
  40.93% { opacity: 1; }
  41.11% { opacity: 0; }
  41.65% { opacity: 1; }
  41.83% { opacity: 0; }
  42.37% { opacity: 1; }
  42.91% { opacity: 0; }
  43.09% { opacity: 1; }
  43.27% { opacity: 0; }
  43.45% { opacity: 1; }
  43.99% { opacity: 0; }
  44.17% { opacity: 1; }
  44.70% { opacity: 0; }
  45.24% { opacity: 1; }
  45.42% { opacity: 0; }
  45.60% { opacity: 1; }
  45.78% { opacity: 0; }
  47.04% { opacity: 1; }
  47.22% { opacity: 0; }
  47.40% { opacity: 1; }
  47.58% { opacity: 0; }
  48.11% { opacity: 1; }
  48.29% { opacity: 0; }
  48.47% { opacity: 1; }
  49.01% { opacity: 0; }
  49.19% { opacity: 1; }
  49.73% { opacity: 0; }
  49.91% { opacity: 1; }
  50.09% { opacity: 0; }
  50.27% { opacity: 1; }
  50.45% { opacity: 0; }
  50.99% { opacity: 1; }
  51.17% { opacity: 0; }
  51.35% { opacity: 1; }
  51.53% { opacity: 0; }
  52.06% { opacity: 1; }
  52.60% { opacity: 0; }
  53.14% { opacity: 1; }
  53.32% { opacity: 0; }
  53.86% { opacity: 1; }
  54.40% { opacity: 0; }
  54.58% { opacity: 1; }
  54.76% { opacity: 0; }
  55.30% { opacity: 1; }
  55.83% { opacity: 0; }
  56.01% { opacity: 1; }
  56.19% { opacity: 0; }
  56.37% { opacity: 1; }
  56.55% { opacity: 0; }
  57.09% { opacity: 1; }
  57.27% { opacity: 0; }
  57.45% { opacity: 1; }
  57.63% { opacity: 0; }
  58.17% { opacity: 1; }
  58.35% { opacity: 0; }
  58.53% { opacity: 1; }
  59.07% { opacity: 0; }
  59.25% { opacity: 1; }
  59.43% { opacity: 0; }
  59.96% { opacity: 1; }
  60.50% { opacity: 0; }
  60.68% { opacity: 1; }
  60.86% { opacity: 0; }
  61.04% { opacity: 1; }
  61.58% { opacity: 0; }
  61.76% { opacity: 1; }
  61.94% { opacity: 0; }
  62.12% { opacity: 1; }
  62.66% { opacity: 0; }
  62.84% { opacity: 1; }
  63.02% { opacity: 0; }
  64.27% { opacity: 1; }
  64.45% { opacity: 0; }
  64.63% { opacity: 1; }
  64.81% { opacity: 0; }
  64.99% { opacity: 1; }
  65.17% { opacity: 0; }
  65.35% { opacity: 1; }
  65.53% { opacity: 0; }
  66.07% { opacity: 1; }
  66.25% { opacity: 0; }
  66.79% { opacity: 1; }
  66.97% { opacity: 0; }
  67.15% { opacity: 1; }
  67.68% { opacity: 0; }
  67.86% { opacity: 1; }
  68.04% { opacity: 0; }
  69.30% { opacity: 1; }
  69.48% { opacity: 0; }
  69.66% { opacity: 1; }
  70.20% { opacity: 0; }
  70.38% { opacity: 1; }
  70.92% { opacity: 0; }
  71.10% { opacity: 1; }
  71.27% { opacity: 0; }
  71.45% { opacity: 1; }
  71.63% { opacity: 0; }
  72.17% { opacity: 1; }
  72.35% { opacity: 0; }
  72.89% { opacity: 1; }
  73.43% { opacity: 0; }
  73.61% { opacity: 1; }
  73.79% { opacity: 0; }
  73.97% { opacity: 1; }
  74.51% { opacity: 0; }
  74.69% { opacity: 1; }
  75.22% { opacity: 0; }
  75.76% { opacity: 1; }
  75.94% { opacity: 0; }
  76.12% { opacity: 1; }
  76.30% { opacity: 0; }
  77.56% { opacity: 1; }
  78.10% { opacity: 0; }
  78.28% { opacity: 1; }
  78.82% { opacity: 0; }
  78.99% { opacity: 1; }
  79.17% { opacity: 0; }
  79.71% { opacity: 1; }
  80.25% { opacity: 0; }
  80.43% { opacity: 1; }
  80.97% { opacity: 0; }
  81.15% { opacity: 1; }
  81.69% { opacity: 0; }
  81.87% { opacity: 1; }
  82.05% { opacity: 0; }
  82.59% { opacity: 1; }
  82.76% { opacity: 0; }
  82.94% { opacity: 1; }
  83.48% { opacity: 0; }
  83.66% { opacity: 1; }
  83.84% { opacity: 0; }
  84.38% { opacity: 1; }
  84.56% { opacity: 0; }
  85.10% { opacity: 1; }
  85.64% { opacity: 0; }
  85.82% { opacity: 1; }
  86.00% { opacity: 0; }
  86.54% { opacity: 1; }
  87.07% { opacity: 0; }
  87.25% { opacity: 1; }
  87.43% { opacity: 0; }
  87.61% { opacity: 1; }
  87.79% { opacity: 0; }
  88.33% { opacity: 1; }
  88.51% { opacity: 0; }
  88.69% { opacity: 1; }
  88.87% { opacity: 0; }
  89.41% { opacity: 1; }
  89.59% { opacity: 0; }
  89.77% { opacity: 1; }
  90.31% { opacity: 0; }
  90.48% { opacity: 1; }
  90.66% { opacity: 0; }
  91.20% { opacity: 1; }
  91.38% { opacity: 0; }
  91.56% { opacity: 1; }
  92.10% { opacity: 0; }
  92.28% { opacity: 1; }
  92.46% { opacity: 0; }
  92.64% { opacity: 1; }
  93.18% { opacity: 0; }
  93.36% { opacity: 1; }
  93.54% { opacity: 0; }
  93.72% { opacity: 1; }
  94.25% { opacity: 0; }
  94.43% { opacity: 1; }
  94.61% { opacity: 0; }
  100% { opacity: 0; }
}
</style>
