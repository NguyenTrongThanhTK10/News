import { expect, test, type Page } from '@playwright/test';

async function registerReader(page: Page, suffix: string) {
  await page.getByRole('button', { name: 'Đăng ký để bình luận', exact: true }).click();
  const dialog = page.getByRole('dialog');
  await expect(dialog).toHaveAccessibleName('Đăng ký để bình luận');
  await dialog.getByLabel('Họ tên', { exact: true }).fill('Minh An');
  await dialog.getByLabel('Email', { exact: true }).fill(`reader-${suffix}@example.com`);
  await dialog.getByLabel('Mật khẩu', { exact: true }).fill('PrivateReaderPassword!2026');
  await dialog.getByLabel('Xác nhận mật khẩu', { exact: true }).fill('PrivateReaderPassword!2026');
  await dialog.getByRole('button', { name: 'Đăng ký tài khoản', exact: true }).click();
  await expect(dialog).toHaveCount(0);
  await expect(page.locator('.comment-account')).toContainText('Minh An');
}

test('readers post comments and update a persistent browser rating', async ({ page }, testInfo) => {
  await page.goto('/bai-viet/11');
  const feedback = page.locator('.article-feedback');
  await expect(feedback.getByRole('heading', { name: 'Đánh giá bài viết' })).toBeVisible();
  await expect(feedback.locator('.feedback-loading')).toHaveCount(0);
  const before = await (await page.request.get('/api/articles/11/ratings')).json();
  await feedback.getByRole('radio', { name: '5 sao — Rất hữu ích', exact: true }).check();
  await feedback.getByRole('button', { name: 'Gửi đánh giá', exact: true }).click();
  await expect(feedback.getByText('Đã lưu đánh giá. Cảm ơn góc nhìn của bạn!')).toBeVisible();
  await expect(feedback.locator('.rating-overview')).toContainText(`${before.count + 1} lượt đánh giá`);
  const message = `Góc nhìn của độc giả ${testInfo.project.name} ${Date.now()}`;
  await feedback.getByLabel('Chia sẻ góc nhìn của bạn').fill(message);
  await feedback.getByRole('button', { name: 'Gửi bình luận', exact: true }).click();
  const dialog = page.getByRole('dialog');
  await expect(dialog).toHaveAccessibleName('Đăng ký để bình luận');
  await expect(dialog.getByRole('button', { name: 'Tiếp tục với Google' })).toBeDisabled();
  await dialog.getByRole('button', { name: 'Close', exact: true }).click();
  await registerReader(page, `${testInfo.project.name}-${Date.now()}`);
  await expect(feedback.getByLabel('Chia sẻ góc nhìn của bạn')).toHaveValue(message);
  await feedback.getByRole('button', { name: 'Gửi bình luận', exact: true }).click();
  await expect(feedback.getByText('Bình luận của bạn đã được đăng.')).toBeVisible();
  await expect(feedback.locator('.reader-comment').filter({ hasText: message })).toBeVisible();
  await page.reload();
  await expect(feedback.getByRole('radio', { name: '5 sao — Rất hữu ích', exact: true })).toBeChecked();
  await expect(feedback.locator('.comment-account')).toContainText('Minh An');
  await expect(feedback.locator('.reader-comment').filter({ hasText: message })).toBeVisible();
  await feedback.getByRole('radio', { name: '3 sao — Khá hữu ích', exact: true }).check();
  await feedback.getByRole('button', { name: 'Cập nhật đánh giá', exact: true }).click();
  await expect(feedback.getByText('Đã lưu đánh giá. Cảm ơn góc nhìn của bạn!')).toBeVisible();
  await expect(feedback.locator('.rating-overview')).toContainText(`${before.count + 1} lượt đánh giá`);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await feedback.getByLabel('Chia sẻ góc nhìn của bạn').fill('Bản nháp chưa gửi cho bài hiện tại');
  await page.locator('.related-section h3 a').click();
  await expect(feedback.getByLabel('Chia sẻ góc nhìn của bạn')).toHaveValue('');
  await expect(feedback.locator('.reader-comment').filter({ hasText: message })).toHaveCount(0);
});

test('feedback loading failures do not hide the article and can be retried', async ({ page }) => {
  await page.route('**/api/articles/*/comments?**', route => route.abort());
  await page.route('**/api/articles/*/ratings', route => route.abort());
  await page.goto('/bai-viet/1');
  await expect(page.locator('.article-header h1')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Thử tải lại đánh giá' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Thử tải lại bình luận' })).toBeVisible();
  await page.unroute('**/api/articles/*/ratings');
  await page.unroute('**/api/articles/*/comments?**');
  await page.getByRole('button', { name: 'Thử tải lại đánh giá' }).click();
  await page.getByRole('button', { name: 'Thử tải lại bình luận' }).click();
  await expect(page.locator('.article-feedback .feedback-error')).toHaveCount(0);
  await expect(page.getByRole('radio', { name: '4 sao — Hữu ích', exact: true })).toBeEnabled();
});

test('failed submissions preserve input and comments display HTML as plain text', async ({ page }, testInfo) => {
  await page.goto('/bai-viet/10');
  const form = page.locator('.article-feedback');
  await expect(form.locator('.feedback-loading')).toHaveCount(0);
  await page.route('**/api/articles/10/comments', route => route.request().method() === 'POST' ? route.abort() : route.continue());
  const content = '<img src=x onerror="window.feedbackXss=true"> Nội dung được hiển thị như văn bản.';
  await registerReader(page, `retry-${testInfo.project.name}-${Date.now()}`);
  await form.getByLabel('Chia sẻ góc nhìn của bạn').fill(content);
  await form.getByRole('button', { name: 'Gửi bình luận', exact: true }).click();
  await expect(form.locator('.comment-form .feedback-error')).toBeVisible();
  await expect(form.getByLabel('Chia sẻ góc nhìn của bạn')).toHaveValue(content);
  await page.unroute('**/api/articles/10/comments');
  await form.getByRole('button', { name: 'Gửi bình luận', exact: true }).click();
  const comment = form.locator('.reader-comment').filter({ hasText: content }).first();
  await expect(comment).toBeVisible();
  await expect(comment.locator('img')).toHaveCount(0);
  expect(await page.evaluate(() => 'feedbackXss' in window)).toBe(false);
  await page.route('**/api/articles/10/ratings', route => route.request().method() === 'POST' ? route.abort() : route.continue());
  await form.getByRole('radio', { name: '4 sao — Hữu ích', exact: true }).check();
  await form.getByRole('button', { name: 'Gửi đánh giá', exact: true }).click();
  await expect(form.locator('.rating-form .feedback-error')).toBeVisible();
  await expect(form.getByRole('radio', { name: '4 sao — Hữu ích', exact: true })).toBeChecked();
  await page.unroute('**/api/articles/10/ratings');
  await form.getByRole('button', { name: 'Gửi đánh giá', exact: true }).click();
  await expect(form.getByText('Đã lưu đánh giá. Cảm ơn góc nhìn của bạn!')).toBeVisible();
});

test('comment pagination loads the requested page', async ({ page }) => {
  await page.route('**/api/articles/1/comments?**', async route => {
    const number = Number(new URL(route.request().url()).searchParams.get('page'));
    const items = Array.from({ length: number === 1 ? 10 : 1 }, (_, i) => ({
      id: `${number}-${i}`, displayName: 'Độc giả', content: `Góc nhìn trang ${number}, bình luận ${i + 1}`,
      createdAt: '2026-10-21T07:00:00Z',
    }));
    await route.fulfill({ json: { items, totalCount: 11, page: number, pageSize: 10, totalPages: 2 } });
  });
  await page.goto('/bai-viet/1');
  await expect(page.locator('.reader-comment')).toHaveCount(10);
  await page.getByRole('button', { name: 'Bình luận sau', exact: true }).click();
  await expect(page.locator('.reader-comment')).toHaveCount(1);
  await expect(page.locator('.reader-comment')).toContainText('Góc nhìn trang 2');
  await expect(page.getByRole('button', { name: 'Bình luận sau', exact: true })).toBeDisabled();
  await page.getByRole('button', { name: 'Bình luận trước', exact: true }).click();
  await expect(page.locator('.reader-comment')).toHaveCount(10);
});


test('expired comment session opens login and preserves the draft', async ({ page }, testInfo) => {
  await page.goto('/bai-viet/9');
  const suffix = `expired-${testInfo.project.name}-${Date.now()}`;
  await registerReader(page, suffix);
  const field = page.getByLabel('Chia sẻ góc nhìn của bạn');
  await field.fill('Bình luận tiếp tục sau khi đăng nhập lại.');
  await page.route('**/api/articles/9/comments', route => route.request().method() === 'POST' ? route.fulfill({ status: 401, json: {} }) : route.continue());
  await page.route('**/api/auth/refresh', route => route.fulfill({ status: 401, json: {} }));
  await page.getByRole('button', { name: 'Gửi bình luận', exact: true }).click();
  const dialog = page.getByRole('dialog');
  await expect(dialog).toHaveAccessibleName('Đăng nhập để bình luận');
  await page.unroute('**/api/articles/9/comments');
  await page.unroute('**/api/auth/refresh');
  await dialog.getByLabel('Email', { exact: true }).fill(`reader-${suffix}@example.com`);
  await dialog.getByLabel('Mật khẩu', { exact: true }).fill('PrivateReaderPassword!2026');
  await dialog.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
  await expect(dialog).toHaveCount(0);
  await expect(field).toHaveValue('Bình luận tiếp tục sau khi đăng nhập lại.');
  await page.getByRole('button', { name: 'Gửi bình luận', exact: true }).click();
  await expect(page.locator('.reader-comment').filter({ hasText: 'Bình luận tiếp tục sau khi đăng nhập lại.' }).first()).toBeVisible();
  await page.locator('.comment-account').getByRole('button', { name: 'Đăng xuất', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Đăng ký để bình luận', exact: true })).toBeVisible();
});

test('Google sign-in sends the credential and keeps the reader on the article', async ({ page }, testInfo) => {
  const nonce = 'a'.repeat(64);
  await page.route('**/api/auth/google/options', route => route.fulfill({ json: { clientId: 'test.apps.googleusercontent.com', nonce } }));
  await page.route('https://accounts.google.com/gsi/client', route => route.fulfill({
    contentType: 'application/javascript', body: `window.google = { accounts: { id: {
      initialize(options) { window.testGoogleOptions = options; },
      renderButton(container) {
        const button = document.createElement('button'); button.type = 'button';
        button.textContent = 'Tiếp tục với Google';
        button.onclick = () => window.testGoogleOptions.callback({ credential: 'google-test-credential' });
        container.append(button);
      }
    } } };`,
  }));
  await page.route('**/api/auth/google', async route => {
    expect(route.request().postDataJSON()).toEqual({ credential: 'google-test-credential' });
    // Thay nhà cung cấp bên ngoài; giữ phiên đăng nhập thật để kiểm tra gửi bình luận.
    const response = await route.fetch({ url: new URL('/api/auth/register', route.request().url()).href, method: 'POST',
      postData: { name: 'Độc giả Google', email: `google-${testInfo.project.name}-${Date.now()}@example.com`, password: 'PrivateReaderPassword!2026' },
    });
    await route.fulfill({ response });
  });
  await page.goto('/bai-viet/8');
  await page.getByLabel('Chia sẻ góc nhìn của bạn').fill('Bình luận sau khi đăng nhập với Google.');
  await page.getByRole('button', { name: 'Gửi bình luận', exact: true }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Tiếp tục với Google' }).click();
  await expect(page.getByRole('dialog')).toHaveCount(0);
  await expect(page.locator('.comment-account')).toContainText('Độc giả Google');
  await expect(page).toHaveURL(/bai-viet\/8$/);
  await page.getByRole('button', { name: 'Gửi bình luận', exact: true }).click();
  await expect(page.locator('.reader-comment').filter({ hasText: 'Bình luận sau khi đăng nhập với Google.' }).first()).toBeVisible();
  await page.reload();
  await expect(page.locator('.comment-account')).toContainText('Độc giả Google');
});
