<script setup>
import { ref, computed, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { store } from '../store'

const router = useRouter()
const isDark = ref(true)

const logout = () => { store.logout(); router.push('/login') }

// ── Request form ──────────────────────────────────────────────────────────────
const form = reactive({ type: 'request', message: '' })
const submitting = ref(false)
const successMsg = ref('')

const submit = async () => {
  if (!form.message.trim()) return
  submitting.value = true
  await new Promise(r => setTimeout(r, 400))
  store.submitRequest(form.type, form.message)
  form.message = ''
  successMsg.value = 'Submitted successfully!'
  setTimeout(() => { successMsg.value = '' }, 3000)
  submitting.value = false
}

// ── User's submissions ────────────────────────────────────────────────────────
const myRequests = computed(() => store.userRequests())

const typeColor = (t) => ({
  request:    { bg: 'bg-blue-500/15',   text: 'text-blue-400' },
  complaint:  { bg: 'bg-rose-500/15',   text: 'text-rose-400' },
  suggestion: { bg: 'bg-amber-500/15',  text: 'text-amber-400' },
}[t] ?? { bg: 'bg-slate-800', text: 'text-slate-400' })
</script>

<template>
  <div class="min-h-screen bg-[#0a0f18] flex flex-col">

    <!-- Topbar -->
    <header class="h-14 flex-shrink-0 flex items-center justify-between px-6 bg-[#0d131f] border-b border-slate-800">
      <div class="flex items-center gap-3">
        <h1 class="text-sm font-bold bg-gradient-to-r from-blue-400 to-indigo-400 bg-clip-text text-transparent">Rimer</h1>
        <span class="text-[10px] font-bold uppercase tracking-widest text-slate-500 bg-slate-800 px-2 py-0.5 rounded-full">User</span>
      </div>
      <div class="flex items-center gap-3">
        <span class="text-xs text-slate-400">{{ store.displayName }}</span>
        <button
          @click="logout"
          class="text-xs font-semibold text-slate-500 hover:text-rose-400 transition-colors px-3 py-1.5 rounded-lg hover:bg-rose-500/5 border border-transparent hover:border-rose-500/20"
        >Sign out</button>
      </div>
    </header>

    <!-- Content -->
    <div class="flex-1 overflow-y-auto">
      <div class="max-w-2xl mx-auto px-4 py-8 space-y-6">

        <!-- Welcome -->
        <div>
          <h2 class="text-xl font-bold text-white">Hi, {{ store.displayName }} 👋</h2>
          <p class="text-sm text-slate-500 mt-0.5">Submit a request, complaint, or suggestion below.</p>
        </div>

        <!-- Submit form -->
        <div class="bg-[#0d131f] border border-slate-800 rounded-2xl p-6 relative overflow-hidden">
          <div class="absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-blue-500/20 to-transparent" />
          <h3 class="text-xs font-bold uppercase tracking-widest text-slate-400 mb-4">New Submission</h3>

          <form @submit.prevent="submit" class="space-y-4">
            <!-- Type selector -->
            <div class="flex gap-2">
              <button
                v-for="t in ['request', 'complaint', 'suggestion']" :key="t"
                type="button"
                @click="form.type = t"
                class="flex-1 py-2 rounded-xl text-xs font-bold capitalize transition-all duration-200 border"
                :class="form.type === t
                  ? t === 'request'    ? 'bg-blue-600/20 text-blue-400 border-blue-500/30' :
                    t === 'complaint'  ? 'bg-rose-600/20 text-rose-400 border-rose-500/30' :
                                        'bg-amber-600/20 text-amber-400 border-amber-500/30'
                  : 'border-slate-800 text-slate-500 hover:border-slate-700 hover:text-slate-400'"
              >{{ t }}</button>
            </div>

            <!-- Message textarea -->
            <textarea
              v-model="form.message"
              rows="4"
              :placeholder="form.type === 'request' ? 'Describe your request…' : form.type === 'complaint' ? 'Describe the issue…' : 'Share your idea…'"
              class="w-full bg-slate-900/60 border border-slate-700 rounded-xl px-4 py-3 text-sm text-slate-100 placeholder-slate-600 focus:outline-none focus:border-blue-500 transition-colors resize-none"
            />

            <!-- Success message -->
            <transition name="fade">
              <p v-if="successMsg" class="text-xs text-emerald-400 bg-emerald-500/10 border border-emerald-500/20 rounded-xl px-4 py-2.5">
                ✓ {{ successMsg }}
              </p>
            </transition>

            <!-- Submit button -->
            <button
              type="submit"
              :disabled="!form.message.trim() || submitting"
              class="w-full py-3 rounded-xl text-sm font-bold transition-all duration-200 bg-blue-600 hover:bg-blue-500 text-white disabled:opacity-40 disabled:cursor-not-allowed"
            >
              <span v-if="submitting" class="flex items-center justify-center gap-2">
                <span class="w-4 h-4 border-2 border-white/40 border-t-white rounded-full animate-spin" />
                Submitting…
              </span>
              <span v-else>Submit {{ form.type }}</span>
            </button>
          </form>
        </div>

        <!-- My submissions -->
        <div class="bg-[#0d131f] border border-slate-800 rounded-2xl p-6 relative overflow-hidden">
          <div class="absolute inset-x-0 top-0 h-px bg-gradient-to-r from-transparent via-indigo-500/20 to-transparent" />
          <div class="flex items-center justify-between mb-4">
            <h3 class="text-xs font-bold uppercase tracking-widest text-slate-400">My Submissions</h3>
            <span class="text-xs text-slate-500">{{ myRequests.length }} total</span>
          </div>

          <div v-if="myRequests.length === 0" class="flex flex-col items-center justify-center py-10 text-center">
            <span class="text-3xl mb-2">📭</span>
            <p class="text-sm text-slate-500">No submissions yet</p>
          </div>

          <div v-else class="space-y-3">
            <transition-group name="list">
              <div
                v-for="req in myRequests" :key="req.id"
                class="p-4 rounded-xl bg-slate-900/50 border border-slate-800/50 hover:border-slate-700 transition-colors"
              >
                <div class="flex items-start justify-between gap-3">
                  <div class="flex-1 min-w-0">
                    <div class="flex items-center gap-2 mb-1.5">
                      <span
                        class="text-[10px] font-bold uppercase tracking-widest px-2 py-0.5 rounded-full"
                        :class="[typeColor(req.type).bg, typeColor(req.type).text]"
                      >{{ req.type }}</span>
                    </div>
                    <p class="text-sm text-slate-300 leading-snug">{{ req.message }}</p>
                  </div>
                  <div class="flex-shrink-0 text-right">
                    <p class="text-[10px] font-bold uppercase"
                      :class="req.status === 'pending' ? 'text-amber-400' : 'text-emerald-400'"
                    >{{ req.status }}</p>
                    <p class="text-[10px] text-slate-600 mt-0.5">{{ new Date(req.createdAt).toLocaleDateString('tr-TR') }}</p>
                  </div>
                </div>
              </div>
            </transition-group>
          </div>
        </div>

      </div>
    </div>
  </div>
</template>

<style scoped>
.fade-enter-active, .fade-leave-active { transition: opacity 0.2s ease; }
.fade-enter-from, .fade-leave-to { opacity: 0; }

.list-enter-active { animation: slideIn 0.3s ease-out; }
@keyframes slideIn { from { opacity: 0; transform: translateY(-6px); } to { opacity: 1; transform: translateY(0); } }
</style>
