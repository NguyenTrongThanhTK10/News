import { computed, ref } from 'vue';
import { defineStore } from 'pinia';
import { api, configureSession, refreshSession, setAccessToken } from '../services/http';
import type { AuthResponse, RegisterRequest, User } from '../types/admin';
import { roleLabels } from '../utils/roles';

// Pinia store là nơi giữ trạng thái dùng chung. Mọi component gọi useAuthStore()
// trong cùng ứng dụng đều dùng chung user, thay vì tạo một phiên đăng nhập riêng.
export const useAuthStore = defineStore('auth', () => {
  // ref<T> tạo dữ liệu phản ứng: đổi .value trong script sẽ cập nhật nơi đang hiển thị nó.
  const user = ref<User | null>(null);
  const initialized = ref(false);
  // Giữ Promise để nhiều route guard gọi initialize cùng lúc vẫn chỉ khởi tạo một lần.
  let initialization: Promise<void> | undefined;
  // computed tự tính lại khi user đổi; đây là quyền hỗ trợ hiển thị, API vẫn kiểm tra quyền.
  const canReview = computed(() => user.value?.role === 'Admin' || user.value?.role === 'Editor');
  const isAdmin = computed(() => user.value?.role === 'Admin');
  const isStaff = computed(() => !!user.value && ['Admin', 'Editor', 'Author'].includes(user.value.role));
  const roleLabel = computed(() => user.value?.roleName || roleLabels[user.value?.role || 'Reader']);
  // Một điểm cập nhật cả người dùng trong Pinia lẫn access token trong HTTP layer.
  // ?. đọc an toàn khi session = null; || cung cấp giá trị thay thế khi không có phiên.
  function apply(session: AuthResponse | null) {
    user.value = session?.user || null;
    setAccessToken(session?.accessToken || '');
  }
  // Khi refresh tự động thành công/thất bại, HTTP layer gọi apply để giao diện biết phiên đã đổi.
  configureSession(apply);
  async function initialize() {
    if (initialized.value) return;
    // ??= chỉ gán khi chưa có Promise. Không có cookie hợp lệ được xem là chưa đăng nhập;
    // lỗi khởi tạo được chặn ở đây để router vẫn chuyển đến trang đăng nhập bình thường.
    initialization ??= refreshSession().then(() => {}).catch(() => {}).finally(() => { initialized.value = true; });
    await initialization;
  }
  // Lỗi đăng nhập được để truyền lên LoginView; chỉ cập nhật store sau khi API thành công.
  async function login(email: string, password: string) {
    const { data } = await api.post<AuthResponse>('/auth/login', { email, password });
    apply(data);
    initialized.value = true;
  }
  async function register(request: RegisterRequest) {
    const { data } = await api.post<AuthResponse>('/auth/register', request);
    apply(data);
    initialized.value = true;
  }
  async function loginGoogle(credential: string) {
    const { data } = await api.post<AuthResponse>('/auth/google', { credential });
    apply(data);
    initialized.value = true;
  }
  // Đợi server thu hồi phiên trước khi xóa trạng thái phía frontend.
  async function logout() {
    await api.post('/auth/logout');
    apply(null);
    initialized.value = true;
  }
  // Chỉ các state/getter/action được return mới được truy cập từ component.
  return { user, initialized, canReview, isAdmin, isStaff, roleLabel, initialize, login, register, loginGoogle, logout };
});
