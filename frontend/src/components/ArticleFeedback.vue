<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue';
import { isAxiosError } from 'axios';
import ReaderAuthDialog from './ReaderAuthDialog.vue';
import { useAuthStore } from '../stores/auth';
import type { ArticleComment, PagedResult, RatingStatistics } from '../types/news';
import { getComments, getRatings, postComment, rateArticle } from '../services/feedback';
import { errorMessage } from '../services/http';

// Component nhận ID bài từ ArticleView. Cha dùng :key="article.id" để tạo instance mới khi đổi bài.
// Mỗi instance giữ form, danh sách và các request của đúng một bài viết.
const props = defineProps<{ articleId: number }>();
const auth = useAuthStore();
const authVisible = ref(false);
const authMode = ref<'register' | 'login'>('register');
const logoutBusy = ref(false);
async function openAuth(mode: 'register' | 'login') {
  await auth.initialize();
  if (lifetime.signal.aborted || auth.user) return;
  authMode.value = mode;
  authVisible.value = true;
}
async function logout() {
  if (logoutBusy.value) return;
  logoutBusy.value = true;
  try { await auth.logout(); }
  catch (cause) { formError.value = errorMessage(cause); }
  finally { logoutBusy.value = false; }
}
// lifetime hủy mọi request gắn với vòng đời component; commentsController hủy riêng lần tải
// bình luận trước khi phân trang, không ảnh hưởng request đánh giá hoặc gửi form.
const lifetime = new AbortController();
let commentsController: AbortController | undefined;
// Mỗi vùng có loading/error riêng: lỗi đánh giá không làm mất danh sách bình luận và ngược lại.
const comments = ref<PagedResult<ArticleComment> | null>(null);
const commentsLoading = ref(true);
const commentsError = ref('');
const ratings = ref<RatingStatistics | null>(null);
const ratingsLoading = ref(true);
const ratingsError = ref('');
// selectedStars là lựa chọn đang nhập, chưa chắc đã lưu; ratings.myRating là bản server đã lưu.
// Các cờ Busy vô hiệu hóa nút khi đang gửi để tránh thao tác lặp.
const selectedStars = ref(0);
const ratingBusy = ref(false);
const ratingMessage = ref('');
// v-model trong template nối các ref này với ô nhập; thông báo form tách khỏi lỗi tải dữ liệu.
const content = ref('');
const commentBusy = ref(false);
const formError = ref('');
const commentMessage = ref('');
// Mảng bắt đầu từ 0: nhãn của mức sao n nằm ở starLabels[n - 1].
const starLabels = ['Chưa hữu ích', 'Cần cải thiện', 'Khá hữu ích', 'Hữu ích', 'Rất hữu ích'];
const timestamp = new Intl.DateTimeFormat('vi-VN', {
  day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', timeZone: 'Asia/Ho_Chi_Minh',
});

// Mỗi lần chọn trang tạo controller mới, hủy lần cũ và chỉ nhận response chưa bị hủy.
// Mẫu try/catch/finally: lấy dữ liệu → báo lỗi khi cần → luôn kết thúc trạng thái tải còn hiệu lực.
async function loadComments(page = 1) {
  commentsController?.abort();
  const controller = new AbortController();
  commentsController = controller;
  commentsLoading.value = true;
  commentsError.value = '';
  try {
    const data = await getComments(props.articleId, page, controller.signal);
    if (!controller.signal.aborted) comments.value = data;
  } catch (cause) {
    if (!controller.signal.aborted) commentsError.value = errorMessage(cause);
  } finally {
    if (!controller.signal.aborted) commentsLoading.value = false;
  }
}

// Tải thống kê và điền lại đánh giá của trình duyệt; ?? chỉ thay null/undefined bằng 0.
async function loadRatings() {
  ratingsLoading.value = true;
  ratingsError.value = '';
  try {
    const data = await getRatings(props.articleId, lifetime.signal);
    if (!lifetime.signal.aborted) {
      ratings.value = data;
      selectedStars.value = data.myRating ?? 0;
    }
  } catch (cause) {
    if (!lifetime.signal.aborted) ratingsError.value = errorMessage(cause);
  } finally {
    if (!lifetime.signal.aborted) ratingsLoading.value = false;
  }
}

// Chưa chọn sao hoặc đang gửi thì dừng sớm. Dùng thống kê mới API trả về sau khi lưu thành công,
// không tự cộng lượt vì gửi lại từ cùng trình duyệt là cập nhật đánh giá hiện có.
async function submitRating() {
  if (ratingBusy.value || !selectedStars.value) return;
  ratingBusy.value = true;
  ratingMessage.value = '';
  ratingsError.value = '';
  try {
    const data = await rateArticle(props.articleId, selectedStars.value, lifetime.signal);
    if (lifetime.signal.aborted) return;
    ratings.value = data;
    ratingMessage.value = 'Đã lưu đánh giá. Cảm ơn góc nhìn của bạn!';
  } catch (cause) {
    if (!lifetime.signal.aborted) ratingsError.value = errorMessage(cause);
  } finally {
    if (!lifetime.signal.aborted) ratingBusy.value = false;
  }
}

// Validation phía client giúp phản hồi sớm; backend vẫn kiểm tra nội dung và giới hạn độ dài.
// HTML required/minlength/maxlength trong template bổ sung kiểm tra khi người dùng gửi form.
async function submitComment() {
  if (commentBusy.value || commentsLoading.value) return;
  if (!auth.user) { await openAuth('register'); return; }
  formError.value = '';
  commentMessage.value = '';
  const text = content.value.trim();
  if (text.length < 5) {
    formError.value = 'Vui lòng nhập bình luận từ 5 ký tự, không tính khoảng trắng ở đầu và cuối.';
    return;
  }
  commentBusy.value = true;
  try {
    const comment = await postComment(props.articleId, text, lifetime.signal);
    if (lifetime.signal.aborted) return;
    // Chỉ sau khi POST thành công mới thêm bình luận server trả về vào đầu trang 1.
    // Nếu trước đó ở trang khác thì không ghép các mục của trang đó vào trang đầu.
    const previous = comments.value;
    comments.value = {
      items: [comment, ...(previous?.page === 1 ? previous.items : [])].slice(0, 10),
      totalCount: (previous?.totalCount ?? 0) + 1,
      page: 1, pageSize: 10, totalPages: Math.ceil(((previous?.totalCount ?? 0) + 1) / 10),
    };
    content.value = '';
    commentMessage.value = 'Bình luận của bạn đã được đăng.';
    // Sau cập nhật tức thời, đọc lại trang 1 để đồng bộ thứ tự và tổng số với dữ liệu server.
    await loadComments(1);
  } catch (cause) {
    if (!lifetime.signal.aborted) {
      formError.value = errorMessage(cause);
      if (isAxiosError(cause) && cause.response?.status === 401 && !auth.user) await openAuth('login');
    }
  } finally {
    if (!lifetime.signal.aborted) commentBusy.value = false;
  }
}

// Khởi chạy hai lần tải độc lập để chúng không phải chờ nhau.
// void cho biết không await tại hook này; từng hàm đã tự xử lý lỗi của mình.
onMounted(() => {
  void auth.initialize();
  void loadComments();
  void loadRatings();
});
// Rời bài viết thì hủy cả request theo vòng đời và lần phân trang bình luận đang chạy.
onBeforeUnmount(() => {
  lifetime.abort();
  commentsController?.abort();
});
</script>

<template>
  <section class="article-feedback" aria-labelledby="feedback-title">
    <div class="feedback-heading"><span class="eyebrow">GÓC NHÌN CỦA BẠN</span><h2 id="feedback-title">Câu chuyện tiếp tục từ đây<span class="title-dot">.</span></h2><p>Một đánh giá, một chia sẻ. Để những câu chuyện ngày càng tốt hơn.</p></div>

    <!-- aria-busy cho biết vùng đang cập nhật; meter biểu diễn số lượt mỗi mức sao trên tổng lượt. -->
    <section class="rating-panel" aria-labelledby="rating-title" :aria-busy="ratingsLoading">
      <div class="rating-overview">
        <h3 id="rating-title">Đánh giá bài viết</h3>
        <p v-if="ratingsLoading" class="feedback-hint" role="status">Đang tải đánh giá…</p>
        <template v-else-if="ratings">
          <div class="rating-score"><strong>{{ ratings.count ? ratings.average.toLocaleString('vi-VN', { minimumFractionDigits: 1 }) : '—' }}</strong><span>/ 5</span></div>
          <div class="rating-stars-static" :aria-label="`${ratings.average} trên 5 sao`"><i v-for="star in 5" :key="star" :class="['pi', star <= Math.round(ratings.average) ? 'pi-star-fill' : 'pi-star']" aria-hidden="true"></i></div>
          <p class="feedback-hint">{{ ratings.count ? `${ratings.count} lượt đánh giá` : 'Hãy là người đầu tiên đánh giá' }}</p>
          <div v-if="ratings.count" class="rating-distribution" aria-label="Phân bố đánh giá">
            <div v-for="star in [5, 4, 3, 2, 1]" :key="star"><span>{{ star }} <i class="pi pi-star-fill" aria-hidden="true"></i></span><meter :value="ratings.distribution[star - 1] ?? 0" :max="ratings.count" :aria-label="`${star} sao: ${ratings.distribution[star - 1] ?? 0} lượt`"></meter><span>{{ ratings.distribution[star - 1] ?? 0 }}</span></div>
          </div>
        </template>
      </div>
      <!-- Radio cùng name tạo nhóm chọn một mức sao, dùng được bằng bàn phím.
           :value="star" truyền số, v-model giữ lựa chọn; .prevent tránh tải lại trang khi submit. -->
      <form class="rating-form" @submit.prevent="submitRating">
        <fieldset :disabled="ratingBusy || ratingsLoading || !ratings"><legend>Bài viết có hữu ích với bạn?</legend><div class="star-picker"><label v-for="star in 5" :key="star" :class="{ filled: selectedStars >= star }"><input v-model="selectedStars" class="rating-radio" type="radio" :name="`article-rating-${articleId}`" :value="star" :aria-label="`${star} sao — ${starLabels[star - 1]}`" @change="ratingMessage = ''"><i :class="['pi', selectedStars >= star ? 'pi-star-fill' : 'pi-star']" aria-hidden="true"></i></label></div></fieldset>
        <p class="rating-selection" aria-live="polite">{{ selectedStars ? `${selectedStars}/5 · ${starLabels[selectedStars - 1]}` : 'Chọn số sao để chia sẻ cảm nhận' }}</p>
        <p class="feedback-hint">Mỗi trình duyệt có một đánh giá. Bạn có thể thay đổi bất cứ lúc nào.</p>
        <button type="submit" class="button button-primary" :disabled="ratingBusy || ratingsLoading || !ratings || !selectedStars || selectedStars === ratings.myRating"><i v-if="ratingBusy" class="pi pi-spin pi-spinner" aria-hidden="true"></i>{{ ratingBusy ? 'Đang lưu…' : ratings?.myRating ? 'Cập nhật đánh giá' : 'Gửi đánh giá' }}</button>
        <p v-if="ratingMessage" class="feedback-success" role="status">{{ ratingMessage }}</p>
        <div v-if="ratingsError" class="feedback-error" role="alert"><p>{{ ratingsError }}</p><button v-if="!ratings" type="button" class="text-button" @click="loadRatings">Thử tải lại đánh giá <i class="pi pi-refresh" aria-hidden="true"></i></button></div>
      </form>
    </section>

    <section class="comments-section" aria-labelledby="comments-title">
      <div class="comments-heading"><h3 id="comments-title">Bình luận <span v-if="comments" class="section-count">{{ comments.totalCount }}</span></h3><span class="feedback-hint">Mới nhất trước</span></div>
      <!-- ID input chứa articleId để label trỏ đúng ô; disabled ngăn sửa form lúc đang gửi. -->
      <form class="comment-form" @submit.prevent="submitComment">
        <div v-if="auth.user" class="comment-account"><span><i class="pi pi-user" aria-hidden="true"></i> Bình luận với tên <strong>{{ auth.user.name }}</strong></span><button type="button" class="text-button" :disabled="commentBusy || logoutBusy" @click="logout">{{ logoutBusy ? 'Đang đăng xuất…' : 'Đăng xuất' }}</button></div>
        <div v-else class="comment-auth-prompt"><p>Đăng ký tài khoản để chia sẻ góc nhìn của bạn.</p><div><button type="button" class="text-button" @click="openAuth('register')">Đăng ký để bình luận</button><span>Đã có tài khoản?</span><button type="button" class="text-button" @click="openAuth('login')">Đăng nhập</button></div></div>
        <div class="form-field"><label :for="`comment-content-${articleId}`">Chia sẻ góc nhìn của bạn <span class="required">*</span></label><textarea :id="`comment-content-${articleId}`" v-model="content" class="feedback-input" rows="4" placeholder="Điều gì trong câu chuyện khiến bạn suy nghĩ?" required minlength="5" maxlength="2000" :disabled="commentBusy" :aria-describedby="`comment-hint-${articleId}`"></textarea><div class="comment-field-meta"><small :id="`comment-hint-${articleId}`">Hãy chia sẻ lịch sự và tôn trọng những góc nhìn khác.</small><span>{{ content.length }}/2000</span></div></div>
        <div class="comment-form-bottom"><span class="feedback-hint"><i class="pi pi-globe" aria-hidden="true"></i> Tên tài khoản và bình luận sẽ hiển thị công khai.</span><button :type="auth.user ? 'submit' : 'button'" class="button button-primary" :disabled="commentBusy || commentsLoading" @click="!auth.user && openAuth('register')"><i :class="['pi', commentBusy ? 'pi-spin pi-spinner' : 'pi-send']" aria-hidden="true"></i>{{ commentBusy ? 'Đang gửi…' : 'Gửi bình luận' }}</button></div>
        <p v-if="formError" class="feedback-error" role="alert">{{ formError }}</p>
        <p v-if="commentMessage" class="feedback-success" role="status">{{ commentMessage }}</p>
      </form>

      <div v-if="commentsError" class="feedback-error" role="alert"><p>{{ commentsError }}</p><button type="button" class="text-button" :disabled="commentsLoading" @click="loadComments(comments?.page ?? 1)">Thử tải lại bình luận <i class="pi pi-refresh" aria-hidden="true"></i></button></div>
      <p v-if="commentsLoading" class="feedback-loading" role="status"><i class="pi pi-spin pi-spinner" aria-hidden="true"></i> Đang tải bình luận…</p>
      <!-- {{ comment.content }} hiển thị văn bản, không thực thi HTML người đọc nhập.
           :key dùng ID do server cấp để Vue nhận diện từng bình luận khi danh sách thay đổi. -->
      <div v-if="comments?.items.length" class="comment-list" :aria-busy="commentsLoading">
        <article v-for="comment in comments.items" :key="comment.id" class="reader-comment"><span class="comment-avatar" aria-hidden="true">{{ comment.displayName.charAt(0).toLocaleUpperCase('vi-VN') }}</span><div class="comment-main"><header><strong>{{ comment.displayName }}</strong><time :datetime="comment.createdAt">{{ timestamp.format(new Date(comment.createdAt)) }}</time></header><p>{{ comment.content }}</p></div></article>
      </div>
      <div v-else-if="!commentsLoading && !commentsError" class="comments-empty"><i class="pi pi-comments" aria-hidden="true"></i><strong>Góc nhìn đầu tiên có thể là của bạn.</strong><p>Chưa có bình luận. Hãy bắt đầu cuộc trò chuyện.</p></div>
      <nav v-if="comments && comments.totalPages > 1" class="comment-pagination" aria-label="Phân trang bình luận"><button type="button" class="button button-outline" :disabled="commentsLoading || commentBusy || comments.page <= 1" @click="loadComments(comments.page - 1)"><i class="pi pi-angle-left" aria-hidden="true"></i> Bình luận trước</button><span>Trang {{ comments.page }} / {{ comments.totalPages }}</span><button type="button" class="button button-outline" :disabled="commentsLoading || commentBusy || comments.page >= comments.totalPages" @click="loadComments(comments.page + 1)">Bình luận sau <i class="pi pi-angle-right" aria-hidden="true"></i></button></nav>
    </section>
    <ReaderAuthDialog v-model:visible="authVisible" :initial-mode="authMode" />
  </section>
</template>
