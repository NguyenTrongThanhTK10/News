<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, ref } from 'vue';
import { useRouter } from 'vue-router';
import Dialog from 'primevue/dialog';
import type { ArticleSummary } from '../types/news';

const props = withDefaults(defineProps<{
  article: Pick<ArticleSummary, 'id' | 'title' | 'summary'>;
  variant?: 'button' | 'panel';
}>(), { variant: 'button' });
const router = useRouter();
const visible = ref(false);
const busy = ref(false);
const copied = ref(false);
const message = ref('');
const linkInput = ref<HTMLInputElement | null>(null);
const nativeSupported = typeof navigator.share === 'function';
let disposed = false;
onBeforeUnmount(() => { disposed = true; });

// Luôn chia sẻ URL bài viết, bỏ query/hash của phiên đang đọc và giữ base URL của router.
const articleUrl = computed(() => new URL(router.resolve({ name: 'article', params: { id: props.article.id } }).href, window.location.origin).href);
const facebookUrl = computed(() => `https://www.facebook.com/sharer/sharer.php?${new URLSearchParams({ u: articleUrl.value })}`);
const xUrl = computed(() => `https://twitter.com/intent/tweet?${new URLSearchParams({ url: articleUrl.value, text: props.article.title })}`);
const emailUrl = computed(() => `mailto:?subject=${encodeURIComponent(props.article.title)}&body=${encodeURIComponent(`${props.article.summary}\r\n\r\n${articleUrl.value}`)}`);

function open() {
  copied.value = false;
  message.value = '';
  visible.value = true;
}
function selectLink() { linkInput.value?.select(); }
async function copyLink() {
  if (busy.value) return;
  busy.value = true; copied.value = false; message.value = '';
  try {
    await navigator.clipboard.writeText(articleUrl.value);
    if (disposed) return;
    copied.value = true;
    message.value = 'Đã sao chép liên kết bài viết.';
  } catch {
    if (disposed) return;
    message.value = 'Chưa thể sao chép tự động. Liên kết đã được chọn để bạn sao chép thủ công.';
    await nextTick();
    linkInput.value?.focus();
    selectLink();
  } finally { if (!disposed) busy.value = false; }
}
async function shareOnDevice() {
  if (busy.value || !nativeSupported) return;
  busy.value = true; message.value = '';
  try {
    // Gọi ngay trong thao tác nhấn nút để giữ quyền mở menu chia sẻ của trình duyệt.
    await navigator.share({ title: props.article.title, text: props.article.summary, url: articleUrl.value });
    if (!disposed) message.value = 'Đã mở chia sẻ trên thiết bị.';
  } catch (cause) {
    // Đóng menu chia sẻ là thao tác bình thường của người đọc.
    if (!disposed && !(cause instanceof DOMException && cause.name === 'AbortError'))
      message.value = 'Chưa mở được chia sẻ trên thiết bị. Bạn có thể chọn cách khác hoặc sao chép liên kết.';
  } finally { if (!disposed) busy.value = false; }
}
</script>

<template>
  <section v-if="variant === 'panel'" class="article-share-panel" aria-label="Chia sẻ bài viết">
    <div><span class="eyebrow">LAN TỎA GÓC NHÌN</span><h2>Một câu chuyện đáng chia sẻ</h2><p>Gửi bài viết này đến người bạn muốn cùng trò chuyện.</p></div>
    <button type="button" class="button button-primary" @click="open"><i class="pi pi-share-alt" aria-hidden="true"></i> Chia sẻ bài viết</button>
  </section>
  <button v-else type="button" class="button button-outline" aria-haspopup="dialog" @click="open"><i class="pi pi-share-alt" aria-hidden="true"></i> Chia sẻ</button>

  <Dialog v-model:visible="visible" header="Chia sẻ bài viết" modal :style="{ width: '500px' }" :breakpoints="{ '600px': '94vw' }" :closable="!busy" :close-on-escape="!busy">
    <div class="article-share-dialog" :aria-busy="busy">
      <p class="share-article-title">{{ article.title }}</p>
      <p class="share-description">Chọn cách gửi câu chuyện này đến bạn bè.</p>
      <div class="share-options">
        <a :href="facebookUrl" target="_blank" rel="noopener noreferrer" class="share-option" aria-label="Chia sẻ qua Facebook"><i class="pi pi-facebook" aria-hidden="true"></i><span>Facebook</span><i class="pi pi-arrow-up-right" aria-hidden="true"></i></a>
        <a :href="xUrl" target="_blank" rel="noopener noreferrer" class="share-option" aria-label="Chia sẻ qua X"><i class="pi pi-twitter" aria-hidden="true"></i><span>X (Twitter)</span><i class="pi pi-arrow-up-right" aria-hidden="true"></i></a>
        <a :href="emailUrl" class="share-option" aria-label="Chia sẻ qua email"><i class="pi pi-envelope" aria-hidden="true"></i><span>Email</span></a>
        <button v-if="nativeSupported" type="button" class="share-option" :disabled="busy" @click="shareOnDevice"><i class="pi pi-share-alt" aria-hidden="true"></i><span>Ứng dụng khác</span></button>
      </div>
      <div class="share-link-field"><label>Liên kết bài viết<input ref="linkInput" :value="articleUrl" readonly spellcheck="false" class="feedback-input" @click="selectLink" /></label><button type="button" class="button button-primary" :disabled="busy" @click="copyLink"><i :class="['pi', copied ? 'pi-check' : 'pi-copy']" aria-hidden="true"></i>{{ copied ? 'Đã sao chép' : 'Sao chép liên kết' }}</button></div>
      <p v-if="message" class="share-message" role="status">{{ message }}</p>
    </div>
  </Dialog>
</template>
