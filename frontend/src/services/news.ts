import axios from 'axios';
import type { Article, ArticleSummary, Category, PagedResult } from '../types/news';

import { api } from './http';

export interface ArticleQuery {
  search?: string;
  category?: string;
  page?: number;
  pageSize?: number;
}

export async function getArticles(query: ArticleQuery = {}, signal?: AbortSignal) {
  const response = await api.get<PagedResult<ArticleSummary>>('/articles', { params: query, signal });
  return response.data;
}

export async function getArticle(id: number, signal?: AbortSignal): Promise<Article> {
  const response = await api.get<Article>(`/articles/${id}`, { signal });
  return response.data;
}

export async function getCategories(signal?: AbortSignal): Promise<Category[]> {
  const response = await api.get<Category[]>('/categories', { signal });
  return response.data;
}

export function isNotFound(error: unknown): boolean {
  return axios.isAxiosError(error) && error.response?.status === 404;
}
