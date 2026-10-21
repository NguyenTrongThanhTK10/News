import { computed, ref } from 'vue';
import { defineStore } from 'pinia';
import { api, configureSession, refreshSession, setAccessToken } from '../services/http';
import type { AuthResponse, User } from '../types/admin';

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null);
  const initialized = ref(false);
  let initialization: Promise<void> | undefined;
  const canReview = computed(() => user.value?.role === 'Admin' || user.value?.role === 'Editor');
  const roleLabel = computed(() => ({ Admin: 'Quản trị viên', Editor: 'Biên tập viên', Author: 'Tác giả' })[user.value?.role || 'Author']);
  function apply(session: AuthResponse | null) {
    user.value = session?.user || null;
    setAccessToken(session?.accessToken || '');
  }
  configureSession(apply);
  async function initialize() {
    if (initialized.value) return;
    initialization ??= refreshSession().then(() => {}).catch(() => {}).finally(() => { initialized.value = true; });
    await initialization;
  }
  async function login(email: string, password: string) {
    const { data } = await api.post<AuthResponse>('/auth/login', { email, password });
    apply(data);
    initialized.value = true;
  }
  async function logout() {
    await api.post('/auth/logout');
    apply(null);
    initialized.value = true;
  }
  return { user, initialized, canReview, roleLabel, initialize, login, logout };
});
