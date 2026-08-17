import assert from 'node:assert/strict';
import {mkdtemp, rm} from 'node:fs/promises';
import {tmpdir} from 'node:os';
import {join} from 'node:path';
import {createApiRouter} from '../backend/routes/api-router.mjs';
import {createProjectStore} from '../backend/storage/project-store.mjs';
import {DEFAULT_CATALOG, DEFAULT_LAYOUT} from '../web/project-defaults.js';
import {LiveSimulation, generatePopulation, createRng} from '../web/live-engine.js';
import {createSimResult} from '../web/sim-result.js';

// Covers the merge point between the real GET /api/analytics (this branch) and the mock
// GET /api/statistics-by/:type/:year (merged in from `test`) — both must keep working from
// the same router after being combined by hand during conflict resolution.

function fakeRequest(method = 'GET') {
  return {method, async *[Symbol.asyncIterator]() {}}; // GET routes never read a body
}
function fakeResponse() {
  return {
    status: null, body: null,
    writeHead(status) { this.status = status; },
    end(payload) { this.body = payload; },
    json() { return JSON.parse(this.body.toString('utf8')); },
  };
}

const directory = await mkdtemp(join(tmpdir(), 'aisle-router-'));
try {
  const store = createProjectStore(directory, {DEFAULT_LAYOUT, DEFAULT_CATALOG});
  const routeApi = createApiRouter(store);

  const population = generatePopulation(DEFAULT_CATALOG, 40, createRng(11));
  const sim = new LiveSimulation({layout: DEFAULT_LAYOUT, catalog: DEFAULT_CATALOG, population, seed: 11, durationMinutes: 5});
  while (!sim.completed) sim.step(0.5);
  await store.saveHistory(createSimResult({simulation: sim, name: 'router test', layout: DEFAULT_LAYOUT, catalog: DEFAULT_CATALOG}));

  {
    const res = fakeResponse();
    const handled = await routeApi(fakeRequest(), res, new URL('http://localhost/api/analytics'));
    assert.equal(handled, true);
    assert.equal(res.status, 200);
    const body = res.json();
    assert.equal(body.totals.runs, 1);
    assert.equal(body.series.daily.length, 1);
  }

  {
    const res = fakeResponse();
    const handled = await routeApi(fakeRequest(), res, new URL('http://localhost/api/statistics-by/thang/2026'));
    assert.equal(handled, true);
    assert.equal(res.status, 200);
    const body = res.json();
    assert.equal(body.percent.length, 12);
    assert.ok(body.percent.every(p => /^\d+%$/.test(p.value)));
    assert.equal(typeof body.numberOfPurchases, 'number');
  }

  {
    const res = fakeResponse();
    await routeApi(fakeRequest(), res, new URL('http://localhost/api/statistics-by/invalid-type/2026'));
    assert.equal(res.status, 400, 'an unknown period type must 400, not throw or fall through silently');
  }

  {
    const res = fakeResponse();
    await routeApi(fakeRequest(), res, new URL('http://localhost/api/does-not-exist'));
    assert.equal(res.status, 404);
  }

  {
    const res = fakeResponse();
    const handled = await routeApi(fakeRequest(), res, new URL('http://localhost/index.html'));
    assert.equal(handled, false, 'non-API paths must fall through to static file handling in server.mjs');
  }
} finally {
  await rm(directory, {recursive: true, force: true});
}

console.log('ok — merged router serves both the real analytics endpoint and the mock statistics-by endpoint');
