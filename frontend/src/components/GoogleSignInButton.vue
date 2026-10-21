<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue';
import { getGoogleOptions, loadGoogleIdentity } from '../services/google';

const props = defineProps<{ disabled?: boolean }>();
const emit = defineEmits<{ credential: [value: string] }>();
const container = ref<HTMLElement | null>(null);
const loading = ref(true);
const unavailable = ref(false);
const error = ref(false);
let lifetime = new AbortController();
async function load() {
  lifetime.abort();
  const request = lifetime = new AbortController();
  loading.value = true; error.value = false; unavailable.value = false;
  try {
    const options = await getGoogleOptions(request.signal);
    if (request.signal.aborted) return;
    if (!options.clientId || !options.nonce) { unavailable.value = true; return; }
    const google = await loadGoogleIdentity();
    if (request.signal.aborted || !container.value) return;
    google.initialize({ client_id: options.clientId, nonce: options.nonce, auto_select: false,
      callback: response => { if (!request.signal.aborted && !props.disabled) emit('credential', response.credential); } });
    container.value.replaceChildren();
    google.renderButton(container.value, { type: 'standard', theme: 'outline', size: 'large', text: 'continue_with', locale: 'vi', width: Math.min(360, container.value.clientWidth || 280) });
  } catch { if (!request.signal.aborted) error.value = true; }
  finally { if (!request.signal.aborted) loading.value = false; }
}
onMounted(load);
onBeforeUnmount(() => lifetime.abort());
</script>

<template>
  <div class="google-signin" :class="{ 'is-busy': disabled }" :inert="disabled || undefined">
    <div ref="container" class="google-button-container"></div>
    <p v-if="loading" class="feedback-hint" role="status">Đang tải đăng nhập Google…</p>
    <template v-else-if="unavailable"><button type="button" class="button button-outline" disabled><i class="pi pi-google" aria-hidden="true"></i> Tiếp tục với Google</button><p class="feedback-hint">Đăng nhập Google hiện chưa khả dụng.</p></template>
    <p v-else-if="error" class="feedback-hint" role="status">Chưa tải được đăng nhập Google. <button class="text-button" type="button" @click="load">Thử lại</button></p>
  </div>
</template>
