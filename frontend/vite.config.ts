import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';

// Vite chạy dev server và đóng gói frontend; plugin-vue xử lý các tệp Single File Component .vue.
export default defineConfig({
  plugins: [vue()],
  server: {
    host: '127.0.0.1',
    port: 5173,
    // Giữ đúng cổng 5173; báo lỗi nếu đã bị dùng thay vì tự nhảy sang cổng khác.
    strictPort: true,
    // Trong lúc phát triển, /api được chuyển tới ASP.NET Core ở cổng 5080.
    // Trình duyệt chỉ gọi địa chỉ frontend; khi triển khai cần cấu hình API/proxy tương ứng riêng.
    proxy: { '/api': process.env.VITE_PROXY_TARGET || 'http://localhost:5080' },
  },
});
