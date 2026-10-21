<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue';
import Button from 'primevue/button';
import AdminOverview from '../../components/AdminOverview.vue';
import InputText from 'primevue/inputtext';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Dialog from 'primevue/dialog';
import Message from 'primevue/message';
import { useToast } from 'primevue/usetoast';
import { createCategory, listAdminCategories } from '../../services/admin';
import { errorMessage } from '../../services/http';
import { useCategoriesStore } from '../../stores/categories';
import type { Category } from '../../types/news';

const toast = useToast();
const categories = useCategoriesStore();
const items = ref<Category[]>([]);
const loading = ref(true);
const error = ref('');
const visible = ref(false);
const busy = ref(false);
const saveError = ref('');
const name = ref('');
const slug = ref('');
const slugEdited = ref(false);
watch(name, value => {
  if (!slugEdited.value) slug.value = value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/[đĐ]/g, 'd').toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '').slice(0, 80).replace(/-$/, '');
});
const overview = computed(() => [
  { label: 'Chuyên mục', value: items.value.length, hint: 'Tổ chức nội dung theo chủ đề', icon: 'pi-folder-open', tone: 'blue' as const },
  { label: 'Bài viết', value: items.value.reduce((sum, category) => sum + category.count, 0), hint: 'Nội dung trong các chuyên mục', icon: 'pi-file-edit', tone: 'teal' as const },
  { label: 'Chờ nội dung mới', value: items.value.filter(category => category.count === 0).length, hint: 'Chuyên mục chưa có bài viết', icon: 'pi-sparkles', tone: 'amber' as const },
]);
let controller: AbortController | undefined;
async function load() {
  controller?.abort();
  const request = controller = new AbortController();
  loading.value = true; error.value = '';
  try { const data = await listAdminCategories(request.signal); if (!request.signal.aborted) items.value = data; }
  catch (cause) { if (!request.signal.aborted) error.value = errorMessage(cause); }
  finally { if (!request.signal.aborted) loading.value = false; }
}
function open() { name.value = ''; slug.value = ''; slugEdited.value = false; saveError.value = ''; visible.value = true; }
async function save() {
  if (busy.value) return;
  busy.value = true; saveError.value = '';
  try {
    await createCategory({ name: name.value, slug: slug.value });
    visible.value = false;
    toast.add({ severity: 'success', summary: 'Đã tạo danh mục', detail: 'Danh mục đã có thể chọn khi soạn bài.', life: 3500 });
    await Promise.all([load(), categories.load(true)]);
  } catch (cause) { saveError.value = errorMessage(cause); }
  finally { busy.value = false; }
}
onMounted(load);
onBeforeUnmount(() => controller?.abort());
</script>

<template>
  <div class="admin-page-heading"><div><span class="eyebrow">QUẢN TRỊ NỘI DUNG</span><h1>Danh mục bài viết<span class="title-dot">.</span></h1><p>Tổ chức chuyên mục để tác giả phân loại bài viết khi soạn.</p></div><Button label="Tạo danh mục" icon="pi pi-plus" @click="open" /></div>
  <AdminOverview :items="overview" :loading="loading" />
  <section class="admin-panel">
    <div class="admin-panel-heading"><h2>Danh sách danh mục</h2><Button label="Tải lại" icon="pi pi-refresh" text :disabled="loading" @click="load" /></div>
    <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>
    <DataTable :value="items" :loading="loading" data-key="slug" scrollable table-style="min-width: 440px">
      <template #empty>Chưa có danh mục nào. Tạo danh mục đầu tiên để bắt đầu viết bài.</template>
      <Column header="Tên danh mục"><template #body="{ data }"><div class="record-identity"><span class="record-avatar is-teal"><i class="pi pi-folder" aria-hidden="true"></i></span><strong>{{ data.name }}</strong></div></template></Column><Column header="Slug"><template #body="{ data }"><code class="slug-chip">{{ data.slug }}</code></template></Column><Column header="Số bài viết"><template #body="{ data }"><span class="count-badge-pill">{{ data.count }} bài viết</span></template></Column>
    </DataTable>
  </section>
  <Dialog class="admin-dialog" v-model:visible="visible" header="Tạo danh mục" modal :style="{ width: '500px' }" :breakpoints="{ '600px': '92vw' }" :closable="!busy" :close-on-escape="!busy">
    <form class="management-form" @submit.prevent="save"><fieldset :disabled="busy">
      <div class="form-field"><label for="category-name">Tên danh mục</label><InputText id="category-name" v-model="name" required maxlength="80" placeholder="Ví dụ: Kinh tế" /></div>
      <div class="form-field"><label for="category-slug">Slug danh mục</label><InputText id="category-slug" v-model="slug" required maxlength="80" pattern="[a-z0-9]+(-[a-z0-9]+)*" placeholder="kinh-te" @input="slugEdited = true" /><small>Tự tạo từ tên; có thể sửa. Dùng chữ thường không dấu, số và dấu gạch nối.</small></div>
      <Message v-if="saveError" severity="error" :closable="false">{{ saveError }}</Message>
      <div class="dialog-actions"><Button label="Hủy" type="button" severity="secondary" :disabled="busy" @click="visible = false" /><Button label="Lưu danh mục" type="submit" :loading="busy" /></div>
    </fieldset></form>
  </Dialog>
</template>
