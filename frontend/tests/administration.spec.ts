import { expect, test, type Page } from '@playwright/test';

async function login(page: Page, role: 'Admin' | 'Editor' | 'Author') {
  await page.goto('/dang-nhap');
  await page.getByLabel('Email', { exact: true }).fill(`${role.toLowerCase()}@tin24.local`);
  await page.getByLabel('Mật khẩu', { exact: true }).fill(`Tin24@${role}2026!`);
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Tòa soạn của bạn.' })).toBeVisible();
}

test('admin creates a role, creates an account, reassigns it and creates a usable category', async ({ page, browser }, testInfo) => {
  const suffix = `${testInfo.project.name}-${Date.now()}`;
  const roleName = `Cộng tác viên ${suffix}`;
  const userName = `Nhân sự ${suffix}`;
  const email = `staff-${suffix}@example.com`;
  const categoryName = `Kinh tế ${suffix}`;
  await login(page, 'Admin');
  const mobileMenu = page.getByRole('button', { name: 'Mở menu quản trị' });
  if (testInfo.project.name === 'mobile') await mobileMenu.click();
  const accountsMenu = page.getByRole('button', { name: 'Quản lý tài khoản', exact: true });
  await expect(accountsMenu).toHaveAttribute('aria-expanded', 'false');
  await expect(page.getByRole('link', { name: 'Vai trò', exact: true })).toBeHidden();
  await accountsMenu.click();
  await expect(accountsMenu).toHaveAttribute('aria-expanded', 'true');
  if (testInfo.project.name === 'mobile') await expect(mobileMenu).toHaveAttribute('aria-expanded', 'true');
  await page.getByRole('link', { name: 'Vai trò', exact: true }).click();
  await expect(page).toHaveURL(/quan-tri\/vai-tro$/);
  if (testInfo.project.name === 'mobile') await expect(mobileMenu).toHaveAttribute('aria-expanded', 'false');
  await page.getByRole('button', { name: 'Tạo vai trò', exact: true }).click();
  await page.getByLabel('Tên vai trò', { exact: true }).fill(roleName);
  await page.getByRole('button', { name: 'Lưu vai trò', exact: true }).click();
  await expect(page.getByRole('row').filter({ hasText: roleName })).toContainText('Tác giả');

  await page.reload();
  if (testInfo.project.name === 'mobile') await mobileMenu.click();
  await expect(accountsMenu).toHaveAttribute('aria-expanded', 'true');
  await page.getByRole('link', { name: 'Người dùng', exact: true }).click();
  await page.getByRole('button', { name: 'Tạo người dùng', exact: true }).click();
  await page.getByLabel('Họ tên', { exact: true }).fill(userName);
  await page.getByLabel('Email', { exact: true }).fill(email);
  await page.getByLabel('Mật khẩu', { exact: true }).fill('PrivatePassword!2026');
  await page.locator('#user-role').click();
  await page.getByRole('option', { name: roleName, exact: true }).click();
  await page.getByRole('button', { name: 'Lưu người dùng', exact: true }).click();
  await expect(page.getByRole('dialog')).toHaveCount(0);
  await page.getByRole('textbox', { name: 'Tìm người dùng' }).fill(email);
  const userRow = page.getByRole('row').filter({ hasText: email });
  await expect(userRow).toContainText(roleName);

  const context = await browser.newContext();
  try {
    const staff = await context.newPage();
    await staff.goto('/dang-nhap');
    await staff.getByLabel('Email', { exact: true }).fill(email);
    await staff.getByLabel('Mật khẩu', { exact: true }).fill('PrivatePassword!2026');
    await staff.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
    await expect(staff.getByRole('heading', { name: 'Tòa soạn của bạn.' })).toBeVisible();
    await expect(staff.locator('.admin-role')).toHaveText(roleName);
    await staff.goto('/quan-tri/nguoi-dung');
    await expect(staff).toHaveURL(/quan-tri$/);

    // Đăng nhập cập nhật version của tài khoản; tải bản mới trước khi phân quyền.
    await page.getByRole('button', { name: 'Tải lại', exact: true }).click();
    await expect(page.getByRole('button', { name: 'Tải lại', exact: true })).toBeEnabled();
    await userRow.getByRole('button', { name: 'Phân quyền', exact: true }).click();
    await page.locator('#user-role').click();
    await page.getByRole('option', { name: 'Biên tập viên', exact: true }).click();
    await page.getByRole('button', { name: 'Lưu phân quyền', exact: true }).click();
    await expect(userRow).toContainText('Biên tập viên');
    await staff.reload();
    await expect(staff).toHaveURL(/dang-nhap/);
  } finally { await context.close(); }

  if (testInfo.project.name === 'mobile') await mobileMenu.click();
  const newsMenu = page.getByRole('button', { name: 'Quản lý tin tức', exact: true });
  await expect(newsMenu).toHaveAttribute('aria-expanded', 'false');
  await newsMenu.click();
  if (testInfo.project.name === 'mobile') await expect(mobileMenu).toHaveAttribute('aria-expanded', 'true');
  await page.getByRole('link', { name: 'Danh mục bài viết', exact: true }).click();
  await page.getByRole('button', { name: 'Tạo danh mục', exact: true }).click();
  await page.getByLabel('Tên danh mục', { exact: true }).fill(categoryName);
  await expect(page.getByLabel('Slug danh mục', { exact: true })).toHaveValue(`kinh-te-${suffix}`);
  await page.getByRole('button', { name: 'Lưu danh mục', exact: true }).click();
  await expect(page.getByRole('row').filter({ hasText: categoryName })).toBeVisible();
  await page.reload();
  await expect(page.getByRole('row').filter({ hasText: categoryName })).toBeVisible();
  if (testInfo.project.name === 'mobile') await mobileMenu.click();
  await expect(newsMenu).toHaveAttribute('aria-expanded', 'true');
  await page.getByRole('navigation', { name: 'Điều hướng quản trị' }).getByRole('link', { name: 'Soạn bài mới', exact: true }).click();
  await page.locator('#article-category').click();
  await expect(page.getByRole('option', { name: categoryName, exact: true })).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.goto('/');
  if (testInfo.project.name === 'mobile') await page.getByRole('button', { name: 'Mở chuyên mục' }).click();
  const categoryLink = page.locator('#main-nav').getByRole('link', { name: categoryName, exact: true });
  await categoryLink.click();
  await expect(page).toHaveURL(new RegExp(`category=kinh-te-${suffix}`));
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
});

test('editor cannot access administration pages or see administration links', async ({ page }) => {
  await login(page, 'Editor');
  await expect(page.getByRole('button', { name: 'Quản lý tài khoản', exact: true })).toHaveCount(0);
  await expect(page.getByRole('link', { name: 'Người dùng', exact: true })).toHaveCount(0);
  await expect(page.getByRole('link', { name: 'Vai trò', exact: true })).toHaveCount(0);
  await expect(page.getByRole('link', { name: 'Danh mục bài viết', exact: true })).toHaveCount(0);
  for (const path of ['nguoi-dung', 'vai-tro', 'danh-muc']) {
    await page.goto(`/quan-tri/${path}`);
    await expect(page).toHaveURL(/quan-tri$/);
  }
});
