<script setup lang="ts">
import { computed, ref } from 'vue';
import { RouterLink } from 'vue-router';
import Button from 'primevue/button';
import Menu from 'primevue/menu';
import { useAuthStore } from '../stores/auth';
import { actionLabels, allowedActions, canDelete } from '../utils/workflow';
import type { AdminArticle, ArticleAction } from '../types/admin';

const props = defineProps<{ article: AdminArticle }>();
const emit = defineEmits<{ choose: [operation: ArticleAction | 'delete'] }>();
const auth = useAuthStore();
const menu = ref<InstanceType<typeof Menu>>();
const menuOpen = ref(false);
const menuId = computed(() => `article-actions-${props.article.article.id}`);
const actions = computed(() => allowedActions(props.article, auth.user));
const primaryAction = computed(() => actions.value[0]);
const icons: Record<ArticleAction, string> = {
  submit: 'pi pi-send', approve: 'pi pi-check-circle', reject: 'pi pi-undo',
  publish: 'pi pi-globe', unpublish: 'pi pi-eye-slash', archive: 'pi pi-folder', restore: 'pi pi-replay',
};
const menuItems = computed(() => {
  const secondary = actions.value.slice(1).map(operation => ({
    label: actionLabels[operation], icon: icons[operation],
    command: () => emit('choose', operation),
  }));
  return [
    ...secondary,
    ...(canDelete(props.article, auth.user) ? [
      ...(secondary.length ? [{ separator: true }] : []),
      { label: 'Xóa bài viết', icon: 'pi pi-trash', danger: true, command: () => emit('choose', 'delete') },
    ] : []),
  ];
});
</script>

<template>
  <div class="article-actions" role="group" :aria-label="`Thao tác bài: ${article.article.title}`">
    <RouterLink :to="`/quan-tri/bai-viet/${article.article.id}/sua`" class="article-edit-button"
      :aria-label="`Mở bài: ${article.article.title}`" title="Mở bài viết">
      <i class="pi pi-pen-to-square" aria-hidden="true"></i>
    </RouterLink>
    <Button v-if="primaryAction" :label="actionLabels[primaryAction]" :icon="icons[primaryAction]"
      class="article-workflow-button" :class="`action-${primaryAction}`" outlined size="small"
      @click="emit('choose', primaryAction)" />
    <Button v-if="menuItems.length" icon="pi pi-ellipsis-h" class="article-more-button" outlined
      :aria-label="`Thao tác khác: ${article.article.title}`" title="Thao tác khác" aria-haspopup="menu"
      :aria-controls="menuId" :aria-expanded="menuOpen" @click="menu?.toggle($event)" />
    <Menu v-if="menuItems.length" :id="menuId" ref="menu" :model="menuItems" popup class="article-action-menu"
      :aria-label="`Thao tác khác: ${article.article.title}`" @show="menuOpen = true" @hide="menuOpen = false">
      <template #item="{ item, props: itemProps }">
        <a v-bind="itemProps.action" class="article-action-menu-item" :class="{ 'is-danger': item.danger }">
          <i :class="item.icon" aria-hidden="true"></i><span>{{ item.label }}</span>
        </a>
      </template>
    </Menu>
  </div>
</template>
