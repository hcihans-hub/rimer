<script setup>
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { auth, toastStore } from '../auth'
import { i18n, theme } from '../lang'
import LangSelector from '../components/LangSelector.vue'

const router = useRouter()
const form = reactive({ firstName: '', lastName: '', email: '', password: '', confirmPassword: '' })
const loading = ref(false)
const errorMessage = ref('')

const register = async () => {
  errorMessage.value = ''
  if (!form.firstName || !form.lastName || !form.email || !form.password || !form.confirmPassword) {
    errorMessage.value = 'Lütfen tüm alanları doldurun.'
    return
  }
  if (form.password !== form.confirmPassword) {
    errorMessage.value = 'Şifreler uyuşmuyor.'
    return
  }
  loading.value = true
  try {
    const success = await auth.register(form.firstName, form.lastName, form.email, form.password, form.confirmPassword)
    if (success) {
      toastStore.add(i18n.t.welcomeBack, 'success')
      router.push('/dashboard')
    } else {
      errorMessage.value = 'Kayıt işlemi başarısız oldu.'
    }
  } catch (e) {
    // Backend'den gelen detaylı hata mesajlarını göster
    if (e.response?.data?.errors) {
      const msgs = Object.values(e.response.data.errors).flat()
      errorMessage.value = msgs.join(', ')
    } else {
      errorMessage.value = e.response?.data?.message || i18n.t.connError
    }
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="min-h-screen flex items-center justify-center p-6 relative overflow-hidden bg-slate-50 dark:bg-[#020617] transition-colors duration-500">
    
    <!-- Premium Mesh Gradient Background -->
    <div class="absolute inset-0 z-0 overflow-hidden pointer-events-none">
      <div class="absolute top-[-10%] left-[-10%] w-[40%] h-[40%] rounded-full bg-blue-600/10 blur-[120px] animate-pulse" />
      <div class="absolute bottom-[-10%] right-[-10%] w-[50%] h-[50%] rounded-full bg-indigo-600/10 blur-[120px] animate-pulse" style="animation-delay: 2s" />
    </div>

    <!-- Top Navigation Bar -->
    <div class="absolute top-8 left-8 right-8 flex items-center justify-between z-20">
      <div class="flex items-center gap-2 cursor-pointer" @click="router.push('/login')">
        <div class="w-10 h-10 rounded-xl bg-gradient-to-tr from-blue-600 to-indigo-600 flex items-center justify-center shadow-lg shadow-blue-500/20">
          <span class="text-white font-black text-xl">R</span>
        </div>
        <span class="text-xl font-black text-slate-900 dark:text-white tracking-tight">{{ i18n.t.appName }}</span>
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

    <!-- Register Card -->
    <div class="w-full max-w-[440px] relative z-10 group">
      <div class="absolute -inset-1 bg-gradient-to-r from-blue-600 to-indigo-600 rounded-[32px] blur opacity-10 group-hover:opacity-20 transition duration-1000" />
      
      <div class="relative bg-white/80 dark:bg-slate-900/80 backdrop-blur-2xl border border-white dark:border-slate-800 rounded-[30px] shadow-2xl p-10 transition-all duration-500">
        
        <div class="text-center mb-10">
          <div class="inline-flex items-center justify-center w-16 h-16 rounded-2xl bg-slate-50 dark:bg-slate-800 mb-6 group-hover:scale-110 transition-transform duration-500">
            <span class="text-3xl">📝</span>
          </div>
          <h1 class="text-3xl font-black text-slate-900 dark:text-white mb-2 tracking-tight">
            {{ i18n.t.register }}
          </h1>
          <p class="text-slate-500 dark:text-slate-400 font-medium">
            Create your account to start
          </p>
        </div>

        <form @submit.prevent="register" class="space-y-5">
          <!-- First Name & Last Name Grid -->
          <div class="grid grid-cols-2 gap-4">
            <div class="space-y-2">
              <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-[0.2em] ml-1">
                {{ i18n.t.firstName }}
              </label>
              <div class="relative group/input">
                <div class="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none text-slate-400 group-focus-within/input:text-blue-500 transition-colors">
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" /></svg>
                </div>
                <input 
                  v-model="form.firstName" 
                  type="text" 
                  required
                  class="w-full pl-11 pr-3 py-3.5 rounded-2xl bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-700 text-slate-900 dark:text-white focus:ring-4 focus:ring-blue-500/10 focus:border-blue-500 outline-none transition-all duration-300 placeholder-slate-400 dark:placeholder-slate-600 text-sm"
                  placeholder="John"
                />
              </div>
            </div>

            <div class="space-y-2">
              <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-[0.2em] ml-1">
                {{ i18n.t.lastName }}
              </label>
              <div class="relative group/input">
                <div class="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none text-slate-400 group-focus-within/input:text-blue-500 transition-colors">
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" /></svg>
                </div>
                <input 
                  v-model="form.lastName" 
                  type="text" 
                  required
                  class="w-full pl-11 pr-3 py-3.5 rounded-2xl bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-700 text-slate-900 dark:text-white focus:ring-4 focus:ring-blue-500/10 focus:border-blue-500 outline-none transition-all duration-300 placeholder-slate-400 dark:placeholder-slate-600 text-sm"
                  placeholder="Doe"
                />
              </div>
            </div>
          </div>

          <div class="space-y-2">
            <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-[0.2em] ml-1">
              {{ i18n.t.email }}
            </label>
            <div class="relative group/input">
              <div class="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none text-slate-400 group-focus-within/input:text-blue-500 transition-colors">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 12a4 4 0 10-8 0 4 4 0 008 0zm0 0v1.5a2.5 2.5 0 005 0V12a9 9 0 10-9 9m4.5-1.206a8.959 8.959 0 01-4.5 1.206" /></svg>
              </div>
              <input 
                v-model="form.email" 
                type="email" 
                required
                class="w-full pl-12 pr-5 py-3.5 rounded-2xl bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-700 text-slate-900 dark:text-white focus:ring-4 focus:ring-blue-500/10 focus:border-blue-500 outline-none transition-all duration-300 placeholder-slate-400 dark:placeholder-slate-600"
                placeholder="name@example.com"
              />
            </div>
          </div>

          <div class="space-y-2">
            <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-[0.2em] ml-1">
              {{ i18n.t.password }}
            </label>
            <div class="relative group/input">
              <div class="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none text-slate-400 group-focus-within/input:text-blue-500 transition-colors">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" /></svg>
              </div>
              <input 
                v-model="form.password" 
                type="password" 
                required
                class="w-full pl-12 pr-5 py-3.5 rounded-2xl bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-700 text-slate-900 dark:text-white focus:ring-4 focus:ring-blue-500/10 focus:border-blue-500 outline-none transition-all duration-300 placeholder-slate-400 dark:placeholder-slate-600"
                placeholder="••••••••"
              />
            </div>
            <p class="text-[10px] text-slate-500 dark:text-slate-400 mt-1 pl-1">
              En az 8 karakter, 1 büyük harf, 1 küçük harf, 1 rakam ve 1 özel karakter içermelidir.
            </p>
          </div>

          <div class="space-y-2">
            <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 uppercase tracking-[0.2em] ml-1">
              Şifre Tekrarı
            </label>
            <div class="relative group/input">
              <div class="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none text-slate-400 group-focus-within/input:text-blue-500 transition-colors">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" /></svg>
              </div>
              <input 
                v-model="form.confirmPassword" 
                type="password" 
                required
                class="w-full pl-12 pr-5 py-3.5 rounded-2xl bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-700 text-slate-900 dark:text-white focus:ring-4 focus:ring-blue-500/10 focus:border-blue-500 outline-none transition-all duration-300 placeholder-slate-400 dark:placeholder-slate-600"
                placeholder="••••••••"
              />
            </div>
          </div>

          <div v-if="errorMessage" class="p-3 bg-red-50 dark:bg-red-900/20 text-red-600 dark:text-red-400 text-xs font-semibold rounded-xl border border-red-200 dark:border-red-800/50">
            {{ errorMessage }}
          </div>

          <button 
            type="submit" 
            :disabled="loading"
            class="w-full relative group/btn overflow-hidden py-4 bg-slate-900 dark:bg-white text-white dark:text-slate-900 rounded-2xl font-bold text-base shadow-xl active:scale-[0.98] transition-all duration-300 disabled:opacity-50 disabled:pointer-events-none flex items-center justify-center gap-3 mt-4"
          >
            <div class="absolute inset-0 bg-gradient-to-r from-blue-600 to-indigo-600 opacity-0 group-hover/btn:opacity-100 transition-opacity duration-500" />
            <span v-if="loading" class="relative z-10 w-5 h-5 border-2 border-current/30 border-t-current rounded-full animate-spin" />
            <span class="relative z-10 transition-colors duration-500 group-hover/btn:text-white">
              {{ loading ? i18n.t.sending : i18n.t.register }}
            </span>
          </button>
        </form>

        <div class="mt-8 text-center">
          <p class="text-sm text-slate-500 dark:text-slate-400">
            Already have an account? 
            <router-link to="/login" class="text-blue-600 dark:text-blue-400 font-bold hover:underline ml-1">
              {{ i18n.t.login }}
            </router-link>
          </p>
        </div>
      </div>
    </div>
  </div>
</template>
