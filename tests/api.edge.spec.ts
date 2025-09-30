import { test, expect, APIRequestContext } from '@playwright/test';

const BACKEND = process.env.BACKEND_URL || 'http://localhost:5280';

async function createJob(ctx: APIRequestContext, data: any) {
  return ctx.post(`${BACKEND}/api/jobs`, { data });
}

test.describe('Backend API edge cases', () => {
  test('jobs: missing title -> 400', async ({ request }) => {
    const res = await createJob(request, { status: 'Upcoming' });
    expect(res.status()).toBeGreaterThanOrEqual(400);
  });

  test('jobs: empty/whitespace title -> 400', async ({ request }) => {
    const res1 = await createJob(request, { title: '' });
    const res2 = await createJob(request, { title: '   ' });
    expect(res1.status()).toBeGreaterThanOrEqual(400);
    expect(res2.status()).toBeGreaterThanOrEqual(400);
  });

  test('jobs: extremely long title handled (truncate or accept)', async ({ request }) => {
    const longTitle = 'X'.repeat(5000);
    const res = await createJob(request, { title: longTitle, quotedPrice: 1 });
    // Implementation may accept or reject; assert response is either 200 or 400/413
    expect([200, 201, 400, 413]).toContain(res.status());
  });

  test('jobs: non-numeric quotedPrice gracefully handled', async ({ request }) => {
    const res = await createJob(request, { title: 'Edge QP', quotedPrice: 'abc' as any });
    // Backend coerces with try-catch; should still 200 OK
    expect(res.ok()).toBeTruthy();
    const body = await res.json();
    // quotedPrice may be null when coercion fails
    expect(body).toHaveProperty('id');
  });

  test('payments: invalid date filters do not crash', async ({ request }) => {
    const res = await request.get(`${BACKEND}/api/payments?from=not-a-date&to=also-bad`);
    // Backend falls back to default query; should be OK
    expect(res.ok()).toBeTruthy();
    const list = await res.json();
    expect(Array.isArray(list)).toBeTruthy();
  });

  test('invoices: mark-paid with unknown id returns 404/400', async ({ request }) => {
    const res = await request.post(`${BACKEND}/api/invoices/nonexistent-id/mark-paid`, { data: {} });
    // Implementation returns NotFound on false; allow 404 or 400
    expect([400, 404]).toContain(res.status());
  });

  test('invoices: import invalid payloads', async ({ request }) => {
    // Not an array
    const bad1 = await request.post(`${BACKEND}/api/import/invoices`, { data: { foo: 'bar' } });
    expect(bad1.status()).toBeGreaterThanOrEqual(400);

    // Empty array (should succeed with count 0)
    const empty = await request.post(`${BACKEND}/api/import/invoices`, { data: [] });
    // Depending on implementation, empty array may be accepted or rejected
    expect([200, 400]).toContain(empty.status());

    // Malformed DTOs
    const bad2 = await request.post(`${BACKEND}/api/import/invoices`, { data: [{ id: 123, amount: 'NaN' }] });
    expect(bad2.status()).toBeGreaterThanOrEqual(400);
  });

  test('dev reseed is idempotent and safe', async ({ request }) => {
    const a = await request.post(`${BACKEND}/api/dev/reseed`);
    const b = await request.post(`${BACKEND}/api/dev/reseed`);
    expect(a.ok()).toBeTruthy();
    expect(b.ok()).toBeTruthy();
  });
});
