<script setup lang="ts">
// Component dùng chung cho trạng thái rỗng, không tìm thấy và lỗi; cha quyết định nội dung.
defineProps<{ title: string; description?: string; icon?: string; error?: boolean }>();
</script>

<template>
  <!-- alert thông báo lỗi ngay cho trình đọc màn hình; status dành cho cập nhật thông thường. -->
  <div class="state-panel" :role="error ? 'alert' : 'status'">
    <span class="state-icon"><i :class="['pi', icon || (error ? 'pi-wifi' : 'pi-search')]" aria-hidden="true"></i></span>
    <h2>{{ title }}</h2>
    <p v-if="description">{{ description }}</p>
    <!-- Slot là vị trí cha chèn giao diện riêng, ví dụ nút "Thử lại" hoặc link về trang chủ. -->
    <slot />
  </div>
</template>
