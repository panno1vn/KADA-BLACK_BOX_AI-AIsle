function periodKeys(createdAt) {
  const date = new Date(createdAt);
  if (Number.isNaN(date.getTime())) return null;
  const year = date.getUTCFullYear();
  const month = date.getUTCMonth() + 1;
  const day = date.getUTCDate();
  const quarter = Math.ceil(month / 3);
  return {
    day: `${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`,
    month: `${year}-${String(month).padStart(2, '0')}`,
    quarter: `${year}-Q${quarter}`,
    year: String(year),
  };
}

function emptyBucket() {
  return {runs: 0, revenue: 0, purchases: 0, customersIn: 0, customersOut: 0, converted: 0, mainBuyers: 0, impulseBuyers: 0, emotionWeighted: 0};
}

function addRun(bucket, run) {
  const s = run.summary || {};
  const spawned = Number(s.spawned) || 0;
  const active = Number(s.active) || 0;
  bucket.runs += 1;
  bucket.revenue += Number(s.revenue) || 0;
  bucket.purchases += Number(s.purchases) || 0;
  bucket.customersIn += spawned;
  bucket.customersOut += Math.max(0, spawned - active);
  bucket.converted += Math.round((Number(s.conversionRate) || 0) * spawned);
  bucket.mainBuyers += Math.round((Number(s.mainRate) || 0) * spawned);
  bucket.impulseBuyers += Math.round((Number(s.impulseRate) || 0) * spawned);
  // avgEmotion is a per-NPC average on a -1..1 scale (runs saved before this field existed contribute 0, i.e. neutral).
  bucket.emotionWeighted += (Number(s.avgEmotion) || 0) * spawned;
}

function finalizeBucket(bucket) {
  return {
    ...bucket,
    conversionRate: bucket.customersIn ? bucket.converted / bucket.customersIn : 0,
    mainRate: bucket.customersIn ? bucket.mainBuyers / bucket.customersIn : 0,
    impulseRate: bucket.customersIn ? bucket.impulseBuyers / bucket.customersIn : 0,
    avgEmotion: bucket.customersIn ? bucket.emotionWeighted / bucket.customersIn : 0,
  };
}

function sortedSeries(map) {
  return [...map.entries()]
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([period, bucket]) => ({period, ...finalizeBucket(bucket)}));
}

// runs: the array returned by project-store's listHistory() — one summary per saved SimResult.
export function buildAnalytics(runs = []) {
  const totals = emptyBucket();
  const daily = new Map(), monthly = new Map(), quarterly = new Map(), yearly = new Map();

  for (const run of runs) {
    addRun(totals, run);
    const keys = periodKeys(run.createdAt);
    if (!keys) continue;
    for (const [map, key] of [[daily, keys.day], [monthly, keys.month], [quarterly, keys.quarter], [yearly, keys.year]]) {
      if (!map.has(key)) map.set(key, emptyBucket());
      addRun(map.get(key), run);
    }
  }

  return {
    totals: finalizeBucket(totals),
    series: {
      daily: sortedSeries(daily),
      monthly: sortedSeries(monthly),
      quarterly: sortedSeries(quarterly),
      yearly: sortedSeries(yearly),
    },
  };
}
