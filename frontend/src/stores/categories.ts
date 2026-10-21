import { ref } from 'vue';
import { defineStore } from 'pinia';
import { getCategories } from '../services/news';
import type { Category } from '../types/news';

// Store dùng chung cho menu, trang chủ và form quản trị; tránh mỗi nơi tải một bản riêng.
export const useCategoriesStore = defineStore('categories', () => {
  const items = ref<Category[]>([]);
  const error = ref('');
  // pending không hiển thị trên giao diện nên không cần ref; nó chỉ giữ lần tải đang chạy.
  let pending: Promise<void> | undefined;

  function load(force = false): Promise<void> {
    // Đã có dữ liệu thì dùng lại; đang tải thì trả cùng Promise để các nơi gọi cùng chờ.
    if (items.value.length && !force) return Promise.resolve();
    if (pending) return pending;
    pending = getCategories().then(data => {
      items.value = data;
      error.value = '';
    }).catch(() => {
      // Lưu lỗi vào state cho nút "Tải lại chuyên mục"; lần gọi sau vẫn có thể thử lại.
      error.value = 'Chưa tải được chuyên mục.';
    }).finally(() => { pending = undefined; });
    return pending;
  }

  return { items, error, load };
});
