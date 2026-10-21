<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue';
import Button from 'primevue/button';
import AdminOverview from '../../components/AdminOverview.vue';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Dialog from 'primevue/dialog';
import Message from 'primevue/message';
import { useToast } from 'primevue/usetoast';
import { createRole, listRoles } from '../../services/admin';
import { errorMessage } from '../../services/http';
import { roleDescriptions, roleLabels, roleOptions } from '../../utils/roles';
import type { CreateRoleRequest, ManagedRole } from '../../types/admin';

const toast = useToast();
const items = ref<ManagedRole[]>([]);
const loading = ref(true);
const error = ref('');
const visible = ref(false);
const busy = ref(false);
const saveError = ref('');
const form = ref<CreateRoleRequest>({ name: '', baseRole: 'Author' });
const overview = computed(() => [
  { label: 'Vai trò đã tạo', value: items.value.length, hint: 'Hệ thống và vai trò tùy chỉnh', icon: 'pi-shield', tone: 'violet' as const },
  { label: 'Thành viên được gán', value: items.value.reduce((sum, role) => sum + role.userCount, 0), hint: 'Được phân quyền theo vai trò', icon: 'pi-users', tone: 'blue' as const },
  { label: 'Mức quyền sử dụng', value: new Set(items.value.map(role => role.baseRole)).size, hint: 'Quản trị, biên tập, tác giả, độc giả', icon: 'pi-sliders-h', tone: 'teal' as const },
]);
let controller: AbortController | undefined;
async function load() {
  controller?.abort();
  const request = controller = new AbortController();
  loading.value = true; error.value = '';
  try { const data = await listRoles(request.signal); if (!request.signal.aborted) items.value = data; }
  catch (cause) { if (!request.signal.aborted) error.value = errorMessage(cause); }
  finally { if (!request.signal.aborted) loading.value = false; }
}
function open() { form.value = { name: '', baseRole: 'Author' }; saveError.value = ''; visible.value = true; }
async function save() {
  if (busy.value) return;
  busy.value = true; saveError.value = '';
  try {
    await createRole(form.value);
    visible.value = false;
    toast.add({ severity: 'success', summary: 'Đã tạo vai trò', life: 3500 });
    await load();
  } catch (cause) { saveError.value = errorMessage(cause); }
  finally { busy.value = false; }
}
onMounted(load);
onBeforeUnmount(() => controller?.abort());
</script>

<template>
  <div class="admin-page-heading"><div><span class="eyebrow">QUẢN TRỊ HỆ THỐNG</span><h1>Vai trò<span class="title-dot">.</span></h1><p>Tạo vai trò cho đội ngũ và chọn mức quyền phù hợp.</p></div><Button label="Tạo vai trò" icon="pi pi-plus" @click="open" /></div>
  <AdminOverview :items="overview" :loading="loading" />
  <section class="admin-panel">
    <div class="admin-panel-heading"><h2>Danh sách vai trò</h2><Button label="Tải lại" icon="pi pi-refresh" text :disabled="loading" @click="load" /></div>
    <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>
    <DataTable :value="items" :loading="loading" data-key="id" scrollable table-style="min-width: 620px">
      <template #empty>Chưa có vai trò nào.</template>
      <Column header="Tên vai trò"><template #body="{ data }"><div class="record-identity"><span class="record-avatar is-violet"><i class="pi pi-shield" aria-hidden="true"></i></span><div><strong>{{ data.name }}</strong><small>{{ ['Admin', 'Editor', 'Author', 'Reader'].includes(data.id) ? 'Vai trò hệ thống' : 'Vai trò tùy chỉnh' }}</small></div></div></template></Column>
      <Column header="Mức quyền"><template #body="{ data }"><span class="role-badge" :class="`role-${data.baseRole.toLowerCase()}`">{{ roleLabels[data.baseRole as keyof typeof roleLabels] }}</span></template></Column>
      <Column header="Quyền được cấp"><template #body="{ data }">{{ roleDescriptions[data.baseRole as keyof typeof roleDescriptions] }}</template></Column>
      <Column header="Người dùng"><template #body="{ data }"><span class="count-badge-pill"><i class="pi pi-user" aria-hidden="true"></i>{{ data.userCount }}</span></template></Column>
    </DataTable>
  </section>
  <Dialog class="admin-dialog" v-model:visible="visible" header="Tạo vai trò" modal :style="{ width: '500px' }" :breakpoints="{ '600px': '92vw' }" :closable="!busy" :close-on-escape="!busy">
    <form class="management-form" @submit.prevent="save"><fieldset :disabled="busy">
      <div class="form-field"><label for="role-name">Tên vai trò</label><InputText id="role-name" v-model="form.name" required maxlength="80" placeholder="Ví dụ: Cộng tác viên" /></div>
      <div class="form-field"><label for="role-base">Mức quyền kế thừa</label><Select v-model="form.baseRole" input-id="role-base" :options="roleOptions" option-label="label" option-value="value" :disabled="busy" /><small>{{ roleDescriptions[form.baseRole] }}</small></div>
      <Message severity="info" :closable="false">Vai trò mới kế thừa đầy đủ quyền của mức đã chọn. Chọn vai trò này khi tạo hoặc phân quyền người dùng.</Message>
      <Message v-if="saveError" severity="error" :closable="false">{{ saveError }}</Message>
      <div class="dialog-actions"><Button label="Hủy" type="button" severity="secondary" :disabled="busy" @click="visible = false" /><Button label="Lưu vai trò" type="submit" :loading="busy" /></div>
    </fieldset></form>
  </Dialog>
</template>
