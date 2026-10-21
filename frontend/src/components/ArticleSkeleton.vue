<template>
  <!-- Skeleton là khung giữ chỗ trong lúc chờ API, không phải dữ liệu bài viết thật.
       v-for với số 6 tạo sáu thẻ; hiệu ứng shimmer được định nghĩa trong style.css.
       Ẩn các hình trang trí với trình đọc màn hình, chỉ đọc thông báo "Đang tải tin tức". -->
  <div class="cards skeleton-cards" role="status" aria-label="Đang tải tin tức">
    <div v-for="item in 6" :key="item" class="skeleton-card" aria-hidden="true">
      <div class="skeleton skeleton-image"></div>
      <div class="skeleton-content"><div class="skeleton skeleton-tag"></div><div class="skeleton skeleton-line"></div><div class="skeleton skeleton-line"></div><div class="skeleton skeleton-line short"></div></div>
    </div>
    <span class="sr-only">Đang tải tin tức…</span>
  </div>
</template>
