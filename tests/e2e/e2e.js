const { chromium, devices } = require('playwright');
const SHOTS = process.argv[2];
const BASE = 'http://localhost:8080/Workoutapp/';
const errors = [];
const step = (m) => console.log('✓ ' + m);

function seed() {
  const ex = (id, name, muscle, sets, type = 'Strength') => ({ exerciseId: id, name, muscle, type, sets: sets.map(([w, r]) => ({ weight: w, reps: r, durationSeconds: 0, distanceKm: 0, completed: true })) });
  const plans = {
    'Push Day': (k) => [ex('bench-press', 'Bench Press', 'Chest', [[70 + k * 2.5, 8], [70 + k * 2.5, 8], [72.5 + k * 2.5, 6]]), ex('overhead-press', 'Overhead Press', 'Shoulders', [[42.5 + k, 8], [42.5 + k, 7]]), ex('triceps-pushdown', 'Triceps Pushdown', 'Triceps', [[25, 12], [25, 12]])],
    'Pull Day': (k) => [ex('deadlift', 'Deadlift', 'Back', [[120 + k * 5, 5], [120 + k * 5, 5]]), ex('pull-up', 'Pull-Up', 'Back', [[0, 8 + Math.floor(k / 2)], [0, 7 + Math.floor(k / 2)]], 'Bodyweight'), ex('barbell-curl', 'Barbell Curl', 'Biceps', [[30, 10], [30, 9]])],
    'Leg Day': (k) => [ex('squat', 'Squat', 'Legs', [[100 + k * 5, 5], [100 + k * 5, 5], [100 + k * 5, 5]]), ex('romanian-deadlift', 'Romanian Deadlift', 'Legs', [[80 + k * 2.5, 8], [80 + k * 2.5, 8]]), ex('running', 'Running', 'Cardio', [[0, 0]], 'Cardio')],
  };
  const workouts = [];
  const now = new Date(); now.setHours(0, 0, 0, 0);
  const iso = (d) => new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString().slice(0, 19);
  let k = 0;
  for (let daysAgo = 52; daysAgo >= 1; daysAgo -= 2 + (daysAgo % 3 === 0 ? 1 : 0)) {
    const name = Object.keys(plans)[k % 3];
    const start = new Date(now); start.setDate(now.getDate() - daysAgo); start.setHours(18, 10);
    const end = new Date(start.getTime() + (55 + (k % 4) * 6) * 60000);
    const exercises = plans[name](Math.floor(k / 3));
    exercises.filter(e => e.type === 'Cardio').forEach(e => e.sets = [{ weight: 0, reps: 0, durationSeconds: 900 + k * 20, distanceKm: 2.5 + k * 0.05, completed: true }]);
    workouts.push({ id: crypto.randomUUID(), name, startedAt: iso(start), finishedAt: iso(end), exercises, notes: null });
    k++;
  }
  return JSON.stringify({ version: 1, workouts, customExercises: [], settings: { unit: 'Kg', defaultRestSeconds: 90, weeklyGoal: 4, theme: 'System' } });
}

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ ...devices['Pixel 7'], colorScheme: 'dark', serviceWorkers: 'allow' });
  const page = await ctx.newPage();
  page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
  page.on('pageerror', e => errors.push(e.message));
  const shot = (n) => page.screenshot({ path: `${SHOTS}/${n}.png` });

  // 1. Fresh install
  await page.goto(BASE);
  await page.getByRole('heading', { name: /Good|Late night/ }).waitFor({ timeout: 30000 });
  step('app boots under /Workoutapp/');
  await shot('01-home-empty');

  // 2. Start a workout and add exercises
  await page.getByRole('button', { name: 'Start workout' }).click();
  await page.waitForURL(/workout\//);
  await page.getByRole('button', { name: 'Add exercises' }).click();
  await page.getByPlaceholder('Search exercises').fill('bench');
  await page.locator('.pick-list .list-row', { hasText: /^B\s*Bench Press/ }).first().click();
  await page.getByPlaceholder('Search exercises').fill('');
  await page.locator('.chip', { hasText: 'Legs' }).click();
  await page.locator('.pick-list .list-row', { hasText: /^S\s*Squat/ }).first().click();
  await shot('02-picker');
  await page.locator('.sheet-footer button').click();
  await page.locator('.exercise-card').nth(1).waitFor();
  step('added Bench Press + Squat via picker');

  // 3. Log sets
  const card = page.locator('.exercise-card').first();
  await card.getByLabel('Weight, set 1').fill('80'); await card.getByLabel('Weight, set 1').blur();
  await card.getByLabel('Reps, set 1').fill('8'); await card.getByLabel('Reps, set 1').blur();
  await card.getByLabel('Complete set 1').click();
  await page.locator('.rest-bar').waitFor();
  step('completing a set starts the rest timer');
  await card.getByRole('button', { name: 'Add set' }).click();
  await card.getByLabel('Complete set 2').click();
  const sq = page.locator('.exercise-card').nth(1);
  await sq.getByLabel('Weight, set 1').fill('100'); await sq.getByLabel('Weight, set 1').blur();
  await sq.getByLabel('Reps, set 1').fill('5'); await sq.getByLabel('Reps, set 1').blur();
  await sq.getByLabel('Complete set 1').click();
  await sq.getByRole('button', { name: 'Add set' }).click();
  await page.waitForTimeout(400);
  await shot('03-logging');

  // 4. Navigate away: banner + timer persist
  await page.locator('.tabbar').getByText('History').click();
  await page.locator('.active-banner').waitFor();
  step('in-progress banner shows on other pages');
  await page.locator('.active-banner').click();

  // 5. Finish
  await page.getByRole('button', { name: 'Finish' }).click();
  await page.locator('.dialog').getByText('1 unchecked set will be dropped', { exact: false }).waitFor();
  await page.locator('.dialog .btn-primary').click();
  await page.locator('.title-input').waitFor();
  const sets = await page.locator('.set-row').count();
  if (sets !== 3) throw new Error(`expected 3 saved sets, got ${sets}`);
  step('finish drops unchecked set, keeps 3 completed');

  // 6. Persists across reload
  await page.reload();
  await page.goto(BASE + 'history');
  await page.locator('.workout-card').first().waitFor();
  step('workout persisted after reload; deep link /history works');

  // 7. Seed realistic history for screenshots
  await page.evaluate((d) => localStorage.setItem('workout-tracker:data', d), seed());
  await page.goto(BASE);
  await page.locator('.goal-card').waitFor();
  await page.waitForTimeout(500);
  await shot('04-home');
  await page.screenshot({ path: `${SHOTS}/04-home-full.png`, fullPage: true });
  await page.goto(BASE + 'history'); await page.locator('.workout-card').first().waitFor(); await shot('05-history');
  await page.goto(BASE + 'progress'); await page.locator('.hbars').waitFor(); await page.waitForTimeout(300);
  await page.screenshot({ path: `${SHOTS}/06-progress.png`, fullPage: true });
  await page.goto(BASE + 'exercises/bench-press'); await page.locator('.line-chart').waitFor(); await page.waitForTimeout(300); await shot('07-exercise');
  await page.goto(BASE + 'exercises'); await page.locator('.list-row').first().waitFor(); await shot('08-exercises');
  await page.goto(BASE + 'settings'); await page.getByText('Units').waitFor(); await shot('09-settings');
  step('all pages render with seeded data (deep links OK)');

  // Repeat-workout template
  await page.goto(BASE);
  await page.locator('.template-card').first().click();
  await page.waitForURL(/workout\//);
  await page.locator('.exercise-card').first().waitFor();
  const prevText = await page.locator('.prev').first().textContent();
  step(`repeat workout pre-fills previous sets ("${prevText.trim()}")`);
  await shot('10-repeat');
  await page.getByRole('button', { name: 'Discard workout' }).click();
  await page.locator('.dialog .btn-danger').click();
  await page.waitForURL(u => !u.toString().includes('workout/'));

  // Light theme
  await page.goto(BASE + 'settings');
  await page.locator('.segmented button', { hasText: 'Light' }).click();
  await page.goto(BASE); await page.locator('.goal-card').waitFor(); await page.waitForTimeout(300);
  await shot('11-home-light');
  step('light theme applies');

  // Offline support via service worker
  await page.evaluate(() => navigator.serviceWorker.ready);
  await page.waitForTimeout(3000);
  const cached = await page.evaluate(async () => (await Promise.all((await caches.keys()).map(async k => (await (await caches.open(k)).keys()).length)))[0] || 0);
  await ctx.setOffline(true);
  await page.reload();
  await page.locator('.goal-card').waitFor({ timeout: 15000 });
  await ctx.setOffline(false);
  step(`works offline (${cached} assets cached by service worker)`);

  // Desktop layout
  const desk = await browser.newPage({ viewport: { width: 1280, height: 860 }, colorScheme: 'dark' });
  await desk.goto(BASE);
  await desk.evaluate((d) => localStorage.setItem('workout-tracker:data', d), seed());
  await desk.goto(BASE + 'progress'); await desk.locator('.hbars').waitFor(); await desk.waitForTimeout(300);
  await desk.screenshot({ path: `${SHOTS}/12-desktop.png` });
  step('desktop sidebar layout renders');

  // No horizontal overflow on a small phone
  const small = await browser.newPage({ viewport: { width: 360, height: 740 } });
  await small.goto(BASE);
  await small.evaluate((d) => localStorage.setItem('workout-tracker:data', d), seed());
  for (const r of ['', 'history', 'progress', 'exercises/bench-press', 'settings']) {
    await small.goto(BASE + r); await small.waitForTimeout(1200);
    const over = await small.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
    if (over > 0) throw new Error(`horizontal overflow ${over}px on /${r}`);
  }
  step('no horizontal scroll at 360px width');

  await browser.close();
  const real = errors.filter(e => !/ERR_INTERNET_DISCONNECTED|Failed to fetch|net::ERR/.test(e));
  console.log(real.length ? 'CONSOLE ERRORS:\n' + real.join('\n') : '✓ no console errors');
})().catch(e => { console.error('✗ FAILED:', e.message); console.error(errors.join('\n')); process.exit(1); });
