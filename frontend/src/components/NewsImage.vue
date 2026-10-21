<script setup lang="ts">
import { ref, watch } from 'vue';
// src là URL, alt là mô tả ảnh; eager mặc định false để ảnh ngoài màn hình tải khi cần (lazy).
const props = withDefaults(defineProps<{ src: string; alt: string; eager?: boolean }>(), { eager: false });
const failed = ref(false);
// Nếu Vue dùng lại component cho URL mới, cho phép thử ảnh mới dù URL trước đã tải lỗi.
watch(() => props.src, () => { failed.value = false; });
</script>

<template>
  <div class="news-image" :class="{ 'image-unavailable': failed }">
    <!-- Ảnh đầu trang có eager được ưu tiên tải; decoding async giảm việc chặn render.
         @error bật failed để thay bằng khung dự phòng, giữ bố cục và đường dẫn bài hoạt động. -->
    <img v-if="!failed" :src="src" :alt="alt" :loading="eager ? 'eager' : 'lazy'"
      :fetchpriority="eager ? 'high' : 'auto'" decoding="async" @error="failed = true">
    <div v-else class="image-fallback" role="img" :aria-label="alt">
      <i class="pi pi-image" aria-hidden="true"></i><span>TIN<span class="accent">24</span></span>
    </div>
  </div>
</template>
