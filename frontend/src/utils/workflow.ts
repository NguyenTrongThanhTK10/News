import type { AdminArticle, ArticleAction, ArticleStatus, User } from '../types/admin';
export const statusLabels: Record<ArticleStatus, string> = {
  Draft: 'Bản nháp', PendingReview: 'Chờ duyệt', Approved: 'Đã duyệt', Published: 'Đã xuất bản', Rejected: 'Cần chỉnh sửa', Archived: 'Lưu trữ',
};
export function statusSeverity(status: ArticleStatus): 'secondary' | 'warn' | 'info' | 'success' | 'danger' {
  return ({ Draft: 'secondary', PendingReview: 'warn', Approved: 'info', Published: 'success', Rejected: 'danger', Archived: 'secondary' } as const)[status];
}
export const actionLabels: Record<ArticleAction, string> = {
  submit: 'Gửi duyệt', approve: 'Duyệt bài', reject: 'Trả lại', publish: 'Xuất bản', unpublish: 'Gỡ xuất bản', archive: 'Lưu trữ', restore: 'Đưa về nháp',
};
export function allowedActions(item: AdminArticle, user: User | null): ArticleAction[] {
  if (!user) return [];
  const reviewer = user.role === 'Admin' || user.role === 'Editor';
  if (!reviewer && item.authorId !== user.id) return [];
  switch (item.status) {
    case 'Draft': case 'Rejected': return ['submit'];
    case 'PendingReview': return reviewer ? ['approve', 'reject'] : [];
    case 'Approved': return reviewer ? ['publish'] : [];
    case 'Published': return reviewer ? ['unpublish', 'archive'] : [];
    case 'Archived': return reviewer ? ['restore'] : [];
  }
}
export function canDelete(item: AdminArticle, user: User | null) {
  return user?.role === 'Admin' || (user?.id === item.authorId && ['Draft', 'Rejected'].includes(item.status));
}
