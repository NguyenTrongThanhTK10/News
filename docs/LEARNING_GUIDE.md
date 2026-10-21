# Năm phần học trong dự án

Mở **`/hoc-vue`** để xem đủ 5 phần và thử ví dụ tương tác. Mở **`/quan-tri`** để thực hành nghiệp vụ với API/database thật.

## 01 — Vue căn bản

| Khái niệm | Thực hành | Mã nguồn |
| --- | --- | --- |
| Component và template | Sửa tiêu đề ở component cha, xem thẻ bài con cập nhật | `LearningView.vue`, `LearningArticlePreview.vue` |
| `ref` | Tiêu đề, chuyên mục, hiện/ẩn, đã lưu | `LearningView.vue` |
| `computed` | Đếm ký tự, lọc danh sách bài học | `LearningView.vue` |
| `v-if` | Bật/tắt component xem trước; loading/error/empty | `LearningView.vue`, `HomeView.vue` |
| `v-for` | Render năm phần học, thẻ tin, danh sách trạng thái | `LearningView.vue`, `HomeView.vue` |
| `v-model` | Form thực hành, form đăng nhập, form soạn bài | `LearningView.vue`, `LoginView.vue`, `ArticleEditorView.vue` |
| Props / emits | Component con gửi sự kiện `toggleSave` lên cha | `LearningArticlePreview.vue` |

Bài tập: thêm trường mô tả vào ví dụ, truyền qua props, tính số từ bằng computed và dùng v-if để cảnh báo khi tiêu đề trống.

## 02 — PrimeVue và giao diện

| Component | Nơi dùng thật |
| --- | --- |
| Button | Đăng nhập, lưu nháp, duyệt, xuất bản, thử lại |
| InputText | Tìm kiếm tòa soạn, email, tiêu đề, ảnh minh họa |
| Tag | Trạng thái bản nháp/chờ duyệt/đã duyệt/đã xuất bản |
| DataTable + Column | Bảng quản lý bài, phân trang server, bảng bài học |
| Dialog | Xác nhận thay đổi trạng thái/xóa, trả bài kèm góp ý, xem trước, xác nhận rời trang chưa lưu |
| Toast | Báo lưu nháp, cập nhật trạng thái và ví dụ tương tác |
| Editor | Soạn thảo văn bản có định dạng qua Quill |
| Select, Textarea, InputNumber, Password, Message | Các trường form, thông báo và bộ lọc |

Theme được đăng ký trong `main.ts`; `ToastService` đi cùng một `Toast` ở `App.vue`. `style.css` xử lý giao diện desktop/mobile, cuộn ngang bên trong bảng và menu tòa soạn trên mobile.

Bài tập: đổi màu trạng thái, thêm cột thời gian đọc, điều chỉnh bố cục form khi màn hình nhỏ.

## 03 — Vue Router và Axios

- `router/index.ts`: route trang tin, `:id`, trang 404, route quản trị có guard.
- `services/http.ts`: một Axios instance, base URL, timeout, Bearer token, refresh một lần khi access token hết hạn, chuẩn hóa lỗi Problem Details.
- `services/news.ts`: API đọc công khai.
- `services/admin.ts`: API CRUD và quy trình biên tập.
- `ArticleView.vue`: watch ID, abort request cũ, phân biệt 404 với lỗi kết nối.
- `HomeView.vue`: query string giữ tìm kiếm/chuyên mục/phân trang, hỗ trợ Back/Forward.

Bài tập: mở `/bai-viet/1`, `/bai-viet/999999`, tắt API rồi thử nút tải lại. Kiểm tra tab Network để đọc request/response.

## 04 — Backend và SQL Server

Mở `backend/News.sln`:

1. `News.Domain`: Article, Category, User, LoginSession, enum trạng thái, quy tắc chuyển trạng thái và hợp đồng repository.
2. `News.Application`: DTO, validation hợp đồng, use case đọc bài và `ArticleManagementService` kiểm tra quyền/sở hữu/phiên bản.
3. `News.Infrastructure`: `NewsDbContext`, EF Core repository, SQL Server provider, migrations, PasswordHasher, JWT và lọc HTML.
4. `News.Api`: controller mỏng, DI, authentication/authorization, CORS, rate limit, Problem Details và health check database.
5. `News.Tests`: test nghiệp vụ và HTTP integration bằng database SQLite tách biệt.

CRUD bài viết: POST tạo, GET danh sách/chi tiết, PUT sửa, DELETE xóa mềm. Truy vấn phân trang ở database; danh sách trả DTO tóm tắt. `Version` là concurrency token: cập nhật bản cũ nhận `409` thay vì ghi đè.

Theo [SQL_SERVER.md](SQL_SERVER.md) để dựng SQL Server, cấu hình connection string và áp dụng migrations. SQLite Development lưu dữ liệu vào file; khởi động lại backend vẫn giữ bài viết.

Bài tập: thêm trường nguồn tin vào entity/DTO/form rồi tạo migration SQL Server mới.

## 05 — Quản trị tin tức

Dùng ba tài khoản Development trong README để thực hành:

1. Đăng nhập **Author**, soạn bài bằng Editor, xem trước và lưu nháp.
2. Gửi duyệt từ danh sách bài viết. Bản nháp/chờ duyệt chưa xuất hiện công khai.
3. Đăng nhập **Editor** ở trình duyệt khác, mở bài và trả lại kèm lý do hoặc duyệt.
4. Tác giả sửa bài bị trả lại và gửi duyệt lại.
5. Biên tập viên duyệt rồi xuất bản. Chỉ sau bước này bài xuất hiện ở API/trang tin công khai.
6. Gỡ xuất bản để sửa; sửa một bài đã duyệt đưa bài về nháp, bắt buộc duyệt lại.
7. Đăng nhập **Admin** để quản lý/xóa nội dung toàn tòa soạn.

JWT được xác thực issuer, audience, chữ ký, thời hạn và phiên còn tồn tại trong DB. Frontend giữ access token trong bộ nhớ; refresh token dùng cookie HttpOnly và chỉ lưu hash trong DB. API kiểm tra quyền thực sự, guard/nút ẩn ở frontend chỉ hỗ trợ trải nghiệm. HTML được lọc ở cả server và trình xem trước/công khai.

| Thao tác | Author | Editor | Admin |
| --- | --- | --- | --- |
| Tạo bài nháp | Có | Có | Có |
| Xem/sửa bản nháp | Của mình | Toàn bộ | Toàn bộ |
| Gửi duyệt | Của mình | Toàn bộ | Toàn bộ |
| Duyệt/trả lại | Không | Có | Có |
| Xuất bản/gỡ/lưu trữ | Không | Có | Có |
| Xóa | Nháp/bị trả lại của mình | Nháp/bị trả lại của mình | Toàn bộ |

Bài tập: mở cùng bài ở hai tab, sửa/lưu ở tab thứ nhất rồi thử lưu ở tab thứ hai để quan sát lỗi `409`. Thử gọi API xuất bản bằng token Author để thấy `403`.
