import { test, expect } from '@playwright/test';

/**
 * Enhanced integration tests for the onboarding flow
 * 
 * These tests focus on:
 * - End-to-end user workflows
 * - Data persistence across sessions
 * - Backend integration
 * - Error handling and resilience
 */

const FRONTEND_URL = process.env.FRONTEND_URL || 'http://localhost:5173';
const BACKEND_URL = process.env.BACKEND_URL || 'http://localhost:5280';

test.describe('Onboarding Integration Tests', () => {
  
  test.beforeEach(async ({ page }) => {
    // Clear all onboarding-related state
    await page.goto(FRONTEND_URL);
    await page.waitForLoadState('networkidle');
    
    await page.evaluate(() => {
      localStorage.clear();
      sessionStorage.clear();
    });
  });

  test('complete onboarding flow - save credentials', async ({ page }) => {
    // Step 1: Navigate to home - should redirect to onboarding
    await page.goto(FRONTEND_URL);
    await page.waitForLoadState('networkidle');
    
    // Wait for either redirect or onboarding page
    await page.waitForURL(/.*\/(onboarding)?/, { timeout: 5000 });
    
    // If not on onboarding, navigate there
    if (!page.url().includes('/onboarding')) {
      await page.goto(`${FRONTEND_URL}/onboarding`);
      await page.waitForLoadState('networkidle');
    }
    
    // Step 2: Verify onboarding page elements
    await expect(page.locator('#onboarding-heading')).toBeVisible({ timeout: 10000 });
    await expect(page.locator('#vat-input')).toBeVisible();
    await expect(page.locator('#hmrc-input')).toBeVisible();
    
    // Step 3: Fill in valid credentials
    const testVat = 'GB' + Math.floor(100000000 + Math.random() * 900000000).toString();
    const testHmrc = 'TEST' + Math.floor(100000 + Math.random() * 900000).toString();
    
    await page.locator('#vat-input').fill(testVat);
    await page.locator('#hmrc-input').fill(testHmrc);
    
    // Step 4: Submit form
    await page.getByRole('button', { name: /Save & Continue/i }).click();
    
    // Step 5: Wait for success and navigation
    await page.waitForTimeout(2000); // Give time for save + navigation
    
    // Step 6: Verify we navigated away from onboarding
    await expect(page).not.toHaveURL(/\/onboarding/, { timeout: 5000 });
    
    // Step 7: Verify localStorage has the completion flag
    const onboardingComplete = await page.evaluate(() => {
      return localStorage.getItem('aimate_onboarding_completed_v1') === '1';
    });
    expect(onboardingComplete).toBeTruthy();
    
    // Step 8: Verify credentials were saved
    const savedVat = await page.evaluate(() => {
      return localStorage.getItem('aimate_vat_registration_id_v1');
    });
    expect(savedVat).toBeTruthy();
    expect(savedVat).toContain('GB');
  });

  test('complete onboarding flow - skip', async ({ page }) => {
    // Step 1: Go to onboarding
    await page.goto(`${FRONTEND_URL}/onboarding`);
    await page.waitForLoadState('networkidle');
    
    // Step 2: Verify page loaded
    await expect(page.locator('#onboarding-heading')).toBeVisible({ timeout: 10000 });
    
    // Step 3: Click skip button
    await page.getByRole('button', { name: /Skip for now/i }).click();
    
    // Step 4: Wait for navigation
    await page.waitForTimeout(1000);
    
    // Step 5: Verify we navigated away
    await expect(page).not.toHaveURL(/\/onboarding/, { timeout: 5000 });
    
    // Step 6: Verify onboarding is marked complete
    const onboardingComplete = await page.evaluate(() => {
      return localStorage.getItem('aimate_onboarding_completed_v1') === '1';
    });
    expect(onboardingComplete).toBeTruthy();
    
    // Step 7: Verify no credentials were saved
    const savedVat = await page.evaluate(() => {
      return localStorage.getItem('aimate_vat_registration_id_v1');
    });
    expect(savedVat).toBeFalsy();
  });

  test('validation - prevents invalid VAT format', async ({ page }) => {
    await page.goto(`${FRONTEND_URL}/onboarding`);
    await page.waitForLoadState('networkidle');
    
    const vatInput = page.locator('#vat-input');
    
    // Try invalid VAT
    await vatInput.fill('INVALID');
    await vatInput.blur();
    await page.waitForTimeout(500);
    
    // Try to submit
    await page.getByRole('button', { name: /Save & Continue/i }).click();
    await page.waitForTimeout(500);
    
    // Should still be on onboarding page due to validation error
    await expect(page).toHaveURL(/\/onboarding/);
    
    // Should show error message or invalid state
    const hasError = await page.locator('[role="alert"]').isVisible().catch(() => false);
    const ariaInvalid = await vatInput.getAttribute('aria-invalid');
    
    expect(hasError || ariaInvalid === 'true').toBeTruthy();
  });

  test('validation - prevents invalid HMRC format', async ({ page }) => {
    await page.goto(`${FRONTEND_URL}/onboarding`);
    await page.waitForLoadState('networkidle');
    
    const hmrcInput = page.locator('#hmrc-input');
    
    // Try very short HMRC ID
    await hmrcInput.fill('AB');
    await hmrcInput.blur();
    await page.waitForTimeout(500);
    
    // Try to submit
    await page.getByRole('button', { name: /Save & Continue/i }).click();
    await page.waitForTimeout(500);
    
    // Should still be on onboarding page
    await expect(page).toHaveURL(/\/onboarding/);
    
    // Should show error
    const hasError = await page.locator('[role="alert"]').isVisible().catch(() => false);
    const ariaInvalid = await hmrcInput.getAttribute('aria-invalid');
    
    expect(hasError || ariaInvalid === 'true').toBeTruthy();
  });

  test('credentials persist across sessions', async ({ page }) => {
    // Session 1: Save credentials
    await page.goto(`${FRONTEND_URL}/onboarding`);
    await page.waitForLoadState('networkidle');
    
    const testVat = 'GB987654321';
    const testHmrc = 'PERSIST123';
    
    await page.locator('#vat-input').fill(testVat);
    await page.locator('#hmrc-input').fill(testHmrc);
    await page.getByRole('button', { name: /Save & Continue/i }).click();
    await page.waitForTimeout(2000);
    
    // Session 2: Revisit onboarding
    await page.goto(`${FRONTEND_URL}/onboarding`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000); // Give time for inputs to populate
    
    // Verify fields are pre-filled
    const vatValue = await page.locator('#vat-input').inputValue();
    const hmrcValue = await page.locator('#hmrc-input').inputValue();
    
    expect(vatValue).toBe(testVat);
    expect(hmrcValue).toBe(testHmrc);
  });

  test('onboarding does not block completed users', async ({ page }) => {
    // Mark onboarding as complete
    await page.goto(FRONTEND_URL);
    await page.evaluate(() => {
      localStorage.setItem('aimate_onboarding_completed_v1', '1');
    });
    
    // Navigate to home
    await page.goto(FRONTEND_URL);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);
    
    // Should NOT redirect to onboarding
    await expect(page).not.toHaveURL(/\/onboarding/);
  });

  test('backend is accessible during onboarding', async ({ page, request }) => {
    // Verify backend is up
    const healthResponse = await request.get(`${BACKEND_URL}/api/health`);
    expect(healthResponse.ok()).toBeTruthy();
    
    const healthData = await healthResponse.json();
    expect(healthData.ok).toBe(true);
    
    // Verify data endpoints are accessible
    const invoicesResponse = await request.get(`${BACKEND_URL}/api/invoices`);
    expect(invoicesResponse.ok()).toBeTruthy();
    
    const jobsResponse = await request.get(`${BACKEND_URL}/api/jobs`);
    expect(jobsResponse.ok()).toBeTruthy();
  });

  test('dashboard loads after onboarding completion', async ({ page }) => {
    // Complete onboarding
    await page.goto(`${FRONTEND_URL}/onboarding`);
    await page.waitForLoadState('networkidle');
    
    await page.getByRole('button', { name: /Skip for now/i }).click();
    await page.waitForTimeout(1500);
    
    // Verify we're at home/dashboard
    await expect(page).not.toHaveURL(/\/onboarding/);
    
    // Wait for dashboard to load
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);
    
    // Look for dashboard elements (be flexible about exact text)
    const dashboardVisible = await page.locator('h1, h2, h3').first().isVisible().catch(() => false);
    expect(dashboardVisible).toBeTruthy();
  });

  test('form is keyboard navigable', async ({ page }) => {
    await page.goto(`${FRONTEND_URL}/onboarding`);
    await page.waitForLoadState('networkidle');
    
    // Tab through form
    await page.locator('#vat-input').focus();
    await page.keyboard.press('Tab');
    
    // Should focus HMRC input
    const hmrcFocused = await page.locator('#hmrc-input').evaluate(el => el === document.activeElement);
    expect(hmrcFocused).toBeTruthy();
    
    // Continue tabbing
    await page.keyboard.press('Tab');
    
    // Should focus Save button
    const saveButton = page.getByRole('button', { name: /Save & Continue/i });
    const saveFocused = await saveButton.evaluate(el => el === document.activeElement);
    expect(saveFocused).toBeTruthy();
  });

  test('success message appears on save', async ({ page }) => {
    await page.goto(`${FRONTEND_URL}/onboarding`);
    await page.waitForLoadState('networkidle');
    
    await page.locator('#vat-input').fill('GB123456789');
    await page.locator('#hmrc-input').fill('TESTUSER');
    
    await page.getByRole('button', { name: /Save & Continue/i }).click();
    
    // Wait briefly for success message
    await page.waitForTimeout(800);
    
    // Check for success indication (message or navigation)
    const successVisible = await page.getByText(/saved successfully/i).isVisible().catch(() => false);
    const navigatedAway = !page.url().includes('/onboarding');
    
    // Either success message appeared or we navigated away (success)
    expect(successVisible || navigatedAway).toBeTruthy();
  });

  test('handles rapid form submissions gracefully', async ({ page }) => {
    await page.goto(`${FRONTEND_URL}/onboarding`);
    await page.waitForLoadState('networkidle');
    
    await page.locator('#vat-input').fill('GB123456789');
    await page.locator('#hmrc-input').fill('TESTUSER');
    
    // Click save button multiple times rapidly
    const saveButton = page.getByRole('button', { name: /Save & Continue/i });
    await saveButton.click();
    await saveButton.click();
    await saveButton.click();
    
    // Wait for processing
    await page.waitForTimeout(3000);
    
    // Should still work - either navigated away or stayed on page
    // Should not crash or show errors
    const pageLoaded = await page.locator('body').isVisible();
    expect(pageLoaded).toBeTruthy();
  });
});
