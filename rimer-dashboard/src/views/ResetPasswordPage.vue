<script setup>
import { ref, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { resetPassword } from '../services/ticketApi'
import { toastStore } from '../auth'

const router = useRouter()
const route = useRoute()

const email = ref('')
const token = ref('')
const newPassword = ref('')
const confirmPassword = ref('')
const loading = ref(false)
const success = ref(false)
const errorMsg = ref('')
const showConfirm = ref(false)

onMounted(() => {
  if (route.query.email && route.query.token) {
    email.value = route.query.email
    token.value = route.query.token
  } else {
    errorMsg.value = 'Geçersiz veya eksik şifre sıfırlama bağlantısı.'
  }
})

const submit = async () => {
  if (!newPassword.value || newPassword.value.length < 6) {
    toastStore.add('Şifre en az 6 karakter olmalıdır.', 'error')
    return
  }
  if (newPassword.value !== confirmPassword.value) {
    toastStore.add('Şifreler eşleşmiyor.', 'error')
    return
  }

  showConfirm.value = true
}

const confirmSubmit = async () => {
  showConfirm.value = false
  loading.value = true
  try {
    await resetPassword(email.value, token.value, newPassword.value)
    success.value = true
    toastStore.add('Şifreniz başarıyla sıfırlandı.', 'success')
  } catch (error) {
    let msg = error.response?.data?.message || 'Şifre sıfırlanırken bir hata oluştu.'
    if (msg.includes('Passwords must') || msg.includes('PasswordRequires')) {
      msg = 'Şifreniz güvenlik gereksinimlerini karşılamıyor (En az 1 büyük harf, 1 rakam ve özel karakter içermelidir).'
    }
    toastStore.add(msg, 'error')
  } finally {
    loading.value = false
  }
}

const cancelSubmit = () => {
  showConfirm.value = false
}
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-slate-50 dark:bg-slate-950 p-4">
    <div class="w-full max-w-md bg-white dark:bg-slate-900 rounded-3xl border border-slate-200 dark:border-slate-800 p-8 shadow-xl">
      <div class="text-center mb-8">
        <img src="/logo.png" alt="Rimer" class="h-12 mx-auto mb-6 object-contain block dark:hidden" />
        <img src="/logonight.png" alt="Rimer" class="h-12 mx-auto mb-6 object-contain hidden dark:block" />
        <h2 class="text-2xl font-black text-slate-900 dark:text-white">Yeni Şifre Belirle</h2>
        <p class="text-sm text-slate-500 dark:text-slate-400 mt-2">{{ email }} hesabı için yeni şifrenizi oluşturun.</p>
      </div>

      <div v-if="errorMsg" class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-2xl p-6 text-center">
        <div class="text-4xl mb-3">❌</div>
        <h3 class="text-red-700 dark:text-red-400 font-bold mb-2">Hata</h3>
        <p class="text-sm text-red-600 dark:text-red-300">{{ errorMsg }}</p>
        <button @click="router.push('/login')" class="mt-6 w-full py-3 rounded-xl bg-slate-200 hover:bg-slate-300 text-slate-800 font-bold text-sm transition-all">Giriş Ekranına Dön</button>
      </div>

      <div v-else-if="success" class="bg-emerald-50 dark:bg-emerald-900/20 border border-emerald-200 dark:border-emerald-800 rounded-2xl p-6 text-center">
        <div class="text-4xl mb-3">✅</div>
        <h3 class="text-emerald-700 dark:text-emerald-400 font-bold mb-2">Şifre Sıfırlandı!</h3>
        <p class="text-sm text-emerald-600 dark:text-emerald-300">Şifreniz başarıyla güncellendi. Artık yeni şifrenizle giriş yapabilirsiniz.</p>
        <button @click="router.push('/login')" class="mt-6 w-full py-3 rounded-xl bg-emerald-600 hover:bg-emerald-700 text-white font-bold text-sm transition-all">Giriş Yap</button>
      </div>

      <form v-else @submit.prevent="submit" class="space-y-5">
        <div>
          <label class="block text-[10px] font-black text-slate-500 uppercase tracking-widest mb-1.5">Yeni Şifre</label>
          <input v-model="newPassword" type="password" placeholder="••••••••" class="w-full px-4 py-3 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-900 dark:text-white outline-none focus:ring-2 focus:ring-blue-500 transition-all" required minlength="6" />
        </div>
        <div>
          <label class="block text-[10px] font-black text-slate-500 uppercase tracking-widest mb-1.5">Yeni Şifre (Tekrar)</label>
          <input v-model="confirmPassword" type="password" placeholder="••••••••" class="w-full px-4 py-3 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-slate-900 dark:text-white outline-none focus:ring-2 focus:ring-blue-500 transition-all" required minlength="6" />
        </div>

        <button type="button" @click="submit" :disabled="loading" class="w-full py-3 rounded-xl bg-blue-600 hover:bg-blue-700 text-white font-black text-sm transition-all shadow-lg shadow-blue-500/25 active:scale-[0.98] disabled:opacity-50 flex items-center justify-center gap-2">
          {{ loading ? 'Bekleyiniz...' : 'Şifreyi Güncelle' }}
        </button>
      </form>
    </div>

    <!-- Confirmation Modal -->
    <div v-if="showConfirm" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 backdrop-blur-sm p-4">
      <div class="bg-white dark:bg-slate-900 rounded-3xl border border-slate-200 dark:border-slate-800 p-8 w-full max-w-md shadow-2xl animate-in fade-in zoom-in-95 duration-200">
        <h3 class="text-xl font-black text-slate-900 dark:text-white mb-2">İşlemi Onaylayın</h3>
        <p class="text-sm text-slate-600 dark:text-slate-400 mb-8 leading-relaxed">
          Yeni şifreniz <strong class="text-slate-900 dark:text-white">{{ email }}</strong> hesabı için güncellenecektir. Bu işlemi onaylıyor musunuz?
        </p>
        <div class="flex gap-4">
          <button @click="cancelSubmit" type="button" class="flex-1 py-3 px-4 rounded-xl border-2 border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-300 font-bold hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors">
            İptal
          </button>
          <button @click="confirmSubmit" type="button" class="flex-1 py-3 px-4 rounded-xl bg-blue-600 hover:bg-blue-700 text-white font-black shadow-lg shadow-blue-500/30 flex items-center justify-center gap-2 transition-all active:scale-95">
            Evet, Onaylıyorum
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
