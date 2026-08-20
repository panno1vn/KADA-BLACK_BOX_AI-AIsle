import {DEFAULT_PARAMETERS, manualPopulation} from './live-engine.js';
import {NativeSimulationAdapter} from './native-simulation.mjs';
import {NpcSpriteRenderer, NPC_SPRITE_ASSETS} from './npc-renderer.mjs';
import {validateLayout} from './layout-validation.js';
import {loadDashboard} from './dashboard.js';

const $=s=>document.querySelector(s),$$=s=>[...document.querySelectorAll(s)];
const clamp=(v,a,b)=>Math.max(a,Math.min(b,v));
const pointDistance=(a,b)=>Math.hypot(a.x-b.x,a.y-b.y);
function pointSegmentDistance(p,a,b){const dx=b.x-a.x,dy=b.y-a.y,l2=dx*dx+dy*dy,t=l2?clamp(((p.x-a.x)*dx+(p.y-a.y)*dy)/l2,0,1):0;return pointDistance(p,{x:a.x+t*dx,y:a.y+t*dy})}
const money=n=>new Intl.NumberFormat('vi-VN').format(Math.round(n))+' ₫';
const pct=n=>(n*100).toFixed(1)+'%';
const escapeHTML=(s='')=>String(s).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));

export const SHELF_PRESETS={
  standard:{w:2.0,h:1.0,label:'Kệ đôi tiêu chuẩn (2.0m × 1.0m)'},
  cooler:{w:1.0,h:1.0,label:'Tủ mát / Kệ vuông (1.0m × 1.0m)'},
  endcap:{w:1.0,h:2.0,label:'Kệ đầu dãy / Slim (1.0m × 2.0m)'}
};

export const CATEGORY_NAMES={
  'beverage':'Đồ uống',
  'dry-food':'Hàng khô',
  'instant-food':'Đồ ăn nhanh',
  'frozen-food':'Hàng đông lạnh',
  'fresh-food':'Hàng tươi sống',
  'snack':'Bánh kẹo / Snack',
  'personal-care':'Chăm sóc cá nhân',
  'cleaning':'Dung dịch vệ sinh',
  'household':'Đồ gia dụng',
  'candy':'Kẹo & Gum',
  'other':'Khác'
};

let layout,catalog,simulation=null,simResult=null,manualRows=[],parameters={...DEFAULT_PARAMETERS};
let selected=null,tool='select',draft=null,drag=null,playing=false,lastFrame=0,dirty=true;
let lastRunSeed=null;
let liveHistory=[],maxRecordedTime=0,rewindTime=null;
let historySortMode='date';
const colors={WAITING:'#8b6b4a',DECIDING:'#a87bca',TRANSIT:'#5fa8d3',QUEUE:'#d59b45',DWELL:'#ffca58',PURCHASED:'#5dba4f',CHECKOUT:'#e05252',LEAVING:'#e05252'};
const npcRenderer=new NpcSpriteRenderer({assets:NPC_SPRITE_ASSETS});

const STORE_ASSETS = {
  floor: { src: 'assets/asset/san.jpg', img: new Image(), ready: false },
  wall: { src: 'assets/asset/wall.png', img: new Image(), ready: false },
  entrance: { src: 'assets/asset/cua_vao.png', img: new Image(), ready: false },
  checkout: { src: 'assets/asset/quay_thu_ngan.png', img: new Image(), ready: false },
  beverage: { src: 'assets/asset/do_uong.jpg', img: new Image(), ready: false },
  'instant-food': { src: 'assets/asset/hang_tuoi_song.png', img: new Image(), ready: false },
  'fresh-food': { src: 'assets/asset/hang_tuoi_song.png', img: new Image(), ready: false },
  'frozen-food': { src: 'assets/asset/hang_tuoi_song.png', img: new Image(), ready: false },
  snack: { src: 'assets/asset/snack.png', img: new Image(), ready: false },
  candy: { src: 'assets/asset/snack.png', img: new Image(), ready: false },
  'personal-care': { src: 'assets/asset/hang_kho_cham_soc_ca_nhan.png', img: new Image(), ready: false },
  'dry-food': { src: 'assets/asset/hang_kho_cham_soc_ca_nhan.png', img: new Image(), ready: false },
  cleaning: { src: 'assets/asset/hoa_pham.png', img: new Image(), ready: false },
  household: { src: 'assets/asset/hoa_pham.png', img: new Image(), ready: false },
  defaultShelf: { src: 'assets/asset/hang_kho_cham_soc_ca_nhan.png', img: new Image(), ready: false }
};

const SHELF_CATEGORY_DIMENSIONS = {
  beverage: { w: 1.2, h: 1.6 },
  'instant-food': { w: 2.0, h: 1.3 },
  'fresh-food': { w: 2.0, h: 1.3 },
  'frozen-food': { w: 2.0, h: 1.3 },
  snack: { w: 0.7, h: 1.8 },
  candy: { w: 0.7, h: 1.8 },
  'personal-care': { w: 3.0, h: 1.8 },
  'dry-food': { w: 3.0, h: 1.8 },
  cleaning: { w: 1.2, h: 1.6 },
  household: { w: 1.2, h: 1.6 },
  other: { w: 2.0, h: 1.4 }
};

// These are layout objects, not decorative map pins. Their stored point is
// the centre, keeping the visual, hit area and simulation approach aligned.
const STORE_ENTITY_DIMENSIONS = {
  entrance: { w: 1.8, h: 1.6 },
  checkout: { w: 1.0, h: 2.4 }
};
function entityBounds(type, point){
  const size=STORE_ENTITY_DIMENSIONS[type];
  return point&&size?{x:point.x-size.w/2,y:point.y-size.h/2,w:size.w,h:size.h}:null;
}
function checkoutApproachPoint(point){
  const size=STORE_ENTITY_DIMENSIONS.checkout;
  return point?{x:point.x,y:point.y+size.h/2+.35}:null;
}

Object.values(STORE_ASSETS).forEach(asset => {
  asset.img.onload = () => { asset.ready = true; if(typeof draw === 'function') draw(); };
  asset.img.onerror = () => { asset.ready = false; };
  asset.img.src = asset.src;
});

function getShelfAsset(shelf) {
  if (!shelf) return STORE_ASSETS.defaultShelf;
  const cat = (shelf.category || '').toLowerCase();
  const label = (shelf.label || '').toLowerCase();
  const id = (shelf.id || '').toLowerCase();
  if (cat === 'beverage' || label.includes('uống') || id === 's1') return STORE_ASSETS.beverage;
  if (cat === 'instant-food' || cat === 'fresh-food' || cat === 'frozen-food' || label.includes('tươi') || label.includes('nhanh') || id === 's2') return STORE_ASSETS['instant-food'];
  if (cat === 'snack' || cat === 'candy' || label.includes('snack') || label.includes('kẹo') || id === 's3' || id === 's6') return STORE_ASSETS.snack;
  if (cat === 'personal-care' || cat === 'dry-food' || label.includes('cá nhân') || label.includes('khô') || id === 's4') return STORE_ASSETS['personal-care'];
  if (cat === 'cleaning' || cat === 'household' || label.includes('hóa phẩm') || label.includes('gia dụng') || id === 's5') return STORE_ASSETS.household;
  return STORE_ASSETS[cat] || STORE_ASSETS.defaultShelf;
}

let floorPattern = null;
let currentTab='welcome';
let lastPurchaseCount=0;
let lastFinishedCount=0;
const cashierMoodsSmile=['Dạ em chào quý khách ạ!','Dạ em cảm ơn quý khách ạ!','Dạ em nhận được tiền rồi ạ.','Em gửi lại hóa đơn cho mình ạ.','Quý khách có dùng thẻ tích điểm không ạ?','Dạ để em tính tiền cho mình nhé.','Dạ em thanh toán xong rồi ạ.','Cảm ơn quý khách, chúc quý khách ngày mới tốt lành!'];
const cashierMoodsSad=['Dạ em cảm ơn quý khách đã ghé ạ.','Em chào quý khách, hẹn gặp lại ạ!'];
const TAB_ORDER={welcome:0,setup:1,simulate:2,results:3,load:3,analytics:4};
function switchTab(tab){
  if(tab!==currentTab && currentTab==='simulate' && playing){
    playing=false;
    simulation?.pause().catch(()=>{});
    $('#play-btn').textContent='▶ Chạy trực tiếp';
    $('#stage-status').textContent='TẠM DỪNG';
  }

  const oldIndex=TAB_ORDER[currentTab]??1;
  const newIndex=TAB_ORDER[tab]??1;
  const isForward=newIndex>oldIndex;

  currentTab=tab;
  document.body.dataset.tab=tab;
  $$('.tab-btn').forEach(b=>b.classList.toggle('active',b.dataset.tab===tab));
  $$('.nav-pill-btn').forEach(b=>b.classList.toggle('active',b.dataset.navTab===tab));
  const screenMap={
    welcome:'screen-welcome',
    setup:'screen-setup',
    simulate:'screen-simulate',
    results:'screen-results',
    load:'screen-results',
    analytics:'screen-analytics'
  };
  const targetScreen=screenMap[tab];
  if(targetScreen){
    document.querySelectorAll('.screen').forEach(s=>s.classList.remove('active','slide-from-right','slide-from-left'));
    const el=document.getElementById(targetScreen);
    if(el){
      el.classList.add('active',isForward?'slide-from-right':'slide-from-left');
    }
    const canvas=$('#scene');
    if(canvas){
      if(tab==='setup'){
        const wrap=$('#canvas-wrapper');
        if(wrap && canvas.parentElement!==wrap) wrap.appendChild(canvas);
      }else if(tab==='simulate'){
        const wrap=$('#sim-canvas-container');
        if(wrap && canvas.parentElement!==wrap) wrap.appendChild(canvas);
      }
    }
    if(targetScreen==='screen-results'){
      loadHistoryList();
    }
    if(targetScreen==='screen-analytics'){
      if(simulation && ((simulation.time || 0) > 0.5 || (simulation.snapshot?.().spawned || 0) > 0)){
        saveSimulationSession(simulation, true).then(() => loadDashboard()).catch(() => loadDashboard());
      } else {
        loadDashboard();
      }
    }
  }
  if(tab==='setup'){
    updateToolButtons('select');
    $('#stage-status').textContent='EDIT MODE';
    selected=null;
    renderObjects();
    renderInspector();
  }
  if(tab==='simulate'){
    selected=null;
    renderInspector();
    $('#stage-status').textContent=simulation&&!dirty?(playing?'ĐANG CHẠY MÔ PHỎNG':'SẴN SÀNG'):'SẴN SÀNG';
  }
  requestAnimationFrame(()=>{resizeCanvas();draw()});
  setTimeout(()=>{resizeCanvas();draw();}, 330);
}
window.switchTab = switchTab;
function triggerCashierReaction(type='smile'){
  const avatar=$('#cashier-avatar'),mood=$('#cashier-mood');
  if(!avatar)return;
  const imgIdle=avatar.querySelector('.cashier-img-idle');
  const imgSmile=avatar.querySelector('.cashier-img-smile');
  const imgSad=avatar.querySelector('.cashier-img-sad');
  [imgIdle,imgSmile,imgSad].forEach(img=>img?.classList.add('hidden'));
  avatar.classList.remove('cashier-react','cashier-smile','cashier-sad');
  mood?.classList.remove('happy','sad');
  void avatar.offsetWidth;
  if(type==='smile'){
    if(imgSmile)imgSmile.classList.remove('hidden');else if(imgIdle)imgIdle.classList.remove('hidden');
    avatar.classList.add('cashier-smile');
    if(mood){mood.textContent=cashierMoodsSmile[Math.floor(Math.random()*cashierMoodsSmile.length)];mood.classList.add('happy')}
  }else if(type==='sad'){
    if(imgSad)imgSad.classList.remove('hidden');else if(imgIdle)imgIdle.classList.remove('hidden');
    avatar.classList.add('cashier-sad');
    if(mood){mood.textContent=cashierMoodsSad[Math.floor(Math.random()*cashierMoodsSad.length)];mood.classList.add('sad')}
  }else{
    if(imgIdle)imgIdle.classList.remove('hidden');
    if(mood)mood.textContent='Đang trực quầy...';
  }
  clearTimeout(triggerCashierReaction.t);
  triggerCashierReaction.t=setTimeout(()=>{
    [imgSmile,imgSad].forEach(img=>img?.classList.add('hidden'));
    imgIdle?.classList.remove('hidden');
    avatar.classList.remove('cashier-react','cashier-smile','cashier-sad');
    if(mood){mood.textContent='Đang trực quầy...';mood.classList.remove('happy','sad')}
  },2800);
}
function updateCashier(){
  if(!simulation)return;
  const s=simulation.snapshot();
  const served=simulation.stats.converted||0;
  const rev=s.revenue||0;
  const servedEl=$('#cashier-served');
  const revEl=$('#cashier-revenue');
  if(servedEl)servedEl.textContent=served;
  if(revEl)revEl.textContent=money(rev);
  
  const checkoutPos=checkoutApproachPoint(layout?.checkout);
  if(!checkoutPos)return;
  
  // Chỉ kích hoạt khi khách hàng thực sự đã đứng trước mặt quầy thu ngân.
  const customerAtCounter=simulation.agents?.find(a=>{
    if(a.finished)return false;
    const dist=Math.hypot(a.x-checkoutPos.x,a.y-checkoutPos.y);
    return dist<=0.4;
  });
  
  if(customerAtCounter){
    if(!customerAtCounter._cashierTriggered){
      customerAtCounter._cashierTriggered=true;
      triggerCashierReaction('smile');
    }
  }
}

async function api(path,options={}){const response=await fetch(path,{headers:{'Content-Type':'application/json'},...options});const data=await response.json();if(!response.ok)throw new Error(data.error||response.statusText);return data}
function toast(text){const e=$('#toast');e.textContent=text;e.classList.add('show');clearTimeout(toast.t);toast.t=setTimeout(()=>e.classList.remove('show'),1800)}
function markDirty(message='Inputs changed · press Run live to rebuild'){dirty=true;playing=false;simulation?.pause().catch(error=>showSystemEvent(error.message));$('#play-btn').textContent='▶ Run live';$('#stage-status').textContent='EDIT MODE';showSystemEvent(message)}
function showSystemEvent(message){$('#event-log-list').innerHTML=`<span>${escapeHTML(message)}</span>`}

function showLayoutWarningModal(title, items, isError = false) {
  const dialog = $('#layout-warning-dialog');
  const msgBox = $('#layout-warning-messages');
  const titleEl = $('#layout-warning-title');
  if (!dialog || !msgBox) {
    toast('⚠️ ' + items.join(' | '));
    return;
  }
  if (titleEl) titleEl.textContent = title || (isError ? 'LỖI SƠ ĐỒ CỬA HÀNG' : 'CẢNH BÁO BỐ TRÍ CỬA HÀNG');
  msgBox.innerHTML = items.map(msg => `
    <div class="flex items-start gap-2.5">
      <span class="material-symbols-outlined text-amber-600 text-lg shrink-0 mt-0.5" style="font-variation-settings: 'FILL' 1;">${isError ? 'error' : 'warning'}</span>
      <span class="font-semibold text-sm leading-relaxed">${escapeHTML(msg)}</span>
    </div>
  `).join('');
  try {
    dialog.showModal();
  } catch {
    dialog.setAttribute('open', '');
  }
}

function checkLayoutAndNotify(){
  if(!layout)return true;
  const validation=validateLayout(layout,parameters);
  if(!validation.valid){
    const errorMsg='❌ '+(validation.errors||[]).join('; ');
    showLayoutWarningModal('LỖI BỐ TRÍ CỬA HÀNG', validation.errors||[], true);
    showSystemEvent(errorMsg);
    return false;
  }
  if(validation.warnings?.length){
    const warnMsg='⚠️ '+(validation.warnings||[]).join(' | ');
    showLayoutWarningModal('CẢNH BÁO BỐ TRÍ CỬA HÀNG', validation.warnings||[], false);
    showSystemEvent(warnMsg);
  }
  return true;
}

async function init(){const project=await api('/api/project');layout=project.layout;catalog=project.catalog;await npcRenderer.load();bind();buildParameterLab();switchTab('welcome');renderObjects();renderInspector();draw();loadHistoryList();showSystemEvent('Ready. One click on Run live starts both the engine and visualization.')}

function updateToolButtons(activeTool){
  tool=activeTool;
  $$('[data-tool]').forEach(b=>{
    const isActive=b.dataset.tool===activeTool;
    b.classList.toggle('active',isActive);
  });
}

function bind(){
  $$('[data-tool]').forEach(button=>button.onclick=()=>{updateToolButtons(button.dataset.tool);$('#stage-status').textContent='EDIT MODE'});
  const npcCount=$('#npc-count');if(npcCount)npcCount.oninput=e=>{$('#npc-output').textContent=e.target.value;markDirty()};
  const durationInput=$('#duration');if(durationInput)durationInput.oninput=e=>{$('#duration-output').textContent=e.target.value+' min';markDirty()};
  const popMode=$('#population-mode');if(popMode)popMode.onchange=()=>{$('#npc-count').disabled=$('#population-mode').value==='manual';markDirty()};
  const manualBtn=$('#manual-btn');if(manualBtn)manualBtn.onclick=openManual;
  const applyManualBtn=$('#apply-manual');if(applyManualBtn)applyManualBtn.onclick=applyManual;
  const btnParam=$('#parameter-btn');if(btnParam)btnParam.onclick=()=>{$('#parameter-dialog').showModal()};
  const btnOut=$('#output-toggle');if(btnOut)btnOut.onclick=()=>{const collapsed=document.body.classList.toggle('outputs-collapsed');btnOut.setAttribute('aria-pressed',String(collapsed));btnOut.textContent=collapsed?'▤ Show output':'▤ Output';requestAnimationFrame(resizeCanvas)};
  const applyParam=$('#apply-parameters');if(applyParam)applyParam.onclick=applyParameters;
  const resetParam=$('#reset-parameters');if(resetParam)resetParam.onclick=()=>{parameters={...DEFAULT_PARAMETERS};buildParameterLab()};
  const playBtn=$('#play-btn');if(playBtn)playBtn.onclick=toggleRun;
  const resetBtn=$('#reset-btn');if(resetBtn)resetBtn.onclick=resetSimulation;
  const btnStep=$('#step-btn');if(btnStep)btnStep.onclick=singleStep;
  const speedSel=$('#speed');if(speedSel)speedSel.onchange=async()=>{try{if(simulation)await simulation.setSpeed(Number($('#speed').value));showSystemEvent(`Playback speed ${$('#speed').value}×. Physics tick remains ${parameters.tickSeconds}s.`)}catch(error){showSystemEvent(error.message)}};
  const timelineEl=$('#timeline');if(timelineEl){timelineEl.oninput=timelineEl.onchange=e=>seekTo(Number(e.target.value)/1000*durationSeconds());}
  const addWallBtn=$('#add-wall');if(addWallBtn)addWallBtn.onclick=()=>{pushUndoState();const id='w'+Date.now();layout.walls.push({id,x1:4,y1:3,x2:6,y2:3});selected={type:'wall',id};renderObjects();renderInspector();markDirty('Wall added.');draw();saveProject()};
  const addShelfBtn=$('#add-shelf');if(addShelfBtn)addShelfBtn.onclick=()=>{pushUndoState();const id='s'+Date.now();const category='dry-food';const dims=SHELF_CATEGORY_DIMENSIONS[category]||{w:3.0,h:1.8};const label=CATEGORY_NAMES[category]||'Hàng khô';layout.shelves.push({id,label,presetId:'standard',category,x:4,y:3,w:dims.w,h:dims.h,valence:.2});selected={type:'shelf',id};renderObjects();renderInspector();markDirty('Kệ hàng mới đã được thêm.');draw();saveProject()};
  const exportBtn=$('#export-btn');if(exportBtn)exportBtn.onclick=exportSimulation;
  const canvas=$('#scene');
  if(canvas){
    canvas.onpointerdown=pointerDown;
    canvas.onpointermove=pointerMove;
    canvas.onpointerup=pointerUp;
    new ResizeObserver(resizeCanvas).observe(canvas);
  }
  ['shelf-category','shelf-valence'].forEach(id=>{const el=$('#'+id);if(el)el.oninput=el.onchange=()=>{pushUndoState();updateShelf();}});
  ['wall-x1','wall-y1','wall-x2','wall-y2'].forEach(id=>{const el=$('#'+id);if(el)el.oninput=()=>{pushUndoState();updateWall();};});
  const btnShelfRot=$('#btn-shelf-rotate');if(btnShelfRot)btnShelfRot.onclick=rotateSelectedShelf;
  const btnShelfFlipH=$('#btn-shelf-flip-h');if(btnShelfFlipH)btnShelfFlipH.onclick=()=>flipSelectedShelf('h');
  const btnShelfFlipV=$('#btn-shelf-flip-v');if(btnShelfFlipV)btnShelfFlipV.onclick=()=>flipSelectedShelf('v');
  const addProdBtn=$('#add-shelf-product-btn');if(addProdBtn)addProdBtn.onclick=addProductToSelectedShelf;
  const newProdName=$('#new-prod-name');if(newProdName)newProdName.onkeydown=e=>{if(e.key==='Enter')addProductToSelectedShelf();};
  const newProdPrice=$('#new-prod-price');if(newProdPrice)newProdPrice.onkeydown=e=>{if(e.key==='Enter')addProductToSelectedShelf();};
  const delShelf=$('#delete-shelf');if(delShelf)delShelf.onclick=()=>deleteSelected('shelf');
  const delWall=$('#delete-wall');if(delWall)delWall.onclick=()=>deleteSelected('wall');
  const btnClearLayout=$('#clear-layout-btn');if(btnClearLayout)btnClearLayout.onclick=clearAllObjects;
  const btnClearLayoutSidebar=$('#clear-layout-sidebar-btn');if(btnClearLayoutSidebar)btnClearLayoutSidebar.onclick=clearAllObjects;
  const btnUndo=$('#undo-btn');if(btnUndo)btnUndo.onclick=undo;
  const btnRedo=$('#redo-btn');if(btnRedo)btnRedo.onclick=redo;
  window.addEventListener('keydown',e=>{
    const tag=document.activeElement?.tagName?.toLowerCase();
    if(tag==='input'||tag==='textarea'||tag==='select')return;
    if((e.ctrlKey||e.metaKey)&&e.key.toLowerCase()==='z'){
      if(e.shiftKey){e.preventDefault();redo()}else{e.preventDefault();undo()}
    }else if((e.ctrlKey||e.metaKey)&&e.key.toLowerCase()==='y'){
      e.preventDefault();redo()
    }else if(e.key.toLowerCase()==='r'&&selected?.type==='shelf'){
      e.preventDefault();rotateSelectedShelf();
    }else if((e.key==='Delete'||e.key==='Backspace')&&selected){
      e.preventDefault();deleteSelected(selected.type);
    }
  });
  $$('.tab-btn').forEach(btn=>btn.onclick=()=>switchTab(btn.dataset.tab));
  $$('.nav-pill-btn').forEach(btn=>{
    btn.onclick=async()=>{
      const targetTab=btn.dataset.navTab;
      if(!targetTab||targetTab===currentTab)return;
      if(targetTab==='simulate'){
        const ok=checkLayoutAndNotify();
        if(!ok)return;
      }
      if(currentTab==='simulate'&&targetTab!=='simulate'){
        if(simulation&&((simulation.time||0)>0.5||(simulation.snapshot?.().spawned||0)>0)){
          await saveSimulationSession(simulation,true);
        }
      }
      switchTab(targetTab);
    };
  });
  const inputRunName=$('#run-name');if(inputRunName)inputRunName.oninput=()=>markDirty('Tên cửa hàng đã thay đổi.');
  const headerBrandLogo=$('#header-brand-logo');if(headerBrandLogo)headerBrandLogo.onclick=()=>switchTab('welcome');
  const btnNew=$('#btn-new');if(btnNew)btnNew.onclick=()=>switchTab('setup');
  const btnLoad=$('#btn-load');if(btnLoad)btnLoad.onclick=()=>switchTab('results');
  const btnRunSim=$('#btn-run-sim');if(btnRunSim)btnRunSim.onclick=()=>{checkLayoutAndNotify();switchTab('simulate');};
  const btnBackSetup=$('#btn-back-setup');if(btnBackSetup)btnBackSetup.onclick=()=>switchTab('setup');
  const btnSimToResults=$('#btn-sim-to-results');
  if(btnSimToResults){
    btnSimToResults.onclick=async()=>{
      if(simulation&&((simulation.time||0)>0.5||(simulation.snapshot?.().spawned||0)>0)){
        await saveSimulationSession(simulation,true);
      }
      switchTab('results');
    };
  }
  const btnEvaluate=$('#btn-evaluate');if(btnEvaluate)btnEvaluate.onclick=()=>switchTab('analytics');
  const btnResBackSetup=$('#btn-results-back-setup');if(btnResBackSetup)btnResBackSetup.onclick=()=>switchTab('setup');
  const btnResBackSim=$('#btn-results-back-simulate');if(btnResBackSim)btnResBackSim.onclick=()=>switchTab('simulate');
  const btnAnaBackSetup=$('#btn-analytics-back-setup');if(btnAnaBackSetup)btnAnaBackSetup.onclick=()=>switchTab('setup');
  const btnAnaBackRes=$('#btn-analytics-back-results');if(btnAnaBackRes)btnAnaBackRes.onclick=()=>switchTab('results');
  
  const handleClearHistory=async()=>{
    try{
      let localRuns = JSON.parse(localStorage.getItem('aisle_history_runs') || '[]');
      let trashRuns = JSON.parse(localStorage.getItem('aisle_trash_runs') || '[]');
      for(const r of localRuns){
        if(!trashRuns.some(t => (t.id || t.Id || '') === (r.id || r.Id || ''))){
          trashRuns.unshift(r);
        }
      }
      localStorage.setItem('aisle_trash_runs', JSON.stringify(trashRuns.slice(0, 100)));
    }catch(e){}
    
    localStorage.removeItem('aisle_history_runs');
    localStorage.removeItem('sim-history-list');
    
    try{
      if(window.aisleBridge&&typeof window.aisleBridge.request==='function'){
        await window.aisleBridge.request('history.clear');
      }
    }catch(e){}
    await loadHistoryList();
    await loadDashboard();
    toast('Đã chuyển toàn bộ lịch sử vào thùng rác.');
    showSystemEvent('Đã chuyển toàn bộ lịch sử vào thùng rác.');
  };
  const btnClearHist=$('#btn-clear-history');if(btnClearHist)btnClearHist.onclick=handleClearHistory;
  const btnClearAllHist=$('#btn-clear-all-history');if(btnClearAllHist)btnClearAllHist.onclick=handleClearHistory;
  const btnAnaClearHist=$('#btn-analytics-clear-history');if(btnAnaClearHist)btnAnaClearHist.onclick=handleClearHistory;
  const btnOpenTrash=$('#btn-open-trash');if(btnOpenTrash)btnOpenTrash.onclick=openTrashDialog;
  const btnCloseTrash=$('#btn-close-trash');if(btnCloseTrash)btnCloseTrash.onclick=()=>$('#trash-dialog')?.close();
  const btnCloseTrashFooter=$('#btn-close-trash-footer');if(btnCloseTrashFooter)btnCloseTrashFooter.onclick=()=>$('#trash-dialog')?.close();
  const btnSortRevenue=$('#btn-sort-revenue');if(btnSortRevenue)btnSortRevenue.onclick=toggleHistorySort;
  const btnCloseRunDiagram=$('#btn-close-run-diagram');if(btnCloseRunDiagram)btnCloseRunDiagram.onclick=()=>$('#run-diagram-dialog')?.close();
  const btnRestoreAllTrash=$('#btn-restore-all-trash');if(btnRestoreAllTrash)btnRestoreAllTrash.onclick=restoreAllTrashRuns;
  const btnWarnBack=$('#btn-warning-back-setup');if(btnWarnBack)btnWarnBack.onclick=()=>{$('#layout-warning-dialog')?.close();switchTab('setup');};
  const btnWarnCont=$('#btn-warning-continue');if(btnWarnCont)btnWarnCont.onclick=()=>{$('#layout-warning-dialog')?.close();};
  const toggleSidebarBtn=$('#toggle-sidebar-btn');
  const sidebarContainer=$('#setup-sidebar-container');
  const toggleSidebarIcon=$('#toggle-sidebar-icon');
  if(toggleSidebarBtn&&sidebarContainer&&toggleSidebarIcon){
    toggleSidebarBtn.onclick=()=>{
      const isCollapsed=sidebarContainer.classList.toggle('collapsed');
      toggleSidebarIcon.textContent=isCollapsed?'chevron_right':'chevron_left';
      toggleSidebarBtn.title=isCollapsed?'Mở rộng bảng điều khiển':'Ẩn bảng điều khiển';
      setTimeout(resizeCanvas,320);
    };
  }
  const btnViewMetrics=$('#toggle-view-metrics');if(btnViewMetrics)btnViewMetrics.onclick=()=>setSimPanelView('metrics');
  const btnViewLog=$('#toggle-view-log');if(btnViewLog)btnViewLog.onclick=()=>setSimPanelView('log');
  const toggleInspectorBtn=$('#toggle-inspector-btn');
  const inspectorContainer=$('#setup-inspector-container');
  const toggleInspectorIcon=$('#toggle-inspector-icon');
  if(toggleInspectorBtn&&inspectorContainer&&toggleInspectorIcon){
    toggleInspectorBtn.onclick=()=>{
      const isCollapsed=inspectorContainer.classList.toggle('collapsed');
      toggleInspectorIcon.textContent=isCollapsed?'chevron_left':'chevron_right';
      toggleInspectorBtn.title=isCollapsed?'Mở rộng bảng thuộc tính':'Ẩn bảng thuộc tính';
      setTimeout(resizeCanvas,320);
    };
  }
}

function setSimPanelView(mode){
  const btnMetrics=$('#toggle-view-metrics'),btnLog=$('#toggle-view-log');
  const viewLog=$('#view-log-container'),viewMetrics=$('#view-metrics-container');
  const titleIcon=$('#panel-toggle-icon'),titleHeading=$('#panel-toggle-heading');
  if(mode==='metrics'){
    viewLog?.classList.add('hidden');
    viewMetrics?.classList.remove('hidden');
    btnMetrics?.classList.add('bg-primary','text-on-primary','shadow-xs');
    btnMetrics?.classList.remove('text-on-surface-variant');
    btnLog?.classList.remove('bg-primary','text-on-primary','shadow-xs');
    btnLog?.classList.add('text-on-surface-variant');
    if(titleIcon)titleIcon.textContent='monitoring';
    if(titleHeading)titleHeading.textContent='Chỉ số trực tiếp';
  }else{
    viewMetrics?.classList.add('hidden');
    viewLog?.classList.remove('hidden');
    btnLog?.classList.add('bg-primary','text-on-primary','shadow-xs');
    btnLog?.classList.remove('text-on-surface-variant');
    btnMetrics?.classList.remove('bg-primary','text-on-primary','shadow-xs');
    btnMetrics?.classList.add('text-on-surface-variant');
    if(titleIcon)titleIcon.textContent='receipt_long';
    if(titleHeading)titleHeading.textContent='Nhật ký quyết định';
  }
}

function resizeCanvas(){
  const canvas=$('#scene');
  if(!canvas)return;
  const rect=canvas.getBoundingClientRect();
  const width=Math.max(1,Math.floor(rect.width)),height=Math.max(1,Math.floor(rect.height));
  if(canvas.width!==width||canvas.height!==height){canvas.width=width;canvas.height=height}
  draw();
}

function buildParameterLab(){const groups={
  'TIME & SPAWN':['tickSeconds','needTimeScale','spawnPeakStrength','trajectorySampleSeconds'],
  'UTILITY AI':['utilityNeedWeight','utilityExploreWeight','utilityValenceWeight','distancePenalty','decisionNoise','maxShelfVisits'],
  'PURCHASE':['purchaseNeedA','purchaseValenceB','purchaseBiasC','impulseBase'],
  'MOTION & PATH':['dwellScale','collisionRadius','separationStrength','pathCellSize','obstacleMargin','stuckTimeout','maxReplans'],
};
  $('#parameter-grid').innerHTML=Object.entries(groups).map(([group,keys])=>`<fieldset><legend>${group}</legend>${keys.map(key=>`<label>${parameterLabel(key)}<input type="number" data-param="${key}" value="${parameters[key]}" step="${parameterStep(key)}"></label>`).join('')}</fieldset>`).join('')}
function parameterLabel(key){return key.replace(/([A-Z])/g,' $1').replace(/^./,x=>x.toUpperCase())}
function parameterStep(key){return['maxShelfVisits','maxReplans'].includes(key)?1:['purchaseNeedA','purchaseValenceB','purchaseBiasC','utilityNeedWeight','utilityExploreWeight','dwellScale','stuckTimeout'].includes(key)?.1:.01}
function applyParameters(){for(const input of $$('[data-param]')){const value=Number(input.value);if(!Number.isFinite(value))return toast(`${input.dataset.param} is not a number`);parameters[input.dataset.param]=value}parameters.tickSeconds=clamp(parameters.tickSeconds,.02,2);parameters.trajectorySampleSeconds=clamp(parameters.trajectorySampleSeconds,.05,10);parameters.maxShelfVisits=clamp(Math.round(parameters.maxShelfVisits),1,10);parameters.maxReplans=clamp(Math.round(parameters.maxReplans),0,8);parameters.stuckTimeout=clamp(parameters.stuckTimeout,.2,10);parameters.pathCellSize=clamp(parameters.pathCellSize,.1,.75);parameters.impulseBase=clamp(parameters.impulseBase,0,1);$('#parameter-dialog').close();markDirty('Parameter set changed. Reset/Run will use the new constants.');toast('Parameters applied')}

async function population(){if($('#population-mode').value==='manual'){if(!manualRows.length)throw new Error('Enter at least one manual NPC.');return manualPopulation(manualRows).map(mapManualProfile)}const categories=[...new Set(catalog.map(product=>product.category).filter(Boolean))];const generated=await window.aisleBridge.request('population.generate',{config:{count:Number($('#npc-count').value),categoryIds:categories}});if(!generated.validation?.valid)throw new Error('Generated population did not pass validation.');return generated.profiles}
function normalizeLayout(){
  if(!layout)return;
  let minX=0,minY=0;
  let maxX=12,maxY=8;

  if(Array.isArray(layout.walls)){
    for(const w of layout.walls){
      minX=Math.min(minX,w.x1||0,w.x2||0);
      maxX=Math.max(maxX,w.x1||0,w.x2||0);
      minY=Math.min(minY,w.y1||0,w.y2||0);
      maxY=Math.max(maxY,w.y1||0,w.y2||0);
    }
  }
  if(Array.isArray(layout.shelves)){
    for(const s of layout.shelves){
      minX=Math.min(minX,s.x||0);
      maxX=Math.max(maxX,(s.x||0)+(s.w||0));
      minY=Math.min(minY,s.y||0);
      maxY=Math.max(maxY,(s.y||0)+(s.h||0));
    }
  }
  if(layout.entrance){
    minX=Math.min(minX,layout.entrance.x);
    maxX=Math.max(maxX,layout.entrance.x);
    minY=Math.min(minY,layout.entrance.y);
    maxY=Math.max(maxY,layout.entrance.y);
  }
  if(layout.checkout){
    minX=Math.min(minX,layout.checkout.x);
    maxX=Math.max(maxX,layout.checkout.x);
    minY=Math.min(minY,layout.checkout.y);
    maxY=Math.max(maxY,layout.checkout.y);
  }

  const shiftX=minX<0?-minX:0;
  const shiftY=minY<0?-minY:0;

  if(shiftX>0||shiftY>0){
    if(Array.isArray(layout.walls)){
      for(const w of layout.walls){
        w.x1=Math.round((w.x1+shiftX)*4)/4;
        w.x2=Math.round((w.x2+shiftX)*4)/4;
        w.y1=Math.round((w.y1+shiftY)*4)/4;
        w.y2=Math.round((w.y2+shiftY)*4)/4;
      }
    }
    if(Array.isArray(layout.shelves)){
      for(const s of layout.shelves){
        s.x=Math.round((s.x+shiftX)*4)/4;
        s.y=Math.round((s.y+shiftY)*4)/4;
      }
    }
    if(layout.entrance){
      layout.entrance.x=Math.round((layout.entrance.x+shiftX)*4)/4;
      layout.entrance.y=Math.round((layout.entrance.y+shiftY)*4)/4;
    }
    if(layout.checkout){
      layout.checkout.x=Math.round((layout.checkout.x+shiftX)*4)/4;
      layout.checkout.y=Math.round((layout.checkout.y+shiftY)*4)/4;
    }
    maxX+=shiftX;
    maxY+=shiftY;
  }

  layout.width=Math.max(12,Math.ceil(maxX));
  layout.height=Math.max(8,Math.ceil(maxY));
}

function simulationInput(profiles){
  normalizeLayout();
  return{
    name:$('#run-name').value,
    layout:{
      width:layout.width,
      height:layout.height,
      walls:layout.walls,
      shelves:layout.shelves.map(shelf=>({...shelf,width:shelf.w,height:shelf.h})),
      entrance:layout.entrance,
      checkout:layout.checkout,
      spawnRateCurve:layout.spawnRateCurve||[]
    },
    catalog:catalog.map(product=>({...product,shelfId:product.shelf})),
    population:{
      populationId:`desktop-${crypto.randomUUID()}`,
      npcProfiles:profiles,
      metadata:{generatorName:$('#population-mode').value==='manual'?'manual-input':'GeneticSharp',generatorVersion:'desktop-bridge'}
    },
    config:{...parameters,durationMinutes:Number($('#duration').value)}
  };
}
function recordHistoryFrame(){
  if(!simulation) return;
  const t = simulation.time || 0;
  if(t > maxRecordedTime) maxRecordedTime = t;
  if(rewindTime === null){
    const last = liveHistory[liveHistory.length - 1];
    if(!last || t - last.time >= 0.25 || simulation.completed){
      liveHistory.push({
        time: t,
        agents: (simulation.agents || []).map(a => ({
          id: a.id,
          x: a.x,
          y: a.y,
          status: a.status,
          targetId: a.targetId,
          currentShelf: a.currentShelf,
          facingDx: a.facingDx,
          facingDy: a.facingDy,
          trail: a.trail ? [...a.trail] : [],
          path: a.path ? [...a.path] : [],
          pathIndex: a.pathIndex || 0,
          finished: a.finished
        }))
      });
    }
  }
}

async function createSimulation(){
  if(!window.aisleBridge?.request)throw new Error('Run Live requires the AIsle Desktop bridge.');
  checkLayoutAndNotify();
  const profiles=await population();
  const adapter=new NativeSimulationAdapter(window.aisleBridge,profiles,durationSeconds());
  await adapter.start(simulationInput(profiles));
  await adapter.setSpeed(Number($('#speed').value));
  simulation=adapter;
  lastRunSeed=adapter.seed;
  npcRenderer.reset(adapter.seed,performance.now());
  simResult=null;
  dirty=false;
  selected=null;
  lastPurchaseCount=0;
  lastFinishedCount=0;
  liveHistory=[];
  maxRecordedTime=0;
  rewindTime=null;
  recordHistoryFrame();
  updateAll();
  return simulation;
}

async function toggleRun(){
  try{
    if(dirty||!simulation||simulation.completed){
      await createSimulation();
      playing=simulation.running;
    }else if(rewindTime!==null){
      rewindTime=null;
      await simulation.setSpeed(Number($('#speed').value));
      await simulation.resume();
      playing=simulation.running;
    }else if(playing){
      await simulation.pause();
      playing=false;
    }else{
      await simulation.setSpeed(Number($('#speed').value));
      await simulation.resume();
      playing=simulation.running;
    }
    $('#play-btn').textContent=playing?'❚❚ Tạm dừng':'▶ Chạy trực tiếp';
    $('#stage-status').textContent=playing?'ĐANG CHẠY MÔ PHỎNG':'TẠM DỪNG';
    if(playing){
      lastFrame=0;
      requestAnimationFrame(frame);
    }
  }catch(error){
    playing=false;
    toast(error.message);
    showSystemEvent(error.message);
  }
}

async function resetSimulation(){
  playing=false;
  try{
    await createSimulation();
    await simulation.reset();
    await simulation.pause();
    liveHistory=[];
    maxRecordedTime=0;
    rewindTime=null;
    $('#play-btn').textContent='▶ Chạy trực tiếp';
    $('#stage-status').textContent='ĐÃ ĐẶT LẠI · T=0';
    showSystemEvent('Đã đặt lại phiên mô phỏng mới.');
  }catch(error){
    toast(error.message);
    showSystemEvent(error.message);
  }
}

async function singleStep(){
  playing=false;
  try{
    const created=dirty||!simulation||simulation.completed;
    if(created){
      await createSimulation();
      await simulation.pause();
    }else{
      rewindTime=null;
      await simulation.step();
      recordHistoryFrame();
    }
    $('#play-btn').textContent='▶ Chạy trực tiếp';
    $('#stage-status').textContent=`BƯỚC ĐƠN · Δt=${parameters.tickSeconds}s`;
    updateAll();
  }catch(error){
    toast(error.message);
    showSystemEvent(error.message);
  }
}

async function frame(now){
  if(!playing||!simulation)return;
  if(now-lastFrame>=50){
    lastFrame=now;
    try{
      await simulation.refresh();
      recordHistoryFrame();
      updateAll();
    }catch(error){
      playing=false;
      showSystemEvent(error.message);
      return;
    }
  }
  if(simulation.completed){
    playing=false;
    $('#play-btn').textContent='▶ Chạy trực tiếp';
    $('#stage-status').textContent='HOÀN THÀNH';
    const finishedSim=simulation;
    await saveSimulationSession(finishedSim,true);
    updateResultsScreen();
    switchTab('results');
  }else requestAnimationFrame(frame);
}

function renderHistoryRow(item,index=0,sortMode='date'){
  const name=item.name||item.Name||'Phiên mô phỏng';
  const date=new Date(item.createdAt||item.CreatedAt||Date.now());
  const timeStr=isNaN(date.getTime())?'--:--':date.toLocaleTimeString('vi-VN',{hour:'2-digit',minute:'2-digit'});
  const dateStr=isNaN(date.getTime())?'':date.toLocaleDateString('vi-VN');
  const summary=item.summary||item.Summary||{};
  const spawned=summary.spawned??summary.Spawned??0;
  const converted=summary.converted??summary.Converted??0;
  const revenue=money(summary.revenue??summary.Revenue??0);
  const durationSec = summary.duration ?? summary.Duration ?? 1800;
  const durationMin = Math.round(durationSec / 60) || 30;
  const key=item.id||item.Id||(item.createdAt||item.CreatedAt||'')+(item.name||item.Name||'');
  const rank=index+1;
  const medal=rank===1?'🥇':rank===2?'🥈':rank===3?'🥉':null;
  const badgeInner=sortMode==='revenue'
    ?(medal?`<span class="text-base leading-none">${medal}</span>`:`<span class="font-black text-xs">#${rank}</span>`)
    :`<span class="material-symbols-outlined text-base" style="font-variation-settings: 'FILL' 1;">storefront</span>`;

  return `
    <div class="history-row grid grid-cols-[1fr_120px_160px_130px_48px] gap-3 px-6 hover:bg-surface-bright transition-colors duration-300 items-center group cursor-pointer py-4 border-b border-surface-container-low/50" title="Bấm để xem sơ đồ phiên mô phỏng này">
      <div class="flex items-center gap-3 min-w-0">
        <div class="w-9 h-9 rounded-full bg-primary-container flex items-center justify-center text-primary group-hover:scale-110 transition-transform shadow-xs shrink-0">
          ${badgeInner}
        </div>
        <div class="min-w-0 flex-1">
          <div class="font-label-md text-sm text-on-surface font-bold truncate">${escapeHTML(name)}</div>
          <div class="text-[11px] text-on-surface-variant opacity-70">${dateStr ? 'Ngày '+dateStr : 'Lưu gần đây'}</div>
        </div>
      </div>
      <div class="font-body-md text-sm text-on-surface-variant flex flex-col justify-center">
        <div class="flex items-center gap-1 font-bold text-on-surface text-xs">
          <span class="material-symbols-outlined text-sm text-primary">schedule</span> ${timeStr}
        </div>
        <div class="text-[10px] text-on-surface-variant opacity-75">${durationMin} phút chạy</div>
      </div>
      <div class="font-label-md text-on-surface text-right">
        <span class="inline-flex items-center justify-center bg-tertiary-container text-on-tertiary-container px-3 py-1 rounded-full text-xs font-bold shadow-xs">
          ${spawned} khách (${converted} đã mua)
        </span>
      </div>
      <div class="font-label-md text-secondary text-right font-bold text-base md:text-lg">${revenue}</div>
      <div class="text-center flex justify-center items-center">
        <button type="button" class="btn-delete-row p-1.5 rounded-lg text-on-surface-variant/40 hover:text-error hover:bg-error-container/40 transition-colors cursor-pointer" data-delete-key="${escapeHTML(key)}" data-item-id="${escapeHTML(item.id || item.Id || '')}" title="Xóa phiên này">
          <span class="material-symbols-outlined text-lg">delete</span>
        </button>
      </div>
    </div>
  `;
}

function toggleHistorySort(){
  historySortMode = historySortMode==='revenue' ? 'date' : 'revenue';
  const btn=$('#btn-sort-revenue');
  if(btn){
    btn.classList.toggle('text-primary',historySortMode==='revenue');
    btn.classList.toggle('font-bold',historySortMode==='revenue');
  }
  loadHistoryList();
}

async function openRunDiagram(item){
  const dialog=$('#run-diagram-dialog');
  const canvas=$('#run-diagram-canvas');
  const status=$('#run-diagram-status');
  if(!dialog||!canvas)return;
  const id=item.id||item.Id;
  const name=item.name||item.Name||'Phiên mô phỏng';
  const summary=item.summary||item.Summary||{};
  const spawned=summary.spawned??summary.Spawned??0;
  const converted=summary.converted??summary.Converted??0;
  const revenueVal=summary.revenue??summary.Revenue??0;
  const convRate=spawned?pct(converted/spawned):'0.0%';

  $('#run-diagram-title').querySelector('span:last-child').textContent=name;
  $('#run-diagram-meta').textContent=`${Math.round(spawned)} khách · ${Math.round(converted)} đã mua (${convRate}) · Doanh thu ${money(revenueVal)}`;
  const ctx=canvas.getContext('2d');
  ctx.clearRect(0,0,canvas.width,canvas.height);
  ctx.fillStyle='#1c1007';
  ctx.fillRect(0,0,canvas.width,canvas.height);
  if(status)status.textContent='Đang tải sơ đồ…';
  dialog.showModal();

  if(!id||!window.aisleBridge||typeof window.aisleBridge.request!=='function'){
    if(status)status.textContent='Không có kết nối tới ứng dụng để tải sơ đồ.';
    return;
  }
  try{
    const replay=await window.aisleBridge.request('replay.project',{id});
    const agents=replay?.agents||replay?.Agents||[];
    renderRunDiagramCanvas(canvas,layout,agents);
    if(status)status.textContent=`${agents.length} khách hàng được vẽ đường đi trên sơ đồ.`;
  }catch(error){
    if(status)status.textContent='Không tải được sơ đồ: '+(error?.message||error);
  }
}

function renderRunDiagramCanvas(canvas,layoutData,agents){
  const ctx=canvas.getContext('2d');
  const W=canvas.width,H=canvas.height;
  ctx.clearRect(0,0,W,H);
  ctx.fillStyle='#1c1007';
  ctx.fillRect(0,0,W,H);
  if(!layoutData||!layoutData.width||!layoutData.height)return;

  const pad=28;
  const scale=Math.min((W-pad*2)/layoutData.width,(H-pad*2)/layoutData.height);
  const ox=(W-layoutData.width*scale)/2;
  const oy=(H-layoutData.height*scale)/2;
  const tx=x=>ox+x*scale;
  const ty=y=>oy+y*scale;

  ctx.strokeStyle='#c8844a';
  ctx.lineWidth=5;
  ctx.lineCap='round';
  for(const wall of layoutData.walls||[]){
    ctx.beginPath();
    ctx.moveTo(tx(wall.x1),ty(wall.y1));
    ctx.lineTo(tx(wall.x2),ty(wall.y2));
    ctx.stroke();
  }

  ctx.font='700 10px "Nunito Sans", sans-serif';
  ctx.textAlign='center';
  for(const s of layoutData.shelves||[]){
    ctx.fillStyle='#2e1509';
    ctx.strokeStyle='#6b3519';
    ctx.lineWidth=1.5;
    ctx.fillRect(tx(s.x),ty(s.y),s.w*scale,s.h*scale);
    ctx.strokeRect(tx(s.x),ty(s.y),s.w*scale,s.h*scale);
    ctx.fillStyle='#f5e6c8';
    ctx.fillText(s.label||'',tx(s.x+s.w/2),ty(s.y+s.h/2)+3);
  }

  const markerAt=(p,color)=>{
    if(!p)return;
    ctx.fillStyle='#120a04';
    ctx.strokeStyle=color;
    ctx.lineWidth=2;
    ctx.beginPath();
    ctx.arc(tx(p.x),ty(p.y),8,0,Math.PI*2);
    ctx.fill();
    ctx.stroke();
  };
  markerAt(layoutData.entrance,'#5dba4f');
  markerAt(layoutData.checkout,'#e05252');

  const palette=['#5fa8d3','#e0a458','#8bc34a','#e05252','#b388eb','#ffca58','#4fd1c5','#f06292'];
  agents.forEach((agent,index)=>{
    const samples=agent.samples||agent.Samples||[];
    if(samples.length<2)return;
    const color=palette[index%palette.length];
    ctx.strokeStyle=color;
    ctx.globalAlpha=0.55;
    ctx.lineWidth=1.4;
    ctx.beginPath();
    samples.forEach((p,i)=>{
      const x=tx(p.x??p.X??0),y=ty(p.y??p.Y??0);
      if(i===0)ctx.moveTo(x,y);else ctx.lineTo(x,y);
    });
    ctx.stroke();
    ctx.globalAlpha=1;
    const last=samples[samples.length-1];
    ctx.fillStyle=color;
    ctx.beginPath();
    ctx.arc(tx(last.x??last.X??0),ty(last.y??last.Y??0),2.6,0,Math.PI*2);
    ctx.fill();
  });
}

function getDeletedHistoryIds(){
  try{
    return new Set(JSON.parse(localStorage.getItem('aisle_deleted_history_ids')||'[]'));
  }catch{
    return new Set();
  }
}
function markHistoryIdDeleted(id){
  if(!id)return;
  const set=getDeletedHistoryIds();
  set.add(id);
  localStorage.setItem('aisle_deleted_history_ids',JSON.stringify([...set]));
}

async function deleteHistoryRunByKey(deleteKey, rawItem){
  if(!deleteKey) return;
  markHistoryIdDeleted(deleteKey);
  
  try{
    let localRuns = JSON.parse(localStorage.getItem('aisle_history_runs') || '[]');
    let trashRuns = JSON.parse(localStorage.getItem('aisle_trash_runs') || '[]');
    
    let target = rawItem || localRuns.find(item => {
      const key = item.id || item.Id || (item.createdAt || item.CreatedAt || '') + (item.name || item.Name || '');
      return key === deleteKey || item.id === deleteKey || item.Id === deleteKey;
    });
    
    if(target){
      trashRuns = trashRuns.filter(t => (t.id || t.Id || '') !== (target.id || target.Id || ''));
      trashRuns.unshift(target);
      localStorage.setItem('aisle_trash_runs', JSON.stringify(trashRuns.slice(0, 100)));
    }
    
    localRuns = localRuns.filter(item => {
      const key = item.id || item.Id || (item.createdAt || item.CreatedAt || '') + (item.name || item.Name || '');
      return key !== deleteKey && item.id !== deleteKey && item.Id !== deleteKey;
    });
    localStorage.setItem('aisle_history_runs', JSON.stringify(localRuns));
  }catch(e){}
  
  try{
    let simList = JSON.parse(localStorage.getItem('sim-history-list') || '[]');
    simList = simList.filter(item => {
      const key = item.id || item.Id || (item.createdAt || item.CreatedAt || '') + (item.name || item.Name || '');
      return key !== deleteKey && item.id !== deleteKey && item.Id !== deleteKey;
    });
    localStorage.setItem('sim-history-list', JSON.stringify(simList));
  }catch(e){}
  
  try{
    if(window.aisleBridge && typeof window.aisleBridge.request === 'function'){
      const idToSend = (rawItem?.id || rawItem?.Id) || deleteKey;
      await window.aisleBridge.request('history.delete', { id: idToSend });
    }
  }catch(e){}
  
  await loadHistoryList();
  await loadDashboard();
  toast('Đã chuyển phiên mô phỏng vào thùng rác.');
}

async function loadHistoryList(){
  const tableBody=$('#results-table-body');
  if(!tableBody)return;
  let bridgeItems=[];
  let localItems=[];
  try{
    if(window.aisleBridge&&typeof window.aisleBridge.request==='function'){
      const res=await window.aisleBridge.request('history.list');
      bridgeItems=res?.items||res?.Items||[];
    }
  }catch(e){
    console.warn('history.list error:',e);
  }
  try{
    localItems=JSON.parse(localStorage.getItem('aisle_history_runs')||'[]');
  }catch(e){}

  const deletedIds=getDeletedHistoryIds();
  const map=new Map();
  for(const item of [...bridgeItems, ...localItems]){
    const key=item.id||item.Id||(item.createdAt||item.CreatedAt||'')+(item.name||item.Name||'');
    if(key && !deletedIds.has(key) && !deletedIds.has(item.id) && !deletedIds.has(item.Id) && !map.has(key)){
      map.set(key, item);
    }
  }
  const merged=[...map.values()];
  merged.sort((a,b)=>{
    if(historySortMode==='revenue'){
      const ra=Number((a.summary||a.Summary||{}).revenue??(a.summary||a.Summary||{}).Revenue??0);
      const rb=Number((b.summary||b.Summary||{}).revenue??(b.summary||b.Summary||{}).Revenue??0);
      if(rb!==ra) return rb-ra;
    }
    const da=new Date(a.createdAt||a.CreatedAt||0).getTime();
    const db=new Date(b.createdAt||b.CreatedAt||0).getTime();
    return db-da;
  });

  const countText=$('#results-count-text');
  if(countText) countText.textContent=historySortMode==='revenue'?`${merged.length} phiên · xếp hạng theo doanh thu`:`${merged.length} phiên đã lưu`;

  const emptyState=$('#results-empty-state');
  if(merged.length>0){
    if(emptyState)emptyState.remove();
    tableBody.innerHTML=merged.map((item,index)=>renderHistoryRow(item,index,historySortMode)).join('');
    tableBody.querySelectorAll('.btn-delete-row').forEach((btn, index) => {
      btn.onclick = async (e) => {
        e.stopPropagation();
        const deleteKey = btn.dataset.deleteKey;
        const item = merged[index];
        await deleteHistoryRunByKey(deleteKey, item);
      };
    });
    tableBody.querySelectorAll('.history-row').forEach((row, index) => {
      row.onclick = () => openRunDiagram(merged[index]);
    });
  }else{
    if(!emptyState){
      tableBody.innerHTML=`
        <div id="results-empty-state" class="py-16 text-center text-on-surface-variant text-sm font-mono opacity-60 flex flex-col items-center justify-center gap-2 my-auto">
          <span class="material-symbols-outlined text-4xl opacity-50">receipt_long</span>
          <span class="text-base font-semibold">Chưa có phiên mô phỏng nào được lưu.</span>
          <span class="text-xs">Hãy chạy một phiên mô phỏng để xem kết quả chi tiết!</span>
        </div>
      `;
    }
  }
}

async function openTrashDialog(){
  const dialog=$('#trash-dialog');
  const body=$('#trash-list-body');
  if(!dialog||!body)return;
  
  let trashBridgeItems=[];
  try{
    if(window.aisleBridge&&typeof window.aisleBridge.request==='function'){
      const res=await window.aisleBridge.request('history.trash.list');
      trashBridgeItems=res?.items||res?.Items||[];
    }
  }catch(e){
    console.warn('history.trash.list error:',e);
  }
  
  let trashLocalRuns=[];
  try{
    trashLocalRuns=JSON.parse(localStorage.getItem('aisle_trash_runs')||'[]');
  }catch(e){}
  
  const trashMap=new Map();
  
  for(const item of [...trashBridgeItems, ...trashLocalRuns]){
    const key=item.id||item.Id||(item.createdAt||item.CreatedAt||'')+(item.name||item.Name||'');
    if(key&&!trashMap.has(key)){
      trashMap.set(key,item);
    }
  }
  
  const trashList=[...trashMap.values()];
  trashList.sort((a,b)=>{
    const da=new Date(a.createdAt||a.CreatedAt||0).getTime();
    const db=new Date(b.createdAt||b.CreatedAt||0).getTime();
    return db-da;
  });
  
  if(trashList.length===0){
    body.innerHTML=`
      <div class="py-8 text-center text-on-surface-variant text-sm font-mono opacity-60 flex flex-col items-center justify-center gap-2">
        <span class="material-symbols-outlined text-3xl opacity-50">delete_outline</span>
        <span>Thùng rác trống. Chưa có phiên nào bị xóa.</span>
      </div>
    `;
  }else{
    body.innerHTML=trashList.map(item=>{
      const key=item.id||item.Id||(item.createdAt||item.CreatedAt||'')+(item.name||item.Name||'');
      const name=item.name||item.Name||'Cửa hàng tiện lợi';
      const timeStr=item.createdAt||item.CreatedAt?new Date(item.createdAt||item.CreatedAt).toLocaleString('vi-VN'):'Không rõ';
      const summary=item.summary||item.Summary||{};
      const revenue=money(summary.revenue||summary.Revenue||0);
      const spawned=summary.spawned||summary.Spawned||0;
      const converted=summary.converted||summary.Converted||0;
      return `
        <div class="flex items-center justify-between p-3 rounded-xl bg-surface-container border border-outline-variant/60 hover:bg-surface-container-high transition-colors">
          <div class="flex flex-col min-w-0 pr-2">
            <span class="font-bold text-xs text-primary truncate">${escapeHTML(name)}</span>
            <span class="text-[11px] text-on-surface-variant">${timeStr} · ${spawned} khách (${converted} đã mua) · <b class="text-secondary">${revenue}</b></span>
          </div>
          <button type="button" class="btn-restore-item px-3 py-1 rounded-lg bg-surface-bright hover:bg-primary hover:text-on-primary border border-outline-variant text-xs font-bold transition-colors flex items-center gap-1 shrink-0 cursor-pointer shadow-xs" data-restore-key="${escapeHTML(key)}" title="Khôi phục lại phiên này">
            <span class="material-symbols-outlined text-sm">settings_backup_restore</span> Khôi phục
          </button>
        </div>
      `;
    }).join('');
    
    body.querySelectorAll('.btn-restore-item').forEach(btn=>{
      btn.onclick=async(e)=>{
        e.stopPropagation();
        const key=btn.dataset.restoreKey;
        const itemToRestore = trashList.find(it => (it.id || it.Id || (it.createdAt || it.CreatedAt || '') + (it.name || it.Name || '')) === key);
        await restoreHistoryRunByKey(key, itemToRestore);
      };
    });
  }
  
  dialog.showModal();
}

async function restoreHistoryRunByKey(restoreKey, restoredItem){
  if(!restoreKey)return;
  const deletedSet=getDeletedHistoryIds();
  deletedSet.delete(restoreKey);
  if(restoredItem?.id) deletedSet.delete(restoredItem.id);
  if(restoredItem?.Id) deletedSet.delete(restoredItem.Id);
  localStorage.setItem('aisle_deleted_history_ids',JSON.stringify([...deletedSet]));
  
  try{
    let trashRuns=JSON.parse(localStorage.getItem('aisle_trash_runs')||'[]');
    let localRuns=JSON.parse(localStorage.getItem('aisle_history_runs')||'[]');
    
    const item = restoredItem || trashRuns.find(it => (it.id || it.Id || (it.createdAt || it.CreatedAt || '') + (it.name || it.Name || '')) === restoreKey);
    if(item){
      trashRuns = trashRuns.filter(it => (it.id || it.Id || (it.createdAt || it.CreatedAt || '') + (it.name || it.Name || '')) !== restoreKey && (it.id || it.Id) !== (item.id || item.Id));
      localStorage.setItem('aisle_trash_runs', JSON.stringify(trashRuns));
      
      if(!localRuns.some(lr => (lr.id || lr.Id) === (item.id || item.Id))){
        localRuns.unshift(item);
        localStorage.setItem('aisle_history_runs', JSON.stringify(localRuns.slice(0, 100)));
      }
    }
  }catch(e){}
  
  try{
    if(window.aisleBridge&&typeof window.aisleBridge.request==='function'){
      const idToSend = (restoredItem?.id || restoredItem?.Id) || restoreKey;
      await window.aisleBridge.request('history.restore',{id:idToSend});
    }
  }catch(e){}
  
  await openTrashDialog();
  await loadHistoryList();
  await loadDashboard();
  toast('Đã khôi phục phiên mô phỏng thành công.');
}

async function restoreAllTrashRuns(){
  localStorage.removeItem('aisle_deleted_history_ids');
  try{
    let trashRuns=JSON.parse(localStorage.getItem('aisle_trash_runs')||'[]');
    let localRuns=JSON.parse(localStorage.getItem('aisle_history_runs')||'[]');
    for(const tr of trashRuns){
      if(!localRuns.some(lr => (lr.id || lr.Id) === (tr.id || tr.Id))){
        localRuns.unshift(tr);
      }
    }
    localStorage.setItem('aisle_history_runs', JSON.stringify(localRuns.slice(0, 100)));
    localStorage.removeItem('aisle_trash_runs');
  }catch(e){}
  
  try{
    if(window.aisleBridge&&typeof window.aisleBridge.request==='function'){
      await window.aisleBridge.request('history.restore.all');
    }
  }catch(e){}
  $('#trash-dialog')?.close();
  await loadHistoryList();
  await loadDashboard();
  toast('Đã khôi phục toàn bộ lịch sử thành công.');
}

function updateResultsScreen(){
  loadHistoryList();
}
function getRewindAgents(targetTime){
  if(!liveHistory.length) return simulation?.agents || [];
  if(targetTime <= liveHistory[0].time) return liveHistory[0].agents;
  if(targetTime >= liveHistory[liveHistory.length - 1].time) return liveHistory[liveHistory.length - 1].agents;
  for(let i = 0; i < liveHistory.length - 1; i++){
    const s1 = liveHistory[i], s2 = liveHistory[i + 1];
    if(targetTime >= s1.time && targetTime <= s2.time){
      const span = s2.time - s1.time;
      const ratio = span > 0 ? (targetTime - s1.time) / span : 0;
      const map1 = new Map(s1.agents.map(a => [a.id, a]));
      return s2.agents.map(a2 => {
        const a1 = map1.get(a2.id);
        if(!a1) return a2;
        return {
          ...a2,
          x: a1.x + (a2.x - a1.x) * ratio,
          y: a1.y + (a2.y - a1.y) * ratio,
          status: ratio < 0.5 ? a1.status : a2.status
        };
      });
    }
  }
  return liveHistory[liveHistory.length - 1].agents;
}

async function seekTo(targetTime){
  if(!simulation)return;
  const dur = durationSeconds();
  if(targetTime > maxRecordedTime){
    targetTime = maxRecordedTime;
    $('#timeline').value = dur > 0 ? (maxRecordedTime / dur) * 1000 : 0;
    toast(`Chỉ có thể tua lại quá khứ (tối đa ${formatTime(maxRecordedTime)}).`);
  }
  if(targetTime < maxRecordedTime - 0.2){
    rewindTime = Math.max(0, targetTime);
    if(playing){
      playing = false;
      try{ await simulation.pause(); }catch(e){}
    }
    $('#play-btn').textContent = '▶ Tiếp tục';
    $('#stage-status').textContent = `TUA LẠI · ${formatTime(rewindTime)} (Hiện tại: ${formatTime(maxRecordedTime)})`;
    draw();
  } else {
    rewindTime = null;
    $('#play-btn').textContent = playing ? '❚❚ Tạm dừng' : '▶ Chạy trực tiếp';
    $('#stage-status').textContent = playing ? 'ĐANG CHẠY MÔ PHỎNG' : 'TẠM DỪNG';
    draw();
  }
}
function updateAll(){updateMetrics();updateCashier();renderEvents();renderInspector();draw()}

function updateMetrics(){const s=simulation?.snapshot()||{revenue:0,conversionRate:0,purchases:0,notFoundRate:0,spawned:0};const cells=$$('#metrics .metric-card');if(!cells.length){const fallback=$$('#metrics .bg-surface-container-low, #metrics>div');if(fallback.length>=3){setMetric(fallback[0],pct(s.conversionRate),`${simulation?.stats.converted||0} converted`);setMetric(fallback[1],s.purchases,`${simulation?.stats.mainBuyers||0} main · ${simulation?.stats.impulseBuyers||0} impulse`);setMetric(fallback[2],pct(s.notFoundRate),`${simulation?.stats.notFound||0} outside catalog`)}return}if(cells.length>=3){setMetric(cells[0],pct(s.conversionRate),`${simulation?.stats.converted||0} converted`);setMetric(cells[1],s.purchases,`${simulation?.stats.mainBuyers||0} main · ${simulation?.stats.impulseBuyers||0} impulse`);setMetric(cells[2],pct(s.notFoundRate),`${simulation?.stats.notFound||0} outside catalog`)}}
function setMetric(cell,value,note){cell.querySelector('b').textContent=value;cell.querySelector('small').textContent=note}
function renderEvents(){if(!simulation)return;const items=simulation.events.slice(-10).reverse();$('#event-log-list').innerHTML=items.map(item=>`<div title="${escapeHTML(item.message)}"><b>${formatTime(item.time)}</b>${escapeHTML(item.npc)} · ${escapeHTML(item.message)}</div>`).join('')||'<span>Waiting for first tick…</span>'}

function renderObjects(){
  if(!layout)return;
  const walls=layout.walls.map((w,index)=>{
    const isSelected=selected?.type==='wall'&&selected.id===w.id;
    const len=pointDistance({x:w.x1,y:w.y1},{x:w.x2,y:w.y2}).toFixed(2);
    return `
      <button type="button" data-type="wall" data-id="${w.id}" class="w-full flex items-center justify-between px-3 py-2 rounded-xl text-xs transition-all cursor-pointer ${
        isSelected
          ? 'bg-primary text-on-primary font-bold shadow-xs'
          : 'bg-surface hover:bg-surface-container-high border border-outline-variant text-on-surface'
      }">
        <span class="flex items-center gap-2">
          <span class="material-symbols-outlined text-sm opacity-80">straighten</span>
          <span>Tường ${index+1}</span>
        </span>
        <span class="text-[11px] font-mono opacity-80">${len} m</span>
      </button>
    `;
  });
  const shelves=layout.shelves.map(s=>{
    const isSelected=selected?.type==='shelf'&&selected.id===s.id;
    const catName=CATEGORY_NAMES[s.category]||s.category||'Kệ hàng';
    const prodCount=(catalog||[]).filter(p=>p.shelf===s.id||p.shelfId===s.id).length;
    return `
      <button type="button" data-type="shelf" data-id="${s.id}" class="w-full flex items-center justify-between px-3 py-2 rounded-xl text-xs transition-all cursor-pointer ${
        isSelected
          ? 'bg-primary text-on-primary font-bold shadow-xs'
          : 'bg-surface hover:bg-surface-container-high border border-outline-variant text-on-surface'
      }">
        <span class="flex items-center gap-2">
          <span class="material-symbols-outlined text-sm ${isSelected?'text-on-primary':'text-secondary'}">shelves</span>
          <span class="truncate max-w-[130px]">${escapeHTML(s.label||catName)}</span>
        </span>
        <span class="text-[10px] px-2 py-0.5 rounded-full font-semibold ${isSelected?'bg-on-primary/20 text-on-primary':'bg-surface-container text-on-surface-variant'}">
          ${prodCount} món
        </span>
      </button>
    `;
  });
  $('#object-list').innerHTML=[...walls,...shelves].join('');
  $$('#object-list button').forEach(button=>button.onclick=()=>{selected={type:button.dataset.type,id:button.dataset.id};renderObjects();renderInspector();draw()});
}
function renderInspector(){
  const wall=selected?.type==='wall'?layout.walls.find(w=>w.id===selected.id):null,shelf=selected?.type==='shelf'?layout.shelves.find(s=>s.id===selected.id):null,agent=selected?.type==='npc'&&simulation?simulation.agents.find(a=>a.id===selected.id):null;
  $('#nothing-selected').hidden=!!(wall||shelf||agent);$('#wall-form').hidden=!wall;$('#shelf-form').hidden=!shelf;$('#npc-inspector').hidden=!agent;
  if(wall){$('#wall-id').value=wall.id;for(const key of['x1','y1','x2','y2'])$('#wall-'+key).value=wall[key]}
  if(shelf){
    const catEl=$('#shelf-category');if(catEl)catEl.value=shelf.category||'beverage';
    const valEl=$('#shelf-valence');if(valEl)valEl.value=shelf.valence;
    const valOut=$('#shelf-valence-out');if(valOut)valOut.textContent=(shelf.valence>=0?'+':'')+Number(shelf.valence).toFixed(2);
    
    const sizeLabel=$('#shelf-size-label');
    if(sizeLabel)sizeLabel.textContent=`${(shelf.w||2).toFixed(1)}m × ${(shelf.h||1).toFixed(1)}m`;
    const flipBadge=$('#shelf-flip-badge');
    if(flipBadge){
      const flips=[];
      if(shelf.flipX)flips.push('Lật H');
      if(shelf.flipY)flips.push('Lật V');
      flipBadge.textContent=`${shelf.rotation||0}°${flips.length?' ('+flips.join(', ')+')':''}`;
    }
    renderShelfProducts(shelf);
  }
  if(agent){$('#npc-inspector').innerHTML=`<div class="border-b border-outline-variant pb-2 mb-3"><h3 class="font-label-md text-on-surface-variant tracking-wider uppercase text-sm font-bold flex items-center gap-2"><span class="material-symbols-outlined text-lg text-primary">person</span> THÔNG TIN KHÁCH HÀNG</h3></div><h4 class="font-bold text-xs text-primary mb-1">${escapeHTML(agent.id)}</h4><p class="text-xs text-on-surface">Trạng thái: <b>${agent.status}</b></p><p class="text-xs text-on-surface">Mục tiêu: <b>${escapeHTML(agent.target||'Chỉ xem dạo')}</b></p><p class="text-[11px] text-on-surface-variant mt-2">Nguồn: <b>C# Simulation Core</b></p>`}
}
function rotateSelectedShelf(){
  if(selected?.type!=='shelf')return;
  const s=layout.shelves.find(x=>x.id===selected.id);
  if(!s)return;
  pushUndoState();
  const temp=s.w;
  s.w=s.h;
  s.h=temp;
  s.rotation=((s.rotation||0)+90)%360;
  normalizeLayout();
  renderObjects();
  renderInspector();
  markDirty(`Đã xoay kệ (${s.w.toFixed(1)}m × ${s.h.toFixed(1)}m, ${s.rotation}°).`);
  draw();
  saveProject();
}
function flipSelectedShelf(axis='h'){
  if(selected?.type!=='shelf')return;
  const s=layout.shelves.find(x=>x.id===selected.id);
  if(!s)return;
  pushUndoState();
  if(axis==='h'){
    s.flipX=!s.flipX;
    markDirty('Đã lật ngang mặt kệ.');
  }else{
    s.flipY=!s.flipY;
    markDirty('Đã lật dọc mặt kệ.');
  }
  renderInspector();
  draw();
  saveProject();
}
function renderShelfProducts(shelf){
  const listEl=$('#shelf-product-list'),countEl=$('#shelf-product-count');
  if(!listEl||!countEl||!shelf)return;
  const prods=(catalog||[]).filter(p=>p.shelf===shelf.id||p.shelfId===shelf.id);
  countEl.textContent=prods.length;
  if(!prods.length){
    listEl.innerHTML='<div class="text-[11px] text-on-surface-variant italic p-2 text-center bg-surface-container-low rounded border border-outline-variant/50">Chưa có mặt hàng nào trên kệ này.</div>';
    return;
  }
  listEl.innerHTML=prods.map(p=>`
    <div class="flex justify-between items-center bg-surface-container-low px-2.5 py-1.5 rounded border border-outline-variant text-xs">
      <div class="flex flex-col min-w-0 pr-2">
        <span class="font-bold text-on-surface truncate text-xs">${escapeHTML(p.name)}</span>
        <span class="text-[10px] text-primary font-semibold">${Number(p.price||0).toLocaleString('vi-VN')} ₫</span>
      </div>
      <button type="button" data-prod-id="${p.id}" class="delete-prod-btn text-on-surface-variant hover:text-error p-1 rounded transition-colors flex items-center justify-center shrink-0" title="Xóa mặt hàng">
        <span class="material-symbols-outlined text-sm">delete</span>
      </button>
    </div>
  `).join('');
  listEl.querySelectorAll('.delete-prod-btn').forEach(btn=>{
    btn.onclick=e=>{
      e.preventDefault();
      deleteProductFromShelf(btn.dataset.prodId,shelf);
    };
  });
}
function addProductToSelectedShelf(){
  if(selected?.type!=='shelf')return;
  const shelf=layout.shelves.find(x=>x.id===selected.id);
  if(!shelf)return;
  const nameInput=$('#new-prod-name'),priceInput=$('#new-prod-price');
  const name=nameInput.value.trim();
  if(!name){nameInput.focus();toast('Vui lòng nhập tên mặt hàng.');return}
  pushUndoState();
  const price=Math.max(0,Number(priceInput.value)||15000);
  const newProduct={
    id:'p'+Date.now(),
    name,
    category:shelf.category||'other',
    shelf:shelf.id,
    shelfId:shelf.id,
    price
  };
  if(!catalog)catalog=[];
  catalog.push(newProduct);
  nameInput.value='';
  priceInput.value='';
  renderShelfProducts(shelf);
  markDirty(`Đã thêm mặt hàng ${name} vào kệ.`);
  saveProject();
}
function deleteProductFromShelf(prodId,shelf){
  if(!catalog)return;
  pushUndoState();
  catalog=catalog.filter(p=>p.id!==prodId);
  renderShelfProducts(shelf);
  markDirty('Đã xóa mặt hàng khỏi kệ.');
  saveProject();
}

const undoStack=[];
const redoStack=[];
const MAX_UNDO=10;
let dragInitialState=null;

function snapshotState(){
  if(!layout)return null;
  return JSON.stringify({
    layout:JSON.parse(JSON.stringify(layout)),
    catalog:catalog?JSON.parse(JSON.stringify(catalog)):[]
  });
}

function pushUndoState(){
  const snap=snapshotState();
  if(!snap)return;
  if(undoStack.length&&undoStack[undoStack.length-1]===snap)return;
  undoStack.push(snap);
  if(undoStack.length>MAX_UNDO)undoStack.shift();
  redoStack.length=0;
  updateUndoRedoButtons();
}

function restoreState(jsonStr){
  if(!jsonStr)return;
  try{
    const state=JSON.parse(jsonStr);
    layout=state.layout;
    catalog=state.catalog;
    selected=null;
    normalizeLayout();
    renderObjects();
    renderInspector();
    markDirty('Đã khôi phục trạng thái.');
    draw();
    saveProject();
  }catch(e){
    console.error('Lỗi khi khôi phục trạng thái:',e);
  }
}

function undo(){
  if(!undoStack.length)return;
  const currentSnap=snapshotState();
  redoStack.push(currentSnap);
  const prevState=undoStack.pop();
  restoreState(prevState);
  updateUndoRedoButtons();
  toast('↩ Đã hoàn tác');
}

function redo(){
  if(!redoStack.length)return;
  const currentSnap=snapshotState();
  undoStack.push(currentSnap);
  const nextState=redoStack.pop();
  restoreState(nextState);
  updateUndoRedoButtons();
  toast('↪ Đã làm lại');
}

function updateUndoRedoButtons(){
  const undoBtn=$('#undo-btn');
  const redoBtn=$('#redo-btn');
  if(undoBtn)undoBtn.disabled=undoStack.length===0;
  if(redoBtn)redoBtn.disabled=redoStack.length===0;
}

function clearAllObjects(){
  if(!layout)return;
  pushUndoState();
  layout.walls=[];
  layout.shelves=[];
  if(catalog)catalog=[];
  layout.entrance={x:5,y:7.5};
  layout.checkout={x:7,y:7.5};
  layout.width=12;
  layout.height=8;
  selected=null;
  renderObjects();
  renderInspector();
  markDirty('Đã dọn trống toàn bộ đối tượng thiết lập.');
  draw();
  saveProject();
  toast('Đã dọn trống khu vực thiết lập');
}

function getCanvasTransform(){
  const canvas=$('#scene');
  if(!canvas)return{sx:1,sy:1,ox:0,oy:0,W:960,H:640,scale:1,worldMinX:0,worldMaxX:12,worldMinY:0,worldMaxY:8};
  const W=canvas.width,H=canvas.height;
  if(!layout)return{sx:1,sy:1,ox:0,oy:0,W,H,scale:1,worldMinX:0,worldMaxX:12,worldMinY:0,worldMaxY:8};

  let minObjX=0,minObjY=0;
  let maxObjX=12,maxObjY=8;
  if(Array.isArray(layout.walls)){
    for(const w of layout.walls){
      minObjX=Math.min(minObjX,w.x1||0,w.x2||0);
      maxObjX=Math.max(maxObjX,(w.x1||0)+0.5,(w.x2||0)+0.5);
      minObjY=Math.min(minObjY,w.y1||0,w.y2||0);
      maxObjY=Math.max(maxObjY,(w.y1||0)+0.5,(w.y2||0)+0.5);
    }
  }
  if(Array.isArray(layout.shelves)){
    for(const s of layout.shelves){
      minObjX=Math.min(minObjX,s.x||0);
      maxObjX=Math.max(maxObjX,(s.x||0)+(s.w||0)+0.5);
      minObjY=Math.min(minObjY,s.y||0);
      maxObjY=Math.max(maxObjY,(s.y||0)+(s.h||0)+0.5);
    }
  }
  if(layout.entrance){
    minObjX=Math.min(minObjX,layout.entrance.x);
    maxObjX=Math.max(maxObjX,layout.entrance.x+0.5);
    minObjY=Math.min(minObjY,layout.entrance.y);
    maxObjY=Math.max(maxObjY,layout.entrance.y+0.5);
  }
  if(layout.checkout){
    minObjX=Math.min(minObjX,layout.checkout.x);
    maxObjX=Math.max(maxObjX,layout.checkout.x+0.5);
    minObjY=Math.min(minObjY,layout.checkout.y);
    maxObjY=Math.max(maxObjY,layout.checkout.y+0.5);
  }

  const objW=Math.max(12,maxObjX-minObjX);
  const objH=Math.max(8,maxObjY-minObjY);
  const centerX=(minObjX+maxObjX)/2;
  const centerY=(minObjY+maxObjY)/2;

  const padding=24;
  const availW=Math.max(10,W-padding*2);
  const availH=Math.max(10,H-padding*2);
  const scale=Math.min(availW/objW,availH/objH);

  const ox=W/2-centerX*scale;
  const oy=H/2-centerY*scale;

  const worldMinX=-ox/scale;
  const worldMaxX=(W-ox)/scale;
  const worldMinY=-oy/scale;
  const worldMaxY=(H-oy)/scale;

  layout.width=Math.max(12,Math.ceil(maxObjX));
  layout.height=Math.max(8,Math.ceil(maxObjY));

  return{sx:scale,sy:scale,ox,oy,W,H,scale,worldMinX,worldMaxX,worldMinY,worldMaxY};
}
function canvasPoint(event){
  const canvas=$('#scene');
  if(!canvas||!layout)return{x:0,y:0};
  const r=canvas.getBoundingClientRect(),{scale,ox,oy,worldMinX,worldMaxX,worldMinY,worldMaxY}=getCanvasTransform();
  const px=event.clientX-r.left,py=event.clientY-r.top;
  const x=Math.round(((px-ox)/scale)*4)/4;
  const y=Math.round(((py-oy)/scale)*4)/4;
  const minX=Math.floor(worldMinX*4)/4;
  const maxX=Math.ceil(worldMaxX*4)/4;
  const minY=Math.floor(worldMinY*4)/4;
  const maxY=Math.ceil(worldMaxY*4)/4;
  return{
    x:clamp(x,minX,maxX),
    y:clamp(y,minY,maxY)
  };
}
function pointerDown(event){
  const p=canvasPoint(event);event.currentTarget.setPointerCapture?.(event.pointerId);
  if(currentTab==='simulate'){if(simulation&&!dirty){let nearest=null,best=.22;for(const a of simulation.agents){if(a.status==='WAITING'||a.finished)continue;const d=pointDistance(a,p);if(d<best){best=d;nearest=a}}if(nearest){selected={type:'npc',id:nearest.id};renderInspector();draw()}}return}
  if(tool==='entrance'||tool==='checkout'){pushUndoState();layout[tool]=p;markDirty();saveProject();draw();return}
  if(tool==='wall'||tool==='shelf'){dragInitialState=snapshotState();draft={start:p,end:p};return}
  if(simulation&&!dirty){let nearest=null,best=.22;for(const a of simulation.agents){if(a.status==='WAITING'||a.finished)continue;const d=pointDistance(a,p);if(d<best){best=d;nearest=a}}if(nearest){selected={type:'npc',id:nearest.id};renderInspector();draw();return}}
  const shelf=[...layout.shelves].reverse().find(s=>p.x>=s.x&&p.x<=s.x+s.w&&p.y>=s.y&&p.y<=s.y+s.h);
  if(shelf){dragInitialState=snapshotState();selected={type:'shelf',id:shelf.id};drag={kind:'shelf',dx:p.x-shelf.x,dy:p.y-shelf.y};renderObjects();renderInspector();draw();return}
  const wall=[...layout.walls].reverse().find(w=>pointSegmentDistance(p,{x:w.x1,y:w.y1},{x:w.x2,y:w.y2})<=.22);
  if(wall){
    dragInitialState=snapshotState();
    selected={type:'wall',id:wall.id};const d1=pointDistance(p,{x:wall.x1,y:wall.y1}),d2=pointDistance(p,{x:wall.x2,y:wall.y2});
    drag=Math.min(d1,d2)<=.32?{kind:'wall-end',endpoint:d1<=d2?1:2}:{kind:'wall-move',start:p,initial:{...wall}};
  }else selected=null;
  renderObjects();renderInspector();draw();
}
function pointerMove(event){
  const p=canvasPoint(event);if(draft){draft.end=p;draw();return}if(!drag)return;
  const {worldMinX,worldMaxX,worldMinY,worldMaxY}=getCanvasTransform();
  if(drag.kind==='shelf'&&selected?.type==='shelf'){const s=layout.shelves.find(x=>x.id===selected.id);s.x=clamp(p.x-drag.dx,worldMinX+.25,worldMaxX-s.w-.25);s.y=clamp(p.y-drag.dy,worldMinY+.25,worldMaxY-s.h-.25)}
  if(drag.kind==='wall-end'&&selected?.type==='wall'){const w=layout.walls.find(x=>x.id===selected.id);w[`x${drag.endpoint}`]=clamp(p.x,worldMinX,worldMaxX);w[`y${drag.endpoint}`]=clamp(p.y,worldMinY,worldMaxY)}
  if(drag.kind==='wall-move'&&selected?.type==='wall'){const w=layout.walls.find(x=>x.id===selected.id),dx=p.x-drag.start.x,dy=p.y-drag.start.y,minX=Math.min(drag.initial.x1,drag.initial.x2),maxX=Math.max(drag.initial.x1,drag.initial.x2),minY=Math.min(drag.initial.y1,drag.initial.y2),maxY=Math.max(drag.initial.y1,drag.initial.y2),safeDx=clamp(dx,worldMinX-minX,worldMaxX-maxX),safeDy=clamp(dy,worldMinY-minY,worldMaxY-maxY);w.x1=drag.initial.x1+safeDx;w.x2=drag.initial.x2+safeDx;w.y1=drag.initial.y1+safeDy;w.y2=drag.initial.y2+safeDy}
  markDirty('Layout geometry changed.');renderObjects();renderInspector();draw();
}
function pointerUp(event){
  const p=canvasPoint(event);
  let hasCreated=false;
  if(draft){
    if(tool==='wall'&&pointDistance(p,draft.start)>.4){
      const wall={id:'w'+Date.now(),x1:draft.start.x,y1:draft.start.y,x2:p.x,y2:p.y};
      layout.walls.push(wall);
      selected={type:'wall',id:wall.id};
      hasCreated=true;
    }
    if(tool==='shelf'){
      const x=Math.min(p.x,draft.start.x),y=Math.min(p.y,draft.start.y),w=Math.abs(p.x-draft.start.x),h=Math.abs(p.y-draft.start.y);
      if(w>=.5&&h>=.4){
        const category='beverage';
        const preset=SHELF_PRESETS.standard;
        const shelf={id:'s'+Date.now(),label:CATEGORY_NAMES[category]||'Đồ uống',presetId:'standard',category,x,y,w:preset.w,h:preset.h,valence:.2};
        layout.shelves.push(shelf);
        selected={type:'shelf',id:shelf.id};
        hasCreated=true;
      }
    }
    draft=null;
  }
  if((hasCreated||drag)&&dragInitialState){
    const currentSnap=snapshotState();
    if(dragInitialState!==currentSnap){
      undoStack.push(dragInitialState);
      if(undoStack.length>MAX_UNDO)undoStack.shift();
      redoStack.length=0;
      updateUndoRedoButtons();
    }
  }
  dragInitialState=null;
  drag=null;
  event.currentTarget.releasePointerCapture?.(event.pointerId);
  normalizeLayout();
  renderObjects();
  renderInspector();
  markDirty();
  draw();
  saveProject();
}

function updateShelf(){
  if(selected?.type!=='shelf')return;
  const s=layout.shelves.find(x=>x.id===selected.id);
  if(!s)return;
  const oldCategory=s.category;
  s.category=$('#shelf-category')?.value||'other';
  s.label=CATEGORY_NAMES[s.category]||s.category;
  if(oldCategory!==s.category){
    const defDims=SHELF_CATEGORY_DIMENSIONS[s.category]||{w:2.0,h:1.4};
    if((s.rotation||0)%180===90){
      s.w=defDims.h;
      s.h=defDims.w;
    }else{
      s.w=defDims.w;
      s.h=defDims.h;
    }
  }
  const valEl=$('#shelf-valence');
  if(valEl){
    s.valence=Number(valEl.value);
    const valOut=$('#shelf-valence-out');
    if(valOut)valOut.textContent=(s.valence>=0?'+':'')+s.valence.toFixed(2);
  }
  if(oldCategory!==s.category&&catalog){
    catalog.forEach(p=>{if(p.shelf===s.id||p.shelfId===s.id)p.category=s.category});
  }
  renderObjects();
  renderInspector();
  markDirty('Thuộc tính kệ hàng đã thay đổi.');
  draw();
  saveProject();
}
function updateWall(){if(selected?.type!=='wall')return;const w=layout.walls.find(x=>x.id===selected.id);for(const key of['x1','y1','x2','y2'])w[key]=clamp(Number($('#wall-'+key).value),0,key.startsWith('x')?layout.width:layout.height);renderObjects();markDirty('Wall geometry changed.');draw();saveProject()}
function deleteSelected(type){
  if(selected?.type!==type)return;
  pushUndoState();
  if(type==='shelf'){
    layout.shelves=layout.shelves.filter(s=>s.id!==selected.id);
    if(catalog)catalog=catalog.filter(p=>p.shelf!==selected.id&&p.shelfId!==selected.id);
  }else if(type==='wall'){
    layout.walls=layout.walls.filter(w=>w.id!==selected.id);
  }
  selected=null;
  renderObjects();
  renderInspector();
  markDirty('Object deleted.');
  draw();
  saveProject();
}

function draw(){
  if(!layout)return;
  const canvas=$('#scene');
  if(!canvas)return;
  const ctx=canvas.getContext('2d');
  const {sx,sy,ox,oy,W,H,worldMinX,worldMaxX,worldMinY,worldMaxY}=getCanvasTransform();
  ctx.clearRect(0,0,W,H);
  ctx.fillStyle='#1c1007';
  ctx.fillRect(0,0,W,H);
  ctx.save();
  ctx.translate(ox,oy);

  // Floor texture: each tile unit fits exactly 1x1m cell
  const startGX=Math.floor(worldMinX);
  const endGX=Math.ceil(worldMaxX);
  const startGY=Math.floor(worldMinY);
  const endGY=Math.ceil(worldMaxY);

  if(STORE_ASSETS.floor.ready && STORE_ASSETS.floor.img.naturalWidth > 0){
    ctx.imageSmoothingEnabled=false;
    for(let gx=startGX;gx<endGX;gx++){
      for(let gy=startGY;gy<endGY;gy++){
        ctx.drawImage(STORE_ASSETS.floor.img, gx*sx, gy*sy, sx, sy);
      }
    }
  } else {
    ctx.fillStyle='#1c1007';
    ctx.fillRect(worldMinX*sx, worldMinY*sy, (worldMaxX-worldMinX)*sx, (worldMaxY-worldMinY)*sy);
  }

  // Draw Walls with metallic beam styling derived from wall.png
  for(const wall of layout.walls){
    const isSelected=selected?.type==='wall'&&selected.id===wall.id;
    const dx=(wall.x2-wall.x1)*sx;
    const dy=(wall.y2-wall.y1)*sy;
    const len=Math.hypot(dx,dy);
    const angle=Math.atan2(dy,dx);

    ctx.save();
    ctx.translate(wall.x1*sx, wall.y1*sy);
    ctx.rotate(angle);

    const beamH=isSelected?12:10;
    // Dark metallic beam base
    ctx.fillStyle=isSelected?'#ffca58':'#1e1c24';
    ctx.fillRect(0, -beamH/2, len, beamH);

    // Metallic highlight strip
    ctx.fillStyle=isSelected?'#ffe082':'#3d3846';
    ctx.fillRect(0, -beamH/2, len, beamH*0.35);

    // Bevel bottom shadow
    ctx.fillStyle=isSelected?'#ffb300':'#110f14';
    ctx.fillRect(0, beamH/2-beamH*0.25, len, beamH*0.25);

    // Joint caps
    ctx.fillStyle=isSelected?'#fff3d6':'#5c5468';
    ctx.fillRect(-2, -beamH/2-1, 4, beamH+2);
    ctx.fillRect(len-2, -beamH/2-1, 4, beamH+2);

    ctx.restore();

    if(isSelected){
      for(const p of[{x:wall.x1,y:wall.y1},{x:wall.x2,y:wall.y2}]){
        ctx.fillStyle='#120a04';
        ctx.strokeStyle='#ffca58';
        ctx.lineWidth=2;
        ctx.beginPath();
        ctx.arc(p.x*sx,p.y*sy,7,0,Math.PI*2);
        ctx.fill();
        ctx.stroke();
      }
    }
  }

  // Draw Shelves (Preserve native aspect ratio without stretching/distortion)
  for(const s of layout.shelves){
    const isSelected=selected?.type==='shelf'&&selected.id===s.id;
    const asset=getShelfAsset(s);
    const rx=s.x*sx, ry=s.y*sy, rw=s.w*sx, rh=s.h*sy;

    ctx.save();
    ctx.translate(rx+rw/2, ry+rh/2);

    const rotation=(s.rotation||0)*Math.PI/180;
    if(rotation) ctx.rotate(rotation);

    const scaleX=s.flipX?-1:1;
    const scaleY=s.flipY?-1:1;
    if(scaleX!==1||scaleY!==1) ctx.scale(scaleX,scaleY);

    if(asset?.ready && asset.img.naturalWidth > 0){
      const imgRatio=asset.img.naturalWidth/asset.img.naturalHeight;
      const boxRatio=rw/rh;
      let drawW=rw, drawH=rh;
      if(boxRatio>imgRatio){
        drawW=rh*imgRatio;
      }else{
        drawH=rw/imgRatio;
      }
      ctx.imageSmoothingEnabled=false;
      ctx.drawImage(asset.img, -drawW/2, -drawH/2, drawW, drawH);
    } else {
      ctx.fillStyle='#2e1509';
      ctx.fillRect(-rw/2, -rh/2, rw, rh);
    }
    ctx.restore();

    // Clean selection highlight box if selected
    if(isSelected){
      ctx.strokeStyle='#ffca58';
      ctx.lineWidth=2;
      ctx.strokeRect(rx, ry, rw, rh);
      ctx.fillStyle='#ffca58';
      const sz=5;
      ctx.fillRect(rx-sz/2, ry-sz/2, sz, sz);
      ctx.fillRect(rx+rw-sz/2, ry-sz/2, sz, sz);
      ctx.fillRect(rx-sz/2, ry+rh-sz/2, sz, sz);
      ctx.fillRect(rx+rw-sz/2, ry+rh-sz/2, sz, sz);
    }
  }

  // Entrance & Checkout Markers (Proportional crisp pixel-art sprites)
  drawMarkerWithAsset(ctx, layout.entrance, STORE_ASSETS.entrance, '🚪', 'LỐI VÀO', '#5dba4f', sx, sy);
  drawMarkerWithAsset(ctx, layout.checkout, STORE_ASSETS.checkout, '🛒', 'THU NGÂN', '#e05252', sx, sy);

  if(draft){
    ctx.strokeStyle='#ffca58';
    ctx.setLineDash([6,4]);
    ctx.lineWidth=2;
    if(tool==='wall'){
      ctx.beginPath();
      ctx.moveTo(draft.start.x*sx,draft.start.y*sy);
      ctx.lineTo(draft.end.x*sx,draft.end.y*sy);
      ctx.stroke();
    } else {
      ctx.strokeRect(draft.start.x*sx,draft.start.y*sy,(draft.end.x-draft.start.x)*sx,(draft.end.y-draft.start.y)*sy);
    }
    ctx.setLineDash([]);
  }

  if(simulation&&!dirty){
    const visibleAgents=[];
    const agentsList = rewindTime !== null ? getRewindAgents(rewindTime) : simulation.agents;
    for(const agent of agentsList){
      if(agent.status==='WAITING'||agent.finished)continue;
      const shelfId=agent.currentShelf||agent.targetId,shelf=agent.status==='DWELL'&&shelfId?layout.shelves.find(item=>item.id===shelfId):null;
      visibleAgents.push(shelf?{...agent,facingDx:(shelf.x+shelf.w/2)-agent.x,facingDy:(shelf.y+shelf.h/2)-agent.y}:agent);
      if(agent.trail&&agent.trail.length>1){
        ctx.strokeStyle=(colors[agent.status]||'#5fa8d3')+'35';
        ctx.lineWidth=1;
        ctx.beginPath();
        agent.trail.forEach((p,i)=>i?ctx.lineTo(p.x*sx,p.y*sy):ctx.moveTo(p.x*sx,p.y*sy));
        ctx.stroke();
      }
      if(selected?.type==='npc'&&selected.id===agent.id&&agent.path&&agent.path.length){
        ctx.strokeStyle='#ffffff90';
        ctx.setLineDash([5,4]);
        ctx.beginPath();
        ctx.moveTo(agent.x*sx,agent.y*sy);
        for(let i=agent.pathIndex;i<agent.path.length;i++)ctx.lineTo(agent.path[i].x*sx,agent.path[i].y*sy);
        ctx.stroke();
        ctx.setLineDash([]);
      }
    }
    npcRenderer.draw(ctx,visibleAgents,{runSeed:simulation.seed,animationTimeMs:performance.now(),running:playing&&(rewindTime===null),scaleX:sx,scaleY:sy,selectedId:selected?.type==='npc'?selected.id:null,fallbackColors:colors});
  }else npcRenderer.draw(ctx,[],{runSeed:lastRunSeed,animationTimeMs:performance.now(),running:false,scaleX:sx,scaleY:sy});
  ctx.restore();
  const displayTime = rewindTime !== null ? rewindTime : (simulation?.time || 0);
  $('#clock').textContent=formatTime(displayTime);
  $('#timeline').value=simulation?(displayTime/durationSeconds())*1000:0;
  const activeCount = rewindTime !== null 
    ? (getRewindAgents(rewindTime).filter(a => a.status !== 'WAITING' && !a.finished).length)
    : (simulation?.snapshot().active || 0);
  $('#active-count').textContent=`${activeCount} khách đang trong cửa hàng`;
  ctx.textAlign='left'}

function drawMarkerWithAsset(ctx, p, asset, icon, label, color, sx, sy){
  if(!p) return;
  if(asset?.ready && asset.img.naturalWidth > 0){
    const imgRatio=asset.img.naturalWidth/asset.img.naturalHeight;
    const baseH=(asset===STORE_ASSETS.checkout?2.4:1.6)*sy;
    const baseW=baseH*imgRatio;
    const mx=p.x*sx-baseW/2, my=p.y*sy-baseH/2;
    ctx.imageSmoothingEnabled=false;
    ctx.drawImage(asset.img, mx, my, baseW, baseH);
  } else {
    marker(ctx, p, icon, label, color, sx, sy);
  }
}

function marker(ctx,p,icon,label,color,sx,sy){if(!p)return;ctx.fillStyle='#120a04';ctx.strokeStyle=color;ctx.lineWidth=2;ctx.beginPath();ctx.arc(p.x*sx,p.y*sy,14,0,Math.PI*2);ctx.fill();ctx.stroke();ctx.fillStyle=color;ctx.font='bold 14px "Nunito Sans", sans-serif';ctx.textAlign='center';ctx.fillText(icon,p.x*sx,p.y*sy+5);ctx.font='bold 10px "Nunito Sans", sans-serif';ctx.fillText(label,p.x*sx,p.y*sy-20)}

function openManual(){const columns='npc_id,target_category,need_product,need_growth,need_explore,explore_growth,attractor,stability,dispersion,recovery,speed,dwell,steadiness',sample='test_001,beverage,0.8,0.02,0.25,0.01,0.3,0.65,0.4,0.15,1.3,9,0.75';$('#manual-editor').value=columns+'\n'+(manualRows.length?manualRows.map(row=>columns.split(',').map(k=>row[k]??'').join(',')).join('\n'):sample);$('#manual-error').textContent='Values are clamped to safe ranges.';$('#manual-dialog').showModal()}
function applyManual(){try{const lines=$('#manual-editor').value.trim().split(/\r?\n/),headers=lines.shift().split(',').map(x=>x.trim());manualRows=lines.filter(Boolean).map(line=>Object.fromEntries(headers.map((h,i)=>[h,line.split(',')[i]?.trim()??''])));manualPopulation(manualRows);if(!manualRows.length)throw new Error('Enter at least one NPC row.');$('#manual-count').textContent=manualRows.length;$('#population-mode').value='manual';$('#npc-count').disabled=true;$('#manual-dialog').close();markDirty(`${manualRows.length} manual NPC inputs applied.`)}catch(error){$('#manual-error').textContent=error.message;$('#manual-error').style.color='#e05252'}}
async function saveProject(){normalizeLayout();try{const result=await api('/api/project',{method:'POST',body:JSON.stringify({layout,catalog})});$('#save-state').textContent='● Saved';if(result.warnings?.length){const message=`Saved with warning: ${result.warnings.join(' ')}`;showSystemEvent(message);toast(`${result.warnings.length} layout warning${result.warnings.length>1?'s':''}`)}}catch(error){$('#save-state').textContent='● Unsaved';showSystemEvent(error.message);toast(error.message)}}
async function currentSimResult(){if(simResult&&simulation.completed)return simResult;const result=await simulation.result($('#run-name').value);if(simulation.completed)simResult=result;return result}
async function saveSimulationSession(simToSave = simulation, force = false){
  if(!simToSave) return;
  const snap = simToSave.snapshot?.() || {};
  if(!simToSave.completed && !force && (simToSave.time || 0) < 0.1 && (snap.spawned || 0) < 1) return;

  const runName = $('#run-name')?.value?.trim() || 'Cửa hàng tiện lợi Cozy';
  const nowIso = new Date().toISOString();
  let runId = simToSave.seed || ('sim-' + Date.now() + '-' + Math.random().toString(36).slice(2, 6));

  try{
    let res = null;
    if(typeof simToSave.result === 'function'){
      try{
        res = await simToSave.result(runName);
      }catch(e){
        console.warn('simToSave.result failed:', e);
      }
    }

    if(!res){
      res = {
        schemaVersion: 'aisle.sim-result.v1',
        id: runId,
        createdAt: nowIso,
        name: runName,
        summary: {
          durationSeconds: simToSave.time || 0,
          revenue: snap.revenue || 0,
          purchases: snap.purchases || 0,
          spawned: snap.spawned || 0,
          converted: (simToSave.stats?.converted) || 0,
          mainBuyers: (simToSave.stats?.mainBuyers) || 0,
          impulseBuyers: (simToSave.stats?.impulseBuyers) || 0,
          notFound: (simToSave.stats?.notFound) || 0,
          unreachable: (simToSave.stats?.unreachable) || 0,
          stuckRecoveries: (simToSave.stats?.stuckRecoveries) || 0,
          completed: Boolean(simToSave.completed)
        },
        events: simToSave.events || [],
        purchases: simToSave.purchases || [],
        replay: { sampleSeconds: 0.5, columns: ['time','x','y','status','shelfId'], agents: [] }
      };
    }

    let savedId = res.id || runId;

    if(window.aisleBridge && typeof window.aisleBridge.request === 'function'){
      try{
        const saved = await window.aisleBridge.request('history.save', { result: res });
        if(saved?.id) savedId = saved.id;
        showSystemEvent(`Đã lưu phiên vào lịch sử: ${savedId}`);
      }catch(bridgeErr){
        console.warn('history.save bridge error, retrying with new ID:', bridgeErr);
        res.id = 'sim-' + Date.now() + '-' + Math.random().toString(36).slice(2, 8);
        try{
          const saved = await window.aisleBridge.request('history.save', { result: res });
          if(saved?.id) savedId = saved.id;
          showSystemEvent(`Đã lưu phiên vào lịch sử: ${savedId}`);
        }catch(e2){
          console.error('Second history.save attempt failed:', e2);
        }
      }
    }

    try{
      const localRuns = JSON.parse(localStorage.getItem('aisle_history_runs') || '[]');
      const filtered = localRuns.filter(r => r.id !== savedId);
      filtered.unshift({
        id: savedId,
        name: res.name || runName,
        createdAt: res.createdAt || nowIso,
        summary: res.summary || snap
      });
      localStorage.setItem('aisle_history_runs', JSON.stringify(filtered.slice(0, 100)));
    }catch(e){}

    await loadHistoryList();
  }catch(error){
    console.error('saveSimulationSession error:', error);
    showSystemEvent(`History save error: ${error.message}`);
  }
}
async function exportSimulation(){if(!simulation||dirty)return toast('Run or Step the current inputs before exporting.');const payload=await currentSimResult(),blob=new Blob([JSON.stringify(payload,null,2)],{type:'application/json'}),a=document.createElement('a');a.href=URL.createObjectURL(blob);a.download=`aisle-${payload.id}.sim-result.json`;a.click();URL.revokeObjectURL(a.href)}
function durationSeconds(){return Number($('#duration').value)*60}function formatTime(seconds){return`${String(Math.floor(seconds/60)).padStart(2,'0')}:${String(Math.floor(seconds%60)).padStart(2,'0')}`}

init().catch(error=>showSystemEvent(error.message));
