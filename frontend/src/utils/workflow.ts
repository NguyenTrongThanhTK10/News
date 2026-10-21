import type { AdminArticle, ArticleAction, ArticleStatus, User } from '../types/admin';
// Tách quy tắc hiển thị khỏi view để danh sách và trình soạn bài dùng cùng nhãn trạng thái.
// Record yêu cầu mỗi ArticleStatus đều có nhãn, giúp TypeScript phát hiện trường hợp bỏ sót.
export const statusLabels: Record<ArticleStatus, string> = {
  Draft: 'Bản nháp', PendingReview: 'Chờ duyệt', Approved: 'Đã duyệt', Published: 'Đã xuất bản', Rejected: 'Cần chỉnh sửa', Archived: 'Lưu trữ',
};
// severity là tên màu ngữ nghĩa PrimeVue nhận cho Tag, không phải mã màu CSS.
export function statusSeverity(status: ArticleStatus): 'secondary' | 'warn' | 'info' | 'success' | 'danger' {
  return ({ Draft: 'secondary', PendingReview: 'warn', Approved: 'info', Published: 'success', Rejected: 'danger', Archived: 'secondary' } as const)[status];
}
// Khóa dùng khi gọi API; giá trị là nhãn tiếng Việt trên nút thao tác/xác nhận.
export const actionLabels: Record<ArticleAction, string> = {
  submit: 'Gửi duyệt', approve: 'Duyệt bài', reject: 'Trả lại', publish: 'Xuất bản', unpublish: 'Gỡ xuất bản', archive: 'Lưu trữ', restore: 'Đưa về nháp',
};
// Quyền trên UI phụ thuộc cả vai trò, chủ sở hữu và trạng thái hiện tại của bài.
// Hàm chỉ chọn nút nên hiện; backend vẫn kiểm tra lại mọi yêu cầu để thực thi phân quyền.
export function allowedActions(item: AdminArticle, user: User | null): ArticleAction[] {
  if (!user) return [];
  const reviewer = user.role === 'Admin' || user.role === 'Editor';
  if (!reviewer && item.authorId !== user.id) return [];
  // Ví dụ: nháp được gửi duyệt; bài chờ duyệt chỉ Editor/Admin mới được duyệt hoặc trả lại.
  switch (item.status) {
    case 'Draft': case 'Rejected': return ['submit'];
    case 'PendingReview': return reviewer ? ['approve', 'reject'] : [];
    case 'Approved': return reviewer ? ['publish'] : [];
    case 'Published': return reviewer ? ['unpublish', 'archive'] : [];
    case 'Archived': return reviewer ? ['restore'] : [];
  }
}
// Admin được xóa mọi trạng thái; người sở hữu chỉ được xóa bài nháp hoặc bị trả lại.
export function canDelete(item: AdminArticle, user: User | null) {
  return user?.role === 'Admin' || (user?.id === item.authorId && ['Draft', 'Rejected'].includes(item.status));
}
