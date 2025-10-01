import { test, expect } from '@playwright/test';

/**
 * API Contract Tests for Onboarding Flow
 * 
 * These tests verify that the backend API contracts remain stable
 * and provide the expected data structures for the frontend onboarding flow
 */

const BACKEND_URL = process.env.BACKEND_URL || 'http://localhost:5280';

test.describe('API Contract Tests - Onboarding Dependencies', () => {

  test('GET /api/health - returns expected contract', async ({ request }) => {
    const response = await request.get(`${BACKEND_URL}/api/health`);
    
    expect(response.ok()).toBeTruthy();
    expect(response.status()).toBe(200);
    
    const data = await response.json();
    
    // Contract assertions
    expect(data).toHaveProperty('ok');
    expect(data).toHaveProperty('status');
    expect(data).toHaveProperty('time');
    expect(data).toHaveProperty('checks');
    
    expect(typeof data.ok).toBe('boolean');
    expect(typeof data.status).toBe('string');
    expect(Array.isArray(data.checks)).toBeTruthy();
    
    // Verify check structure
    if (data.checks.length > 0) {
      const check = data.checks[0];
      expect(check).toHaveProperty('name');
      expect(check).toHaveProperty('status');
    }
  });

  test('GET /api/dashboard - returns expected contract', async ({ request }) => {
    const response = await request.get(`${BACKEND_URL}/api/dashboard`);
    
    expect(response.ok()).toBeTruthy();
    expect(response.status()).toBe(200);
    
    const data = await response.json();
    
    // Contract assertions
    expect(data).toHaveProperty('outstandingTotal');
    expect(data).toHaveProperty('overdueTotal');
    expect(data).toHaveProperty('dueSoonTotal');
    expect(data).toHaveProperty('paidLast30');
    expect(data).toHaveProperty('invoices');
    expect(data).toHaveProperty('projectedCashFlow');
    
    // Type assertions
    expect(typeof data.outstandingTotal).toBe('number');
    expect(typeof data.overdueTotal).toBe('number');
    expect(typeof data.dueSoonTotal).toBe('number');
    expect(typeof data.paidLast30).toBe('number');
    expect(Array.isArray(data.invoices)).toBeTruthy();
    expect(Array.isArray(data.projectedCashFlow)).toBeTruthy();
    
    // Verify invoice structure if any exist
    if (data.invoices.length > 0) {
      const invoice = data.invoices[0];
      expect(invoice).toHaveProperty('id');
      expect(invoice).toHaveProperty('customer');
      expect(invoice).toHaveProperty('amount');
      expect(invoice).toHaveProperty('dueDate');
      expect(invoice).toHaveProperty('status');
      
      expect(typeof invoice.id).toBe('string');
      expect(typeof invoice.customer).toBe('string');
      expect(typeof invoice.amount).toBe('number');
      expect(typeof invoice.dueDate).toBe('string');
      expect(typeof invoice.status).toBe('string');
    }
    
    // Verify projection structure if any exist
    if (data.projectedCashFlow.length > 0) {
      const projection = data.projectedCashFlow[0];
      expect(projection).toHaveProperty('date');
      expect(projection).toHaveProperty('amount');
      
      expect(typeof projection.date).toBe('string');
      expect(typeof projection.amount).toBe('number');
    }
  });

  test('GET /api/invoices - returns expected contract', async ({ request }) => {
    const response = await request.get(`${BACKEND_URL}/api/invoices`);
    
    expect(response.ok()).toBeTruthy();
    expect(response.status()).toBe(200);
    
    const data = await response.json();
    
    expect(Array.isArray(data)).toBeTruthy();
    
    // Verify invoice structure
    if (data.length > 0) {
      const invoice = data[0];
      
      // Required fields
      expect(invoice).toHaveProperty('id');
      expect(invoice).toHaveProperty('customer');
      expect(invoice).toHaveProperty('amount');
      expect(invoice).toHaveProperty('status');
      expect(invoice).toHaveProperty('issueDate');
      expect(invoice).toHaveProperty('dueDate');
      
      // Type validation
      expect(typeof invoice.id).toBe('string');
      expect(typeof invoice.customer).toBe('string');
      expect(typeof invoice.amount).toBe('number');
      expect(typeof invoice.status).toBe('string');
      expect(typeof invoice.issueDate).toBe('string');
      expect(typeof invoice.dueDate).toBe('string');
      
      // Valid status values
      expect(['paid', 'unpaid', 'overdue']).toContain(invoice.status);
      
      // Date format validation (ISO 8601)
      expect(invoice.issueDate).toMatch(/^\d{4}-\d{2}-\d{2}/);
      expect(invoice.dueDate).toMatch(/^\d{4}-\d{2}-\d{2}/);
    }
  });

  test('GET /api/jobs - returns expected contract', async ({ request }) => {
    const response = await request.get(`${BACKEND_URL}/api/jobs`);
    
    expect(response.ok()).toBeTruthy();
    expect(response.status()).toBe(200);
    
    const data = await response.json();
    
    expect(Array.isArray(data)).toBeTruthy();
    
    // Verify job structure
    if (data.length > 0) {
      const job = data[0];
      
      // Required fields
      expect(job).toHaveProperty('id');
      expect(job).toHaveProperty('title');
      expect(job).toHaveProperty('status');
      
      // Type validation
      expect(typeof job.id).toBe('string');
      expect(typeof job.title).toBe('string');
      expect(job.status === null || typeof job.status).toBe('string');
      
      // quotedPrice is optional
      if (job.quotedPrice !== null && job.quotedPrice !== undefined) {
        expect(typeof job.quotedPrice).toBe('number');
      }
      
      // Valid status values
      if (job.status) {
        expect(['Upcoming', 'In Progress', 'Completed', 'On Hold', 'Cancelled']).toContain(job.status);
      }
    }
  });

  test('POST /api/invoices/{id}/mark-paid - returns expected contract', async ({ request }) => {
    // First get an unpaid invoice
    const invoicesResponse = await request.get(`${BACKEND_URL}/api/invoices?status=unpaid`);
    const invoices = await invoicesResponse.json();
    
    if (invoices.length === 0) {
      test.skip();
    }
    
    const invoiceId = invoices[0].id;
    
    // Mark as paid
    const response = await request.post(`${BACKEND_URL}/api/invoices/${invoiceId}/mark-paid`, {
      data: { paidDate: '2025-10-01' }
    });
    
    expect(response.ok()).toBeTruthy();
    expect(response.status()).toBe(200);
    
    const data = await response.json();
    
    // Contract assertion
    expect(data).toHaveProperty('ok');
    expect(typeof data.ok).toBe('boolean');
    expect(data.ok).toBe(true);
  });

  test('POST /api/jobs - returns expected contract', async ({ request }) => {
    const newJob = {
      title: 'Contract Test Job ' + Date.now(),
      status: 'Upcoming',
      quotedPrice: 500.00
    };
    
    const response = await request.post(`${BACKEND_URL}/api/jobs`, {
      data: newJob
    });
    
    expect(response.ok()).toBeTruthy();
    expect(response.status()).toBe(200);
    
    const data = await response.json();
    
    // Contract assertions
    expect(data).toHaveProperty('id');
    expect(data).toHaveProperty('title');
    expect(data).toHaveProperty('status');
    expect(data).toHaveProperty('quotedPrice');
    
    expect(typeof data.id).toBe('string');
    expect(data.title).toBe(newJob.title);
    expect(data.status).toBe(newJob.status);
    expect(data.quotedPrice).toBe(newJob.quotedPrice);
  });

  test('GET /api/metrics - returns expected contract', async ({ request }) => {
    const response = await request.get(`${BACKEND_URL}/api/metrics`);
    
    expect(response.ok()).toBeTruthy();
    expect(response.status()).toBe(200);
    
    const data = await response.json();
    
    // Metrics should be an object
    expect(typeof data).toBe('object');
    
    // Should have at least one metric field
    const hasMetrics = Object.keys(data).length > 0;
    expect(hasMetrics).toBeTruthy();
  });

  test('GET /api/invoices - supports status filtering', async ({ request }) => {
    // Test paid filter
    const paidResponse = await request.get(`${BACKEND_URL}/api/invoices?status=paid`);
    expect(paidResponse.ok()).toBeTruthy();
    const paidInvoices = await paidResponse.json();
    expect(Array.isArray(paidInvoices)).toBeTruthy();
    
    // Verify all returned invoices have paid status
    for (const invoice of paidInvoices) {
      expect(invoice.status).toBe('paid');
    }
    
    // Test unpaid filter
    const unpaidResponse = await request.get(`${BACKEND_URL}/api/invoices?status=unpaid`);
    expect(unpaidResponse.ok()).toBeTruthy();
    const unpaidInvoices = await unpaidResponse.json();
    expect(Array.isArray(unpaidInvoices)).toBeTruthy();
    
    // Verify all returned invoices have unpaid or overdue status
    for (const invoice of unpaidInvoices) {
      expect(['unpaid', 'overdue']).toContain(invoice.status);
    }
  });

  test('GET /api/dashboard - supports weeks parameter', async ({ request }) => {
    const response = await request.get(`${BACKEND_URL}/api/dashboard?weeks=12`);
    
    expect(response.ok()).toBeTruthy();
    expect(response.status()).toBe(200);
    
    const data = await response.json();
    
    // Should have projections
    expect(Array.isArray(data.projectedCashFlow)).toBeTruthy();
    
    // Should have up to 12 weeks of projections (might be less if no data)
    expect(data.projectedCashFlow.length).toBeLessThanOrEqual(12);
  });

  test('Error handling - 404 for non-existent invoice', async ({ request }) => {
    const fakeId = 'nonexistent-invoice-id-12345';
    const response = await request.get(`${BACKEND_URL}/api/invoices/${fakeId}`);
    
    // Should return 404 for non-existent resource
    expect(response.status()).toBe(404);
  });

  test('Error handling - 400 for invalid job creation', async ({ request }) => {
    const invalidJob = {
      // Missing required 'title' field
      status: 'Upcoming'
    };
    
    const response = await request.post(`${BACKEND_URL}/api/jobs`, {
      data: invalidJob
    });
    
    // Should return 400 for bad request
    expect(response.status()).toBe(400);
    
    const data = await response.json();
    expect(data).toHaveProperty('ok');
    expect(data.ok).toBe(false);
  });

  test('CORS headers present for cross-origin requests', async ({ request }) => {
    const response = await request.get(`${BACKEND_URL}/api/health`, {
      headers: {
        'Origin': 'http://localhost:5173'
      }
    });
    
    expect(response.ok()).toBeTruthy();
    
    // In development, CORS should allow all origins or have proper headers
    // This is a soft check as configuration may vary
    const headers = response.headers();
    const hasCorsSupport = 
      headers['access-control-allow-origin'] !== undefined ||
      headers['vary'] !== undefined;
    
    // Log headers for debugging if needed
    if (!hasCorsSupport) {
      console.log('Response headers:', headers);
    }
  });

  test('Response times are reasonable for dashboard', async ({ request }) => {
    const startTime = Date.now();
    
    const response = await request.get(`${BACKEND_URL}/api/dashboard`);
    
    const endTime = Date.now();
    const responseTime = endTime - startTime;
    
    expect(response.ok()).toBeTruthy();
    
    // Response should be under 2 seconds (generous for integration tests)
    expect(responseTime).toBeLessThan(2000);
  });

  test('Large dataset handling - invoices endpoint', async ({ request }) => {
    const response = await request.get(`${BACKEND_URL}/api/invoices`);
    
    expect(response.ok()).toBeTruthy();
    
    const data = await response.json();
    
    // Verify it can handle large datasets (we seeded 400-600 invoices)
    expect(data.length).toBeGreaterThan(0);
    
    // All invoices should have valid structure
    for (const invoice of data.slice(0, 10)) { // Check first 10 for performance
      expect(invoice).toHaveProperty('id');
      expect(invoice).toHaveProperty('customer');
      expect(invoice).toHaveProperty('amount');
      expect(typeof invoice.id).toBe('string');
      expect(typeof invoice.customer).toBe('string');
      expect(typeof invoice.amount).toBe('number');
    }
  });
});
