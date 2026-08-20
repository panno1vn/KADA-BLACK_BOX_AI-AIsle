export const FLOOR_TILE_WORLD_SIZE = 1;
export const MAX_DIRECT_FLOOR_TILES = 20000;

const finitePositive = (value, fallback) => Number.isFinite(value) && value > 0 ? value : fallback;
const clamp = (value, minimum, maximum) => Math.max(minimum, Math.min(maximum, value));

export function layoutBounds(layout) {
  return {
    width: finitePositive(Number(layout?.width), 12),
    height: finitePositive(Number(layout?.height), 8),
  };
}

export function clampShelfToLayout(shelf, layout) {
  const {width, height} = layoutBounds(layout);
  const w = finitePositive(Number(shelf?.w), 1);
  const h = finitePositive(Number(shelf?.h), 1);
  return {
    ...shelf,
    x: clamp(Number.isFinite(Number(shelf?.x)) ? Number(shelf.x) : 0, 0, Math.max(0, width - w)),
    y: clamp(Number.isFinite(Number(shelf?.y)) ? Number(shelf.y) : 0, 0, Math.max(0, height - h)),
    w,
    h,
  };
}

export function rotateShelfInLayout(shelf, layout) {
  const safe = clampShelfToLayout(shelf, layout);
  const centerX = safe.x + safe.w / 2;
  const centerY = safe.y + safe.h / 2;
  const rotated = {
    ...safe,
    x: centerX - safe.h / 2,
    y: centerY - safe.w / 2,
    w: safe.h,
    h: safe.w,
    rotation: ((Number(safe.rotation) || 0) + 90) % 360,
  };
  return clampShelfToLayout(rotated, layout);
}

export function canvasTransformForLayout(layout, canvasWidth, canvasHeight, padding = 24) {
  const {width, height} = layoutBounds(layout);
  const W = finitePositive(Number(canvasWidth), 960);
  const H = finitePositive(Number(canvasHeight), 640);
  const safePadding = clamp(Number.isFinite(padding) ? padding : 24, 0, Math.min(W, H) / 2);
  const scale = Math.max(Number.EPSILON, Math.min(Math.max(1, W - safePadding * 2) / width, Math.max(1, H - safePadding * 2) / height));
  const ox = (W - width * scale) / 2;
  const oy = (H - height * scale) / 2;
  return {sx: scale, sy: scale, scale, ox, oy, W, H, worldMinX: 0, worldMaxX: width, worldMinY: 0, worldMaxY: height};
}

export function canvasViewportTransform(layout, canvasWidth, canvasHeight, viewport = {}, padding = 24) {
  const base = canvasTransformForLayout(layout, canvasWidth, canvasHeight, padding);
  const zoom = clamp(Number.isFinite(Number(viewport.zoom)) ? Number(viewport.zoom) : 3, 0.6, 5);
  const scale = base.scale * zoom;
  const {width, height} = layoutBounds(layout);
  const ox = (base.W - width * scale) / 2 + (Number(viewport.panX) || 0);
  const oy = (base.H - height * scale) / 2 + (Number(viewport.panY) || 0);
  return {...base, sx: scale, sy: scale, scale, ox, oy, zoom};
}

export function zoomViewportAtPoint(layout, canvasWidth, canvasHeight, viewport, point, nextZoom, padding = 24) {
  const before = canvasViewportTransform(layout, canvasWidth, canvasHeight, viewport, padding);
  const zoom = clamp(Number(nextZoom), 0.6, 5);
  if (!Number.isFinite(zoom) || zoom <= 1) return {zoom: Number.isFinite(zoom) ? zoom : 1, panX: 0, panY: 0};
  const px = Number.isFinite(Number(point?.x)) ? Number(point.x) : before.W / 2;
  const py = Number.isFinite(Number(point?.y)) ? Number(point.y) : before.H / 2;
  const worldX = (px - before.ox) / before.scale;
  const worldY = (py - before.oy) / before.scale;
  const {width, height} = layoutBounds(layout);
  const scale = canvasTransformForLayout(layout, canvasWidth, canvasHeight, padding).scale * zoom;
  const centeredOx = (before.W - width * scale) / 2;
  const centeredOy = (before.H - height * scale) / 2;
  const maxPanX = Math.max(0, (width * scale - before.W) / 2);
  const maxPanY = Math.max(0, (height * scale - before.H) / 2);
  return {
    zoom,
    panX: clamp(px - worldX * scale - centeredOx, -maxPanX, maxPanX),
    panY: clamp(py - worldY * scale - centeredOy, -maxPanY, maxPanY),
  };
}

export function panViewportByScreen(layout, canvasWidth, canvasHeight, viewport, deltaX, deltaY, padding = 24) {
  const transform = canvasViewportTransform(layout, canvasWidth, canvasHeight, viewport, padding);
  const {width, height} = layoutBounds(layout);
  const maxPanX = Math.max(0, (width * transform.scale - transform.W) / 2);
  const maxPanY = Math.max(0, (height * transform.scale - transform.H) / 2);
  return {
    zoom: transform.zoom,
    panX: clamp((Number(viewport?.panX) || 0) + Number(deltaX || 0), -maxPanX, maxPanX),
    panY: clamp((Number(viewport?.panY) || 0) + Number(deltaY || 0), -maxPanY, maxPanY),
  };
}

export function expandLegacyFloor(layout, targetWidth = 48, targetHeight = 32) {
  const width = finitePositive(Number(layout?.width), 12);
  const height = finitePositive(Number(layout?.height), 8);
  const supportedSource =
    (Math.abs(width - 12) < 1e-9 && Math.abs(height - 8) < 1e-9) ||
    (Math.abs(width - 24) < 1e-9 && Math.abs(height - 16) < 1e-9);
  if (!supportedSource) return {layout, expanded: false};

  const scaleX = targetWidth / width;
  const scaleY = targetHeight / height;
  const expanded = JSON.parse(JSON.stringify(layout));
  expanded.width = targetWidth;
  expanded.height = targetHeight;
  for (const wall of expanded.walls || []) {
    wall.x1 *= scaleX; wall.x2 *= scaleX;
    wall.y1 *= scaleY; wall.y2 *= scaleY;
  }
  for (const shelf of expanded.shelves || []) {
    shelf.x *= scaleX;
    shelf.y *= scaleY;
  }
  for (const marker of [expanded.entrance, expanded.checkout]) {
    if (!marker) continue;
    marker.x *= scaleX;
    marker.y *= scaleY;
  }
  return {layout: expanded, expanded: true, fromWidth: width, fromHeight: height};
}

export function floorTilePlan(layout, tileSize = FLOOR_TILE_WORLD_SIZE) {
  const {width, height} = layoutBounds(layout);
  const size = finitePositive(Number(tileSize), FLOOR_TILE_WORLD_SIZE);
  const columns = Math.ceil(width / size);
  const rows = Math.ceil(height / size);
  const count = columns * rows;
  return {width, height, tileSize: size, columns, rows, count, usePattern: !Number.isSafeInteger(count) || count > MAX_DIRECT_FLOOR_TILES};
}
