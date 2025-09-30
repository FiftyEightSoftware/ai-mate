import { test, expect } from '@playwright/test';

const BACKEND = process.env.BACKEND_URL || 'http://localhost:5280';

test.describe('Voice endpoints', () => {
  test('enroll and verify flow', async ({ request }) => {
    const sampleDataUrl = 'data:audio/wav;base64,UklGRiQAAABXQVZFZm10IBAAAAABAAEAESsAACJWAAACABAAZGF0YQ==';

    // Enroll
    const enroll = await request.post(`${BACKEND}/voice/enroll`, { data: { dataUrl: sampleDataUrl } });
    expect(enroll.ok()).toBeTruthy();
    const ej = await enroll.json();
    expect(ej.ok).toBeTruthy();
    expect(ej.count).toBeGreaterThan(0);

    // Verify
    const verify = await request.post(`${BACKEND}/voice/verify`, { data: { dataUrl: sampleDataUrl } });
    expect(verify.ok()).toBeTruthy();
    const vj = await verify.json();
    expect(vj).toHaveProperty('ok');
    expect(typeof vj.score).toBe('number');
  });
});
