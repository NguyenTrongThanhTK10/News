import { createRouter, createWebHistory } from 'vue-router';
import HomeView from '../views/HomeView.vue';
import { useAuthStore } from '../stores/auth';

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', name: 'home', component: HomeView, meta: { title: 'Tin tức & góc nhìn' } },
    { path: '/bai-viet/:id', name: 'article', component: () => import('../views/ArticleView.vue'), meta: { title: 'Đọc bài viết' } },
    { path: '/da-luu', name: 'saved', component: () => import('../views/SavedView.vue'), meta: { title: 'Bài viết đã lưu' } },
    { path: '/hoc-vue', name: 'learn', component: () => import('../views/LearningView.vue'), meta: { title: 'Lộ trình học Vue & C#' } },
    { path: '/dang-nhap', name: 'login', component: () => import('../views/LoginView.vue'), meta: { title: 'Đăng nhập tòa soạn', admin: true } },
    { path: '/quan-tri', component: () => import('../layouts/AdminLayout.vue'), meta: { title: 'Tòa soạn', requiresAuth: true, admin: true }, children: [
      { path: '', name: 'admin', component: () => import('../views/admin/ArticleListView.vue') },
      { path: 'bai-viet/tao-moi', name: 'article-new', component: () => import('../views/admin/ArticleEditorView.vue'), meta: { title: 'Soạn bài viết' } },
      { path: 'bai-viet/:id/sua', name: 'article-edit', component: () => import('../views/admin/ArticleEditorView.vue'), meta: { title: 'Biên tập bài viết' } },
    ] },
    { path: '/:pathMatch(.*)*', name: 'not-found', component: () => import('../views/NotFoundView.vue'), meta: { title: 'Không tìm thấy trang' } },
  ],
  scrollBehavior(to, from, savedPosition) {
    if (savedPosition) return savedPosition;
    if (to.hash) return { el: to.hash, top: 24, behavior: 'smooth' };
    if (to.path === from.path && to.name === 'home') return false;
    return { top: 0 };
  },
});

router.beforeEach(async to => {
  if (to.meta.requiresAuth || to.name === 'login') {
    const auth = useAuthStore();
    await auth.initialize();
    if (to.meta.requiresAuth && !auth.user) return { name: 'login', query: { redirect: to.fullPath } };
    if (to.name === 'login' && auth.user) return { name: 'admin' };
  }
  return true;
});

router.afterEach(to => { document.title = `${String(to.meta.title)} | TIN24`; });
export default router;
