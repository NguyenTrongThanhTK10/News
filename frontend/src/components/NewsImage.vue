<script setup lang="ts">
import { ref, watch } from 'vue';
const props = withDefaults(defineProps<{ src: string; alt: string; eager?: boolean }>(), { eager: false });
const failed = ref(false);
watch(() => props.src, () => { failed.value = false; });
</script>

<template>
  <div class="news-image" :class="{ 'image-unavailable': failed }">
    <img v-if="!failed" :src="src" :alt="alt" :loading="eager ? 'eager' : 'lazy'"
      :fetchpriority="eager ? 'high' : 'auto'" decoding="async" @error="failed = true">
    <div v-else class="image-fallback" role="img" :aria-label="alt">
      <i class="pi pi-image" aria-hidden="true"></i><span>TIN<span class="accent">24</span></span>
    </div>
  </div>
</template>
