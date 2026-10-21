<script setup lang="ts">
import { useBookmarksStore } from '../stores/bookmarks';
import type { ArticleSummary } from '../types/news';
defineProps<{ article: ArticleSummary; showLabel?: boolean }>();
const bookmarks = useBookmarksStore();
</script>

<template>
  <button type="button" class="bookmark-button" :class="{ saved: bookmarks.has(article.id) }"
    :aria-label="`${bookmarks.has(article.id) ? 'Bỏ lưu' : 'Lưu bài'}: ${article.title}`"
    :aria-pressed="bookmarks.has(article.id)" :title="bookmarks.has(article.id) ? 'Bỏ lưu bài viết' : 'Lưu để đọc sau'"
    @click="bookmarks.toggle(article)">
    <i :class="['pi', bookmarks.has(article.id) ? 'pi-bookmark-fill' : 'pi-bookmark']" aria-hidden="true"></i>
    <span v-if="showLabel">{{ bookmarks.has(article.id) ? 'Đã lưu' : 'Lưu bài viết' }}</span>
  </button>
</template>
