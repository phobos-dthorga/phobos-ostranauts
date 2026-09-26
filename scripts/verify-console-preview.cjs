// A browser reference check, not a Unity or native-input test.
const { chromium } = require('playwright');
const fs = require('fs'), path = require('path');
(async () => {
  const root = path.resolve(__dirname, '..'), out = path.join(root, '.local/art-review');
  fs.mkdirSync(out, { recursive: true });
  const browser = await chromium.launch({ channel: 'msedge', headless: true });
  try {
    for (const [width, height, scale] of [[1920,1080,1], [2560,1440,1], [3440,1440,1], [1920,1080,1.5], [1280,720,1.25]]) {
      const page = await browser.newPage({ viewport: { width, height } });
      await page.goto(require('url').pathToFileURL(path.join(root, 'assets/phobos-industrial-console/console-preview.html')).href);
      await page.evaluate(s => document.documentElement.style.setProperty('--scale', s), scale);
      await page.screenshot({ path: path.join(out, `console-${width}-${height}-${scale}.png`) });
      const result = await page.evaluate(() => {
        const main = document.querySelector('main'), footer = document.querySelector('footer');
        const list = document.querySelector('aside'), detail = document.querySelector('article');
        const frame = main.getBoundingClientRect(), action = footer.getBoundingClientRect();
        const visible = getComputedStyle(list).display !== 'none', errors = [];
        if (action.bottom > frame.bottom || action.top <= frame.top || document.body.scrollWidth > innerWidth)
          errors.push('Frame/action bounds');
        if (visible) {
          list.scrollTop = 150;
          if (list.scrollTop === 0 || detail.scrollTop !== 0) errors.push('Independent equipment scrolling');
        } else if (getComputedStyle(document.querySelector('.back')).display === 'none') errors.push('Narrow back navigation');
        const priorList = list.scrollTop, extra = document.createElement('div');
        extra.style.height = '1600px'; detail.append(extra); detail.scrollTop = 200;
        if (detail.scrollTop === 0 || list.scrollTop !== priorList || footer.getBoundingClientRect().top !== action.top)
          errors.push('Independent detail scrolling/fixed actions');
        extra.remove(); detail.scrollTop = 0;
        document.querySelector('h2').textContent = "Phobos' Verdemorrow Groundwork B2 — Arbeitsstation zur Wiederaufbereitung charakterisierter Pflanzenrückstände";
        for (const item of document.querySelectorAll('.equipment'))
          item.firstChild.textContent = 'Duplicate store — Materialvorrat mit einem außergewöhnlich langen Anzeigenamen';
        if (main.scrollWidth > main.clientWidth || detail.scrollWidth > detail.clientWidth) errors.push('Long names overflow');
        return errors;
      });
      if (result.length) throw Error(`${width}×${height} @ ${scale}: ${result.join(', ')}`);
      await page.screenshot({ path: path.join(out, `console-long-${width}-${height}-${scale}.png`) });
      await page.close();
    }
    console.log('PASS: five browser reference sizes/scales, independent scrolling, fixed actions, narrow navigation and long duplicate names. Not Unity validation.');
  } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
