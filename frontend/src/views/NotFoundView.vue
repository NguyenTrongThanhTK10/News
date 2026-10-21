<script setup lang="ts">
// Trang 404 của router khi URL không khớp route nào; khác với bài viết có ID nhưng API trả 404.
// Không cần state hoặc request: chỉ ghép StatePanel và đường dẫn quay về trang chủ.
import { RouterLink } from 'vue-router';
import StatePanel from '../components/StatePanel.vue';
</script>

<template>
  <div class="container page"><StatePanel title="Trang này chưa có câu chuyện nào" description="Đường dẫn có thể đã thay đổi. Hãy trở về trang chủ để khám phá các bài viết khác." icon="pi-compass"><span class="error-code">404</span><RouterLink to="/" class="button button-primary">Về trang chủ <i class="pi pi-arrow-right" aria-hidden="true"></i></RouterLink></StatePanel></div>
</template>
