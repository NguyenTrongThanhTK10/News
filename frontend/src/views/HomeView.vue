<script setup lang="ts">
// Luồng trang chủ: đọc bộ lọc từ URL → tải API → tính các nhóm tin → render template.
// Component dùng chung được import bên dưới để ghép giao diện từ các phần nhỏ.
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { RouterLink, useRoute, useRouter } from 'vue-router';
import Button from 'primevue/button';
import { getArticles } from '../services/news';
import { useCategoriesStore } from '../stores/categories';
import { useBookmarksStore } from '../stores/bookmarks';
import type { ArticleSummary, PagedResult } from '../types/news';
import { categoryIcon, formatDate } from '../utils/format';
import ArticleCard from '../components/ArticleCard.vue';
import ArticleSkeleton from '../components/ArticleSkeleton.vue';
import BookmarkButton from '../components/BookmarkButton.vue';
import NewsImage from '../components/NewsImage.vue';
import StatePanel from '../components/StatePanel.vue';

// route dùng để đọc URL, router dùng để đổi URL; store cung cấp chuyên mục và bài đã lưu.
const route = useRoute();
const router = useRouter();
const categories = useCategoriesStore();
const bookmarks = useBookmarksStore();
// State cục bộ chỉ thuộc trang này. Union 'list' | 'grid' giới hạn hai chế độ hợp lệ.
// ref phải đọc/ghi bằng .value trong script, nhưng template tự mở ref nên không cần .value.
const viewMode = ref<'list' | 'grid'>('list');
const loading = ref(true);
const error = ref('');
const result = ref<PagedResult<ArticleSummary> | null>(null);
// Query string có thể thiếu hoặc có nhiều giá trị, nên phải kiểm tra kiểu trước khi dùng.
// computed tự tính lại khi phụ thuộc đổi; không cần tạo một bản sao bộ lọc bằng ref.
const search = computed(() => typeof route.query.search === 'string' ? route.query.search.trim().slice(0, 200) : '');
const category = computed(() => typeof route.query.category === 'string' ? route.query.category.slice(0, 80) : '');
// URL do người dùng sửa được: số trang phải là số nguyên dương trong miền int của backend.
// Giá trị sai, thiếu hoặc quá lớn được đưa về trang 1 để API nhận đầu vào hợp lệ.
const page = computed(() => {
  const value = Number(route.query.page);
  return Number.isSafeInteger(value) && value > 0 && value <= 2147483647 ? value : 1;
});
// find tra tên chuyên mục theo slug; ?. và || cung cấp tên dự phòng khi chưa tải xong.
const categoryName = computed(() => categories.items.find(item => item.slug === category.value)?.name || 'Chuyên mục');
const isFrontPage = computed(() => !search.value && !category.value && page.value === 1);
// Trang đầu không lọc chia 9 bài thành: 1 tiêu điểm + 2 đáng chú ý + 6 bài trong dòng tin.
// Khi tìm kiếm/lọc/chuyển trang, cả 9 bài vào danh sách; slice tạo mảng mới, không sửa response.
const featured = computed(() => isFrontPage.value ? result.value?.items[0] : undefined);
const highlights = computed(() => isFrontPage.value ? result.value?.items.slice(1, 3) || [] : []);
const articles = computed(() => result.value?.items.slice(featured.value ? 3 : 0) || []);
// Hiển thị tối đa 5 nút số trang quanh trang hiện tại, kẹp vào khoảng [1, totalPages].
// Array.from tạo dãy số; ví dụ start = 3 và length = 5 sẽ cho [3, 4, 5, 6, 7].
const pagination = computed(() => {
  const total = result.value?.totalPages || 0;
  const start = Math.max(1, Math.min(page.value - 2, total - 4));
  return Array.from({ length: Math.min(5, total) }, (_, index) => start + index);
});
// Controller không dùng để render nên là biến thường; nó cho phép hủy lần tải trước.
let activeController: AbortController | undefined;

async function load() {
  // Người dùng có thể đổi bộ lọc nhanh hơn tốc độ API: hủy request cũ trước khi gửi request mới.
  // Mỗi lần tải giữ controller riêng để response cũ không ghi đè kết quả và loading của lần mới.
  activeController?.abort();
  const controller = new AbortController();
  activeController = controller;
  loading.value = true;
  error.value = '';
  try {
    // undefined khiến Axios bỏ qua bộ lọc trống. await chờ dữ liệu mà không khóa giao diện.
    const data = await getArticles({ search: search.value || undefined, category: category.value || undefined, page: page.value, pageSize: 9 }, controller.signal);
    if (!controller.signal.aborted) result.value = data;
  } catch {
    // Hủy request là chủ động, không phải lỗi kết nối cần báo cho người dùng.
    if (!controller.signal.aborted) error.value = 'Không thể tải tin tức lúc này. Kiểm tra kết nối rồi thử lại nhé.';
  } finally {
    // finally chạy dù thành công hay lỗi; chỉ request còn hiệu lực mới được kết thúc loading.
    if (!controller.signal.aborted) loading.value = false;
  }
}

// URL là nguồn trạng thái duy nhất: chia sẻ đường dẫn và Back/Forward đều giữ bộ lọc.
// Theo dõi một mảng nguồn: bất kỳ search/category/page đổi đều tải lại; immediate tải lần đầu.
watch([search, category, page], () => { void load(); }, { immediate: true });
// Dọn request khi rời trang để không tiếp tục cập nhật một component đã bị tháo khỏi DOM.
onBeforeUnmount(() => activeController?.abort());

// Giữ query hiện tại bằng ...route.query, chỉ thay page; trang 1 bỏ page để URL ngắn hơn.
// hash đưa người đọc về vùng tin mới. Hàm không gọi load trực tiếp vì watch ở trên sẽ thực hiện.
function selectPage(value: number) {
  void router.push({ path: '/', query: { ...route.query, page: value === 1 ? undefined : String(value) }, hash: '#tin-moi' });
}
</script>

<template>
  <div class="container page home-page">
    <section class="intro" aria-labelledby="home-title">
      <div>
        <div class="eyebrow"><span class="live-dot"></span> MỘT NGÀY MỚI. MỘT GÓC NHÌN MỚI.</div>
        <h1 id="home-title">Nhịp sống <em>hôm nay.</em></h1>
      </div>
      <p class="intro-description">Chuyện đang diễn ra.<br>Điều đáng nghĩ. <strong>Tất cả ở đây.</strong></p>
    </section>

    <!-- Bộ lọc dùng RouterLink để ghi lựa chọn vào URL. Chọn chuyên mục bỏ page cũ,
         giữ search nếu có. :class chỉ tô trạng thái; dữ liệu thật vẫn lấy từ route.query. -->
    <div class="category-filter" aria-label="Lọc theo chuyên mục">
      <span class="filter-caption">Dành cho bạn</span>
      <RouterLink :to="{ path: '/', query: search ? { search } : {} }" class="category-chip" :class="{ active: !category }" :aria-current="!category ? 'page' : undefined">Tất cả</RouterLink>
      <RouterLink v-for="item in categories.items" :key="item.slug" :to="{ path: '/', query: { category: item.slug, ...(search ? { search } : {}) } }" class="category-chip" :class="{ active: category === item.slug }" :aria-current="category === item.slug ? 'page' : undefined">{{ item.name }}</RouterLink>
      <button v-if="categories.error" class="text-button" type="button" @click="categories.load()">Tải lại chuyên mục</button>
    </div>

    <div v-if="search || category" class="filter-summary">
      <span v-if="search">Kết quả cho <strong>“{{ search }}”</strong></span>
      <span v-if="category" class="filter-label">{{ categoryName }}</span>
      <RouterLink to="/" class="text-button">Xóa bộ lọc <i class="pi pi-times" aria-hidden="true"></i></RouterLink>
    </div>

    <!-- Chuỗi v-if / v-else-if chọn trạng thái tải → lỗi → có kết quả.
         Comment không tạo phần tử DOM, nên các nhánh vẫn nối tiếp nhau. -->
    <ArticleSkeleton v-if="loading" />
    <StatePanel v-else-if="error" title="Kết nối đang gián đoạn" :description="error" error>
      <Button label="Thử lại" icon="pi pi-refresh" class="button-primary" @click="load" />
    </StatePanel>
    <template v-else-if="result">
      <!-- Chỉ trang đầu không lọc có tin tiêu điểm. :src truyền URL ảnh từ dữ liệu,
           còn eager là prop boolean yêu cầu ưu tiên tải ảnh nằm đầu trang. -->
      <section v-if="featured" class="featured-grid" aria-label="Tin nổi bật">
        <article class="lead-story">
          <div class="lead-content">
            <span class="feature-label"><i class="pi pi-sparkles" aria-hidden="true"></i> CÂU CHUYỆN TIÊU ĐIỂM</span>
            <div class="lead-kicker">
              <RouterLink :to="{ path: '/', query: { category: featured.categorySlug } }" class="category">{{ featured.category }}</RouterLink>
              <span class="article-meta">{{ featured.readingMinutes }} phút đọc</span>
            </div>
            <h2><RouterLink :to="`/bai-viet/${featured.id}`">{{ featured.title }}</RouterLink></h2>
            <p>{{ featured.summary }}</p>
            <div class="lead-bottom">
              <div class="lead-author">
                <span class="author-avatar" aria-hidden="true">{{ featured.author.charAt(0) }}</span>
                <span><strong>{{ featured.author }}</strong><time :datetime="featured.publishedAt">{{ formatDate(featured.publishedAt) }}</time></span>
              </div>
              <RouterLink :to="`/bai-viet/${featured.id}`" class="round-arrow" aria-label="Đọc câu chuyện nổi bật"><i class="pi pi-arrow-up-right" aria-hidden="true"></i></RouterLink>
            </div>
          </div>
          <RouterLink :to="`/bai-viet/${featured.id}`" class="lead-image" tabindex="-1" aria-hidden="true">
            <NewsImage :src="featured.imageUrl" alt="" eager />
            <span class="image-caption">MỞ THÊM MỘT GÓC NHÌN <i class="pi pi-arrow-up-right" aria-hidden="true"></i></span>
          </RouterLink>
        </article>
        <div class="highlights">
          <div class="highlights-heading"><span>Đáng chú ý</span><span class="eyebrow">BIÊN TẬP CHỌN</span></div>
          <!-- v-for cung cấp cả phần tử và vị trí; key dùng ID ổn định thay vì vị trí mảng. -->
          <article v-for="(article, index) in highlights" :key="article.id" class="highlight-card">
            <span class="highlight-number" aria-hidden="true">0{{ index + 1 }}</span>
            <div class="highlight-content">
              <RouterLink :to="{ path: '/', query: { category: article.categorySlug } }" class="category">{{ article.category }}</RouterLink>
              <h3><RouterLink :to="`/bai-viet/${article.id}`">{{ article.title }}</RouterLink></h3>
              <div class="card-bottom"><span class="article-meta">{{ article.readingMinutes }} phút đọc</span><BookmarkButton :article="article" /></div>
            </div>
            <RouterLink :to="`/bai-viet/${article.id}`" class="highlight-image" tabindex="-1" aria-hidden="true"><NewsImage :src="article.imageUrl" alt="" eager /></RouterLink>
          </article>
        </div>
      </section>

      <div class="news-workspace">
        <section id="tin-moi" class="latest-section" aria-labelledby="latest-title">
          <div class="section-title">
            <div><span class="eyebrow">KHÔNG BỎ LỠ ĐIỀU HAY</span><h2 id="latest-title">{{ search ? 'Kết quả tìm kiếm' : category ? categoryName : 'Dòng tin mới nhất' }}<span class="title-dot">.</span></h2></div>
            <!-- @click đổi ref; :class và prop layout bên dưới tự cập nhật theo viewMode.
                 aria-pressed cho biết nút đang được chọn khi dùng trình đọc màn hình. -->
            <div class="view-switch" role="group" aria-label="Cách hiển thị tin tức">
              <button type="button" :aria-pressed="viewMode === 'list'" aria-label="Hiển thị danh sách" @click="viewMode = 'list'"><i class="pi pi-list" aria-hidden="true"></i></button>
              <button type="button" :aria-pressed="viewMode === 'grid'" aria-label="Hiển thị dạng lưới" @click="viewMode = 'grid'"><i class="pi pi-th-large" aria-hidden="true"></i></button>
            </div>
          </div>
          <div class="feed-meta" aria-live="polite"><span><span class="live-dot"></span>{{ result.totalCount }} câu chuyện để khám phá</span><span v-if="result.totalPages > 1">Trang {{ page }} / {{ result.totalPages }}</span></div>
          <!-- Cha truyền từng bài qua prop :article; ArticleCard lo hiển thị và nút lưu.
               Mảng rỗng vẫn là response thành công nên có thông báo riêng ở nhánh v-else. -->
          <div v-if="articles.length" class="cards feed-cards" :class="{ 'is-list': viewMode === 'list' }">
            <ArticleCard v-for="article in articles" :key="article.id" :article="article" :layout="viewMode" />
          </div>
          <StatePanel v-else :title="result.totalCount ? 'Trang này chưa có bài viết' : 'Chưa tìm thấy câu chuyện phù hợp'" :description="result.totalCount ? 'Hãy quay lại trang đầu để tiếp tục đọc.' : 'Thử từ khóa khác hoặc khám phá tất cả chuyên mục.'">
            <RouterLink :to="result.totalCount ? { path: '/', query: { ...route.query, page: undefined } } : '/'" class="button button-primary">{{ result.totalCount ? 'Về trang đầu' : 'Xem tất cả tin tức' }}<i class="pi pi-arrow-right" aria-hidden="true"></i></RouterLink>
          </StatePanel>
          <!-- Chỉ hiện phân trang khi cần; disabled chặn đi ra ngoài khoảng trang hợp lệ. -->
          <nav v-if="result.totalPages > 1" class="pagination" aria-label="Phân trang tin tức">
            <button type="button" :disabled="page <= 1" aria-label="Trang trước" @click="selectPage(page - 1)"><i class="pi pi-angle-left" aria-hidden="true"></i></button>
            <button v-for="number in pagination" :key="number" type="button" :class="{ current: page === number }" :aria-label="`Trang ${number}`" :aria-current="page === number ? 'page' : undefined" @click="selectPage(number)">{{ number }}</button>
            <button type="button" :disabled="page >= result.totalPages" aria-label="Trang sau" @click="selectPage(page + 1)"><i class="pi pi-angle-right" aria-hidden="true"></i></button>
          </nav>
        </section>

        <!-- Sidebar dùng cùng categories/bookmarks store với header nên dữ liệu luôn đồng bộ. -->
        <aside class="discovery-sidebar" aria-label="Khám phá tin tức">
          <section class="topics-panel" aria-labelledby="topics-title">
            <span class="eyebrow">ĐI THEO SỰ TÒ MÒ</span>
            <h2 id="topics-title">Thế giới bạn quan tâm</h2>
            <p>Mỗi chủ đề, một cánh cửa mới.</p>
            <div class="topic-links">
              <RouterLink v-for="item in categories.items" :key="item.slug" :to="{ path: '/', query: { category: item.slug } }" class="topic-link" :data-topic="item.slug">
                <span class="topic-icon"><i :class="['pi', categoryIcon(item.slug)]" aria-hidden="true"></i></span>
                <span>{{ item.name }}<small>{{ item.count }} bài viết</small></span>
                <i class="pi pi-arrow-up-right" aria-hidden="true"></i>
              </RouterLink>
            </div>
            <button v-if="categories.error" type="button" class="text-button" @click="categories.load()">Tải lại chuyên mục <i class="pi pi-refresh" aria-hidden="true"></i></button>
          </section>
          <section class="reading-banner" aria-labelledby="reading-title">
            <span class="reading-symbol" aria-hidden="true"><i class="pi pi-bookmark"></i><span>+</span></span>
            <span class="eyebrow">GÓC ĐỌC CỦA RIÊNG BẠN</span>
            <h2 id="reading-title">Đọc điều hay.<br>Vào lúc bạn muốn.</h2>
            <p>Lưu lại những câu chuyện chạm đến bạn. Một khoảng đọc riêng, luôn ở đây.</p>
            <RouterLink to="/da-luu" class="button">{{ bookmarks.count ? `${bookmarks.count} bài đang chờ bạn` : 'Khám phá bài đã lưu' }} <i class="pi pi-arrow-up-right" aria-hidden="true"></i></RouterLink>
          </section>
          <RouterLink to="/hoc-vue" class="learning-callout"><span><i class="pi pi-code" aria-hidden="true"></i> TỪ ĐỌC ĐẾN TỰ TAY XÂY DỰNG</span><strong>Khám phá phía sau<br>một trang tin.</strong><span>Lộ trình học Vue <i class="pi pi-arrow-up-right" aria-hidden="true"></i></span></RouterLink>
        </aside>
      </div>
    </template>
    <p class="demo-note"><i class="pi pi-info-circle" aria-hidden="true"></i> Nội dung biên tập minh họa cho dự án, không phải bản tin thực tế.</p>
  </div>
</template>
