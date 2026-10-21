import type { Article } from './news';
// Union type (các giá trị nối bằng |) giới hạn chuỗi được phép dùng, tránh gõ sai tên.
// Giá trị phải khớp hợp đồng API; nhãn tiếng Việt được ánh xạ trong utils/workflow.ts.
export type Role = 'Admin' | 'Editor' | 'Author';
export type ArticleStatus = 'Draft' | 'PendingReview' | 'Approved' | 'Published' | 'Rejected' | 'Archived';
// Chỉ giữ thông tin người dùng phục vụ giao diện, không có mật khẩu trong kiểu này.
export interface User { id: string; name: string; email: string; role: Role; }
// accessToken được http.ts giữ trong bộ nhớ. Cookie refresh do trình duyệt và server quản lý.
export interface AuthResponse { accessToken: string; expiresAt: string; user: User; }
export interface AdminArticle {
  // Omit bỏ publishedAt của Article rồi khai báo lại: bản nháp có thể chưa có ngày xuất bản.
  article: Omit<Article, 'publishedAt'> & { publishedAt: string | null };
  // version là mã phiên bản server cấp, gửi lại khi sửa/chuyển trạng thái/xóa để tránh ghi đè.
  status: ArticleStatus; authorId: string; version: string; reviewNote: string; createdAt: string; updatedAt: string;
}
// Dữ liệu gửi từ form tới API. Dấu ? ở version nghĩa là có thể bỏ qua khi tạo bài mới.
export interface SaveArticleRequest {
  title: string; summary: string; content: string; categorySlug: string; imageUrl: string;
  readingMinutes: number; version?: string;
}
// Tên thao tác tương ứng đoạn cuối endpoint /admin/articles/:id/:action.
export type ArticleAction = 'submit' | 'approve' | 'reject' | 'publish' | 'unpublish' | 'archive' | 'restore';
