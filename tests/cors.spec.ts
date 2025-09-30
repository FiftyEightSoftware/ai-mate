import { test, expect } from '@playwright/test';

const BACKEND = process.env.BACKEND_URL || 'http://localhost:5280';

async function preflight(request: any, path: string, origin: string, method: string = 'GET') {
  const res = await request.fetch(`${BACKEND}${path}`, {
    method: 'OPTIONS',
    headers: {
      Origin: origin,
      'Access-Control-Request-Method': method,
      'Access-Control-Request-Headers': 'content-type',
    },
  });
  return res;
}

test.describe('CORS preflight', () => {
  const allowedOrigin = 'http://localhost:5173';
  const disallowedOrigin = 'http://evil.example';

  test('allowed origin receives CORS headers', async ({ request }) => {
    const res = await preflight(request, '/api/jobs', allowedOrigin, 'GET');
    expect(res.status()).toBeLessThan(400);
    const acao = res.headers()['access-control-allow-origin'] || res.headers().get?.('access-control-allow-origin');
    expect(acao === allowedOrigin || acao === '*').toBeTruthy();
  });

  test('disallowed origin does not receive ACAO', async ({ request }) => {
    const res = await preflight(request, '/api/jobs', disallowedOrigin, 'GET');
    expect(res.status()).toBeLessThan(500); // may still be 200/204
    const acao = res.headers()['access-control-allow-origin'] || res.headers().get?.('access-control-allow-origin');
    expect(acao).toBeFalsy();
  });
});
