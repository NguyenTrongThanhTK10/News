<script setup lang="ts">
// Single File Component (.vue) gồm script (logic), template (giao diện) và có thể có style.
// setup cho phép template dùng trực tiếp biến/hàm/import bên dưới; lang="ts" bật TypeScript.
import { computed, onMounted, ref, watch } from 'vue';
import { RouterView, RouterLink, useRoute, useRouter } from 'vue-router';
import { useBookmarksStore } from './stores/bookmarks';
import { useCategoriesStore } from './stores/categories';
import Toast from 'primevue/toast';
import BackToTop from './components/BackToTop.vue';

// route mô tả URL hiện tại; router cung cấp hàm chuyển trang như push().
// Hai store bên dưới chia sẻ dữ liệu với mọi trang, nên số bài đã lưu luôn cập nhật cùng nhau.
const route = useRoute();
const router = useRouter();
const bookmarks = useBookmarksStore();
const categories = useCategoriesStore();
// ref giữ dữ liệu có thể thay đổi: trong script đọc/ghi .value; template tự bỏ lớp .value.
const search = ref('');
const menuOpen = ref(false);
// Ngày được tính lúc component khởi tạo; cố định múi giờ để người dùng thấy giờ Việt Nam.
const today = new Intl.DateTimeFormat('vi-VN', {
  weekday: 'long', day: '2-digit', month: '2-digit', year: 'numeric', timeZone: 'Asia/Ho_Chi_Minh',
}).format(new Date());
// computed là dữ liệu suy ra từ state khác; tự cập nhật khi URL thay đổi.
const activeCategory = computed(() => route.path === '/' ? route.query.category : undefined);

// watch dùng khi cần làm một việc sau thay đổi: đồng bộ ô tìm kiếm và đóng menu mobile.
// immediate: true chạy ngay lần đầu, nên URL có sẵn ?search=... cũng điền đúng ô tìm kiếm.
watch(() => route.fullPath, () => {
  search.value = typeof route.query.search === 'string' ? route.query.search : '';
  menuOpen.value = false;
}, { immediate: true });

// Chỉ cập nhật URL; HomeView theo dõi query để gọi API. trim bỏ khoảng trắng hai đầu,
// slice giới hạn độ dài. void cho biết không chờ Promise của router.push ở hàm này.
function submitSearch() {
  const term = search.value.trim().slice(0, 200);
  void router.push({ path: '/', query: term ? { search: term } : {} });
}

// Lifecycle hook: chạy sau khi Vue gắn component vào DOM (cây phần tử của trang).
// Store tự dùng lại dữ liệu/lần tải đang chạy nên các trang khác có thể gọi load an toàn.
onMounted(() => { void categories.load(); });
</script>

<template>
  <!-- Liên kết cho người dùng bàn phím bỏ qua menu và đi thẳng đến nội dung chính. -->
  <a class="skip-link" href="#main-content">Đến nội dung chính</a>
  <!-- v-if chỉ dựng phần tử khi điều kiện đúng. Các trang admin dùng khung riêng. -->
  <div v-if="!route.meta.admin" class="topbar">
    <div class="container topbar-inner"><span>{{ today }}</span><span class="topbar-tagline"><span class="live-dot"></span> Thế giới chuyển động. Góc nhìn mở rộng.</span><RouterLink to="/hoc-vue" class="topbar-learning">Khám phá lộ trình học <i class="pi pi-arrow-up-right" aria-hidden="true"></i></RouterLink></div>
  </div>
  <header v-if="!route.meta.admin" class="site-header">
    <div class="container header-inner">
      <RouterLink to="/" class="brand" aria-label="TIN24 — Trang chủ"><span class="logo">TIN<span>24</span><span class="logo-dot">.</span></span><span class="brand-caption">Tin hay mỗi ngày.<br>Góc nhìn rộng mở.</span></RouterLink>
      <!-- @ là v-on để bắt sự kiện; .prevent ngăn form tải lại trang khi submit.
           v-model đồng bộ hai chiều: nhập ô → search đổi, search đổi → ô nhập cập nhật. -->
      <form class="header-search" role="search" @submit.prevent="submitSearch">
        <i class="pi pi-search" aria-hidden="true"></i>
        <label for="site-search" class="sr-only">Tìm kiếm tin tức</label>
        <input id="site-search" v-model="search" type="search" placeholder="Tìm câu chuyện bạn quan tâm..." maxlength="200">
        <button type="submit" aria-label="Tìm kiếm" class="search-submit"><i class="pi pi-arrow-right" aria-hidden="true"></i></button>
      </form>
      <div class="header-actions">
        <RouterLink to="/quan-tri" class="saved-link admin-entry" aria-label="Tòa soạn"><i class="pi pi-pen-to-square" aria-hidden="true"></i><span>Tòa soạn</span></RouterLink>
        <RouterLink to="/da-luu" class="saved-link" :class="{ active: route.path === '/da-luu' }"><i class="pi pi-bookmark" aria-hidden="true"></i><span>Đã lưu</span><span v-if="bookmarks.count" class="count-badge">{{ bookmarks.count }}</span></RouterLink>
        <button type="button" class="icon-button menu-toggle" aria-controls="main-nav" :aria-expanded="menuOpen" :aria-label="menuOpen ? 'Đóng chuyên mục' : 'Mở chuyên mục'" @click="menuOpen = !menuOpen"><i :class="['pi', menuOpen ? 'pi-times' : 'pi-bars']" aria-hidden="true"></i></button>
      </div>
    </div>
    <!-- : là v-bind để truyền biểu thức thay vì chuỗi; :class thêm lớp theo điều kiện.
         v-for tạo một liên kết cho mỗi chuyên mục; :key giúp Vue nhận diện đúng từng mục.
         RouterLink đổi URL trong ứng dụng; các thuộc tính aria hỗ trợ trình đọc màn hình. -->
    <nav id="main-nav" class="main-nav" :class="{ 'is-open': menuOpen }" aria-label="Chuyên mục chính">
      <div class="container nav-inner">
        <RouterLink to="/" :class="{ selected: route.path === '/' && !activeCategory && !route.query.search }"><i class="pi pi-home" aria-hidden="true"></i><span>Trang chủ</span></RouterLink>
        <RouterLink v-for="category in categories.items" :key="category.slug" :to="{ path: '/', query: { category: category.slug } }" :class="{ selected: activeCategory === category.slug }" :aria-current="activeCategory === category.slug ? 'page' : undefined">{{ category.name }}</RouterLink>
        <RouterLink to="/#tin-moi" class="nav-latest"><span class="live-dot"></span> Mới nhất</RouterLink>
      </div>
    </nav>
  </header>
  <!-- Toast là nơi hiển thị thông báo do useToast().add() gọi từ các trang con. -->
  <Toast position="top-right" />
  <!-- RouterView là chỗ router chèn trang khớp URL; header/footer không cần dựng lại khi đổi trang. -->
  <main id="main-content" tabindex="-1"><RouterView /></main>
  <!-- Component tự quản lý việc hiện nút và lắng nghe cuộn, dùng chung cho toàn ứng dụng. -->
  <BackToTop />
  <div v-if="bookmarks.storageError" class="storage-notice" role="status">{{ bookmarks.storageError }}</div>
  <!-- Nội suy {{ ... }} hiển thị giá trị như văn bản và tự cập nhật theo dữ liệu phản ứng. -->
  <footer v-if="!route.meta.admin" class="site-footer">
    <div class="container footer-main">
      <div><RouterLink to="/" class="logo footer-logo">TIN<span>24</span><span class="logo-dot">.</span></RouterLink><p>Mỗi ngày một góc nhìn mới.<br>Đọc chậm hơn. Hiểu nhiều hơn.</p></div>
      <div class="footer-links"><RouterLink to="/hoc-vue">Lộ trình học</RouterLink><RouterLink to="/">Trang chủ</RouterLink><RouterLink to="/#tin-moi">Tin mới nhất</RouterLink><RouterLink to="/da-luu">Bài viết đã lưu</RouterLink></div>
      <p class="footer-note">Một không gian cho những câu chuyện đáng đọc.<br>Phiên bản minh họa · Nội dung và hình ảnh mẫu.</p>
    </div>
    <div class="container footer-bottom"><span>© {{ new Date().getFullYear() }} TIN24. Dự án thực hành tin tức.</span><span>Được xây dựng với Vue & ASP.NET Core</span></div>
  </footer>
</template>
