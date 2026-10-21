import { expect, test } from '@playwright/test';

test('homepage and pagination expose all articles', async ({ page }) => {
  const errors: string[] = [];
  page.on('pageerror', error => errors.push(error.message));
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Nhịp sống hôm nay.' })).toBeVisible();
  await expect(page.locator('.lead-story')).toBeVisible();
  await expect(page.locator('.highlight-card')).toHaveCount(2);
  await expect(page.locator('.news-card')).toHaveCount(6);
  await page.getByRole('button', { name: 'Trang sau', exact: true }).click();
  await expect(page).toHaveURL(/page=2/);
  await expect(page.locator('.news-card')).toHaveCount(3);
  await expect(page.locator('.lead-story')).toHaveCount(0);
  await page.getByRole('button', { name: 'Trang trước', exact: true }).click();
  await expect(page.locator('.lead-story')).toBeVisible();
  expect(errors).toEqual([]);
});

test('Vietnamese search works without accents and survives reload', async ({ page }) => {
  await page.goto('/');
  await page.getByRole('searchbox', { name: 'Tìm kiếm tin tức' }).fill('tri tue nhan tao');
  await page.getByRole('button', { name: 'Tìm kiếm', exact: true }).click();
  await expect(page.locator('.news-card')).toHaveCount(1);
  await expect(page.locator('.news-card h3')).toContainText('Trí tuệ nhân tạo');
  await page.reload();
  await expect(page.locator('.news-card')).toHaveCount(1);
  await expect(page.getByRole('searchbox')).toHaveValue('tri tue nhan tao');
});

test('category filters, empty results and browser history work', async ({ page }) => {
  await page.goto('/');
  await page.locator('.category-filter').getByRole('link', { name: 'Du lịch' }).click();
  await expect(page.locator('.news-card')).toHaveCount(2);
  await expect(page.locator('.news-card .category')).toHaveText(['Du lịch', 'Du lịch']);
  await page.goBack();
  await expect(page.locator('.lead-story')).toBeVisible();
  await page.goto('/?search=khong-co-ket-qua-xyz');
  await expect(page.getByRole('heading', { name: 'Chưa tìm thấy câu chuyện phù hợp' })).toBeVisible();
  await page.getByRole('link', { name: 'Xem tất cả tin tức' }).click();
  await expect(page.locator('.lead-story')).toBeVisible();
});

test('saved articles persist and can be removed', async ({ page }) => {
  await page.goto('/?category=cong-nghe');
  const card = page.locator('.news-card').first();
  const title = await card.locator('h3').innerText();
  await card.getByRole('button', { name: /^Lưu bài:/ }).click();
  await page.getByRole('link', { name: /^Đã lưu/ }).click();
  await expect(page.locator('.news-card')).toHaveCount(1);
  await expect(page.locator('.news-card h3')).toHaveText(title);
  await page.reload();
  await expect(page.locator('.news-card')).toHaveCount(1);
  await page.getByRole('button', { name: /^Bỏ lưu:/ }).click();
  await expect(page.getByRole('heading', { name: 'Chưa có bài viết nào được lưu' })).toBeVisible();
});

test('related article navigation reloads the reused detail view', async ({ page }) => {
  await page.goto('/bai-viet/1');
  await expect(page.locator('.article-header h1')).toHaveText('Khi thành phố dành nhiều không gian hơn cho con người');
  await expect(page.locator('.article-content > p')).toHaveCount(3);
  await page.locator('.related-section h3 a').click();
  await expect(page).toHaveURL(/\/bai-viet\/7$/);
  await expect(page.locator('.article-header h1')).toHaveText('Bữa cơm nhà và những kết nối giản dị');
  await expect(page).toHaveTitle(/Bữa cơm nhà/);
});

test('failed requests offer a working retry', async ({ page }) => {
  await page.route('**/api/articles?**', route => route.abort());
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Kết nối đang gián đoạn' })).toBeVisible();
  await page.unroute('**/api/articles?**');
  await page.getByRole('button', { name: 'Thử lại' }).click();
  await expect(page.locator('.lead-story')).toBeVisible();
});

test('invalid routes and article identifiers show clear empty states', async ({ page }) => {
  await page.goto('/bai-viet/abc');
  await expect(page.getByRole('heading', { name: 'Không tìm thấy bài viết' })).toBeVisible();
  await page.goto('/bai-viet/999');
  await expect(page.getByRole('heading', { name: 'Không tìm thấy bài viết' })).toBeVisible();
  await page.goto('/khong-ton-tai');
  await expect(page.getByRole('heading', { name: 'Trang này chưa có câu chuyện nào' })).toBeVisible();
});

test('layout fits the viewport and mobile menu navigates', async ({ page }, testInfo) => {
  await page.goto('/');
  await expect(page.locator('.lead-story')).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  if (testInfo.project.name === 'mobile') {
    const banner = await page.locator('.reading-banner').boundingBox();
    expect(banner?.height).toBeLessThan(350);
    await page.getByRole('button', { name: 'Mở chuyên mục' }).click();
    await page.locator('#main-nav').getByRole('link', { name: 'Công nghệ' }).click();
    await expect(page.locator('.news-card')).toHaveCount(2);
    await expect(page.getByRole('button', { name: 'Mở chuyên mục' })).toBeVisible();
  }
});

test('broken images fall back without breaking article links', async ({ page }) => {
  await page.route('https://images.unsplash.com/**', route => route.abort());
  await page.goto('/?category=cong-nghe');
  await expect(page.locator('.image-fallback').first()).toBeVisible();
  await page.locator('.news-card h3 a').first().click();
  await expect(page.locator('.article-header h1')).toBeVisible();
});
