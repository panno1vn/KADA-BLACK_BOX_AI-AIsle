// Module Dashboard Analytics trực quan hóa biểu đồ cho Desktop App (Dựa trên code của Khôi)
// Hỗ trợ KPI tiles, 2 biểu đồ Donut và 1 biểu đồ Bar chart đa năng trên Canvas thuần túy.

const PALETTE = { blue: '#4f8fd1', gold: '#b87a26', red: '#e05252', green: '#5dba4f' };
const INK = { text: '#f5e6c8', dim: '#b8946a', line: '#6b3519', bg: '#1c1007' };
const MONO = "'Nunito Sans', sans-serif";
const DOW = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'];

const $ = s => document.querySelector(s);
const panel = () => document.getElementById('dashboard-panel');
const escapeHTML = (s = '') => String(s).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
const money = n => new Intl.NumberFormat('vi-VN').format(Math.round(n)) + ' ₫';
const pct = n => (n * 100).toFixed(1) + '%';
const count = n => Math.round(n).toLocaleString('vi-VN');

const MAX_DAILY_BARS = 60;

let cache = null;
let granularity = 'daily'; // 'daily' | 'monthly' | 'quarterly' | 'yearly'
let metric = 'revenue'; // 'revenue' | 'purchases' | 'customers' | 'emotion'
let selectedPeriod = null;
let calendarViewDate = null;
let tableVisible = false;
let resizeBound = false;

// ---------- Tính toán số liệu Analytics ----------

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
  return { runs: 0, revenue: 0, purchases: 0, customersIn: 0, customersOut: 0, converted: 0, mainBuyers: 0, impulseBuyers: 0, emotionWeighted: 0 };
}

function addRun(bucket, run) {
  const s = run.summary || run.kpis || run || {};
  const spawned = Number(s.spawned || s.customersIn || s.totalNpc || s.total_customers || s.totalCustomers) || 0;
  const active = Number(s.active) || 0;
  const revenue = Number(s.revenue || s.totalRevenue || s.total_revenue) || 0;
  const purchases = Number(s.purchases || s.totalPurchases || s.total_purchases) || 0;
  const converted = Number(s.converted || s.purchasersCount || s.total_buyers) || (purchases > 0 ? Math.min(spawned, purchases) : 0);
  const mainBuyers = Number(s.mainBuyers || s.needBuyers) || Math.round(converted * 0.65);
  const impulseBuyers = Number(s.impulseBuyers) || Math.max(0, converted - mainBuyers);
  const avgEmotion = Number(s.avgEmotion ?? s.averageValence ?? s.avg_valence ?? 0.2);

  bucket.runs += 1;
  bucket.revenue += revenue;
  bucket.purchases += purchases;
  bucket.customersIn += spawned;
  bucket.customersOut += Math.max(0, spawned - active);
  bucket.converted += converted;
  bucket.mainBuyers += mainBuyers;
  bucket.impulseBuyers += impulseBuyers;
  bucket.emotionWeighted += avgEmotion * (spawned || 1);
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
    .map(([period, bucket]) => ({ period, ...finalizeBucket(bucket) }));
}

function emotionIndex(raw) {
  return Math.max(0, Math.min(100, Math.round(((Number(raw) || 0) + 1) / 2 * 100)));
}

function withEmotionIndex(bucket) {
  bucket.emotionIndex = emotionIndex(bucket.avgEmotion);
  return bucket;
}

export function buildAnalytics(runs = []) {
  const totals = emptyBucket();
  const daily = new Map(), monthly = new Map(), quarterly = new Map(), yearly = new Map();

  for (const run of runs) {
    addRun(totals, run);
    const keys = periodKeys(run.createdAt || run.timestamp || Date.now());
    if (!keys) continue;
    for (const [map, key] of [[daily, keys.day], [monthly, keys.month], [quarterly, keys.quarter], [yearly, keys.year]]) {
      if (!map.has(key)) map.set(key, emptyBucket());
      addRun(map.get(key), run);
    }
  }

  const result = {
    totals: finalizeBucket(totals),
    series: {
      daily: sortedSeries(daily),
      monthly: sortedSeries(monthly),
      quarterly: sortedSeries(quarterly),
      yearly: sortedSeries(yearly),
    },
  };

  withEmotionIndex(result.totals);
  for (const key of ['daily', 'monthly', 'quarterly', 'yearly']) {
    (result.series[key] || []).forEach(withEmotionIndex);
  }
  return result;
}

// ---------- Biểu đồ & Hiển thị ----------

function currentSeries() {
  const series = cache?.series?.[granularity] || [];
  return granularity === 'daily' ? series.slice(-MAX_DAILY_BARS) : series;
}

function findBucket(gran, period) {
  return (cache?.series?.[gran] || []).find(d => d.period === period) || null;
}

function activeStats() {
  if (selectedPeriod) {
    const bucket = findBucket(granularity, selectedPeriod);
    if (bucket) return bucket;
  }
  return cache.totals;
}

function formatPeriodLabel(period, gran = granularity) {
  if (!period) return '';
  if (gran === 'daily') { const parts = period.split('-'); return `${parts[2]}/${parts[1]}`; }
  if (gran === 'monthly') { const parts = period.split('-'); return `Th${parts[1]}/${parts[0].slice(2)}`; }
  if (gran === 'quarterly') { const parts = period.split('-'); return `${parts[1]}/${parts[0].slice(2)}`; }
  return period;
}

function scopeLabel() {
  if (!selectedPeriod) return 'Toàn bộ các phiên mô phỏng';
  if (granularity === 'daily') { const parts = selectedPeriod.split('-'); return `Ngày ${parts[2]}/${parts[1]}/${parts[0]}`; }
  if (granularity === 'monthly') { const parts = selectedPeriod.split('-'); return `Tháng ${parts[1]}/${parts[0]}`; }
  if (granularity === 'quarterly') { const parts = selectedPeriod.split('-'); return `${parts[1]}/${parts[0]}`; }
  return `Năm ${selectedPeriod}`;
}

function metricConfig() {
  if (metric === 'customers') return { series: [{ key: 'customersIn', label: 'Khách vào', color: PALETTE.blue }, { key: 'customersOut', label: 'Khách ra', color: PALETTE.gold }], format: count };
  if (metric === 'purchases') return { series: [{ key: 'purchases', label: 'Lượt mua', color: PALETTE.blue }], format: count };
  if (metric === 'emotion') return { series: [{ key: 'emotionIndex', label: 'Chỉ số cảm xúc', color: PALETTE.green }], format: n => `${Math.round(n)}/100` };
  return { series: [{ key: 'revenue', label: 'Doanh thu', color: PALETTE.gold }], format: money };
}

function compactNumber(n) {
  if (n >= 1e9) return (n / 1e9).toFixed(1).replace(/\.0$/, '') + 'tỷ';
  if (n >= 1e6) return (n / 1e6).toFixed(1).replace(/\.0$/, '') + 'tr';
  if (n >= 1e3) return (n / 1e3).toFixed(1).replace(/\.0$/, '') + 'k';
  return String(Math.round(n));
}

function niceMax(raw) {
  if (raw <= 0) return 1;
  const magnitude = Math.pow(10, Math.floor(Math.log10(raw)));
  const norm = raw / magnitude;
  const step = norm <= 1 ? 1 : norm <= 2 ? 2 : norm <= 5 ? 5 : 10;
  return step * magnitude;
}

function roundedTopRect(ctx, x, y, w, h, r) {
  if (w <= 0 || h <= 0) return;
  r = Math.min(r, w / 2, h);
  ctx.beginPath();
  ctx.moveTo(x, y + h);
  ctx.lineTo(x, y + r);
  ctx.arcTo(x, y, x + r, y, r);
  ctx.lineTo(x + w - r, y);
  ctx.arcTo(x + w, y, x + w, y + r, r);
  ctx.lineTo(x + w, y + h);
  ctx.closePath();
}

// ---------- Tooltip ----------

function tooltipEl() { return document.getElementById('chart-tooltip'); }
function showTooltip(clientX, clientY, html) {
  const t = tooltipEl(); if (!t) return;
  t.hidden = false; t.innerHTML = html;
  const rect = t.getBoundingClientRect(); const pad = 14;
  let left = clientX + pad, top = clientY + pad;
  if (left + rect.width > window.innerWidth) left = clientX - rect.width - pad;
  if (top + rect.height > window.innerHeight) top = clientY - rect.height - pad;
  t.style.left = Math.max(4, left) + 'px'; t.style.top = Math.max(4, top) + 'px';
}
function hideTooltip() { const t = tooltipEl(); if (t) t.hidden = true; }

// ---------- Biểu đồ Donut ----------

function drawDonut(canvasId, slices, centerText, centerSub) {
  const canvas = document.getElementById(canvasId);
  if (!canvas) return;
  const ctx = canvas.getContext('2d');
  const W = canvas.width, H = canvas.height, cx = W / 2, cy = H / 2;
  const rOuter = Math.min(W, H) / 2 - 4, rInner = rOuter * 0.62;
  ctx.clearRect(0, 0, W, H);
  const total = slices.reduce((s, x) => s + Math.max(0, x.value), 0);
  const hitSlices = [];
  if (!total) {
    ctx.strokeStyle = INK.line; ctx.lineWidth = rOuter - rInner;
    ctx.beginPath(); ctx.arc(cx, cy, (rOuter + rInner) / 2, 0, Math.PI * 2); ctx.stroke();
  } else {
    const gap = 0.035;
    let rel = 0;
    for (const slice of slices) {
      const value = Math.max(0, slice.value);
      const sweep = (value / total) * Math.PI * 2;
      const start = rel + gap / 2, end = rel + sweep - gap / 2;
      if (end > start) {
        ctx.beginPath();
        ctx.arc(cx, cy, rOuter, start - Math.PI / 2, end - Math.PI / 2);
        ctx.arc(cx, cy, rInner, end - Math.PI / 2, start - Math.PI / 2, true);
        ctx.closePath();
        ctx.fillStyle = slice.color; ctx.fill();
      }
      hitSlices.push({ start: rel, end: rel + sweep, slice });
      rel += sweep;
    }
  }
  canvas._hit = { cx, cy, rInner, rOuter, slices: hitSlices, total };
  ctx.fillStyle = INK.text; ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
  ctx.font = '700 13px ' + MONO;
  ctx.fillText(centerText, cx, cy - (centerSub ? 6 : 0));
  if (centerSub) { ctx.fillStyle = INK.dim; ctx.font = '9px ' + MONO; ctx.fillText(centerSub, cx, cy + 8); }
}

function bindDonutInteractions(canvasId) {
  const canvas = document.getElementById(canvasId);
  if (!canvas) return;
  canvas.onpointermove = e => {
    const hit = canvas._hit;
    if (!hit || !hit.total) { hideTooltip(); return; }
    const dx = e.offsetX - hit.cx, dy = e.offsetY - hit.cy, dist = Math.hypot(dx, dy);
    if (dist < hit.rInner || dist > hit.rOuter) { hideTooltip(); return; }
    let rel = Math.atan2(dy, dx) + Math.PI / 2; if (rel < 0) rel += Math.PI * 2;
    const found = hit.slices.find(s => rel >= s.start && rel < s.end);
    if (!found) { hideTooltip(); return; }
    const share = (Math.max(0, found.slice.value) / hit.total * 100).toFixed(1);
    showTooltip(e.clientX, e.clientY, `<div><b>${escapeHTML(found.slice.label)}</b></div><div>${count(found.slice.value)} lượt · ${share}%</div>`);
  };
  canvas.onpointerleave = hideTooltip;
}

function donutLegendHTML(slices, total) {
  return slices.map(s => {
    const share = total ? (Math.max(0, s.value) / total * 100).toFixed(1) : '0.0';
    return `<div class="flex items-center gap-1.5 text-[11px] text-on-surface-variant"><i class="w-2.5 h-2.5 rounded-xs shrink-0" style="background:${s.color}"></i><span class="flex-1">${escapeHTML(s.label)}</span><b class="text-on-surface">${count(s.value)} · ${share}%</b></div>`;
  }).join('');
}

function drawConversionDonut() {
  const s = activeStats();
  const converted = s.converted, notConverted = Math.max(0, s.customersIn - s.converted);
  const slices = [{ label: 'Đã mua', value: converted, color: PALETTE.blue }, { label: 'Chưa mua', value: notConverted, color: PALETTE.red }];
  drawDonut('donut-conversion', slices, pct(s.conversionRate), 'chuyển đổi');
  const legend = document.getElementById('legend-conversion');
  if (legend) legend.innerHTML = donutLegendHTML(slices, converted + notConverted);
  bindDonutInteractions('donut-conversion');
}

function drawPurchaseDonut() {
  const s = activeStats();
  const main = s.mainBuyers, impulse = s.impulseBuyers, total = main + impulse;
  const slices = [{ label: 'Theo nhu cầu', value: main, color: PALETTE.gold }, { label: 'Bốc đồng', value: impulse, color: PALETTE.blue }];
  drawDonut('donut-purchase', slices, pct(total ? main / total : 0), 'theo nhu cầu');
  const legend = document.getElementById('legend-purchase');
  if (legend) legend.innerHTML = donutLegendHTML(slices, total) +
    '<div class="text-[9px] text-on-surface-variant italic mt-0.5">*Một khách có thể vừa mua theo nhu cầu vừa mua bốc đồng.</div>';
  bindDonutInteractions('donut-purchase');
}

// ---------- Biểu đồ Cột đa năng (Bar Chart) ----------

function drawBarChart() {
  const canvas = document.getElementById('bar-chart');
  const legendEl = document.getElementById('legend-bar');
  if (!canvas) return;
  const width = Math.max(320, (canvas.parentElement.clientWidth || 900) - 8);
  const height = 190;
  canvas.width = width; canvas.height = height;
  const ctx = canvas.getContext('2d');
  ctx.clearRect(0, 0, width, height);

  const config = metricConfig();
  const data = currentSeries();
  canvas._hit = { config, cols: [] };

  if (!data.length) {
    ctx.fillStyle = INK.dim; ctx.font = '11px ' + MONO; ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
    ctx.fillText('Chưa đủ dữ liệu theo kỳ này.', width / 2, height / 2);
    if (legendEl) legendEl.innerHTML = '';
    return;
  }

  const padL = 55, padR = 15, padT = 14, padB = 26;
  const plotW = width - padL - padR, plotH = height - padT - padB;
  const maxValue = niceMax(Math.max(1, ...data.flatMap(d => config.series.map(s => d[s.key] || 0))));
  const ticks = 3;

  ctx.strokeStyle = INK.line; ctx.lineWidth = 1;
  ctx.fillStyle = INK.dim; ctx.font = '9px ' + MONO; ctx.textAlign = 'right'; ctx.textBaseline = 'middle';
  for (let i = 0; i <= ticks; i++) {
    const v = maxValue * i / ticks, y = padT + plotH - (v / maxValue) * plotH;
    ctx.beginPath(); ctx.moveTo(padL, y); ctx.lineTo(padL + plotW, y); ctx.stroke();
    ctx.fillText(compactNumber(v), padL - 6, y);
  }

  const colW = plotW / data.length, maxBarW = 24;
  const labelEvery = Math.max(1, Math.ceil((data.length * 36) / plotW));
  data.forEach((d, i) => {
    const colX = padL + i * colW;
    if (d.period === selectedPeriod) {
      ctx.fillStyle = 'rgba(79,143,209,.2)';
      ctx.fillRect(colX + 1, padT, colW - 2, plotH);
    }
    const n = config.series.length;
    const groupW = Math.min(maxBarW * n + (n - 1) * 2, colW * 0.75);
    const barW = (groupW - (n - 1) * 2) / n;
    let bx = colX + (colW - groupW) / 2;
    const bars = [];
    config.series.forEach(s => {
      const value = d[s.key] || 0;
      const h = (value / maxValue) * plotH;
      const y = padT + plotH - h;
      roundedTopRect(ctx, bx, y, barW, h, 3);
      ctx.fillStyle = s.color; ctx.fill();
      bars.push({ series: s, value });
      bx += barW + 2;
    });
    canvas._hit.cols.push({ x: colX, w: colW, period: d.period, bars });
    if (i % labelEvery === 0 || i === data.length - 1) {
      ctx.fillStyle = d.period === selectedPeriod ? INK.text : INK.dim;
      ctx.font = '9px ' + MONO; ctx.textAlign = 'center'; ctx.textBaseline = 'top';
      ctx.fillText(formatPeriodLabel(d.period), colX + colW / 2, padT + plotH + 6);
    }
  });

  ctx.strokeStyle = INK.line;
  ctx.beginPath(); ctx.moveTo(padL, padT + plotH); ctx.lineTo(padL + plotW, padT + plotH); ctx.stroke();

  if (legendEl) {
    legendEl.innerHTML = config.series.length > 1
      ? config.series.map(s => `<div class="flex items-center gap-1.5 text-xs text-on-surface-variant"><i class="w-2.5 h-2.5 rounded-xs shrink-0" style="background:${s.color}"></i>${escapeHTML(s.label)}</div>`).join('')
      : '';
  }
}

function bindBarInteractions() {
  const canvas = document.getElementById('bar-chart');
  if (!canvas) return;
  canvas.onpointermove = e => {
    const hit = canvas._hit;
    if (!hit || !hit.cols?.length) { hideTooltip(); return; }
    const x = e.offsetX;
    const col = hit.cols.find(c => x >= c.x && x < c.x + c.w);
    if (!col) { hideTooltip(); return; }
    const lines = col.bars.map(b => `<div>${escapeHTML(b.series.label)}: <b>${hit.config.format(b.value)}</b></div>`).join('');
    showTooltip(e.clientX, e.clientY, `<div><b>${escapeHTML(formatPeriodLabel(col.period))}</b></div>${lines}`);
  };
  canvas.onpointerleave = hideTooltip;
  canvas.onclick = e => {
    const hit = canvas._hit;
    if (!hit || !hit.cols?.length) return;
    const x = e.offsetX;
    const col = hit.cols.find(c => x >= c.x && x < c.x + c.w);
    if (!col) return;
    selectedPeriod = selectedPeriod === col.period ? null : col.period;
    render();
  };
}

// ---------- Bảng số liệu chi tiết ----------

function tableHTML() {
  const data = currentSeries();
  if (!data.length) return '<div class="text-xs text-on-surface-variant italic p-4 text-center">Không có dữ liệu.</div>';
  const rows = data.map(d => {
    const isSel = d.period === selectedPeriod;
    return `<tr class="${isSel ? 'bg-primary-container/20 font-bold' : 'hover:bg-surface-container-low'} border-b border-outline-variant/40">
      <td class="p-2 text-left">${escapeHTML(formatPeriodLabel(d.period))}</td>
      <td class="p-2 text-right">${money(d.revenue)}</td>
      <td class="p-2 text-right">${count(d.customersIn)} / ${count(d.customersOut)}</td>
      <td class="p-2 text-right">${count(d.purchases)}</td>
      <td class="p-2 text-right">${pct(d.conversionRate)}</td>
      <td class="p-2 text-right">${d.emotionIndex}/100</td>
    </tr>`;
  }).join('');

  return `<table class="w-full text-xs text-on-surface border-collapse mt-2">
    <thead>
      <tr class="border-b border-outline-variant text-on-surface-variant font-bold">
        <th class="p-2 text-left">Thời gian</th>
        <th class="p-2 text-right">Doanh thu</th>
        <th class="p-2 text-right">Khách vào/ra</th>
        <th class="p-2 text-right">Lượt mua</th>
        <th class="p-2 text-right">Tỉ lệ chuyển đổi</th>
        <th class="p-2 text-right">Cảm xúc</th>
      </tr>
    </thead>
    <tbody>${rows}</tbody>
  </table>`;
}

// ---------- Lịch chọn ngày (Calendar) ----------

function dailyIndex() {
  return new Map((cache?.series?.daily || []).map(d => [d.period, d]));
}

function shiftMonth({ year, month }, delta) {
  let m = month + delta, y = year;
  if (m < 1) { m = 12; y--; } else if (m > 12) { m = 1; y++; }
  return { year: y, month: m };
}

function openCalendar() {
  if (!calendarViewDate) {
    const anchor = (granularity === 'daily' && selectedPeriod) || cache?.series?.daily?.at(-1)?.period;
    if (anchor) { const [y, m] = anchor.split('-'); calendarViewDate = { year: +y, month: +m }; }
    else { const now = new Date(); calendarViewDate = { year: now.getUTCFullYear(), month: now.getUTCMonth() + 1 }; }
  }
  renderCalendar();
  document.getElementById('calendar-dialog')?.showModal();
}

function renderCalendar() {
  const body = document.getElementById('calendar-body');
  if (!body) return;
  const { year, month } = calendarViewDate;
  const dailyMap = dailyIndex();
  const first = new Date(Date.UTC(year, month - 1, 1));
  const startWeekday = (first.getUTCDay() + 6) % 7;
  const daysInMonth = new Date(Date.UTC(year, month, 0)).getUTCDate();

  let cells = '';
  for (let i = 0; i < startWeekday; i++) cells += '<div class="cal-cell empty"></div>';
  for (let day = 1; day <= daysInMonth; day++) {
    const period = `${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
    const bucket = dailyMap.get(period);
    const isSelected = granularity === 'daily' && selectedPeriod === period;
    const cls = ['cal-cell', bucket ? 'has-data' : 'no-data', isSelected ? 'selected' : ''].filter(Boolean).join(' ');
    cells += `<button type="button" class="${cls}" data-period="${period}" ${bucket ? '' : 'disabled'}>${day}${bucket ? '<i></i>' : ''}</button>`;
  }

  body.innerHTML = `
    <div class="cal-head flex justify-between items-center mb-3 font-bold text-sm text-on-surface">
      <button type="button" id="cal-prev" class="px-2 py-1 bg-surface-container rounded border border-outline-variant hover:bg-surface-container-high">‹</button>
      <span>Tháng ${String(month).padStart(2, '0')}/${year}</span>
      <button type="button" id="cal-next" class="px-2 py-1 bg-surface-container rounded border border-outline-variant hover:bg-surface-container-high">›</button>
    </div>
    <div class="cal-grid grid grid-cols-7 gap-1">${DOW.map(d => `<div class="text-center font-bold text-[10px] text-on-surface-variant py-1">${d}</div>`).join('')}${cells}</div>
    <div class="mt-4 flex justify-end">
      <button type="button" id="cal-close" class="px-4 py-1.5 bg-primary text-on-primary text-xs rounded-lg font-bold">Đóng</button>
    </div>`;

  document.getElementById('cal-prev').onclick = () => { calendarViewDate = shiftMonth(calendarViewDate, -1); renderCalendar(); };
  document.getElementById('cal-next').onclick = () => { calendarViewDate = shiftMonth(calendarViewDate, 1); renderCalendar(); };
  document.getElementById('cal-close').onclick = () => document.getElementById('calendar-dialog')?.close();
  body.querySelectorAll('.cal-cell.has-data').forEach(btn => {
    btn.onclick = () => {
      granularity = 'daily';
      selectedPeriod = btn.dataset.period;
      document.getElementById('calendar-dialog')?.close();
      render();
    };
  });
}

// ---------- Giao diện chính của Dashboard ----------

function scopeHTML() {
  return `<div class="flex flex-wrap items-center justify-between gap-2 mb-3 bg-surface-container-low py-2 px-3.5 rounded-xl border border-outline-variant">
    <div class="flex items-center gap-1.5 font-bold text-xs text-primary">
      <span class="material-symbols-outlined text-sm">${selectedPeriod ? 'filter_alt' : 'public'}</span>
      <span>${escapeHTML(scopeLabel())}</span>
    </div>
    <div class="flex items-center gap-2">
      ${selectedPeriod ? '<button id="dash-clear-scope" class="px-2.5 py-1 rounded-lg bg-error-container text-on-error-container text-[11px] font-bold hover:bg-error hover:text-white transition-colors cursor-pointer">✕ Xem toàn bộ</button>' : ''}
      <button id="dash-calendar-btn" class="px-2.5 py-1 rounded-lg bg-surface border border-outline-variant text-on-surface text-[11px] font-bold hover:bg-surface-container transition-colors flex items-center gap-1 cursor-pointer">
        <span class="material-symbols-outlined text-xs">calendar_month</span> Chọn ngày
      </button>
    </div>
  </div>`;
}

function kpiHTML(s) {
  return `<div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-2.5 mb-3">
    <div class="tactile-card p-2.5 px-3.5 flex flex-col justify-between rounded-xl bg-surface border border-outline-variant shadow-xs">
      <span class="text-[10px] font-bold text-on-surface-variant uppercase tracking-wider flex items-center gap-1"><span class="material-symbols-outlined text-xs text-primary">payments</span> Doanh thu</span>
      <b class="text-base font-extrabold text-primary mt-0.5">${money(s.revenue)}</b>
    </div>
    <div class="tactile-card p-2.5 px-3.5 flex flex-col justify-between rounded-xl bg-surface border border-outline-variant shadow-xs">
      <span class="text-[10px] font-bold text-on-surface-variant uppercase tracking-wider flex items-center gap-1"><span class="material-symbols-outlined text-xs text-primary">group</span> Khách vào / Ra</span>
      <b class="text-base font-extrabold text-primary mt-0.5">${count(s.customersIn)} / ${count(s.customersOut)}</b>
    </div>
    <div class="tactile-card p-2.5 px-3.5 flex flex-col justify-between rounded-xl bg-surface border border-outline-variant shadow-xs">
      <span class="text-[10px] font-bold text-on-surface-variant uppercase tracking-wider flex items-center gap-1"><span class="material-symbols-outlined text-xs text-primary">shopping_bag</span> Lượt mua</span>
      <b class="text-base font-extrabold text-primary mt-0.5">${count(s.purchases)}</b>
    </div>
    <div class="tactile-card p-2.5 px-3.5 flex flex-col justify-between rounded-xl bg-surface border border-outline-variant shadow-xs">
      <span class="text-[10px] font-bold text-on-surface-variant uppercase tracking-wider flex items-center gap-1"><span class="material-symbols-outlined text-xs text-primary">trending_up</span> Tỉ lệ chuyển đổi</span>
      <b class="text-base font-extrabold text-primary mt-0.5">${pct(s.conversionRate)}</b>
    </div>
    <div class="tactile-card p-2.5 px-3.5 flex flex-col justify-between rounded-xl bg-surface border border-outline-variant shadow-xs col-span-2 sm:col-span-1">
      <span class="text-[10px] font-bold text-on-surface-variant uppercase tracking-wider flex items-center gap-1"><span class="material-symbols-outlined text-xs text-primary">sentiment_satisfied</span> Cảm xúc khách</span>
      <b class="text-base font-extrabold text-emerald-600 mt-0.5">${s.emotionIndex ?? 50}/100</b>
    </div>
  </div>`;
}

function groupButtons(role, options, activeValue) {
  return `<div class="flex items-center gap-0.5 bg-surface-container p-0.5 rounded-lg border border-outline-variant" data-role="${role}">
    ${options.map(([value, label]) => `<button type="button" data-value="${value}" class="${value === activeValue ? 'bg-surface font-bold text-primary shadow-xs border border-outline-variant' : 'text-on-surface-variant hover:bg-surface/60'} px-2.5 py-0.5 rounded-md text-[11px] transition-colors cursor-pointer">${label}</button>`).join('')}
  </div>`;
}

function chartsHTML() {
  return `<div class="grid grid-cols-1 lg:grid-cols-2 gap-3 mb-3">
    <!-- Donut 1: Tỉ lệ chuyển đổi -->
    <div class="tactile-card p-3.5 rounded-xl bg-surface border border-outline-variant shadow-xs flex flex-col">
      <h3 class="font-bold text-xs text-primary mb-2 flex items-center gap-1.5">
        <span class="material-symbols-outlined text-sm">donut_large</span> TỶ LỆ CHUYỂN ĐỔI (KHÁCH ĐÃ MUA)
      </h3>
      <div class="flex flex-row items-center gap-4 flex-1 justify-center">
        <canvas id="donut-conversion" width="110" height="110" class="shrink-0 cursor-pointer"></canvas>
        <div id="legend-conversion" class="flex flex-col gap-1.5 w-full max-w-[200px]"></div>
      </div>
    </div>

    <!-- Donut 2: Cơ cấu mua hàng -->
    <div class="tactile-card p-3.5 rounded-xl bg-surface border border-outline-variant shadow-xs flex flex-col">
      <h3 class="font-bold text-xs text-primary mb-2 flex items-center gap-1.5">
        <span class="material-symbols-outlined text-sm">pie_chart</span> CƠ CẤU MUA HÀNG
      </h3>
      <div class="flex flex-row items-center gap-4 flex-1 justify-center">
        <canvas id="donut-purchase" width="110" height="110" class="shrink-0 cursor-pointer"></canvas>
        <div id="legend-purchase" class="flex flex-col gap-1.5 w-full max-w-[200px]"></div>
      </div>
    </div>

    <!-- Bar Chart đa năng -->
    <div class="tactile-card p-3.5 rounded-xl bg-surface border border-outline-variant shadow-xs lg:col-span-2 flex flex-col">
      <div class="flex flex-wrap items-center justify-between gap-2 mb-2">
        <h3 class="font-bold text-xs text-primary flex items-center gap-1.5">
          <span class="material-symbols-outlined text-sm">bar_chart</span> DOANH THU &amp; HOẠT ĐỘNG THEO KỲ
          <span class="text-[10px] font-normal text-on-surface-variant">(bấm vào cột để xem chi tiết)</span>
        </h3>
        <div class="flex flex-wrap items-center gap-2">
          ${groupButtons('granularity', [['daily', 'Ngày'], ['monthly', 'Tháng'], ['quarterly', 'Quý'], ['yearly', 'Năm']], granularity)}
          ${groupButtons('metric', [['revenue', 'Doanh thu'], ['purchases', 'Lượt mua'], ['customers', 'Khách vào/ra'], ['emotion', 'Cảm xúc']], metric)}
          <button id="dash-table-btn" class="px-2.5 py-0.5 bg-surface-container text-on-surface border border-outline-variant rounded-lg text-[11px] font-bold hover:bg-surface-container-high transition-colors cursor-pointer">
            ${tableVisible ? '▲ Ẩn bảng' : '▤ Xem dạng bảng'}
          </button>
        </div>
      </div>
      <div class="w-full overflow-x-auto">
        <canvas id="bar-chart" width="900" height="190" class="w-full min-w-[550px]"></canvas>
      </div>
      <div id="legend-bar" class="flex items-center gap-3 mt-1.5 justify-center"></div>
      <div id="dash-table-wrap" class="mt-2 ${tableVisible ? '' : 'hidden'} overflow-x-auto">${tableVisible ? tableHTML() : ''}</div>
    </div>
  </div>`;
}

function wireControls() {
  const root = panel();
  if (!root) return;
  root.querySelectorAll('[data-role="granularity"] button').forEach(btn => {
    btn.onclick = () => { granularity = btn.dataset.value; selectedPeriod = null; render(); };
  });
  root.querySelectorAll('[data-role="metric"] button').forEach(btn => {
    btn.onclick = () => { metric = btn.dataset.value; render(); };
  });
  const tableBtn = root.querySelector('#dash-table-btn');
  if (tableBtn) tableBtn.onclick = () => { tableVisible = !tableVisible; render(); };
  const clearBtn = root.querySelector('#dash-clear-scope');
  if (clearBtn) clearBtn.onclick = () => { selectedPeriod = null; render(); };
  const calBtn = root.querySelector('#dash-calendar-btn');
  if (calBtn) calBtn.onclick = openCalendar;
}

function ensureResizeListener() {
  if (resizeBound) return;
  resizeBound = true;
  window.addEventListener('resize', () => {
    if (document.body.dataset.tab === 'analytics' && cache?.totals?.runs) {
      drawBarChart();
      bindBarInteractions();
    }
  });
}

function render() {
  const root = panel();
  if (!root) return;
  if (!cache || !cache.totals || !cache.totals.runs) {
    root.innerHTML = `<div class="flex flex-col items-center justify-center p-12 text-center text-on-surface-variant bg-surface rounded-3xl border border-outline-variant my-8">
      <span class="material-symbols-outlined text-6xl text-primary mb-3">analytics</span>
      <h3 class="text-lg font-bold text-on-surface mb-1">Chưa có dữ liệu phân tích</h3>
      <p class="text-xs max-w-md mb-4">Lịch sử hiện đang trống. Hãy chuyển sang tab <b>"Mô phỏng"</b> và bấm <b>Chạy trực tiếp</b> để bắt đầu chạy khách hàng. Kết quả sẽ tự động lưu lại và vẽ biểu đồ tại đây.</p>
      <button id="dash-empty-run-btn" type="button" class="px-5 py-2.5 bg-primary text-on-primary text-xs font-bold rounded-full hover:scale-105 transition-transform flex items-center gap-1.5 cursor-pointer">
        <span class="material-symbols-outlined text-sm">play_arrow</span> Sang Mô Phỏng Ngay
      </button>
    </div>`;
    const emptyBtn = root.querySelector('#dash-empty-run-btn');
    if (emptyBtn && typeof window.switchTab === 'function') {
      emptyBtn.onclick = () => window.switchTab('simulate');
    }
    return;
  }
  root.innerHTML = scopeHTML() + kpiHTML(activeStats()) + chartsHTML();
  wireControls();
  drawConversionDonut();
  drawPurchaseDonut();
  drawBarChart();
  bindBarInteractions();
  ensureResizeListener();
}

export async function loadDashboard() {
  const root = panel();
  if (!root) return;
  root.innerHTML = '<div class="flex items-center justify-center p-12 text-xs font-bold text-on-surface-variant"><span class="material-symbols-outlined animate-spin mr-2">progress_activity</span> Đang tổng hợp dữ liệu phân tích…</div>';

  let rawRuns = [];

  // 1. Thử lấy từ C# Desktop App qua Bridge
  try {
    if (window.aisleBridge && typeof window.aisleBridge.request === 'function') {
      const res = await window.aisleBridge.request('history.list');
      const items = res?.items || res?.Items || [];
      if (Array.isArray(items) && items.length > 0) {
        rawRuns = items;
      }
    }
  } catch (e) {
    console.warn('Bridge history.list error in dashboard:', e);
  }

  // 2. Thử lấy từ localStorage ('aisle_history_runs' và 'sim-history-list')
  if (!rawRuns.length) {
    try {
      const rawAisle = localStorage.getItem('aisle_history_runs');
      if (rawAisle) {
        const parsed = JSON.parse(rawAisle);
        if (Array.isArray(parsed) && parsed.length > 0) rawRuns = parsed;
      }
    } catch (e) {}
  }
  if (!rawRuns.length) {
    try {
      const rawSim = localStorage.getItem('sim-history-list');
      if (rawSim) {
        const parsed = JSON.parse(rawSim);
        if (Array.isArray(parsed) && parsed.length > 0) rawRuns = parsed;
      }
    } catch (e) {}
  }

  // 3. Thử gọi API backend /api/history nếu có
  if (!rawRuns.length) {
    try {
      const res = await fetch('/api/history');
      if (res.ok) {
        const data = await res.json();
        if (Array.isArray(data.runs) && data.runs.length > 0) rawRuns = data.runs;
      }
    } catch {}
  }

  cache = buildAnalytics(rawRuns);
  render();
}


