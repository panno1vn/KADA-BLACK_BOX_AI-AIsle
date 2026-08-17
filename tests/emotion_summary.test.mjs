import assert from 'node:assert/strict';
import {LiveSimulation, manualPopulation} from '../web/live-engine.js';

const layout = {
  width: 8, height: 4, walls: [],
  shelves: [{id: 'dwell', label: 'Dwell shelf', category: 'beverage', x: 2.5, y: 1.2, w: 1, h: 1, valence: 0.9}],
  entrance: {x: 1, y: 1.7}, checkout: {x: 1, y: 2.7},
};

const sim = new LiveSimulation({
  layout, catalog: [],
  population: manualPopulation([{npc_id: 'a', target_category: 'beverage', need_product: .4, need_explore: .2, speed: 1.2, dwell: 3}]),
  parameters: {decisionNoise: 0},
  durationMinutes: 1,
});
const agent = sim.agents[0];
agent.spawn = 0;
agent.currentShelf = 'dwell'; agent.status = 'DWELL';
agent.valence = 0.1; agent.attractor = 0.2; agent.stability = 0.1; agent.dispersion = 0.8; agent.recovery = 0.25;

assert.equal(agent.peakValence, agent.attractor, 'peakValence starts at the spawn baseline (attractor)');

sim.finishDwell(agent);
assert.ok(agent.peakValence > agent.attractor, 'peakValence must rise to record the shelf-reaction moment');
assert.ok(agent.peakValence >= agent.valence, 'peak can never be below the current (post-recovery) valence');

// snapshot().avgEmotion is the Peak-End average — mean of (peak, end) across every spawned NPC.
const snap = sim.snapshot();
const expected = (agent.peakValence + agent.valence) / 2;
assert.ok(Math.abs(snap.avgEmotion - expected) < 1e-9, 'snapshot avgEmotion must average peak and end valence');

// A later, worse dwell must not erase the recorded peak.
const peakBefore = agent.peakValence;
agent.currentShelf = 'dwell'; agent.status = 'DWELL';
agent.dispersion = 0.9; agent.stability = 0; // force a big swing toward the (still positive) shelf, then heavy recovery
sim.finishDwell(agent);
assert.ok(agent.peakValence >= peakBefore, 'peakValence must never decrease once recorded');

// An NPC that never spawns yet (this.time < a.spawn) must not be counted in the average at all.
agent.spawn = 999;
sim.time = 0;
const emptySnap = sim.snapshot();
assert.equal(emptySnap.avgEmotion, 0, 'with no spawned agents, avgEmotion falls back to neutral 0 instead of NaN');

console.log('ok — peak valence tracking and snapshot avgEmotion follow the Peak-End formula');
