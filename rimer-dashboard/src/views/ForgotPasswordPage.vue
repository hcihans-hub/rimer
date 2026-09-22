<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { forgotPassword } from '../services/ticketApi'

const router = useRouter()
const email = ref('')
const loading = ref(false)
const success = ref(false)
const errorMessage = ref('')

const submit = async () => {
  if (!email.value) {
    errorMessage.value = 'Lütfen e-posta adresinizi girin.'
    return
  }

  loading.value = true
  errorMessage.value = ''
  try {
    await forgotPassword(email.value)
    success.value = true
  } catch (error) {
    errorMessage.value = error.response?.data?.message || 'Bir hata oluştu.'
  } finally {
    loading.value = false
  }
}

const retry = () => {
  errorMessage.value = ''
  success.value = false
}
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-slate-50 dark:bg-slate-950 p-4">
    <div class="w-full max-w-md bg-white dark:bg-slate-900 rounded-3xl border border-slate-200 dark:border-slate-800 p-8 shadow-xl">
      <div class="text-center mb-8">
        <img src="/logo.png" alt="Rimer" class="h-12 mx-auto mb-6 object-contain block dark:hidden" />
        <img src="/logonight.png" alt="Rimer" class="h-12 mx-auto mb-6 object-contain hidden dark:block" />
        <h2 class="text-2xl font-black text-slate-900 dark:text-white">Şifremi Unuttum</h2>
        <p class="text-sm text-slate-500 dark:text-slate-400 mt-2">Kayıtlı e-posta adresinizi girin, size bir şifre sıfırlama bağlantısı gönderelim.</p>
      </div>

      <!-- Başarı bloğu -->
      <div v-if="success" class="bg-emerald-50 dark:bg-emerald-900/20 border border-emerald-200 dark:border-emerald-800 rounded-2xl p-6 text-center">
        <div class="text-4xl mb-3">✉️</div>
        <h3 class="text-emerald-700 dark:text-emerald-400 font-bold mb-2">E-posta Gönderildi!</h3>
        <p class="text-sm text-emerald-600 dark:text-emerald-300">Şifre sıfırlama bağlantısı e-posta adresinize gönderildi. Lütfen gelen kutunuzu kontrol edin.</p>
        <button @click="router.push('/login')" class="mt-6 w-full py-3 rounded-xl bg-emerald-600 hover:bg-emerald-700 text-white font-bold text-sm transition-all">Giriş Ekranına Dön</button>
      </div>

      <!-- Hata bloğu -->
      <div v-else-if="errorMessage" class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-2xl p-6 text-center">
        <div class="w-14 h-14 rounded-full bg-red-100 dark:bg-red-900/40 flex items-center justify-center mx-auto mb-4">
          <svg class="w-7 h-7 text-red-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
          </svg>
        </div>
        <h3 class="text-red-700 dark:text-red-400 font-bold mb-2">İşlem Başarısız</h3>
        <p class="text-sm text-red-600 dark:text-red-300 leading-relaxed">{{ errorMessage }}</p>
        <button @click="retry" class="mt-6 w-full py-3 rounded-xl bg-red-600 hover:bg-red-700 text-white font-bold text-sm transition-all">Tekrar Dene</button>
        <button @click="router.push('/login')" class="mt-3 w-full py-3 rounded-xl border border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-400 font-bold text-sm hover:bg-slate-50 dark:hover:bg-slate-800 transition-all">Giriş Ekranına Dön</button>
      </div>

      <!-- Form -->
      <form v-else @submit.prevent="submit" class="space-y-5">
        <div>
          <label class="block text-[10px] font-black text-slate-500 uppercase tracking-widest mb-1.5">E-posta Adresi</label>
          <input v-model="email" type="email" placeholder="ornek@posta.com"
            class="w-full px-4 py-3 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-900 dark:text-white outline-none focus:ring-2 focus:ring-blue-500 transition-all" required />
        </div>

        <button type="submit" :disabled="loading" class="w-full py-3 rounded-xl bg-blue-600 hover:bg-blue-700 text-white font-black text-sm transition-all shadow-lg shadow-blue-500/25 active:scale-[0.98] disabled:opacity-50 flex items-center justify-center gap-2">
          <svg v-if="loading" class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path></svg>
          {{ loading ? 'Gönderiliyor...' : 'Bağlantı Gönder' }}
        </button>

        <div class="text-center mt-6">
          <button type="button" @click="router.push('/login')" class="text-sm font-bold text-slate-500 hover:text-blue-600 transition-colors">Geri Dön</button>
        </div>
      </form>
    </div>
  </div>
</template>
