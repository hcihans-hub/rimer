<script setup>
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { auth } from '../auth'

const props = defineProps({
  activeTab: { type: String, default: '' }
})

const emit = defineEmits(['update:activeTab'])
const router = useRouter()

const allTabs = [
  {
    v: 'tickets',
    l: 'Talepler',
    path: '/admin',
    operatorAllowed: true,
    color: 'indigo',
    icon: `<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
      d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2
         M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"/>`
  },
  {
    v: 'users',
    l: 'Kullanıcılar',
    path: '/admin',
    operatorAllowed: false,
    color: 'violet',
    icon: `<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
      d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857
         M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857
         m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z"/>`
  },
  {
    v: 'departments',
    l: 'Birimler',
    path: '/admin',
    operatorAllowed: false,
    color: 'sky',
    icon: `<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
      d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5
         M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4"/>`
  },
  {
    v: 'permissions',
    l: 'Chart Yetkileri',
    path: '/admin',
    operatorAllowed: false,
    color: 'purple',
    icon: `<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
      d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0
         V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0
         012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"/>`
  },
  {
    v: 'announcements',
    l: 'Duyurular',
    path: '/admin',
    operatorAllowed: false,
    color: 'amber',
    icon: `<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
      d="M11 5.882V19.24a1.76 1.76 0 01-3.417.592l-2.147-6.15M18 13a3 3 0 100-6M5.436
         13.683A4.001 4.001 0 017 6h1.832c4.1 0 7.625-1.234 9.168-3v14c-1.543-1.766-5.067-3-9.168-3H7
         a3.988 3.988 0 01-1.564-.317z"/>`
  },
  {
    v: 'analytics',
    l: 'Analytics',
    path: '/admin',
    operatorAllowed: false,
    color: 'emerald',
    icon: `<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
      d="M13 7h8m0 0v8m0-8l-8 8-4-4-6 6"/>`
  },
  {
    v: 'logs',
    l: 'Sistem Logları',
    path: '/admin',
    operatorAllowed: false,
    color: 'rose',
    icon: `<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
      d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293
         l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>`
  }
]

const tabs = computed(() =>
  auth.isOperator ? allTabs.filter(t => t.operatorAllowed) : allTabs
)

const handleTabClick = (t) => {
  if (t.path === router.currentRoute.value.path) {
    emit('update:activeTab', t.v)
  } else {
    if (t.path === '/admin') {
      localStorage.setItem('admin_last_tab', t.v)
      router.push('/admin')
    } else {
      router.push(t.path)
    }
  }
}
</script>

<template>
  <aside v-if="auth.isAdmin || auth.isOperator" class="admin-sidebar">

    <!-- Brand header -->
    <div class="sidebar-brand">
      <div class="sidebar-brand-icon">
        <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round">
          <!-- Outer hexagon -->
          <path stroke-width="1.5" d="M12 2l8.66 5v10L12 22l-8.66-5V7z"/>
          <!-- Inner hexagon -->
          <path stroke-width="1.3" d="M12 6.5l5.2 3v6L12 18.5 6.8 15.5v-6z" opacity="0.7"/>
          <!-- Center dot filled -->
          <circle cx="12" cy="12" r="2" fill="currentColor" stroke="none"/>
          <!-- Cardinal tick marks -->
          <path stroke-width="1.5" d="M12 9v1.2M12 13.8V15M9 12h1.2M13.8 12H15" opacity="0.8"/>
        </svg>
      </div>
      <div>
        <p class="sidebar-brand-title">Kontrol Merkezi</p>
        <p class="sidebar-brand-sub">{{ auth.isOperator ? 'Operatör' : 'Admin' }} · Rimer</p>
      </div>
    </div>

    <!-- Divider -->
    <div class="sidebar-divider" />

    <nav class="sidebar-nav">
      <button
        v-for="t in tabs"
        :key="t.v"
        @click="handleTabClick(t)"
        :data-color="t.color"
        :class="['nav-item', activeTab === t.v ? 'nav-item--active' : 'nav-item--idle', `nav-item--${t.color}`]"
      >
        <!-- Icon -->
        <span class="nav-icon">
          <svg width="16" height="16" fill="none" stroke="currentColor" viewBox="0 0 24 24" v-html="t.icon" />
        </span>

        <!-- Label -->
        <span class="nav-label">{{ t.l }}</span>

        <!-- Active indicator -->
        <span v-if="activeTab === t.v" class="nav-dot" />
      </button>
    </nav>

    <!-- Bottom: Operator badge -->
    <div v-if="auth.isOperator" class="sidebar-operator-badge">
      <span class="badge-dot" />
      Operatör Modu
    </div>
  </aside>
</template>

<style>
/* ══════════════════════════════════════════════════════════════════
   ADMIN SIDEBAR — LIGHT + DARK MODE
   Scoped değil, html.dark selector ile tema değişikliğine duyarlı
══════════════════════════════════════════════════════════════════ */

/* ── Sidebar shell — LIGHT ─────────────────────────────────────── */
.admin-sidebar {
  width: 220px;
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  gap: 4px;
  padding: 16px 10px;
  background: #f8fafc;
  border-right: 1px solid #e2e8f0;
  overflow-y: auto;
  position: relative;
  z-index: 10;
  transition: background 0.3s ease, border-color 0.3s ease;
}

/* ── Brand header — LIGHT ──────────────────────────────────────── */
.sidebar-brand {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 4px 6px 12px;
}

.sidebar-brand-icon {
  width: 32px;
  height: 32px;
  border-radius: 9px;
  background: linear-gradient(135deg, #6366f1, #8b5cf6);
  display: flex;
  align-items: center;
  justify-content: center;
  color: #fff;
  flex-shrink: 0;
  box-shadow: 0 4px 12px rgba(99,102,241,0.3);
}

.sidebar-brand-title {
  font-size: 12px;
  font-weight: 800;
  letter-spacing: -0.01em;
  color: #1e293b;
  line-height: 1.2;
}

.sidebar-brand-sub {
  font-size: 9px;
  font-weight: 600;
  letter-spacing: 0.03em;
  color: #94a3b8;
  text-transform: uppercase;
  margin-top: 1px;
}

html.dark .sidebar-brand-title {
  color: #f1f5f9;
}

html.dark .sidebar-brand-sub {
  color: rgba(148,163,184,0.55);
}

/* ── Divider ───────────────────────────────────────────────────── */
.sidebar-divider {
  height: 1px;
  background: #e2e8f0;
  margin: 0 4px 10px;
  border-radius: 1px;
}

html.dark .sidebar-divider {
  background: rgba(255,255,255,0.06);
}


.admin-sidebar::before {
  content: '';
  position: absolute;
  inset: 0;
  background: linear-gradient(180deg,
    rgba(99,102,241,0.03) 0%,
    transparent 40%,
    rgba(139,92,246,0.02) 100%
  );
  pointer-events: none;
}

/* ── Sidebar shell — DARK ──────────────────────────────────────── */
html.dark .admin-sidebar {
  background: #080e1a;
  border-right: 1px solid rgba(255,255,255,0.05);
}

/* ── Section label — LIGHT ─────────────────────────────────────── */
.sidebar-section-label {
  font-size: 9px;
  font-weight: 800;
  letter-spacing: 0.15em;
  text-transform: uppercase;
  color: rgba(71,85,105,0.5);
  padding: 0 10px 10px;
}

/* ── Section label — DARK ──────────────────────────────────────── */
html.dark .sidebar-section-label {
  color: rgba(148,163,184,0.4);
}

/* ── Nav container ─────────────────────────────────────────────── */
.sidebar-nav {
  display: flex;
  flex-direction: column;
  gap: 2px;
  flex: 1;
}

/* ── Nav item base ─────────────────────────────────────────────── */
.nav-item {
  position: relative;
  display: flex;
  align-items: center;
  gap: 10px;
  width: 100%;
  padding: 9px 12px;
  border-radius: 10px;
  font-size: 12px;
  font-weight: 600;
  letter-spacing: 0.01em;
  border: 1px solid transparent;
  cursor: pointer;
  transition:
    background 0.2s ease,
    border-color 0.2s ease,
    box-shadow 0.3s ease,
    color 0.2s ease,
    transform 0.15s ease;
  text-align: left;
  outline: none;
  background: transparent;
}

/* ── Idle state — LIGHT ────────────────────────────────────────── */
.nav-item--idle {
  color: #64748b;
}

/* ── Idle state — DARK ─────────────────────────────────────────── */
html.dark .nav-item--idle {
  color: rgba(148,163,184,0.7);
}

/* ── Hover per color — LIGHT ───────────────────────────────────── */
.nav-item--idle.nav-item--indigo:hover {
  background: rgba(99,102,241,0.08);
  border-color: rgba(99,102,241,0.2);
  color: #4f46e5;
}
.nav-item--idle.nav-item--violet:hover {
  background: rgba(139,92,246,0.08);
  border-color: rgba(139,92,246,0.2);
  color: #7c3aed;
}
.nav-item--idle.nav-item--sky:hover {
  background: rgba(14,165,233,0.08);
  border-color: rgba(14,165,233,0.2);
  color: #0284c7;
}
.nav-item--idle.nav-item--purple:hover {
  background: rgba(168,85,247,0.08);
  border-color: rgba(168,85,247,0.2);
  color: #9333ea;
}
.nav-item--idle.nav-item--amber:hover {
  background: rgba(245,158,11,0.08);
  border-color: rgba(245,158,11,0.2);
  color: #d97706;
}
.nav-item--idle.nav-item--emerald:hover {
  background: rgba(16,185,129,0.08);
  border-color: rgba(16,185,129,0.2);
  color: #059669;
}
.nav-item--idle.nav-item--rose:hover {
  background: rgba(244,63,94,0.08);
  border-color: rgba(244,63,94,0.2);
  color: #e11d48;
}

/* ── Hover per color — DARK (with glow) ────────────────────────── */
html.dark .nav-item--idle.nav-item--indigo:hover {
  background: rgba(99,102,241,0.1);
  border-color: rgba(99,102,241,0.2);
  color: #a5b4fc;
  box-shadow: 0 0 16px rgba(99,102,241,0.2), inset 0 0 8px rgba(99,102,241,0.05);
}
html.dark .nav-item--idle.nav-item--violet:hover {
  background: rgba(139,92,246,0.1);
  border-color: rgba(139,92,246,0.2);
  color: #c4b5fd;
  box-shadow: 0 0 16px rgba(139,92,246,0.2), inset 0 0 8px rgba(139,92,246,0.05);
}
html.dark .nav-item--idle.nav-item--sky:hover {
  background: rgba(14,165,233,0.1);
  border-color: rgba(14,165,233,0.2);
  color: #7dd3fc;
  box-shadow: 0 0 16px rgba(14,165,233,0.2), inset 0 0 8px rgba(14,165,233,0.05);
}
html.dark .nav-item--idle.nav-item--purple:hover {
  background: rgba(168,85,247,0.1);
  border-color: rgba(168,85,247,0.2);
  color: #d8b4fe;
  box-shadow: 0 0 16px rgba(168,85,247,0.2), inset 0 0 8px rgba(168,85,247,0.05);
}
html.dark .nav-item--idle.nav-item--amber:hover {
  background: rgba(245,158,11,0.1);
  border-color: rgba(245,158,11,0.2);
  color: #fcd34d;
  box-shadow: 0 0 16px rgba(245,158,11,0.2), inset 0 0 8px rgba(245,158,11,0.05);
}
html.dark .nav-item--idle.nav-item--emerald:hover {
  background: rgba(16,185,129,0.1);
  border-color: rgba(16,185,129,0.2);
  color: #6ee7b7;
  box-shadow: 0 0 16px rgba(16,185,129,0.2), inset 0 0 8px rgba(16,185,129,0.05);
}
html.dark .nav-item--idle.nav-item--rose:hover {
  background: rgba(244,63,94,0.1);
  border-color: rgba(244,63,94,0.2);
  color: #fda4af;
  box-shadow: 0 0 16px rgba(244,63,94,0.2), inset 0 0 8px rgba(244,63,94,0.05);
}

/* Hover scale — both modes */
.nav-item--idle:hover {
  transform: translateX(2px);
}

/* ── Active state — LIGHT ──────────────────────────────────────── */
.nav-item--active.nav-item--indigo {
  background: rgba(99,102,241,0.1);
  border-color: rgba(99,102,241,0.3);
  color: #4f46e5;
}
.nav-item--active.nav-item--violet {
  background: rgba(139,92,246,0.1);
  border-color: rgba(139,92,246,0.3);
  color: #7c3aed;
}
.nav-item--active.nav-item--sky {
  background: rgba(14,165,233,0.1);
  border-color: rgba(14,165,233,0.3);
  color: #0284c7;
}
.nav-item--active.nav-item--purple {
  background: rgba(168,85,247,0.1);
  border-color: rgba(168,85,247,0.3);
  color: #9333ea;
}
.nav-item--active.nav-item--amber {
  background: rgba(245,158,11,0.1);
  border-color: rgba(245,158,11,0.3);
  color: #d97706;
}
.nav-item--active.nav-item--emerald {
  background: rgba(16,185,129,0.1);
  border-color: rgba(16,185,129,0.3);
  color: #059669;
}
.nav-item--active.nav-item--rose {
  background: rgba(244,63,94,0.1);
  border-color: rgba(244,63,94,0.3);
  color: #e11d48;
}

/* ── Active state — DARK (with glow) ───────────────────────────── */
html.dark .nav-item--active.nav-item--indigo {
  background: linear-gradient(135deg, rgba(99,102,241,0.25), rgba(99,102,241,0.1));
  border-color: rgba(99,102,241,0.35);
  color: #a5b4fc;
  box-shadow: 0 0 24px rgba(99,102,241,0.3), 0 4px 12px rgba(0,0,0,0.3), inset 0 1px 0 rgba(99,102,241,0.2);
}
html.dark .nav-item--active.nav-item--violet {
  background: linear-gradient(135deg, rgba(139,92,246,0.25), rgba(139,92,246,0.1));
  border-color: rgba(139,92,246,0.35);
  color: #c4b5fd;
  box-shadow: 0 0 24px rgba(139,92,246,0.3), 0 4px 12px rgba(0,0,0,0.3);
}
html.dark .nav-item--active.nav-item--sky {
  background: linear-gradient(135deg, rgba(14,165,233,0.25), rgba(14,165,233,0.1));
  border-color: rgba(14,165,233,0.35);
  color: #7dd3fc;
  box-shadow: 0 0 24px rgba(14,165,233,0.3), 0 4px 12px rgba(0,0,0,0.3);
}
html.dark .nav-item--active.nav-item--purple {
  background: linear-gradient(135deg, rgba(168,85,247,0.25), rgba(168,85,247,0.1));
  border-color: rgba(168,85,247,0.35);
  color: #d8b4fe;
  box-shadow: 0 0 24px rgba(168,85,247,0.3), 0 4px 12px rgba(0,0,0,0.3);
}
html.dark .nav-item--active.nav-item--amber {
  background: linear-gradient(135deg, rgba(245,158,11,0.25), rgba(245,158,11,0.1));
  border-color: rgba(245,158,11,0.35);
  color: #fcd34d;
  box-shadow: 0 0 24px rgba(245,158,11,0.3), 0 4px 12px rgba(0,0,0,0.3);
}
html.dark .nav-item--active.nav-item--emerald {
  background: linear-gradient(135deg, rgba(16,185,129,0.25), rgba(16,185,129,0.1));
  border-color: rgba(16,185,129,0.35);
  color: #6ee7b7;
  box-shadow: 0 0 24px rgba(16,185,129,0.3), 0 4px 12px rgba(0,0,0,0.3);
}
html.dark .nav-item--active.nav-item--rose {
  background: linear-gradient(135deg, rgba(244,63,94,0.25), rgba(244,63,94,0.1));
  border-color: rgba(244,63,94,0.35);
  color: #fda4af;
  box-shadow: 0 0 24px rgba(244,63,94,0.3), 0 4px 12px rgba(0,0,0,0.3);
}

/* ── Icon ──────────────────────────────────────────────────────── */
.nav-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  transition: filter 0.2s ease;
}
html.dark .nav-item--active .nav-icon,
html.dark .nav-item:hover .nav-icon {
  filter: drop-shadow(0 0 5px currentColor);
}

/* ── Label ─────────────────────────────────────────────────────── */
.nav-label {
  flex: 1;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

/* ── Active dot indicator ──────────────────────────────────────── */
.nav-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: currentColor;
  box-shadow: 0 0 6px currentColor;
  flex-shrink: 0;
  animation: pulse-dot 2s ease-in-out infinite;
}

@keyframes pulse-dot {
  0%, 100% { opacity: 1; transform: scale(1); }
  50%       { opacity: 0.6; transform: scale(0.8); }
}

/* ── Operator badge — LIGHT ────────────────────────────────────── */
.sidebar-operator-badge {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: 8px;
  padding: 8px 12px;
  background: rgba(245,158,11,0.08);
  border: 1px solid rgba(245,158,11,0.25);
  border-radius: 10px;
  font-size: 10px;
  font-weight: 800;
  letter-spacing: 0.1em;
  text-transform: uppercase;
  color: #d97706;
}

html.dark .sidebar-operator-badge {
  color: #fbbf24;
}

.badge-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: currentColor;
  box-shadow: 0 0 8px currentColor;
  animation: pulse-dot 1.5s ease-in-out infinite;
  flex-shrink: 0;
}

/* ── Scrollbar ─────────────────────────────────────────────────── */
.admin-sidebar::-webkit-scrollbar { width: 3px; }
.admin-sidebar::-webkit-scrollbar-track { background: transparent; }
.admin-sidebar::-webkit-scrollbar-thumb { background: rgba(99,102,241,0.2); border-radius: 2px; }
html.dark .admin-sidebar::-webkit-scrollbar-thumb { background: rgba(99,102,241,0.35); }
</style>
