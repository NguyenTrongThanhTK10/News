# Kết nối SQL Server cho TIN24

Dự án đã có EF Core SQL Server provider, migration và script tạo schema. **Chưa có SQL Server trên máy hiện tại**, nên chế độ Development chạy SQLite để có thể học ngay. Hai chế độ dùng chung entity, repository và use case; SQLite không thay thế việc kiểm thử trên SQL Server trước khi triển khai.

## 1. Chuẩn bị một SQL Server

Chọn một trong hai cách:

- **Windows:** cài SQL Server Developer và SQL Server Management Studio. Bật TCP/IP, tạo tài khoản đăng nhập cho ứng dụng và xác định cổng (thường là 1433).
- **Linux/Windows máy x64 có Docker:** dùng file `backend/compose.sqlserver.yml` bên dưới. SQL Server Linux container yêu cầu môi trường x86-64 được hỗ trợ. Với Mac Apple Silicon hiện tại, dùng SQL Server trên máy Windows/Linux khác hoặc dịch vụ SQL Server/Azure SQL từ xa; vẫn dùng SQLite để chạy bài học tại chỗ.

Tài liệu chính thức: [SQL Server Linux container](https://learn.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker?view=sql-server-ver17&tabs=cli), [SQL Server Developer](https://www.microsoft.com/en-us/sql-server/sql-server-downloads).

Trên máy x64 đã cài Docker, tạo `backend/.env` (đã được gitignore):

```dotenv
MSSQL_SA_PASSWORD=ThayBangMatKhauManhCuaBan!2026
```

Từ `backend/` chạy:

```bash
docker compose -f compose.sqlserver.yml up -d
docker compose -f compose.sqlserver.yml logs sqlserver
```

Container chỉ mở cổng 1433 trên loopback của máy chạy Docker. Muốn kết nối từ Mac đến máy khác, cấu hình mạng/firewall riêng cho máy chủ và tài khoản ứng dụng. Volume `tin24-sql-data` giữ dữ liệu khi container khởi động lại.

## 2. Cấu hình ứng dụng

Dùng user-secrets trong Development hoặc biến môi trường khi triển khai. Không đưa mật khẩu thật vào mã nguồn.

Từ `backend/`:

```bash
dotnet user-secrets set 'ConnectionStrings:NewsDatabase' 'Server=localhost,1433;Database=Tin24;User Id=sa;Password=MAT_KHAU_CUA_BAN;Encrypt=True;TrustServerCertificate=True' --project News.Api
```

`sa` và `TrustServerCertificate=True` chỉ phù hợp bài thực hành trên máy riêng. Khi triển khai, dùng tài khoản database riêng với quyền cần thiết và chứng chỉ TLS hợp lệ.

Để chạy với SQL Server ngay trong Development:

```bash
Database__Provider=SqlServer Database__Initialize=true dotnet run --project News.Api
```

Backend áp dụng migration SQL Server rồi seed dữ liệu mẫu (do `Seed:DemoData` trong Development). Sau đó kiểm tra:

- `http://localhost:5080/health` phải trả `Healthy`.
- `http://localhost:5080/api/articles` phải trả dữ liệu phân trang.
- Tạo một bản nháp, dừng/chạy lại backend và mở bản nháp để kiểm tra dữ liệu đã được lưu.

Chuyển lại SQLite bằng `dotnet run --project News.Api`. Hai database độc lập, không tự đồng bộ dữ liệu cho nhau.

## 3. Migrations và script

Các migration trong `News.Infrastructure/Migrations/SqlServer` được sinh cho **SQL Server**. SQLite Development dùng `EnsureCreatedAsync()` để dựng database thực hành mới, không chạy migration SQL Server.

Cài công cụ từ manifest đã có trong repository:

```bash
dotnet tool restore
```

Sinh script có thể chạy lại an toàn theo bảng lịch sử migration:

```bash
dotnet ef migrations script --idempotent --project News.Infrastructure --startup-project News.Api --output sql/001-initial-schema.sql
```

Script đã sinh sẵn: [`backend/sql/001-initial-schema.sql`](../backend/sql/001-initial-schema.sql). Script cần được chạy trong database `Tin24` đã tạo, bằng SSMS/sqlcmd. Script chỉ tạo schema, không chứa mật khẩu hay tài khoản ứng dụng.

Chức năng bình luận/đánh giá bổ sung migration `AddArticleFeedback`. Database đã chạy schema ban đầu cần áp dụng [`backend/sql/002-article-feedback.sql`](../backend/sql/002-article-feedback.sql), hoặc dùng `dotnet ef database update` để áp dụng mọi migration còn thiếu. Với database mới, chạy `001` rồi `002`; script `002` chỉ thêm hai bảng cùng chỉ mục/ràng buộc, không thay đổi bài viết hay tài khoản hiện có. Script này đã được sinh bằng provider SQL Server nhưng chưa chạy trên SQL Server thật.

Áp dụng migration qua CLI (cần đặt connection string qua biến môi trường):

```bash
export ConnectionStrings__NewsDatabase='Server=...;Database=Tin24;User Id=...;Password=...;Encrypt=True'
dotnet ef database update --project News.Infrastructure --startup-project News.Api
```

`NewsDbContextFactory` dùng SQL Server ở design-time, không gọi seed hay đăng nhập, và không kết nối server khi chỉ sinh migration/script. Khi thay đổi entity:

```bash
dotnet ef migrations add TenThayDoi --project News.Infrastructure --startup-project News.Api --output-dir Migrations/SqlServer
```

Không dùng `EnsureCreated` lên database SQL Server đã quản lý bằng migrations. Riêng chức năng bình luận/đánh giá có `SqliteFeedbackSchema.UpgradeAsync`: khi bật `Database:Initialize`, ứng dụng bổ sung idempotent hai bảng vào SQLite hiện có, không xóa dữ liệu. Những thay đổi schema khác vẫn cần kế hoạch migration riêng.

## 4. Khởi tạo môi trường triển khai

`appsettings.json` mặc định chọn SQL Server, tắt tự khởi tạo database và không có signing key/mật khẩu. Cấu hình tối thiểu:

```dotenv
ASPNETCORE_ENVIRONMENT=Production
Database__Provider=SqlServer
ConnectionStrings__NewsDatabase=Server=...;Database=Tin24;User Id=...;Password=...;Encrypt=True
Jwt__SigningKey=CHUOI_NGAU_NHIEN_BI_MAT_TOI_THIEU_32_BYTE
Cors__AllowedOrigins__0=https://tin24.example.com
Seed__AdminEmail=admin@example.com
Seed__AdminPassword=MAT_KHAU_MANH_IT_NHAT_12_KY_TU
```

Trong lần khởi tạo có kiểm soát, đặt `Database__Initialize=true` để áp dụng migration, tạo chuyên mục và tài khoản quản trị đầu tiên. Sau đó đặt lại `false` và bỏ biến `Seed__AdminPassword` khỏi cấu hình chạy thường ngày. Không bật dữ liệu/tài khoản demo trong Production. Có thể thêm tài khoản nhân sự bằng cấu hình `Seed:StaffUsers` trong lần khởi tạo (xem README); mật khẩu luôn được hash bằng ASP.NET Core PasswordHasher trước khi lưu.

Frontend và backend nên được phục vụ cùng site, chuyển tiếp `/api` qua reverse proxy. Cookie refresh là `HttpOnly`, `SameSite=Strict`, bắt buộc `Secure` khi Production; vì vậy môi trường triển khai phải dùng HTTPS. Access token JWT sống 15 phút trong bộ nhớ frontend, refresh session sống tối đa 8 giờ và bị thu hồi khi đăng xuất.
