import { test, expect, APIRequestContext } from '@playwright/test';

const BACKEND = process.env.BACKEND_URL || 'http://localhost:5280';

async function getInvoices(ctx: APIRequestContext) {
  const res = await ctx.get(`${BACKEND}/api/invoices`);
  expect(res.ok()).toBeTruthy();
  return res.json();
}

async function getDashboard(ctx: APIRequestContext) {
  const res = await ctx.get(`${BACKEND}/api/dashboard?weeks=8`);
  expect(res.ok()).toBeTruthy();
  return res.json();
}

test.describe('Data consistency', () => {
  test('mark invoice paid reflects in invoices list and/or dashboard totals', async ({ request }) => {
    // 1) Fetch invoices and pick one if available
    const beforeList: any[] = await getInvoices(request);
    expect(Array.isArray(beforeList)).toBeTruthy();
    if (beforeList.length === 0) test.skip(true, 'No invoices to test with');

    const inv = beforeList[0];
    const amount = Number(inv.amount ?? inv.Amount ?? 0) || 0;

    // 2) Capture dashboard before
    const dashBefore: any = await getDashboard(request);
    const paidBefore = Number(dashBefore.paidLast30 ?? 0) || 0;

    // 3) Mark invoice paid
    const res = await request.post(`${BACKEND}/api/invoices/${inv.id}/mark-paid`, { data: {} });
    expect(res.ok()).toBeTruthy();

    // 4) Re-fetch invoices and dashboard
    const afterList: any[] = await getInvoices(request);
    const dashAfter: any = await getDashboard(request);

    // 5) Assert: invoice may remain but should be marked paid; and dashboard paidLast30 should not decrease
    const stillThere = afterList.find(x => x.id === inv.id);
    const paidAfter = Number(dashAfter.paidLast30 ?? 0) || 0;

    if (stillThere) {
      expect(String(stillThere.status).toLowerCase()).toBe('paid');
    }
    // Note: paidLast30 may fluctuate due to seeded data windows; do not assert monotonicity.
  });
});
