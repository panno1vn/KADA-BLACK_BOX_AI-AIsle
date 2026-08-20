import test from 'node:test';
import assert from 'node:assert/strict';
import {createSimulationMusic} from '../web/simulation-music.mjs';

test('one music instance uses the source volume, starts on enter and pauses on leave', async () => {
  let created=0,warnings=0;
  const fake={loop:false,volume:1,plays:0,pauses:0,play(){this.plays++;return Promise.resolve()},pause(){this.pauses++},addEventListener(){}};
  const music=createSimulationMusic({audioFactory:()=>{created++;return fake},warn:()=>warnings++});
  assert.equal(created,1);assert.equal(fake.loop,true);assert.equal(fake.volume,1);assert.equal(fake.plays,0);
  music.enter();await Promise.resolve();music.leave();
  assert.equal(fake.plays,1);assert.equal(fake.pauses,1);assert.equal(warnings,0);
});

test('T11 music failure warns once', async () => {
  let warnings=0;
  const fake={play(){return Promise.reject(new Error('blocked'))},pause(){},addEventListener(){}};
  const music=createSimulationMusic({audioFactory:()=>fake,warn:()=>warnings++});
  music.enter();music.enter();await new Promise(resolve=>setTimeout(resolve,0));
  assert.equal(warnings,1);
});
