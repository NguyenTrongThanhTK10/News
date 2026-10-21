import { ref } from 'vue';
import { defineStore } from 'pinia';
import { getCategories } from '../services/news';
import type { Category } from '../types/news';

export const useCategoriesStore = defineStore('categories', () => {
  const items = ref<Category[]>([]);
  const error = ref('');
  let pending: Promise<void> | undefined;

  function load(): Promise<void> {
    if (items.value.length) return Promise.resolve();
    if (pending) return pending;
    pending = getCategories().then(data => {
      items.value = data;
      error.value = '';
    }).catch(() => {
      error.value = 'Chưa tải được chuyên mục.';
    }).finally(() => { pending = undefined; });
    return pending;
  }

  return { items, error, load };
});
