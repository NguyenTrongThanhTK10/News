<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import Button from 'primevue/button';
import AdminOverview from '../../components/AdminOverview.vue';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import DataTable, { type DataTablePageEvent } from 'primevue/datatable';
import Column from 'primevue/column';
import Dialog from 'primevue/dialog';
import Message from 'primevue/message';
import { useToast } from 'primevue/usetoast';
import { assignUserRole, createUser, listRoles, listUsers } from '../../services/admin';
import { errorMessage } from '../../services/http';
import { roleDescriptions, roleLabels } from '../../utils/roles';
import { useAuthStore } from '../../stores/auth';
import type { CreateUserRequest, ManagedRole, ManagedUser } from '../../types/admin';

const auth = useAuthStore();
const toast = useToast();
const items = ref<ManagedUser[]>([]);
const roles = ref<ManagedRole[]>([]);
const loading = ref(true);
const error = ref('');
const search = ref('');
const page = ref(0);
const total = ref(0);
const pageSize = 10;
const visible = ref(false);
const selected = ref<ManagedUser | null>(null);
const busy = ref(false);
const saveError = ref('');
const form = ref<CreateUserRequest>({ name: '', email: '', password: '', roleId: 'Author' });
const overview = computed(() => [
  { label: 'Thành viên', value: roles.value.reduce((sum, role) => sum + role.userCount, 0), hint: 'Tài khoản trong toàn hệ thống', icon: 'pi-users', tone: 'blue' as const },
  { label: 'Vai trò', value: roles.value.length, hint: 'Phân quyền theo nhiệm vụ', icon: 'pi-shield', tone: 'violet' as const },
  { label: 'Quản trị viên', value: roles.value.filter(role => role.baseRole === 'Admin').reduce((sum, role) => sum + role.userCount, 0), hint: 'Thành viên có quyền quản trị', icon: 'pi-verified', tone: 'teal' as const },
]);
let controller: AbortController | undefined;
let timer: ReturnType<typeof setTimeout> | undefined;

async function load() {
  controller?.abort();
  const request = controller = new AbortController();
  loading.value = true; error.value = '';
  try {
    const [users, availableRoles] = await Promise.all([
      listUsers({ page: page.value + 1, pageSize, search: search.value || undefined }, request.signal),
      listRoles(request.signal),
    ]);
    if (request.signal.aborted) return;
    items.value = users.items; total.value = users.totalCount; roles.value = availableRoles;
  } catch (cause) { if (!request.signal.aborted) error.value = errorMessage(cause); }
  finally { if (!request.signal.aborted) loading.value = false; }
}
watch(search, () => {
  controller?.abort(); clearTimeout(timer);
  timer = setTimeout(() => { page.value = 0; void load(); }, 300);
});
function changePage(event: DataTablePageEvent) { clearTimeout(timer); page.value = event.page; void load(); }
function open(user: ManagedUser | null = null) {
  selected.value = user;
  form.value = { name: '', email: '', password: '', roleId: user?.roleId ?? 'Author' };
  saveError.value = ''; visible.value = true;
}
async function save() {
  if (busy.value || !form.value.roleId) return;
  busy.value = true; saveError.value = '';
  try {
    const isSelf = selected.value?.id === auth.user?.id;
    const roleChanged = selected.value && selected.value.roleId !== form.value.roleId;
    if (selected.value) await assignUserRole(selected.value, form.value.roleId);
    else await createUser(form.value);
    // Mật khẩu chỉ tồn tại trong form lúc tạo, xóa ngay sau khi lưu thành công.
    form.value.password = '';
    visible.value = false;
    toast.add({ severity: 'success', summary: selected.value ? 'Đã cập nhật vai trò' : 'Đã tạo người dùng', life: 3500 });
    if (isSelf && roleChanged) {
      // Đổi vai trò thu hồi phiên; lần tải lại sẽ chuyển về trang đăng nhập.
      window.location.assign('/dang-nhap');
      return;
    }
    page.value = 0;
    await load();
  } catch (cause) { saveError.value = errorMessage(cause); }
  finally { busy.value = false; }
}
watch(visible, value => { if (!value) form.value.password = ''; });
void load();
onBeforeUnmount(() => { controller?.abort(); clearTimeout(timer); form.value.password = ''; });
</script>

<template>
  <div class="admin-page-heading"><div><span class="eyebrow">QUẢN TRỊ HỆ THỐNG</span><h1>Người dùng<span class="title-dot">.</span></h1><p>Tạo tài khoản và phân quyền cho đội ngũ tòa soạn.</p></div><Button label="Tạo người dùng" icon="pi pi-user-plus" :disabled="loading || !roles.length" @click="open()" /></div>
  <AdminOverview :items="overview" :loading="loading" />
  <section class="admin-panel">
    <div class="admin-panel-heading"><h2>Danh sách người dùng <span class="muted">({{ total }})</span></h2><Button label="Tải lại" icon="pi pi-refresh" text :disabled="loading" @click="load" /></div>
    <div class="admin-filters"><div class="admin-search"><i class="pi pi-search" aria-hidden="true"></i><InputText v-model="search" aria-label="Tìm người dùng" placeholder="Tìm theo tên hoặc email…" maxlength="254" /></div></div>
    <Message v-if="error" severity="error" :closable="false">{{ error }}</Message>
    <DataTable :value="items" :loading="loading" data-key="id" lazy paginator :rows="pageSize" :first="page * pageSize" :total-records="total" scrollable table-style="min-width: 680px" @page="changePage">
      <template #empty>{{ search ? 'Không tìm thấy người dùng phù hợp.' : 'Chưa có người dùng nào.' }}</template>
      <Column header="Họ tên"><template #body="{ data }"><div class="record-identity"><span class="record-avatar">{{ data.name.charAt(0) }}</span><strong>{{ data.name }}</strong></div></template></Column><Column field="email" header="Email"><template #body="{ data }"><span class="table-email">{{ data.email }}</span></template></Column><Column field="roleName" header="Vai trò"><template #body="{ data }"><span class="role-badge" :class="`role-${data.role.toLowerCase()}`"><i class="pi pi-shield" aria-hidden="true"></i>{{ data.roleName }}</span></template></Column>
      <Column header="Mức quyền"><template #body="{ data }">{{ roleLabels[data.role as keyof typeof roleLabels] }}</template></Column>
      <Column header="Thao tác"><template #body="{ data }"><Button label="Phân quyền" icon="pi pi-shield" text size="small" @click="open(data)" /></template></Column>
    </DataTable>
  </section>
  <Dialog class="admin-dialog" v-model:visible="visible" :header="selected ? 'Phân quyền người dùng' : 'Tạo người dùng'" modal :style="{ width: '520px' }" :breakpoints="{ '600px': '92vw' }" :closable="!busy" :close-on-escape="!busy">
    <form class="management-form" @submit.prevent="save"><fieldset :disabled="busy">
      <p v-if="selected" class="dialog-copy"><strong>{{ selected.name }}</strong><br>{{ selected.email }}</p>
      <template v-else>
        <div class="form-field"><label for="user-name">Họ tên</label><InputText id="user-name" v-model="form.name" required maxlength="100" autocomplete="name" /></div>
        <div class="form-field"><label for="user-email">Email</label><InputText id="user-email" v-model="form.email" required type="email" maxlength="254" autocomplete="off" /></div>
        <div class="form-field"><label for="user-password">Mật khẩu</label><InputText id="user-password" v-model="form.password" required type="password" minlength="12" maxlength="200" autocomplete="new-password" /><small>Từ 12 đến 200 ký tự.</small></div>
      </template>
      <div class="form-field"><label for="user-role">Vai trò</label><Select v-model="form.roleId" input-id="user-role" :options="roles" option-label="name" option-value="id" :disabled="busy" placeholder="Chọn vai trò" /><small v-if="roles.find(r => r.id === form.roleId)">{{ roleDescriptions[roles.find(r => r.id === form.roleId)!.baseRole] }}</small></div>
      <Message v-if="selected" severity="info" :closable="false">Sau khi đổi vai trò, người dùng cần đăng nhập lại để sử dụng quyền mới.</Message>
      <Message v-if="saveError" severity="error" :closable="false">{{ saveError }}</Message>
      <div class="dialog-actions"><Button label="Hủy" type="button" severity="secondary" :disabled="busy" @click="visible = false" /><Button :label="selected ? 'Lưu phân quyền' : 'Lưu người dùng'" type="submit" :loading="busy" :disabled="!form.roleId" /></div>
    </fieldset></form>
  </Dialog>
</template>
