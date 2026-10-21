<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import DOMPurify from 'dompurify';
import { RouterLink, useRoute } from 'vue-router';
import Button from 'primevue/button';
import { getArticle, getArticles, isNotFound } from '../services/news';
import type { Article, ArticleSummary } from '../types/news';
import { formatDate } from '../utils/format';
import ArticleCard from '../components/ArticleCard.vue';
import BookmarkButton from '../components/BookmarkButton.vue';
import NewsImage from '../components/NewsImage.vue';
import StatePanel from '../components/StatePanel.vue';

const route = useRoute();
const article = ref<Article | null>(null);
const related = ref<ArticleSummary[]>([]);
const loading = ref(true);
const error = ref('');
const notFound = ref(false);
const shareMessage = ref('');
const safeContent = computed(() => DOMPurify.sanitize(article.value?.content || ''));
const retry = ref(0);

// Vue tái sử dụng component khi đổi :id; watch tải lại và hủy request cũ.
watch([() => route.params.id, retry], async (_, __, onCleanup) => {
  const controller = new AbortController();
  onCleanup(() => controller.abort());
  article.value = null;
  related.value = [];
  loading.value = true;
  error.value = '';
  notFound.value = false;
  shareMessage.value = '';
  const rawId = String(route.params.id);
  const id = Number(rawId);
  if (!/^\d+$/.test(rawId) || !Number.isSafeInteger(id) || id < 1 || id > 2147483647) {
    notFound.value = true;
    loading.value = false;
    return;
  }
  try {
    const data = await getArticle(id, controller.signal);
    if (controller.signal.aborted) return;
    article.value = data;
    loading.value = false;
    document.title = `${data.title} | TIN24`;
    // Gợi ý lỗi không được làm mất bài viết đã tải thành công.
    try {
      const suggestions = await getArticles({ category: data.categorySlug, pageSize: 4 }, controller.signal);
      if (!controller.signal.aborted) related.value = suggestions.items.filter(item => item.id !== id).slice(0, 3);
    } catch { /* Gợi ý là nội dung bổ sung. */ }
  } catch (cause) {
    if (controller.signal.aborted) return;
    notFound.value = isNotFound(cause);
    if (!notFound.value) error.value = 'Không thể tải bài viết. Kiểm tra kết nối rồi thử lại nhé.';
  } finally {
    if (!controller.signal.aborted) loading.value = false;
  }
}, { immediate: true });

async function share() {
  try {
    await navigator.clipboard.writeText(window.location.href);
    shareMessage.value = 'Đã sao chép liên kết bài viết.';
  } catch {
    shareMessage.value = 'Bạn có thể sao chép đường dẫn trên thanh địa chỉ để chia sẻ.';
  }
}
</script>

<template>
  <div class="container article-page">
    <nav class="breadcrumb" aria-label="Đường dẫn"><RouterLink to="/">Trang chủ</RouterLink><i class="pi pi-angle-right" aria-hidden="true"></i><RouterLink v-if="article" :to="{ path: '/', query: { category: article.categorySlug } }">{{ article.category }}</RouterLink><span v-else>Bài viết</span></nav>
    <div v-if="loading" class="article-loading" role="status" aria-label="Đang tải bài viết"><div class="skeleton skeleton-tag"></div><div class="skeleton skeleton-heading"></div><div class="skeleton skeleton-line"></div><div class="skeleton skeleton-cover"></div><span class="sr-only">Đang tải bài viết…</span></div>
    <StatePanel v-else-if="notFound" title="Không tìm thấy bài viết" description="Bài viết không tồn tại hoặc đã được gỡ. Vẫn còn nhiều câu chuyện khác đang chờ bạn." icon="pi-file"><RouterLink to="/" class="button button-primary">Khám phá tin khác <i class="pi pi-arrow-right" aria-hidden="true"></i></RouterLink></StatePanel>
    <StatePanel v-else-if="error" title="Chưa thể mở câu chuyện này" :description="error" error><Button label="Thử lại" icon="pi pi-refresh" @click="retry++" /></StatePanel>
    <template v-else-if="article">
      <article>
        <header class="article-header"><RouterLink :to="{ path: '/', query: { category: article.categorySlug } }" class="category">{{ article.category }}</RouterLink><h1>{{ article.title }}</h1><p class="article-lead">{{ article.summary }}</p><div class="article-byline"><div class="author-avatar" aria-hidden="true">{{ article.author.charAt(0) }}</div><div><strong>{{ article.author }}</strong><div class="article-meta"><time :datetime="article.publishedAt">{{ formatDate(article.publishedAt) }}</time><span>·</span>{{ article.readingMinutes }} phút đọc</div></div><div class="article-tools"><BookmarkButton :article="article" show-label /><button type="button" class="button button-outline" @click="share"><i class="pi pi-link" aria-hidden="true"></i> Chia sẻ</button></div></div><p v-if="shareMessage" class="share-message" role="status">{{ shareMessage }}</p></header>
        <figure class="article-figure"><NewsImage :src="article.imageUrl" :alt="article.title" eager /><figcaption>Ảnh minh họa · Unsplash</figcaption></figure>
        <div class="article-reading"><div class="rich-content article-content" v-html="safeContent"></div><div class="article-end"><span class="end-mark"></span><span>Chậm lại một chút, hiểu thêm một điều.</span></div><aside class="article-demo">Bài viết thuộc bộ nội dung minh họa của TIN24, phục vụ thực hành phát triển ứng dụng.</aside><RouterLink :to="{ path: '/', query: { category: article.categorySlug } }" class="category-tag"><i class="pi pi-tag" aria-hidden="true"></i>{{ article.category }}</RouterLink></div>
      </article>
      <section v-if="related.length" class="related-section" aria-labelledby="related-title"><div class="section-title"><div><span class="eyebrow">TIẾP NỐI CÂU CHUYỆN</span><h2 id="related-title">Có thể bạn muốn đọc<span class="title-dot">.</span></h2></div></div><div class="cards"><ArticleCard v-for="item in related" :key="item.id" :article="item" /></div></section>
    </template>
  </div>
</template>
