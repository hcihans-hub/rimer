<script setup>
import { ref, onMounted } from 'vue'
import { announcementApi } from '../api/announcementApi'
import { isAuthenticated } from '../auth'

const announcements = ref([])
const loading = ref(true)
const currentIndex = ref(0)

const fetchAnnouncements = async () => {
  if (!isAuthenticated()) {
    loading.value = false
    return
  }
  try {
    const res = await announcementApi.getActive()
    if (res.data.success && res.data.data.length > 0) {
      announcements.value = res.data.data
    }
  } catch (error) {
    console.error('Duyurular yüklenemedi', error)
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  fetchAnnouncements()
  
  // Rotate announcements if more than 1
  setInterval(() => {
    if (announcements.value.length > 1) {
      currentIndex.value = (currentIndex.value + 1) % announcements.value.length
    }
  }, 5000)
})
</script>

<template>
  <div v-if="!loading && announcements.length > 0" class="bg-indigo-600 text-white shadow-lg overflow-hidden relative group">
    <!-- Background pattern -->
    <div class="absolute inset-0 opacity-10 pointer-events-none" style="background-image: radial-gradient(circle at 2px 2px, white 1px, transparent 0); background-size: 20px 20px;"></div>
    
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
      <div class="flex items-center justify-between py-3">
        <div class="flex items-center gap-3 flex-1 overflow-hidden">
          <span class="flex h-8 w-8 items-center justify-center rounded-lg bg-white/20 backdrop-blur-md shrink-0">
            <svg class="w-5 h-5 text-white animate-pulse" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5.882V19.24a1.76 1.76 0 01-3.417.592l-2.147-6.15M18 13a3 3 0 100-6M5.436 13.683A4.001 4.001 0 017 6h1.832c4.1 0 7.625-1.234 9.168-3v14c-1.543-1.766-5.067-3-9.168-3H7a3.988 3.988 0 01-1.564-.317z"></path></svg>
          </span>
          <div class="flex flex-col relative w-full h-10 justify-center">
            <transition-group name="fade-slide">
              <div v-for="(a, index) in announcements" :key="a.id" v-show="index === currentIndex" class="absolute w-full truncate">
                <p class="font-black text-sm truncate text-white tracking-wide">{{ a.title }}</p>
                <p class="text-xs text-white/80 truncate mt-0.5">{{ a.content }}</p>
              </div>
            </transition-group>
          </div>
        </div>
        
        <div v-if="announcements.length > 1" class="flex gap-1 ml-4 shrink-0">
          <button v-for="(a, index) in announcements" :key="'dot-'+a.id" @click="currentIndex = index" :class="index === currentIndex ? 'bg-white scale-125' : 'bg-white/40'" class="w-1.5 h-1.5 rounded-full transition-all duration-300"></button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.fade-slide-enter-active,
.fade-slide-leave-active {
  transition: all 0.5s ease;
}
.fade-slide-enter-from {
  opacity: 0;
  transform: translateY(10px);
}
.fade-slide-leave-to {
  opacity: 0;
  transform: translateY(-10px);
}
</style>
