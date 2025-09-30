import { test, expect, APIRequestContext } from '@playwright/test';

const BACKEND = process.env.BACKEND_URL || 'http://localhost:5280';

async function waitForBackendReady(ctx: APIRequestContext) {
  const deadline = Date.now() + 60_000;
  let lastErr: any;
  while (Date.now() < deadline) {
    try {
      const res = await ctx.get(`${BACKEND}/api/health`);
      if (res.ok()) return;
      lastErr = await res.text();
    } catch (e) {
      lastErr = e;
    }
    await new Promise((r) => setTimeout(r, 1000));
  }
  throw new Error(`Backend not ready: ${lastErr}`);
}

// Helper to create a job
async function createJob(ctx: APIRequestContext, title: string, status?: string, quotedPrice?: number) {
  const res = await ctx.post(`${BACKEND}/api/jobs`, {
    data: { title, status, quotedPrice },
  });
  expect(res.ok()).toBeTruthy();
  return res.json();
}

test.describe('Backend API', () => {
  test.beforeAll(async ({ request }) => {
    await waitForBackendReady(request);
  });
  test('health endpoint responds', async ({ request }) => {
    const res = await request.get(`${BACKEND}/api/health`);
    expect(res.ok()).toBeTruthy();
    const body = await res.json();
    expect(body.ok).toBeTruthy();
    expect(body.time).toBeTruthy();
  });

  test('jobs list and create', async ({ request }) => {
    // List before
    const listBefore = await request.get(`${BACKEND}/api/jobs`);
    expect(listBefore.ok()).toBeTruthy();
    const before = await listBefore.json();
    expect(Array.isArray(before)).toBeTruthy();

    // Create
    const title = `E2E Job ${Date.now()}`;
    const created = await createJob(request, title, 'Upcoming', 123.45);
    expect(created.id).toBeTruthy();
    expect(created.title).toBe(title);

    // List after
    const listAfter = await request.get(`${BACKEND}/api/jobs`);
    expect(listAfter.ok()).toBeTruthy();
    const after = await listAfter.json();
    expect(after.length).toBeGreaterThan(0);
  });

  test('dashboard aggregates', async ({ request }) => {
    const res = await request.get(`${BACKEND}/api/dashboard?weeks=8`);
    expect(res.ok()).toBeTruthy();
    const json = await res.json();
    // Accept either of the two known response shapes
    const hasOldShape = 'outstanding' in json && 'overdue' in json && 'dueSoon' in json && 'paidLast30' in json && 'projected' in json;
    const hasNewShape = 'outstandingTotal' in json && 'overdueTotal' in json && 'dueSoonTotal' in json && 'paidLast30' in json && 'projectedCashFlow' in json;
    expect(hasOldShape || hasNewShape).toBeTruthy();
    expect(json).toHaveProperty('invoices');
  });

  test('invoices list, import invalid payload error', async ({ request }) => {
    const inv = await request.get(`${BACKEND}/api/invoices`);
    expect(inv.ok()).toBeTruthy();
    const list = await inv.json();
    expect(Array.isArray(list)).toBeTruthy();

    const bad = await request.post(`${BACKEND}/api/import/invoices`, { data: { foo: 'bar' } });
    expect(bad.status()).toBeGreaterThanOrEqual(400);
  });

  test('payments list optionally filtered', async ({ request }) => {
    const res = await request.get(`${BACKEND}/api/payments`);
    expect(res.ok()).toBeTruthy();
    const all = await res.json();
    expect(Array.isArray(all)).toBeTruthy();

    const res2 = await request.get(`${BACKEND}/api/payments?from=2000-01-01&to=2099-12-31`);
    expect(res2.ok()).toBeTruthy();
    const filtered = await res2.json();
    expect(Array.isArray(filtered)).toBeTruthy();
  });

  test('dev reseed endpoint', async ({ request }) => {
    const reseed = await request.post(`${BACKEND}/api/dev/reseed`);
    expect(reseed.ok()).toBeTruthy();
    const json = await reseed.json();
    expect(json.ok).toBeTruthy();
  });
});
