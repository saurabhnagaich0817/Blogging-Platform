import { test, expect } from '@playwright/test';

test.describe('Authentication Flow', () => {
  const timestamp = Date.now();
  const testEmail = `test_${timestamp}@inkwell.com`;
  const testPassword = 'TestPassword@123';

  test('should register and then login successfully', async ({ page }) => {
    // 1. Register
    await page.goto('http://localhost:4200/auth/register');
    await page.fill('input[placeholder="Enter your full name"]', 'Test User');
    await page.fill('input[placeholder="john_doe"]', `user_${timestamp}`);
    await page.fill('input[placeholder="john@example.com"]', testEmail);
    await page.fill('input[placeholder="Enter your password"]', testPassword);
    
    // Select Author role (assuming it's a select or radio)
    // For now, just click register if it's default
    await page.click('button.btn-premium');
    
    // Wait for redirect to login
    await page.waitForURL('**/auth/login', { timeout: 15000 });

    // 2. Login
    await page.fill('input[placeholder="john@example.com"]', testEmail);
    await page.fill('input[placeholder="Enter your password"]', testPassword);
    await page.click('button.btn-premium');

    // 3. Verify Redirect to a secure page (like explore or posts)
    await expect(page).not.toHaveURL(/.*auth\/login/, { timeout: 20000 });
  });

  test('should have disabled login button initially', async ({ page }) => {
    await page.goto('http://localhost:4200/auth/login');
    const loginButton = page.locator('button.btn-premium');
    await expect(loginButton).toBeDisabled();
  });
});
