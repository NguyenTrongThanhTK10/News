import { computed, ref, watch } from 'vue';
import { defineStore } from 'pinia';
import type { ArticleSummary } from '../types/news';

const storageKey = 'tin24.bookmarks.v1';

function isArticle(value: unknown): value is ArticleSummary {
  if (!value || typeof value !== 'object') return false;
  const article = value as Record<string, unknown>;
  return Number.isSafeInteger(article.id) && Number(article.id) > 0
    && ['title', 'summary', 'categorySlug', 'category', 'imageUrl', 'publishedAt', 'author']
      .every(key => typeof article[key] === 'string')
    && typeof article.readingMinutes === 'number';
}

function readBookmarks(): ArticleSummary[] {
  try {
    const data: unknown = JSON.parse(localStorage.getItem(storageKey) || '[]');
    return Array.isArray(data)
      ? data.filter(isArticle).filter((article, index, all) => all.findIndex(item => item.id === article.id) === index)
      : [];
  } catch { return []; }
}

export const useBookmarksStore = defineStore('bookmarks', () => {
  const articles = ref<ArticleSummary[]>(readBookmarks());
  const storageError = ref('');
  const count = computed(() => articles.value.length);
  const has = (id: number) => articles.value.some(article => article.id === id);

  function toggle(article: ArticleSummary) {
    if (has(article.id)) articles.value = articles.value.filter(item => item.id !== article.id);
    else {
      // Lưu snapshot danh sách, không lưu nội dung bài chi tiết vào localStorage.
      const { id, title, summary, categorySlug, category, imageUrl, publishedAt, author, readingMinutes } = article;
      articles.value.unshift({ id, title, summary, categorySlug, category, imageUrl, publishedAt, author, readingMinutes });
    }
  }

  watch(articles, value => {
    try {
      localStorage.setItem(storageKey, JSON.stringify(value));
      storageError.value = '';
    } catch {
      storageError.value = 'Trình duyệt không cho phép lưu dữ liệu. Danh sách chỉ được giữ trong phiên này.';
    }
  }, { deep: true, flush: 'sync' });

  return { articles, count, has, toggle, storageError };
});
