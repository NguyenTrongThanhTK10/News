<script setup lang="ts">
// Component con nhận props và gửi sự kiện lên component cha.
import Tag from 'primevue/tag';
import Button from 'primevue/button';
defineProps<{ title: string; category: string; saved: boolean }>();
defineEmits<{ toggleSave: [] }>();
</script>
<template>
  <article class="learning-preview"><Tag :value="category || 'Chưa chọn chuyên mục'" severity="secondary" /><h3>{{ title || 'Tiêu đề của bạn sẽ xuất hiện ở đây' }}</h3><p>Component con cập nhật ngay khi props thay đổi.</p><Button :label="saved ? 'Đã lưu ví dụ' : 'Lưu ví dụ'" :icon="saved ? 'pi pi-bookmark-fill' : 'pi pi-bookmark'" outlined :aria-pressed="saved" @click="$emit('toggleSave')" /></article>
</template>
