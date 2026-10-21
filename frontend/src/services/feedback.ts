import { api } from './http';
import type { ArticleComment, PagedResult, RatingStatistics } from '../types/news';

// API phản hồi gắn với ID bài viết. GET đọc dữ liệu, POST gửi dữ liệu mới/cập nhật đánh giá.
// Mỗi trang bình luận có 10 mục; signal giúp ngừng request khi rời bài hoặc chuyển trang.
export async function getComments(id: number, page = 1, signal?: AbortSignal) {
  const { data } = await api.get<PagedResult<ArticleComment>>(`/articles/${id}/comments`, {
    params: { page, pageSize: 10 }, signal,
  });
  return data;
}

// { data } là destructuring: lấy trực tiếp trường data từ Axios response.
// Tên và tài khoản bình luận được backend lấy từ phiên đăng nhập.
export async function postComment(id: number, content: string, signal?: AbortSignal) {
  const { data } = await api.post<ArticleComment>(`/articles/${id}/comments`, { content }, { signal });
  return data;
}

// Server trả cả điểm chung lẫn myRating dựa trên cookie của trình duyệt hiện tại.
export async function getRatings(id: number, signal?: AbortSignal) {
  const { data } = await api.get<RatingStatistics>(`/articles/${id}/ratings`, { signal });
  return data;
}

// Sau khi gửi sao, dùng thống kê server trả về thay vì tự tính điểm trung bình ở frontend.
export async function rateArticle(id: number, stars: number, signal?: AbortSignal) {
  const { data } = await api.post<RatingStatistics>(`/articles/${id}/ratings`, { stars }, { signal });
  return data;
}
