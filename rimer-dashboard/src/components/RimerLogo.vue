<template>
  <!-- Her iki logo aynı boyutta, üst üste — opacity ile geçiş yapılır, layout değişmez -->
  <div :class="containerClass" class="relative flex-shrink-0">
    <img
      src="/logo.png"
      alt="Rimer Logo"
      :class="[imgClass, isDark ? 'opacity-0' : 'opacity-100']"
      class="absolute inset-0 w-full h-full object-contain transition-opacity duration-200"
    />
    <img
      src="/logonight.png"
      alt="Rimer Logo"
      :class="[imgClass, isDark ? 'opacity-100' : 'opacity-0']"
      class="absolute inset-0 w-full h-full object-contain transition-opacity duration-200"
    />
    <!-- Görünmez spacer: yükseklik/boyutu korur -->
    <img src="/logo.png" alt="" aria-hidden="true" :class="imgClass" class="w-full h-full object-contain invisible" />
  </div>
</template>

<script setup>
defineProps({
  isDark: {
    type: Boolean,
    default: false
  },
  containerClass: {
    type: String,
    default: ''
  },
  imgClass: {
    type: String,
    default: ''
  }
})
</script>
