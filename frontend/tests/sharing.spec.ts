import { expect, test } from '@playwright/test';

test('share dialog copies the article URL and opens the selected sharing destination', async ({ page, context }) => {
  await page.addInitScript(() => {
    Object.defineProperty(navigator, 'share', { configurable: true, value: undefined });
    Object.defineProperty(navigator, 'clipboard', { configurable: true, value: {
      writeText: async (value: string) => { (window as unknown as { copiedArticleUrl: string }).copiedArticleUrl = value; },
    } });
  });
  await page.goto('/bai-viet/1?source=reading#feedback-title');
  await page.getByRole('button', { name: 'Chia sẻ', exact: true }).click();
  const dialog = page.getByRole('dialog', { name: 'Chia sẻ bài viết', exact: true });
  await expect(dialog).toBeVisible();
  const title = await page.locator('.article-header h1').innerText();
  const url = new URL('/bai-viet/1', page.url()).href;
  await expect(dialog.getByLabel('Liên kết bài viết')).toHaveValue(url);
  const facebook = dialog.getByRole('link', { name: 'Chia sẻ qua Facebook', exact: true });
  const facebookUrl = new URL((await facebook.getAttribute('href'))!);
  expect(facebookUrl.searchParams.get('u')).toBe(url);
  await expect(facebook).toHaveAttribute('rel', 'noopener noreferrer');
  const xUrl = new URL((await dialog.getByRole('link', { name: 'Chia sẻ qua X', exact: true }).getAttribute('href'))!);
  expect(xUrl.searchParams.get('url')).toBe(url);
  expect(xUrl.searchParams.get('text')).toBe(title);
  const emailUrl = new URL((await dialog.getByRole('link', { name: 'Chia sẻ qua email', exact: true }).getAttribute('href'))!);
  expect(emailUrl.protocol).toBe('mailto:');
  expect(emailUrl.searchParams.get('subject')).toBe(title);
  expect(emailUrl.searchParams.get('body')).toContain(url);
  await expect(dialog.getByRole('button', { name: 'Ứng dụng khác', exact: true })).toHaveCount(0);
  await dialog.getByRole('button', { name: 'Sao chép liên kết', exact: true }).click();
  await expect(dialog.getByRole('status')).toHaveText('Đã sao chép liên kết bài viết.');
  expect(await page.evaluate(() => (window as unknown as { copiedArticleUrl: string }).copiedArticleUrl)).toBe(url);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  // Dừng ở màn hình chia sẻ giả lập, không gửi gì tới mạng xã hội.
  await context.route('https://www.facebook.com/sharer/**', route => route.fulfill({ body: '<h1>Share destination</h1>', contentType: 'text/html' }));
  const popupPromise = page.waitForEvent('popup');
  await facebook.click();
  const popup = await popupPromise;
  await expect(popup).toHaveURL(facebookUrl.href);
  await popup.close();
});

test('blocked clipboard selects the link and switching articles resets the sharing state', async ({ page }) => {
  await page.addInitScript(() => {
    Object.defineProperty(navigator, 'clipboard', { configurable: true, value: {
      writeText: async () => { throw new DOMException('Clipboard blocked', 'NotAllowedError'); },
    } });
  });
  await page.goto('/bai-viet/1');
  await page.getByRole('button', { name: 'Chia sẻ', exact: true }).click();
  const dialog = page.getByRole('dialog', { name: 'Chia sẻ bài viết', exact: true });
  await dialog.getByRole('button', { name: 'Sao chép liên kết', exact: true }).click();
  await expect(dialog.getByRole('status')).toContainText('sao chép thủ công');
  const input = dialog.getByLabel('Liên kết bài viết');
  await expect(input).toBeFocused();
  expect(await input.evaluate((element: HTMLInputElement) => element.selectionStart === 0 && element.selectionEnd === element.value.length)).toBe(true);
  await dialog.getByRole('button', { name: 'Close', exact: true }).click();
  await page.locator('.related-section h3 a').click();
  await expect(page).toHaveURL(/bai-viet\/7$/);
  await page.getByRole('button', { name: 'Chia sẻ bài viết', exact: true }).click();
  await expect(dialog.getByLabel('Liên kết bài viết')).toHaveValue(new URL('/bai-viet/7', page.url()).href);
  await expect(dialog.locator('.share-article-title')).toHaveText(await page.locator('.article-header h1').innerText());
  await expect(dialog.getByRole('status')).toHaveCount(0);
});

test('device sharing uses the current article and handles cancellation and failure', async ({ page }) => {
  await page.addInitScript(() => {
    type ShareWindow = { shareResult?: string; sharedArticle?: ShareData };
    Object.defineProperty(navigator, 'share', { configurable: true, value: async (data: ShareData) => {
      const state = window as unknown as ShareWindow;
      state.sharedArticle = data;
      if (state.shareResult === 'cancel') throw new DOMException('User cancelled', 'AbortError');
      if (state.shareResult === 'error') throw new DOMException('Sharing blocked', 'NotAllowedError');
    } });
  });
  await page.goto('/bai-viet/2');
  await page.getByRole('button', { name: 'Chia sẻ', exact: true }).click();
  const dialog = page.getByRole('dialog', { name: 'Chia sẻ bài viết', exact: true });
  const button = dialog.getByRole('button', { name: 'Ứng dụng khác', exact: true });
  await button.click();
  await expect(dialog.getByRole('status')).toHaveText('Đã mở chia sẻ trên thiết bị.');
  expect(await page.evaluate(() => (window as unknown as { sharedArticle: ShareData }).sharedArticle)).toEqual({
    title: await page.locator('.article-header h1').innerText(),
    text: await page.locator('.article-lead').innerText(),
    url: new URL('/bai-viet/2', page.url()).href,
  });
  await page.evaluate(() => { (window as unknown as { shareResult: string }).shareResult = 'cancel'; });
  await button.click();
  await expect(dialog.getByRole('status')).toHaveCount(0);
  await expect(button).toBeEnabled();
  await page.evaluate(() => { (window as unknown as { shareResult: string }).shareResult = 'error'; });
  await button.click();
  await expect(dialog.getByRole('status')).toContainText('chọn cách khác hoặc sao chép liên kết');
  await expect(dialog.getByRole('button', { name: 'Sao chép liên kết', exact: true })).toBeEnabled();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
});
