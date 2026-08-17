// Revenue / conversion / traffic dashboard for the "📊 Thống Kê" tab.
// Reads GET /api/analytics (backend/analytics.mjs) and renders KPI tiles,
// two donut charts and one bar chart, all on <canvas> to match the rest
// of the app (no chart library / npm dependency).

const PALETTE = {blue: '#4f8fd1', gold: '#b87a26', red: '#e05252'};
const INK = {text: '#f5e6c8', dim: '#b8946a', line: '#6b3519'};
const MONO = "'IBM Plex Mono',monospace";
const DOW = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'];

const $ = s => document.querySelector(s);
const panel = () => document.getElementById('dashboard-panel');
const escapeHTML = (s = '') => String(s).replace(/[&<>"']/g, c => ({'&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'}[c]));
const money = n => new Intl.NumberFormat('vi-VN').format(Math.round(n)) + ' ₫';
const pct = n => (n * 100).toFixed(1) + '%';
const count = n => Math.round(n).toLocaleString('vi-VN');

const MAX_DAILY_BARS = 60;

let cache = null;
let granularity = 'monthly'; // 'daily' | 'monthly' | 'quarterly' | 'yearly'
let metric = 'revenue'; // 'revenue' | 'purchases' | 'customers'
let selectedPeriod = null; // a period string from the active granularity's series, or null = all-time totals
let calendarViewDate = null; // {year, month} currently shown in the calendar dialog
let tableVisible = false;
let resizeBound = false;

function currentSeries() {
  const series = cache?.series?.[granularity] || [];
  return granularity === 'daily' ? series.slice(-MAX_DAILY_BARS) : series;
}

function findBucket(gran, period) {
  return (cache?.series?.[gran] || []).find(d => d.period === period) || null;
}

// KPI tiles + donuts read this: the selected period's numbers if one is picked, otherwise the grand total.
function activeStats() {
  if (selectedPeriod) {
    const bucket = findBucket(granularity, selectedPeriod);
    if (bucket) return bucket;
  }
  return cache.totals;
}

function formatPeriodLabel(period, gran = granularity) {
  if (gran === 'daily') { const [, m, d] = period.split('-'); return `${d}/${m}`; }
  if (gran === 'monthly') { const [y, m] = period.split('-'); return `Th${m}/${y.slice(2)}`; }
  if (gran === 'quarterly') { const [y, q] = period.split('-'); return `${q}/${y.slice(2)}`; }
  return period;
}

function scopeLabel() {
  if (!selectedPeriod) return 'Toàn bộ thời gian';
  if (granularity === 'daily') { const [y, m, d] = selectedPeriod.split('-'); return `Ngày ${d}/${m}/${y}`; }
  if (granularity === 'monthly') { const [y, m] = selectedPeriod.split('-'); return `Tháng ${m}/${y}`; }
  if (granularity === 'quarterly') { const [y, q] = selectedPeriod.split('-'); return `${q}/${y}`; }
  return `Năm ${selectedPeriod}`;
}

// avgEmotion from the API is a per-NPC average on a -1..1 scale (Peak-End: mean of each
// customer's peak valence and their end-of-visit valence). Displayed as a friendlier 0-100 index.
const emotionIndex = raw => Math.max(0, Math.min(100, Math.round(((Number(raw) || 0) + 1) / 2 * 100)));
function withEmotionIndex(bucket) { bucket.emotionIndex = emotionIndex(bucket.avgEmotion); return bucket; }

function metricConfig() {
  if (metric === 'customers') return {series: [{key: 'customersIn', label: 'Khách vào', color: PALETTE.blue}, {key: 'customersOut', label: 'Khách ra', color: PALETTE.gold}], format: count};
  if (metric === 'purchases') return {series: [{key: 'purchases', label: 'Lượt mua', color: PALETTE.blue}], format: count};
  if (metric === 'emotion') return {series: [{key: 'emotionIndex', label: 'Chỉ số cảm xúc', color: PALETTE.blue}], format: n => `${Math.round(n)}/100`};
  return {series: [{key: 'revenue', label: 'Doanh thu', color: PALETTE.blue}], format: money};
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

// ---------- tooltip ----------

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

// ---------- donut ----------

function drawDonut(canvasId, slices, centerText, centerSub) {
  const canvas = document.getElementById(canvasId);
  if (!canvas) return;
  const ctx = canvas.getContext('2d');
  const W = canvas.width, H = canvas.height, cx = W / 2, cy = H / 2;
  const rOuter = Math.min(W, H) / 2 - 6, rInner = rOuter * 0.6;
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
      hitSlices.push({start: rel, end: rel + sweep, slice});
      rel += sweep;
    }
  }
  canvas._hit = {cx, cy, rInner, rOuter, slices: hitSlices, total};
  ctx.fillStyle = INK.text; ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
  ctx.font = '700 16px ' + MONO;
  ctx.fillText(centerText, cx, cy - (centerSub ? 7 : 0));
  if (centerSub) { ctx.fillStyle = INK.dim; ctx.font = '8px ' + MONO; ctx.fillText(centerSub, cx, cy + 10); }
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
    showTooltip(e.clientX, e.clientY, `<div>${escapeHTML(found.slice.label)}</div><div><b>${count(found.slice.value)}</b> · ${share}%</div>`);
  };
  canvas.onpointerleave = hideTooltip;
}

function donutLegendHTML(slices, total) {
  return slices.map(s => {
    const share = total ? (Math.max(0, s.value) / total * 100).toFixed(1) : '0.0';
    return `<div class="row"><i class="swatch" style="background:${s.color}"></i>${escapeHTML(s.label)}<b>${count(s.value)} · ${share}%</b></div>`;
  }).join('');
}

function drawConversionDonut() {
  const s = activeStats();
  const converted = s.converted, notConverted = Math.max(0, s.customersIn - s.converted);
  const slices = [{label: 'Đã mua', value: converted, color: PALETTE.blue}, {label: 'Chưa mua', value: notConverted, color: PALETTE.red}];
  drawDonut('donut-conversion', slices, pct(s.conversionRate), 'chuyển đổi');
  const legend = document.getElementById('legend-conversion');
  if (legend) legend.innerHTML = donutLegendHTML(slices, converted + notConverted);
  bindDonutInteractions('donut-conversion');
}

function drawPurchaseDonut() {
  const s = activeStats();
  const main = s.mainBuyers, impulse = s.impulseBuyers, total = main + impulse;
  const slices = [{label: 'Mua theo nhu cầu', value: main, color: PALETTE.gold}, {label: 'Mua bốc đồng', value: impulse, color: PALETTE.blue}];
  drawDonut('donut-purchase', slices, pct(total ? main / total : 0), 'theo nhu cầu');
  const legend = document.getElementById('legend-purchase');
  if (legend) legend.innerHTML = donutLegendHTML(slices, total) +
    '<small style="color:var(--dim);font:400 8px/1.5 var(--mono);display:block;margin-top:4px">*Một khách có thể vừa mua theo nhu cầu vừa mua bốc đồng.</small>';
  bindDonutInteractions('donut-purchase');
}

// ---------- bar chart ----------

function drawBarChart() {
  const canvas = document.getElementById('bar-chart');
  const legendEl = document.getElementById('legend-bar');
  if (!canvas) return;
  const width = Math.max(320, (canvas.parentElement.clientWidth || 900) - 8);
  const height = 260;
  canvas.width = width; canvas.height = height;
  const ctx = canvas.getContext('2d');
  ctx.clearRect(0, 0, width, height);

  const config = metricConfig();
  const data = currentSeries();
  canvas._hit = {config, cols: []};

  if (!data.length) {
    ctx.fillStyle = INK.dim; ctx.font = '10px ' + MONO; ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
    ctx.fillText('Chưa đủ dữ liệu theo kỳ này.', width / 2, height / 2);
    if (legendEl) legendEl.innerHTML = '';
    return;
  }

  const padL = 54, padR = 16, padT = 14, padB = 30;
  const plotW = width - padL - padR, plotH = height - padT - padB;
  const maxValue = niceMax(Math.max(1, ...data.flatMap(d => config.series.map(s => d[s.key] || 0))));
  const ticks = 4;

  ctx.strokeStyle = INK.line; ctx.lineWidth = 1;
  ctx.fillStyle = INK.dim; ctx.font = '8px ' + MONO; ctx.textAlign = 'right'; ctx.textBaseline = 'middle';
  for (let i = 0; i <= ticks; i++) {
    const v = maxValue * i / ticks, y = padT + plotH - (v / maxValue) * plotH;
    ctx.beginPath(); ctx.moveTo(padL, y); ctx.lineTo(padL + plotW, y); ctx.stroke();
    ctx.fillText(compactNumber(v), padL - 8, y);
  }

  const colW = plotW / data.length, maxBarW = 24;
  const labelEvery = Math.max(1, Math.ceil((data.length * 32) / plotW)); // keep x-axis labels from colliding as bar count grows
  data.forEach((d, i) => {
    const colX = padL + i * colW;
    if (d.period === selectedPeriod) {
      ctx.fillStyle = 'rgba(79,143,209,.16)';
      ctx.fillRect(colX + 1, padT, colW - 2, plotH);
    }
    const n = config.series.length;
    const groupW = Math.min(maxBarW * n + (n - 1) * 2, colW * 0.72);
    const barW = (groupW - (n - 1) * 2) / n;
    let bx = colX + (colW - groupW) / 2;
    const bars = [];
    config.series.forEach(s => {
      const value = d[s.key] || 0;
      const h = (value / maxValue) * plotH;
      const y = padT + plotH - h;
      roundedTopRect(ctx, bx, y, barW, h, 4);
      ctx.fillStyle = s.color; ctx.fill();
      bars.push({series: s, value});
      bx += barW + 2;
    });
    canvas._hit.cols.push({x: colX, w: colW, period: d.period, bars});
    if (i % labelEvery === 0 || i === data.length - 1) {
      ctx.fillStyle = d.period === selectedPeriod ? INK.text : INK.dim;
      ctx.font = '8px ' + MONO; ctx.textAlign = 'center'; ctx.textBaseline = 'top';
      ctx.fillText(formatPeriodLabel(d.period), colX + colW / 2, padT + plotH + 8);
    }
  });

  ctx.strokeStyle = INK.line;
  ctx.beginPath(); ctx.moveTo(padL, padT + plotH); ctx.lineTo(padL + plotW, padT + plotH); ctx.stroke();

  if (legendEl) legendEl.innerHTML = config.series.length > 1
    ? config.series.map(s => `<div class="row"><i class="swatch" style="background:${s.color}"></i>${escapeHTML(s.label)}</div>`).join('')
    : '';
}

function bindBarInteractions() {
  const canvas = document.getElementById('bar-chart');
  if (!canvas) return;
  canvas.onpointermove = e => {
    const hit = canvas._hit;
    if (!hit || !hit.cols.length) return;
    const col = hit.cols.find(c => e.offsetX >= c.x && e.offsetX < c.x + c.w);
    if (!col) { hideTooltip(); return; }
    const lines = col.bars.map(b => `<div>${escapeHTML(b.series.label)}: <b>${escapeHTML(hit.config.format(b.value))}</b></div>`).join('');
    showTooltip(e.clientX, e.clientY, `<div>${escapeHTML(formatPeriodLabel(col.period))} <span style="color:var(--dim)">(bấm để xem chi tiết)</span></div>${lines}`);
  };
  canvas.onpointerleave = hideTooltip;
  canvas.onclick = e => {
    const hit = canvas._hit;
    if (!hit || !hit.cols.length) return;
    const col = hit.cols.find(c => e.offsetX >= c.x && e.offsetX < c.x + c.w);
    if (!col) return;
    selectedPeriod = selectedPeriod === col.period ? null : col.period;
    render();
  };
}

// ---------- table view (accessibility: same numbers reachable without hover) ----------

function tableHTML() {
  const rows = currentSeries();
  if (!rows.length) return '<p style="color:var(--dim);font:400 9px var(--mono)">Chưa có dữ liệu.</p>';
  return `<table class="dash-table"><thead><tr><th>Kỳ</th><th>Doanh thu</th><th>Lượt mua</th><th>Khách vào</th><th>Khách ra</th><th>Tỉ lệ chuyển đổi</th><th>Cảm xúc</th></tr></thead><tbody>${rows.map(d => `<tr class="${d.period === selectedPeriod ? 'sel' : ''}"><td>${escapeHTML(formatPeriodLabel(d.period))}</td><td>${money(d.revenue)}</td><td>${count(d.purchases)}</td><td>${count(d.customersIn)}</td><td>${count(d.customersOut)}</td><td>${pct(d.conversionRate)}</td><td>${d.emotionIndex ?? 50}/100</td></tr>`).join('')}</tbody></table>`;
}

// ---------- calendar (pick any day that has data, even outside the visible bar window) ----------

function dailyIndex() {
  return new Map((cache?.series?.daily || []).map(d => [d.period, d]));
}

function shiftMonth({year, month}, delta) {
  let m = month + delta, y = year;
  if (m < 1) { m = 12; y--; } else if (m > 12) { m = 1; y++; }
  return {year: y, month: m};
}

function openCalendar() {
  if (!calendarViewDate) {
    const anchor = (granularity === 'daily' && selectedPeriod) || cache?.series?.daily?.at(-1)?.period;
    if (anchor) { const [y, m] = anchor.split('-'); calendarViewDate = {year: +y, month: +m}; }
    else { const now = new Date(); calendarViewDate = {year: now.getUTCFullYear(), month: now.getUTCMonth() + 1}; }
  }
  renderCalendar();
  document.getElementById('calendar-dialog')?.showModal();
}

function renderCalendar() {
  const body = document.getElementById('calendar-body');
  if (!body) return;
  const {year, month} = calendarViewDate;
  const dailyMap = dailyIndex();
  const first = new Date(Date.UTC(year, month - 1, 1));
  const startWeekday = (first.getUTCDay() + 6) % 7; // Monday = 0
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
    <div class="cal-head">
      <button type="button" id="cal-prev">‹</button>
      <strong>Tháng ${String(month).padStart(2, '0')}/${year}</strong>
      <button type="button" id="cal-next">›</button>
    </div>
    <div class="cal-grid">${DOW.map(d => `<div class="cal-dow">${d}</div>`).join('')}${cells}</div>`;

  document.getElementById('cal-prev').onclick = () => { calendarViewDate = shiftMonth(calendarViewDate, -1); renderCalendar(); };
  document.getElementById('cal-next').onclick = () => { calendarViewDate = shiftMonth(calendarViewDate, 1); renderCalendar(); };
  body.querySelectorAll('.cal-cell.has-data').forEach(btn => {
    btn.onclick = () => {
      granularity = 'daily';
      selectedPeriod = btn.dataset.period;
      document.getElementById('calendar-dialog')?.close();
      render();
    };
  });
}

// ---------- layout ----------

function scopeHTML() {
  return `<div class="dash-scope">
    <span>${selectedPeriod ? '📌' : '🌐'} ${escapeHTML(scopeLabel())}</span>
    <div class="dash-scope-actions">
      ${selectedPeriod ? '<button id="dash-clear-scope">✕ Xem tất cả</button>' : ''}
      <button id="dash-calendar-btn">📅 Chọn ngày</button>
    </div>
  </div>`;
}

function kpiHTML(s) {
  return `<div class="dash-kpis">
    <div><span>DOANH THU</span><b>${money(s.revenue)}</b></div>
    <div><span>KHÁCH VÀO / RA</span><b>${count(s.customersIn)} / ${count(s.customersOut)}</b></div>
    <div><span>LƯỢT MUA</span><b>${count(s.purchases)}</b></div>
    <div><span>TỈ LỆ CHUYỂN ĐỔI</span><b>${pct(s.conversionRate)}</b></div>
    <div><span>CHỈ SỐ CẢM XÚC KHÁCH HÀNG</span><b>${s.emotionIndex ?? 50}/100</b></div>
  </div>`;
}

function dailyCapNote() {
  const total = cache?.series?.daily?.length || 0;
  if (granularity !== 'daily' || total <= MAX_DAILY_BARS) return '';
  return `<div style="color:var(--dim);font:400 8px var(--mono);margin:-4px 0 10px">*Hiển thị ${MAX_DAILY_BARS} ngày gần nhất trong tổng ${total} ngày có dữ liệu — dùng lịch để xem các ngày cũ hơn.</div>`;
}

function groupButtons(role, options, activeValue) {
  return `<div class="group" data-role="${role}">${options.map(([value, label]) =>
    `<button data-value="${value}" class="${value === activeValue ? 'active' : ''}">${label}</button>`).join('')}</div>`;
}

function chartsHTML() {
  return `<div class="dash-charts">
    <div class="dash-card"><h3>TỈ LỆ CHUYỂN ĐỔI (KHÁCH ĐÃ MUA)</h3><div class="dash-donut-body">
      <canvas id="donut-conversion" width="140" height="140"></canvas>
      <div class="dash-legend" id="legend-conversion"></div>
    </div></div>
    <div class="dash-card"><h3>CƠ CẤU MUA HÀNG</h3><div class="dash-donut-body">
      <canvas id="donut-purchase" width="140" height="140"></canvas>
      <div class="dash-legend" id="legend-purchase"></div>
    </div></div>
    <div class="dash-card wide">
      <h3>DOANH THU · LƯỢT MUA · KHÁCH RA VÀO THEO KỲ <span style="color:var(--dim);font-weight:400">(bấm vào cột để xem chi tiết ngày/kỳ đó)</span></h3>
      <div class="dash-controls">
        ${groupButtons('granularity', [['daily', 'Ngày'], ['monthly', 'Tháng'], ['quarterly', 'Quý'], ['yearly', 'Năm']], granularity)}
        ${groupButtons('metric', [['revenue', 'Doanh thu'], ['purchases', 'Lượt mua'], ['customers', 'Khách vào/ra'], ['emotion', 'Cảm xúc']], metric)}
        <div class="dash-table-toggle"><button id="dash-table-btn">${tableVisible ? '▲ Ẩn bảng' : '▤ Xem dạng bảng'}</button></div>
      </div>
      ${dailyCapNote()}
      <canvas id="bar-chart" width="900" height="260"></canvas>
      <div class="dash-legend" id="legend-bar" style="flex-direction:row;gap:16px;margin-top:10px"></div>
      <div class="dash-table-wrap" id="dash-table-wrap" ${tableVisible ? '' : 'hidden'}>${tableVisible ? tableHTML() : ''}</div>
    </div>
  </div>`;
}

function wireControls() {
  const root = panel();
  root.querySelectorAll('.dash-controls .group[data-role="granularity"] button').forEach(btn => {
    btn.onclick = () => { granularity = btn.dataset.value; selectedPeriod = null; render(); };
  });
  root.querySelectorAll('.dash-controls .group[data-role="metric"] button').forEach(btn => {
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
    if (document.body.dataset.tab === 'dashboard' && cache?.totals?.runs) { drawBarChart(); bindBarInteractions(); }
  });
}

function render() {
  const root = panel();
  if (!root) return;
  if (!cache || !cache.totals.runs) {
    root.innerHTML = `<div class="dash-empty"><span>📭</span>Chưa có dữ liệu mô phỏng nào được lưu.<br>Sang tab "🚀 Mô Phỏng &amp; Run Thử", bấm Run live và để mô phỏng chạy tới khi hoàn tất — kết quả sẽ tự lưu vào lịch sử và xuất hiện ở đây.</div>`;
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
  root.innerHTML = '<div class="dash-empty"><span>⏳</span>Đang tải dữ liệu…</div>';
  try {
    const response = await fetch('/api/analytics', {headers: {'Content-Type': 'application/json'}});
    const data = await response.json();
    if (!response.ok) throw new Error(data.error || response.statusText);
    withEmotionIndex(data.totals);
    for (const key of ['daily', 'monthly', 'quarterly', 'yearly']) (data.series[key] || []).forEach(withEmotionIndex);
    cache = data;
  } catch (error) {
    root.innerHTML = `<div class="dash-empty"><span>⚠</span>${escapeHTML(error.message)}</div>`;
    return;
  }
  render();
}
