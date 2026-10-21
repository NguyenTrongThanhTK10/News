import { expect, test, type Page } from '@playwright/test';

async function login(page: Page, role: 'Author' | 'Editor' | 'Admin') {
  await page.goto('/dang-nhap');
  await page.getByLabel('Email', { exact: true }).fill(`${role.toLowerCase()}@tin24.local`);
  await page.getByLabel('Mật khẩu', { exact: true }).fill(`Tin24@${role}2026!`);
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Tòa soạn của bạn.' })).toBeVisible();
}

test('admin route requires login and rejects invalid credentials', async ({ page }) => {
  await page.goto('/quan-tri');
  await expect(page).toHaveURL(/dang-nhap/);
  await page.getByLabel('Email', { exact: true }).fill('nobody@example.com');
  await page.getByLabel('Mật khẩu', { exact: true }).fill('incorrect');
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
  await expect(page.getByText('Email hoặc mật khẩu không đúng, hoặc tài khoản tạm khóa.')).toBeVisible();
});

test('learning center covers all five parts and interactive Vue examples', async ({ page }) => {
  await page.goto('/hoc-vue');
  await expect(page.locator('.lesson-card')).toHaveCount(5);
  await page.getByLabel('Tiêu đề · v-model & ref').fill('Tiêu đề học Vue mới');
  await expect(page.locator('.learning-preview h3')).toHaveText('Tiêu đề học Vue mới');
  await page.getByRole('button', { name: 'Ẩn component (v-if)' }).click();
  await expect(page.locator('.learning-preview')).toHaveCount(0);
  await page.getByRole('button', { name: 'Hiện component (v-if)' }).click();
  await page.getByRole('button', { name: 'Lưu ví dụ' }).click();
  await expect(page.getByRole('button', { name: 'Đã lưu ví dụ' })).toBeVisible();
  await page.getByRole('button', { name: 'Mở Dialog' }).click();
  await expect(page.getByRole('dialog')).toBeVisible();
  await page.getByRole('button', { name: 'Đã hiểu' }).click();
  await expect(page.getByRole('dialog')).toHaveCount(0);
  await page.getByRole('button', { name: 'Thử Toast' }).click();
  await expect(page.getByText('Toast đang hoạt động')).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
});

test('author creates draft and submits; editor approves and publishes', async ({ page, browser }, testInfo) => {
  await login(page, 'Author');
  await page.getByRole('link', { name: 'Soạn bài mới', exact: true }).last().click();
  const title = `Câu chuyện E2E ${testInfo.project.name} ${Date.now()}`;
  await page.getByLabel('Tiêu đề', { exact: false }).fill(title);
  await page.getByLabel('Mô tả ngắn', { exact: false }).fill('Một câu chuyện được tạo để kiểm tra trình soạn thảo và toàn bộ quy trình biên tập.');
  await page.locator('.ql-editor').fill('Nội dung câu chuyện có đầy đủ bối cảnh, thông tin và góc nhìn để gửi đến biên tập viên duyệt trước khi xuất bản.');
  await page.locator('#article-category').click();
  await page.getByRole('option', { name: 'Công nghệ', exact: true }).click();
  await page.getByLabel('Ảnh minh họa', { exact: false }).fill('https://images.unsplash.com/photo-1498050108023-c5249f4df085');
  await page.getByRole('button', { name: 'Xem trước', exact: true }).click();
  await expect(page.getByRole('dialog').getByRole('heading', { name: title })).toBeVisible();
  await page.getByRole('button', { name: 'Close', exact: true }).click();
  await page.getByRole('button', { name: 'Lưu bản nháp', exact: true }).click();
  await expect(page).toHaveURL(/quan-tri\/bai-viet\/\d+\/sua/);
  const id = page.url().match(/bai-viet\/(\d+)/)![1];
  await page.getByRole('link', { name: 'Danh sách bài viết', exact: true }).click();
  await page.getByRole('textbox', { name: 'Tìm bài viết trong tòa soạn' }).fill(title);
  const row = page.getByRole('row').filter({ hasText: title });
  await expect(row).toBeVisible();
  await expect(row.getByRole('button', { name: 'Xuất bản', exact: true })).toHaveCount(0);
  await row.getByRole('button', { name: 'Gửi duyệt', exact: true }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Gửi duyệt', exact: true }).click();
  await expect(row.getByText('Chờ duyệt', { exact: true })).toBeVisible();
  const editorContext = await browser.newContext();
  const editor = await editorContext.newPage();
  await login(editor, 'Editor');
  await editor.getByRole('textbox', { name: 'Tìm bài viết trong tòa soạn' }).fill(title);
  const reviewRow = editor.getByRole('row').filter({ hasText: title });
  await reviewRow.getByRole('button', { name: 'Duyệt bài', exact: true }).click();
  await editor.getByRole('dialog').getByRole('button', { name: 'Duyệt bài', exact: true }).click();
  await expect(reviewRow.getByText('Đã duyệt', { exact: true })).toBeVisible();
  await reviewRow.getByRole('button', { name: 'Xuất bản', exact: true }).click();
  await editor.getByRole('dialog').getByRole('button', { name: 'Xuất bản', exact: true }).click();
  await expect(reviewRow.getByText('Đã xuất bản', { exact: true })).toBeVisible();
  await page.goto(`/bai-viet/${id}`);
  await expect(page.locator('.article-header h1')).toHaveText(title);
  // Gỡ bài sau test để dữ liệu công khai mẫu và phân trang không bị thay đổi lâu dài.
  await reviewRow.getByRole('button', { name: 'Gỡ xuất bản', exact: true }).click();
  await editor.getByRole('dialog').getByRole('button', { name: 'Gỡ xuất bản', exact: true }).click();
  await expect(reviewRow.getByText('Bản nháp', { exact: true })).toBeVisible();
  await editorContext.close();
});

test('session survives reload and logout protects routes', async ({ page }, testInfo) => {
  await login(page, 'Admin');
  await page.reload();
  await expect(page.getByRole('heading', { name: 'Tòa soạn của bạn.' })).toBeVisible();
  expect(await page.evaluate(() => Object.keys(localStorage).some(key => /token/i.test(key)))).toBe(false);
  if (testInfo.project.name === 'mobile') await page.getByRole('button', { name: 'Mở menu quản trị' }).click();
  await page.getByRole('button', { name: 'Đăng xuất', exact: true }).click();
  await expect(page).toHaveURL(/dang-nhap/);
  await page.goto('/quan-tri');
  await expect(page).toHaveURL(/dang-nhap/);
});
