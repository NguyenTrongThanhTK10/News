export interface ArticleSummary {
  id: number;
  title: string;
  summary: string;
  categorySlug: string;
  category: string;
  imageUrl: string;
  publishedAt: string;
  author: string;
  readingMinutes: number;
}

export interface Article extends ArticleSummary {
  content: string;
}

export interface Category {
  slug: string;
  name: string;
  count: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
