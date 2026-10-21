<script setup lang="ts">
import { ref } from 'vue';
import { RouterLink, useRoute, useRouter } from 'vue-router';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Password from 'primevue/password';
import Message from 'primevue/message';
import { useAuthStore } from '../stores/auth';
import { errorMessage } from '../services/http';
// Router guard đã thử khôi phục phiên trước khi vào đây; form chỉ phụ trách đăng nhập mới.
// Auth store giữ user/token dùng chung, view giữ riêng dữ liệu ô nhập và thông báo lỗi.
const auth = useAuthStore();
const route = useRoute();
const router = useRouter();
const email = ref('');
const password = ref('');
const busy = ref(false);
const error = ref('');
// .prevent trong template ngăn submit tải lại trang; busy bật trạng thái chờ của nút/ô nhập.
async function submit() {
  busy.value = true; error.value = '';
  try {
    await auth.login(email.value.trim(), password.value);
    // redirect đến từ query URL nên chỉ chấp nhận đường dẫn bắt đầu bằng /quan-tri hợp lệ.
    // Không dùng trực tiếp một URL tùy ý do người dùng đưa vào tham số này.
    const redirect = typeof route.query.redirect === 'string' && /^\/quan-tri(?:\/|$)/.test(route.query.redirect) ? route.query.redirect : '/quan-tri';
    // replace thay mục hiện tại trong history, tránh Back quay ngay lại form đăng nhập vừa xong.
    await router.replace(redirect);
  } catch (cause) { error.value = errorMessage(cause); }
  finally { busy.value = false; }
}
</script>

<template>
  <!-- v-model cập nhật ref theo ô nhập; required/type=email bật kiểm tra cơ bản của form.
       Password là component PrimeVue: toggle-mask cho phép hiện/ẩn ký tự mật khẩu.
       Message chỉ hiện khi có lỗi; :loading trên Button báo request đang chạy. -->
  <div class="login-page"><div class="login-story"><RouterLink to="/" class="logo">TIN<span>24</span><span class="logo-dot">.</span></RouterLink><div><span class="eyebrow">KHÔNG GIAN TÒA SOẠN</span><h1>Một câu chuyện hay<br>bắt đầu từ bạn.</h1><p>Viết bằng sự tò mò. Biên tập bằng sự cẩn trọng.<br>Mang những góc nhìn ý nghĩa đến độc giả.</p></div><RouterLink to="/" class="login-back"><i class="pi pi-arrow-left" aria-hidden="true"></i> Trở về trang tin</RouterLink></div><div class="login-form-panel"><form class="login-form" @submit.prevent="submit"><span class="eyebrow">CHÀO MỪNG TRỞ LẠI</span><h2>Đăng nhập tòa soạn</h2><p>Dành cho tác giả, biên tập viên và quản trị viên.</p><Message v-if="error" severity="error" :closable="false">{{ error }}</Message><div class="form-field"><label for="login-email">Email</label><InputText id="login-email" v-model="email" type="email" autocomplete="username" placeholder="ban@toasoan.vn" required maxlength="254" :disabled="busy" /></div><div class="form-field"><label for="login-password">Mật khẩu</label><Password v-model="password" input-id="login-password" :feedback="false" toggle-mask autocomplete="current-password" placeholder="Nhập mật khẩu" required :disabled="busy" :input-props="{ maxlength: 200 }" /></div><Button type="submit" label="Đăng nhập" icon="pi pi-arrow-right" icon-pos="right" :loading="busy" class="login-submit" /><p class="login-help">Cần tài khoản? Liên hệ quản trị viên tòa soạn.</p><RouterLink to="/hoc-vue" class="text-button">Khám phá lộ trình học <i class="pi pi-arrow-up-right" aria-hidden="true"></i></RouterLink></form></div></div>
</template>
