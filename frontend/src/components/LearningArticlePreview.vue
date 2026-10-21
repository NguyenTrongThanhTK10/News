<script setup lang="ts">
// Ví dụ luồng một chiều: cha (LearningView) giữ dữ liệu → truyền props xuống con.
// Con yêu cầu thay đổi bằng emit; cha nhận sự kiện rồi tự cập nhật state của mình.
import Tag from 'primevue/tag';
import Button from 'primevue/button';
defineProps<{ title: string; category: string; saved: boolean }>();
// [] nghĩa là sự kiện không có tham số. toggleSave trong script được cha nghe bằng @toggle-save.
defineEmits<{ toggleSave: [] }>();
</script>
<template>
  <!-- $emit không sửa prop saved trực tiếp. Khi cha đổi saved, nhãn/icon của nút tự cập nhật. -->
  <article class="learning-preview"><Tag :value="category || 'Chưa chọn chuyên mục'" severity="secondary" /><h3>{{ title || 'Tiêu đề của bạn sẽ xuất hiện ở đây' }}</h3><p>Component con cập nhật ngay khi props thay đổi.</p><Button :label="saved ? 'Đã lưu ví dụ' : 'Lưu ví dụ'" :icon="saved ? 'pi pi-bookmark-fill' : 'pi pi-bookmark'" outlined :aria-pressed="saved" @click="$emit('toggleSave')" /></article>
</template>
