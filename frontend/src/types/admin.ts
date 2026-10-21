import type { Article } from './news';
export type Role = 'Admin' | 'Editor' | 'Author';
export type ArticleStatus = 'Draft' | 'PendingReview' | 'Approved' | 'Published' | 'Rejected' | 'Archived';
export interface User { id: string; name: string; email: string; role: Role; }
export interface AuthResponse { accessToken: string; expiresAt: string; user: User; }
export interface AdminArticle {
  article: Omit<Article, 'publishedAt'> & { publishedAt: string | null };
  status: ArticleStatus; authorId: string; version: string; reviewNote: string; createdAt: string; updatedAt: string;
}
export interface SaveArticleRequest {
  title: string; summary: string; content: string; categorySlug: string; imageUrl: string;
  readingMinutes: number; version?: string;
}
export type ArticleAction = 'submit' | 'approve' | 'reject' | 'publish' | 'unpublish' | 'archive' | 'restore';
