<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { RouterLink, useRoute, useRouter } from 'vue-router';
import Button from 'primevue/button';
import { getArticles } from '../services/news';
import { useCategoriesStore } from '../stores/categories';
import type { ArticleSummary, PagedResult } from '../types/news';
import { categoryIcon } from '../utils/format';
import ArticleCard from '../components/ArticleCard.vue';
import ArticleSkeleton from '../components/ArticleSkeleton.vue';
import BookmarkButton from '../components/BookmarkButton.vue';
import NewsImage from '../components/NewsImage.vue';
import StatePanel from '../components/StatePanel.vue';

const route = useRoute();
const router = useRouter();
const categories = useCategoriesStore();
const loading = ref(true);
const error = ref('');
const result = ref<PagedResult<ArticleSummary> | null>(null);
const search = computed(() => typeof route.query.search === 'string' ? route.query.search.trim().slice(0, 200) : '');
const category = computed(() => typeof route.query.category === 'string' ? route.query.category.slice(0, 80) : '');
const page = computed(() => {
  const value = Number(route.query.page);
  return Number.isSafeInteger(value) && value > 0 && value <= 2147483647 ? value : 1;
});
const categoryName = computed(() => categories.items.find(item => item.slug === category.value)?.name || 'Chuyên mục');
const isFrontPage = computed(() => !search.value && !category.value && page.value === 1);
const featured = computed(() => isFrontPage.value ? result.value?.items[0] : undefined);
const highlights = computed(() => isFrontPage.value ? result.value?.items.slice(1, 3) || [] : []);
const articles = computed(() => result.value?.items.slice(featured.value ? 3 : 0) || []);
const pagination = computed(() => {
  const total = result.value?.totalPages || 0;
  const start = Math.max(1, Math.min(page.value - 2, total - 4));
  return Array.from({ length: Math.min(5, total) }, (_, index) => start + index);
});
let activeController: AbortController | undefined;

async function load() {
  activeController?.abort();
  const controller = new AbortController();
  activeController = controller;
  loading.value = true;
  error.value = '';
  try {
    const data = await getArticles({ search: search.value || undefined, category: category.value || undefined, page: page.value, pageSize: 9 }, controller.signal);
    if (!controller.signal.aborted) result.value = data;
  } catch {
    if (!controller.signal.aborted) error.value = 'Không thể tải tin tức lúc này. Kiểm tra kết nối rồi thử lại nhé.';
  } finally {
    if (!controller.signal.aborted) loading.value = false;
  }
}

// URL là nguồn trạng thái duy nhất: chia sẻ đường dẫn và Back/Forward đều giữ bộ lọc.
watch([search, category, page], () => { void load(); }, { immediate: true });
onBeforeUnmount(() => activeController?.abort());

function selectPage(value: number) {
  void router.push({ path: '/', query: { ...route.query, page: value === 1 ? undefined : String(value) }, hash: '#tin-moi' });
}
</script>

<template>
  <div class="container page home-page">
    <section class="intro" aria-labelledby="home-title">
      <div><div class="eyebrow"><span class="live-dot"></span> MỖI NGÀY MỘT GÓC NHÌN MỚI</div><h1 id="home-title">Nhịp sống <em>hôm nay.</em></h1><p>Những câu chuyện đáng đọc, những góc nhìn đáng suy ngẫm.</p></div>
      <div class="edition"><i class="pi pi-sparkles" aria-hidden="true"></i><span>Chọn lọc để bạn<br><strong>đọc điều ý nghĩa.</strong></span></div>
    </section>

    <div class="category-filter" aria-label="Lọc theo chuyên mục">
      <RouterLink :to="{ path: '/', query: search ? { search } : {} }" class="category-chip" :class="{ active: !category }"><i class="pi pi-th-large" aria-hidden="true"></i>Tất cả</RouterLink>
      <RouterLink v-for="item in categories.items" :key="item.slug" :to="{ path: '/', query: { category: item.slug, ...(search ? { search } : {}) } }" class="category-chip" :class="{ active: category === item.slug }"><i :class="['pi', categoryIcon(item.slug)]" aria-hidden="true"></i>{{ item.name }}</RouterLink>
      <button v-if="categories.error" class="text-button" type="button" @click="categories.load()">Tải lại chuyên mục</button>
    </div>

    <div v-if="search || category" class="filter-summary"><span v-if="search">Kết quả cho <strong>“{{ search }}”</strong></span><span v-if="category" class="filter-label">{{ categoryName }}</span><RouterLink to="/" class="text-button">Xóa bộ lọc <i class="pi pi-times" aria-hidden="true"></i></RouterLink></div>

    <ArticleSkeleton v-if="loading" />
    <StatePanel v-else-if="error" title="Kết nối đang gián đoạn" :description="error" error><Button label="Thử lại" icon="pi pi-refresh" class="button-primary" @click="load" /></StatePanel>
    <template v-else-if="result">
      <section v-if="featured" class="featured-grid" aria-label="Tin nổi bật">
        <article class="lead-story">
          <NewsImage :src="featured.imageUrl" :alt="featured.title" eager />
          <div class="lead-shade"></div>
          <div class="lead-content"><span class="feature-label"><span class="live-dot"></span> CÂU CHUYỆN NỔI BẬT</span><RouterLink :to="{ path: '/', query: { category: featured.categorySlug } }" class="lead-category">{{ featured.category }}</RouterLink><h2><RouterLink :to="`/bai-viet/${featured.id}`">{{ featured.title }}</RouterLink></h2><p>{{ featured.summary }}</p><div class="lead-bottom"><span>{{ featured.author }} <span class="meta-dot">·</span> {{ featured.readingMinutes }} phút đọc</span><RouterLink :to="`/bai-viet/${featured.id}`" class="round-arrow" aria-label="Đọc câu chuyện nổi bật"><i class="pi pi-arrow-up-right" aria-hidden="true"></i></RouterLink></div></div>
        </article>
        <div class="highlights"><div class="highlights-heading"><span class="eyebrow">ĐÁNG CHÚ Ý</span><span class="tiny-line"></span></div><article v-for="article in highlights" :key="article.id" class="highlight-card"><RouterLink :to="`/bai-viet/${article.id}`" class="highlight-image" tabindex="-1" aria-hidden="true"><NewsImage :src="article.imageUrl" alt="" eager /></RouterLink><div class="highlight-content"><RouterLink :to="{ path: '/', query: { category: article.categorySlug } }" class="category">{{ article.category }}</RouterLink><h3><RouterLink :to="`/bai-viet/${article.id}`">{{ article.title }}</RouterLink></h3><p>{{ article.summary }}</p><div class="card-bottom"><span class="article-meta">{{ article.readingMinutes }} phút đọc</span><BookmarkButton :article="article" /></div></div></article></div>
      </section>

      <section id="tin-moi" class="latest-section" aria-labelledby="latest-title">
        <div class="section-title"><div><span class="eyebrow">DÀNH CHO BẠN</span><h2 id="latest-title">{{ search ? 'Kết quả tìm kiếm' : category ? categoryName : 'Dòng tin mới nhất' }}<span class="title-dot">.</span></h2></div><span class="results-count" aria-live="polite">{{ result.totalCount }} bài viết <span v-if="result.totalPages > 1">· Trang {{ page }}/{{ result.totalPages }}</span></span></div>
        <div v-if="articles.length" class="cards"><ArticleCard v-for="article in articles" :key="article.id" :article="article" /></div>
        <StatePanel v-else :title="result.totalCount ? 'Trang này chưa có bài viết' : 'Chưa tìm thấy câu chuyện phù hợp'" :description="result.totalCount ? 'Hãy quay lại trang đầu để tiếp tục đọc.' : 'Thử từ khóa khác hoặc khám phá tất cả chuyên mục.'"><RouterLink :to="result.totalCount ? { path: '/', query: { ...route.query, page: undefined } } : '/'" class="button button-primary">{{ result.totalCount ? 'Về trang đầu' : 'Xem tất cả tin tức' }}<i class="pi pi-arrow-right" aria-hidden="true"></i></RouterLink></StatePanel>
        <nav v-if="result.totalPages > 1" class="pagination" aria-label="Phân trang tin tức"><button type="button" :disabled="page <= 1" aria-label="Trang trước" @click="selectPage(page - 1)"><i class="pi pi-angle-left" aria-hidden="true"></i></button><button v-for="number in pagination" :key="number" type="button" :class="{ current: page === number }" :aria-label="`Trang ${number}`" :aria-current="page === number ? 'page' : undefined" @click="selectPage(number)">{{ number }}</button><button type="button" :disabled="page >= result.totalPages" aria-label="Trang sau" @click="selectPage(page + 1)"><i class="pi pi-angle-right" aria-hidden="true"></i></button></nav>
      </section>
    </template>

    <section class="reading-banner"><div class="banner-icon"><i class="pi pi-bookmark" aria-hidden="true"></i></div><div><span class="eyebrow">MỘT KHOẢNG DỪNG CHO BẠN</span><h2>Câu chuyện hay, để dành đọc lại.</h2><p>Lưu những bài viết bạn thích và trở lại bất cứ lúc nào.</p></div><RouterLink to="/da-luu" class="button button-dark">Góc đọc của bạn <i class="pi pi-arrow-up-right" aria-hidden="true"></i></RouterLink></section>
    <p class="demo-note"><i class="pi pi-info-circle" aria-hidden="true"></i> Nội dung biên tập minh họa cho dự án, không phải bản tin thực tế.</p>
  </div>
</template>
