<script setup lang="ts">
import { RouterLink } from 'vue-router';
import type { ArticleSummary } from '../types/news';
import { formatDate } from '../utils/format';
import NewsImage from './NewsImage.vue';
import BookmarkButton from './BookmarkButton.vue';
// Props là dữ liệu cha truyền xuống: article bắt buộc, layout tùy chọn và mặc định là 'grid'.
// defineProps/withDefaults là macro của Vue, không cần import. Component chỉ đọc props,
// không tự gọi API, nên dùng lại được ở trang chủ, bài đã lưu và gợi ý bài liên quan.
withDefaults(defineProps<{ article: ArticleSummary; layout?: 'list' | 'grid' }>(), { layout: 'grid' });
</script>

<template>
  <!-- :class bổ sung lớp khi chọn dạng danh sách; CSS quyết định bố cục thay vì đổi dữ liệu.
       Link ảnh trùng đích với tiêu đề nên bỏ khỏi thứ tự Tab để tránh lặp thao tác bàn phím. -->
  <article class="news-card" :class="{ 'news-card-list': layout === 'list' }">
    <RouterLink :to="`/bai-viet/${article.id}`" class="card-image-link" tabindex="-1" aria-hidden="true">
      <NewsImage :src="article.imageUrl" alt="" />
    </RouterLink>
    <div class="card-body">
      <RouterLink :to="{ path: '/', query: { category: article.categorySlug } }" class="category">{{ article.category }}</RouterLink>
      <h3><RouterLink :to="`/bai-viet/${article.id}`">{{ article.title }}</RouterLink></h3>
      <p>{{ article.summary }}</p>
      <div class="card-bottom">
        <span class="article-meta"><time :datetime="article.publishedAt">{{ formatDate(article.publishedAt) }}</time><span>·</span>{{ article.readingMinutes }} phút đọc</span>
        <!-- Truyền tiếp bài xuống nút lưu; nút dùng Pinia nên trạng thái đồng bộ ở mọi thẻ tin. -->
        <BookmarkButton :article="article" />
      </div>
    </div>
  </article>
</template>
