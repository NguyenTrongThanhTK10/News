<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { RouterLink } from 'vue-router';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import DataTable, { type DataTablePageEvent } from 'primevue/datatable';
import Column from 'primevue/column';
import Tag from 'primevue/tag';
import NewsImage from '../../components/NewsImage.vue';
import ArticleActions from '../../components/ArticleActions.vue';
import Select from 'primevue/select';
import Dialog from 'primevue/dialog';
import Textarea from 'primevue/textarea';
import Message from 'primevue/message';
import { useToast } from 'primevue/usetoast';
import { useAuthStore } from '../../stores/auth';
import { deleteArticle, listAdminArticles, transitionArticle } from '../../services/admin';
import { errorMessage } from '../../services/http';
import { actionLabels, statusLabels, statusSeverity } from '../../utils/workflow';
import { formatDate } from '../../utils/format';
import type { AdminArticle, ArticleAction, ArticleStatus } from '../../types/admin';

// Trang này ghép bộ lọc → API phân trang → DataTable, cùng Dialog cho các thao tác biên tập.
// Quyền hiển thị lấy từ auth/workflow; quyền thực thi và trạng thái hợp lệ do server quyết định.
const auth = useAuthStore();
const toast = useToast();
const items = ref<AdminArticle[]>([]);
const loading = ref(true);
const error = ref('');
const search = ref('');
const status = ref<ArticleStatus | null>(null);
// DataTable đánh số trang từ 0, trong khi API dùng từ 1; chuyển đổi trong hàm load().
const page = ref(0);
const pageSize = ref(10);
const total = ref(0);
const metrics = ref<{ label: string; count: number; status: ArticleStatus | null; icon: string; tone: string; hint: string }[]>([
  { label: 'Tất cả bài viết', count: 0, status: null, icon: 'pi-file-edit', tone: 'blue', hint: 'Toàn bộ nội dung' },
  { label: 'Đã xuất bản', count: 0, status: 'Published', icon: 'pi-globe', tone: 'teal', hint: 'Đang hiển thị trên trang tin' },
  { label: 'Chờ duyệt', count: 0, status: 'PendingReview', icon: 'pi-clock', tone: 'amber', hint: 'Chờ bước biên tập tiếp theo' },
  { label: 'Bản nháp', count: 0, status: 'Draft', icon: 'pi-pencil', tone: 'violet', hint: 'Những câu chuyện đang viết' },
]);
const metricsLoading = ref(true);
const metricsError = ref('');
let metricsController: AbortController | undefined;
async function loadMetrics() {
  metricsController?.abort();
  const current = metricsController = new AbortController();
  metricsLoading.value = true; metricsError.value = '';
  try {
    const counts = await Promise.all(metrics.value.map(metric => listAdminArticles({ page: 1, pageSize: 1, status: metric.status || undefined }, current.signal)));
    if (!current.signal.aborted) metrics.value = metrics.value.map((metric, index) => ({ ...metric, count: counts[index].totalCount }));
  } catch { if (!current.signal.aborted) metricsError.value = 'Chưa tải được thống kê bài viết.'; }
  finally { if (!current.signal.aborted) metricsLoading.value = false; }
}
function selectStatus(value: ArticleStatus | null) { status.value = value; page.value = 0; }
async function refresh() { await Promise.all([load(), loadMetrics()]); }
void loadMetrics();
// Chuyển bảng trạng thái thành { value, label } để Select hiện tiếng Việt nhưng gửi mã API.
const statusOptions = Object.entries(statusLabels).map(([value, label]) => ({ value, label }));
// selected/action giữ đối tượng và thao tác chờ xác nhận; mở Dialog chưa gọi API thay đổi dữ liệu.
const selected = ref<AdminArticle | null>(null);
const action = ref<ArticleAction | 'delete'>('submit');
const dialogOpen = ref(false);
const note = ref('');
const actionError = ref('');
const busy = ref(false);
const dialogTitle = computed(() => action.value === 'delete' ? 'Xóa bài viết' : actionLabels[action.value]);
// Controller hủy request cũ; số request giúp nhận diện response mới nhất ngay cả khi gọi liên tiếp.
// debounce giữ timer tìm kiếm để không gửi API theo từng phím gõ.
let controller: AbortController | undefined;
let debounce: ReturnType<typeof setTimeout> | undefined;
let request = 0;
// Luồng tải: đặt loading → lấy đúng trang/bộ lọc → nhận response mới nhất → cập nhật bảng.
async function load() {
  controller?.abort(); controller = new AbortController();
  const current = ++request;
  loading.value = true; error.value = '';
  try {
    const data = await listAdminArticles({ page: page.value + 1, pageSize: pageSize.value, search: search.value.trim() || undefined, status: status.value || undefined }, controller.signal);
    if (current !== request) return;
    // Sau khi xóa/lọc, trang cuối có thể rỗng: lùi về trang cuối còn dữ liệu.
    // Gán page sẽ kích hoạt watcher tải lại, không cần gọi đệ quy load().
    if (data.items.length === 0 && data.totalCount > 0 && page.value > 0) { page.value = Math.max(0, Math.ceil(data.totalCount / pageSize.value) - 1); return; }
    items.value = data.items; total.value = data.totalCount;
  } catch (cause) { if (current === request && !controller.signal.aborted) error.value = errorMessage(cause); }
  finally { if (current === request) loading.value = false; }
}
// Theo dõi phân trang/bộ lọc và tải ngay lần đầu; tìm kiếm riêng chờ 300ms sau lần gõ cuối.
watch([page, pageSize, status], () => { void load(); }, { immediate: true });
watch(search, () => {
  clearTimeout(debounce);
  debounce = setTimeout(() => { if (page.value) page.value = 0; else void load(); }, 300);
});
// Đổi trạng thái bắt đầu từ trang đầu; khi rời trang hủy cả request lẫn timer còn chờ.
watch(status, () => { page.value = 0; });
onBeforeUnmount(() => { controller?.abort(); metricsController?.abort(); clearTimeout(debounce); });
// DataTable phát @page kèm trang/số dòng; cập nhật ref để watcher thực hiện tải dữ liệu mới.
function paginate(event: DataTablePageEvent) { page.value = event.page; pageSize.value = event.rows; }
// Xóa lỗi/góp ý lần trước mỗi khi chọn thao tác mới, rồi mở Dialog để người dùng xem và xác nhận.
function confirm(item: AdminArticle, operation: ArticleAction | 'delete') {
  selected.value = item; action.value = operation; note.value = ''; actionError.value = ''; dialogOpen.value = true;
}
// Chỉ gọi API sau khi submit Dialog. Thành công thì đóng, báo Toast và tải lại dữ liệu server.
// Thất bại giữ Dialog mở cùng lỗi, giúp người dùng biết thao tác chưa được thực hiện thành công.
async function execute() {
  if (!selected.value) return;
  busy.value = true; actionError.value = '';
  try {
    if (action.value === 'delete') await deleteArticle(selected.value);
    else await transitionArticle(selected.value, action.value, note.value);
    dialogOpen.value = false;
    toast.add({ severity: 'success', summary: 'Đã cập nhật bài viết', detail: selected.value.article.title, life: 3500 });
    await refresh();
  } catch (cause) { actionError.value = errorMessage(cause); }
  finally { busy.value = false; }
}
</script>

<template>
  <!-- DataTable dùng lazy: phân trang trên server, items chỉ chứa trang đang xem.
       :first là chỉ số dòng bắt đầu = page * pageSize, :total-records là tổng toàn bộ kết quả.
       Các <template #body="{ data }"> là scoped slot: bảng đưa dữ liệu dòng vào biến data.
       ArticleActions chọn nút theo quyền; confirm chỉ mở hộp thoại, chưa sửa dữ liệu. -->
  <div class="admin-page-heading article-dashboard-heading"><div><span class="eyebrow">BIÊN TẬP & XUẤT BẢN</span><h1>Tòa soạn của bạn<span class="title-dot">.</span></h1><p>{{ auth.canReview ? 'Chọn lọc, duyệt và đưa những câu chuyện hay đến độc giả.' : 'Viết câu chuyện của bạn và gửi đến biên tập viên.' }}</p></div><RouterLink to="/quan-tri/bai-viet/tao-moi" class="button button-primary"><i class="pi pi-plus" aria-hidden="true"></i> Soạn bài mới</RouterLink></div>
  <div class="admin-metrics" :aria-busy="metricsLoading">
    <button v-for="metric in metrics" :key="metric.label" type="button" class="metric-card" :class="[`tone-${metric.tone}`, { 'is-active': status === metric.status }]" :aria-pressed="status === metric.status" @click="selectStatus(metric.status)">
      <span class="overview-card-top"><span>{{ metric.label }}</span><span class="overview-icon"><i :class="['pi', metric.icon]" aria-hidden="true"></i></span></span>
      <strong class="overview-value">{{ metricsLoading || metricsError ? '—' : metric.count.toLocaleString('vi-VN') }}</strong>
      <span class="metric-card-bottom"><span>{{ metric.hint }}</span><i class="pi pi-arrow-up-right" aria-hidden="true"></i></span>
    </button>
  </div>
  <p v-if="metricsError" class="metrics-error" role="status">{{ metricsError }} <button type="button" class="text-button" @click="loadMetrics">Thử lại thống kê</button></p>
  <section class="admin-panel" aria-labelledby="article-list-title"><div class="admin-panel-heading"><div class="panel-title-group"><span class="panel-title-icon"><i class="pi pi-align-left" aria-hidden="true"></i></span><div><h2 id="article-list-title">Danh sách bài viết</h2><p>{{ total.toLocaleString('vi-VN') }} kết quả · Quản lý và theo dõi nội dung</p></div></div><Button icon="pi pi-refresh" label="Tải lại" text :loading="loading" @click="refresh" /></div><div class="admin-filters"><div class="admin-search"><i class="pi pi-search" aria-hidden="true"></i><InputText v-model="search" aria-label="Tìm bài viết trong tòa soạn" placeholder="Tìm tiêu đề hoặc từ khóa…" maxlength="200" /></div><Select v-model="status" :options="statusOptions" option-label="label" option-value="value" placeholder="Tất cả trạng thái" show-clear aria-label="Lọc trạng thái" /></div><Message v-if="error" severity="error" :closable="false">{{ error }}</Message><DataTable :value="items" :loading="loading" lazy paginator :rows="pageSize" :first="page * pageSize" :total-records="total" :rows-per-page-options="[10, 20, 30]" data-key="article.id" table-style="min-width: 760px" scrollable @page="paginate"><template #empty><div class="table-empty"><i class="pi pi-inbox" aria-hidden="true"></i><p>Chưa có bài viết phù hợp.</p><RouterLink to="/quan-tri/bai-viet/tao-moi" class="text-button">Viết câu chuyện đầu tiên</RouterLink></div></template><Column header="Bài viết" style="min-width: 270px; max-width: 360px"><template #body="{ data }"><div class="table-article-cell"><NewsImage :src="data.article.imageUrl" :alt="data.article.title" /><div><RouterLink :to="`/quan-tri/bai-viet/${data.article.id}/sua`" class="table-article-title">{{ data.article.title }}</RouterLink><span class="table-summary">{{ data.article.summary }}</span><span class="table-article-id">#{{ data.article.id }} <span>·</span> {{ data.article.readingMinutes }} phút đọc</span></div></div></template></Column><Column header="Chuyên mục" style="min-width: 110px"><template #body="{ data }"><span class="table-category">{{ data.article.category }}</span></template></Column><Column header="Trạng thái" style="min-width: 140px"><template #body="{ data }"><Tag :value="statusLabels[data.status as ArticleStatus]" :severity="statusSeverity(data.status)" /></template></Column><Column header="Tác giả" style="min-width: 100px"><template #body="{ data }"><span class="table-author"><span class="table-avatar">{{ data.article.author.charAt(0) }}</span>{{ data.article.author }}</span></template></Column><Column header="Cập nhật" style="min-width: 100px"><template #body="{ data }">{{ formatDate(data.updatedAt) }}</template></Column><Column header="Thao tác" frozen align-frozen="right" header-class="article-actions-heading" body-class="article-actions-cell" style="min-width: 236px; width: 236px"><template #body="{ data }"><ArticleActions :article="data" @choose="confirm(data, $event)" /></template></Column></DataTable></section>
  <!-- v-model:visible đồng bộ mở/đóng Dialog. modal chặn tương tác nền;
       busy chặn nút đóng/Escape trong lúc gửi. Trả bài yêu cầu note qua Textarea required. -->
  <Dialog class="admin-dialog" v-model:visible="dialogOpen" :header="dialogTitle" modal :style="{ width: '480px' }" :breakpoints="{ '600px': '92vw' }" :closable="!busy" :close-on-escape="!busy"><form class="workflow-dialog" @submit.prevent="execute"><p><strong>{{ selected?.article.title }}</strong></p><p v-if="action === 'delete'">Bài viết sẽ bị gỡ khỏi danh sách và trang tin.</p><p v-else-if="action === 'publish'">Bài viết đã được duyệt sẽ hiển thị công khai trên trang tin.</p><p v-else>Trạng thái bài viết sẽ được cập nhật sau khi xác nhận.</p><div v-if="action === 'reject'" class="form-field"><label for="review-note">Lý do trả lại <span class="required">*</span></label><Textarea id="review-note" v-model="note" required maxlength="1000" rows="4" placeholder="Góp ý cụ thể để tác giả chỉnh sửa…" /></div><Message v-if="actionError" severity="error" :closable="false">{{ actionError }}</Message><div class="dialog-actions"><Button label="Hủy" severity="secondary" type="button" :disabled="busy" @click="dialogOpen = false" /><Button :label="dialogTitle" :severity="action === 'delete' ? 'danger' : undefined" type="submit" :loading="busy" /></div></form></Dialog>
</template>
