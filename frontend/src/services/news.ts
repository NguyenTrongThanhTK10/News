import axios from 'axios';
import type { Article, ArticleSummary, Category, PagedResult } from '../types/news';

import { api } from './http';

// Service gom cách gọi API vào một nơi; view chỉ cần truyền bộ lọc và nhận dữ liệu.
// Các thuộc tính có ? là tùy chọn, ví dụ getArticles({ category: 'cong-nghe', page: 2 }).
export interface ArticleQuery {
  search?: string;
  category?: string;
  page?: number;
  pageSize?: number;
}

// params được Axios đổi thành query string. signal cho phép view hủy yêu cầu đã lỗi thời.
// <PagedResult<ArticleSummary>> mô tả response.data cho TypeScript, không kiểm tra JSON lúc chạy.
export async function getArticles(query: ArticleQuery = {}, signal?: AbortSignal) {
  const response = await api.get<PagedResult<ArticleSummary>>('/articles', { params: query, signal });
  return response.data;
}

// Template literal `${id}` chèn mã bài vào đường dẫn; trả data để view không phụ thuộc Axios response.
export async function getArticle(id: number, signal?: AbortSignal): Promise<Article> {
  const response = await api.get<Article>(`/articles/${id}`, { signal });
  return response.data;
}

// Danh sách chuyên mục được categories store giữ dùng chung cho header, bộ lọc và form soạn bài.
export async function getCategories(signal?: AbortSignal): Promise<Category[]> {
  const response = await api.get<Category[]>('/categories', { signal });
  return response.data;
}

// Tách lỗi 404 (bài không tồn tại/không công khai) khỏi lỗi mạng để view hiển thị đúng trạng thái.
export function isNotFound(error: unknown): boolean {
  return axios.isAxiosError(error) && error.response?.status === 404;
}
