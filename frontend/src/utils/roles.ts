import type { Role } from '../types/admin';

export const roleLabels: Record<Role, string> = { Admin: 'Quản trị viên', Editor: 'Biên tập viên', Author: 'Tác giả', Reader: 'Độc giả' };
export const roleDescriptions: Record<Role, string> = {
  Reader: 'Đọc bài viết và gửi bình luận bằng tài khoản; không truy cập tòa soạn.',
  Admin: 'Quản lý toàn bộ bài viết, người dùng, vai trò và danh mục.',
  Editor: 'Duyệt, trả lại, xuất bản và quản lý quy trình biên tập bài viết.',
  Author: 'Soạn, chỉnh sửa bản nháp của mình và gửi bài để duyệt.',
};
export const roleOptions = Object.entries(roleLabels).map(([value, label]) => ({ value, label }));
