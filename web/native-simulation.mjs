export const DESKTOP_SPEEDS = Object.freeze([1, 2, 3, 5, 15, 30]);

export class NativeSimulationAdapter {
  constructor(bridge, profiles, durationSeconds) {
    if (!bridge?.request) throw new Error('AIsle Desktop bridge is unavailable.');
    this.bridge = bridge;
    this.profileById = new Map(profiles.map(profile => [profile.id, profile]));
    this.duration = durationSeconds;
    this.seed = '';
    this.time = 0;
    this.completed = false;
    this.running = false;
    this.agents = [];
    this.events = [];
    this.purchases = [];
    this.stats = emptyStats();
    this.currentSnapshot = emptySnapshot();
    this.agentCache = new Map();
  }

  async start(input) {
    await this.bridge.request('simulation.start', { input });
    return this.refresh();
  }

  async resume() {
    await this.bridge.request('simulation.start', {});
    return this.refresh();
  }

  async pause() {
    await this.bridge.request('simulation.pause', {});
    return this.refresh();
  }

  async step() {
    await this.bridge.request('simulation.step', {});
    return this.refresh();
  }

  async reset() {
    await this.bridge.request('simulation.reset', {});
    this.agentCache.clear();
    return this.refresh();
  }

  async setSpeed(multiplier) {
    if (!DESKTOP_SPEEDS.includes(Number(multiplier))) throw new Error('Unsupported simulation speed.');
    const snapshot = await this.bridge.request('simulation.speed', { multiplier: Number(multiplier) });
    return this.apply(snapshot);
  }

  async refresh() {
    return this.apply(await this.bridge.request('simulation.snapshot', {}));
  }

  async result(name) {
    return this.bridge.request('simulation.result', { name });
  }

  snapshot() {
    return this.currentSnapshot;
  }

  apply(payload) {
    const state = payload?.state ?? {};
    const summary = payload?.summary ?? {};
    const purchasedNpcIds = new Set((payload?.purchases ?? []).map(item => item.npcId));
    this.seed = payload?.runId || this.seed;
    this.time = Number(state.time) || 0;
    this.running = Boolean(state.running);
    this.completed = Boolean(state.completed);
    this.events = (payload?.events ?? []).map(item => ({ ...item, npc: item.npcId || 'system' }));
    this.purchases = (payload?.purchases ?? []).map(item => ({ ...item, npc: item.npcId, product: item.productId }));
    this.agents = (state.agents ?? []).map(item => this.projectAgent(item, purchasedNpcIds));
    this.stats = {
      spawned: Number(summary.spawned) || 0,
      converted: Number(summary.converted) || 0,
      mainBuyers: Number(summary.mainBuyers) || 0,
      impulseBuyers: Number(summary.impulseBuyers) || 0,
      notFound: Number(summary.notFound) || 0,
      unreachable: Number(summary.unreachable) || 0,
      stuckRecoveries: Number(summary.stuckRecoveries) || 0
    };
    this.currentSnapshot = {
      time: this.time,
      revenue: Number(summary.revenue) || 0,
      purchases: Number(summary.purchases) || 0,
      spawned: this.stats.spawned,
      active: Number(state.counters?.active) || 0,
      conversionRate: this.stats.spawned ? this.stats.converted / this.stats.spawned : 0,
      mainRate: this.stats.spawned ? this.stats.mainBuyers / this.stats.spawned : 0,
      impulseRate: this.stats.spawned ? this.stats.impulseBuyers / this.stats.spawned : 0,
      notFoundRate: this.agents.length ? this.stats.notFound / this.agents.length : 0,
      completed: this.completed
    };
    return this.currentSnapshot;
  }

  projectAgent(item, purchasedNpcIds) {
    const previous = this.agentCache.get(item.id);
    const profile = this.profileById.get(item.id) ?? {};
    const trail = previous?.trail ?? [];
    if (!trail.length || trail.at(-1).x !== item.x || trail.at(-1).y !== item.y) {
      trail.push({ x: item.x, y: item.y });
      if (trail.length > 80) trail.shift();
    }
    const finished = item.status === 'LEFT' || item.status === 'BLOCKED';
    const agent = {
      ...profile,
      id: item.id,
      x: item.x,
      y: item.y,
      status: item.status,
      targetId: item.targetId || '',
      currentShelf: item.targetId && item.targetId !== 'checkout' && item.targetId !== 'entrance' ? item.targetId : null,
      target: profile.targetCategory ?? profile.target ?? null,
      need: Number(profile.initialNeed ?? profile.needProduct) || 0,
      explore: Number(profile.initialExplorationNeed ?? profile.needExplore) || 0,
      valence: Number(profile.affectAttractor ?? profile.attractor) || 0,
      visited: [],
      replans: 0,
      utility: null,
      path: [],
      pathIndex: 0,
      trail,
      converted: purchasedNpcIds.has(item.id),
      finished
    };
    this.agentCache.set(item.id, agent);
    return agent;
  }
}

function emptyStats() {
  return { spawned: 0, converted: 0, mainBuyers: 0, impulseBuyers: 0, notFound: 0, unreachable: 0, stuckRecoveries: 0 };
}

function emptySnapshot() {
  return { time: 0, revenue: 0, purchases: 0, spawned: 0, active: 0, conversionRate: 0, mainRate: 0, impulseRate: 0, notFoundRate: 0, completed: false };
}
