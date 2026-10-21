<script setup lang="ts">
import { RouterLink } from 'vue-router';
import type { ArticleSummary } from '../types/news';
import { formatDate } from '../utils/format';
import NewsImage from './NewsImage.vue';
import BookmarkButton from './BookmarkButton.vue';
defineProps<{ article: ArticleSummary }>();
</script>

<template>
  <article class="news-card">
    <RouterLink :to="`/bai-viet/${article.id}`" class="card-image-link" tabindex="-1" aria-hidden="true">
      <NewsImage :src="article.imageUrl" alt="" />
    </RouterLink>
    <div class="card-body">
      <RouterLink :to="{ path: '/', query: { category: article.categorySlug } }" class="category">{{ article.category }}</RouterLink>
      <h3><RouterLink :to="`/bai-viet/${article.id}`">{{ article.title }}</RouterLink></h3>
      <p>{{ article.summary }}</p>
      <div class="card-bottom">
        <span class="article-meta"><time :datetime="article.publishedAt">{{ formatDate(article.publishedAt) }}</time><span>·</span>{{ article.readingMinutes }} phút đọc</span>
        <BookmarkButton :article="article" />
      </div>
    </div>
  </article>
</template>
