import { api } from './http';
import type { AdminArticle, ArticleAction, ArticleStatus, SaveArticleRequest } from '../types/admin';
import type { PagedResult } from '../types/news';
export async function listAdminArticles(query: { page: number; pageSize: number; search?: string; status?: ArticleStatus }, signal?: AbortSignal) {
  return (await api.get<PagedResult<AdminArticle>>('/admin/articles', { params: query, signal })).data;
}
export async function getAdminArticle(id: number, signal?: AbortSignal) {
  return (await api.get<AdminArticle>(`/admin/articles/${id}`, { signal })).data;
}
export async function saveArticle(id: number | null, request: SaveArticleRequest) {
  return (id === null ? await api.post<AdminArticle>('/admin/articles', request) : await api.put<AdminArticle>(`/admin/articles/${id}`, request)).data;
}
export async function transitionArticle(article: AdminArticle, action: ArticleAction, note = '') {
  return (await api.post<AdminArticle>(`/admin/articles/${article.article.id}/${action}`, { version: article.version, note })).data;
}
export async function deleteArticle(article: AdminArticle) {
  await api.delete(`/admin/articles/${article.article.id}`, { params: { version: article.version } });
}
