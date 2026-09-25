// Browser-only research mockup checks. This does not test Unity widgets or gameplay.
// Uses the same local Playwright/Edge approach as verify-autonav-preview.cjs.
const { chromium } = require('playwright');
const { pathToFileURL } = require('node:url');
const path = require('node:path');
const fs = require('node:fs');
(async () => {
  const root = path.resolve(__dirname, '..');
  const output = path.join(root, '.local', 'art-review', 'furnace');
  fs.mkdirSync(output, { recursive: true });
  const browser = await chromium.launch({ channel: 'msedge', headless: true });
  try {
    const page = await browser.newPage({ viewport: { width: 1440, height: 1080 } });
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    await page.goto(pathToFileURL(path.join(root, 'assets/phobos-furnace/research/layouts.html')).href);
    const check = (condition, message) => { if (!condition) throw new Error(message); };
    await page.screenshot({ path: path.join(output, 'installation.png'), fullPage: true });
    await page.locator('[data-view="full"]').click();
    await page.selectOption('#scenario', 'melt');
    check((await page.locator('#received').innerText()) === '125', 'Partial delivery displayed');
    await page.locator('#power').fill('100');
    await page.locator('#power').dispatchEvent('input');
    check((await page.locator('#request').innerText()) === '100', 'Requested heat changes');
    check((await page.locator('#received').innerText()) === '100', 'Sample delivery is bounded by request');
    await page.locator('#power').fill('250');
    await page.locator('#power').dispatchEvent('input');
    await page.screenshot({ path: path.join(output, 'full-panel.png'), fullPage: true });
    await page.selectOption('#scenario', 'unknown');
    check((await page.locator('#temp').innerText()) === 'Unknown', 'Unknown is not zero');
    check((await page.locator('#received').innerText()) === '0', 'Unavailable instrument isolates sample heat');
    check((await page.locator('#request').innerText()) === '0', 'Configured ceiling is not an active request');
    await page.locator('#guard').click();
    check(await page.locator('#isolate').isVisible(), 'Guard exposes isolation');
    await page.locator('#isolate').click();
    check((await page.locator('#phase').innerText()) === 'ISOLATED', 'Isolation feedback');
    await page.locator('#guard').click();
    await page.locator('[data-view="compact"]').click();
    await page.setViewportSize({ width: 800, height: 1050 });
    await page.selectOption('#scenario', 'blocked');
    await page.screenshot({ path: path.join(output, 'compact-panel.png'), fullPage: true });
    for (const width of [1440, 800, 360]) {
      await page.setViewportSize({ width, height: 900 });
      for (const view of ['installation', 'full', 'compact']) {
        await page.locator(`[data-view="${view}"]`).click();
        check(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), `${view} fits at ${width}`);
        if (view !== 'installation') {
          check(await page.evaluate(() => {
            const details = document.querySelector('.instruments');
            const alarm = document.querySelector('.alarm-strip');
            const footer = document.querySelector('.footer');
            const before = footer.getBoundingClientRect().top;
            details.scrollTop = details.scrollHeight;
            return details.getBoundingClientRect().bottom <= alarm.getBoundingClientRect().top + 1
              && alarm.getBoundingClientRect().bottom <= footer.getBoundingClientRect().top
              && Math.abs(footer.getBoundingClientRect().top - before) < 1;
          }), `${view} keeps readings, alarms and stop controls separate at ${width}`);
          await page.locator('.instruments').evaluate(el => { el.scrollTop = 0; });
        }
      }
    }
    await page.selectOption('#scenario', 'unknown');
    await page.screenshot({ path: path.join(output, 'narrow-panel.png'), fullPage: true });
    await page.locator('#stop').click();
    check((await page.locator('#phase').innerText()) === 'COOLING', 'Stop remains reachable at narrow width');
    check(errors.length === 0, errors.join('\n'));
    console.log('Furnace study: scenarios, guard, controls and 3 layouts at 3 widths passed. Screenshots are local research artifacts.');
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
