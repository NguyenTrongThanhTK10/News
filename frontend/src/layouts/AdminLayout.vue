<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { RouterView, RouterLink, useRoute, useRouter } from 'vue-router';
import Button from 'primevue/button';
import { useToast } from 'primevue/usetoast';
import { useAuthStore } from '../stores/auth';
import { errorMessage } from '../services/http';
// Layout là khung chung cho các route con /quan-tri: menu bên trái + vùng nội dung bên phải.
// App.vue ẩn header/footer công khai nhờ route.meta.admin, còn RouterView ở đây dựng trang con.
const auth = useAuthStore();
const router = useRouter();
const route = useRoute();
const toast = useToast();
const menuOpen = ref(false);
const newsOpen = ref(false);
const newsActive = computed(() => ['admin', 'article-new', 'article-edit', 'admin-categories'].includes(String(route.name)));
const accountsOpen = ref(false);
const accountsActive = computed(() => route.name === 'admin-users' || route.name === 'admin-roles');
const loggingOut = ref(false);
const sectionLabel = computed(() => accountsActive.value ? 'Quản lý tài khoản' : 'Quản lý tin tức');
const pageLabel = computed(() => ({ admin: 'Tổng quan bài viết', 'article-new': 'Soạn bài mới', 'article-edit': 'Biên tập bài viết',
  'admin-users': 'Người dùng', 'admin-roles': 'Vai trò', 'admin-categories': 'Danh mục bài viết' })[String(route.name)] || 'Tòa soạn');
const today = new Intl.DateTimeFormat('vi-VN', { day: '2-digit', month: 'long', year: 'numeric', timeZone: 'Asia/Ho_Chi_Minh' }).format(new Date());
// Mở nhóm tương ứng khi đi thẳng vào trang con hoặc tải lại trang đang xem.
watch(() => route.name, () => {
  if (newsActive.value) newsOpen.value = true;
  if (accountsActive.value) accountsOpen.value = true;
  menuOpen.value = false;
}, { immediate: true });
function navigate(event: MouseEvent) {
  // Nhấn mở/đóng submenu vẫn giữ sidebar trên mobile; chỉ đóng khi chọn liên kết.
  if (event.target instanceof Element && event.target.closest('a')) menuOpen.value = false;
}
// Đợi API thu hồi phiên rồi chuyển về đăng nhập; lỗi được báo bằng Toast và giữ màn hình hiện tại.
// finally tắt trạng thái chờ cho cả trường hợp thành công lẫn thất bại.
async function logout() {
  loggingOut.value = true;
  try { await auth.logout(); await router.replace('/dang-nhap'); }
  catch (error) { toast.add({ severity: 'error', summary: 'Chưa thể đăng xuất', detail: errorMessage(error), life: 5000 }); }
  finally { loggingOut.value = false; }
}
</script>

<template>
  <!-- menuOpen điều khiển sidebar mobile bằng lớp is-open; nhấp link sẽ đóng menu.
       exact-active-class chỉ tô mục danh sách khi URL khớp chính xác, tránh sáng cả khi soạn bài.
       auth.user dùng ?. vì có thể chưa có phiên; RouterView cuối khung chứa trang quản trị con. -->
  <div class="admin-shell" @keydown.esc="menuOpen = false">
    <button v-if="menuOpen" type="button" class="admin-menu-backdrop" aria-label="Đóng menu bên ngoài" @click="menuOpen = false"></button>
    <aside id="admin-sidebar" class="admin-sidebar" :class="{ 'is-open': menuOpen }">
      <div class="admin-brand"><RouterLink to="/" class="logo">TIN<span>24</span><span class="logo-dot">.</span></RouterLink><span class="admin-brand-badge">CMS</span><button type="button" class="admin-sidebar-close" aria-label="Đóng menu quản trị" @click="menuOpen = false"><i class="pi pi-times" aria-hidden="true"></i></button></div>
      <span class="admin-space-label">KHÔNG GIAN TÒA SOẠN</span>
      <span class="admin-nav-caption">KHÔNG GIAN LÀM VIỆC</span>
      <nav aria-label="Điều hướng quản trị" @click="navigate">
        <div class="admin-nav-group">
          <button type="button" class="admin-nav-toggle" :class="{ active: newsActive }" :aria-expanded="newsOpen" aria-controls="admin-news-submenu" @click="newsOpen = !newsOpen">
            <i class="pi pi-objects-column" aria-hidden="true"></i><span>Quản lý tin tức</span><i class="pi pi-chevron-down admin-nav-chevron" :class="{ 'is-open': newsOpen }" aria-hidden="true"></i>
          </button>
          <ul v-show="newsOpen" id="admin-news-submenu" class="admin-submenu">
            <li><RouterLink to="/quan-tri" exact-active-class="active"><i class="pi pi-file" aria-hidden="true"></i> Quản lý bài viết</RouterLink></li>
            <li><RouterLink to="/quan-tri/bai-viet/tao-moi" active-class="active"><i class="pi pi-pen-to-square" aria-hidden="true"></i> Soạn bài mới</RouterLink></li>
            <li v-if="auth.isAdmin"><RouterLink to="/quan-tri/danh-muc" active-class="active"><i class="pi pi-folder" aria-hidden="true"></i> Danh mục bài viết</RouterLink></li>
          </ul>
        </div>
        <template v-if="auth.isAdmin">
          <div class="admin-nav-group">
            <button type="button" class="admin-nav-toggle" :class="{ active: accountsActive }" :aria-expanded="accountsOpen" aria-controls="admin-account-submenu" @click="accountsOpen = !accountsOpen">
              <i class="pi pi-users" aria-hidden="true"></i><span>Quản lý tài khoản</span><i class="pi pi-chevron-down admin-nav-chevron" :class="{ 'is-open': accountsOpen }" aria-hidden="true"></i>
            </button>
            <ul v-show="accountsOpen" id="admin-account-submenu" class="admin-submenu">
              <li><RouterLink to="/quan-tri/nguoi-dung" active-class="active"><i class="pi pi-user" aria-hidden="true"></i> Người dùng</RouterLink></li>
              <li><RouterLink to="/quan-tri/vai-tro" active-class="active"><i class="pi pi-shield" aria-hidden="true"></i> Vai trò</RouterLink></li>
            </ul>
          </div>
        </template>
        <RouterLink to="/hoc-vue"><i class="pi pi-graduation-cap" aria-hidden="true"></i> Lộ trình học</RouterLink>
        <RouterLink to="/"><i class="pi pi-arrow-up-right" aria-hidden="true"></i> Xem trang tin</RouterLink>
      </nav>
      <div class="sidebar-note"><span class="sidebar-note-icon"><i class="pi pi-pen-to-square" aria-hidden="true"></i></span><strong>Câu chuyện hay bắt đầu từ bạn.</strong><p>Thêm một góc nhìn.<br>Mang lại một điều ý nghĩa.</p></div>
      <div class="admin-profile"><span class="author-avatar">{{ auth.user?.name.charAt(0) }}</span><div><strong>{{ auth.user?.name }}</strong><small>{{ auth.roleLabel }}</small></div><Button icon="pi pi-sign-out" text rounded aria-label="Đăng xuất" :loading="loggingOut" @click="logout" /></div>
    </aside>
    <div class="admin-workspace">
      <header class="admin-topbar">
        <button type="button" class="icon-button admin-menu-button" :aria-expanded="menuOpen" aria-controls="admin-sidebar" aria-label="Mở menu quản trị" @click="menuOpen = !menuOpen"><i class="pi pi-bars" aria-hidden="true"></i></button>
        <div class="admin-breadcrumb"><span>{{ sectionLabel }}</span><i class="pi pi-angle-right" aria-hidden="true"></i><strong>{{ pageLabel }}</strong></div>
        <div class="admin-topbar-right"><span class="admin-today"><i class="pi pi-calendar" aria-hidden="true"></i>{{ today }}</span><span class="admin-topbar-divider"></span><span class="admin-topbar-avatar" aria-hidden="true">{{ auth.user?.name.charAt(0) }}</span><div class="admin-topbar-user"><strong>{{ auth.user?.name }}</strong><span class="admin-role">{{ auth.roleLabel }}</span></div></div>
      </header>
      <div class="admin-content"><RouterView /></div>
      <footer class="admin-workspace-footer"><span>TIN24 <span class="admin-footer-dot">·</span> Không gian cho những câu chuyện đáng đọc.</span><span>Tòa soạn của bạn.</span></footer>
    </div>
  </div>
</template>
