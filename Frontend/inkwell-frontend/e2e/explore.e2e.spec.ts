import { test, expect } from '@playwright/test';

test.describe('Explore Page', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('http://localhost:4200/explore');
  });

  test('should display posts on the explore page', async ({ page }) => {
    // Increased timeout for slow browsers
    const postCards = page.locator('article.post-card');
    await expect(postCards.first()).toBeVisible({ timeout: 30000 });
  });

  test('should navigate to post details when clicked', async ({ page }) => {
    const firstPost = page.locator('article.post-card').first();
    await firstPost.click();
    await expect(page).toHaveURL(/.*posts\/.*/, { timeout: 30000 });
  });
});
