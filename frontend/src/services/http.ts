import axios from 'axios';
import type { AuthResponse } from '../types/admin';

// Một Axios instance dùng chung giúp các service thống nhất URL, timeout và phiên đăng nhập.
// VITE_API_URL lấy từ môi trường Vite; nếu thiếu thì gọi /api qua proxy khi phát triển.
// withCredentials cho phép gửi/nhận cookie trong request theo chính sách của trình duyệt.
export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || '/api', timeout: 15_000, withCredentials: true,
});
// Access token chỉ nằm trong bộ nhớ của tab, mất khi tải lại trang; không lưu localStorage.
let token = '';
// Promise đại diện yêu cầu refresh đang chạy: nhiều request hết hạn sẽ dùng chung một lần refresh.
let refreshPending: Promise<AuthResponse> | undefined;
// Auth store đăng ký callback để đồng bộ user khi HTTP layer khôi phục hoặc mất phiên.
let sessionChanged: (session: AuthResponse | null) => void = () => {};
export function configureSession(callback: typeof sessionChanged) { sessionChanged = callback; }
export function setAccessToken(value: string) { token = value; }

// async trả về Promise; await ở nơi gọi sẽ chờ API hoàn tất mà không chặn giao diện.
// Cookie HttpOnly được trình duyệt gửi tự động, JavaScript không cần đọc refresh token.
export async function refreshSession(): Promise<AuthResponse> {
  if (!refreshPending) {
    refreshPending = api.post<AuthResponse>('/auth/refresh').then(({ data }) => {
      token = data.accessToken;
      sessionChanged(data);
      return data;
    }).catch(error => {
      // Refresh thất bại: xóa phiên trong bộ nhớ rồi ném lỗi để nơi gọi tự xử lý.
      token = '';
      sessionChanged(null);
      throw error;
    // finally luôn chạy sau thành công/thất bại, mở đường cho lần refresh tiếp theo.
    }).finally(() => { refreshPending = undefined; });
  }
  return refreshPending;
}

// Interceptor là hàm chạy trước/sau mỗi request: tự gắn Bearer token nếu đang có phiên.
api.interceptors.request.use(config => {
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});
// Với lỗi 401 ở API nghiệp vụ, thử refresh rồi gửi lại request cũ đúng một lần.
// Bỏ qua /auth/ và đánh dấu _retry để không lặp vô hạn khi phiên thực sự không còn hợp lệ.
api.interceptors.response.use(response => response, async error => {
  const config = error.config;
  if (error.response?.status === 401 && config && !config.url?.startsWith('/auth/') && token && !config._retry) {
    config._retry = true;
    await refreshSession();
    return api.request(config);
  }
  return Promise.reject(error);
});

// unknown buộc kiểm tra kiểu trước khi đọc lỗi. Ưu tiên lỗi validation/Problem Details từ API,
// sau đó mới dùng thông báo dự phòng. Hàm này chỉ tạo chuỗi, component quyết định chỗ hiển thị.
export function errorMessage(error: unknown): string {
  if (!axios.isAxiosError(error)) return 'Có lỗi xảy ra. Vui lòng thử lại.';
  const problem = error.response?.data;
  if (problem?.errors) return Object.values(problem.errors).flat().join(' ');
  if (problem?.title) return String(problem.title);
  if (error.response?.status === 401) return 'Phiên đăng nhập đã hết hạn. Hãy đăng nhập lại.';
  if (error.response?.status === 403) return 'Bạn không có quyền thực hiện thao tác này.';
  if (error.response?.status === 429) return 'Bạn thao tác quá nhanh. Vui lòng đợi một phút.';
  return 'Không kết nối được máy chủ. Kiểm tra kết nối và thử lại.';
}
