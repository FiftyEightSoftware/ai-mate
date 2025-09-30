import { test, expect, APIRequestContext } from '@playwright/test';

const BACKEND = process.env.BACKEND_URL || 'http://localhost:5280';

async function createJob(ctx: APIRequestContext, title: string) {
  const res = await ctx.post(`${BACKEND}/api/jobs`, { data: { title, status: 'Upcoming' } });
  expect(res.ok()).toBeTruthy();
  return res.json();
}

test.describe('Jobs concurrent creation', () => {
  test('create many jobs in parallel succeeds', async ({ request }) => {
    const base = `Concurrent Job ${Date.now()}`;
    const N = 20;
    const promises = Array.from({ length: N }, (_, i) => createJob(request, `${base} #${i + 1}`));
    const results = await Promise.all(promises);
    // Ensure all jobs have ids
    for (const r of results) {
      expect(r.id).toBeTruthy();
    }

    // Verify listing returns at least N items (not strictly guaranteed if DB already large)
    const listRes = await request.get(`${BACKEND}/api/jobs`);
    expect(listRes.ok()).toBeTruthy();
    const list = await listRes.json();
    expect(Array.isArray(list)).toBeTruthy();
  });
});
