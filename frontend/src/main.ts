import { createApp } from 'vue';
import PrimeVue from 'primevue/config';
import Aura from '@primeuix/themes/aura';
import { definePreset } from '@primeuix/themes';
import ToastService from 'primevue/toastservice';
import { createPinia } from 'pinia';
import router from './router';
import App from './App.vue';
import 'primeicons/primeicons.css';
import './style.css';

const Tin24 = definePreset(Aura, {
  semantic: {
    primary: {
      50: '#fff3ee', 100: '#ffe3d7', 200: '#ffc7b3', 300: '#ffa085', 400: '#ee7250',
      500: '#d94d31', 600: '#c9442d', 700: '#a93624', 800: '#8b2f21', 900: '#742b20', 950: '#3d130c',
      color: '#c9442d', contrastColor: '#ffffff', hoverColor: '#a93624', activeColor: '#8b2f21',
    },
    formField: { borderColor: '#dfe3d8', hoverBorderColor: '#b6c2a5', color: '#3b4535' },
  },
});

// Khởi tạo ứng dụng Vue, đăng ký thư viện giao diện và bộ định tuyến.
createApp(App).use(createPinia()).use(router).use(PrimeVue, {
  theme: { preset: Tin24, options: { darkModeSelector: false } },
}).use(ToastService).mount('#app');
