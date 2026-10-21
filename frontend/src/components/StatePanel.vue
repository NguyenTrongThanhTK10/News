<script setup lang="ts">
defineProps<{ title: string; description?: string; icon?: string; error?: boolean }>();
</script>

<template>
  <div class="state-panel" :role="error ? 'alert' : 'status'">
    <span class="state-icon"><i :class="['pi', icon || (error ? 'pi-wifi' : 'pi-search')]" aria-hidden="true"></i></span>
    <h2>{{ title }}</h2>
    <p v-if="description">{{ description }}</p>
    <slot />
  </div>
</template>
