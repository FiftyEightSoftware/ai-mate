import { test, expect } from '@playwright/test';

const BACKEND = process.env.BACKEND_URL || 'http://localhost:5280';

async function getJobs(request: any) {
  const res = await request.get(`${BACKEND}/api/jobs`);
  expect(res.ok()).toBeTruthy();
  return res.json();
}

test.describe('UI workflows (best-effort, resilient)', () => {
  test('create job from UI and verify via API', async ({ page, request }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Navigate using Home cards
    await page.getByRole('link', { name: 'Jobs' }).first().click();
    await expect(page.getByRole('heading', { name: 'Jobs', level: 3 })).toBeVisible();

    // Fill known input and click known button per Jobs.razor
    const title = `UI E2E Job ${Date.now()}`;
    await page.getByPlaceholder('New job title').fill(title);
    await page.getByRole('button', { name: 'Add' }).click();

    // Verify via API the new job exists (appears at top by insertion)
    const jobs = await getJobs(request);
    const match = jobs.find((j: any) => String(j.title) === title);
    expect(!!match).toBeTruthy();

    // Verify in UI list the title is present
    const titleLocator = page.locator('.list .list-item .title', { hasText: title }).first();
    await expect(titleLocator).toBeVisible();
  });

  test('dashboard totals remain visible after marking an invoice paid', async ({ page, request }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');

    // Navigate to Dashboard using Home card
    await page.getByRole('link', { name: 'Dashboard' }).first().click();
    await expect(page.getByRole('heading', { name: 'Dashboard', level: 3 })).toBeVisible();

    // Target specific cards by their titles
    const paidCard = page.locator('.card', { has: page.getByText('Paid (30d') }).first();
    const paidValue = paidCard.locator(':scope > div').last();
    await expect(paidCard).toBeVisible();
    await expect(paidValue).toBeVisible();

    // Mark one invoice paid via API
    const invRes = await request.get(`${BACKEND}/api/invoices`);
    expect(invRes.ok()).toBeTruthy();
    const list: any[] = await invRes.json();
    if (list.length > 0) {
      const inv = list[0];
      await request.post(`${BACKEND}/api/invoices/${inv.id}/mark-paid`, { data: {} });
    }

    // Refresh dashboard
    await page.reload();
    await page.waitForLoadState('networkidle');

    await expect(paidCard).toBeVisible();
    await expect(paidValue).toBeVisible();
  });
});
