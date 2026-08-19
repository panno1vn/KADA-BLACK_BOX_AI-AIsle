import assert from 'node:assert/strict';
import { performance } from 'node:perf_hooks';
import test from 'node:test';
import {
  NPC_DIRECTIONS,
  NPC_SPRITE_ASSETS,
  NpcSpriteRenderer,
  directionIndexFromDelta,
  resolveSpriteFrame,
  selectModelIndex,
  validateSpriteDimensions,
  walkingFrameAt,
} from '../../src/AIsle.DesktopApp/UI/npc-renderer.mjs';

test('R1-R2 model selection is deterministic, bounded and spread across registry', () => {
  assert.equal(selectModelIndex(42, 'npc-7', 4), selectModelIndex(42, 'npc-7', 4));
  const models = new Set();
  for (let index = 0; index < 200; index++) {
    const model = selectModelIndex(42, `npc-${index}`, NPC_SPRITE_ASSETS.length);
    assert.ok(model >= 0 && model < NPC_SPRITE_ASSETS.length);
    models.add(model);
  }
  assert.equal(models.size, 4);
});

test('R3-R7 direction mapping follows screen-space clockwise rows from South', () => {
  const cases = [
    [0, 1, 'S'], [-1, 1, 'SW'], [-1, 0, 'W'], [-1, -1, 'NW'],
    [0, -1, 'N'], [1, -1, 'NE'], [1, 0, 'E'], [1, 1, 'SE'],
  ];
  for (const [dx, dy, expected] of cases)
    assert.equal(NPC_DIRECTIONS[directionIndexFromDelta(dx, dy)], expected);
  assert.equal(directionIndexFromDelta(0.00001, 0.00001, 5, 0.001), 5);
});

test('R8-R9 frame loop and crop rectangles stay inside the 8x4 sheet', () => {
  assert.deepEqual([0, 125, 250, 375, 500].map(time => walkingFrameAt(time, 8)), [0, 1, 2, 3, 0]);
  assert.deepEqual(validateSpriteDimensions(128, 384), { frameWidth: 32, frameHeight: 48 });
  for (let direction = 0; direction < 8; direction++) for (let frame = 0; frame < 4; frame++) {
    const crop = resolveSpriteFrame(128, 384, direction, frame);
    assert.ok(crop.x >= 0 && crop.y >= 0 && crop.x + crop.width <= 128 && crop.y + crop.height <= 384);
  }
  assert.throws(() => validateSpriteDimensions(127, 384), /8 rows x 4 columns/);
});

test('R10 reset clears state; live and replay positions reuse one renderer/model mapping', async () => {
  const renderer = await readyRenderer();
  const context = fakeContext();
  renderer.reset(77, 0);
  renderer.draw(context, [{ id: 'npc-a', x: 1, y: 1, status: 'TRANSIT' }], { runSeed: 77, animationTimeMs: 0, running: true, scaleX: 50, scaleY: 50 });
  renderer.draw(context, [{ id: 'npc-a', x: 1.2, y: 1, status: 'TRANSIT' }], { runSeed: 77, animationTimeMs: 125, running: true, scaleX: 50, scaleY: 50 });
  const liveModel = renderer.states.get('npc-a').modelIndex;
  assert.equal(renderer.states.get('npc-a').direction, 6);
  assert.equal(context.imageSmoothingEnabled, false);
  renderer.reset(77, 0);
  assert.equal(renderer.states.size, 0);
  renderer.draw(context, [{ id: 'npc-a', x: 3, y: 2, status: 'REPLAY' }], { runSeed: 77, animationTimeMs: 0, running: false, scaleX: 50, scaleY: 50 });
  assert.equal(renderer.states.get('npc-a').modelIndex, liveModel);
});

test('T10 F1-F5 shelf-facing override is stable and has priority over stale movement', async () => {
  const cases = [
    [{ facingDx: 0, facingDy: 1 }, 0],
    [{ facingDx: 0, facingDy: -1 }, 4],
    [{ facingDx: 1, facingDy: 0 }, 6],
    [{ facingDx: -1, facingDy: 0 }, 2],
  ];
  for (const [facing, expected] of cases) {
    const renderer = await readyRenderer();
    const context = fakeContext();
    renderer.reset(88, 0);
    renderer.draw(context, [{ id: 'shelf-npc', x: 2, y: 2, status: 'DWELL', ...facing }], { runSeed: 88, animationTimeMs: 0, running: true });
    renderer.draw(context, [{ id: 'shelf-npc', x: 2, y: 2, status: 'DWELL', ...facing }], { runSeed: 88, animationTimeMs: 500, running: true });
    assert.equal(renderer.states.get('shelf-npc').direction, expected);
    assert.equal(renderer.states.get('shelf-npc').frame, 0);
  }
});

test('renderer benchmark records 200/500/1000 NPC frame cost and bounded state', async () => {
  for (const count of [200, 500, 1000]) {
    const renderer = await readyRenderer();
    const context = fakeContext();
    const agents = Array.from({ length: count }, (_, index) => ({ id: `npc-${index}`, x: index % 40, y: Math.floor(index / 40), status: 'TRANSIT' }));
    renderer.reset(1234, 0);
    const memoryBefore = process.memoryUsage().heapUsed;
    const samples = [];
    for (let frame = 0; frame < 120; frame++) {
      for (let index = 0; index < agents.length; index++) agents[index].x += index % 2 ? -0.002 : 0.002;
      const started = performance.now();
      const metrics = renderer.draw(context, agents, { runSeed: 1234, animationTimeMs: frame * 16.667, running: true, scaleX: 32, scaleY: 32 });
      samples.push(performance.now() - started);
      assert.equal(metrics.drawCalls, count);
      assert.equal(metrics.visualStates, count);
    }
    samples.sort((left, right) => left - right);
    const average = samples.reduce((sum, value) => sum + value, 0) / samples.length;
    const p95 = samples[Math.floor(samples.length * 0.95)];
    const memoryDeltaMb = (process.memoryUsage().heapUsed - memoryBefore) / 1024 / 1024;
    console.log(`BENCH NPC=${count} avg=${average.toFixed(3)}ms p95=${p95.toFixed(3)}ms fps=${(1000 / average).toFixed(1)} memoryDelta=${memoryDeltaMb.toFixed(2)}MB drawCalls=${count} states=${renderer.states.size}`);
    assert.ok(Number.isFinite(average) && Number.isFinite(p95));
  }
});

async function readyRenderer() {
  const renderer = new NpcSpriteRenderer({ imageFactory: () => new FakeImage(), warn: () => {} });
  assert.equal(await renderer.load(), 4);
  return renderer;
}

class FakeImage {
  constructor() { this.width = this.naturalWidth = 128; this.height = this.naturalHeight = 384; }
  set src(value) { this._src = value; queueMicrotask(() => this.onload?.()); }
  get src() { return this._src; }
  decode() { return Promise.resolve(); }
}

function fakeContext() {
  return {
    imageSmoothingEnabled: true,
    drawImage() {}, beginPath() {}, arc() {}, fill() {}, stroke() {},
    fillStyle: '', strokeStyle: '', lineWidth: 1,
  };
}
