import { computed, ref, watch } from 'vue';
import { defineStore } from 'pinia';
import type { ArticleSummary } from '../types/news';

// localStorage lưu chuỗi theo từng trình duyệt/origin và còn sau khi tải lại trang.
// Đây là danh sách cá nhân trên máy hiện tại, không phải dữ liệu đồng bộ tài khoản qua API.
const storageKey = 'tin24.bookmarks.v1';

// Type guard kiểm tra dữ liệu thực tế đọc từ storage trước khi tin rằng đó là ArticleSummary.
// JSON có thể hỏng, bị sửa thủ công hoặc đến từ phiên bản ứng dụng cũ.
function isArticle(value: unknown): value is ArticleSummary {
  if (!value || typeof value !== 'object') return false;
  const article = value as Record<string, unknown>;
  return Number.isSafeInteger(article.id) && Number(article.id) > 0
    && ['title', 'summary', 'categorySlug', 'category', 'imageUrl', 'publishedAt', 'author']
      .every(key => typeof article[key] === 'string')
    && typeof article.readingMinutes === 'number';
}

// JSON.parse đổi chuỗi thành dữ liệu; lọc bài sai cấu trúc và loại bản trùng ID.
// Nếu storage bị chặn hoặc JSON không hợp lệ, trả mảng rỗng để ứng dụng vẫn mở được.
function readBookmarks(): ArticleSummary[] {
  try {
    const data: unknown = JSON.parse(localStorage.getItem(storageKey) || '[]');
    return Array.isArray(data)
      ? data.filter(isArticle).filter((article, index, all) => all.findIndex(item => item.id === article.id) === index)
      : [];
  } catch { return []; }
}

export const useBookmarksStore = defineStore('bookmarks', () => {
  // Khởi tạo từ storage một lần khi store được tạo; count là giá trị tính từ danh sách hiện tại.
  const articles = ref<ArticleSummary[]>(readBookmarks());
  const storageError = ref('');
  const count = computed(() => articles.value.length);
  const has = (id: number) => articles.value.some(article => article.id === id);

  // Toggle nghĩa là đảo trạng thái: đã có thì bỏ, chưa có thì thêm ở đầu danh sách bằng unshift.
  function toggle(article: ArticleSummary) {
    if (has(article.id)) articles.value = articles.value.filter(item => item.id !== article.id);
    else {
      // Lưu snapshot danh sách, không lưu nội dung bài chi tiết vào localStorage.
      const { id, title, summary, categorySlug, category, imageUrl, publishedAt, author, readingMinutes } = article;
      articles.value.unshift({ id, title, summary, categorySlug, category, imageUrl, publishedAt, author, readingMinutes });
    }
  }

  // watch theo dõi danh sách để tự ghi lại JSON sau mỗi thay đổi.
  // deep theo dõi cả phần tử bên trong; flush: 'sync' ghi ngay khi state thay đổi.
  // Nếu ghi thất bại, bài vẫn có trong bộ nhớ và App.vue hiển thị storageError cho người dùng.
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
