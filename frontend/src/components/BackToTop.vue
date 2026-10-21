<script setup lang="ts">
import { onMounted, onUnmounted, ref } from 'vue';

// Trạng thái cục bộ: chưa cuộn thì ẩn nút. Đổi ref sẽ khiến v-if cập nhật giao diện.
const visible = ref(false);

// scrollY là số pixel trang đã cuộn theo chiều dọc; chỉ hiện nút sau mốc 300px.
function updateVisibility() {
  visible.value = window.scrollY > 300;
}

// top: 0 là đầu trang. Nếu người dùng chọn giảm chuyển động trong hệ điều hành,
// dùng instant để tránh hiệu ứng cuộn; các trường hợp còn lại dùng smooth.
function scrollToTop() {
  window.scrollTo({
    top: 0,
    behavior: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'instant' : 'smooth',
  });
}

// Chỉ truy cập vị trí cuộn khi component đã gắn vào DOM. Gọi lần đầu để xử lý cả trường hợp
// trình duyệt khôi phục vị trí cũ; passive cho biết listener không chặn thao tác cuộn mặc định.
onMounted(() => {
  updateVisibility();
  window.addEventListener('scroll', updateVisibility, { passive: true });
});

// Dọn listener bằng đúng hàm đã đăng ký, tránh giữ callback sau khi component bị tháo.
onUnmounted(() => {
  window.removeEventListener('scroll', updateVisibility);
});
</script>

<template>
  <!-- Nút chỉ có icon nên cần aria-label; icon trang trí được ẩn với trình đọc màn hình. -->
  <button
    v-if="visible"
    type="button"
    class="back-to-top"
    aria-label="Lên đầu trang"
    title="Lên đầu trang"
    @click="scrollToTop"
  >
    <i class="pi pi-arrow-up" aria-hidden="true"></i>
  </button>
</template>

<style scoped>
/* scoped giới hạn CSS vào component này. fixed giữ nút ở góc màn hình khi trang cuộn;
   safe-area-inset chừa vùng an toàn của thiết bị; z-index đặt nút trên nội dung thông thường. */
.back-to-top {
  position: fixed;
  right: max(20px, env(safe-area-inset-right));
  bottom: max(24px, env(safe-area-inset-bottom));
  z-index: 25;
  display: grid;
  place-items: center;
  width: 48px;
  height: 48px;
  padding: 0;
  border: 1px solid #ffffff40;
  border-radius: 50%;
  background: var(--accent);
  color: #fff;
  box-shadow: 0 4px 16px #17233c26;
}

.back-to-top:hover {
  background: var(--accent-hover);
}
</style>
