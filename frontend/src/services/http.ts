import axios from 'axios';
import type { AuthResponse } from '../types/admin';

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || '/api', timeout: 15_000, withCredentials: true,
});
let token = '';
let refreshPending: Promise<AuthResponse> | undefined;
let sessionChanged: (session: AuthResponse | null) => void = () => {};
export function configureSession(callback: typeof sessionChanged) { sessionChanged = callback; }
export function setAccessToken(value: string) { token = value; }

export async function refreshSession(): Promise<AuthResponse> {
  if (!refreshPending) {
    refreshPending = api.post<AuthResponse>('/auth/refresh').then(({ data }) => {
      token = data.accessToken;
      sessionChanged(data);
      return data;
    }).catch(error => {
      token = '';
      sessionChanged(null);
      throw error;
    }).finally(() => { refreshPending = undefined; });
  }
  return refreshPending;
}

api.interceptors.request.use(config => {
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});
api.interceptors.response.use(response => response, async error => {
  const config = error.config;
  if (error.response?.status === 401 && config && !config.url?.startsWith('/auth/') && token && !config._retry) {
    config._retry = true;
    await refreshSession();
    return api.request(config);
  }
  return Promise.reject(error);
});

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
