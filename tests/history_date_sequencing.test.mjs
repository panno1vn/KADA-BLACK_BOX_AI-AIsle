import assert from 'node:assert/strict';
import {mkdtemp, rm} from 'node:fs/promises';
import {tmpdir} from 'node:os';
import {join} from 'node:path';
import {createProjectStore} from '../backend/storage/project-store.mjs';
import {DEFAULT_CATALOG, DEFAULT_LAYOUT} from '../web/project-defaults.js';

function minimalResult(id, clientCreatedAt) {
  return {
    schemaVersion: 'aisle.sim-result.v1', id, createdAt: clientCreatedAt, name: 'Test',
    input: {seed: 1}, project: {layout: {shelves: []}},
    summary: {time: 100, revenue: 0, purchases: 0, spawned: 0, active: 0, conversionRate: 0, mainRate: 0, impulseRate: 0, notFoundRate: 0, avgEmotion: 0, completed: true},
    replay: {agents: []},
  };
}

const directory = await mkdtemp(join(tmpdir(), 'aisle-date-seq-'));
try {
  const store = createProjectStore(directory, {DEFAULT_LAYOUT, DEFAULT_CATALOG});

  // Each saved run is meant to represent one simulated business day, not the literal save
  // timestamp, so the server must ignore whatever createdAt the client sends.
  const before = Date.now();
  const first = await store.saveHistory(minimalResult('run1', '2000-01-01T00:00:00.000Z'));
  const after = Date.now();
  assert.notEqual(first.createdAt, '2000-01-01T00:00:00.000Z', 'server must ignore the client-supplied createdAt');
  const firstTime = new Date(first.createdAt).getTime();
  assert.ok(firstTime >= before - 1000 && firstTime <= after + 1000, 'the first-ever save should land at roughly now');

  const second = await store.saveHistory(minimalResult('run2', '1999-12-31T00:00:00.000Z'));
  const expectedSecond = new Date(first.createdAt);
  expectedSecond.setUTCDate(expectedSecond.getUTCDate() + 1);
  assert.equal(second.createdAt, expectedSecond.toISOString(), 'second save must land exactly one day after the first');

  const third = await store.saveHistory(minimalResult('run3', '2030-06-01T00:00:00.000Z'));
  const expectedThird = new Date(second.createdAt);
  expectedThird.setUTCDate(expectedThird.getUTCDate() + 1);
  assert.equal(third.createdAt, expectedThird.toISOString(), 'sequencing must read the current latest run from disk each time, not a cached value');

  // listHistory must reflect the server-assigned dates, sorted newest first.
  const list = await store.listHistory();
  assert.deepEqual(list.map(item => item.id), ['run3', 'run2', 'run1']);
} finally {
  await rm(directory, {recursive: true, force: true});
}

console.log('ok — saved history always advances one simulated day, ignoring client-supplied createdAt');
