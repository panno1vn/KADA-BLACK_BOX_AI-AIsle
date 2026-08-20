import test from 'node:test';
import assert from 'node:assert/strict';
import {canvasTransformForLayout, canvasViewportTransform, clampShelfToLayout, expandLegacyFloor, floorTilePlan, panViewportByScreen, rotateShelfInLayout, zoomViewportAtPoint} from '../web/store-geometry.mjs';

test('T11 G1-G3 shelf rotation preserves center, dimensions and four-turn identity', () => {
  const layout={width:12,height:8};
  const original={id:'s',x:4,y:3,w:3,h:1,rotation:0,flipX:true};
  const once=rotateShelfInLayout(original,layout);
  assert.deepEqual([once.x+once.w/2,once.y+once.h/2],[original.x+original.w/2,original.y+original.h/2]);
  assert.deepEqual([once.w,once.h],[original.h,original.w]);
  let current=original;for(let i=0;i<4;i++)current=rotateShelfInLayout(current,layout);
  assert.deepEqual(current,original);
});

test('T11 G4/G7 rotate and far drag clamp to fixed layout without shrinking', () => {
  const layout={width:8,height:6};
  const shelf=clampShelfToLayout({x:1e9,y:-1e9,w:3,h:1},layout);
  assert.deepEqual([shelf.x,shelf.y,shelf.w,shelf.h],[5,0,3,1]);
  const rotated=rotateShelfInLayout({x:7,y:5,w:3,h:1,rotation:0},layout);
  assert.deepEqual([rotated.x,rotated.y,rotated.w,rotated.h],[6,3,1,3]);
});

test('T11 G5-G8 canvas and floor depend only on finite layout bounds', () => {
  const transform=canvasTransformForLayout({width:12,height:8},960,640);
  assert.deepEqual([transform.worldMinX,transform.worldMaxX,transform.worldMinY,transform.worldMaxY],[0,12,0,8]);
  const plan=floorTilePlan({width:12,height:8});
  assert.deepEqual([plan.columns,plan.rows,plan.count,plan.usePattern],[12,8,96,false]);
  const huge=floorTilePlan({width:1e7,height:1e7});
  assert.equal(huge.usePattern,true);
  assert.ok(Object.values(canvasTransformForLayout({width:1e7,height:1e7},960,640)).every(Number.isFinite));
});

test('mouse-wheel viewport zoom is bounded and keeps the cursor world point stable', () => {
  const layout={width:12,height:8},W=1000,H=700,point={x:700,y:300};
  const before=canvasViewportTransform(layout,W,H,{zoom:1,panX:0,panY:0});
  const worldBefore={x:(point.x-before.ox)/before.scale,y:(point.y-before.oy)/before.scale};
  const viewport=zoomViewportAtPoint(layout,W,H,{zoom:1,panX:0,panY:0},point,2);
  const after=canvasViewportTransform(layout,W,H,viewport);
  assert.ok(Math.abs((point.x-after.ox)/after.scale-worldBefore.x)<1e-9);
  assert.ok(Math.abs((point.y-after.oy)/after.scale-worldBefore.y)<1e-9);
  assert.equal(zoomViewportAtPoint(layout,W,H,viewport,point,99).zoom,5);
  assert.deepEqual(zoomViewportAtPoint(layout,W,H,viewport,point,.1),{zoom:.6,panX:0,panY:0});
});

test('viewport defaults to 300% and arrow-key panning stays inside the 500% canvas', () => {
  const layout={width:48,height:32},W=960,H=640;
  assert.equal(canvasViewportTransform(layout,W,H,{}).zoom,3);
  const moved=panViewportByScreen(layout,W,H,{zoom:3,panX:0,panY:0},-72,72);
  assert.deepEqual([moved.panX,moved.panY],[-72,72]);
  const bounded=panViewportByScreen(layout,W,H,{zoom:5,panX:0,panY:0},-1e9,1e9);
  assert.ok(Number.isFinite(bounded.panX)&&Number.isFinite(bounded.panY));
  assert.ok(bounded.panX<0&&bounded.panY>0);
});

test('12x8 and current 24x16 floors migrate to 48x32 without stretching shelves', () => {
  const source={width:12,height:8,walls:[{x1:0,y1:0,x2:12,y2:8}],shelves:[{x:2,y:3,w:3,h:1.8}],entrance:{x:6,y:8},checkout:{x:9,y:7}};
  const result=expandLegacyFloor(source);
  assert.equal(result.expanded,true);
  assert.equal(result.layout.width*result.layout.height,48*32);
  assert.deepEqual(result.layout.shelves[0],{x:8,y:12,w:3,h:1.8});
  assert.deepEqual(result.layout.entrance,{x:24,y:32});
  assert.equal(source.width,12);
  assert.equal(expandLegacyFloor(result.layout).expanded,false);
  const current=expandLegacyFloor({width:24,height:16,shelves:[{x:5,y:4,w:3,h:1.8}]});
  assert.deepEqual(current.layout.shelves[0],{x:10,y:8,w:3,h:1.8});
  assert.deepEqual([current.fromWidth,current.fromHeight],[24,16]);
});
