<template>
  <div class="cards skeleton-cards" role="status" aria-label="Đang tải tin tức">
    <div v-for="item in 6" :key="item" class="skeleton-card" aria-hidden="true">
      <div class="skeleton skeleton-image"></div>
      <div class="skeleton-content"><div class="skeleton skeleton-tag"></div><div class="skeleton skeleton-line"></div><div class="skeleton skeleton-line"></div><div class="skeleton skeleton-line short"></div></div>
    </div>
    <span class="sr-only">Đang tải tin tức…</span>
  </div>
</template>
