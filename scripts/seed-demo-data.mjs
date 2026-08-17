// Generates ~3 months of sample simulation history so a fresh clone has something
// to look at in the "📊 Thống Kê" dashboard right away, instead of an empty state.
// Runs the real engine (not fake numbers) and writes straight into runtime/history/,
// which is gitignored — this script is what a new clone runs to reproduce that data locally.
//
// Usage: node scripts/seed-demo-data.mjs
import {mkdir, writeFile, unlink, readdir} from 'node:fs/promises';
import {join, dirname} from 'node:path';
import {fileURLToPath} from 'node:url';
import {DEFAULT_LAYOUT, DEFAULT_CATALOG} from '../web/project-defaults.js';
import {LiveSimulation, generatePopulation, createRng} from '../web/live-engine.js';
import {createSimResult} from '../web/sim-result.js';

const repoRoot = join(dirname(fileURLToPath(import.meta.url)), '..');
const historyDir = join(repoRoot, 'runtime', 'history');
const RUN_COUNT = 48;
const END = new Date(); // "today" — always fresh relative to when you run this
const START = new Date(END.getTime() - 92 * 24 * 3600 * 1000); // ~3 months back

await mkdir(historyDir, {recursive: true});

// Only ever touch our own demo-* files — never a real saved run.
const existing = (await readdir(historyDir)).filter(f => f.startsWith('demo-') && f.endsWith('.json'));
for (const f of existing) await unlink(join(historyDir, f));
if (existing.length) console.log(`Removed ${existing.length} old demo-* files.`);

function randomDates(n) {
  const dates = [];
  for (let i = 0; i < n; i++) {
    const span = END.getTime() - START.getTime();
    const t = START.getTime() + (i / (n - 1)) * span + (Math.random() - 0.5) * (span / n) * 0.8;
    const d = new Date(Math.min(END.getTime() - 1000, Math.max(START.getTime(), t)));
    d.setUTCHours(8 + Math.floor(Math.random() * 11), Math.floor(Math.random() * 60), 0, 0);
    dates.push(d);
  }
  return dates.sort((a, b) => a - b);
}

const dates = randomDates(RUN_COUNT);
let seed = Math.floor(Math.random() * 100000);
let ok = 0;

for (let i = 0; i < dates.length; i++) {
  const date = dates[i];
  const progress = i / (dates.length - 1); // mild growth trend over the window, plus noise
  const noise = (Math.random() - 0.5) * 30;
  const npcCount = Math.max(150, Math.min(200, Math.round(155 + progress * 35 + noise)));
  const durationMinutes = 25 + Math.round(Math.random() * 10);
  const runSeed = seed++;

  const population = generatePopulation(DEFAULT_CATALOG, npcCount, createRng(runSeed));
  const sim = new LiveSimulation({layout: DEFAULT_LAYOUT, catalog: DEFAULT_CATALOG, population, seed: runSeed, durationMinutes});
  let guard = 0;
  while (!sim.completed && guard++ < 20000) sim.step(0.5);

  const result = createSimResult({simulation: sim, name: `Ca chạy ${date.toISOString().slice(0, 10)}`, layout: DEFAULT_LAYOUT, catalog: DEFAULT_CATALOG, populationMode: 'ga'});
  result.id = `demo-${date.toISOString().slice(0, 10).replace(/-/g, '')}-${runSeed}`;
  result.createdAt = date.toISOString();

  await writeFile(join(historyDir, `${result.id}.json`), JSON.stringify(result), 'utf8');
  ok++;
}

console.log(`Seeded ${ok} demo runs (${START.toISOString().slice(0, 10)} → ${END.toISOString().slice(0, 10)}) into runtime/history/.`);
console.log('Start the app (run.bat) and open the "📊 Thống Kê" tab to see it.');
