import { test, expect } from '@playwright/test';

const BACKEND = process.env.BACKEND_URL || 'http://localhost:5280';
const allowedOrigin = 'http://localhost:5173';
const disallowedOrigin = 'http://evil.example';

async function preflight(request: any, path: string, origin: string, method: string, headers: string = 'content-type') {
  const res = await request.fetch(`${BACKEND}${path}`, {
    method: 'OPTIONS',
    headers: {
      Origin: origin,
      'Access-Control-Request-Method': method,
      'Access-Control-Request-Headers': headers,
    },
  });
  return res;
}

test.describe('CORS preflight matrix', () => {
  const cases: Array<{ path: string; method: string; headers?: string }> = [
    { path: '/api/jobs', method: 'GET' },
    { path: '/api/jobs', method: 'POST' },
    { path: '/api/invoices', method: 'GET' },
    { path: '/api/payments', method: 'GET' },
    { path: '/api/import/invoices', method: 'POST' },
  ];

  for (const c of cases) {
    test(`${c.method} ${c.path} allowed origin`, async ({ request }) => {
      const res = await preflight(request, c.path, allowedOrigin, c.method, c.headers);
      expect(res.status()).toBeLessThan(400);
      const acao = res.headers()['access-control-allow-origin'] || res.headers().get?.('access-control-allow-origin');
      expect(acao === allowedOrigin || acao === '*').toBeTruthy();
    });

    test(`${c.method} ${c.path} disallowed origin`, async ({ request }) => {
      const res = await preflight(request, c.path, disallowedOrigin, c.method, c.headers);
      expect(res.status()).toBeLessThan(500);
      const acao = res.headers()['access-control-allow-origin'] || res.headers().get?.('access-control-allow-origin');
      expect(acao).toBeFalsy();
    });
  }
});
