import { createRouter, createWebHistory } from 'vue-router';
import HomeView from '../views/HomeView.vue';
import { useAuthStore } from '../stores/auth';

// Router ánh xạ URL → component trang, để chuyển trang mà không tải lại toàn bộ ứng dụng.
// Web history dùng đường dẫn bình thường; khi triển khai, server cần trả index.html cho route SPA.
const router = createRouter({
  history: createWebHistory(),
  // HomeView được import sẵn vì là trang vào chính. () => import(...) tải các trang khác khi cần.
  // :id là tham số động; query (ví dụ ?page=2) được đọc riêng qua route.query trong view.
  // meta chứa thông tin phụ do ứng dụng quy ước, không tự tạo cơ chế đăng nhập/phân quyền.
  routes: [
    { path: '/', name: 'home', component: HomeView, meta: { title: 'Tin tức & góc nhìn' } },
    { path: '/bai-viet/:id', name: 'article', component: () => import('../views/ArticleView.vue'), meta: { title: 'Đọc bài viết' } },
    { path: '/da-luu', name: 'saved', component: () => import('../views/SavedView.vue'), meta: { title: 'Bài viết đã lưu' } },
    { path: '/hoc-vue', name: 'learn', component: () => import('../views/LearningView.vue'), meta: { title: 'Lộ trình học Vue & C#' } },
    { path: '/dang-nhap', name: 'login', component: () => import('../views/LoginView.vue'), meta: { title: 'Đăng nhập tòa soạn', admin: true } },
    // Route con hiển thị trong RouterView của AdminLayout; giữ nguyên sidebar khi chuyển trang con.
    { path: '/quan-tri', component: () => import('../layouts/AdminLayout.vue'), meta: { title: 'Tòa soạn', requiresAuth: true, admin: true }, children: [
      { path: '', name: 'admin', component: () => import('../views/admin/ArticleListView.vue') },
      { path: 'bai-viet/tao-moi', name: 'article-new', component: () => import('../views/admin/ArticleEditorView.vue'), meta: { title: 'Soạn bài viết' } },
      { path: 'bai-viet/:id/sua', name: 'article-edit', component: () => import('../views/admin/ArticleEditorView.vue'), meta: { title: 'Biên tập bài viết' } },
    ] },
    // Route bắt mọi đường dẫn còn lại, dùng cho trang 404 ở frontend.
    { path: '/:pathMatch(.*)*', name: 'not-found', component: () => import('../views/NotFoundView.vue'), meta: { title: 'Không tìm thấy trang' } },
  ],
  // Back/Forward khôi phục vị trí cũ; #hash cuộn đến mục tương ứng.
  // Đổi bộ lọc trang chủ giữ vị trí; các trang mới còn lại bắt đầu từ đầu trang.
  scrollBehavior(to, from, savedPosition) {
    if (savedPosition) return savedPosition;
    if (to.hash) return { el: to.hash, top: 24, behavior: 'smooth' };
    if (to.path === from.path && to.name === 'home') return false;
    return { top: 0 };
  },
});

// Navigation guard chạy trước khi vào trang: khôi phục phiên rồi mới kiểm tra đăng nhập.
// Trả về route để chuyển hướng, hoặc true để cho phép đi tiếp.
// redirect giữ URL đích để LoginView đưa người dùng trở lại sau khi đăng nhập thành công.
router.beforeEach(async to => {
  if (to.meta.requiresAuth || to.name === 'login') {
    const auth = useAuthStore();
    await auth.initialize();
    if (to.meta.requiresAuth && !auth.user) return { name: 'login', query: { redirect: to.fullPath } };
    if (to.name === 'login' && auth.user) return { name: 'admin' };
  }
  return true;
});

// Sau điều hướng, cập nhật tên tab trình duyệt. Trang chi tiết còn thay bằng tiêu đề bài sau khi tải.
router.afterEach(to => { document.title = `${String(to.meta.title)} | TIN24`; });
export default router;
