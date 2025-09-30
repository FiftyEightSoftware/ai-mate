import { test, expect } from '@playwright/test';
const BACKEND = process.env.BACKEND_URL || 'http://localhost:5280';

// Invoices UI import flow

test.describe('Invoices UI import flow', () => {
  test('import minimal invoices JSON and verify in list', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Navigate to Invoices via Home
    await page.getByRole('link', { name: 'Invoices' }).first().click();
    await expect(page.getByRole('heading', { name: 'Invoices', level: 3 })).toBeVisible();

    // Prepare payload
    const id = `E2E-${Date.now()}`;
    const customer = `E2E Customer ${Date.now()}`;
    const payload = [
      {
        id,
        customer,
        amount: 123.45,
        status: 'unpaid',
        issueDate: '2025-01-01',
        dueDate: '2025-12-31'
      }
    ];

    // Find textarea by generic placeholder starting with '[' and fill JSON
    const ta = page.locator('textarea[placeholder^="["]').first();
    await ta.fill(JSON.stringify(payload));

    // Click Import
    await page.getByRole('button', { name: 'Import' }).click();

    // Info banner may appear; don't fail if not visible due to quick refresh
    const info = page.getByText(/Imported/i).first();
    await info.waitFor({ state: 'visible', timeout: 1500 }).catch(() => {});

    // Verify via API that the invoice exists
    await test.step('verify via API', async () => {
      await page.request.post(`${BACKEND}/api/import/invoices`, { data: [] }).catch(() => {});
      const res = await page.request.get(`${BACKEND}/api/invoices`);
      expect(res.ok()).toBeTruthy();
      const list = await res.json();
      expect(Array.isArray(list)).toBeTruthy();
      const found = list.find((x: any) => x.customer === customer && x.id === id);
      expect(!!found).toBeTruthy();
    });

    // Trigger UI refresh and assert presence visually
    await page.getByRole('button', { name: '↻ Refresh' }).click();
    await expect(page.locator('.list-item .title', { hasText: customer }).first()).toBeVisible({ timeout: 8000 });
  });
});
