// Optional visual check of the browser mockup, not a Unity or gameplay test.
// Requires Playwright plus an installed Edge browser; never opens a visible window.
const { chromium } = require('playwright');
const { pathToFileURL } = require('node:url');
const path = require('node:path');
const fs = require('node:fs');
(async () => {
  const repo = path.resolve(__dirname, '..');
  const output = path.join(repo, '.local', 'art-review');
  fs.mkdirSync(output, { recursive: true });
  const browser = await chromium.launch({ channel: 'msedge', headless: true });
  try {
    const page = await browser.newPage({ viewport: { width: 1120, height: 800 }, deviceScaleFactor: 1 });
    const errors = []; page.on('pageerror', e => errors.push(e.message));
    await page.goto(pathToFileURL(path.join(repo, 'assets/phobos-autonav/previews/instruments.html')).href);
    const assert = (condition, message) => { if (!condition) throw new Error(message); };
    await page.locator('#size').click();
    await page.locator('#arrival').click({ position: { x: 65, y: 35 } });
    assert(await page.locator('#km').innerText() === '2 km', 'Arrival dial changes value');
    await page.locator('#fly').click();
    assert(await page.locator('#arrival').isDisabled(), 'Active flight locks arrival');
    await page.selectOption('#state', 'coast');
    assert(await page.locator('#notice').innerText() === 'Translation idle', 'Coast label is explicit');
    await page.locator('#more').click();
    assert(await page.locator('#details').isVisible(), 'Details opens');
    assert(await page.locator('#details').evaluate(e => e.scrollHeight > e.clientHeight), 'Long details scroll');
    await page.locator('#details').evaluate(e => e.scrollTop = e.scrollHeight);
    assert(await page.locator('#details').evaluate(e => e.scrollTop > 0), 'Details content is reachable');
    await page.locator('#more').click();
    await page.selectOption('#state', 'saved');
    assert(await page.locator('#prop').isDisabled(), 'Old RCS profile cannot gain torch through selector');
    assert(await page.locator('#fly').innerText() === 'RESUME', 'Saved flight offers Resume');
    await page.selectOption('#state', 'burn');
    await page.locator('.panel').screenshot({ path: path.join(output, 'polaris-instruments-reference.png') });
    await page.locator('#size').click();
    await page.locator('.panel').screenshot({ path: path.join(output, 'polaris-instruments-enlarged.png') });
    assert(errors.length === 0, errors.join('\n'));
    console.log('PASS: instrument preview interactions and scroll. Screenshots: .local/art-review. Not an in-game test.');
  } finally { await browser.close(); }
})().catch(e => { console.error(e); process.exitCode = 1; });
