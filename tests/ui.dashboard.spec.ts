import { test, expect } from '@playwright/test';

test.describe('Dashboard horizon change', () => {
  test('changing horizon to 12 weeks shows info banner', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    await page.getByRole('link', { name: 'Dashboard' }).first().click();
    await expect(page.getByRole('heading', { name: 'Dashboard', level: 3 })).toBeVisible();

    // Find the Horizon select (single select on page)
    const horizon = page.getByRole('combobox').first();
    await expect(horizon).toBeVisible();

    await horizon.selectOption('12');

    // Info banner should appear: "Horizon set to 12 weeks"
    await expect(page.getByText(/Horizon set to\s+12\s+weeks/)).toBeVisible();
  });
});
