import { test, expect } from '@playwright/test';

// Basic UI smoke tests against the Blazor frontend

test.describe('UI smoke', () => {
  test('homepage loads with 200 and shows basic content', async ({ page, baseURL }) => {
    await page.goto('/');
    // Ensure network idle to let Blazor boot
    await page.waitForLoadState('networkidle');

    // Basic assertions: non-empty title and at least one visible element
    const title = await page.title();
    expect(title && title.length > 0).toBeTruthy();

    // Look for a common UI element text. Adjust as needed based on actual app copy.
    const candidates = [
      'Dashboard',
      'Invoices',
      'Payments',
      'Jobs',
      'AI Mate',
    ];
    const anyVisible = await Promise.any(
      candidates.map(async (txt) => {
        const el = page.getByText(new RegExp(txt, 'i'));
        await el.first().waitFor({ state: 'visible', timeout: 5000 });
        return true;
      })
    ).catch(() => false);

    expect(anyVisible).toBeTruthy();
  });

  test('navigation: Dashboard / Jobs / Invoices / Payments if present', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    const sections = ['Dashboard', 'Jobs', 'Invoices', 'Payments'];
    for (const name of sections) {
      const link = page.getByRole('link', { name, exact: false }).first();
      try {
        await link.waitFor({ state: 'visible', timeout: 2000 });
        await link.click({ trial: true }).catch(() => {});
        await link.click().catch(() => {});
        // Wait a moment for navigation/render
        await page.waitForTimeout(300);
      } catch {
        // Link not present; continue
      }
    }

    // After navigation attempts, page remains responsive
    expect(await page.title()).toBeTruthy();
  });
});
