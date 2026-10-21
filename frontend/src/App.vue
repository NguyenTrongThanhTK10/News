<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { RouterView, RouterLink, useRoute, useRouter } from 'vue-router';
import { useBookmarksStore } from './stores/bookmarks';
import { useCategoriesStore } from './stores/categories';
import Toast from 'primevue/toast';

const route = useRoute();
const router = useRouter();
const bookmarks = useBookmarksStore();
const categories = useCategoriesStore();
const search = ref('');
const menuOpen = ref(false);
const today = new Intl.DateTimeFormat('vi-VN', {
  weekday: 'long', day: '2-digit', month: '2-digit', year: 'numeric', timeZone: 'Asia/Ho_Chi_Minh',
}).format(new Date());
const activeCategory = computed(() => route.path === '/' ? route.query.category : undefined);

watch(() => route.fullPath, () => {
  search.value = typeof route.query.search === 'string' ? route.query.search : '';
  menuOpen.value = false;
}, { immediate: true });

function submitSearch() {
  const term = search.value.trim().slice(0, 200);
  void router.push({ path: '/', query: term ? { search: term } : {} });
}

onMounted(() => { void categories.load(); });
</script>

<template>
  <a class="skip-link" href="#main-content">Đến nội dung chính</a>
  <div v-if="!route.meta.admin" class="topbar">
    <div class="container topbar-inner"><span>{{ today }}</span><span class="topbar-tagline"><span class="live-dot"></span> Thêm thông tin. Thêm góc nhìn.</span></div>
  </div>
  <header v-if="!route.meta.admin" class="site-header">
    <div class="container header-inner">
      <RouterLink to="/" class="brand" aria-label="TIN24 — Trang chủ"><span class="logo">TIN<span>24</span><span class="logo-dot">.</span></span><span class="brand-caption">NHỊP SỐNG · GÓC NHÌN</span></RouterLink>
      <form class="header-search" role="search" @submit.prevent="submitSearch">
        <i class="pi pi-search" aria-hidden="true"></i>
        <label for="site-search" class="sr-only">Tìm kiếm tin tức</label>
        <input id="site-search" v-model="search" type="search" placeholder="Bạn muốn đọc gì hôm nay?" maxlength="200">
        <button type="submit" aria-label="Tìm kiếm" class="search-submit"><i class="pi pi-arrow-right" aria-hidden="true"></i></button>
      </form>
      <div class="header-actions">
        <RouterLink to="/quan-tri" class="saved-link admin-entry"><i class="pi pi-pen-to-square" aria-hidden="true"></i><span>Tòa soạn</span></RouterLink>
        <RouterLink to="/da-luu" class="saved-link" :class="{ active: route.path === '/da-luu' }"><i class="pi pi-bookmark" aria-hidden="true"></i><span>Đã lưu</span><span v-if="bookmarks.count" class="count-badge">{{ bookmarks.count }}</span></RouterLink>
        <button type="button" class="icon-button menu-toggle" aria-controls="main-nav" :aria-expanded="menuOpen" :aria-label="menuOpen ? 'Đóng chuyên mục' : 'Mở chuyên mục'" @click="menuOpen = !menuOpen"><i :class="['pi', menuOpen ? 'pi-times' : 'pi-bars']" aria-hidden="true"></i></button>
      </div>
    </div>
    <nav id="main-nav" class="main-nav" :class="{ 'is-open': menuOpen }" aria-label="Chuyên mục chính">
      <div class="container nav-inner">
        <RouterLink to="/" :class="{ selected: route.path === '/' && !activeCategory && !route.query.search }"><i class="pi pi-home" aria-hidden="true"></i><span>Trang chủ</span></RouterLink>
        <RouterLink v-for="category in categories.items" :key="category.slug" :to="{ path: '/', query: { category: category.slug } }" :class="{ selected: activeCategory === category.slug }">{{ category.name }}</RouterLink>
        <RouterLink to="/#tin-moi" class="nav-latest"><span class="live-dot"></span> Mới nhất</RouterLink>
      </div>
    </nav>
  </header>
  <Toast position="top-right" />
  <main id="main-content" tabindex="-1"><RouterView /></main>
  <div v-if="bookmarks.storageError" class="storage-notice" role="status">{{ bookmarks.storageError }}</div>
  <footer v-if="!route.meta.admin" class="site-footer">
    <div class="container footer-main">
      <div><RouterLink to="/" class="logo footer-logo">TIN<span>24</span><span class="logo-dot">.</span></RouterLink><p>Mỗi ngày một góc nhìn mới.<br>Đọc chậm hơn. Hiểu nhiều hơn.</p></div>
      <div class="footer-links"><RouterLink to="/hoc-vue">Lộ trình học</RouterLink><RouterLink to="/">Trang chủ</RouterLink><RouterLink to="/#tin-moi">Tin mới nhất</RouterLink><RouterLink to="/da-luu">Bài viết đã lưu</RouterLink></div>
      <p class="footer-note">Một không gian cho những câu chuyện đáng đọc.<br>Phiên bản minh họa · Nội dung và hình ảnh mẫu.</p>
    </div>
    <div class="container footer-bottom"><span>© {{ new Date().getFullYear() }} TIN24. Dự án thực hành tin tức.</span><span>Được xây dựng với Vue & ASP.NET Core</span></div>
  </footer>
</template>
