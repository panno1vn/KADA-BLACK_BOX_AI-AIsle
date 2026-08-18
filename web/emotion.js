// "😊 Cảm Xúc" tab — live facial emotion via the browser's own webcam, analyzed by the
// Python demo service in services/VideoAnalytics/EmotionRecognition/ (DeepFace, 3-way
// simplified: vui/buon/trung_tinh). Internal demo only — see that folder's README.md.

const SERVICE_URL = 'http://127.0.0.1:8801';
const POLL_MS = 2200; // matches the service's measured ~1.8s/frame CPU inference time

const LABEL = {vui: 'Vui', buon: 'Buồn', trung_tinh: 'Trung tính'};
const COLOR = {vui: '#5dba4f', buon: '#e05252', trung_tinh: '#b8946a'};
const ICON = {vui: '😄', buon: '😢', trung_tinh: '😐'};

let stream = null;
let timer = null;
let busy = false;
let canvas = null;

const escapeHTML = (s = '') => String(s).replace(/[&<>"']/g, c => ({'&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'}[c]));

function panel() { return document.getElementById('emotion-panel'); }

function shell() {
  return `<div class="emo-wrap">
    <div class="emo-camera">
      <video id="emo-video" autoplay playsinline muted></video>
      <div class="emo-badge" id="emo-badge">—</div>
    </div>
    <div class="emo-card">
      <h3>CẢM XÚC KHÁCH HÀNG (DEMO)</h3>
      <p class="emo-status" id="emo-status">Đang chờ bật camera…</p>
      <div class="emo-bars" id="emo-bars">${['vui', 'buon', 'trung_tinh'].map(barRow).join('')}</div>
      <p class="emo-hint">Chỉ chạy thử nghiệm nội bộ — không lưu ảnh nào. Cần service Python đang chạy ở <code>${SERVICE_URL}</code> (xem <code>services/VideoAnalytics/EmotionRecognition/README.md</code>).</p>
    </div>
  </div>`;
}

function barRow(key) {
  return `<div class="emo-bar-row"><span class="emo-bar-icon">${ICON[key]}</span><span class="emo-bar-label">${LABEL[key]}</span><span class="emo-bar-track"><span class="emo-bar-fill" id="emo-fill-${key}" style="background:${COLOR[key]};width:0%"></span></span><span class="emo-bar-value" id="emo-value-${key}">0%</span></div>`;
}

function setStatus(text) {
  const el = document.getElementById('emo-status');
  if (el) el.textContent = text;
}

function render(data) {
  const badge = document.getElementById('emo-badge');
  if (!data.faceCount) {
    setStatus('Không thấy khuôn mặt trong khung hình.');
    if (badge) { badge.textContent = '—'; badge.style.color = '#b8946a'; }
    for (const key of ['vui', 'buon', 'trung_tinh']) {
      document.getElementById(`emo-fill-${key}`).style.width = '0%';
      document.getElementById(`emo-value-${key}`).textContent = '0%';
    }
    return;
  }
  const face = data.faces[0];
  setStatus(`Phát hiện ${data.faceCount} khuôn mặt · cập nhật mỗi ${(POLL_MS / 1000).toFixed(1)}s`);
  if (badge) {
    badge.textContent = `${ICON[face.dominantEmotion]} ${LABEL[face.dominantEmotion]}`;
    badge.style.color = COLOR[face.dominantEmotion];
  }
  for (const key of ['vui', 'buon', 'trung_tinh']) {
    const value = face.scores[key] ?? 0;
    document.getElementById(`emo-fill-${key}`).style.width = `${value}%`;
    document.getElementById(`emo-value-${key}`).textContent = `${value.toFixed(1)}%`;
  }
}

async function tick() {
  if (busy) return;
  const video = document.getElementById('emo-video');
  if (!video || !video.videoWidth) return;
  busy = true;
  try {
    if (!canvas) canvas = document.createElement('canvas');
    canvas.width = video.videoWidth; canvas.height = video.videoHeight;
    canvas.getContext('2d').drawImage(video, 0, 0);
    const blob = await new Promise(resolve => canvas.toBlob(resolve, 'image/jpeg', 0.85));
    const form = new FormData();
    form.append('file', blob, 'frame.jpg');
    const response = await fetch(`${SERVICE_URL}/analyze`, {method: 'POST', body: form});
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    render(await response.json());
  } catch (error) {
    setStatus(`Không kết nối được service cảm xúc (${escapeHTML(error.message)}). Chạy service rồi thử lại — xem services/VideoAnalytics/EmotionRecognition/README.md.`);
  } finally {
    busy = false;
  }
}

export async function startEmotionTab() {
  const root = panel();
  if (!root) return;
  root.innerHTML = shell();
  const video = document.getElementById('emo-video');
  try {
    stream = await navigator.mediaDevices.getUserMedia({video: {width: 480, height: 360}});
    video.srcObject = stream;
    setStatus('Camera bật — đang phân tích…');
    timer = setInterval(tick, POLL_MS);
  } catch (error) {
    setStatus(`Không mở được camera: ${escapeHTML(error.message)}`);
  }
}

export function stopEmotionTab() {
  if (timer) { clearInterval(timer); timer = null; }
  if (stream) { stream.getTracks().forEach(track => track.stop()); stream = null; }
}
