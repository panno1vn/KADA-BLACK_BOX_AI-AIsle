import {validateLayout} from '../../web/layout-validation.js';
import {validateSimResult} from '../../web/sim-result.js';
import {buildAnalytics} from '../analytics.mjs';

async function readBody(request, maxBytes = 12_000_000) {
  const chunks = [];
  let size = 0;
  for await (const chunk of request) {
    size += chunk.length;
    if (size > maxBytes) throw Object.assign(new Error('Request too large'), {status: 413});
    chunks.push(chunk);
  }
  if (!chunks.length) return {};
  try {
    return JSON.parse(Buffer.concat(chunks).toString('utf8'));
  } catch {
    throw Object.assign(new Error('Invalid JSON body'), {status: 400});
  }
}

export function sendJson(response, data, status = 200) {
  const payload = Buffer.from(JSON.stringify(data));
  response.writeHead(status, {
    'Content-Type': 'application/json; charset=utf-8',
    'Content-Length': payload.length,
    'Cache-Control': 'no-store',
  });
  response.end(payload);
}

function seededRandom(seed) {
  let s = (seed >>> 0) || 1;
  return function next() {
    s = (s * 48271) % 2147483647;
    return (s - 1) / 2147483646;
  };
}

export function buildStatistics(type, year) {
  const count = type === 'thang' ? 12 : type === 'quy' ? 4 : Math.max(1, year - 2000 + 1);
  const keys = Array.from({length: count}, (_, i) => (type === 'nam' ? 2000 + i : i + 1));
  const rng = seededRandom(year * 1000 + count);
  const raw = keys.map(() => 5 + rng() * 95);
  const total = raw.reduce((a, b) => a + b, 0);
  const weights = raw.map(value => value / total);
  const shares = weights.map(weight => Math.floor(weight * 100));
  const remainder = 100 - shares.reduce((a, b) => a + b, 0);
  const sorted = weights
    .map((weight, i) => ({i, rest: weight * 100 - Math.floor(weight * 100)}))
    .sort((a, b) => b.rest - a.rest);
  for (let k = 0; k < remainder; k++) shares[sorted[k % sorted.length].i] += 1;
  return {
    percent: keys.map((key, i) => ({key, value: `${shares[i]}%`})),
    numberOfPurchases: (year - 2000) * 137 + Math.round(800 + rng() * 400),
  };
}

export function createApiRouter(store) {
  return async function routeApi(request, response, url) {
    if (url.pathname === '/health' && request.method === 'GET') {
      sendJson(response, {ok: true, engine: 'javascript-live'});
      return true;
    }
    if (url.pathname === '/api/project' && request.method === 'GET') {
      sendJson(response, await store.getProject());
      return true;
    }
    if (url.pathname === '/api/project' && request.method === 'POST') {
      const project = await readBody(request);
      const validation = validateLayout(project.layout);
      if (!validation.valid) {
        sendJson(response, {error: validation.errors.join(' '), errors: validation.errors}, 400);
        return true;
      }
      await store.saveProject(project);
      sendJson(response, {ok: true, warnings: validation.warnings, unreachableShelfIds: validation.unreachableShelfIds});
      return true;
    }
    if (url.pathname === '/api/live-result' && request.method === 'POST') {
      await store.saveLiveResult(await readBody(request));
      sendJson(response, {ok: true});
      return true;
    }
    if (url.pathname === '/api/history' && request.method === 'GET') {
      sendJson(response, {runs: await store.listHistory()});
      return true;
    }
    if (url.pathname === '/api/history' && request.method === 'POST') {
      const result = await readBody(request), validation = validateSimResult(result);
      if (!validation.valid) {
        sendJson(response, {error: validation.errors.join(' '), errors: validation.errors}, 400);
        return true;
      }
      sendJson(response, {ok: true, ...(await store.saveHistory(result))}, 201);
      return true;
    }
    if (url.pathname.startsWith('/api/history/') && request.method === 'GET') {
      sendJson(response, await store.getHistory(decodeURIComponent(url.pathname.slice('/api/history/'.length))));
      return true;
    }
    if (url.pathname === '/api/analytics' && request.method === 'GET') {
      sendJson(response, buildAnalytics(await store.listHistory()));
      return true;
    }
    if (url.pathname.startsWith('/api/statistics-by/') && request.method === 'GET') {
      const [type, yearText] = decodeURIComponent(url.pathname.slice('/api/statistics-by/'.length)).split('/');
      const year = Number(yearText);
      if (!['thang', 'quy', 'nam'].includes(type) || !Number.isInteger(year)) {
        sendJson(response, {error: 'Invalid statistics-by params'}, 400);
        return true;
      }
      sendJson(response, buildStatistics(type, year));
      return true;
    }
    if (url.pathname.startsWith('/api/')) {
      sendJson(response, {error: 'API route not found'}, 404);
      return true;
    }
    return false;
  };
}
