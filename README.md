# TIN24 — Học Vue 3, PrimeVue và ASP.NET Core qua dự án tin tức

Ứng dụng tin tức tiếng Việt với trang tin công khai, khu vực học tương tác và tòa soạn quản trị. Backend có **solution `.sln`, EF Core, CRUD, SQL Server provider, migrations, JWT và phân quyền**. Frontend dùng **Vue 3, TypeScript, Pinia, Vue Router, Axios và PrimeVue**.

## Đủ 5 phần học

| Phần | Nội dung đã triển khai | Thực hành |
| --- | --- | --- |
| **01 · Vue căn bản** | Component/template, ref, computed, v-if, v-for, v-model, props/emits | `/hoc-vue` — sửa dữ liệu và xem component con cập nhật |
| **02 · PrimeVue và giao diện** | Button, InputText, Tag, DataTable, Dialog, Toast, responsive; thêm Editor, Select, Password, Textarea, Message | `/hoc-vue`, `/quan-tri` |
| **03 · Vue Router và Axios** | Điều hướng, ID bài viết, gọi API C#, loading/error/404, abort request, query string, guard, interceptor | Trang chủ, `/bai-viet/:id`, tòa soạn |
| **04 · Backend và SQL Server** | ASP.NET Core 8, EF Core, DTO, CRUD, lưu database, phân trang, migrations SQL Server | `backend/News.sln`, [hướng dẫn SQL Server](docs/SQL_SERVER.md) |
| **05 · Quản trị tin tức** | Đăng nhập JWT, phiên refresh, tạo người dùng/vai trò/danh mục, phân quyền, soạn thảo, xem trước, nháp, gửi duyệt, trả bài, duyệt, xuất bản, gỡ bài và xóa | `/quan-tri` |

[Hướng dẫn học theo từng phần và vị trí mã nguồn](docs/LEARNING_GUIDE.md).

## Chạy ngay trên máy hiện tại

Cần **Node.js ≥ 20.19**, npm và **.NET SDK 8**. `backend/global.json` chọn SDK 8; mở [`backend/News.sln`](backend/News.sln) trong IDE hỗ trợ .NET.

Terminal backend, từ thư mục gốc:

```bash
cd backend
dotnet restore News.sln
dotnet run --project News.Api
```

Terminal frontend:

```bash
cd frontend
# Nếu dùng nvm: nvm install && nvm use
npm ci
npm run dev
```

Chạy các lệnh npm trong thư mục `frontend`, nơi có `package.json`. Nếu gặp `ENOENT ... News/package.json`, hãy chạy `cd frontend` trước. Nếu gặp `EBADENGINE`, kiểm tra `node -v`; với nvm, chạy `nvm install` rồi `nvm use` trong chính terminal đó để dùng Node 20.19 theo `frontend/.nvmrc`.

- Trang tin: <http://127.0.0.1:5173>
- Năm phần học: <http://127.0.0.1:5173/hoc-vue>
- Tòa soạn: <http://127.0.0.1:5173/quan-tri>
- Backend/health: <http://localhost:5080/health>

**Development mặc định dùng SQLite**, tạo `backend/News.Api/news-dev.db` cùng dữ liệu minh họa. Bài mới được lưu thật và còn sau khi khởi động lại. SQL Server chưa được cài trên máy hiện tại; xem [SQL_SERVER.md](docs/SQL_SERVER.md) để dựng server và chuyển provider. Khi triển khai Production, cấu hình mặc định yêu cầu SQL Server.

Ảnh minh họa từ Unsplash cần Internet; font tiếng Việt được đóng gói nội bộ. 12 bài mẫu phục vụ học tập, không phải tin tức thực tế.

## Tài khoản thực hành

Chỉ được seed khi **Development/Testing** và `Seed:DemoData=true`:

| Vai trò | Email | Mật khẩu |
| --- | --- | --- |
| Quản trị viên | `admin@tin24.local` | `Tin24@Admin2026!` |
| Biên tập viên | `editor@tin24.local` | `Tin24@Editor2026!` |
| Tác giả | `author@tin24.local` | `Tin24@Author2026!` |

Đăng nhập Author để tạo bài; dùng cửa sổ trình duyệt khác với Editor để duyệt/xuất bản. Tài khoản/mật khẩu này chỉ dành cho dữ liệu thực hành. Production không tạo tài khoản demo.

Menu **Quản lý tin tức** gồm **Quản lý bài viết**, **Soạn bài mới** và **Danh mục bài viết** (dành cho Admin). Menu **Quản lý tài khoản** dành cho Admin gồm **Người dùng**, **Vai trò**:

- **Người dùng** (`/quan-tri/nguoi-dung`): tạo tài khoản với họ tên, email, mật khẩu 12–200 ký tự và vai trò; tìm kiếm, phân trang và đổi vai trò cho tài khoản hiện có. Đổi vai trò thu hồi các phiên của tài khoản đó và yêu cầu đăng nhập lại; không thể tự gỡ quyền Admin.
- **Vai trò** (`/quan-tri/vai-tro`): tạo vai trò có tên riêng, kế thừa đầy đủ một mức quyền **Admin**, **Editor**, **Author** hoặc **Reader**. Ví dụ, “Cộng tác viên” kế thừa Author. Chọn vai trò này khi tạo hoặc phân quyền người dùng.
- **Danh mục bài viết** (`/quan-tri/danh-muc`): tạo tên và slug duy nhất; tự gợi ý slug không dấu từ tên. Danh mục mới xuất hiện trong lựa chọn chuyên mục khi soạn bài và trên trang tin. Số bài tại trang quản trị tính cả bản nháp, còn trang tin chỉ đếm bài công khai.

Giao diện quản trị dùng sidebar tối màu, bảng dữ liệu kèm ảnh thu nhỏ và các thẻ thống kê lấy số liệu từ API. Nhấn thẻ trạng thái bài viết để lọc nhanh; số liệu thống kê tính theo phạm vi quyền của tài khoản và được cập nhật sau thao tác biên tập. Trên điện thoại, menu mở dạng ngăn kéo và bảng cuộn ngang trong khung. Kiểu dáng quản trị được tập trung tại `frontend/src/admin.css`.

Khi bật khởi tạo dữ liệu, hệ thống bổ sung các danh mục còn thiếu: Thời sự, Thế giới, Kinh tế, Pháp luật, Sức khỏe, Khoa học, Giải trí, Xe, Bất động sản, Công nghệ, Đời sống, Du lịch, Văn hóa, Giáo dục và Thể thao. Danh mục đã có và các danh mục tự tạo được giữ nguyên.

API kiểm tra quyền Admin cho cả ba nhóm thao tác; Editor/Author không truy cập được các trang và API này. SQLite Development tự nâng cấp schema khi khởi động lại với `Database:Initialize=true`, giữ nguyên tài khoản và bài viết hiện có. SQL Server dùng migration khởi tạo duy nhất `InitialNewsSchema`, bao gồm cả quản trị tài khoản/vai trò; xem [cách áp dụng và xử lý lịch sử cũ](docs/SQL_SERVER.md).

## Quy trình biên tập và quyền

```text
Nháp ──gửi duyệt──→ Chờ duyệt ──duyệt──→ Đã duyệt ──xuất bản──→ Công khai
                         │
                      trả lại
                         ↓
                  Cần chỉnh sửa ──sửa/lưu──→ Nháp

Công khai ──gỡ xuất bản──→ Nháp
Công khai ──lưu trữ──→ Lưu trữ ──khôi phục──→ Nháp
```

| Thao tác | Author | Editor | Admin |
| --- | --- | --- | --- |
| Tạo nháp | Có | Có | Có |
| Xem/sửa/gửi duyệt | Bài của mình | Toàn bộ | Toàn bộ |
| Duyệt/trả lại kèm góp ý | Không | Có | Có |
| Xuất bản/gỡ/lưu trữ/khôi phục | Không | Có | Có |
| Xóa mềm | Nháp/bị trả lại của mình | Nháp/bị trả lại của mình | Toàn bộ |

Bài đang chờ duyệt, đã xuất bản hoặc lưu trữ không được sửa trực tiếp. Sửa bài đã duyệt chuyển về nháp và cần duyệt lại. API công khai chỉ trả bài đã xuất bản và chưa xóa. Backend kiểm tra vai trò, chủ sở hữu, trạng thái và `Version`; frontend ẩn nút theo quyền để hỗ trợ trải nghiệm.

## Chia sẻ bài viết

Nút **Chia sẻ** bên cạnh nút lưu và mục chia sẻ cuối bài mở lựa chọn **Facebook**, **X**, **email**, **sao chép liên kết**. Trình duyệt có Web Share API sẽ hiện thêm **Ứng dụng khác** để mở menu chia sẻ của thiết bị. Chia sẻ không yêu cầu tài khoản TIN24.

Liên kết luôn trỏ đến bài đang đọc, bỏ query/hash. Nếu trình duyệt chặn Clipboard API, hộp thoại chọn sẵn đường dẫn để sao chép thủ công. Đóng menu chia sẻ của thiết bị không hiển thị lỗi. Không tự đăng bài lên tài khoản mạng xã hội; người đọc tiếp tục xác nhận ở ứng dụng đã chọn.

## Kiến trúc

```text
backend/
├── News.sln
├── News.Domain/             Entity, enum trạng thái, quy tắc chuyển trạng thái, repository interface
├── News.Application/        DTO, query/command, use case, kiểm tra quyền và version
├── News.Infrastructure/     DbContext, EF repository, SQL Server/SQLite, migrations, JWT, PasswordHasher, lọc HTML
├── News.Api/                Controller, DI, auth middleware, CORS, rate limit, Problem Details, health check
├── News.Tests/              Kiểm thử nghiệp vụ và HTTP với database tách biệt
├── sql/                     Script SQL Server idempotent
└── compose.sqlserver.yml    SQL Server Developer trên Docker x64

frontend/src/
├── components/              Thẻ tin, ảnh, nút lưu, skeleton, ví dụ component con
├── layouts/                 Khung tòa soạn responsive
├── views/                   Trang tin, bài viết, đã lưu, đăng nhập, lộ trình học
│   └── admin/               DataTable quản lý, Editor soạn bài
├── services/                Axios chung, auth interceptor, API public/admin
├── stores/                  Pinia auth, chuyên mục, bài đã lưu
├── router/                  Route, guard, lazy loading
├── types/                   Hợp đồng TypeScript
└── utils/                   Định dạng, trạng thái và quyền hiển thị
```

Phụ thuộc backend: **Application → Domain**, **Infrastructure → Application/Domain**, **API → Application/Infrastructure**. Controller không truy vấn database; Application xử lý nghiệp vụ qua interface. Repository truy vấn/filter/count/paginate bằng EF Core async tại database. DTO tách hợp đồng HTTP khỏi entity, danh sách công khai không gửi toàn bộ nội dung bài.

Các bảng: **Articles**, **Categories**, **Users**, **RoleDefinitions**, **Sessions**, **Comments**, **Ratings**; khóa ngoại, chỉ mục và concurrency token. `Version` dạng GUID được EF kiểm tra khi cập nhật, trả `409` nếu dữ liệu đã đổi. Ngày giờ lưu UTC, frontend hiển thị giờ Việt Nam.

## Bình luận và đánh giá bài viết

Cuối mỗi bài công khai có bình luận và đánh giá **1–5 sao**, điểm trung bình, số lượt và phân bố số sao. Đọc bình luận không yêu cầu đăng nhập. Gửi bình luận cần tài khoản: khi chưa đăng nhập, nhấn gửi sẽ mở form đăng ký/đăng nhập ngay tại bài viết và giữ nội dung đang nhập. Đăng ký bằng email và mật khẩu 12–200 ký tự tự tạo quyền **Reader / Độc giả**, không có quyền tòa soạn. Tên bình luận lấy từ tài khoản; nội dung cần 5–2000 ký tự. Nội dung được lưu dưới dạng văn bản và hiển thị bằng nội suy Vue, không thực thi HTML. Danh sách bình luận có phân trang, mới nhất trước.

Có thể đăng ký/đăng nhập bằng Google sau khi cấu hình `Google:ClientId` ở backend. Hiện mặc định chưa có Client ID nên nút Google bị vô hiệu hóa; đăng ký bằng email vẫn hoạt động. Xem [hướng dẫn tạo Client ID và bật Google](docs/GOOGLE_SIGN_IN.md). Backend xác minh Google ID token và nonce, tự tạo tài khoản Reader lần đầu. Không tự liên kết Google vào tài khoản email/mật khẩu có sẵn.

Đánh giá được nhận diện bằng cookie ngẫu nhiên `HttpOnly`, `SameSite=Strict` có hạn một năm, chỉ lưu bản hash của mã nhận diện trong database. Gửi lại từ cùng trình duyệt cập nhật đánh giá cũ, không tăng số lượt. Đây là nhận diện trình duyệt, không xác minh danh tính: xóa cookie hoặc dùng trình duyệt khác có thể tạo lượt mới. Cookie dùng `Secure` trong Production; triển khai frontend và API cùng site qua HTTPS.

Chỉ bài đã xuất bản, chưa xóa mới cho đọc/ghi phản hồi. API giới hạn tổng cộng 12 lần gửi bình luận/đánh giá mỗi phút theo IP. SQLite Development tự bổ sung hai bảng khi `Database:Initialize=true`, giữ nguyên dữ liệu cũ. SQL Server dùng `InitialNewsSchema`, đã bao gồm bình luận và đánh giá; database có lịch sử migration cũ cần làm theo [hướng dẫn chuyển baseline](docs/SQL_SERVER.md).

## Đăng nhập và phiên

- Password được hash bằng ASP.NET Core PasswordHasher; khóa tạm 5 phút sau 5 lần sai, endpoint login có rate limit.
- Access token JWT sống mặc định 15 phút, kiểm tra issuer/audience/chữ ký/thời hạn và phiên trong DB.
- Token frontend chỉ nằm trong bộ nhớ, không lưu localStorage/sessionStorage.
- Refresh token ngẫu nhiên đi qua cookie **HttpOnly, SameSite=Strict**, lưu **hash** trong database và được xoay vòng mỗi lần refresh. Phiên có thời hạn tuyệt đối 8 giờ; đăng xuất thu hồi phiên và access token của phiên đó.
- Axios tự refresh một lần khi access token hết hạn; tải lại trang quản trị khôi phục phiên qua cookie. Khi triển khai dùng HTTPS và frontend/backend cùng site qua `/api` proxy.
- Nội dung Editor được lọc HTML phía server; frontend dùng DOMPurify khi xem trước và đọc bài.

## API

| Nhóm | Endpoint |
| --- | --- |
| Công khai | `GET /health`, `GET /api/categories`, `GET /api/articles`, `GET /api/articles/{id}` |
| Bình luận | `GET/POST /api/articles/{id}/comments` — GET nhận `page`, `pageSize` (1–30, mặc định 10); POST yêu cầu đăng nhập và nhận `{ "content": "Bình luận" }` |
| Đánh giá | `GET/POST /api/articles/{id}/ratings` — POST nhận `{ "stars": 1..5 }`; response có `average`, `count`, `myRating`, `distribution` (số lượt từ 1 đến 5 sao) |
| Đăng ký độc giả | `POST /api/auth/register` |
| Google | `GET /api/auth/google/options`, `POST /api/auth/google` |
| Đăng nhập | `POST /api/auth/login`, `POST /api/auth/refresh`, `POST /api/auth/logout`, `GET /api/auth/me` |
| CRUD quản trị | `GET/POST /api/admin/articles`, `GET/PUT/DELETE /api/admin/articles/{id}` |
| Người dùng (Admin) | `GET/POST /api/admin/users`, `PUT /api/admin/users/{id}/role` |
| Vai trò (Admin) | `GET/POST /api/admin/roles` |
| Danh mục (Admin) | `GET/POST /api/admin/categories` |
| Quy trình | `POST /api/admin/articles/{id}/{operation}` |

`operation`: `submit`, `approve`, `reject`, `publish`, `unpublish`, `archive`, `restore`. Body: `{ "version": "GUID", "note": "Góp ý nếu trả lại" }`. Xóa dùng `?version=GUID`.

Danh sách nhận `search` (≤200 ký tự), `category` (slug), `page` (mặc định 1), `pageSize` (1–30, mặc định 9). Quản trị thêm `status` (`Draft`, `PendingReview`, `Approved`, `Published`, `Rejected`, `Archived`), mặc định `pageSize` như query. Tác giả chỉ nhận các bài của mình.

Response danh sách: `{ items, totalCount, page, pageSize, totalPages }`. Response chi tiết quản trị: `{ article, status, authorId, version, reviewNote, createdAt, updatedAt }`.

`400` dữ liệu sai, `401` chưa đăng nhập/hết phiên, `403` thiếu quyền, `404` không tồn tại/chưa công khai, `409` xung đột phiên bản/trạng thái, `429` vượt giới hạn đăng nhập hoặc gửi phản hồi. Lỗi HTTP dùng Problem Details; client phân biệt lỗi mạng và lỗi API.

## SQL Server và tài khoản triển khai

Xem [hướng dẫn chi tiết dựng SQL Server, connection string và migrations](docs/SQL_SERVER.md). Trong Production cần cấu hình signing key bí mật, SQL Server, HTTPS và origin frontend; không sử dụng signing key Development.

Lần khởi tạo đầu đặt `Seed__AdminEmail`, `Seed__AdminPassword` và `Database__Initialize=true` để tạo quản trị viên. Muốn tạo thêm nhân sự trong lần khởi tạo, cấu hình qua secret store/biến môi trường:

```dotenv
Seed__StaffUsers__0__Email=editor@example.com
Seed__StaffUsers__0__Name=Bien tap vien
Seed__StaffUsers__0__Role=Editor
Seed__StaffUsers__0__Password=MAT_KHAU_RIENG_IT_NHAT_12_KY_TU
```

Role hợp lệ: `Admin`, `Editor`, `Author`. Seed chỉ thêm email chưa tồn tại, không ghi đè mật khẩu/quyền. Sau khởi tạo bỏ các mật khẩu seed khỏi cấu hình và đặt `Database__Initialize=false`. Sau khởi tạo, Admin tạo và phân quyền tài khoản tại mục **Người dùng**. Chưa có chức năng đổi mật khẩu.

## Kiểm thử

```bash
cd backend
dotnet build News.sln
dotnet test News.sln
```

Kiểm thử bằng EF Core SQLite tách biệt: tìm kiếm không dấu, phân trang, validation, DTO, CORS, CRUD, tác giả không truy cập bài người khác, chuyển trạng thái, ẩn bản nháp, version cũ, lọc HTML, JWT giả, refresh cookie và thu hồi phiên.

```bash
cd frontend
npm run typecheck
npm run build
npx playwright install chromium
npm test
```

Playwright tự khởi động backend Development ở cổng **5081** bằng database `news-e2e.db` riêng và Vite ở cổng **5174**, không dùng lại server phát triển ở 5080/5173 để tránh tạo dữ liệu thử nghiệm trong `news-dev.db`. Kiểm tra desktop/mobile: trang tin, tìm kiếm, lưu bài, lỗi mạng/ảnh/404, các ví dụ học Vue/PrimeVue, route guard, đăng nhập, Editor → gửi duyệt → duyệt → xuất bản → gỡ bài, reload phiên và đăng xuất; đăng ký/đăng nhập độc giả từ phần bình luận, Google giả lập, giữ bản nháp khi hết phiên; tạo vai trò, tạo/đăng nhập tài khoản mới, phân quyền và tạo danh mục. Chạy tuần tự để thao tác biên tập và các kiểm tra danh sách công khai không đụng dữ liệu nhau. Có thể đặt `PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH` để dùng Chrome sẵn có. Kết quả lỗi trong `artifacts/playwright/`.

Bốn migration cũ đã được gộp thành một `InitialNewsSchema` chứa toàn bộ bảy bảng và bốn vai trò mặc định. Thư mục migrations còn ba file theo cấu trúc EF: migration chính, `.Designer.cs` và `NewsDbContextModelSnapshot.cs`. Script `backend/sql/001-initial-schema.sql` khởi tạo đầy đủ database mới; các script `002`–`004` dành riêng cho database có lịch sử cũ. Migrations/script được sinh bằng SQL Server provider. **Chưa kiểm thử kết nối hoặc áp dụng migration trên SQL Server thật** vì chưa có instance; cần hoàn thành bước này theo tài liệu trước khi triển khai SQL Server.

Quill được ghim ở `2.0.2` để tránh `2.0.3` đang được liệt kê trong [GHSA-v3m3-f69x-jf25](https://github.com/advisories/GHSA-v3m3-f69x-jf25). Lớp lọc HTML phía server và DOMPurify vẫn được giữ độc lập với lựa chọn phiên bản Editor.

## Cấu hình frontend và triển khai

Vite mặc định chạy `127.0.0.1:5173`, chuyển `/api` tới `localhost:5080`, không tự đổi cổng khi bận. `frontend/.env.example` hướng dẫn đổi `VITE_API_URL` nếu cần gọi API trực tiếp; khi đổi origin cập nhật `Cors:AllowedOrigins` ở backend.

`npm run build` tạo `frontend/dist/`. Static host cần fallback route ứng dụng về `index.html`, chuyển tiếp `/api` tới ASP.NET Core, và hỗ trợ cookie cùng site. Vite proxy chỉ dùng trong phát triển. `npm run preview` chỉ xem frontend đã build.
