const { chromium } = require('playwright');

(async () => {
  // Launch browser with dev tools open
  const browser = await chromium.launch({
    devtools: true,
    headless: false
  });
  
  const context = await browser.newContext();
  const page = await context.newPage();
  
  // Navigate to your local Blazor app
  await page.goto('http://localhost:5055');
  
  // Wait for dev tools to be fully loaded
  await page.waitForTimeout(5000);
  
  // Take screenshot of the entire window including dev tools
  await page.screenshot({ path: 'devtools-screenshot.png', fullPage: true });
  
  await browser.close();
})();
