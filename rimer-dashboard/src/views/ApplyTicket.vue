<script setup>
import { ref, reactive, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { theme, i18n } from '../lang'
import { toastStore } from '../auth'
import * as api from '../services/ticketApi'
import LangSelector from '../components/LangSelector.vue'

const router = useRouter()
const step = ref(1)
const loading = ref(false)
const error = ref('')
const success = ref(false)
const trackingCode = ref('')
const attemptedStep1 = ref(false)

const form = reactive({
  title: '',
  description: '',
  category: 2,
  departmentId: '',
  termsAccepted: false,
  
  identityNumber: '',
  titleName: '',
  firstName: '',
  lastName: '',
  email: '',
  phone: '',
  address: '',
  hidePersonalInfo: false
})

const categories = computed(() => [
  { id: 2, label: i18n.t.cat2, icon: '🛠️', color: 'border-blue-500 text-blue-600 bg-blue-50' },
  { id: 1, label: i18n.t.cat1, icon: '💡', color: 'border-amber-500 text-amber-600 bg-amber-50' },
  { id: 0, label: i18n.t.cat0, icon: '⚠️', color: 'border-rose-500 text-rose-600 bg-rose-50' },
  { id: 3, label: i18n.t.cat3, icon: '⭐', color: 'border-emerald-500 text-emerald-600 bg-emerald-50' },
  { id: 4, label: i18n.t.cat4, icon: '❓', color: 'border-sky-500 text-sky-600 bg-sky-50' }
])

const departments = ref([])

onMounted(async () => {
  try {
    const res = await api.getPublicDepartments()
    if (res.data?.success) {
      departments.value = res.data.data
    }
  } catch (e) {
    console.error('Failed to load departments', e)
  }
})

const nextStep = () => {
  attemptedStep1.value = true
  if (!form.title || !form.description || !form.firstName || !form.lastName || !form.email || !form.address) {
    return toastStore.error(i18n.lang === 'tr' ? 'Lütfen zorunlu alanları doldurunuz.' : 'Please fill in the required fields.')
  }
  step.value = 2
}

const termsText = `İşbu taahhütname; Rektörlük İletişim Merkezi’nin (“RİMER”) kullanımına ilişkin hak ve yükümlülükleri düzenlemektedir. 1. Bilgilerin doğruluğunu beyan ederim. 2. Sorumluluk şahsıma aittir. 3. RİMER sistem işleyişini kabul ederim. 4. Diğer kurum entegrasyonlarını bilmekteyim. 5. İstatistiksel veri kullanımına onay veririm. 6. Güvenlik kodunun gizliliğinden sorumluyum. 7. Kimlik bilgilerimi üçüncü kişilerle paylaşmam. 8. Hakaret, küfür ve tehdit içeren mesaj iletmem. 9. Kişilik haklarına tecavüzde bulunmam. 10. Sisteme zarar verecek yazılımlar kullanmam. 11. Diğer kullanıcıların erişimini kısıtlayacak veya yok edecek biçimde kullanmayacağımı kabul ederim.`

const isSpeakingTerms = ref(false)
const toggleSpeakTerms = () => {
  if (window.speechSynthesis.speaking) { window.speechSynthesis.cancel(); isSpeakingTerms.value = false; }
  else {
    const msg = new SpeechSynthesisUtterance(termsText); msg.lang = i18n.lang === 'tr' ? 'tr-TR' : 'en-US';
    msg.onstart = () => isSpeakingTerms.value = true;
    msg.onend = () => isSpeakingTerms.value = false;
    window.speechSynthesis.speak(msg);
  }
}

const submit = async () => {
  if (!form.termsAccepted) return toastStore.error(i18n.lang === 'tr' ? 'Lütfen taahhütnameyi onaylayınız.' : 'Please agree to the terms and conditions.')
  if (!form.departmentId) return toastStore.error(i18n.t.selectDept)
  
  loading.value = true
  error.value = ''
  
  try {
    const res = await api.submitPublicTicket({
      identityNumber: form.identityNumber,
      titleName: form.titleName,
      firstName: form.firstName,
      lastName: form.lastName,
      email: form.email,
      phone: form.phone,
      address: form.address,
      hidePersonalInfo: form.hidePersonalInfo,
      category: form.category,
      title: form.title,
      message: form.description,
      departmentId: form.departmentId,
      termsAccepted: form.termsAccepted
    })
    
    if (res.data?.success) {
      success.value = true
      trackingCode.value = res.data.data.trackingCode
      toastStore.success(i18n.lang === 'tr' ? 'Başvurunuz başarıyla iletildi.' : 'Your application has been submitted successfully.')
    } else {
      error.value = res.data?.message || 'Error occurred.'
      toastStore.error(error.value)
    }
  } catch (e) {
    error.value = e.response?.data?.message || 'Connection error.'
    toastStore.error(error.value)
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="min-h-screen bg-slate-50 dark:bg-slate-950 flex flex-col">
    <!-- Slim Header -->
    <header class="h-20 bg-white dark:bg-slate-900 border-b border-slate-200 dark:border-slate-800 px-6 flex items-center justify-between sticky top-0 z-50">
      <div class="flex items-center gap-4">
        <div class="relative h-16 w-40 flex-shrink-0 cursor-pointer" @click="router.push('/login')">
          <img src="/logo.png" alt="Rimer Logo" class="absolute inset-0 w-full h-full object-contain transition-opacity duration-200" :class="theme.dark ? 'opacity-0' : 'opacity-100'" />
          <img src="/logonight.png" alt="Rimer Logo" class="absolute inset-0 w-full h-full object-contain transition-opacity duration-200" :class="theme.dark ? 'opacity-100' : 'opacity-0'" />
        </div>
      </div>
      
      <div class="flex items-center gap-3">
        <!-- Adım göstergesi -->
        <div v-if="!success" class="hidden md:flex gap-1.5 mr-4">
          <div :class="step >= 1 ? 'bg-blue-600' : 'bg-slate-200'" class="w-8 h-1 rounded-full"></div>
          <div :class="step >= 2 ? 'bg-blue-600' : 'bg-slate-200'" class="w-8 h-1 rounded-full"></div>
        </div>

        <!-- İptal / Vazgeç kutusu -->
        <div v-if="!success" class="flex items-center gap-1 bg-slate-50 dark:bg-slate-800 p-1.5 rounded-xl border border-slate-300 dark:border-slate-700">
          <button
            v-if="step === 1"
            @click="router.push('/login')"
            class="flex items-center gap-1.5 px-3 py-1.5 text-xs font-black text-rose-600 dark:text-rose-400 hover:bg-rose-50 dark:hover:bg-rose-900/30 rounded-lg transition-colors uppercase tracking-wide"
          >
            <span>✕</span> {{ i18n.t.cancelApply }}
          </button>
          <button
            v-if="step === 2"
            @click="step = 1"
            class="flex items-center gap-1.5 px-3 py-1.5 text-xs font-black text-slate-500 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-700 rounded-lg transition-colors uppercase tracking-wide"
          >
            <span>←</span> {{ i18n.t.prevStep }}
          </button>
          <button
            v-if="step === 2"
            @click="router.push('/login')"
            class="flex items-center gap-1.5 px-3 py-1.5 text-xs font-black text-rose-600 dark:text-rose-400 hover:bg-rose-50 dark:hover:bg-rose-900/30 rounded-lg transition-colors uppercase tracking-wide"
          >
            <span>✕</span> Vazgeç
          </button>
        </div>

        <!-- Dil & Tema -->
        <div class="flex items-center gap-2 bg-slate-50 dark:bg-slate-800 p-1.5 rounded-xl border border-slate-200 dark:border-slate-700">
          <LangSelector />
          <button @click="theme.toggle()" class="p-2 rounded-lg hover:bg-white dark:hover:bg-slate-700 transition-colors">
            {{ theme.dark ? '☀️' : '🌙' }}
          </button>
        </div>
      </div>
    </header>

    <main class="flex-1 p-3 md:p-6 flex justify-center items-start overflow-y-auto">
      <div class="w-full max-w-5xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-[1.5rem] shadow-xl overflow-hidden flex flex-col">
        
        <!-- SUCCESS -->
        <div v-if="success" class="p-10 text-center animate-in fade-in duration-500">
          <div class="w-24 h-24 bg-emerald-100 dark:bg-emerald-900/30 text-emerald-500 rounded-full flex items-center justify-center mx-auto mb-6">
            <span class="text-5xl">✓</span>
          </div>
          <h2 class="text-3xl font-black text-slate-900 dark:text-white mb-4">{{ i18n.t.applySuccess }}</h2>
          <p class="text-lg text-slate-600 dark:text-slate-400 mb-8">
            {{ i18n.t.trackingNo }}: <br/><strong class="text-4xl text-blue-600 dark:text-blue-400 tracking-widest mt-2 block">{{ trackingCode }}</strong>
          </p>
          <div class="bg-blue-50 dark:bg-blue-900/20 p-6 rounded-2xl inline-block text-left mb-8">
            <p class="text-sm font-medium text-slate-700 dark:text-slate-300">{{ i18n.t.trackDescription }}</p>
          </div>
          <br/>
          <div class="flex flex-col sm:flex-row gap-3 justify-center">
            <router-link to="/track" class="px-8 py-4 bg-blue-600 hover:bg-blue-700 text-white rounded-xl font-bold shadow-lg transition-transform hover:scale-105 inline-block">
              {{ i18n.t.trackNow }}
            </router-link>
            <router-link to="/login" class="px-8 py-4 bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-200 rounded-xl font-bold shadow transition-transform hover:scale-105 inline-block border border-slate-200 dark:border-slate-700">
              🏠 Anasayfaya Dön
            </router-link>
          </div>
        </div>

        <!-- STEP 1 -->
        <div v-else-if="step === 1" class="p-6 md:p-8 animate-in fade-in duration-200">
          <div class="flex justify-between items-center mb-6">
            <h2 class="text-2xl font-black text-slate-900 dark:text-white uppercase">{{ i18n.t.publicApply }}</h2>
            <span class="text-xs font-black text-slate-400">ADIM 1 / 2</span>
          </div>

          <div class="space-y-8">
            <!-- Kategoriler -->
            <div>
              <label class="text-xs font-black text-slate-500 dark:text-slate-400 uppercase tracking-widest mb-3 block">{{ i18n.t.category.toUpperCase() }}</label>
              <div class="grid grid-cols-2 md:grid-cols-5 gap-3">
                <button v-for="cat in categories" :key="cat.id" @click="form.category = cat.id"
                  :class="form.category === cat.id ? cat.color + ' border-2 shadow-sm' : 'bg-slate-50 dark:bg-slate-800 border-transparent text-slate-500'"
                  class="flex flex-col md:flex-row items-center justify-center gap-2 py-3 px-4 rounded-xl border-2 transition-all text-xs font-bold"
                >
                  <span class="text-xl">{{ cat.icon }}</span>
                  <span>{{ cat.label }}</span>
                </button>
              </div>
            </div>

            <!-- Kişisel Bilgiler -->
            <div>
              <label class="text-xs font-black text-slate-500 dark:text-slate-400 uppercase tracking-widest mb-3 block">{{ i18n.t.personalInfo.toUpperCase() }}</label>
              <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label class="block text-[10px] font-bold mb-1 transition-colors" :class="attemptedStep1 && !form.firstName ? 'text-rose-600 dark:text-rose-400' : 'text-slate-400'">{{ i18n.t.firstName }} ({{ i18n.t.required }})</label>
                  <input v-model="form.firstName" type="text" class="w-full px-4 py-3 rounded-xl bg-slate-50 dark:bg-slate-800 border-2 border-transparent focus:border-blue-500 outline-none text-sm font-bold text-slate-900 dark:text-white transition-colors" :class="{ '!border-rose-500 !bg-rose-50 dark:!bg-rose-900/20': attemptedStep1 && !form.firstName }" />
                </div>
                <div>
                  <label class="block text-[10px] font-bold mb-1 transition-colors" :class="attemptedStep1 && !form.lastName ? 'text-rose-600 dark:text-rose-400' : 'text-slate-400'">{{ i18n.t.lastName }} ({{ i18n.t.required }})</label>
                  <input v-model="form.lastName" type="text" class="w-full px-4 py-3 rounded-xl bg-slate-50 dark:bg-slate-800 border-2 border-transparent focus:border-blue-500 outline-none text-sm font-bold text-slate-900 dark:text-white transition-colors" :class="{ '!border-rose-500 !bg-rose-50 dark:!bg-rose-900/20': attemptedStep1 && !form.lastName }" />
                </div>
                <div>
                  <label class="block text-[10px] font-bold mb-1 transition-colors" :class="attemptedStep1 && !form.email ? 'text-rose-600 dark:text-rose-400' : 'text-slate-400'">E-POSTA ({{ i18n.t.required }})</label>
                  <input v-model="form.email" type="email" class="w-full px-4 py-3 rounded-xl bg-slate-50 dark:bg-slate-800 border-2 border-transparent focus:border-blue-500 outline-none text-sm font-bold text-slate-900 dark:text-white transition-colors" :class="{ '!border-rose-500 !bg-rose-50 dark:!bg-rose-900/20': attemptedStep1 && !form.email }" />
                </div>
                <div>
                  <label class="block text-[10px] font-bold text-slate-400 mb-1">TELEFON ({{ i18n.t.optional }})</label>
                  <input v-model="form.phone" type="tel" class="w-full px-4 py-3 rounded-xl bg-slate-50 dark:bg-slate-800 border-2 border-transparent focus:border-blue-500 outline-none text-sm font-bold text-slate-900 dark:text-white" />
                </div>
                <div>
                  <label class="block text-[10px] font-bold text-slate-400 mb-1">{{ i18n.t.idNumber }} ({{ i18n.t.optional }})</label>
                  <input v-model="form.identityNumber" type="text" maxlength="11" class="w-full px-4 py-3 rounded-xl bg-slate-50 dark:bg-slate-800 border-2 border-transparent focus:border-blue-500 outline-none text-sm font-bold text-slate-900 dark:text-white" />
                </div>
                <div>
                  <label class="block text-[10px] font-bold text-slate-400 mb-1">{{ i18n.t.jobTitle }} ({{ i18n.t.optional }})</label>
                  <input v-model="form.titleName" type="text" class="w-full px-4 py-3 rounded-xl bg-slate-50 dark:bg-slate-800 border-2 border-transparent focus:border-blue-500 outline-none text-sm font-bold text-slate-900 dark:text-white" />
                </div>
                <div class="md:col-span-2">
                  <label class="block text-[10px] font-bold mb-1 transition-colors" :class="attemptedStep1 && !form.address ? 'text-rose-600 dark:text-rose-400' : 'text-slate-400'">{{ i18n.t.address }} ({{ i18n.t.required }})</label>
                  <textarea v-model="form.address" rows="2" class="w-full px-4 py-3 rounded-xl bg-slate-50 dark:bg-slate-800 border-2 border-transparent focus:border-blue-500 outline-none text-sm font-bold text-slate-900 dark:text-white resize-none transition-colors" :class="{ '!border-rose-500 !bg-rose-50 dark:!bg-rose-900/20': attemptedStep1 && !form.address }"></textarea>
                </div>
                <div class="md:col-span-2 flex items-center gap-3 bg-rose-50 dark:bg-rose-900/20 p-4 rounded-xl border border-rose-100 dark:border-rose-900/50 mt-2">
                  <input type="checkbox" v-model="form.hidePersonalInfo" id="hideInfo" class="w-5 h-5 rounded border-2 border-rose-300 text-rose-600 focus:ring-rose-500" />
                  <label for="hideInfo" class="text-sm font-bold text-rose-700 dark:text-rose-400 cursor-pointer">{{ i18n.t.hideInfo }} ({{ i18n.t.hideInfoSub }})</label>
                </div>
              </div>
            </div>

            <!-- Konu -->
            <div>
              <div class="flex justify-between mb-1.5">
                <label class="text-xs font-black uppercase tracking-widest transition-colors" :class="attemptedStep1 && !form.title ? 'text-rose-600 dark:text-rose-400' : 'text-slate-500 dark:text-slate-400'">{{ i18n.t.titleLabel.toUpperCase() }}</label>
                <span class="text-xs font-bold transition-colors" :class="attemptedStep1 && !form.title ? 'text-rose-500' : 'text-slate-400'">{{ form.title.length }}/250</span>
              </div>
              <input v-model="form.title" type="text" maxlength="250" :placeholder="i18n.t.titlePlaceholder"
                class="w-full px-5 py-3 rounded-xl bg-slate-50 dark:bg-slate-800 border-2 border-transparent focus:border-blue-500 outline-none text-base font-bold text-slate-900 dark:text-white transition-colors" :class="{ '!border-rose-500 !bg-rose-50 dark:!bg-rose-900/20': attemptedStep1 && !form.title }" />
            </div>

            <!-- Mesaj -->
            <div>
              <div class="flex justify-between mb-1.5">
                <label class="text-xs font-black uppercase tracking-widest transition-colors" :class="attemptedStep1 && !form.description ? 'text-rose-600 dark:text-rose-400' : 'text-slate-500 dark:text-slate-400'">{{ i18n.t.msgLabel.toUpperCase() }}</label>
                <span class="text-xs font-bold transition-colors" :class="attemptedStep1 && !form.description ? 'text-rose-500' : 'text-slate-400'">{{ form.description.length }}/2000</span>
              </div>
              <textarea v-model="form.description" rows="5" maxlength="2000" :placeholder="i18n.t.msgPlaceholder"
                class="w-full px-5 py-3 rounded-xl bg-slate-50 dark:bg-slate-800 border-2 border-transparent focus:border-blue-500 outline-none text-base font-medium text-slate-900 dark:text-white resize-none transition-colors" :class="{ '!border-rose-500 !bg-rose-50 dark:!bg-rose-900/20': attemptedStep1 && !form.description }"></textarea>
            </div>

            <div class="flex justify-end items-center pt-4 border-t border-slate-100 dark:border-slate-800">
              <button @click="nextStep" class="px-10 py-3.5 bg-blue-600 hover:bg-blue-700 text-white rounded-xl font-black text-sm shadow-lg transition-all hover:-translate-y-0.5 active:scale-95">
                {{ i18n.t.nextStep }} →
              </button>
            </div>
          </div>
        </div>

        <!-- STEP 2 -->
        <div v-else-if="step === 2" class="p-6 md:p-8 animate-in fade-in duration-200">
          <div class="flex justify-between items-center mb-8">
            <h2 class="text-2xl font-black text-slate-900 dark:text-white uppercase">{{ i18n.t.publicTrack }}</h2>
          </div>
          
          <div class="grid grid-cols-1 md:grid-cols-2 gap-10">
            <div class="space-y-6">
              <div>
                <label class="text-xs font-black text-slate-400 uppercase block mb-3">{{ i18n.t.relevantDept }}</label>
                <select v-model="form.departmentId" class="w-full px-5 py-3.5 rounded-xl bg-slate-50 dark:bg-slate-800 border-2 border-transparent focus:border-blue-500 outline-none text-sm font-black text-slate-900 dark:text-white">
                  <option value="" disabled>{{ i18n.t.selectDept }}</option>
                  <option v-for="dept in departments" :key="dept.id" :value="dept.id">{{ dept.name }}</option>
                </select>
              </div>
              
              <div>
                <div class="flex justify-between items-center mb-3">
                  <label class="text-xs font-black text-slate-400 uppercase">{{ i18n.t.termsTitle }}</label>
                  <button @click="toggleSpeakTerms" :class="isSpeakingTerms ? 'text-rose-600 border-rose-200 bg-rose-50' : 'text-blue-600 border-blue-200 bg-blue-50'" class="text-[10px] font-black px-2 py-1 rounded-md border">
                    {{ isSpeakingTerms ? '🛑 STOP' : '🔊 LISTEN' }}
                  </button>
                </div>
                <div class="h-36 overflow-y-auto p-4 bg-slate-50 dark:bg-slate-800 rounded-xl text-xs text-slate-500 leading-relaxed border border-slate-200 dark:border-slate-700 font-medium">
                  {{ termsText }}
                </div>
                <label class="flex items-center gap-3 mt-5 cursor-pointer group">
                  <input type="checkbox" v-model="form.termsAccepted" class="w-5 h-5 rounded border-2 border-slate-300 text-blue-600 transition-all" />
                  <span class="text-sm font-black text-slate-700 dark:text-slate-200 group-hover:text-blue-600">{{ i18n.t.termsAccept }}</span>
                </label>
              </div>
            </div>

            <div class="bg-blue-50 dark:bg-slate-800/50 p-6 rounded-3xl border border-blue-100 dark:border-slate-800 flex flex-col justify-between shadow-inner">
              <div>
                <p class="text-[10px] font-black text-blue-400 uppercase tracking-widest mb-3">{{ i18n.t.summary }}</p>
                <p class="text-lg font-black text-slate-900 dark:text-white line-clamp-2 mb-3 leading-tight">{{ form.title }}</p>
                <div class="h-px bg-blue-100 dark:bg-slate-700 mb-3" />
                <p class="text-sm text-slate-600 dark:text-slate-400 italic leading-relaxed line-clamp-5">"{{ form.description }}"</p>
                
                <div v-if="error" class="mt-4 p-3 bg-red-100 text-red-700 rounded-lg text-xs font-bold border border-red-200">
                  ⚠️ {{ error }}
                </div>
              </div>
              <div class="flex flex-col gap-3 mt-8">
                <button @click="submit" :disabled="loading" class="w-full py-4 bg-blue-600 hover:bg-blue-700 text-white rounded-xl font-black text-xs shadow-xl shadow-blue-500/30">
                  {{ loading ? i18n.t.sending.toUpperCase() : i18n.t.finishApply }} ✓
                </button>
              </div>
            </div>
          </div>
        </div>

      </div>
    </main>
  </div>
</template>

<style scoped>
.animate-in { animation: slide-in 0.3s cubic-bezier(0.16, 1, 0.3, 1); }
@keyframes slide-in { from { opacity: 0; transform: translateY(8px); } to { opacity: 1; transform: translateY(0); } }
</style>
