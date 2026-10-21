<script setup lang="ts">
defineProps<{
  items: { label: string; value: number; hint: string; icon: string; tone: 'blue' | 'teal' | 'amber' | 'violet' }[];
  loading?: boolean;
}>();
</script>

<template>
  <div class="management-overview" :aria-busy="loading">
    <div v-for="item in items" :key="item.label" class="overview-card" :class="`tone-${item.tone}`">
      <div class="overview-card-top"><span>{{ item.label }}</span><span class="overview-icon"><i :class="['pi', item.icon]" aria-hidden="true"></i></span></div>
      <strong class="overview-value">{{ loading ? '—' : item.value.toLocaleString('vi-VN') }}</strong><p>{{ item.hint }}</p>
    </div>
  </div>
</template>
