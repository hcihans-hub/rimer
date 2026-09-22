<script setup>
import { onMounted, onUnmounted, ref, watch } from 'vue'
import * as echarts from 'echarts'

const props = defineProps({
  chartData:   { type: Array,  default: () => [] },
  chartLabels: { type: Array,  default: () => [] },
  lineColor:   { type: String, default: '#3b82f6' }
})

const chartRef = ref(null)
let chart = null

// Build the full option object — always explicit series type
const buildOption = (data, labels) => ({
  backgroundColor: 'transparent',
  animation: true,
  animationDuration: 400,
  animationEasing: 'cubicOut',
  tooltip: {
    trigger: 'axis',
    backgroundColor: 'rgba(10,15,24,0.95)',
    borderColor: 'rgba(255,255,255,0.08)',
    borderWidth: 1,
    padding: [10, 14],
    textStyle: { color: '#f8fafc', fontSize: 12, fontFamily: 'monospace' },
    axisPointer: { lineStyle: { color: 'rgba(255,255,255,0.1)', width: 1, type: 'dashed' } }
  },
  grid: { left: '2%', right: '2%', bottom: '4%', top: '8%', containLabel: true },
  xAxis: {
    type: 'category',
    data: labels,
    boundaryGap: false,
    axisLine: { show: false },
    axisTick: { show: false },
    axisLabel: { color: '#475569', fontSize: 10, margin: 14, fontFamily: 'monospace' }
  },
  yAxis: {
    type: 'value',
    splitLine: { lineStyle: { color: 'rgba(255,255,255,0.04)', type: 'dashed' } },
    axisLine: { show: false },
    axisLabel: { color: '#475569', fontSize: 10, fontFamily: 'monospace' }
  },
  series: [
    {
      // type MUST always be declared — prevents "Unknown series undefined"
      type: 'line',
      data: data,
      smooth: 0.5,
      showSymbol: data.length > 0,
      symbol: 'circle',
      symbolSize: (_, params) => params.dataIndex === data.length - 1 ? 7 : 0,
      lineStyle: { width: 2.5, color: props.lineColor },
      itemStyle: { color: props.lineColor, borderColor: '#0a0f18', borderWidth: 2 },
      areaStyle: {
        color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
          { offset: 0,   color: `${props.lineColor}30` },
          { offset: 0.6, color: `${props.lineColor}10` },
          { offset: 1,   color: `${props.lineColor}00` }
        ])
      }
    }
  ]
})

const initChart = () => {
  if (!chartRef.value) return
  if (chart) { chart.dispose(); chart = null }
  chart = echarts.init(chartRef.value, null, { renderer: 'canvas' })
  // Always init with full option including type
  chart.setOption(buildOption(props.chartData, props.chartLabels))
}

// Data update — never dispose/recreate, only patch data+labels
const updateChart = () => {
  if (!chart) return
  chart.setOption({
    xAxis: { data: props.chartLabels },
    series: [{
      // MUST include type on update to prevent "Unknown series" warning
      type: 'line',
      data: props.chartData,
      showSymbol: props.chartData.length > 0,
      symbolSize: (_, params) => params.dataIndex === props.chartData.length - 1 ? 7 : 0,
    }]
  }, { notMerge: false })
}

const resizeHandler = () => chart?.resize()

onMounted(() => {
  initChart()
  window.addEventListener('resize', resizeHandler)
})

onUnmounted(() => {
  window.removeEventListener('resize', resizeHandler)
  chart?.dispose()
  chart = null
})

// Watch for data changes — update only, do NOT recreate
watch(() => props.chartData,   () => updateChart(), { deep: true })
watch(() => props.chartLabels, () => updateChart(), { deep: true })
</script>

<template>
  <div ref="chartRef" class="w-full h-full" />
</template>
