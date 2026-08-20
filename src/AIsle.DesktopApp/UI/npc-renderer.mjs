export const NPC_DIRECTIONS = Object.freeze(['S', 'SW', 'W', 'NW', 'N', 'NE', 'E', 'SE']);
export const NPC_SPRITE_ASSETS = Object.freeze([
  Object.freeze({ id: 'npc_1', src: 'assets/npc/npc_1.png' }),
  Object.freeze({ id: 'npc_2', src: 'assets/npc/npc_2.png' }),
  Object.freeze({ id: 'npc_3', src: 'assets/npc/npc_3.png' }),
  Object.freeze({ id: 'npc_4', src: 'assets/npc/npc_4.png' }),
]);

const COLUMN_COUNT = 4;
const ROW_COUNT = 8;
const DEFAULT_FPS = 8;
const DEFAULT_DIRECTION = 0;

export function stableIdentityHash(runSeed, npcId) {
  const text = `${String(runSeed ?? '')}\u001f${String(npcId ?? '')}`;
  let hash = 2166136261;
  for (let index = 0; index < text.length; index++) {
    hash ^= text.charCodeAt(index);
    hash = Math.imul(hash, 16777619);
  }
  return hash >>> 0;
}

export function selectModelIndex(runSeed, npcId, modelCount) {
  if (!Number.isInteger(modelCount) || modelCount <= 0) throw new RangeError('modelCount must be a positive integer.');
  return stableIdentityHash(runSeed, npcId) % modelCount;
}

export function directionIndexFromDelta(dx, dy, lastDirection = DEFAULT_DIRECTION, movementEpsilon = 0.001) {
  if (![dx, dy, movementEpsilon].every(Number.isFinite) || movementEpsilon < 0) return normalizeDirection(lastDirection);
  if ((dx * dx) + (dy * dy) < movementEpsilon * movementEpsilon) return normalizeDirection(lastDirection);
  const sector = Math.round((Math.atan2(dy, dx) - (Math.PI / 2)) / (Math.PI / 4));
  return ((sector % ROW_COUNT) + ROW_COUNT) % ROW_COUNT;
}

export function walkingFrameAt(animationTimeMs, fps = DEFAULT_FPS, phaseOffset = 0) {
  if (!Number.isFinite(animationTimeMs) || !Number.isFinite(fps) || fps <= 0) return 0;
  const frame = Math.floor(Math.max(0, animationTimeMs) / (1000 / fps) + phaseOffset);
  return ((frame % COLUMN_COUNT) + COLUMN_COUNT) % COLUMN_COUNT;
}

export function validateSpriteDimensions(width, height) {
  if (!Number.isInteger(width) || !Number.isInteger(height) || width <= 0 || height <= 0)
    throw new Error('Sprite dimensions must be positive integers.');
  if (width % COLUMN_COUNT !== 0 || height % ROW_COUNT !== 0)
    throw new Error(`Sprite sheet ${width}x${height} is not divisible into 8 rows x 4 columns.`);
  return { frameWidth: width / COLUMN_COUNT, frameHeight: height / ROW_COUNT };
}

export function resolveSpriteFrame(width, height, direction, frame) {
  const dimensions = validateSpriteDimensions(width, height);
  const safeDirection = normalizeDirection(direction);
  const safeFrame = ((Math.trunc(frame) % COLUMN_COUNT) + COLUMN_COUNT) % COLUMN_COUNT;
  return {
    x: safeFrame * dimensions.frameWidth,
    y: safeDirection * dimensions.frameHeight,
    width: dimensions.frameWidth,
    height: dimensions.frameHeight,
  };
}

export class NpcSpriteRenderer {
  constructor({ assets = NPC_SPRITE_ASSETS, imageFactory, warn, fps = DEFAULT_FPS, movementEpsilon = 0.001 } = {}) {
    this.assets = assets.map(asset => ({ ...asset, image: null, ready: false, error: null }));
    this.imageFactory = imageFactory ?? (() => new Image());
    this.warn = warn ?? (message => console.warn(message));
    this.fps = fps;
    this.movementEpsilon = movementEpsilon;
    this.states = new Map();
    this.runSeed = null;
    this.renderFrame = 0;
    this.animationEpoch = 0;
    this.warned = new Set();
    this.drawCalls = 0;
  }

  async load() {
    await Promise.all(this.assets.map(asset => this.#loadAsset(asset)));
    const ready = this.assets.filter(asset => asset.ready);
    if (ready.length > 1) {
      const reference = `${ready[0].frameWidth}x${ready[0].frameHeight}`;
      for (const asset of ready) {
        if (`${asset.frameWidth}x${asset.frameHeight}` !== reference) {
          asset.ready = false;
          asset.error = new Error(`Sprite frame dimensions differ for ${asset.id}.`);
          this.#warnOnce(asset.error.message);
        }
      }
    }
    return this.assets.filter(asset => asset.ready).length;
  }

  reset(runSeed = null, animationTimeMs = 0) {
    this.states.clear();
    this.runSeed = runSeed;
    this.renderFrame = 0;
    this.animationEpoch = Number.isFinite(animationTimeMs) ? animationTimeMs : 0;
    this.drawCalls = 0;
  }

  draw(ctx, agents, {
    runSeed = this.runSeed,
    animationTimeMs = 0,
    running = false,
    scaleX = 1,
    scaleY = 1,
    selectedId = null,
    fallbackColors = {},
  } = {}) {
    if (!ctx || !Array.isArray(agents)) return { drawCalls: 0, visualStates: this.states.size };
    if (runSeed !== this.runSeed) this.reset(runSeed, animationTimeMs);
    this.renderFrame++;
    this.drawCalls = 0;
    ctx.imageSmoothingEnabled = false;

    const drawable = agents
      .filter(agent => agent && Number.isFinite(agent.x) && Number.isFinite(agent.y) && agent.id != null)
      .sort((left, right) => left.y - right.y || String(left.id).localeCompare(String(right.id)));

    for (const agent of drawable) this.#drawAgent(ctx, agent, animationTimeMs, running, scaleX, scaleY, selectedId, fallbackColors);

    const activeIds = new Set(drawable.map(agent => String(agent.id)));
    for (const [id, state] of this.states) {
      if (!activeIds.has(id) && this.renderFrame - state.lastSeenFrame > 2) this.states.delete(id);
    }
    return { drawCalls: this.drawCalls, visualStates: this.states.size };
  }

  #drawAgent(ctx, agent, animationTimeMs, running, scaleX, scaleY, selectedId, fallbackColors) {
    const id = String(agent.id);
    let state = this.states.get(id);
    if (!state) {
      const modelIndex = selectModelIndex(this.runSeed, id, Math.max(1, this.assets.length));
      state = {
        modelIndex,
        lastX: agent.x,
        lastY: agent.y,
        direction: DEFAULT_DIRECTION,
        phaseOffset: stableIdentityHash(this.runSeed, `${id}:phase`) % COLUMN_COUNT,
        frame: 0,
        lastMovedAt: Number.NEGATIVE_INFINITY,
        lastSeenFrame: this.renderFrame,
      };
      this.states.set(id, state);
    }

    const dx = agent.x - state.lastX;
    const dy = agent.y - state.lastY;
    const moved = (dx * dx) + (dy * dy) >= this.movementEpsilon * this.movementEpsilon;
    const facingOverride = Number.isFinite(agent.facingDx) && Number.isFinite(agent.facingDy)
      && ((agent.facingDx * agent.facingDx) + (agent.facingDy * agent.facingDy) >= this.movementEpsilon * this.movementEpsilon);
    if (facingOverride) {
      state.direction = directionIndexFromDelta(agent.facingDx, agent.facingDy, state.direction, this.movementEpsilon);
    } else if (moved) {
      state.direction = directionIndexFromDelta(dx, dy, state.direction, this.movementEpsilon);
      state.lastMovedAt = animationTimeMs;
    }
    if (!facingOverride && running && (moved || animationTimeMs - state.lastMovedAt <= 250))
      state.frame = walkingFrameAt(animationTimeMs - this.animationEpoch, this.fps, state.phaseOffset);
    state.lastX = agent.x;
    state.lastY = agent.y;
    state.lastSeenFrame = this.renderFrame;

    const screenX = agent.x * scaleX;
    const screenY = agent.y * scaleY;
    const asset = this.assets[state.modelIndex];
    if (!asset?.ready) {
      this.#drawFallback(ctx, screenX, screenY, selectedId != null && String(selectedId) === id, fallbackColors[agent.status]);
      return;
    }

    const crop = resolveSpriteFrame(asset.image.width, asset.image.height, state.direction, state.frame);
    const drawHeight = Math.max(32, Math.min(48, Math.round(Math.min(scaleX, scaleY) * 0.68)));
    const drawWidth = Math.round(drawHeight * crop.width / crop.height);
    const destinationX = Math.round(screenX - drawWidth / 2);
    const destinationY = Math.round(screenY - drawHeight + 2);
    ctx.drawImage(asset.image, crop.x, crop.y, crop.width, crop.height, destinationX, destinationY, drawWidth, drawHeight);
    this.drawCalls++;
    if (selectedId != null && String(selectedId) === id) this.#drawSelection(ctx, screenX, screenY);
  }

  #drawFallback(ctx, x, y, selected, color) {
    ctx.fillStyle = color || '#8a98aa';
    ctx.beginPath();
    ctx.arc(x, y, selected ? 7 : 4.5, 0, Math.PI * 2);
    ctx.fill();
    if (selected) this.#drawSelection(ctx, x, y);
    this.#warnOnce('NPC sprite unavailable; using the dot fallback.');
  }

  #drawSelection(ctx, x, y) {
    ctx.strokeStyle = '#fff';
    ctx.lineWidth = 1.5;
    ctx.beginPath();
    ctx.arc(x, y, 8, 0, Math.PI * 2);
    ctx.stroke();
  }

  async #loadAsset(asset) {
    try {
      const image = this.imageFactory();
      asset.image = image;
      const loaded = new Promise((resolve, reject) => {
        image.onload = resolve;
        image.onerror = () => reject(new Error(`Unable to load NPC sprite: ${asset.src}`));
      });
      image.src = asset.src;
      await loaded;
      const dimensions = validateSpriteDimensions(image.naturalWidth || image.width, image.naturalHeight || image.height);
      asset.frameWidth = dimensions.frameWidth;
      asset.frameHeight = dimensions.frameHeight;
      asset.ready = true;
    } catch (error) {
      asset.error = error instanceof Error ? error : new Error(String(error));
      asset.ready = false;
      this.#warnOnce(asset.error.message);
    }
  }

  #warnOnce(message) {
    if (this.warned.has(message)) return;
    this.warned.add(message);
    this.warn(message);
  }
}

function normalizeDirection(direction) {
  const numeric = Number.isFinite(direction) ? Math.trunc(direction) : DEFAULT_DIRECTION;
  return ((numeric % ROW_COUNT) + ROW_COUNT) % ROW_COUNT;
}
