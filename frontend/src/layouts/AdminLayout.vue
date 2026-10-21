<script setup lang="ts">
import { ref } from 'vue';
import { RouterView, RouterLink, useRouter } from 'vue-router';
import Button from 'primevue/button';
import { useToast } from 'primevue/usetoast';
import { useAuthStore } from '../stores/auth';
import { errorMessage } from '../services/http';
// Layout là khung chung cho các route con /quan-tri: menu bên trái + vùng nội dung bên phải.
// App.vue ẩn header/footer công khai nhờ route.meta.admin, còn RouterView ở đây dựng trang con.
const auth = useAuthStore();
const router = useRouter();
const toast = useToast();
const menuOpen = ref(false);
const loggingOut = ref(false);
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
  <div class="admin-shell"><aside class="admin-sidebar" :class="{ 'is-open': menuOpen }"><RouterLink to="/" class="logo">TIN<span>24</span><span class="logo-dot">.</span></RouterLink><span class="admin-space-label">KHÔNG GIAN TÒA SOẠN</span><nav aria-label="Điều hướng quản trị" @click="menuOpen = false"><RouterLink to="/quan-tri" exact-active-class="active"><i class="pi pi-objects-column" aria-hidden="true"></i> Quản lý bài viết</RouterLink><RouterLink to="/quan-tri/bai-viet/tao-moi" active-class="active"><i class="pi pi-pen-to-square" aria-hidden="true"></i> Soạn bài mới</RouterLink><RouterLink to="/hoc-vue"><i class="pi pi-graduation-cap" aria-hidden="true"></i> Lộ trình học</RouterLink><RouterLink to="/"><i class="pi pi-arrow-up-right" aria-hidden="true"></i> Xem trang tin</RouterLink></nav><div class="sidebar-note"><i class="pi pi-feather" aria-hidden="true"></i><p>Mỗi bài viết là một góc nhìn.<br>Mỗi lần biên tập là một lần hiểu thêm.</p></div><div class="admin-profile"><span class="author-avatar">{{ auth.user?.name.charAt(0) }}</span><div><strong>{{ auth.user?.name }}</strong><small>{{ auth.roleLabel }}</small></div><Button icon="pi pi-sign-out" text rounded aria-label="Đăng xuất" :loading="loggingOut" @click="logout" /></div></aside><div class="admin-workspace"><header class="admin-topbar"><button type="button" class="icon-button admin-menu-button" :aria-expanded="menuOpen" aria-label="Mở menu quản trị" @click="menuOpen = !menuOpen"><i class="pi pi-bars" aria-hidden="true"></i></button><span><i class="pi pi-circle-fill" aria-hidden="true"></i> Tòa soạn TIN24</span><span class="admin-role">{{ auth.roleLabel }}</span></header><div class="admin-content"><RouterView /></div></div></div>
</template>
