<script setup>
import { ref, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { auth, toastStore } from '../auth'
import { theme } from '../lang'
import * as api from '../services/ticketApi'

const emit = defineEmits(['created', 'cancel'])
const step = ref(1)
const loading = ref(false)

const form = reactive({
  title: '',
  description: '',
  category: 2,
  institutionName: '',
  termsAccepted: false,
  file: null
})

const categories = [
  { id: 2, label: 'Destek Talebi', icon: '🛠️', color: 'border-blue-500 text-blue-600 bg-blue-50' },
  { id: 1, label: 'Öneri', icon: '💡', color: 'border-amber-500 text-amber-600 bg-amber-50' },
  { id: 0, label: 'Şikayet', icon: '⚠️', color: 'border-rose-500 text-rose-600 bg-rose-50' },
  { id: 3, label: 'Teşekkür', icon: '⭐', color: 'border-emerald-500 text-emerald-600 bg-emerald-50' },
  { id: 5, label: 'Bilgi Edinme', icon: 'ℹ️', color: 'border-sky-500 text-sky-600 bg-sky-50' }
]

const institutions = ["Rektörlük", "Öğrenci İşleri", "Sağlık Kültür", "Kütüphane", "Bilgi İşlem", "Personel", "Hukuk Müşavirliği"]

const handleFileChange = (e) => {
  const file = e.target.files[0]
  if (!file) return
  if (file.size > 1024 * 1024) return toastStore.add('Dosya boyutu 1MB\'dan büyük olamaz', 'error')
  form.file = file
}

const nextStep = () => {
  if (!form.title || !form.description) return toastStore.add('Lütfen tüm alanları doldurun', 'warning')
  step.value = 2
}

const termsText = `İşbu taahhütname; Rektörlük İletişim Merkezi’nin (“RİMER”) kullanımına ilişkin hak ve yükümlülükleri düzenlemektedir. 1. Bilgilerin doğruluğunu beyan ederim. 2. Sorumluluk şahsıma aittir. 3. RİMER sistem işleyişini kabul ederim. 4. Diğer kurum entegrasyonlarını bilmekteyim. 5. İstatistiksel veri kullanımına onay veririm. 6. Güvenlik kodunun gizliliğinden sorumluyum. 7. Kimlik bilgilerimi üçüncü kişilerle paylaşmam. 8. Hakaret, küfür ve tehdit içeren mesaj iletmem. 9. Kişilik haklarına tecavüzde bulunmam. 10. Sisteme zarar verecek yazılımlar kullanmam. 11. Diğer kullanıcıların erişimini kısıtlayacak veya yok edecek biçimde kullanmayacağımı kabul ederim.`

const isSpeakingTerms = ref(false)
const toggleSpeakTerms = () => {
  if (window.speechSynthesis.speaking) { window.speechSynthesis.cancel(); isSpeakingTerms.value = false; }
  else {
    const msg = new SpeechSynthesisUtterance(termsText); msg.lang = 'tr-TR';
    msg.onstart = () => isSpeakingTerms.value = true;
    msg.onend = () => isSpeakingTerms.value = false;
    window.speechSynthesis.speak(msg);
  }
}

const submit = async () => {
  if (!form.termsAccepted || !form.institutionName) return toastStore.add('Lütfen kurum seçin ve onayı işaretleyin', 'warning')
  loading.value = true
  try {
    const fd = new FormData();
    fd.append('title', form.title); fd.append('description', form.description);
    fd.append('category', form.category); fd.append('institutionName', form.institutionName);
    fd.append('termsAccepted', form.termsAccepted); if (form.file) fd.append('attachment', form.file);
    await api.createTicket(fd); 
    toastStore.add('Başvurunuz alındı', 'success'); 
    emit('created')
  } catch (e) { 
    const msg = e.response?.data?.message || 'Bir hata oluştu (Oturumunuz dolmuş olabilir, lütfen tekrar giriş yapın)';
    toastStore.add(msg, 'error'); 
  } finally { loading.value = false }
}
</script>

<template>
  <div class="w-full bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-[1.5rem] shadow-sm overflow-hidden flex flex-col mb-8">
    <div class="bg-slate-50 dark:bg-slate-800 border-b border-slate-200 dark:border-slate-700 px-6 py-4 flex items-center justify-between">
      <h2 class="text-lg font-black text-slate-900 dark:text-white uppercase">Yeni Talep Oluştur</h2>
      <div class="flex gap-1.5">
        <div :class="step >= 1 ? 'bg-blue-600' : 'bg-slate-300 dark:bg-slate-700'" class="w-6 h-1 rounded-full"></div>
        <div :class="step >= 2 ? 'bg-blue-600' : 'bg-slate-300 dark:bg-slate-700'" class="w-6 h-1 rounded-full"></div>
      </div>
    </div>
    
    <!-- STEP 1 -->
    <div v-if="step === 1" class="p-6 md:p-8 animate-in fade-in duration-200">

          <div class="space-y-6">
            <!-- Kategoriler -->
            <div>
              <label class="text-xs font-black text-slate-500 dark:text-slate-400 uppercase tracking-widest mb-3 block">KATEGORİ SEÇİNİZ</label>
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

            <!-- Konu -->
            <div>
              <div class="flex justify-between mb-1.5">
                <label class="text-xs font-black text-slate-500 dark:text-slate-400 uppercase">KONU BAŞLIĞI</label>
                <span class="text-xs font-bold text-slate-400">{{ form.title.length }}/250</span>
              </div>
              <input v-model="form.title" type="text" maxlength="250" placeholder="Kısa ve öz bir başlık..."
                class="w-full px-5 py-3 rounded-xl bg-slate-50 dark:bg-slate-800 border-2 border-transparent focus:border-blue-500 outline-none text-base font-bold text-slate-900 dark:text-white" />
            </div>

            <!-- Mesaj -->
            <div>
              <div class="flex justify-between mb-1.5">
                <label class="text-xs font-black text-slate-500 dark:text-slate-400 uppercase">MESAJINIZ</label>
                <span class="text-xs font-bold text-slate-400">{{ form.description.length }}/2000</span>
              </div>
              <textarea v-model="form.description" rows="5" maxlength="2000" placeholder="Detaylı açıklama yazınız..."
                class="w-full px-5 py-3 rounded-xl bg-slate-50 dark:bg-slate-800 border-2 border-transparent focus:border-blue-500 outline-none text-base font-medium text-slate-900 dark:text-white resize-none"></textarea>
            </div>

            <!-- Dosya -->
            <div class="p-3 bg-slate-50 dark:bg-slate-800 rounded-xl border border-slate-200 dark:border-slate-700 flex items-center justify-between">
              <label class="flex items-center gap-4 cursor-pointer flex-1">
                <span class="text-2xl">📎</span>
                <span class="text-xs font-black text-slate-600 dark:text-slate-300 uppercase tracking-tighter">{{ form.file ? form.file.name : 'EK DOSYA EKLE (MAX 1MB)' }}</span>
                <input type="file" @change="handleFileChange" class="hidden" accept=".docx,.jpeg,.jpg,.png" />
              </label>
              <button v-if="form.file" @click="form.file = null" class="text-xs font-black text-rose-500 px-4 hover:bg-rose-50 py-1 rounded-lg">SİL</button>
            </div>

            <div class="flex justify-between items-center pt-4 border-t border-slate-100 dark:border-slate-800">
              <button @click="emit('cancel')" class="text-xs font-black text-slate-400 hover:text-rose-500 transition-colors">
                İPTAL ET VE VAZGEÇ
              </button>
              <button @click="nextStep" class="px-10 py-3.5 bg-blue-600 hover:bg-blue-700 text-white rounded-xl font-black text-sm shadow-lg transition-all hover:-translate-y-0.5 active:scale-95">
                SONRAKİ ADIM →
              </button>
            </div>
          </div>
        </div>

        <!-- STEP 2 -->
        <div v-if="step === 2" class="p-6 md:p-8 animate-in fade-in duration-200">
          <div class="flex justify-between items-center mb-8">
            <h2 class="text-2xl font-black text-slate-900 dark:text-white uppercase">Son Onay ve Gönderim</h2>
            <button @click="step = 1" class="text-xs font-black text-slate-500 hover:text-blue-600 uppercase flex items-center gap-1 px-3 py-1.5 rounded-lg bg-slate-100 dark:bg-slate-800 transition-colors">
              <span class="text-base">←</span> FORMA DÖN
            </button>
          </div>
          
          <div class="grid grid-cols-1 md:grid-cols-2 gap-10">
            <div class="space-y-6">
              <div>
                <label class="text-xs font-black text-slate-400 uppercase block mb-3">HEDEF KURUM</label>
                <select v-model="form.institutionName" class="w-full px-5 py-3.5 rounded-xl bg-slate-50 dark:bg-slate-800 border-2 border-transparent focus:border-blue-500 outline-none text-sm font-black text-slate-900 dark:text-white">
                  <option value="" disabled>Lütfen seçiniz...</option>
                  <option v-for="inst in institutions" :key="inst">{{ inst }}</option>
                </select>
              </div>
              
              <div>
                <div class="flex justify-between items-center mb-3">
                  <label class="text-xs font-black text-slate-400 uppercase">TAAHHÜTNAME</label>
                  <button @click="toggleSpeakTerms" :class="isSpeakingTerms ? 'text-rose-600 border-rose-200 bg-rose-50' : 'text-blue-600 border-blue-200 bg-blue-50'" class="text-[10px] font-black px-2 py-1 rounded-md border">
                    {{ isSpeakingTerms ? '🛑 DURDUR' : '🔊 DİNLE' }}
                  </button>
                </div>
                <div class="h-36 overflow-y-auto p-4 bg-slate-50 dark:bg-slate-800 rounded-xl text-xs text-slate-500 leading-relaxed border border-slate-200 dark:border-slate-700 font-medium">
                  {{ termsText }}
                </div>
                <label class="flex items-center gap-3 mt-5 cursor-pointer group">
                  <input type="checkbox" v-model="form.termsAccepted" class="w-5 h-5 rounded border-2 border-slate-300 text-blue-600 transition-all" />
                  <span class="text-sm font-black text-slate-700 dark:text-slate-200 group-hover:text-blue-600">Okudum, onaylıyorum.</span>
                </label>
              </div>
            </div>

            <div class="bg-blue-50 dark:bg-slate-800/50 p-6 rounded-3xl border border-blue-100 dark:border-slate-800 flex flex-col justify-between shadow-inner">
              <div>
                <p class="text-[10px] font-black text-blue-400 uppercase tracking-widest mb-3">ÖZET ÖNİZLEME</p>
                <p class="text-lg font-black text-slate-900 dark:text-white line-clamp-2 mb-3 leading-tight">{{ form.title }}</p>
                <div class="h-px bg-blue-100 dark:bg-slate-700 mb-3" />
                <p class="text-sm text-slate-600 dark:text-slate-400 italic leading-relaxed line-clamp-5">"{{ form.description }}"</p>
              </div>
              <div class="flex flex-col gap-3 mt-8">
                <button @click="submit" :disabled="loading" class="w-full py-4 bg-blue-600 hover:bg-blue-700 text-white rounded-xl font-black text-xs shadow-xl shadow-blue-500/30">
                  {{ loading ? 'GÖNDERİLİYOR...' : 'BAŞVURUYU TAMAMLA ✓' }}
                </button>
              </div>
            </div>
        </div>
    </div>
  </div>
</template>

<style scoped>
.animate-in { animation: slide-in 0.3s cubic-bezier(0.16, 1, 0.3, 1); }
@keyframes slide-in { from { opacity: 0; transform: translateY(8px); } to { opacity: 1; transform: translateY(0); } }
</style>
