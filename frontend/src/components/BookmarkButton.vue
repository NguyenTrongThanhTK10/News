<script setup lang="ts">
import { useBookmarksStore } from '../stores/bookmarks';
import type { ArticleSummary } from '../types/news';
// Cùng một nút dùng cho thẻ tin (chỉ icon) và trang chi tiết (showLabel để có thêm chữ).
defineProps<{ article: ArticleSummary; showLabel?: boolean }>();
// Store là nguồn trạng thái duy nhất; không tạo ref "đã lưu" riêng để tránh các nút lệch nhau.
const bookmarks = useBookmarksStore();
</script>

<template>
  <!-- has kiểm tra theo ID; toggle đảo trạng thái và store tự ghi localStorage.
       aria-pressed mô tả nút bật/tắt; aria-label có tên bài để phân biệt nhiều nút trên trang. -->
  <button type="button" class="bookmark-button" :class="{ saved: bookmarks.has(article.id) }"
    :aria-label="`${bookmarks.has(article.id) ? 'Bỏ lưu' : 'Lưu bài'}: ${article.title}`"
    :aria-pressed="bookmarks.has(article.id)" :title="bookmarks.has(article.id) ? 'Bỏ lưu bài viết' : 'Lưu để đọc sau'"
    @click="bookmarks.toggle(article)">
    <i :class="['pi', bookmarks.has(article.id) ? 'pi-bookmark-fill' : 'pi-bookmark']" aria-hidden="true"></i>
    <span v-if="showLabel">{{ bookmarks.has(article.id) ? 'Đã lưu' : 'Lưu bài viết' }}</span>
  </button>
</template>
