// Điểm khởi động: index.html nạp tệp này, sau đó Vue dựng App.vue vào phần tử #app.
// Component là một phần giao diện có thể tái sử dụng; App là component gốc của ứng dụng.
import { createApp } from 'vue';
import PrimeVue from 'primevue/config';
import Aura from '@primeuix/themes/aura';
import { definePreset } from '@primeuix/themes';
import ToastService from 'primevue/toastservice';
import { createPinia } from 'pinia';
import router from './router';
import App from './App.vue';
// CSS import ở đây áp dụng toàn ứng dụng. PrimeIcons cung cấp các lớp icon như pi-arrow-up.
// style.css định nghĩa giao diện chung; feedback.css bổ sung giao diện bình luận/đánh giá.
import 'primeicons/primeicons.css';
import './style.css';
import './feedback.css';

// Kế thừa giao diện Aura của PrimeVue và thay bảng màu để đồng bộ với thương hiệu TIN24.
// Các mức 50–950 là các sắc độ từ nhạt đến đậm, dùng cho nền, viền và trạng thái tương tác.
const Tin24 = definePreset(Aura, {
  semantic: {
    primary: {
      50: '#f0f3ff', 100: '#e2e8ff', 200: '#c6d2ff', 300: '#a0b3ff', 400: '#748ff8',
      500: '#4e70f0', 600: '#3459e6', 700: '#2646c3', 800: '#253c9d', 900: '#25377c', 950: '#17233c',
      color: '#3459e6', contrastColor: '#ffffff', hoverColor: '#2646c3', activeColor: '#253c9d',
    },
    formField: { borderColor: '#e0e5f0', hoverBorderColor: '#9baee8', color: '#344365' },
  },
});

// .use() đăng ký plugin trước khi hiển thị ứng dụng:
// - Pinia giữ dữ liệu dùng chung (phiên đăng nhập, chuyên mục, bài đã lưu).
// - Router chọn trang theo URL; Pinia phải được cài trước vì route guard dùng auth store.
// - PrimeVue cung cấp component giao diện; ToastService cho phép gọi toast.add() từ các trang.
// .mount('#app') bắt đầu hiển thị; darkModeSelector: false giữ theme PrimeVue ở chế độ sáng.
createApp(App).use(createPinia()).use(router).use(PrimeVue, {
  theme: { preset: Tin24, options: { darkModeSelector: false } },
}).use(ToastService).mount('#app');
