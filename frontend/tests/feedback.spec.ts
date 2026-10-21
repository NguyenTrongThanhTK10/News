import { expect, test } from '@playwright/test';

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
  await feedback.getByLabel('Tên hiển thị').fill('Minh An');
  await feedback.getByLabel('Chia sẻ góc nhìn của bạn').fill(message);
  await feedback.getByRole('button', { name: 'Gửi bình luận', exact: true }).click();
  await expect(feedback.getByText('Bình luận của bạn đã được đăng.')).toBeVisible();
  await expect(feedback.locator('.reader-comment').filter({ hasText: message })).toBeVisible();
  await page.reload();
  await expect(feedback.getByRole('radio', { name: '5 sao — Rất hữu ích', exact: true })).toBeChecked();
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

test('failed submissions preserve input and comments display HTML as plain text', async ({ page }) => {
  await page.goto('/bai-viet/10');
  const form = page.locator('.article-feedback');
  await expect(form.locator('.feedback-loading')).toHaveCount(0);
  await page.route('**/api/articles/10/comments', route => route.request().method() === 'POST' ? route.abort() : route.continue());
  const content = '<img src=x onerror="window.feedbackXss=true"> Nội dung được hiển thị như văn bản.';
  await form.getByLabel('Tên hiển thị').fill('Độc giả thử nghiệm');
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
