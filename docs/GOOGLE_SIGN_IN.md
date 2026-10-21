# Đăng ký độc giả và đăng nhập Google

Khách vẫn đọc được bình luận. Khi nhấn **Gửi bình luận** mà chưa đăng nhập, trang mở form **Đăng ký để bình luận**; có thể chuyển sang đăng nhập bằng email. Tài khoản mới có quyền **Reader / Độc giả**, không truy cập tòa soạn. Sau đăng ký hoặc đăng nhập, người dùng ở lại bài viết và giữ nội dung đang nhập; nhấn gửi lần nữa để đăng bình luận.

Tên hiển thị và mã người dùng của bình luận lấy từ phiên đăng nhập, không lấy từ dữ liệu tự khai báo. Bình luận cũ vẫn được giữ nguyên. Đánh giá sao tiếp tục sử dụng cookie trình duyệt.

## Bật Google khi chưa có Client ID

Tích hợp dùng Google Identity Services, nhận ID token qua JavaScript và xác minh ở backend. Chỉ cần **OAuth Client ID**, không cần Client Secret cho luồng này. Nếu chưa cấu hình, nút Google bị vô hiệu hóa và người dùng vẫn đăng ký bằng email được.

1. Mở [Google Auth Platform](https://console.cloud.google.com/auth/overview), tạo hoặc chọn project.
2. Hoàn thành thông tin ứng dụng tại **Branding** và cấu hình **Audience**. Nếu ứng dụng ở chế độ thử nghiệm và Google yêu cầu, thêm email được phép thử vào danh sách test users.
3. Tại **Clients**, tạo OAuth client loại **Web application**.
4. Thêm **Authorized JavaScript origins** tương ứng URL frontend:

   ```text
   http://127.0.0.1:5173
   http://localhost:5173
   ```

   Khi triển khai, thêm origin HTTPS thật của frontend. Luồng JavaScript callback này không cần khai báo redirect URI.

5. Sao chép Client ID dạng `...apps.googleusercontent.com` và cấu hình backend. Từ thư mục `backend`:

   ```bash
   Google__ClientId='CLIENT_ID_CUA_BAN.apps.googleusercontent.com' dotnet run --project News.Api
   ```

   Hoặc lưu cấu hình Development bằng user-secrets:

   ```bash
   dotnet user-secrets set 'Google:ClientId' 'CLIENT_ID_CUA_BAN.apps.googleusercontent.com' --project News.Api
   dotnet run --project News.Api
   ```

6. Chạy frontend, mở một bài viết, nhấn gửi bình luận → **Tiếp tục với Google**. Backend tự tạo tài khoản Reader khi đăng nhập Google lần đầu. Lần sau nhận diện theo Google `sub`, không dựa vào email.

Không tự động gắn Google vào tài khoản email/mật khẩu có sẵn. Nếu email đã có tài khoản loại này, đăng nhập bằng mật khẩu; hiện chưa có màn hình liên kết hai phương thức. Tài khoản chỉ dùng Google không có mật khẩu cục bộ.

## API và database

- `POST /api/auth/register`: `{ name, email, password }`, mật khẩu 12–200 ký tự; trả access token và cookie refresh. Client không được tự chọn quyền.
- `GET /api/auth/google/options`: trả Client ID và nonce; nonce được lưu trong cookie HttpOnly 10 phút. Không cấu hình Google thì trả hai giá trị null.
- `POST /api/auth/google`: `{ credential }`; backend kiểm tra chữ ký, issuer, audience, thời hạn, email đã xác minh và nonce trước khi cấp phiên.
- `POST /api/articles/{id}/comments`: yêu cầu Bearer token, body `{ content }`; đọc bình luận vẫn công khai.

SQLite Development tự thêm `Users.GoogleSubject`, `Comments.UserId` và vai trò Reader khi bật `Database:Initialize`. SQL Server dùng migration gộp `InitialNewsSchema` hoặc [script khởi tạo đầy đủ](../backend/sql/001-initial-schema.sql) cho database mới. Với database đã chạy migration cũ, xem [hướng dẫn chuyển baseline](SQL_SERVER.md). Schema giữ bình luận cũ với `UserId = null`.

Kiểm thử backend sử dụng verifier giả lập cho các trường hợp Google hợp lệ, token sai và trùng email; kiểm thử trình duyệt giả lập Google SDK, giữ phiên ứng dụng thật. Đăng nhập với Google thật cần Client ID của project, origin hợp lệ và trình duyệt kết nối Google.

Tài liệu Google: [thiết lập Client ID](https://developers.google.com/identity/gsi/web/guides/get-google-api-clientid), [JavaScript API](https://developers.google.com/identity/gsi/web/reference/js-reference), [xác minh ID token](https://developers.google.com/identity/gsi/web/guides/verify-google-id-token).
