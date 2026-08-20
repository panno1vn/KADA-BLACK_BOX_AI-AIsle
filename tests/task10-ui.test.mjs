import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

test('T10 S1-S5 speed presets are exact and multiplier is owned by the C# bridge', async () => {
  const html = await readFile(new URL('../web/index.html', import.meta.url), 'utf8');
  const app = await readFile(new URL('../web/app.js', import.meta.url), 'utf8');
  const service = await readFile(new URL('../src/AIsle.DesktopApp/Application/SimulationApplicationService.cs', import.meta.url), 'utf8');
  const select = html.match(/<select id="speed">([\s\S]*?)<\/select>/)?.[1] ?? '';
  const values = [...select.matchAll(/<option value="([^"]+)"/g)].map(match => Number(match[1]));
  assert.deepEqual(values, [1, 2, 3, 5, 15, 30]);
  assert.match(app, /simulation\.setSpeed\(Number\(\$\('#speed'\)\.value\)\)/);
  assert.match(app, /simulation\.refresh\(\)/);
  assert.doesNotMatch(app, /new LiveSimulation/);
  assert.doesNotMatch(app, /accumulator\s*\+=/);
  assert.doesNotMatch(app, /simulation\.step\(parameters\.tickSeconds\s*\*\s*speed\)/);
  assert.match(service, /_accumulatorSeconds \+= realSeconds \* _speedMultiplier/);
  assert.match(service, /_host\.Step\(_input\.Config\.TickSeconds\)/);
  assert.doesNotMatch(service, /_host\.Step\(_input\.Config\.TickSeconds \* _speedMultiplier\)/);
});

test('T10 shelf-facing is a visual-only DWELL projection', async () => {
  const app = await readFile(new URL('../web/app.js', import.meta.url), 'utf8');
  assert.match(app, /agent\.status==='DWELL'/);
  assert.match(app, /facingDx:/);
  assert.match(app, /facingDy:/);
});
