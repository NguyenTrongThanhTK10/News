<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { RouterLink } from 'vue-router';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import DataTable, { type DataTablePageEvent } from 'primevue/datatable';
import Column from 'primevue/column';
import Tag from 'primevue/tag';
import Select from 'primevue/select';
import Dialog from 'primevue/dialog';
import Textarea from 'primevue/textarea';
import Message from 'primevue/message';
import { useToast } from 'primevue/usetoast';
import { useAuthStore } from '../../stores/auth';
import { deleteArticle, listAdminArticles, transitionArticle } from '../../services/admin';
import { errorMessage } from '../../services/http';
import { actionLabels, allowedActions, canDelete, statusLabels, statusSeverity } from '../../utils/workflow';
import { formatDate } from '../../utils/format';
import type { AdminArticle, ArticleAction, ArticleStatus } from '../../types/admin';

const auth = useAuthStore();
const toast = useToast();
const items = ref<AdminArticle[]>([]);
const loading = ref(true);
const error = ref('');
const search = ref('');
const status = ref<ArticleStatus | null>(null);
const page = ref(0);
const pageSize = ref(10);
const total = ref(0);
const statusOptions = Object.entries(statusLabels).map(([value, label]) => ({ value, label }));
const selected = ref<AdminArticle | null>(null);
const action = ref<ArticleAction | 'delete'>('submit');
const dialogOpen = ref(false);
const note = ref('');
const actionError = ref('');
const busy = ref(false);
const dialogTitle = computed(() => action.value === 'delete' ? 'Xóa bài viết' : actionLabels[action.value]);
let controller: AbortController | undefined;
let debounce: ReturnType<typeof setTimeout> | undefined;
let request = 0;
async function load() {
  controller?.abort(); controller = new AbortController();
  const current = ++request;
  loading.value = true; error.value = '';
  try {
    const data = await listAdminArticles({ page: page.value + 1, pageSize: pageSize.value, search: search.value.trim() || undefined, status: status.value || undefined }, controller.signal);
    if (current !== request) return;
    if (data.items.length === 0 && data.totalCount > 0 && page.value > 0) { page.value = Math.max(0, Math.ceil(data.totalCount / pageSize.value) - 1); return; }
    items.value = data.items; total.value = data.totalCount;
  } catch (cause) { if (current === request && !controller.signal.aborted) error.value = errorMessage(cause); }
  finally { if (current === request) loading.value = false; }
}
watch([page, pageSize, status], () => { void load(); }, { immediate: true });
watch(search, () => {
  clearTimeout(debounce);
  debounce = setTimeout(() => { if (page.value) page.value = 0; else void load(); }, 300);
});
watch(status, () => { page.value = 0; });
onBeforeUnmount(() => { controller?.abort(); clearTimeout(debounce); });
function paginate(event: DataTablePageEvent) { page.value = event.page; pageSize.value = event.rows; }
function confirm(item: AdminArticle, operation: ArticleAction | 'delete') {
  selected.value = item; action.value = operation; note.value = ''; actionError.value = ''; dialogOpen.value = true;
}
async function execute() {
  if (!selected.value) return;
  busy.value = true; actionError.value = '';
  try {
    if (action.value === 'delete') await deleteArticle(selected.value);
    else await transitionArticle(selected.value, action.value, note.value);
    dialogOpen.value = false;
    toast.add({ severity: 'success', summary: 'Đã cập nhật bài viết', detail: selected.value.article.title, life: 3500 });
    await load();
  } catch (cause) { actionError.value = errorMessage(cause); }
  finally { busy.value = false; }
}
</script>

<template>
  <div class="admin-page-heading"><div><span class="eyebrow">BIÊN TẬP & XUẤT BẢN</span><h1>Tòa soạn của bạn<span class="title-dot">.</span></h1><p>{{ auth.canReview ? 'Chọn lọc, duyệt và đưa những câu chuyện hay đến độc giả.' : 'Viết câu chuyện của bạn và gửi đến biên tập viên.' }}</p></div><RouterLink to="/quan-tri/bai-viet/tao-moi" class="button button-primary"><i class="pi pi-plus" aria-hidden="true"></i> Soạn bài mới</RouterLink></div>
  <div class="admin-summary"><div><span class="stat-icon"><i class="pi pi-file-edit" aria-hidden="true"></i></span><div><strong>{{ total }}</strong><span>Bài viết phù hợp</span></div></div><div><span class="stat-icon"><i class="pi pi-user" aria-hidden="true"></i></span><div><strong>{{ auth.roleLabel }}</strong><span>{{ auth.canReview ? 'Quản lý nội dung toàn tòa soạn' : 'Quản lý bài viết của bạn' }}</span></div></div><div><span class="stat-icon"><i class="pi pi-verified" aria-hidden="true"></i></span><div><strong>Biên tập trước, xuất bản sau</strong><span>Nháp → Chờ duyệt → Đã duyệt → Xuất bản</span></div></div></div>
  <section class="admin-panel" aria-labelledby="article-list-title"><div class="admin-panel-heading"><h2 id="article-list-title">Danh sách bài viết</h2><Button icon="pi pi-refresh" label="Tải lại" text :loading="loading" @click="load" /></div><div class="admin-filters"><div class="admin-search"><i class="pi pi-search" aria-hidden="true"></i><InputText v-model="search" aria-label="Tìm bài viết trong tòa soạn" placeholder="Tìm tiêu đề hoặc từ khóa…" maxlength="200" /></div><Select v-model="status" :options="statusOptions" option-label="label" option-value="value" placeholder="Tất cả trạng thái" show-clear aria-label="Lọc trạng thái" /></div><Message v-if="error" severity="error" :closable="false">{{ error }}</Message><DataTable :value="items" :loading="loading" lazy paginator :rows="pageSize" :first="page * pageSize" :total-records="total" :rows-per-page-options="[10, 20, 30]" data-key="article.id" table-style="min-width: 760px" scrollable @page="paginate"><template #empty><div class="table-empty"><i class="pi pi-inbox" aria-hidden="true"></i><p>Chưa có bài viết phù hợp.</p><RouterLink to="/quan-tri/bai-viet/tao-moi" class="text-button">Viết câu chuyện đầu tiên</RouterLink></div></template><Column header="Bài viết" style="min-width: 270px; max-width: 360px"><template #body="{ data }"><RouterLink :to="`/quan-tri/bai-viet/${data.article.id}/sua`" class="table-article-title">{{ data.article.title }}</RouterLink><span class="table-summary">{{ data.article.summary }}</span></template></Column><Column header="Chuyên mục" style="min-width: 110px"><template #body="{ data }"><span class="table-category">{{ data.article.category }}</span></template></Column><Column header="Trạng thái" style="min-width: 140px"><template #body="{ data }"><Tag :value="statusLabels[data.status as ArticleStatus]" :severity="statusSeverity(data.status)" /></template></Column><Column header="Tác giả" style="min-width: 100px"><template #body="{ data }">{{ data.article.author }}</template></Column><Column header="Cập nhật" style="min-width: 100px"><template #body="{ data }">{{ formatDate(data.updatedAt) }}</template></Column><Column header="Thao tác" frozen align-frozen="right" style="min-width: 180px"><template #body="{ data }"><div class="table-actions"><RouterLink :to="`/quan-tri/bai-viet/${data.article.id}/sua`" class="table-open">Mở</RouterLink><Button v-for="operation in allowedActions(data, auth.user)" :key="operation" :label="actionLabels[operation]" :severity="operation === 'reject' ? 'secondary' : undefined" size="small" text @click="confirm(data, operation)" /><Button v-if="canDelete(data, auth.user)" icon="pi pi-trash" severity="danger" size="small" text :aria-label="`Xóa bài: ${data.article.title}`" @click="confirm(data, 'delete')" /></div></template></Column></DataTable></section>
  <Dialog v-model:visible="dialogOpen" :header="dialogTitle" modal :style="{ width: '480px' }" :breakpoints="{ '600px': '92vw' }" :closable="!busy" :close-on-escape="!busy"><form class="workflow-dialog" @submit.prevent="execute"><p><strong>{{ selected?.article.title }}</strong></p><p v-if="action === 'delete'">Bài viết sẽ bị gỡ khỏi danh sách và trang tin.</p><p v-else-if="action === 'publish'">Bài viết đã được duyệt sẽ hiển thị công khai trên trang tin.</p><p v-else>Trạng thái bài viết sẽ được cập nhật sau khi xác nhận.</p><div v-if="action === 'reject'" class="form-field"><label for="review-note">Lý do trả lại <span class="required">*</span></label><Textarea id="review-note" v-model="note" required maxlength="1000" rows="4" placeholder="Góp ý cụ thể để tác giả chỉnh sửa…" /></div><Message v-if="actionError" severity="error" :closable="false">{{ actionError }}</Message><div class="dialog-actions"><Button label="Hủy" severity="secondary" type="button" :disabled="busy" @click="dialogOpen = false" /><Button :label="dialogTitle" :severity="action === 'delete' ? 'danger' : undefined" type="submit" :loading="busy" /></div></form></Dialog>
</template>
