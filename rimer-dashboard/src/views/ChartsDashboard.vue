<script setup>
import { ref, onMounted, onUnmounted, nextTick, computed, watch, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { auth, toastStore } from '../auth'
import { i18n, theme } from '../lang'
import * as api from '../services/ticketApi'
import LangSelector from '../components/LangSelector.vue'
import AdminTabs from '../components/AdminTabs.vue'
import * as echarts from 'echarts'
import html2pdf from 'html2pdf.js'

const props = defineProps({ embedded: { type: Boolean, default: false } })

const router = useRouter()
const user = auth.user
const data = ref(null)
const loading = ref(true)

// Date filter
const filterMode = ref('all')
const customStart = ref('')
const customEnd = ref('')

// Chart refs
const categoryChartRef = ref(null)
const statusChartRef = ref(null)
const deptChartRef = ref(null)
const agingChartRef = ref(null)
const performanceChartRef = ref(null)
const satisfactionChartRef = ref(null)
const complaintChartRef = ref(null)
const staffTypeChartRef = ref(null)
// New executive charts
const avgResponseChartRef = ref(null)
const monthlyVolumeChartRef = ref(null)
const priorityMatrixChartRef = ref(null)

let chartInstances = []

// ── Helpers ───────────────────────────────────────────────────────
const has = (key) => data.value?.charts?.[key] !== undefined
const isDark = computed(() => theme.dark)
const textColor = computed(() => isDark.value ? '#94a3b8' : '#64748b')
const bgCard = computed(() => isDark.value ? '#0f172a' : '#ffffff')

// ── Date range ────────────────────────────────────────────────────
const dateRange = computed(() => {
  const now = new Date()
  switch (filterMode.value) {
    case 'month': return { start: new Date(now.getFullYear(), now.getMonth(), 1).toISOString(), end: now.toISOString() }
    case 'year': return { start: new Date(now.getFullYear(), 0, 1).toISOString(), end: now.toISOString() }
    case 'custom': return { start: customStart.value || null, end: customEnd.value || null }
    default: return { start: null, end: null }
  }
})

// ── Load ──────────────────────────────────────────────────────────
const loadData = async () => {
  loading.value = true
  try {
    const { start, end } = dateRange.value
    const res = await api.getCharts(start, end)
    data.value = res.data
    
    // Crucial: Set loading to false FIRST so the DOM elements (refs) are rendered
    loading.value = false
    
    await nextTick()
    renderCharts()
  } catch (e) {
    loading.value = false
    if (e.response?.status === 403) {
      toastStore.add('Grafik erişim izniniz bulunmuyor', 'error')
    } else {
      const errMsg = e.response?.data?.message || e.message || 'Analiz verileri yüklenemedi';
      toastStore.add(`Analiz hatası: ${errMsg}`, 'error')
    }
  }
}

watch(filterMode, () => loadData())
const applyCustomFilter = () => { if (customStart.value && customEnd.value) loadData() }

// ── Category colors ───────────────────────────────────────────────
const catColors = { complaint: '#ef4444', request: '#3b82f6', suggestion: '#22c55e', info: '#6b7280', thanks: '#eab308' }
const catLabels = computed(() => ({ 
  complaint: i18n.t.cat0, 
  request: i18n.t.cat2, 
  suggestion: i18n.t.cat1, 
  info: i18n.t.catInfo, 
  thanks: i18n.t.catThanks 
}))

// ── Chart rendering ───────────────────────────────────────────────
const renderCharts = () => {
  if (!data.value) return
  disposeCharts()
  const charts = data.value.charts || {}

  // Category Pie
  if (charts.categoryDistribution && categoryChartRef.value) {
    const c = echarts.init(categoryChartRef.value)
    chartInstances.push(c)
    
    // Sort descending by count
    const catData = charts.categoryDistribution
      .filter(x => (x.count || x.Count) > 0)
      .sort((a, b) => (b.count || b.Count) - (a.count || a.Count))

    c.setOption({
      tooltip: { 
        trigger: 'item', 
        formatter: '{b}: <b>{c} Adet</b> ({d}%)' 
      },
      legend: { 
        bottom: 0, 
        textStyle: { color: textColor.value, fontSize: 12, fontWeight: 'bold' },
        formatter: (name) => {
          const item = catData.find(x => (catLabels.value[x.name || x.Name] || (x.name || x.Name)) === name)
          return item ? `${name}: ${item.count || item.Count}` : name
        }
      },
      series: [{ 
        type: 'pie', 
        radius: ['0%', '65%'], 
        center: ['50%', '42%'],
        itemStyle: { borderRadius: 6, borderColor: bgCard.value, borderWidth: 3 },
        label: { 
          show: true, 
          position: 'inside', 
          formatter: '{d}%', 
          fontSize: 13, 
          fontWeight: '900',
          color: '#fff',
          textShadowColor: 'rgba(0,0,0,0.8)',
          textShadowBlur: 6
        },
        emphasis: { 
          label: { show: true, fontSize: 14 } 
        },
        data: catData.map(x => ({ 
          value: x.count || x.Count, 
          name: catLabels.value[x.name || x.Name] || (x.name || x.Name), 
          itemStyle: { color: catColors[x.name || x.Name] || '#6b7280' } 
        }))
      }]
    })
  }

  // Status Stacked Area (Trend)
  if (charts.statusTrend && statusChartRef.value) {
    const c = echarts.init(statusChartRef.value)
    chartInstances.push(c)
    const trend = charts.statusTrend
    
    c.setOption({
      tooltip: {
        trigger: 'axis',
        axisPointer: { type: 'cross', label: { backgroundColor: '#6a7985' } },
        formatter: (params) => {
          if (!params.length) return ''
          const item = trend[params[0].dataIndex]
          const u = item.unanswered ?? item.Unanswered ?? 0
          const pe = item.pending ?? item.Pending ?? 0
          const w = item.waiting ?? item.Waiting ?? 0
          const r = item.resolved ?? item.Resolved ?? 0
          const total = u + pe + w + r
          const pct = (v) => total > 0 ? ` (${((v/total)*100).toFixed(0)}%)` : ''
          return `<b>${params[0].axisValue}</b><br/>
            ${i18n.t.statResolved}: <b>${r}</b>${pct(r)}<br/>
            ${i18n.t.statPending}: <b>${pe}</b>${pct(pe)}<br/>
            ${i18n.t.statWaiting}: <b>${w}</b>${pct(w)}<br/>
            ${i18n.t.statUnanswered}: <b>${u}</b>${pct(u)}<br/>
            <hr style="margin:4px 0;border-color:#ddd"/>Toplam: <b>${total}</b>`
        }
      },
      legend: {
        data: [i18n.t.statResolved, i18n.t.statPending, i18n.t.statWaiting, i18n.t.statUnanswered],
        bottom: 0,
        textStyle: { color: textColor.value, fontSize: 11, fontWeight: 'bold' }
      },
      grid: { left: '3%', right: '4%', bottom: '12%', top: '8%', containLabel: true },
      xAxis: {
        type: 'category',
        data: trend.map(x => x.month || x.Month),
        axisLabel: { color: textColor.value, fontWeight: 'bold' },
        axisTick: { alignWithLabel: true }
      },
      yAxis: {
        type: 'value',
        axisLabel: { color: textColor.value, fontWeight: 'bold' },
        splitLine: { lineStyle: { color: isDark.value ? '#1e293b' : '#f1f5f9' } }
      },
      series: [
        {
          name: i18n.t.statUnanswered,
          type: 'bar',
          stack: 'Total',
          barWidth: '50%',
          itemStyle: { color: '#ef4444' },
          label: { show: true, position: 'inside', color: '#fff', fontSize: 10, fontWeight: 'bold', formatter: p => p.value > 0 ? p.value : '' },
          data: trend.map(x => x.unanswered ?? x.Unanswered ?? 0)
        },
        {
          name: i18n.t.statPending,
          type: 'bar',
          stack: 'Total',
          itemStyle: { color: '#eab308' },
          label: { show: true, position: 'inside', color: '#fff', fontSize: 10, fontWeight: 'bold', formatter: p => p.value > 0 ? p.value : '' },
          data: trend.map(x => x.pending ?? x.Pending ?? 0)
        },
        {
          name: i18n.t.statWaiting,
          type: 'bar',
          stack: 'Total',
          itemStyle: { color: '#a855f7' },
          label: { show: true, position: 'inside', color: '#fff', fontSize: 10, fontWeight: 'bold', formatter: p => p.value > 0 ? p.value : '' },
          data: trend.map(x => x.waiting ?? x.Waiting ?? 0)
        },
        {
          name: i18n.t.statResolved,
          type: 'bar',
          stack: 'Total',
          itemStyle: { color: '#3b82f6', borderRadius: [4, 4, 0, 0] },
          label: { show: true, position: 'inside', color: '#fff', fontSize: 10, fontWeight: 'bold', formatter: p => p.value > 0 ? p.value : '' },
          data: trend.map(x => x.resolved ?? x.Resolved ?? 0)
        }
      ]
    })
  }

  // Department Horizontal Bar
  if (charts.departmentRanking?.length && deptChartRef.value) {
    const c = echarts.init(deptChartRef.value)
    chartInstances.push(c)
    
    // Sort and slice top 15 (just in case there are more than 10)
    const d = charts.departmentRanking.slice(0, 15)
    const totalDeptTickets = charts.departmentRanking.reduce((sum, item) => sum + (item.count || item.Count || 0), 0)

    const rankColors = [
      '#800000', // 1. Bordo
      '#ef4444', // 2. Kırmızı
      '#f97316', // 3. Turuncu
      '#eab308', // 4. Sarı
      '#1e3a8a', // 5. Koyu Mavi
      '#3b82f6', // 6. Mavi
      '#93c5fd', // 7. Açık Mavi
      '#064e3b', // 8. Koyu Yeşil
      '#10b981', // 9. Yeşil
      '#6ee7b7', // 10. Açık Yeşil
    ]

    c.setOption({
      tooltip: { 
        trigger: 'axis', 
        axisPointer: { type: 'shadow' },
        formatter: (params) => {
          const p = params[0]
          const val = p.value
          const pct = totalDeptTickets > 0 ? ((val / totalDeptTickets) * 100).toFixed(1) : 0
          return `${p.name}<br/><b>${val} ${i18n.lang === 'tr' ? 'Talep' : 'Tickets'}</b> (${pct}%)`
        }
      },
      grid: { left: '3%', right: '15%', bottom: '3%', top: '3%', containLabel: true },
      xAxis: { 
        type: 'value', 
        boundaryGap: [0, '20%'],
        axisLabel: { color: textColor.value, fontWeight: 'bold' }, 
        splitLine: { lineStyle: { color: isDark.value ? '#1e293b' : '#f1f5f9' } } 
      },
      yAxis: { 
        type: 'category', 
        data: d.map(x => x.name || x.Name).reverse(), 
        axisLabel: { color: textColor.value, fontSize: 11, fontWeight: 'bold' }, 
        axisTick: { show: false }, 
        axisLine: { show: false } 
      },
      series: [{ 
        type: 'bar', 
        barWidth: '65%',
        data: d.map((x, i) => {
          const val = x.count || x.Count || 0
          return {
            value: val,
            itemStyle: { color: rankColors[i] || '#94a3b8', borderRadius: [0, 6, 6, 0] }
          }
        }).reverse(),
        label: {
          show: true,
          position: 'right',
          color: textColor.value,
          fontWeight: 'bold',
          fontSize: 12,
          formatter: (p) => {
            const val = p.value;
            const pct = totalDeptTickets > 0 ? ((val / totalDeptTickets) * 100).toFixed(1) : 0;
            return `${val} (${pct}%)`;
          }
        }
      }]
    })
  }

  // Aging Bar
  if (charts.agingAnalysis && agingChartRef.value) {
    const c = echarts.init(agingChartRef.value)
    chartInstances.push(c)
    const ag = charts.agingAnalysis
    c.setOption({
      tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
      grid: { left: '3%', right: '4%', bottom: '5%', top: '15%', containLabel: true },
      xAxis: { type: 'category', data: [i18n.t.aging45, i18n.t.aging60, i18n.t.aging90, i18n.t.aging180], axisLabel: { color: textColor.value, fontSize: 12, fontWeight: 'bold' }, axisTick: { show: false }, axisLine: { lineStyle: { color: isDark.value ? '#1e293b' : '#e2e8f0' } } },
      yAxis: { 
        type: 'value', 
        boundaryGap: [0, '30%'], // Give 30% extra space at the top for labels
        axisLabel: { color: textColor.value, fontWeight: 'bold' }, 
        splitLine: { lineStyle: { color: isDark.value ? '#1e293b' : '#f1f5f9' } } 
      },
      series: [{ type: 'bar', barWidth: '50%', itemStyle: { borderRadius: [6, 6, 0, 0] },
        label: { show: true, position: 'top', color: textColor.value, fontWeight: 'bold', fontSize: 12 },
        data: [
          { value: ag.over45 || ag.Over45 || 0, itemStyle: { color: '#eab308' } },
          { value: ag.over60 || ag.Over60 || 0, itemStyle: { color: '#f97316' } },
          { value: ag.over90 || ag.Over90 || 0, itemStyle: { color: '#dc2626' } },
          { value: ag.over180 || ag.Over180 || 0, itemStyle: { color: '#991b1b' } }
        ]
      }]
    })
  }

  // Performance Radar Chart
  if (charts.departmentPerformance && performanceChartRef.value) {
    const c = echarts.init(performanceChartRef.value)
    chartInstances.push(c)
    const perf = charts.departmentPerformance.slice(0, 6)
    
    const radarColors = ['#6366f1', '#22c55e', '#ef4444', '#f59e0b', '#8b5cf6', '#06b6d4']
    
    c.setOption({
      tooltip: { trigger: 'item' },
      legend: { 
        data: perf.map(x => x.name || x.Name), 
        bottom: 0, 
        textStyle: { color: textColor.value, fontSize: 11 } 
      },
      radar: {
        indicator: [
          { name: i18n.t.perfResRate, max: 100 },
          { name: i18n.t.perfInProgress, max: 100 },
          { name: i18n.t.perfOverdue + ' ↓', max: 100 },
          { name: i18n.t.perfTransfer + ' ↓', max: 100 },
          { name: i18n.t.perfCritical + ' ↓', max: 100 }
        ],
        shape: 'polygon',
        splitNumber: 4,
        center: ['50%', '45%'],
        radius: '60%',
        axisName: { color: textColor.value, fontSize: 10, fontWeight: 'bold' },
        splitArea: { 
          areaStyle: { color: isDark.value 
            ? ['rgba(99,102,241,0.05)', 'rgba(99,102,241,0.1)', 'rgba(99,102,241,0.15)', 'rgba(99,102,241,0.2)']
            : ['rgba(99,102,241,0.02)', 'rgba(99,102,241,0.05)', 'rgba(99,102,241,0.08)', 'rgba(99,102,241,0.12)']
          }
        },
        splitLine: { lineStyle: { color: isDark.value ? '#1e293b' : '#e2e8f0' } },
        axisLine: { lineStyle: { color: isDark.value ? '#334155' : '#cbd5e1' } }
      },
      series: [{
        type: 'radar',
        data: perf.map((x, i) => ({
          name: x.name || x.Name,
          value: [
            x.resolutionRate || x.ResolutionRate || 0,
            x.totalTickets > 0 ? Math.round(((x.inProgressTickets || x.InProgressTickets || 0) / (x.totalTickets || x.TotalTickets || 1)) * 100) : 0,
            100 - (x.overdueRate || x.OverdueRate || 0),    // Invert: lower overdue = higher score
            100 - (x.transferRate || x.TransferRate || 0),   // Invert: lower transfer = higher score
            x.totalTickets > 0 ? Math.round((1 - (x.criticalTickets || x.CriticalTickets || 0) / (x.totalTickets || x.TotalTickets || 1)) * 100) : 100
          ],
          lineStyle: { width: 2, color: radarColors[i] },
          areaStyle: { opacity: 0.15, color: radarColors[i] },
          itemStyle: { color: radarColors[i] },
          symbol: 'circle',
          symbolSize: 6
        }))
      }]
    })
  }

  // Satisfaction (Thanks) by Department — Horizontal gradient bar (green tones)
  if (charts.satisfactionByDepartment?.length && satisfactionChartRef.value) {
    const c = echarts.init(satisfactionChartRef.value)
    chartInstances.push(c)
    const d = charts.satisfactionByDepartment.slice(0, 10)
    const total = d.reduce((s, x) => s + (x.count || x.Count || 0), 0)

    c.setOption({
      tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' }, formatter: (p) => {
        const val = p[0].value; const pct = total > 0 ? ((val / total) * 100).toFixed(1) : 0
        return `${p[0].name}<br/><b>${val}</b> (${pct}%)`
      }},
      grid: { left: '3%', right: '15%', bottom: '3%', top: '3%', containLabel: true },
      xAxis: { type: 'value', axisLabel: { color: textColor.value }, splitLine: { lineStyle: { color: isDark.value ? '#1e293b' : '#f1f5f9' } } },
      yAxis: { type: 'category', data: d.map(x => x.name || x.Name).reverse(), axisLabel: { color: textColor.value, fontSize: 11, fontWeight: 'bold' }, axisTick: { show: false }, axisLine: { show: false } },
      series: [{ type: 'bar', barWidth: '60%',
        data: d.map((x, i) => {
          const val = x.count || x.Count || 0
          const ratio = d.length > 1 ? i / (d.length - 1) : 0
          return { value: val, itemStyle: { borderRadius: [0, 6, 6, 0], color: new echarts.graphic.LinearGradient(0, 0, 1, 0, [
            { offset: 0, color: isDark.value ? '#064e3b' : '#d1fae5' },
            { offset: 1, color: `hsl(${150 - ratio * 30}, 70%, ${isDark.value ? 45 : 40}%)` }
          ])} }
        }).reverse(),
        label: { show: true, position: 'right', color: textColor.value, fontWeight: 'bold', fontSize: 11,
          formatter: (p) => { const pct = total > 0 ? ((p.value / total) * 100).toFixed(1) : 0; return `${p.value} (${pct}%)` }
        }
      }]
    })
  }

  // Complaint by Department — Stacked bar (red heatmap style)
  if (charts.complaintByDepartment?.length && complaintChartRef.value) {
    const c = echarts.init(complaintChartRef.value)
    chartInstances.push(c)
    const d = charts.complaintByDepartment.slice(0, 10)
    const total = d.reduce((s, x) => s + (x.count || x.Count || 0), 0)
    const maxVal = Math.max(...d.map(x => x.count || x.Count || 0), 1)

    c.setOption({
      tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' }, formatter: (p) => {
        const val = p[0].value; const pct = total > 0 ? ((val / total) * 100).toFixed(1) : 0
        return `${p[0].name}<br/><b>${val}</b> (${pct}%)`
      }},
      grid: { left: '3%', right: '15%', bottom: '3%', top: '3%', containLabel: true },
      xAxis: { type: 'value', axisLabel: { color: textColor.value }, splitLine: { lineStyle: { color: isDark.value ? '#1e293b' : '#f1f5f9' } } },
      yAxis: { type: 'category', data: d.map(x => x.name || x.Name).reverse(), axisLabel: { color: textColor.value, fontSize: 11, fontWeight: 'bold' }, axisTick: { show: false }, axisLine: { show: false } },
      series: [{ type: 'bar', barWidth: '60%',
        data: d.map(x => {
          const val = x.count || x.Count || 0
          const intensity = val / maxVal
          const hue = Math.round(10 - intensity * 10) // 10 (orange-red) → 0 (pure red)
          const light = Math.round(60 - intensity * 25) // lighter for low, darker for high
          return { value: val, itemStyle: { borderRadius: [0, 6, 6, 0], color: `hsl(${hue}, 85%, ${isDark.value ? light - 10 : light}%)` } }
        }).reverse(),
        label: { show: true, position: 'right', color: textColor.value, fontWeight: 'bold', fontSize: 11,
          formatter: (p) => { const pct = total > 0 ? ((p.value / total) * 100).toFixed(1) : 0; return `${p.value} (${pct}%)` }
        }
      }]
    })
  }

  // Staff Type Distribution — Grouped bar chart
  if (charts.staffTypeDistribution?.length && staffTypeChartRef.value) {
    const c = echarts.init(staffTypeChartRef.value)
    chartInstances.push(c)
    const st = charts.staffTypeDistribution

    c.setOption({
      tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
      legend: { data: [i18n.t.cat0, i18n.t.cat1, i18n.t.cat2, i18n.t.catThanks, i18n.t.catInfo], bottom: 0, textStyle: { color: textColor.value, fontSize: 11 } },
      grid: { left: '3%', right: '4%', bottom: '15%', top: '10%', containLabel: true },
      xAxis: { type: 'category', data: st.map(x => x.typeName || x.TypeName), axisLabel: { color: textColor.value, fontSize: 11, fontWeight: 'bold' } },
      yAxis: { type: 'value', axisLabel: { color: textColor.value }, splitLine: { lineStyle: { color: isDark.value ? '#1e293b' : '#f1f5f9' } } },
      series: [
        { 
          name: i18n.t.cat0, type: 'bar', stack: 'total', 
          data: st.map(x => x.complaint || x.Complaint || 0), 
          itemStyle: { color: '#ef4444' },
          label: { show: true, position: 'inside', color: '#fff', fontSize: 10, fontWeight: 'bold', formatter: (p) => p.value > 0 ? p.value : '' }
        },
        { 
          name: i18n.t.cat1, type: 'bar', stack: 'total', 
          data: st.map(x => x.suggestion || x.Suggestion || 0), 
          itemStyle: { color: '#22c55e' },
          label: { show: true, position: 'inside', color: '#fff', fontSize: 10, fontWeight: 'bold', formatter: (p) => p.value > 0 ? p.value : '' }
        },
        { 
          name: i18n.t.cat2, type: 'bar', stack: 'total', 
          data: st.map(x => x.request || x.Request || 0), 
          itemStyle: { color: '#3b82f6' },
          label: { show: true, position: 'inside', color: '#fff', fontSize: 10, fontWeight: 'bold', formatter: (p) => p.value > 0 ? p.value : '' }
        },
        { 
          name: i18n.t.catThanks, type: 'bar', stack: 'total', 
          data: st.map(x => x.thanks || x.Thanks || 0), 
          itemStyle: { color: '#eab308' },
          label: { show: true, position: 'inside', color: '#000', fontSize: 10, fontWeight: 'bold', formatter: (p) => p.value > 0 ? p.value : '' }
        },
        { 
          name: i18n.t.catInfo, type: 'bar', stack: 'total', 
          data: st.map(x => x.info || x.Info || 0), 
          itemStyle: { color: '#6b7280' },
          label: { show: true, position: 'inside', color: '#fff', fontSize: 10, fontWeight: 'bold', formatter: (p) => p.value > 0 ? p.value : '' }
        }
      ]
    })
  }

  // ── Average Response Time — Horizontal bar per department ──────
  if (charts.averageResponseTime?.byDepartment?.length && avgResponseChartRef.value) {
    const c = echarts.init(avgResponseChartRef.value)
    chartInstances.push(c)
    const byDept = charts.averageResponseTime.byDepartment
    const overallAvg = charts.averageResponseTime.overallAvgDays ?? 0
    c.setOption({
      tooltip: { trigger: 'axis', formatter: (p) => `${p[0].name}<br/><b>${p[0].value} gün</b>` },
      grid: { left: '3%', right: '12%', bottom: '3%', top: '5%', containLabel: true },
      xAxis: { type: 'value', axisLabel: { color: textColor.value, formatter: v => `${v}g` },
        splitLine: { lineStyle: { color: isDark.value ? '#1e293b' : '#f1f5f9' } }
      },
      yAxis: { type: 'category', data: byDept.map(x => x.name).reverse(),
        axisLabel: { color: textColor.value, fontSize: 10, fontWeight: 'bold' },
        axisTick: { show: false }, axisLine: { show: false }
      },
      series: [
        { type: 'bar', barWidth: '55%',
          data: byDept.map(x => {
            const v = x.avgDays
            const color = v <= 7 ? '#22c55e' : v <= 30 ? '#f59e0b' : '#ef4444'
            return { value: v, itemStyle: { color, borderRadius: [0, 8, 8, 0] } }
          }).reverse(),
          label: { show: true, position: 'right', color: textColor.value, fontWeight: 'bold', fontSize: 11,
            formatter: p => `${p.value}g`
          },
          markLine: {
            data: [{ xAxis: overallAvg, name: 'Ort.' }],
            label: { formatter: `Ort: ${overallAvg}g`, color: '#6366f1', position: 'insideEndTop' },
            lineStyle: { color: '#6366f1', type: 'dashed' }
          }
        }
      ]
    })
  }

  // ── Monthly Volume Trend — Line chart last 12 months ────────────
  if (charts.monthlyVolumeTrend?.months?.length && monthlyVolumeChartRef.value) {
    const c = echarts.init(monthlyVolumeChartRef.value)
    chartInstances.push(c)
    const months = charts.monthlyVolumeTrend.months
    c.setOption({
      tooltip: {
        trigger: 'axis',
        formatter: (p) => {
          const m = months[p[0].dataIndex]
          const chg = m.changePercent !== null && m.changePercent !== undefined
            ? ` (${m.changePercent >= 0 ? '+' : ''}${m.changePercent}%)` : ''
          return `${p[0].name}<br/><b>${p[0].value} talep</b>${chg}`
        }
      },
      grid: { left: '3%', right: '4%', bottom: '15%', top: '15%', containLabel: true },
      xAxis: { type: 'category', data: months.map(m => m.month),
        axisLabel: { color: textColor.value, fontSize: 10, rotate: 30 },
        axisTick: { alignWithLabel: true }
      },
      yAxis: { type: 'value', axisLabel: { color: textColor.value },
        splitLine: { lineStyle: { color: isDark.value ? '#1e293b' : '#f1f5f9' } }
      },
      series: [{
        type: 'line', smooth: true, symbol: 'circle', symbolSize: 7,
        data: months.map(m => m.count ?? 0),
        itemStyle: { color: '#6366f1' },
        lineStyle: { color: '#6366f1', width: 3 },
        areaStyle: {
          color: {
            type: 'linear', x: 0, y: 0, x2: 0, y2: 1,
            colorStops: [
              { offset: 0, color: isDark.value ? 'rgba(99,102,241,0.35)' : 'rgba(99,102,241,0.15)' },
              { offset: 1, color: 'rgba(99,102,241,0)' }
            ]
          }
        },
        label: {
          show: true, position: 'top', color: textColor.value, fontSize: 10, fontWeight: 'bold',
          formatter: (p) => {
            const chg = months[p.dataIndex]?.changePercent
            if (chg === null || chg === undefined || p.dataIndex === 0) return `${p.value}`
            return `${p.value}\n${chg >= 0 ? '▲' : '▼'}${Math.abs(chg)}%`
          }
        }
      }]
    })
  }

  // ── Priority × Status Matrix — Grouped stacked bar ───────────────
  if (charts.priorityStatusMatrix?.matrix?.length && priorityMatrixChartRef.value) {
    const c = echarts.init(priorityMatrixChartRef.value)
    chartInstances.push(c)
    const d = charts.priorityStatusMatrix
    const statuses = d.statuses ?? ['Yeni','İnceleniyor','Birimde','Cevaplandı','Kapatıldı']
    const matrix = d.matrix
    const statusColors = ['#f59e0b','#3b82f6','#a855f7','#22c55e','#64748b']
    c.setOption({
      tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
      legend: { data: statuses, bottom: 0, textStyle: { color: textColor.value, fontSize: 10 } },
      grid: { left: '3%', right: '4%', bottom: '20%', top: '5%', containLabel: true },
      xAxis: { type: 'category', data: matrix.map(m => m.priority),
        axisLabel: { color: textColor.value, fontWeight: 'bold', fontSize: 11 }
      },
      yAxis: { type: 'value', axisLabel: { color: textColor.value },
        splitLine: { lineStyle: { color: isDark.value ? '#1e293b' : '#f1f5f9' } }
      },
      series: statuses.map((status, si) => ({
        name: status, type: 'bar', stack: 'total',
        itemStyle: { color: statusColors[si] },
        label: { show: true, position: 'inside', color: '#fff', fontSize: 10, fontWeight: 'bold',
          formatter: p => p.value > 0 ? p.value : ''
        },
        data: matrix.map(m => m.byStatus.find(s => s.status === status)?.count ?? 0)
      }))
    })
  }
}

const disposeCharts = () => { chartInstances.forEach(c => c.dispose()); chartInstances = [] }
const handleResize = () => chartInstances.forEach(c => c.resize())

const isAdmin = computed(() => {
  const r = (user.role ?? '').toLowerCase()
  return r === 'admin' || r === 'staff'
})

const goHome = () => {
  const r = (user.role ?? '').toLowerCase()
  if (r === 'admin' || r === 'staff') router.push('/admin')
  else router.push('/dashboard')
}

const doLogout = () => { auth.logout(); router.push('/login') }

const exporting = ref(false)
const chartImages = reactive({ cat: '', trend: '', dept: '', aging: '', perf: '', sat: '', comp: '', staff: '', avgResp: '', monthly: '', priMatrix: '' })

const criticalDepts = computed(() => {
  const aging = data.value?.charts?.agingAnalysis
  if (!aging) return []
  return aging.criticalDepartments || aging.CriticalDepartments || []
})

const exportPdf = async () => {
  exporting.value = true;
  try {
    const getImg = (refObj) => {
      if (!refObj.value) return '';
      const instance = echarts.getInstanceByDom(refObj.value);
      return instance ? instance.getDataURL({ type: 'png', pixelRatio: 2, backgroundColor: '#fff' }) : '';
    };

    chartImages.cat      = getImg(categoryChartRef);
    chartImages.trend    = getImg(statusChartRef);
    chartImages.dept     = getImg(deptChartRef);
    chartImages.aging    = getImg(agingChartRef);
    chartImages.perf     = getImg(performanceChartRef);
    chartImages.sat      = getImg(satisfactionChartRef);
    chartImages.comp     = getImg(complaintChartRef);
    chartImages.staff    = getImg(staffTypeChartRef);
    chartImages.avgResp  = getImg(avgResponseChartRef);
    chartImages.monthly  = getImg(monthlyVolumeChartRef);
    chartImages.priMatrix= getImg(priorityMatrixChartRef);

    await nextTick();
    window.print();
  } finally {
    exporting.value = false;
  }
}

// ── Executive insight computeds ────────────────────────────────────
const avgResponseInsight = computed(() => {
  const d = data.value?.charts?.averageResponseTime
  if (!d) return ''
  const avg = d.overallAvgDays ?? d.OverallAvgDays ?? 0
  const fastest = d.fastestDept ?? d.FastestDept
  const fastestDays = d.fastestDays ?? d.FastestDays
  const slowest = d.slowestDept ?? d.SlowestDept
  const slowestDays = d.slowestDays ?? d.SlowestDays
  const total = d.totalClosed ?? d.TotalClosed ?? 0
  let insight = `Bu dönemde kapatılan ${total} talebin ortalama yanıt süresi ${avg} gündür.`
  if (fastest && slowest && fastest !== slowest)
    insight += ` En hızlı birim: ${fastest} (${fastestDays} gün). En yavaş birim: ${slowest} (${slowestDays} gün).`
  if (avg <= 7)        insight += ' Genel süre hedef aralığında görünmektedir.'
  else if (avg <= 30)  insight += ' Süre kabul edilebilir düzeyde ancak iyileştirme yapılabilir.'
  else                 insight += ' Ortalama süre yüksek; birim kapasiteleri ve önceliklendirme gözden geçirilmelidir.'
  return insight
})

const monthlyVolumeInsight = computed(() => {
  const d = data.value?.charts?.monthlyVolumeTrend
  if (!d) return ''
  const months = d.months ?? d.Months ?? []
  const peak = d.peakMonth ?? d.PeakMonth
  const peakCount = d.peakCount ?? d.PeakCount ?? 0
  const latestChange = d.latestChange ?? d.LatestChange
  const latestCount = d.latestCount ?? d.LatestCount ?? 0
  let insight = `Son 12 ayda toplam ${months.reduce((s, m) => s + (m.count ?? 0), 0)} talep kaydedildi.`
  if (peak) insight += ` En yoğun ay: ${peak} (${peakCount} talep).`
  if (latestChange !== null && latestChange !== undefined) {
    const sign = latestChange >= 0 ? '+' : ''
    insight += ` Son aya göre değişim: ${sign}${latestChange}%.`
    if (latestChange > 20)       insight += ' Belirgin artış dikkat gerektiriyor.'
    else if (latestChange < -15) insight += ' Talep hacminde düşüş gözlemleniyor.'
    else                         insight += ' Hacim istikrarlı seyretmektedir.'
  }
  return insight
})

const priorityInsight = computed(() => {
  const d = data.value?.charts?.priorityStatusMatrix
  if (!d) return ''
  const critOpen = d.criticalOpen ?? d.CriticalOpen ?? 0
  const impOpen  = d.importantOpen ?? d.ImportantOpen ?? 0
  const totalOpen = d.totalOpen ?? d.TotalOpen ?? 0
  let insight = `Açık taleplerin dağılımı: toplam ${totalOpen} bekleyen talep mevcuttur.`
  if (critOpen > 0)
    insight += ` ⚠️ ${critOpen} adet KRİTİK öncelikli talep hâlâ açık durumda — acil müdahale gereklidir.`
  else
    insight += ' Kritik öncelikli açık talep bulunmamaktadır.'
  if (impOpen > 0) insight += ` ${impOpen} adet Önemli öncelikli talep işlem beklemektedir.`
  return insight
})

onMounted(() => { loadData(); window.addEventListener('resize', handleResize) })
onUnmounted(() => { disposeCharts(); window.removeEventListener('resize', handleResize) })
watch([isDark, () => i18n.lang], async () => { await nextTick(); renderCharts() })
</script>

<template>
  <div :class="embedded ? '' : 'min-h-screen bg-slate-50 dark:bg-slate-950 flex flex-col transition-colors duration-300'">

    <!-- HEADER (hidden when embedded) -->
    <header v-if="!embedded" class="h-16 bg-white dark:bg-slate-900 border-b border-slate-200 dark:border-slate-800 px-6 flex items-center justify-between sticky top-0 z-40 shadow-sm print:hidden no-print">
      <div class="flex items-center gap-3">
        <img :src="theme.dark ? '/logonight.png' : '/logo.png'" alt="Rimer Logo" class="h-10 w-auto object-contain cursor-pointer" @click="goHome" />
        <span v-if="auth.isAdmin || auth.isStaff" class="px-2 py-0.5 bg-blue-600 text-[9px] font-black text-white rounded uppercase">Admin</span>
        <span v-else-if="auth.isRector" class="px-2 py-0.5 bg-amber-600 text-[9px] font-black text-white rounded uppercase">Rektör</span>
        <div v-if="auth.isRector" class="ml-2 inline-flex items-center gap-1.5 px-2 py-0.5 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-full">
          <span class="w-1 h-1 rounded-full bg-amber-500 animate-pulse"></span>
          <span class="text-[9px] font-black text-amber-700 dark:text-amber-400 uppercase tracking-tight">Salt Okunur</span>
        </div>
      </div>
      <div class="flex items-center gap-3">
        <LangSelector />
        <button @click="theme.toggle()" class="w-9 h-9 rounded-lg flex items-center justify-center text-sm hover:bg-slate-100 dark:hover:bg-slate-800 transition">
          {{ isDark ? '☀️' : '🌙' }}
        </button>
        <div class="h-5 w-px bg-slate-200 dark:bg-slate-700"></div>
        <span class="text-xs font-semibold text-slate-600 dark:text-slate-400 hidden sm:block">{{ user.fullName || user.name || user.email }}</span>
        <button @click="doLogout" class="w-9 h-9 rounded-lg bg-red-50 dark:bg-red-900/20 text-red-500 flex items-center justify-center hover:bg-red-100 dark:hover:bg-red-900/40 transition">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"/></svg>
        </button>
      </div>
    </header>

    <main :class="embedded ? 'print:hidden no-print' : 'flex-1 p-4 md:p-6 max-w-7xl mx-auto w-full print:hidden no-print'">
      <AdminTabs v-if="!embedded" activeTab="analytics" class="print:hidden no-print" />

      <!-- FILTER BAR -->
      <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-4 mb-6 flex flex-wrap items-center gap-3 shadow-sm print:hidden no-print">
        <span class="text-xs font-bold text-slate-400 uppercase tracking-wider mr-2">Filter:</span>
        <button v-for="f in [{v:'all',l:i18n.t.filterAll},{v:'month',l:i18n.t.filterMonth},{v:'year',l:i18n.t.filterYear},{v:'custom',l:i18n.t.filterCustom}]" :key="f.v"
          @click="filterMode = f.v"
          :class="filterMode === f.v ? 'bg-indigo-600 text-white shadow-lg shadow-indigo-500/25' : 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-700'"
          class="px-4 py-2 rounded-xl text-xs font-bold transition-all duration-200">{{ f.l }}</button>
        <template v-if="filterMode === 'custom'">
          <input type="date" v-model="customStart" class="px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-sm text-slate-700 dark:text-slate-300" />
          <span class="text-slate-400 text-xs">→</span>
          <input type="date" v-model="customEnd" class="px-3 py-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800 text-sm text-slate-700 dark:text-slate-300" />
          <button @click="applyCustomFilter" class="px-4 py-2 rounded-xl bg-indigo-600 text-white text-xs font-bold hover:bg-indigo-700 transition">{{ i18n.t.apply }}</button>
        </template>

        <div class="flex-1"></div>
        
        <button v-if="auth.isAdmin || auth.isStaff || auth.isRector" @click="exportPdf" :disabled="exporting"
          class="px-5 py-2.5 rounded-xl bg-slate-900 dark:bg-indigo-600 text-white text-xs font-bold hover:bg-slate-800 dark:hover:bg-indigo-700 transition flex items-center gap-2 shadow-lg shadow-slate-200 dark:shadow-none disabled:opacity-50">
          <span v-if="exporting">⌛</span>
          <span v-else>📄</span>
          <span>{{ i18n.t.exportPdf }}</span>
        </button>
      </div>

      <!-- LOADING -->
      <template v-if="loading">
        <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
          <div v-for="i in 4" :key="i" class="h-28 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 animate-pulse"></div>
        </div>
        <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <div v-for="i in 4" :key="i" class="h-80 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 animate-pulse"></div>
        </div>
      </template>

      <!-- NO PERMISSIONS -->
      <div v-else-if="!data || !data.charts || Object.keys(data.charts).length === 0" class="flex flex-col items-center justify-center h-[50vh] text-center">
        <div class="w-20 h-20 rounded-full bg-slate-100 dark:bg-slate-800 flex items-center justify-center mb-4">
          <span class="text-4xl">🔒</span>
        </div>
        <h3 class="text-lg font-bold text-slate-700 dark:text-slate-300 mb-1">Erişim İzni Yok</h3>
        <p class="text-sm text-slate-400">Henüz size atanmış bir grafik bulunmuyor. Yöneticinizle iletişime geçin.</p>
      </div>

      <!-- CONTENT -->
      <template v-else>
        <!-- ROW 1 — KPI CARDS (only if totalTickets + statusSummary are available) -->
        <div v-if="has('totalTickets') || has('statusSummary')" class="grid grid-cols-2 md:grid-cols-5 gap-4 mb-6">
          <div v-if="has('totalTickets')" class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-5 shadow-sm hover:shadow-md transition-shadow">
            <div class="flex items-center gap-3 mb-3">
              <div class="w-10 h-10 rounded-xl bg-indigo-100 dark:bg-indigo-900/30 flex items-center justify-center"><span class="text-lg">📋</span></div>
              <span class="text-[11px] font-bold text-slate-400 uppercase tracking-wider">{{ i18n.t.statTotal }}</span>
            </div>
            <p class="text-3xl font-black text-slate-900 dark:text-white">{{ data.charts.totalTickets.total || data.charts.totalTickets.Total || 0 }}</p>
          </div>
          <template v-if="has('statusSummary')">
            <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-5 shadow-sm hover:shadow-md transition-shadow">
              <div class="flex items-center gap-3 mb-3">
                <div class="w-10 h-10 rounded-xl bg-emerald-100 dark:bg-emerald-900/30 flex items-center justify-center"><span class="text-lg">✅</span></div>
                <span class="text-[11px] font-bold text-slate-400 uppercase tracking-wider">{{ i18n.t.statResolved }}</span>
              </div>
              <p class="text-3xl font-black text-emerald-600">{{ data.charts.statusSummary.resolved || data.charts.statusSummary.Resolved || 0 }}</p>
            </div>
            <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-5 shadow-sm hover:shadow-md transition-shadow">
              <div class="flex items-center gap-3 mb-3">
                <div class="w-10 h-10 rounded-xl bg-amber-100 dark:bg-amber-900/30 flex items-center justify-center"><span class="text-lg">⏳</span></div>
                <span class="text-[11px] font-bold text-slate-400 uppercase tracking-wider">{{ i18n.t.statPending }}</span>
              </div>
              <p class="text-3xl font-black text-amber-500">{{ data.charts.statusSummary.pending || data.charts.statusSummary.Pending || 0 }}</p>
            </div>
            <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-5 shadow-sm hover:shadow-md transition-shadow">
              <div class="flex items-center gap-3 mb-3">
                <div class="w-10 h-10 rounded-xl bg-purple-100 dark:bg-purple-900/30 flex items-center justify-center"><span class="text-lg">⏲️</span></div>
                <span class="text-[11px] font-bold text-slate-400 uppercase tracking-wider">{{ i18n.t.statWaiting }}</span>
              </div>
              <p class="text-3xl font-black text-purple-500">{{ data.charts.statusSummary.waiting || data.charts.statusSummary.Waiting || 0 }}</p>
            </div>
            <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-5 shadow-sm hover:shadow-md transition-shadow">
              <div class="flex items-center gap-3 mb-3">
                <div class="w-10 h-10 rounded-xl bg-red-100 dark:bg-red-900/30 flex items-center justify-center"><span class="text-lg">🚨</span></div>
                <span class="text-[11px] font-bold text-slate-400 uppercase tracking-wider">{{ i18n.t.statUnanswered }}</span>
              </div>
              <p class="text-3xl font-black text-red-500">{{ data.charts.statusSummary.unanswered || data.charts.statusSummary.Unanswered || 0 }}</p>
            </div>
          </template>
        </div>

        <!-- ══ MAIN CHART GRID — 3 columns on XL, 2 on LG, 1 on mobile ══ -->
        <div class="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 gap-5 mb-6">

          <!-- Kategori Dağılımı -->
          <div v-if="has('categoryDistribution')" class="chart-card">
            <div class="chart-card-header">
              <span class="chart-card-icon">🍩</span>
              <h4 class="chart-card-title">{{ i18n.t.chartCategory }}</h4>
            </div>
            <div ref="categoryChartRef" class="w-full h-[260px]"></div>
            <div class="chart-insight">
              📌 Kategori dağılımı, kurumun aldığı talep profilini özetler. Şikayet oranı yüksekse süreç iyileştirmesi önceliklendirilmelidir.
            </div>
          </div>

          <!-- Durum Trendi -->
          <div v-if="has('statusSummary')" class="chart-card">
            <div class="chart-card-header">
              <span class="chart-card-icon">📈</span>
              <h4 class="chart-card-title">{{ i18n.t.chartTrend }}</h4>
            </div>
            <div ref="statusChartRef" class="w-full h-[260px]"></div>
            <div class="chart-insight">
              📌 Son 6 aylık durum trendi, sistemin kapasitesini ve tıkandığı noktaları gösterir. Bekleyen artışı takip edilmelidir.
            </div>
          </div>

          <!-- Departman Sıralaması -->
          <div v-if="has('departmentRanking')" class="chart-card">
            <div class="chart-card-header">
              <span class="chart-card-icon">🏢</span>
              <h4 class="chart-card-title">{{ i18n.t.chartDept }}</h4>
            </div>
            <div ref="deptChartRef" class="w-full h-[260px]"></div>
            <p v-if="!data.charts.departmentRanking?.length" class="text-center text-sm text-slate-400 py-12">{{ i18n.t.noDeptData }}</p>
            <div class="chart-insight">
              📌 En fazla talep alan birimler iş yükü açısından değerlendirilmeli; kaynak tahsisi buna göre planlanmalıdır.
            </div>
          </div>

          <!-- Yaşlandırma Analizi -->
          <div v-if="has('agingAnalysis')" class="chart-card">
            <div class="chart-card-header">
              <span class="chart-card-icon">⏳</span>
              <h4 class="chart-card-title">{{ i18n.t.chartAging }}</h4>
            </div>
            <div ref="agingChartRef" class="w-full h-[260px]"></div>
            <div v-if="criticalDepts.length" class="mt-3 p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-900/40 rounded-xl">
              <span class="text-red-600 dark:text-red-400 font-black text-[10px] uppercase tracking-wider">⚠️ Kritik Gecikme</span>
              <p class="text-xs text-slate-700 dark:text-slate-300 mt-1">
                180 gün sınırını geçen birimler: <span class="font-bold text-slate-900 dark:text-white">{{ criticalDepts.join(', ') }}</span>
              </p>
            </div>
            <div class="chart-insight">
              📌 45 günü aşan talepler acil takip gerektirir. 90+ gün askıda kalan talepler birimin müdahale kapasitesini sorgulatır.
            </div>
          </div>

          <!-- ── YENİ 1: Ortalama Yanıt Süresi ── -->
          <div v-if="has('averageResponseTime')" class="chart-card">
            <div class="chart-card-header">
              <span class="chart-card-icon">⏱️</span>
              <h4 class="chart-card-title">Ortalama Yanıt Süresi</h4>
              <span v-if="data.charts.averageResponseTime?.overallAvgDays" class="ml-auto text-xs font-black px-2 py-0.5 rounded-full"
                :class="data.charts.averageResponseTime.overallAvgDays <= 7 ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400'
                      : data.charts.averageResponseTime.overallAvgDays <= 30 ? 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400'
                      : 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400'">
                Ort. {{ data.charts.averageResponseTime.overallAvgDays }}g
              </span>
            </div>
            <div ref="avgResponseChartRef" class="w-full h-[260px]"></div>
            <div class="chart-insight">📌 {{ avgResponseInsight }}</div>
          </div>

          <!-- ── YENİ 2: Aylık Hacim Trendi ── -->
          <div v-if="has('monthlyVolumeTrend')" class="chart-card">
            <div class="chart-card-header">
              <span class="chart-card-icon">📊</span>
              <h4 class="chart-card-title">Aylık Hacim Trendi</h4>
              <span v-if="data.charts.monthlyVolumeTrend?.latestChange !== undefined && data.charts.monthlyVolumeTrend?.latestChange !== null"
                class="ml-auto text-xs font-black px-2 py-0.5 rounded-full"
                :class="data.charts.monthlyVolumeTrend.latestChange >= 0
                  ? 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400'
                  : 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400'">
                {{ data.charts.monthlyVolumeTrend.latestChange >= 0 ? '▲' : '▼' }}{{ Math.abs(data.charts.monthlyVolumeTrend.latestChange) }}%
              </span>
            </div>
            <div ref="monthlyVolumeChartRef" class="w-full h-[260px]"></div>
            <div class="chart-insight">📌 {{ monthlyVolumeInsight }}</div>
          </div>

          <!-- ── YENİ 3: Öncelik × Durum Matrisi ── -->
          <div v-if="has('priorityStatusMatrix')" class="chart-card"
            :class="data.charts.priorityStatusMatrix?.criticalOpen > 0 ? 'ring-2 ring-red-400/40 dark:ring-red-500/30' : ''">
            <div class="chart-card-header">
              <span class="chart-card-icon">⚡</span>
              <h4 class="chart-card-title">Öncelik × Durum Matrisi</h4>
              <span v-if="data.charts.priorityStatusMatrix?.criticalOpen > 0"
                class="ml-auto text-xs font-black px-2 py-0.5 rounded-full bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400 animate-pulse">
                {{ data.charts.priorityStatusMatrix.criticalOpen }} KRİTİK
              </span>
            </div>
            <div ref="priorityMatrixChartRef" class="w-full h-[260px]"></div>
            <div class="chart-insight">📌 {{ priorityInsight }}</div>
          </div>

          <!-- Memnuniyet (Teşekkür) -->
          <div v-if="has('satisfactionByDepartment')" class="chart-card">
            <div class="chart-card-header">
              <span class="chart-card-icon">🌟</span>
              <h4 class="chart-card-title">{{ i18n.t.chartSatisfaction || 'Memnuniyet (Teşekkür)' }}</h4>
            </div>
            <div ref="satisfactionChartRef" class="w-full h-[260px]"></div>
            <p v-if="!data.charts.satisfactionByDepartment?.length" class="text-center text-sm text-slate-400 py-12">{{ i18n.t.noDeptData }}</p>
            <div class="chart-insight">
              📌 Teşekkür içerikli talepler birim bazında memnuniyet göstergesidir. Yüksek alan birimler örnek alınabilir.
            </div>
          </div>

          <!-- Şikayet Dağılımı -->
          <div v-if="has('complaintByDepartment')" class="chart-card">
            <div class="chart-card-header">
              <span class="chart-card-icon">⚠️</span>
              <h4 class="chart-card-title">{{ i18n.t.chartComplaint || 'Şikayet Dağılımı' }}</h4>
            </div>
            <div ref="complaintChartRef" class="w-full h-[260px]"></div>
            <p v-if="!data.charts.complaintByDepartment?.length" class="text-center text-sm text-slate-400 py-12">{{ i18n.t.noDeptData }}</p>
            <div class="chart-insight">
              📌 Şikayet yoğunlaşmasının hangi birimde olduğu gözlemlenmeli; tekrar eden sorunlar için köklü çözüm üretilmelidir.
            </div>
          </div>

          <!-- Personel Türü Dağılımı -->
          <div v-if="has('staffTypeDistribution')" class="chart-card">
            <div class="chart-card-header">
              <span class="chart-card-icon">👥</span>
              <h4 class="chart-card-title">{{ i18n.t.chartStaffType || 'Personel Türü Dağılımı' }}</h4>
            </div>
            <div ref="staffTypeChartRef" class="w-full h-[260px]"></div>
            <p v-if="!data.charts.staffTypeDistribution?.length" class="text-center text-sm text-slate-400 py-12">Yeterli veri bulunamadı.</p>
            <div class="chart-insight">
              📌 Hangi personel grubunun daha fazla talep oluşturduğu, kurumsal iletişim stratejisini şekillendirmede yol göstericidir.
            </div>
          </div>

        </div><!-- end main chart grid -->


        <!-- ROW 4 — PERFORMANCE ANALYSIS -->
        <div v-if="has('departmentPerformance')" class="mt-6 space-y-4">
          <!-- Performance Scorecards -->
          <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-6 shadow-sm">
            <h4 class="text-sm font-bold text-slate-700 dark:text-slate-300 mb-4 uppercase tracking-wider flex items-center gap-2">
              <span class="w-1.5 h-5 rounded-full bg-gradient-to-b from-indigo-500 to-purple-500"></span>
              {{ i18n.t.chartPerformance }} — {{ i18n.t.perfScore }}
            </h4>
            <div class="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-3">
              <div v-for="dept in (data.charts.departmentPerformance || []).slice(0, 10)" :key="dept.name || dept.Name"
                class="relative p-4 rounded-xl border transition-all duration-300 hover:shadow-md"
                :class="{
                  'border-emerald-200 dark:border-emerald-800 bg-emerald-50/50 dark:bg-emerald-950/30': (dept.scoreColor || dept.ScoreColor) === 'green',
                  'border-amber-200 dark:border-amber-800 bg-amber-50/50 dark:bg-amber-950/30': (dept.scoreColor || dept.ScoreColor) === 'yellow',
                  'border-orange-200 dark:border-orange-800 bg-orange-50/50 dark:bg-orange-950/30': (dept.scoreColor || dept.ScoreColor) === 'orange',
                  'border-red-200 dark:border-red-800 bg-red-50/50 dark:bg-red-950/30': (dept.scoreColor || dept.ScoreColor) === 'red'
                }">
                <!-- Score badge -->
                <div class="flex items-start justify-between mb-2">
                  <p class="text-[10px] font-bold text-slate-500 dark:text-slate-400 truncate max-w-[80%]">{{ dept.name || dept.Name }}</p>
                  <span class="text-[9px] font-black px-1.5 py-0.5 rounded-full"
                    :class="{
                      'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/50 dark:text-emerald-400': (dept.scoreColor || dept.ScoreColor) === 'green',
                      'bg-amber-100 text-amber-700 dark:bg-amber-900/50 dark:text-amber-400': (dept.scoreColor || dept.ScoreColor) === 'yellow',
                      'bg-orange-100 text-orange-700 dark:bg-orange-900/50 dark:text-orange-400': (dept.scoreColor || dept.ScoreColor) === 'orange',
                      'bg-red-100 text-red-700 dark:bg-red-900/50 dark:text-red-400': (dept.scoreColor || dept.ScoreColor) === 'red'
                    }">{{ dept.scoreLabel || dept.ScoreLabel }}</span>
                </div>
                <!-- Score number -->
                <p class="text-2xl font-black mb-2"
                  :class="{
                    'text-emerald-600 dark:text-emerald-400': (dept.scoreColor || dept.ScoreColor) === 'green',
                    'text-amber-600 dark:text-amber-400': (dept.scoreColor || dept.ScoreColor) === 'yellow',
                    'text-orange-600 dark:text-orange-400': (dept.scoreColor || dept.ScoreColor) === 'orange',
                    'text-red-600 dark:text-red-400': (dept.scoreColor || dept.ScoreColor) === 'red'
                  }">{{ dept.performanceScore || dept.PerformanceScore }}<span class="text-xs font-bold text-slate-400">/100</span></p>
                <!-- Mini metrics -->
                <div class="grid grid-cols-2 gap-1 text-[9px]">
                  <div class="flex items-center gap-1">
                    <span class="w-1.5 h-1.5 rounded-full bg-emerald-500"></span>
                    <span class="text-slate-500 dark:text-slate-400">{{ i18n.t.perfResRate }}: <b class="text-slate-700 dark:text-slate-300">{{ dept.resolutionRate || dept.ResolutionRate }}%</b></span>
                  </div>
                  <div class="flex items-center gap-1">
                    <span class="w-1.5 h-1.5 rounded-full bg-amber-500"></span>
                    <span class="text-slate-500 dark:text-slate-400">{{ i18n.t.perfOverdue }}: <b class="text-slate-700 dark:text-slate-300">{{ dept.overdueTickets || dept.OverdueTickets || 0 }}</b></span>
                  </div>
                  <div class="flex items-center gap-1">
                    <span class="w-1.5 h-1.5 rounded-full bg-red-500"></span>
                    <span class="text-slate-500 dark:text-slate-400">{{ i18n.t.perfCritical }}: <b class="text-slate-700 dark:text-slate-300">{{ dept.criticalTickets || dept.CriticalTickets || 0 }}</b></span>
                  </div>
                  <div class="flex items-center gap-1">
                    <span class="w-1.5 h-1.5 rounded-full bg-indigo-500"></span>
                    <span class="text-slate-500 dark:text-slate-400">{{ i18n.t.perfTransfer }}: <b class="text-slate-700 dark:text-slate-300">{{ dept.transferRate || dept.TransferRate || 0 }}%</b></span>
                  </div>
                </div>
              </div>
            </div>
          </div>
          <!-- Radar Chart -->
          <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-6 shadow-sm">
            <h4 class="text-sm font-bold text-slate-700 dark:text-slate-300 mb-4 uppercase tracking-wider flex items-center gap-2">
              <span class="w-1.5 h-5 rounded-full bg-gradient-to-b from-purple-500 to-pink-500"></span>
              {{ i18n.t.chartPerformance }} — Radar
            </h4>
            <div ref="performanceChartRef" class="w-full h-[400px]"></div>
          </div>
        </div>

        <!-- ROW 5 — SATISFACTION + COMPLAINT BY DEPARTMENT -->
        <div v-if="has('satisfactionByDepartment') || has('complaintByDepartment')" class="grid grid-cols-1 lg:grid-cols-2 gap-6 mt-6">
          <div v-if="has('satisfactionByDepartment')" class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-6 shadow-sm">
            <h4 class="text-sm font-bold text-slate-700 dark:text-slate-300 mb-4 uppercase tracking-wider flex items-center gap-2">
              <span class="w-1.5 h-5 rounded-full bg-gradient-to-b from-emerald-400 to-green-600"></span>
              {{ i18n.t.chartSatisfaction }}
            </h4>
            <div ref="satisfactionChartRef" class="w-full h-[300px]"></div>
            <p v-if="!data.charts.satisfactionByDepartment?.length" class="text-center text-sm text-slate-400 py-12">{{ i18n.t.noDeptData }}</p>
          </div>
          <div v-if="has('complaintByDepartment')" class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-6 shadow-sm">
            <h4 class="text-sm font-bold text-slate-700 dark:text-slate-300 mb-4 uppercase tracking-wider flex items-center gap-2">
              <span class="w-1.5 h-5 rounded-full bg-gradient-to-b from-red-400 to-red-700"></span>
              {{ i18n.t.chartComplaint }}
            </h4>
            <div ref="complaintChartRef" class="w-full h-[300px]"></div>
            <p v-if="!data.charts.complaintByDepartment?.length" class="text-center text-sm text-slate-400 py-12">{{ i18n.t.noDeptData }}</p>
          </div>
        </div>

        <!-- ROW 6 — STAFF TYPE DISTRIBUTION -->
        <div v-if="has('staffTypeDistribution')" class="mt-6">
          <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 p-6 shadow-sm">
            <h4 class="text-sm font-bold text-slate-700 dark:text-slate-300 mb-4 uppercase tracking-wider flex items-center gap-2">
              <span class="w-1.5 h-5 rounded-full bg-gradient-to-b from-blue-400 to-indigo-600"></span>
              {{ i18n.t.chartStaffType }}
            </h4>
            <div ref="staffTypeChartRef" class="w-full h-[350px]"></div>
          </div>
        </div>

      </template>
    </main>

    <footer class="py-3 text-center text-[10px] text-slate-400 dark:text-slate-600 border-t border-slate-100 dark:border-slate-800">
      &copy; 2026 RİMER İletişim Merkezi — Executive Analytics
    </footer>

    <!-- PDF REPORT TEMPLATE (Visible only during print) -->
    <div v-if="data && data.charts" class="print-only">
      <div id="pdf-report-content" class="p-4 bg-white text-slate-900 w-full font-sans">
        <!-- HEADER -->
        <div class="flex justify-between items-start border-b-2 border-indigo-600 pb-6 mb-8">
          <div class="flex items-center gap-4">
            <img :src="theme.dark ? '/logonight.png' : '/logo.png'" alt="Rimer Logo" class="w-16 h-16 object-contain" />
            <div>
              <h1 class="text-3xl font-black text-indigo-600 tracking-tighter">RİMER</h1>
              <p class="text-[10px] font-bold text-slate-400 uppercase tracking-widest">Rektörlük İletişim Merkezi</p>
            </div>
          </div>
          <div class="text-right">
            <h2 class="text-xl font-bold text-slate-800">{{ i18n.t.pdfReportTitle }}</h2>
            <p class="text-xs text-slate-500 mt-1">{{ new Date().toLocaleString(i18n.lang === 'tr' ? 'tr-TR' : 'en-US') }}</p>
            <div class="mt-2 inline-block px-2 py-1 bg-indigo-50 text-indigo-700 text-[9px] font-bold rounded uppercase">{{ i18n.t.pdfOfficialDoc }}</div>
          </div>
        </div>

        <!-- SUMMARY CARDS -->
        <div class="grid grid-cols-5 gap-2 mb-10">
          <div class="p-3 bg-slate-50 border border-slate-200 rounded-xl" v-if="has('totalTickets')">
            <p class="text-[8px] font-bold text-slate-400 uppercase mb-1">{{ i18n.t.statTotal }}</p>
            <p class="text-xl font-black text-slate-900">{{ data?.charts?.totalTickets?.total || data?.charts?.totalTickets?.Total || 0 }}</p>
          </div>
          <div class="p-3 bg-emerald-50 border border-emerald-100 rounded-xl" v-if="has('statusSummary')">
            <p class="text-[8px] font-bold text-emerald-600 uppercase mb-1">{{ i18n.t.statResolved }}</p>
            <p class="text-xl font-black text-emerald-700">{{ data?.charts?.statusSummary?.resolved || data?.charts?.statusSummary?.Resolved || 0 }}</p>
          </div>
          <div class="p-3 bg-amber-50 border border-amber-100 rounded-xl" v-if="has('statusSummary')">
            <p class="text-[8px] font-bold text-amber-600 uppercase mb-1">{{ i18n.t.statPending }}</p>
            <p class="text-xl font-black text-amber-700">{{ data?.charts?.statusSummary?.pending || data?.charts?.statusSummary?.Pending || 0 }}</p>
          </div>
          <div class="p-3 bg-purple-50 border border-purple-100 rounded-xl" v-if="has('statusSummary')">
            <p class="text-[8px] font-bold text-purple-600 uppercase mb-1">{{ i18n.t.statWaiting }}</p>
            <p class="text-xl font-black text-purple-700">{{ data?.charts?.statusSummary?.waiting || data?.charts?.statusSummary?.Waiting || 0 }}</p>
          </div>
          <div class="p-3 bg-red-50 border border-red-100 rounded-xl" v-if="has('statusSummary')">
            <p class="text-[8px] font-bold text-red-600 uppercase mb-1">{{ i18n.t.statUnanswered }}</p>
            <p class="text-xl font-black text-red-700">{{ data?.charts?.statusSummary?.unanswered || data?.charts?.statusSummary?.Unanswered || 0 }}</p>
          </div>
        </div>

        <!-- DATA TABLES & CHARTS GRID (2 per row) -->
        <div class="grid grid-cols-2 gap-x-8 gap-y-10 mb-10">
          <!-- Category Section -->
          <div v-if="has('categoryDistribution')">
            <h3 class="text-[11px] font-bold text-slate-800 uppercase border-l-4 border-indigo-500 pl-2 mb-3">{{ i18n.t.chartCategory }}</h3>
            <div class="bg-slate-50 p-3 rounded-xl border border-slate-100 flex items-center justify-center mb-2 h-[200px]">
              <img v-if="chartImages.cat" :src="chartImages.cat" class="max-w-full max-h-full object-contain" />
            </div>
            <p class="text-[9px] text-slate-500 leading-tight italic px-1 text-justify">
              * {{ i18n.lang === 'tr' ? 'Sisteme iletilen taleplerin kategorik dağılım oranlarını ifade eder. Kurumsal odak alanlarının belirlenmesinde ve hizmet stratejilerinin geliştirilmesinde temel veri kaynağıdır.' : 'Indicates the categorical distribution rates of requests submitted to the system. Serves as a primary data source for determining institutional focus areas and developing service strategies.' }}
            </p>
          </div>

          <!-- Trend Section -->
          <div v-if="has('statusSummary')">
            <h3 class="text-[11px] font-bold text-slate-800 uppercase border-l-4 border-indigo-500 pl-2 mb-3">{{ i18n.t.chartTrend }}</h3>
            <div class="bg-slate-50 p-3 rounded-xl border border-slate-100 flex items-center justify-center mb-2 h-[200px]">
              <img v-if="chartImages.trend" :src="chartImages.trend" class="max-w-full max-h-full object-contain" />
            </div>
            <p class="text-[9px] text-slate-500 leading-tight italic px-1 text-justify">
              * {{ i18n.lang === 'tr' ? 'Son 6 aylık periyotta kurumun genel iş yükü yönetim performansını ve çözüme kavuşturma eğilimlerini kronolojik olarak özetler. Dönemsel krizlerin ve verimlilik artışlarının tespiti için kullanılır.' : 'Chronologically summarizes the institution\'s overall workload management performance and resolution trends over the last 6 months. Used to identify periodic crises and productivity increases.' }}
            </p>
          </div>

          <!-- Dept Ranking -->
          <div v-if="has('departmentRanking')">
            <h3 class="text-[11px] font-bold text-slate-800 uppercase border-l-4 border-indigo-500 pl-2 mb-3">{{ i18n.t.chartDept }}</h3>
            <div class="bg-slate-50 p-3 rounded-xl border border-slate-100 flex items-center justify-center mb-2 h-[200px]">
              <img v-if="chartImages.dept" :src="chartImages.dept" class="max-w-full max-h-full object-contain" />
            </div>
            <p class="text-[9px] text-slate-500 leading-tight italic px-1 text-justify">
              * {{ i18n.lang === 'tr' ? 'Birimlerin işlem hacimlerine göre yoğunluk sıralamasını kantitatif olarak ortaya koyar. İnsan kaynakları dağılımı ve norm kadro optimizasyonu için kritik bir referans göstergesidir.' : 'Quantitatively reveals the intensity ranking of units based on their transaction volumes. A critical reference indicator for human resources distribution and staff optimization.' }}
            </p>
          </div>

          <div v-if="has('agingAnalysis')">
            <h3 class="text-[11px] font-bold text-slate-800 uppercase border-l-4 border-indigo-500 pl-2 mb-3">{{ i18n.t.chartAging }}</h3>
            <div class="bg-slate-50 p-3 rounded-xl border border-slate-100 flex items-center justify-center mb-2 h-[200px]">
              <img v-if="chartImages.aging" :src="chartImages.aging" class="max-w-full max-h-full object-contain" />
            </div>
            <p class="text-[9px] text-slate-500 leading-tight italic px-1 mb-2 text-justify">
              * {{ i18n.lang === 'tr' ? 'Çözümü gecikmiş başvuruların yaşlandırma periyotlarına göre birikim analizini sunar. Kurumsal risk yönetimi, hizmet standartı ihlalleri ve SLA (Hizmet Seviyesi Anlaşması) takibi açısından stratejik öneme sahiptir.' : 'Presents the accumulation analysis of delayed applications according to aging periods. Strategically important for institutional risk management, service standard violations, and SLA tracking.' }}
            </p>
            
            <!-- Critical Dept Warning -->
            <div v-if="criticalDepts.length" class="mt-2 p-2 bg-red-50 border border-red-100 rounded-lg">
              <p class="text-[8px] font-bold text-red-600 uppercase mb-1">⚠️ {{ i18n.lang === 'tr' ? 'KRİTİK GECİKME UYARISI' : 'CRITICAL DELAY WARNING' }}</p>
              <p class="text-[9px] text-slate-700 leading-tight">
                <span class="text-red-600 font-bold underline">180 {{ i18n.lang === 'tr' ? 'gün' : 'days' }}</span> 
                {{ i18n.lang === 'tr' ? 'sınırını geçen birimler:' : 'exceeded by:' }}
                <span class="font-bold">{{ criticalDepts.join(', ') }}</span>
              </p>
            </div>
          </div>
        </div>


        <!-- ADDITIONAL CHARTS (Flow normally without forced page break) -->
        <div class="pt-6">
          <div class="grid grid-cols-2 gap-x-8 gap-y-10 mb-10">
            <!-- Satisfaction -->
            <div v-if="has('satisfactionByDepartment')" class="break-inside-avoid">
              <h3 class="text-[11px] font-bold text-slate-800 uppercase border-l-4 border-emerald-500 pl-2 mb-3">{{ i18n.t.chartSatisfaction }}</h3>
              <div class="bg-slate-50 p-3 rounded-xl border border-slate-100 flex items-center justify-center mb-2 h-[200px]">
                <img v-if="chartImages.sat" :src="chartImages.sat" class="max-w-full max-h-full object-contain" />
              </div>
              <p class="text-[9px] text-slate-500 leading-tight italic px-1 text-justify mt-2">
                * {{ i18n.lang === 'tr' ? 'Bu grafik, kurumsal hizmetlerin yararlanıcılar nezdinde yarattığı pozitif etkiyi ve birimlerin paydaş memnuniyetini sağlama kapasitelerini yansıtır. Yüksek değerler, süreç optimizasyonunun ve hizmet kalitesinin etkinliğini işaret eder.' : 'This chart reflects the positive impact of institutional services on beneficiaries and the capacity of units to ensure stakeholder satisfaction. High values indicate the effectiveness of process optimization and service quality.' }}
              </p>
            </div>

            <!-- Complaint -->
            <div v-if="has('complaintByDepartment')" class="break-inside-avoid">
              <h3 class="text-[11px] font-bold text-slate-800 uppercase border-l-4 border-red-500 pl-2 mb-3">{{ i18n.t.chartComplaint }}</h3>
              <div class="bg-slate-50 p-3 rounded-xl border border-slate-100 flex items-center justify-center mb-2 h-[200px]">
                <img v-if="chartImages.comp" :src="chartImages.comp" class="max-w-full max-h-full object-contain" />
              </div>
              <p class="text-[9px] text-slate-500 leading-tight italic px-1 text-justify mt-2">
                * {{ i18n.lang === 'tr' ? 'Birim bazlı operasyonel tıkanıklıkları ve hizmet sunumundaki potansiyel aksaklık alanlarını nicel olarak gösterir. Bu veriler, sürekli iyileştirme politikaları ve süreç re-organizasyonu için kritik bir referans niteliğindedir.' : 'Quantitatively demonstrates unit-based operational bottlenecks and potential disruption areas in service delivery. This data serves as a critical reference for continuous improvement policies and process re-organization.' }}
              </p>
            </div>

            <!-- Staff Type -->
            <div v-if="has('staffTypeDistribution')" class="break-inside-avoid">
              <h3 class="text-[11px] font-bold text-slate-800 uppercase border-l-4 border-blue-500 pl-2 mb-3">{{ i18n.t.chartStaffType }}</h3>
              <div class="bg-slate-50 p-3 rounded-xl border border-slate-100 flex items-center justify-center mb-2 h-[200px]">
                <img v-if="chartImages.staff" :src="chartImages.staff" class="max-w-full max-h-full object-contain" />
              </div>
              <p class="text-[9px] text-slate-500 leading-tight italic px-1 text-justify mt-2">
                * {{ i18n.lang === 'tr' ? 'Farklı personel kategorilerinden gelen bildirimlerin yapısal dağılımını analiz eder. Kurum içi iletişim dinamiklerinin, idari şeffaflığın ve hiyerarşik geri bildirim mekanizmalarının etkinliğinin ölçülmesini sağlar.' : 'Analyzes the structural distribution of feedback across different personnel categories. Enables the measurement of internal communication dynamics, transparency, and hierarchical feedback mechanisms.' }}
              </p>
            </div>

            <!-- ── YENİ: Ortalama Yanıt Süresi ── -->
            <div v-if="has('averageResponseTime')" class="break-inside-avoid">
              <h3 class="text-[11px] font-bold text-slate-800 uppercase border-l-4 border-teal-500 pl-2 mb-3">Ortalama Yanıt Süresi</h3>
              <div class="bg-slate-50 p-3 rounded-xl border border-slate-100 flex items-center justify-center mb-2 h-[200px]">
                <img v-if="chartImages.avgResp" :src="chartImages.avgResp" class="max-w-full max-h-full object-contain" />
              </div>
              <p class="text-[9px] text-slate-500 leading-tight italic px-1 text-justify mt-2">* {{ avgResponseInsight }}</p>
            </div>

            <!-- ── YENİ: Aylık Hacim Trendi ── -->
            <div v-if="has('monthlyVolumeTrend')" class="break-inside-avoid">
              <h3 class="text-[11px] font-bold text-slate-800 uppercase border-l-4 border-indigo-500 pl-2 mb-3">Aylık Hacim Trendi (12 Ay)</h3>
              <div class="bg-slate-50 p-3 rounded-xl border border-slate-100 flex items-center justify-center mb-2 h-[200px]">
                <img v-if="chartImages.monthly" :src="chartImages.monthly" class="max-w-full max-h-full object-contain" />
              </div>
              <p class="text-[9px] text-slate-500 leading-tight italic px-1 text-justify mt-2">* {{ monthlyVolumeInsight }}</p>
            </div>

            <!-- ── YENİ: Öncelik × Durum Matrisi ── -->
            <div v-if="has('priorityStatusMatrix')" class="break-inside-avoid">
              <h3 class="text-[11px] font-bold text-slate-800 uppercase border-l-4 border-red-500 pl-2 mb-3">Öncelik × Durum Matrisi</h3>
              <div class="bg-slate-50 p-3 rounded-xl border border-slate-100 flex items-center justify-center mb-2 h-[200px]">
                <img v-if="chartImages.priMatrix" :src="chartImages.priMatrix" class="max-w-full max-h-full object-contain" />
              </div>
              <p class="text-[9px] text-slate-500 leading-tight italic px-1 text-justify mt-2">* {{ priorityInsight }}</p>
            </div>

          </div>
        </div>

        <!-- TABLES SECTION (Flow normally) -->
        <div class="pt-6">
          <!-- CATEGORY TABLE (Compact) -->
          <div v-if="has('categoryDistribution')" class="mb-12 break-inside-avoid">
          <h3 class="text-[11px] font-bold text-slate-800 uppercase border-l-4 border-indigo-500 pl-2 mb-3">{{ i18n.t.chartCategory }}</h3>
          <table class="w-full text-left text-[9px] border-collapse">
            <thead>
              <tr class="bg-slate-100 text-slate-600">
                <th v-for="cat in data.charts.categoryDistribution" :key="cat.name" class="p-2 border border-slate-200 text-center font-bold">
                  {{ catLabels[cat.name || cat.Name] || (cat.name || cat.Name) }}
                </th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td v-for="cat in data.charts.categoryDistribution" :key="cat.name" class="p-2 border border-slate-200 text-center text-slate-700 font-bold">
                  {{ cat.count || cat.Count }} ({{ ((cat.count || cat.Count) / (data.charts.totalTickets.total || data.charts.totalTickets.Total || 1) * 100).toFixed(1) }}%)
                </td>
              </tr>
            </tbody>
          </table>
          <p class="text-[9px] text-slate-500 leading-tight italic px-1 mt-2 text-justify">
            * {{ i18n.lang === 'tr' ? 'Kategori bazlı sayısal hacimler ve toplam içerisindeki yüzdelik paylar tablosal olarak verilmiştir. Bu tablo, spesifik hizmet alanlarındaki talep baskısını detaylandırmaktadır.' : 'Category-based numerical volumes and percentage shares within the total are provided in tabular form. This table details the demand pressure in specific service areas.' }}
          </p>
        </div>

        <!-- DEPARTMENT TABLE (Compact) -->
        <div v-if="has('departmentRanking')" class="mb-10 break-inside-avoid">
          <h3 class="text-[11px] font-bold text-slate-800 uppercase border-l-4 border-indigo-500 pl-2 mb-3">{{ i18n.t.chartDept }}</h3>
          <table class="w-full text-left text-[9px] border-collapse">
            <thead>
              <tr class="bg-slate-100 text-slate-600">
                <th class="p-2 border border-slate-200 font-bold">{{ i18n.t.department }}</th>
                <th class="p-2 border border-slate-200 text-center font-bold">{{ i18n.t.statTotal }}</th>
                <th class="p-2 border border-slate-200 text-center font-bold">%</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="dept in data.charts.departmentRanking.slice(0, 10)" :key="dept.name" class="border-b border-slate-100">
                <td class="p-2 font-medium text-slate-700">{{ dept.name || dept.Name }}</td>
                <td class="p-2 text-center text-slate-600 font-bold">{{ dept.count || dept.Count }}</td>
                <td class="p-2 text-center text-indigo-600 font-bold">
                  {{ ((dept.count || dept.Count) / (data.charts.totalTickets.total || data.charts.totalTickets.Total || 1) * 100).toFixed(1) }}%
                </td>
              </tr>
            </tbody>
          </table>
          <p class="text-[9px] text-slate-500 leading-tight italic px-1 mt-2 text-justify">
            * {{ i18n.lang === 'tr' ? 'En yüksek iş yüküne sahip ilk 10 birimin toplam başvuru havuzundaki oransal karşılıkları listelenmiştir. Kurumsal yükün uç noktalardaki birikimini gösterir.' : 'The proportional equivalents of the top 10 units with the highest workload in the total application pool are listed. Shows the accumulation of institutional load at extreme points.' }}
          </p>
        </div>

        <!-- PERFORMANCE RADAR & TABLE (Page Break) -->
        <div v-if="has('departmentPerformance')" class="mb-10 page-break-before pt-10">
          <h3 class="text-[11px] font-bold text-slate-800 uppercase border-l-4 border-amber-500 pl-2 mb-3">{{ i18n.t.chartPerformance }}</h3>
          
          <div class="bg-slate-50 p-4 rounded-xl border border-slate-100 flex items-center justify-center mb-4 h-[250px]">
            <img v-if="chartImages.perf" :src="chartImages.perf" class="max-w-full max-h-full object-contain" />
          </div>
          <p class="text-[9px] text-slate-500 leading-tight italic px-1 mb-6 text-justify">
            * {{ i18n.lang === 'tr' ? 'Birimlerin çözüm hızı, bekleyen iş yükü, zaman aşımı ve transfer oranları gibi çok boyutlu performans metriklerini kompozit bir skor üzerinden değerlendirir. Radar grafiği yetkinlik dengesini görselleştirirken, tablo detaylı skor dökümünü sunar.' : 'Evaluates multidimensional performance metrics of units such as resolution speed, pending workload, timeout, and transfer rates over a composite score. The radar chart visualizes the competence balance, while the table provides a detailed score breakdown.' }}
          </p>

          <table class="w-full text-left text-[9px] border-collapse">
            <thead>
              <tr class="bg-slate-100 text-slate-600">
                <th class="p-2 border border-slate-200 font-bold">{{ i18n.t.department }}</th>
                <th class="p-2 border border-slate-200 text-center font-bold">{{ i18n.t.perfScore }}</th>
                <th class="p-2 border border-slate-200 text-center font-bold">{{ i18n.t.statTotal }}</th>
                <th class="p-2 border border-slate-200 text-center font-bold">{{ i18n.t.perfResRate }}</th>
                <th class="p-2 border border-slate-200 text-center font-bold">{{ i18n.t.perfOverdue }}</th>
                <th class="p-2 border border-slate-200 text-center font-bold">{{ i18n.t.perfCritical }}</th>
                <th class="p-2 border border-slate-200 text-center font-bold">{{ i18n.t.perfTransfer }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="dept in data.charts.departmentPerformance.slice(0, 10)" :key="dept.name" class="border-b border-slate-100">
                <td class="p-2 font-medium text-slate-700">{{ dept.name || dept.Name }}</td>
                <td class="p-2 text-center font-black" :class="{'text-emerald-600': (dept.scoreColor||dept.ScoreColor)==='green', 'text-amber-600': (dept.scoreColor||dept.ScoreColor)==='yellow', 'text-orange-600': (dept.scoreColor||dept.ScoreColor)==='orange', 'text-red-600': (dept.scoreColor||dept.ScoreColor)==='red'}">{{ dept.performanceScore || dept.PerformanceScore }}</td>
                <td class="p-2 text-center text-slate-600 font-bold">{{ dept.totalTickets || dept.TotalTickets }}</td>
                <td class="p-2 text-center text-emerald-600 font-bold">{{ dept.resolutionRate || dept.ResolutionRate }}%</td>
                <td class="p-2 text-center text-amber-600 font-bold">{{ dept.overdueTickets || dept.OverdueTickets || 0 }}</td>
                <td class="p-2 text-center text-red-600 font-bold">{{ dept.criticalTickets || dept.CriticalTickets || 0 }}</td>
                <td class="p-2 text-center text-indigo-600 font-bold">{{ dept.transferRate || dept.TransferRate || 0 }}%</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- FOOTER -->
        <div class="mt-20 pt-6 border-t border-slate-200 text-center">
          <p class="text-[10px] text-slate-400 leading-relaxed">
            Bu rapor RİMER İletişim Merkezi Analitik Sistemi tarafından otomatik olarak oluşturulmuştur.<br>
            Veriler {{ filterMode === 'all' ? 'Tüm Zamanlar' : filterMode }} filtresine göre düzenlenmiştir.<br>
            &copy; 2026 RİMER — T.C. Üniversite Rektörlüğü
          </p>
        </div>
      </div>
    </div>
  </div>
</template>

<style>
@media print {
  /* Hide EVERYTHING globally except the report */
  header, footer, aside, nav, .fixed, .sticky, .no-print,
  #accessibility-controls, .toast-container, .admin-sidebar,
  [id^="headlessui"], [class*="accessibility"], [class*="toast"] { 
    display: none !important; 
  }
  
  body, #app { 
    background: white !important; 
    color: black !important;
    padding: 0 !important;
    margin: 0 !important;
  }

  /* Show only the report */
  .print-only { 
    display: block !important; 
    position: relative !important;
    width: 100% !important;
    max-width: 100% !important;
    height: auto !important;
    background: white !important;
    margin: 0 !important;
    padding: 0 !important;
  }
  
  #pdf-report-content {
    width: 100% !important;
    padding: 0 !important;
    margin: 0 !important;
  }

  .bg-white { background-color: white !important; }
  .text-slate-900 { color: #0f172a !important; }
  .text-indigo-600 { color: #4f46e5 !important; }
  .page-break-before { page-break-before: always; }

  /* Print chart insight */
  .chart-insight {
    background: #f8fafc !important;
    color: #475569 !important;
    border-left-color: #6366f1 !important;
    -webkit-print-color-adjust: exact;
    print-color-adjust: exact;
  }

  @page {
    size: A4;
    margin: 10mm;
  }
}
</style>

<style scoped>
* { transition: background-color 0.3s ease, border-color 0.3s ease, color 0.2s ease; }

/* ── Chart Card ──────────────────────────────────────────────────── */
.chart-card {
  background: white;
  border-radius: 16px;
  border: 1px solid #e2e8f0;
  padding: 20px;
  box-shadow: 0 1px 3px rgba(0,0,0,0.06);
  display: flex;
  flex-direction: column;
  gap: 0;
  transition: box-shadow 0.2s ease;
}
.chart-card:hover { box-shadow: 0 4px 16px rgba(0,0,0,0.08); }

:deep(.dark) .chart-card,
.dark .chart-card {
  background: #0f172a;
  border-color: #1e293b;
}

.chart-card-header {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 12px;
}
.chart-card-icon {
  font-size: 16px;
  line-height: 1;
  flex-shrink: 0;
}
.chart-card-title {
  font-size: 11px;
  font-weight: 800;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  color: #475569;
}
:is(.dark *) .chart-card-title { color: #94a3b8; }

/* ── Chart Insight (analysis text below chart) ───────────────────── */
.chart-insight {
  margin-top: 12px;
  padding: 10px 12px;
  background: #f8fafc;
  border-left: 3px solid #6366f1;
  border-radius: 0 8px 8px 0;
  font-size: 11px;
  line-height: 1.6;
  color: #475569;
  font-style: italic;
}
:is(.dark *) .chart-insight {
  background: rgba(99,102,241,0.06);
  color: #94a3b8;
  border-left-color: rgba(99,102,241,0.5);
}

@media screen {
  .print-only { display: none; }
}

</style>
