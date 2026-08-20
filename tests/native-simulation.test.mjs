import assert from 'node:assert/strict';
import test from 'node:test';
import {DESKTOP_SPEEDS, NativeSimulationAdapter} from '../web/native-simulation.mjs';

test('native adapter delegates runtime commands and only projects bridge snapshots', async () => {
  const calls = [];
  const snapshot = {
    runId: 'sim-native',
    speedMultiplier: 3,
    state: {
      time: .2,
      running: true,
      completed: false,
      agents: [{id: 'npc-1', x: 2, y: 3, status: 'QUEUE', targetId: 's1'}],
      counters: {active: 1}
    },
    summary: {spawned: 1, converted: 0, purchases: 0, revenue: 0, notFound: 0},
    events: [{time: .2, npcId: 'npc-1', type: 'queue', message: 'waiting'}],
    purchases: []
  };
  const bridge = {request: async(type, payload) => {calls.push({type, payload}); return type === 'simulation.result' ? {id: 'sim-native'} : snapshot}};
  const adapter = new NativeSimulationAdapter(bridge, [{id: 'npc-1', targetCategory: 'beverage'}], 60);

  await adapter.start({name: 'native'});
  await adapter.setSpeed(3);
  await adapter.pause();
  await adapter.step();
  const result = await adapter.result('native');

  assert.deepEqual(DESKTOP_SPEEDS, [1, 2, 3, 5, 15, 30]);
  assert.deepEqual(calls.map(call => call.type), ['simulation.start', 'simulation.snapshot', 'simulation.speed', 'simulation.pause', 'simulation.snapshot', 'simulation.step', 'simulation.snapshot', 'simulation.result']);
  assert.equal(adapter.agents[0].status, 'QUEUE');
  assert.equal(adapter.agents[0].currentShelf, 's1');
  assert.equal(adapter.events[0].npc, 'npc-1');
  assert.equal(result.id, adapter.seed);
});

test('native adapter rejects non-preset speed before sending a command', async () => {
  const bridge = {request: async() => {throw new Error('must not be called')}};
  const adapter = new NativeSimulationAdapter(bridge, [], 60);
  await assert.rejects(() => adapter.setSpeed(4), /Unsupported simulation speed/);
});
