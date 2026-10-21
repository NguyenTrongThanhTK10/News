<script setup lang="ts">
import { ref, watch } from 'vue';
import Dialog from 'primevue/dialog';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Message from 'primevue/message';
import GoogleSignInButton from './GoogleSignInButton.vue';
import { useAuthStore } from '../stores/auth';
import { errorMessage } from '../services/http';

const visible = defineModel<boolean>('visible', { default: false });
const props = withDefaults(defineProps<{ initialMode?: 'register' | 'login' }>(), { initialMode: 'register' });
const emit = defineEmits<{ authenticated: [] }>();
const auth = useAuthStore();
const mode = ref<'register' | 'login'>(props.initialMode);
const name = ref('');
const email = ref('');
const password = ref('');
const confirmPassword = ref('');
const busy = ref(false);
const error = ref('');
watch(visible, value => {
  password.value = ''; confirmPassword.value = ''; error.value = '';
  if (value) mode.value = props.initialMode;
});
function switchMode() {
  mode.value = mode.value === 'register' ? 'login' : 'register';
  password.value = ''; confirmPassword.value = ''; error.value = '';
}
function finish() {
  visible.value = false;
  password.value = ''; confirmPassword.value = '';
  emit('authenticated');
}
async function submit() {
  if (busy.value) return;
  error.value = '';
  if (mode.value === 'register' && password.value !== confirmPassword.value) { error.value = 'Mật khẩu xác nhận chưa khớp.'; return; }
  busy.value = true;
  try {
    if (mode.value === 'register') await auth.register({ name: name.value.trim(), email: email.value.trim(), password: password.value });
    else await auth.login(email.value.trim(), password.value);
    finish();
  } catch (cause) { error.value = errorMessage(cause); }
  finally { busy.value = false; }
}
async function loginGoogle(credential: string) {
  if (busy.value) return;
  busy.value = true; error.value = '';
  try { await auth.loginGoogle(credential); finish(); }
  catch (cause) { error.value = errorMessage(cause); }
  finally { busy.value = false; }
}
</script>

<template>
  <Dialog v-model:visible="visible" :header="mode === 'register' ? 'Đăng ký để bình luận' : 'Đăng nhập để bình luận'" modal :style="{ width: '480px' }" :breakpoints="{ '600px': '94vw' }" :closable="!busy" :close-on-escape="!busy">
    <div class="reader-auth-dialog">
      <p class="dialog-copy">{{ mode === 'register' ? 'Tạo tài khoản để chia sẻ góc nhìn của bạn cùng cộng đồng TIN24.' : 'Đăng nhập và tiếp tục cuộc trò chuyện ngay tại bài viết.' }}</p>
      <GoogleSignInButton :disabled="busy" @credential="loginGoogle" />
      <div class="auth-divider"><span>hoặc {{ mode === 'register' ? 'đăng ký' : 'đăng nhập' }} bằng email</span></div>
      <form class="management-form" @submit.prevent="submit"><fieldset :disabled="busy">
        <div v-if="mode === 'register'" class="form-field"><label for="reader-name">Họ tên</label><InputText id="reader-name" v-model="name" required minlength="2" maxlength="100" autocomplete="name" /></div>
        <div class="form-field"><label for="reader-email">Email</label><InputText id="reader-email" v-model="email" required type="email" maxlength="254" autocomplete="username" /></div>
        <div class="form-field"><label for="reader-password">Mật khẩu</label><InputText id="reader-password" v-model="password" required type="password" :minlength="mode === 'register' ? 12 : undefined" maxlength="200" :autocomplete="mode === 'register' ? 'new-password' : 'current-password'" /><small v-if="mode === 'register'">Từ 12 đến 200 ký tự.</small></div>
        <div v-if="mode === 'register'" class="form-field"><label for="reader-confirm-password">Xác nhận mật khẩu</label><InputText id="reader-confirm-password" v-model="confirmPassword" required type="password" minlength="12" maxlength="200" autocomplete="new-password" /></div>
        <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>
        <Button :label="mode === 'register' ? 'Đăng ký tài khoản' : 'Đăng nhập'" type="submit" :loading="busy" class="reader-auth-submit" />
        <p class="reader-auth-switch">{{ mode === 'register' ? 'Đã có tài khoản?' : 'Chưa có tài khoản?' }} <button type="button" class="text-button" :disabled="busy" @click="switchMode">{{ mode === 'register' ? 'Đăng nhập' : 'Đăng ký' }}</button></p>
      </fieldset></form>
    </div>
  </Dialog>
</template>
