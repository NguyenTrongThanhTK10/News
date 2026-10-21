// Interface mô tả hình dạng dữ liệu để TypeScript phát hiện lỗi khi viết code.
// Nó không tự kiểm tra JSON từ API lúc chạy. ArticleSummary chỉ chứa dữ liệu cho thẻ tin.
export interface ArticleSummary {
  id: number;
  title: string;
  summary: string;
  // Slug là mã dùng trong URL, ví dụ "cong-nghe"; category là tên hiển thị "Công nghệ".
  categorySlug: string;
  category: string;
  imageUrl: string;
  // API gửi ngày giờ dưới dạng chuỗi; utils/format.ts chuyển sang ngày hiển thị ở Việt Nam.
  publishedAt: string;
  author: string;
  readingMinutes: number;
}

// extends kế thừa toàn bộ trường của bản tóm tắt và thêm nội dung HTML cho trang đọc bài.
export interface Article extends ArticleSummary {
  content: string;
}

// count là tổng số bài trong chuyên mục, dùng ở phần khám phá chủ đề.
export interface Category {
  slug: string;
  name: string;
  count: number;
}

// Generic <T> cho phép tái sử dụng kiểu phân trang: PagedResult<ArticleSummary>,
// PagedResult<ArticleComment>... items chỉ chứa trang hiện tại, totalCount tính mọi trang.
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// Bình luận là văn bản thuần; template hiển thị bằng {{ }} thay vì chèn HTML.
export interface ArticleComment {
  id: string;
  displayName: string;
  content: string;
  createdAt: string;
}

// Thống kê do API tính. myRating = null nghĩa là trình duyệt hiện tại chưa có đánh giá.
export interface RatingStatistics {
  average: number;
  count: number;
  myRating: number | null;
  /** Số lượt theo từng mức sao: phần tử [0] là 1 sao, [4] là 5 sao. */
  distribution: number[];
}
