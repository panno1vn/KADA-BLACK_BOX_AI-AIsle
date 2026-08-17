import assert from 'node:assert/strict';
import {buildAnalytics} from '../backend/analytics.mjs';

function run(id, createdAt, summary, extra = {}) {
  return {id, schemaVersion: 'aisle.sim-result.v1', createdAt, name: id, seed: 1, durationMinutes: 30, summary, ...extra};
}

// Empty history must not crash and must report zeroed, empty results.
{
  const empty = buildAnalytics([]);
  assert.equal(empty.totals.runs, 0);
  assert.equal(empty.totals.conversionRate, 0);
  assert.equal(empty.totals.avgEmotion, 0);
  for (const key of ['daily', 'monthly', 'quarterly', 'yearly']) assert.deepEqual(empty.series[key], []);
}

const runA = run('a', '2026-01-15T10:00:00.000Z', {revenue: 100000, purchases: 10, spawned: 100, active: 5, conversionRate: 0.5, mainRate: 0.3, impulseRate: 0.1, avgEmotion: 0.4});
const runB = run('b', '2026-01-20T10:00:00.000Z', {revenue: 50000, purchases: 5, spawned: 50, active: 0, conversionRate: 0.6, mainRate: 0.2, impulseRate: 0.04, avgEmotion: -0.2});
const runC = run('c', '2026-04-05T10:00:00.000Z', {revenue: 20000, purchases: 2, spawned: 20, active: 20, conversionRate: 1, mainRate: 1, impulseRate: 0, avgEmotion: 1});
// Pre-existing history saved before avgEmotion was tracked, and with a corrupt createdAt — must not crash or skew series.
const runD = run('d', 'not-a-real-date', {revenue: 5000, purchases: 1, spawned: 10, active: 0, conversionRate: 0.1, mainRate: 0, impulseRate: 0});

const a = buildAnalytics([runA, runB, runC, runD]);

// Totals: runD still counts (it has real numbers), even though its date can't be bucketed.
assert.equal(a.totals.runs, 4);
assert.equal(a.totals.revenue, 175000);
assert.equal(a.totals.customersIn, 180); // 100+50+20+10
assert.equal(a.totals.customersOut, 155); // (100-5)+(50-0)+(20-20)+(10-0)
assert.equal(a.totals.converted, 101); // round(.5*100)+round(.6*50)+round(1*20)+round(.1*10) = 50+30+20+1
assert.ok(Math.abs(a.totals.conversionRate - 101 / 180) < 1e-9);
// avgEmotion is weighted by customersIn; runD contributes 0 (neutral), diluting rather than skewing negative.
const expectedEmotionWeighted = 0.4 * 100 + -0.2 * 50 + 1 * 20 + 0 * 10;
assert.ok(Math.abs(a.totals.avgEmotion - expectedEmotionWeighted / 180) < 1e-9);

// A run with an unparseable createdAt must be excluded from every period series, not silently miscounted somewhere.
for (const key of ['daily', 'monthly', 'quarterly', 'yearly']) {
  assert.ok(!a.series[key].some(bucket => bucket.runs && bucket.period === undefined), `no undefined period leaked into ${key}`);
}
assert.equal(a.series.monthly.length, 2, 'only 2026-01 and 2026-04 have parseable dates');

// Monthly bucket must combine A+B (same month) with correctly weighted rates.
const jan = a.series.monthly.find(m => m.period === '2026-01');
assert.ok(jan, 'January bucket must exist');
assert.equal(jan.runs, 2);
assert.equal(jan.revenue, 150000);
assert.equal(jan.customersIn, 150);
assert.equal(jan.converted, 80); // 50 + 30
assert.ok(Math.abs(jan.conversionRate - 80 / 150) < 1e-9);
assert.ok(Math.abs(jan.avgEmotion - (0.4 * 100 + -0.2 * 50) / 150) < 1e-9);

const april = a.series.monthly.find(m => m.period === '2026-04');
assert.equal(april.runs, 1);
assert.equal(april.conversionRate, 1);
assert.equal(april.avgEmotion, 1);

// Quarterly must fold January into Q1 and April into Q2, and stay chronologically sorted.
assert.deepEqual(a.series.quarterly.map(q => q.period), ['2026-Q1', '2026-Q2']);
assert.equal(a.series.quarterly[0].runs, 2);
assert.equal(a.series.quarterly[1].runs, 1);

// Yearly must fold everything with a valid date into a single 2026 bucket (runD excluded, since its date is invalid).
assert.equal(a.series.yearly.length, 1);
assert.equal(a.series.yearly[0].period, '2026');
assert.equal(a.series.yearly[0].runs, 3);
assert.equal(a.series.yearly[0].customersIn, 170); // A+B+C only, not D

console.log('ok — analytics aggregation totals, weighted rates, and period bucketing');
