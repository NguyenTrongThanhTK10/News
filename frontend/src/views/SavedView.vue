<script setup lang="ts">
import { RouterLink } from 'vue-router';
import { useBookmarksStore } from '../stores/bookmarks';
import ArticleCard from '../components/ArticleCard.vue';
import StatePanel from '../components/StatePanel.vue';
// Không gọi API: danh sách là bản tóm tắt đã lưu ở localStorage qua bookmarks store.
// Khi một BookmarkButton bỏ lưu, store đổi và danh sách/count trên trang tự cập nhật.
const bookmarks = useBookmarksStore();
</script>

<template>
  <div class="container page saved-page">
    <div class="page-heading"><span class="eyebrow">GÓC ĐỌC CỦA BẠN</span><h1>Để dành một câu chuyện hay.</h1><p>Những bài viết bạn muốn quay lại, trong một không gian riêng.</p></div>
    <div class="section-title"><h2>Bài viết đã lưu <span class="section-count">{{ bookmarks.count }}</span></h2><span class="muted">Lưu trên trình duyệt này</span></div>
    <!-- Có bài thì tái sử dụng ArticleCard; không có thì StatePanel nhận link qua default slot. -->
    <div v-if="bookmarks.count" class="cards"><ArticleCard v-for="article in bookmarks.articles" :key="article.id" :article="article" /></div>
    <StatePanel v-else title="Chưa có bài viết nào được lưu" description="Chạm vào biểu tượng đánh dấu trên bài viết để dành lại cho lần đọc sau." icon="pi-bookmark"><RouterLink to="/" class="button button-primary">Khám phá tin tức <i class="pi pi-arrow-right" aria-hidden="true"></i></RouterLink></StatePanel>
  </div>
</template>
