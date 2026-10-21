import { api } from './http';
import type { AdminArticle, ArticleAction, ArticleStatus, SaveArticleRequest } from '../types/admin';
import type { PagedResult } from '../types/news';
// API quản trị dùng chung api instance nên token được http.ts tự gắn vào request.
// Backend quyết định dữ liệu/quyền thực tế; bộ lọc frontend chỉ giúp người dùng thao tác.
export async function listAdminArticles(query: { page: number; pageSize: number; search?: string; status?: ArticleStatus }, signal?: AbortSignal) {
  return (await api.get<PagedResult<AdminArticle>>('/admin/articles', { params: query, signal })).data;
}
// Bản quản trị gồm nội dung bài và thông tin như trạng thái, version, góp ý biên tập.
export async function getAdminArticle(id: number, signal?: AbortSignal) {
  return (await api.get<AdminArticle>(`/admin/articles/${id}`, { signal })).data;
}
// id = null: POST tạo bài; có id: PUT cập nhật. Cả hai đều trả bản bài viết mới từ server.
export async function saveArticle(id: number | null, request: SaveArticleRequest) {
  return (id === null ? await api.post<AdminArticle>('/admin/articles', request) : await api.put<AdminArticle>(`/admin/articles/${id}`, request)).data;
}
// Chuyển trạng thái bằng endpoint riêng; note dùng khi trả bài, version giúp phát hiện dữ liệu cũ.
export async function transitionArticle(article: AdminArticle, action: ArticleAction, note = '') {
  return (await api.post<AdminArticle>(`/admin/articles/${article.article.id}/${action}`, { version: article.version, note })).data;
}
// DELETE không cần dữ liệu trả về; version đi trong query string để server kiểm tra trước khi xóa.
export async function deleteArticle(article: AdminArticle) {
  await api.delete(`/admin/articles/${article.article.id}`, { params: { version: article.version } });
}
