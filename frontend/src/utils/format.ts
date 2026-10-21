// Tạo formatter một lần rồi tái sử dụng cho mọi thẻ tin, không tạo lại khi render từng bài.
// Chỉ định múi giờ giúp cùng một ngày giờ API luôn được hiển thị theo giờ Việt Nam.
const dateFormatter = new Intl.DateTimeFormat('vi-VN', {
  day: '2-digit', month: '2-digit', year: 'numeric', timeZone: 'Asia/Ho_Chi_Minh',
});

// Ngày không hợp lệ trả chuỗi rỗng để tránh hiển thị "Invalid Date" hoặc gây lỗi định dạng.
export function formatDate(value: string): string {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? '' : dateFormatter.format(date);
}

// Bảng tra slug → lớp PrimeIcons; chuyên mục mới chưa có trong bảng dùng icon mặc định.
// Record<string, string> mô tả một đối tượng có khóa chuỗi và giá trị chuỗi.
export function categoryIcon(slug: string): string {
  return ({
    'doi-song': 'pi-heart', 'cong-nghe': 'pi-microchip', 'du-lich': 'pi-compass',
    'van-hoa': 'pi-palette', 'giao-duc': 'pi-book', 'the-thao': 'pi-trophy',
  } as Record<string, string>)[slug] || 'pi-th-large';
}
