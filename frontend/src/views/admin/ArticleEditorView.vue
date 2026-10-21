<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { onBeforeRouteLeave, RouterLink, useRoute, useRouter } from 'vue-router';
import DOMPurify from 'dompurify';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import InputNumber from 'primevue/inputnumber';
import Textarea from 'primevue/textarea';
import Select from 'primevue/select';
import Editor from 'primevue/editor';
import Tag from 'primevue/tag';
import Message from 'primevue/message';
import Dialog from 'primevue/dialog';
import { useToast } from 'primevue/usetoast';
import { useCategoriesStore } from '../../stores/categories';
import { getAdminArticle, saveArticle } from '../../services/admin';
import { errorMessage } from '../../services/http';
import { statusLabels, statusSeverity } from '../../utils/workflow';
import type { AdminArticle, SaveArticleRequest } from '../../types/admin';

// Cùng một trang dùng cho tạo bài và sửa bài: có route.params.id thì tải bài, không có thì form mới.
// categories là dữ liệu dùng chung; loaded là bản server gần nhất, form là dữ liệu người dùng sửa.
const route = useRoute();
const router = useRouter();
const toast = useToast();
const categories = useCategoriesStore();
const loaded = ref<AdminArticle | null>(null);
const loading = ref(false);
const busy = ref(false);
const error = ref('');
// Hai Dialog độc lập: preview xem nội dung, leaveDialog hỏi khi rời trang còn thay đổi chưa lưu.
const preview = ref(false);
const leaveDialog = ref(false);
// Giữ hàm resolve của Promise trong route guard, để nút trong Dialog quyết định cho rời trang hay ở lại.
let resolveLeave: ((allow: boolean) => void) | undefined;
const initial = ref('');
const form = ref<SaveArticleRequest>({ title: '', summary: '', content: '', categorySlug: '', imageUrl: '', readingMinutes: 3 });
// Bài công khai/chờ duyệt/lưu trữ chỉ được xem. Backend vẫn kiểm tra quyền sửa khi nhận API.
const readonly = computed(() => loaded.value ? ['Published', 'PendingReview', 'Archived'].includes(loaded.value.status) : false);
// initial giữ JSON form sau khi tải/lưu; khác chuỗi này nghĩa là có thay đổi chưa lưu (dirty).
// Cách so sánh này phù hợp form gồm các trường đơn giản ở đây, không phải so sánh mọi loại object.
const dirty = computed(() => !readonly.value && JSON.stringify(form.value) !== initial.value);
// Bản xem trước dùng v-html nên lọc bằng DOMPurify; form gốc vẫn giữ nội dung đang soạn.
const sanitized = computed(() => DOMPurify.sanitize(form.value.content || ''));
// Nếu URL có ID nhưng tải thất bại, không hiển thị form trống để tránh nhầm thành bài mới.
const loadFailed = computed(() => !!route.params.id && !loaded.value);
// Đổi key buộc Editor tạo lại trạng thái nội bộ khi mở bài khác hoặc tải lại.
const editorKey = ref(0);
const retry = ref(0);

// immediate tải ngay khi vào trang; đổi ID/retry tải lại dù Vue vẫn dùng cùng instance component.
// Cleanup hủy request trước để bài mở chậm không ghi đè form của bài người dùng vừa chuyển tới.
watch([() => route.params.id, retry], async (_, __, onCleanup) => {
  const controller = new AbortController();
  onCleanup(() => controller.abort());
  loaded.value = null; error.value = ''; loading.value = true;
  const rawId = route.params.id;
  try {
    // Chờ store bảo đảm có chuyên mục cho Select; store tự xử lý lỗi và dùng lại dữ liệu đã có.
    await categories.load();
    if (controller.signal.aborted) return;
    if (rawId) {
      if (!/^\d+$/.test(String(rawId))) throw new Error('Mã bài viết không hợp lệ.');
      const data = await getAdminArticle(Number(rawId), controller.signal);
      if (controller.signal.aborted) return;
      loaded.value = data;
      // Chép đúng các trường được phép lưu vào form; version đi kèm để server kiểm tra xung đột.
      form.value = { title: data.article.title, summary: data.article.summary, content: data.article.content, categorySlug: data.article.categorySlug, imageUrl: data.article.imageUrl, readingMinutes: data.article.readingMinutes, version: data.version };
    } else form.value = { title: '', summary: '', content: '', categorySlug: '', imageUrl: '', readingMinutes: 3 };
    initial.value = JSON.stringify(form.value);
    editorKey.value++;
  } catch (cause) { if (!controller.signal.aborted) error.value = errorMessage(cause); }
  finally { if (!controller.signal.aborted) loading.value = false; }
}, { immediate: true });

// Form gọi khi submit: id null tạo mới, có id cập nhật. busy điều khiển trạng thái đang lưu.
// Các ràng buộc HTML giúp kiểm tra sớm, nhưng API vẫn là nơi xác thực nội dung và quyền cuối cùng.
async function save() {
  busy.value = true; error.value = '';
  try {
    const data = await saveArticle(loaded.value?.article.id ?? null, form.value);
    // Đánh dấu form vừa lưu để route guard không hỏi bỏ thay đổi khi chuyển sang URL bài đã tạo.
    initial.value = JSON.stringify(form.value);
    toast.add({ severity: 'success', summary: 'Đã lưu bản nháp', detail: 'Bạn có thể gửi bài để biên tập viên duyệt tại danh sách bài viết.', life: 4500 });
    await router.replace(`/quan-tri/bai-viet/${data.article.id}/sua`);
    // Khi replace cùng URL, watcher không chạy: cập nhật version mới để lần lưu tiếp theo hợp lệ.
    loaded.value = data;
    form.value.version = data.version;
    initial.value = JSON.stringify(form.value);
  } catch (cause) { error.value = errorMessage(cause); }
  finally { busy.value = false; }
}
// Hoàn tất Promise route guard: true tiếp tục điều hướng, false hủy điều hướng và giữ form.
function finishLeave(allow: boolean) { leaveDialog.value = false; resolveLeave?.(allow); resolveLeave = undefined; }
// Guard này xử lý rời trang trong ứng dụng. Trả Promise giúp router chờ lựa chọn từ Dialog.
onBeforeRouteLeave(() => {
  if (!dirty.value || busy.value) return true;
  leaveDialog.value = true;
  return new Promise<boolean>(resolve => { resolveLeave = resolve; });
});
// Reload/đóng tab không đi qua router: dùng beforeunload để trình duyệt hỏi bằng hộp thoại của nó.
// Nội dung hộp thoại hệ thống do trình duyệt quyết định, không dùng Dialog của Vue trong trường hợp này.
function warnBeforeUnload(event: BeforeUnloadEvent) { if (dirty.value) { event.preventDefault(); event.returnValue = ''; } }
window.addEventListener('beforeunload', warnBeforeUnload);
// Dọn listener và kết thúc lựa chọn còn chờ khi component bị tháo để không giữ tham chiếu cũ.
onBeforeUnmount(() => { window.removeEventListener('beforeunload', warnBeforeUnload); resolveLeave?.(false); });
</script>

<template>
  <!-- v-model="form.xxx" ghi trực tiếp vào ref form; readonly/busy khóa chỉnh sửa khi cần.
       Editor của PrimeVue bọc Quill: các lớp ql-bold/ql-list/... trong slot #toolbar là lệnh định dạng.
       Select hiện name nhưng lưu slug; InputNumber giữ giá trị số trong khoảng 1–60.
       Submit chỉ lưu nháp; gửi duyệt/xuất bản thực hiện riêng ở danh sách bài viết. -->
  <div class="admin-page-heading"><div><RouterLink to="/quan-tri" class="editor-back"><i class="pi pi-arrow-left" aria-hidden="true"></i> Danh sách bài viết</RouterLink><h1>{{ loaded ? 'Biên tập câu chuyện' : 'Một câu chuyện mới' }}<span class="title-dot">.</span></h1><p>{{ readonly ? 'Xem nội dung bài viết. Quản lý trạng thái tại danh sách bài viết.' : 'Viết rõ ràng, kể chân thật, tạo thêm một góc nhìn.' }}</p></div><Tag v-if="loaded" :value="statusLabels[loaded.status]" :severity="statusSeverity(loaded.status)" /></div>
  <div v-if="loading" class="editor-loading" role="status"><i class="pi pi-spin pi-spinner" aria-hidden="true"></i> Đang mở bài viết…</div>
  <template v-else><Message v-if="error" severity="error" :closable="false">{{ error }}</Message><Button v-if="loadFailed" label="Thử lại" icon="pi pi-refresh" @click="retry++" /><form v-if="!loadFailed" class="editor-form" @submit.prevent="save"><div class="editor-main admin-panel"><Message v-if="loaded?.reviewNote" severity="warn" :closable="false"><strong>Góp ý từ biên tập viên:</strong> {{ loaded.reviewNote }}</Message><div class="form-field"><label for="article-title">Tiêu đề <span class="required">*</span></label><InputText id="article-title" v-model="form.title" required minlength="8" maxlength="200" placeholder="Một tiêu đề khiến người đọc muốn dừng lại…" :disabled="readonly || busy" /><small>{{ form.title.length }}/200 ký tự · Tối thiểu 8 ký tự</small></div><div class="form-field"><label for="article-summary">Mô tả ngắn <span class="required">*</span></label><Textarea id="article-summary" v-model="form.summary" required minlength="20" maxlength="600" rows="4" auto-resize placeholder="Tóm tắt câu chuyện trong vài dòng…" :disabled="readonly || busy" /><small>{{ form.summary.length }}/600 ký tự · Tối thiểu 20 ký tự</small></div><div class="form-field"><label id="content-label">Nội dung bài viết <span class="required">*</span></label><Editor :key="editorKey" v-model="form.content" editor-style="height: 390px" :readonly="readonly || busy" aria-labelledby="content-label"><template #toolbar><span class="ql-formats"><select class="ql-header" aria-label="Kiểu đoạn văn"><option value="2">Tiêu đề 2</option><option value="3">Tiêu đề 3</option><option selected>Đoạn văn</option></select></span><span class="ql-formats"><button type="button" class="ql-bold" aria-label="In đậm"></button><button type="button" class="ql-italic" aria-label="In nghiêng"></button><button type="button" class="ql-underline" aria-label="Gạch chân"></button></span><span class="ql-formats"><button type="button" class="ql-list" value="ordered" aria-label="Danh sách số"></button><button type="button" class="ql-list" value="bullet" aria-label="Danh sách dấu chấm"></button><button type="button" class="ql-blockquote" aria-label="Trích dẫn"></button></span><span class="ql-formats"><button type="button" class="ql-link" aria-label="Chèn liên kết"></button><button type="button" class="ql-clean" aria-label="Xóa định dạng"></button></span></template></Editor><small>Tối thiểu 30 ký tự văn bản. Nội dung được lọc HTML trước khi lưu và hiển thị.</small></div></div><aside class="editor-options"><section class="admin-panel"><h2>Thông tin bài viết</h2><div class="form-field"><label for="article-category">Chuyên mục <span class="required">*</span></label><Select v-model="form.categorySlug" input-id="article-category" :options="categories.items" option-label="name" option-value="slug" placeholder="Chọn chuyên mục" :disabled="readonly || busy" required /></div><div class="form-field"><label for="article-image">Ảnh minh họa <span class="required">*</span></label><InputText id="article-image" v-model="form.imageUrl" type="url" pattern="https://.*" maxlength="2000" required placeholder="https://…" :disabled="readonly || busy" /><small>Đường dẫn HTTPS tới hình ảnh công khai.</small></div><div class="form-field"><label for="reading-minutes">Thời gian đọc (phút)</label><InputNumber v-model="form.readingMinutes" input-id="reading-minutes" :min="1" :max="60" :use-grouping="false" show-buttons required :disabled="readonly || busy" /></div></section><section class="admin-panel editor-publish-panel"><span class="eyebrow">CHUẨN BỊ XUẤT BẢN</span><p>Bản nháp được lưu riêng trong tòa soạn. Sau khi lưu, gửi duyệt tại danh sách bài viết.</p><p v-if="loaded?.status === 'Approved'" class="muted">Sửa bài đã duyệt sẽ chuyển bài về nháp và cần duyệt lại.</p><Button v-if="!readonly" label="Lưu bản nháp" icon="pi pi-save" type="submit" :loading="busy" /><Button label="Xem trước" icon="pi pi-eye" outlined type="button" @click="preview = true" /><RouterLink v-if="loaded?.status === 'Published'" :to="`/bai-viet/${loaded.article.id}`" target="_blank" rel="noopener" class="text-button">Xem bài công khai <i class="pi pi-arrow-up-right" aria-hidden="true"></i></RouterLink></section></aside></form></template>
  <!-- Preview hiển thị HTML đã lọc, không gửi API. breakpoints thu nhỏ Dialog theo màn hình. -->
  <Dialog v-model:visible="preview" header="Xem trước bài viết" modal :style="{ width: '850px' }" :breakpoints="{ '900px': '95vw' }"><article class="editor-preview"><span class="category">{{ categories.items.find(c => c.slug === form.categorySlug)?.name || 'Chuyên mục' }}</span><h1>{{ form.title || 'Tiêu đề bài viết' }}</h1><p class="article-lead">{{ form.summary }}</p><div class="rich-content" v-html="sanitized"></div></article></Dialog>
  <!-- Hai nút giải quyết Promise đang được router chờ trong onBeforeRouteLeave. -->
  <Dialog v-model:visible="leaveDialog" header="Bạn có thay đổi chưa lưu" modal :closable="false" :style="{ width: '440px' }" :breakpoints="{ '600px': '92vw' }"><p class="dialog-copy">Nếu rời trang, những thay đổi này sẽ không được lưu.</p><div class="dialog-actions"><Button label="Ở lại chỉnh sửa" severity="secondary" @click="finishLeave(false)" /><Button label="Rời trang" severity="danger" @click="finishLeave(true)" /></div></Dialog>
</template>
