import { test, expect } from '@playwright/test';

/**
 * Integration tests for the onboarding/setup flow
 * 
 * Tests cover:
 * - First-time user redirection to onboarding
 * - Form validation (VAT and HMRC Gateway ID)
 * - Successful credential saving
 * - Skip functionality
 * - Preventing re-entry after completion
 */

test.describe('Onboarding Setup Flow', () => {
  // Clear localStorage before each test to simulate first-time user
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    
    // Clear onboarding completion flag
    await page.evaluate(() => {
      localStorage.removeItem('aimate_onboarding_completed_v1');
      localStorage.removeItem('aimate_vat_registration_id_v1');
      localStorage.removeItem('aimate_hmrc_gateway_id_v1');
    });
  });

  test('redirects first-time user to onboarding page', async ({ page }) => {
    // Navigate to home - should redirect to onboarding
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    
    // Should be redirected to onboarding
    await expect(page).toHaveURL(/\/onboarding/);
    
    // Verify onboarding content is visible
    await expect(page.getByText('Welcome to AI Mate')).toBeVisible();
    await expect(page.getByText('VAT Registration Number')).toBeVisible();
    await expect(page.getByText('Government Gateway HMRC ID')).toBeVisible();
  });

  test('displays onboarding form with all required elements', async ({ page }) => {
    await page.goto('/onboarding');
    await page.waitForLoadState('networkidle');

    // Check heading
    await expect(page.locator('#onboarding-heading')).toHaveText('Welcome to AI Mate');

    // Check VAT input field
    const vatInput = page.locator('#vat-input');
    await expect(vatInput).toBeVisible();
    await expect(vatInput).toHaveAttribute('placeholder', 'Enter your VAT registration number');

    // Check HMRC input field
    const hmrcInput = page.locator('#hmrc-input');
    await expect(hmrcInput).toBeVisible();
    await expect(hmrcInput).toHaveAttribute('placeholder', 'Enter your Government Gateway HMRC ID');

    // Check action buttons
    await expect(page.getByRole('button', { name: /Save & Continue/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /Skip for now/i })).toBeVisible();
  });

  test('validates VAT registration number format', async ({ page }) => {
    await page.goto('/onboarding');
    await page.waitForLoadState('networkidle');

    const vatInput = page.locator('#vat-input');
    
    // Enter invalid VAT number (too short)
    await vatInput.fill('GB123');
    await vatInput.blur();
    
    // Wait for validation error
    await page.waitForTimeout(500);
    
    // Click save button to trigger validation
    await page.getByRole('button', { name: /Save & Continue/i }).click();
    
    // Check if error appears (the page should show validation errors)
    const errorVisible = await page.locator('div[role="alert"]').isVisible().catch(() => false);
    
    // Either error is shown or input has aria-invalid
    const ariaInvalid = await vatInput.getAttribute('aria-invalid');
    expect(errorVisible || ariaInvalid === 'true').toBeTruthy();
  });

  test('validates HMRC Gateway ID format', async ({ page }) => {
    await page.goto('/onboarding');
    await page.waitForLoadState('networkidle');

    const hmrcInput = page.locator('#hmrc-input');
    
    // Enter invalid HMRC ID (too short)
    await hmrcInput.fill('ABC');
    await hmrcInput.blur();
    
    // Wait for validation
    await page.waitForTimeout(500);
    
    // Click save to trigger validation
    await page.getByRole('button', { name: /Save & Continue/i }).click();
    
    // Check for validation feedback
    const errorVisible = await page.locator('div[role="alert"]').isVisible().catch(() => false);
    const ariaInvalid = await hmrcInput.getAttribute('aria-invalid');
    
    expect(errorVisible || ariaInvalid === 'true').toBeTruthy();
  });

  test('successfully saves valid credentials and completes onboarding', async ({ page }) => {
    await page.goto('/onboarding');
    await page.waitForLoadState('networkidle');

    // Enter valid-looking VAT number (GB format with 9 digits)
    const vatInput = page.locator('#vat-input');
    await vatInput.fill('GB123456789');
    
    // Enter valid HMRC Gateway ID (6-20 alphanumeric)
    const hmrcInput = page.locator('#hmrc-input');
    await hmrcInput.fill('TESTUSER123');
    
    // Click Save & Continue
    await page.getByRole('button', { name: /Save & Continue/i }).click();
    
    // Wait for success message or navigation
    await page.waitForTimeout(1500);
    
    // Should navigate away from onboarding (to home)
    await expect(page).not.toHaveURL(/\/onboarding/);
    
    // Verify onboarding completion flag is set in localStorage
    const onboardingCompleted = await page.evaluate(() => {
      return localStorage.getItem('aimate_onboarding_completed_v1');
    });
    expect(onboardingCompleted).toBe('1');
    
    // Verify credentials were saved
    const savedVat = await page.evaluate(() => {
      return localStorage.getItem('aimate_vat_registration_id_v1');
    });
    expect(savedVat).toBeTruthy();
  });

  test('skip functionality completes onboarding without saving credentials', async ({ page }) => {
    await page.goto('/onboarding');
    await page.waitForLoadState('networkidle');

    // Click Skip for now button
    await page.getByRole('button', { name: /Skip for now/i }).click();
    
    // Wait for navigation
    await page.waitForTimeout(500);
    
    // Should navigate to home
    await expect(page).not.toHaveURL(/\/onboarding/);
    
    // Verify onboarding is marked complete even though skipped
    const onboardingCompleted = await page.evaluate(() => {
      return localStorage.getItem('aimate_onboarding_completed_v1');
    });
    expect(onboardingCompleted).toBe('1');
    
    // Verify credentials were NOT saved
    const savedVat = await page.evaluate(() => {
      return localStorage.getItem('aimate_vat_registration_id_v1');
    });
    expect(savedVat).toBeFalsy();
  });

  test('does not redirect to onboarding after completion', async ({ page }) => {
    // First complete onboarding
    await page.goto('/onboarding');
    await page.waitForLoadState('networkidle');
    
    await page.getByRole('button', { name: /Skip for now/i }).click();
    await page.waitForTimeout(500);
    
    // Now try to visit home page again
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    
    // Should stay on home page, not redirect to onboarding
    await expect(page).not.toHaveURL(/\/onboarding/);
    
    // Wait a bit to ensure no redirect happens
    await page.waitForTimeout(1000);
    await expect(page).not.toHaveURL(/\/onboarding/);
  });

  test('can manually revisit onboarding page after completion', async ({ page }) => {
    // Complete onboarding first
    await page.goto('/onboarding');
    await page.waitForLoadState('networkidle');
    
    await page.getByRole('button', { name: /Skip for now/i }).click();
    await page.waitForTimeout(500);
    
    // Now manually navigate to onboarding
    await page.goto('/onboarding');
    await page.waitForLoadState('networkidle');
    
    // Should be able to see the onboarding page
    await expect(page).toHaveURL(/\/onboarding/);
    await expect(page.getByText('Welcome to AI Mate')).toBeVisible();
  });

  test('preserves existing credentials when revisiting onboarding', async ({ page }) => {
    // Set up some existing credentials in localStorage
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    
    await page.evaluate(() => {
      localStorage.setItem('aimate_onboarding_completed_v1', '1');
      localStorage.setItem('aimate_vat_registration_id_v1', 'GB999888777');
      localStorage.setItem('aimate_hmrc_gateway_id_v1', 'EXISTINGID');
    });
    
    // Navigate to onboarding
    await page.goto('/onboarding');
    await page.waitForLoadState('networkidle');
    
    // Wait for component to load existing values
    await page.waitForTimeout(500);
    
    // Check if inputs are pre-filled with existing values
    const vatInput = page.locator('#vat-input');
    const hmrcInput = page.locator('#hmrc-input');
    
    const vatValue = await vatInput.inputValue();
    const hmrcValue = await hmrcInput.inputValue();
    
    expect(vatValue).toBe('GB999888777');
    expect(hmrcValue).toBe('EXISTINGID');
  });

  test('accessibility: form inputs have proper ARIA attributes', async ({ page }) => {
    await page.goto('/onboarding');
    await page.waitForLoadState('networkidle');

    const vatInput = page.locator('#vat-input');
    const hmrcInput = page.locator('#hmrc-input');

    // Check ARIA attributes on VAT input
    await expect(vatInput).toHaveAttribute('aria-describedby', 'vat-help');
    await expect(vatInput).toHaveAttribute('aria-required', 'false');
    
    // Check ARIA attributes on HMRC input
    await expect(hmrcInput).toHaveAttribute('aria-describedby', 'hmrc-help');
    await expect(hmrcInput).toHaveAttribute('aria-required', 'false');
    
    // Verify help text exists
    await expect(page.locator('#vat-help')).toBeVisible();
    await expect(page.locator('#hmrc-help')).toBeVisible();
  });

  test('form submission shows success message before navigation', async ({ page }) => {
    await page.goto('/onboarding');
    await page.waitForLoadState('networkidle');

    // Fill in valid credentials
    await page.locator('#vat-input').fill('GB123456789');
    await page.locator('#hmrc-input').fill('TESTUSER123');
    
    // Submit form
    await page.getByRole('button', { name: /Save & Continue/i }).click();
    
    // Wait for success message to appear
    await page.waitForTimeout(300);
    
    // Check if success message is visible (before navigation)
    const successMessage = page.getByText(/saved successfully/i);
    const isVisible = await successMessage.isVisible().catch(() => false);
    
    // Success message should appear (even briefly)
    // Note: It may disappear quickly due to navigation, so we use a short timeout
    expect(isVisible).toBeTruthy();
  });
});
