import {DEFAULT_PARAMETERS, manualPopulation} from './live-engine.js';
import {NativeSimulationAdapter} from './native-simulation.mjs';
import {NpcSpriteRenderer, NPC_SPRITE_ASSETS} from './npc-renderer.mjs';
import {validateLayout} from './layout-validation.js';

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
const colors={WAITING:'#8b6b4a',DECIDING:'#a87bca',TRANSIT:'#5fa8d3',QUEUE:'#d59b45',DWELL:'#ffca58',PURCHASED:'#5dba4f',CHECKOUT:'#e05252',LEAVING:'#e05252'};
const npcRenderer=new NpcSpriteRenderer({assets:NPC_SPRITE_ASSETS});
let currentTab='setup';
let lastPurchaseCount=0;
let lastFinishedCount=0;
const cashierMoods=['Yay! Có khách mua rồi! 🎉','Cảm ơn quý khách! ♡','Hàng bán chạy quá! ✨','Tuyệt vời! 💰','Vui ghê, thêm một đơn! 🌟','Khách ơi quay lại nha~ 💕'];
function switchTab(tab){
  currentTab=tab;
  document.body.dataset.tab=tab;
  $$('.tab-btn').forEach(b=>b.classList.toggle('active',b.dataset.tab===tab));
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
    document.querySelectorAll('.screen').forEach(s=>s.classList.remove('active'));
    const el=document.getElementById(targetScreen);
    if(el)el.classList.add('active');
    const canvas=$('#scene');
    if(canvas){
      if(tab==='setup'){const wrap=$('#canvas-wrapper');if(wrap)wrap.appendChild(canvas)}
      else if(tab==='simulate'){const wrap=$('#sim-canvas-container');if(wrap)wrap.appendChild(canvas)}
    }
    if(targetScreen==='screen-results'){
      loadHistoryList();
    }
    if(targetScreen==='screen-analytics'&&typeof window.initCharts==='function'){
      window.initCharts();
    }
  }
  if(tab==='setup'&&playing){
    playing=false;
    simulation?.pause().catch(error=>showSystemEvent(error.message));
    $('#play-btn').textContent='▶ Run live';
    $('#stage-status').textContent='PAUSED';
  }
  if(tab==='setup'){
    tool='select';
    $$('[data-tool]').forEach(b=>b.classList.toggle('active',b.dataset.tool==='select'));
    $('#stage-status').textContent='EDIT MODE';
    selected=null;
    renderInspector();
  }
  if(tab==='simulate'){
    selected=null;
    renderInspector();
    $('#stage-status').textContent=simulation&&!dirty?(playing?'RUNNING LIVE':'READY TO RUN'):'READY TO RUN';
  }
  requestAnimationFrame(()=>{resizeCanvas();draw()});
}
window.switchTab = switchTab;
function triggerCashierReaction(type='happy'){const avatar=$('#cashier-avatar'),mood=$('#cashier-mood');if(!avatar)return;avatar.classList.remove('cashier-react','cashier-smile','cashier-sad');mood.classList.remove('happy','sad');void avatar.offsetWidth;if(type==='happy'){avatar.classList.add('cashier-react');mood.textContent=cashierMoods[Math.floor(Math.random()*cashierMoods.length)];mood.classList.add('happy')}else if(type==='smile'){avatar.classList.add('cashier-smile');mood.textContent='Cảm ơn quý khách~';mood.classList.add('happy')}else if(type==='sad'){avatar.classList.add('cashier-sad');mood.textContent='Trời ơi, hổng mua gì sao...';mood.classList.add('sad')}clearTimeout(triggerCashierReaction.t);triggerCashierReaction.t=setTimeout(()=>{avatar.classList.remove('cashier-react','cashier-smile','cashier-sad');mood.textContent='Đang chờ khách...';mood.classList.remove('happy','sad')},2800)}
function updateCashier(){if(!simulation)return;const s=simulation.snapshot();const served=simulation.stats.converted||0;const rev=s.revenue||0;$('#cashier-served').textContent=served;$('#cashier-revenue').textContent=money(rev);if(s.purchases>lastPurchaseCount&&lastPurchaseCount>=0){const latest=simulation.purchases[simulation.purchases.length-1];if(latest&&latest.price<10000){triggerCashierReaction('smile')}else{triggerCashierReaction('happy')}}else{const finishedCount=simulation.agents.filter(a=>a.finished).length;if(finishedCount>lastFinishedCount){const finished=simulation.agents.filter(a=>a.finished);const last=finished[finished.length-1];if(!last.converted){triggerCashierReaction('sad')}}lastFinishedCount=finishedCount}lastPurchaseCount=s.purchases}

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

async function init(){const project=await api('/api/project');layout=project.layout;catalog=project.catalog;await npcRenderer.load();bind();buildParameterLab();switchTab('setup');renderObjects();renderInspector();draw();loadHistoryList();showSystemEvent('Ready. One click on Run live starts both the engine and visualization.')}

function bind(){
  $$('[data-tool]').forEach(button=>button.onclick=()=>{$$('[data-tool]').forEach(x=>x.classList.toggle('active',x===button));tool=button.dataset.tool;$('#stage-status').textContent='EDIT MODE'});
  $('#npc-count').oninput=e=>{$('#npc-output').textContent=e.target.value;markDirty()};
  $('#duration').oninput=e=>{$('#duration-output').textContent=e.target.value+' min';markDirty()};
  $('#population-mode').onchange=()=>{$('#npc-count').disabled=$('#population-mode').value==='manual';markDirty()};
  $('#manual-btn').onclick=openManual;$('#apply-manual').onclick=applyManual;
  const btnParam=$('#parameter-btn');if(btnParam)btnParam.onclick=()=>{$('#parameter-dialog').showModal()};
  const btnOut=$('#output-toggle');if(btnOut)btnOut.onclick=()=>{const collapsed=document.body.classList.toggle('outputs-collapsed');btnOut.setAttribute('aria-pressed',String(collapsed));btnOut.textContent=collapsed?'▤ Show output':'▤ Output';requestAnimationFrame(resizeCanvas)};
  $('#apply-parameters').onclick=applyParameters;$('#reset-parameters').onclick=()=>{parameters={...DEFAULT_PARAMETERS};buildParameterLab()};
  $('#play-btn').onclick=toggleRun;$('#reset-btn').onclick=resetSimulation;const btnStep=$('#step-btn');if(btnStep)btnStep.onclick=singleStep;
  $('#speed').onchange=async()=>{try{if(simulation)await simulation.setSpeed(Number($('#speed').value));showSystemEvent(`Playback speed ${$('#speed').value}×. Physics tick remains ${parameters.tickSeconds}s.`)}catch(error){showSystemEvent(error.message)}};
  $('#timeline').onchange=e=>seekTo(Number(e.target.value)/1000*durationSeconds());
  $('#add-wall').onclick=()=>{const id='w'+Date.now();layout.walls.push({id,x1:4,y1:3,x2:6,y2:3});selected={type:'wall',id};renderObjects();renderInspector();markDirty('Wall added.');draw();saveProject()};
  $('#add-shelf').onclick=()=>{const id='s'+Date.now();const preset=SHELF_PRESETS.standard;const category='beverage';const label=CATEGORY_NAMES[category]||'Đồ uống';layout.shelves.push({id,label,presetId:'standard',category,x:4,y:3,w:preset.w,h:preset.h,valence:.2});selected={type:'shelf',id};renderObjects();renderInspector();markDirty('Kệ hàng mới đã được thêm.');draw();saveProject()};
  $('#export-btn').onclick=exportSimulation;
  const canvas=$('#scene');canvas.onpointerdown=pointerDown;canvas.onpointermove=pointerMove;canvas.onpointerup=pointerUp;
  new ResizeObserver(resizeCanvas).observe(canvas);
  ['shelf-preset','shelf-category','shelf-valence'].forEach(id=>{const el=$('#'+id);if(el)el.oninput=el.onchange=updateShelf});
  const addProdBtn=$('#add-shelf-product-btn');if(addProdBtn)addProdBtn.onclick=addProductToSelectedShelf;
  ['wall-x1','wall-y1','wall-x2','wall-y2'].forEach(id=>$('#'+id).oninput=updateWall);
  $('#delete-shelf').onclick=()=>deleteSelected('shelf');
  $('#delete-wall').onclick=()=>deleteSelected('wall');
  $$('.tab-btn').forEach(btn=>btn.onclick=()=>switchTab(btn.dataset.tab));
  const inputRunName=$('#run-name');if(inputRunName)inputRunName.oninput=()=>markDirty('Tên cửa hàng đã thay đổi.');
  const btnNew=$('#btn-new');if(btnNew)btnNew.onclick=()=>switchTab('setup');
  const btnLoad=$('#btn-load');if(btnLoad)btnLoad.onclick=()=>switchTab('results');
  const btnRunSim=$('#btn-run-sim');if(btnRunSim)btnRunSim.onclick=()=>{checkLayoutAndNotify();switchTab('simulate');};
  const btnBackSetup=$('#btn-back-setup');if(btnBackSetup)btnBackSetup.onclick=()=>switchTab('setup');
  const btnEvaluate=$('#btn-evaluate');if(btnEvaluate)btnEvaluate.onclick=()=>switchTab('analytics');
  const btnResBackSetup=$('#btn-results-back-setup');if(btnResBackSetup)btnResBackSetup.onclick=()=>switchTab('setup');
  const btnResBackSim=$('#btn-results-back-simulate');if(btnResBackSim)btnResBackSim.onclick=()=>switchTab('simulate');
  const btnAnaBackSetup=$('#btn-analytics-back-setup');if(btnAnaBackSetup)btnAnaBackSetup.onclick=()=>switchTab('setup');
  const btnAnaBackRes=$('#btn-analytics-back-results');if(btnAnaBackRes)btnAnaBackRes.onclick=()=>switchTab('results');
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
    if(titleHeading)titleHeading.textContent='Live Metrics';
  }else{
    viewMetrics?.classList.add('hidden');
    viewLog?.classList.remove('hidden');
    btnLog?.classList.add('bg-primary','text-on-primary','shadow-xs');
    btnLog?.classList.remove('text-on-surface-variant');
    btnMetrics?.classList.remove('bg-primary','text-on-primary','shadow-xs');
    btnMetrics?.classList.add('text-on-surface-variant');
    if(titleIcon)titleIcon.textContent='receipt_long';
    if(titleHeading)titleHeading.textContent='Decision Trace';
  }
}

function resizeCanvas(){
  const canvas=$('#scene'),rect=canvas.getBoundingClientRect();
  const width=Math.max(1,Math.floor(rect.width)),height=Math.max(1,Math.floor(rect.height));
  if(canvas.width!==width||canvas.height!==height){canvas.width=width;canvas.height=height}
  if(layout){
    let maxObjX=12,maxObjY=8;
    if(Array.isArray(layout.walls)){
      for(const w of layout.walls){
        maxObjX=Math.max(maxObjX,w.x1||0,w.x2||0);
        maxObjY=Math.max(maxObjY,w.y1||0,w.y2||0);
      }
    }
    if(Array.isArray(layout.shelves)){
      for(const s of layout.shelves){
        maxObjX=Math.max(maxObjX,(s.x||0)+(s.w||0));
        maxObjY=Math.max(maxObjY,(s.y||0)+(s.h||0));
      }
    }
    if(layout.entrance){maxObjX=Math.max(maxObjX,layout.entrance.x);maxObjY=Math.max(maxObjY,layout.entrance.y)}
    if(layout.checkout){maxObjX=Math.max(maxObjX,layout.checkout.x);maxObjY=Math.max(maxObjY,layout.checkout.y)}

    const baseHeight=Math.max(8,Math.ceil(maxObjY));
    const scale=height/baseHeight;
    const fitWidth=Math.round((width/scale)*2)/2;
    layout.width=Math.max(maxObjX,fitWidth);
    layout.height=baseHeight;
  }
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
function mapManualProfile(profile){return{id:profile.id,walkingSpeed:profile.speed,patience:.5,exploration:profile.needExplore,sociability:.5,impulsiveness:.5,crowdTolerance:.5,priceSensitivity:.5,targetCategory:profile.target||'',initialNeed:profile.needProduct,needGrowthPerMinute:profile.needGrowth,initialExplorationNeed:profile.needExplore,explorationGrowthPerMinute:profile.exploreGrowth,affectAttractor:profile.attractor,affectStability:profile.stability,affectDispersion:profile.dispersion,affectRecovery:profile.recovery,dwellSeconds:profile.dwell,categoryPreferences:[],shoppingMission:0}}
function simulationInput(profiles){return{name:$('#run-name').value,layout:{width:layout.width,height:layout.height,walls:layout.walls,shelves:layout.shelves.map(shelf=>({...shelf,width:shelf.w,height:shelf.h})),entrance:layout.entrance,checkout:layout.checkout,spawnRateCurve:layout.spawnRateCurve||[]},catalog:catalog.map(product=>({...product,shelfId:product.shelf})),population:{populationId:`desktop-${crypto.randomUUID()}`,npcProfiles:profiles,metadata:{generatorName:$('#population-mode').value==='manual'?'manual-input':'GeneticSharp',generatorVersion:'desktop-bridge'}},config:{...parameters,durationMinutes:Number($('#duration').value)}}}
async function createSimulation(){if(!window.aisleBridge?.request)throw new Error('Run Live requires the AIsle Desktop bridge.');checkLayoutAndNotify();const profiles=await population();const adapter=new NativeSimulationAdapter(window.aisleBridge,profiles,durationSeconds());await adapter.start(simulationInput(profiles));await adapter.setSpeed(Number($('#speed').value));simulation=adapter;lastRunSeed=adapter.seed;npcRenderer.reset(adapter.seed,performance.now());simResult=null;dirty=false;selected=null;lastPurchaseCount=0;lastFinishedCount=0;updateAll();return simulation}
async function toggleRun(){try{if(dirty||!simulation||simulation.completed){await createSimulation();playing=simulation.running}else if(playing){await simulation.pause();playing=false}else{await simulation.setSpeed(Number($('#speed').value));await simulation.resume();playing=simulation.running}$('#play-btn').textContent=playing?'❚❚ Tạm dừng':'▶ Chạy trực tiếp';$('#stage-status').textContent=playing?'ĐANG CHẠY MÔ PHỎNG':'TẠM DỪNG';if(playing){lastFrame=0;requestAnimationFrame(frame)}}catch(error){playing=false;toast(error.message);showSystemEvent(error.message)}}
async function resetSimulation(){playing=false;try{await createSimulation();await simulation.reset();await simulation.pause();$('#play-btn').textContent='▶ Chạy trực tiếp';$('#stage-status').textContent='ĐÃ ĐẶT LẠI · T=0';showSystemEvent('Đã đặt lại phiên mô phỏng mới.')}catch(error){toast(error.message);showSystemEvent(error.message)}}
async function singleStep(){playing=false;try{const created=dirty||!simulation||simulation.completed;if(created){await createSimulation();await simulation.pause()}else await simulation.step();$('#play-btn').textContent='▶ Chạy trực tiếp';$('#stage-status').textContent=`BƯỚC ĐƠN · Δt=${parameters.tickSeconds}s`;updateAll()}catch(error){toast(error.message);showSystemEvent(error.message)}}
async function frame(now){
  if(!playing||!simulation)return;
  if(now-lastFrame>=50){
    lastFrame=now;
    try{
      await simulation.refresh();
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

function renderHistoryRow(item){
  const name=item.name||item.Name||'Phiên mô phỏng';
  const date=new Date(item.createdAt||item.CreatedAt||Date.now());
  const timeStr=isNaN(date.getTime())?'--:--':date.toLocaleTimeString('vi-VN',{hour:'2-digit',minute:'2-digit'});
  const dateStr=isNaN(date.getTime())?'':date.toLocaleDateString('vi-VN');
  const summary=item.summary||item.Summary||{};
  const spawned=summary.spawned??summary.Spawned??0;
  const converted=summary.converted??summary.Converted??0;
  const revenue=money(summary.revenue??summary.Revenue??0);

  return `
    <div class="grid grid-cols-4 gap-4 px-6 hover:bg-surface-bright transition-colors duration-300 items-center group cursor-default py-6 border-b border-surface-container-low/50">
      <div class="flex items-center gap-3">
        <div class="w-9 h-9 rounded-full bg-primary-container flex items-center justify-center text-primary group-hover:scale-110 transition-transform shadow-xs">
          <span class="material-symbols-outlined text-base" style="font-variation-settings: 'FILL' 1;">storefront</span>
        </div>
        <div>
          <div class="font-label-md text-sm text-on-surface font-bold">${escapeHTML(name)}</div>
          <div class="text-[11px] text-on-surface-variant opacity-70">${dateStr ? 'Ngày '+dateStr : 'Lưu gần đây'}</div>
        </div>
      </div>
      <div class="font-body-md text-sm text-on-surface-variant flex items-center gap-1.5">
        <span class="material-symbols-outlined text-sm opacity-70">schedule</span> ${timeStr}
      </div>
      <div class="font-label-md text-on-surface text-right">
        <span class="inline-flex items-center justify-center bg-tertiary-container text-on-tertiary-container px-3 py-1 rounded-full text-xs font-bold shadow-xs">
          ${spawned} khách (${converted} đã mua)
        </span>
      </div>
      <div class="font-label-md text-secondary text-right font-bold text-base md:text-lg">${revenue}</div>
    </div>
  `;
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

  const map=new Map();
  for(const item of [...bridgeItems, ...localItems]){
    const key=item.id||item.Id||(item.createdAt||item.CreatedAt||'')+(item.name||item.Name||'');
    if(key && !map.has(key)){
      map.set(key, item);
    }
  }
  const merged=[...map.values()];
  merged.sort((a,b)=>{
    const da=new Date(a.createdAt||a.CreatedAt||0).getTime();
    const db=new Date(b.createdAt||b.CreatedAt||0).getTime();
    return db-da;
  });

  const emptyState=$('#results-empty-state');
  if(merged.length>0){
    if(emptyState)emptyState.remove();
    tableBody.innerHTML=merged.map(renderHistoryRow).join('');
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

function updateResultsScreen(){
  loadHistoryList();
}
async function seekTo(){if(!simulation)return;playing=false;try{await simulation.pause();$('#play-btn').textContent='▶ Run live';$('#timeline').value=simulation.time/durationSeconds()*1000;$('#stage-status').textContent='PAUSED · USE HISTORY FOR REPLAY';showSystemEvent('Live timeline does not re-simulate. Open the saved run in History Replay.')}catch(error){toast(error.message)}}
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
    const presetEl=$('#shelf-preset');if(presetEl)presetEl.value=shelf.presetId||'standard';
    const valEl=$('#shelf-valence');if(valEl)valEl.value=shelf.valence;
    const valOut=$('#shelf-valence-out');if(valOut)valOut.textContent=(shelf.valence>=0?'+':'')+Number(shelf.valence).toFixed(2);
    renderShelfProducts(shelf);
  }
  if(agent){$('#npc-inspector').innerHTML=`<h3 class="font-bold text-base text-primary">${escapeHTML(agent.id)}</h3><p class="text-xs text-on-surface">Trạng thái: <b>${agent.status}</b></p><p class="text-xs text-on-surface">Mục tiêu: <b>${escapeHTML(agent.target||'Chỉ xem dạo')}</b></p><p class="text-[11px] text-on-surface-variant">Nguồn: <b>C# Simulation Core</b></p>`}
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
  catalog=catalog.filter(p=>p.id!==prodId);
  renderShelfProducts(shelf);
  markDirty('Đã xóa mặt hàng khỏi kệ.');
  saveProject();
}
function updateShelf(){
  if(selected?.type!=='shelf')return;
  const s=layout.shelves.find(x=>x.id===selected.id);
  if(!s)return;
  const oldCategory=s.category;
  s.category=$('#shelf-category')?.value||'other';
  s.label=CATEGORY_NAMES[s.category]||s.category;
  s.presetId=$('#shelf-preset')?.value||'standard';
  const preset=SHELF_PRESETS[s.presetId]||SHELF_PRESETS.standard;
  s.w=preset.w;
  s.h=preset.h;
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
  renderShelfProducts(s);
  markDirty('Thuộc tính kệ hàng đã thay đổi.');
  draw();
  saveProject();
}
function updateWall(){if(selected?.type!=='wall')return;const w=layout.walls.find(x=>x.id===selected.id);for(const key of['x1','y1','x2','y2'])w[key]=clamp(Number($('#wall-'+key).value),0,key.startsWith('x')?layout.width:layout.height);renderObjects();markDirty('Wall geometry changed.');draw();saveProject()}
function deleteSelected(type){
  if(selected?.type!==type)return;
  if(type==='shelf'){
    layout.shelves=layout.shelves.filter(s=>s.id!==selected.id);
    if(catalog)catalog=catalog.filter(p=>p.shelf!==selected.id&&p.shelfId!==selected.id);
  }else layout.walls=layout.walls.filter(w=>w.id!==selected.id);
  selected=null;
  renderObjects();
  renderInspector();
  markDirty(`${type==='wall'?'Wall':'Shelf'} removed.`);
  draw();
  saveProject();
}

function getCanvasTransform(){const canvas=$('#scene'),W=canvas.width,H=canvas.height;if(!layout)return{sx:1,sy:1,ox:0,oy:0,W,H,scale:1};const scale=Math.min(W/layout.width,H/layout.height);const ox=(W-layout.width*scale)/2,oy=(H-layout.height*scale)/2;return{sx:scale,sy:scale,ox,oy,W,H,scale}}
function canvasPoint(event){const r=$('#scene').getBoundingClientRect(),{scale,ox,oy}=getCanvasTransform();const px=event.clientX-r.left,py=event.clientY-r.top;return{x:clamp(Math.round(((px-ox)/scale)*4)/4,0,layout.width),y:clamp(Math.round(((py-oy)/scale)*4)/4,0,layout.height)}}
function pointerDown(event){
  const p=canvasPoint(event);event.currentTarget.setPointerCapture?.(event.pointerId);
  if(currentTab==='simulate'){if(simulation&&!dirty){let nearest=null,best=.22;for(const a of simulation.agents){if(a.status==='WAITING'||a.finished)continue;const d=pointDistance(a,p);if(d<best){best=d;nearest=a}}if(nearest){selected={type:'npc',id:nearest.id};renderInspector();draw()}}return}
  if(tool==='entrance'||tool==='checkout'){layout[tool]=p;markDirty();saveProject();draw();return}
  if(tool==='wall'||tool==='shelf'){draft={start:p,end:p};return}
  if(simulation&&!dirty){let nearest=null,best=.22;for(const a of simulation.agents){if(a.status==='WAITING'||a.finished)continue;const d=pointDistance(a,p);if(d<best){best=d;nearest=a}}if(nearest){selected={type:'npc',id:nearest.id};renderInspector();draw();return}}
  const shelf=[...layout.shelves].reverse().find(s=>p.x>=s.x&&p.x<=s.x+s.w&&p.y>=s.y&&p.y<=s.y+s.h);
  if(shelf){selected={type:'shelf',id:shelf.id};drag={kind:'shelf',dx:p.x-shelf.x,dy:p.y-shelf.y};renderObjects();renderInspector();draw();return}
  const wall=[...layout.walls].reverse().find(w=>pointSegmentDistance(p,{x:w.x1,y:w.y1},{x:w.x2,y:w.y2})<=.22);
  if(wall){
    selected={type:'wall',id:wall.id};const d1=pointDistance(p,{x:wall.x1,y:wall.y1}),d2=pointDistance(p,{x:wall.x2,y:wall.y2});
    drag=Math.min(d1,d2)<=.32?{kind:'wall-end',endpoint:d1<=d2?1:2}:{kind:'wall-move',start:p,initial:{...wall}};
  }else selected=null;
  renderObjects();renderInspector();draw();
}
function pointerMove(event){
  const p=canvasPoint(event);if(draft){draft.end=p;draw();return}if(!drag)return;
  if(drag.kind==='shelf'&&selected?.type==='shelf'){const s=layout.shelves.find(x=>x.id===selected.id);s.x=clamp(p.x-drag.dx,.25,layout.width-s.w-.25);s.y=clamp(p.y-drag.dy,.25,layout.height-s.h-.25)}
  if(drag.kind==='wall-end'&&selected?.type==='wall'){const w=layout.walls.find(x=>x.id===selected.id);w[`x${drag.endpoint}`]=clamp(p.x,0,layout.width);w[`y${drag.endpoint}`]=clamp(p.y,0,layout.height)}
  if(drag.kind==='wall-move'&&selected?.type==='wall'){const w=layout.walls.find(x=>x.id===selected.id),dx=p.x-drag.start.x,dy=p.y-drag.start.y,minX=Math.min(drag.initial.x1,drag.initial.x2),maxX=Math.max(drag.initial.x1,drag.initial.x2),minY=Math.min(drag.initial.y1,drag.initial.y2),maxY=Math.max(drag.initial.y1,drag.initial.y2),safeDx=clamp(dx,-minX,layout.width-maxX),safeDy=clamp(dy,-minY,layout.height-maxY);w.x1=drag.initial.x1+safeDx;w.x2=drag.initial.x2+safeDx;w.y1=drag.initial.y1+safeDy;w.y2=drag.initial.y2+safeDy}
  markDirty('Layout geometry changed.');renderObjects();renderInspector();draw();
}
function pointerUp(event){
  const p=canvasPoint(event);if(draft){if(tool==='wall'&&pointDistance(p,draft.start)>.4){const wall={id:'w'+Date.now(),x1:draft.start.x,y1:draft.start.y,x2:p.x,y2:p.y};layout.walls.push(wall);selected={type:'wall',id:wall.id}}if(tool==='shelf'){const x=Math.min(p.x,draft.start.x),y=Math.min(p.y,draft.start.y),w=Math.abs(p.x-draft.start.x),h=Math.abs(p.y-draft.start.y);if(w>=.5&&h>=.4){const category='beverage';const preset=SHELF_PRESETS.standard;const shelf={id:'s'+Date.now(),label:CATEGORY_NAMES[category]||'Đồ uống',presetId:'standard',category,x,y,w:preset.w,h:preset.h,valence:.2};layout.shelves.push(shelf);selected={type:'shelf',id:shelf.id}}}draft=null;renderObjects();renderInspector();markDirty()}
  drag=null;event.currentTarget.releasePointerCapture?.(event.pointerId);saveProject();draw();
}

function draw(){if(!layout)return;const canvas=$('#scene'),ctx=canvas.getContext('2d'),{sx,sy,ox,oy,W,H}=getCanvasTransform();ctx.clearRect(0,0,W,H);ctx.fillStyle='#120a04';ctx.fillRect(0,0,W,H);ctx.save();ctx.translate(ox,oy);ctx.fillStyle='#1c1007';ctx.fillRect(0,0,layout.width*sx,layout.height*sy);for(let x=0;x<=layout.width*2;x++){ctx.strokeStyle=x%4===0?'#3a1c0d':'#241008';ctx.beginPath();ctx.moveTo(x/2*sx,0);ctx.lineTo(x/2*sx,layout.height*sy);ctx.stroke()}for(let y=0;y<=layout.height*2;y++){ctx.strokeStyle=y%4===0?'#3a1c0d':'#241008';ctx.beginPath();ctx.moveTo(0,y/2*sy);ctx.lineTo(layout.width*sx,y/2*sy);ctx.stroke()}ctx.strokeStyle='#5a301a';ctx.lineWidth=2;ctx.strokeRect(0,0,layout.width*sx,layout.height*sy);for(const wall of layout.walls){const isSelected=selected?.type==='wall'&&selected.id===wall.id;ctx.strokeStyle=isSelected?'#ffca58':'#c8844a';ctx.lineWidth=isSelected?10:8;ctx.lineCap='round';ctx.beginPath();ctx.moveTo(wall.x1*sx,wall.y1*sy);ctx.lineTo(wall.x2*sx,wall.y2*sy);ctx.stroke();ctx.strokeStyle=isSelected?'#fff3d6':'#dab078';ctx.lineWidth=2;ctx.stroke();if(isSelected){for(const p of[{x:wall.x1,y:wall.y1},{x:wall.x2,y:wall.y2}]){ctx.fillStyle='#120a04';ctx.strokeStyle='#ffca58';ctx.lineWidth=2;ctx.beginPath();ctx.arc(p.x*sx,p.y*sy,7,0,Math.PI*2);ctx.fill();ctx.stroke()}}}for(const s of layout.shelves){ctx.fillStyle='#2e1509';ctx.strokeStyle=selected?.type==='shelf'&&selected.id===s.id?'#ffca58':'#6b3519';ctx.lineWidth=selected?.id===s.id?3:2;ctx.fillRect(s.x*sx,s.y*sy,s.w*sx,s.h*sy);ctx.strokeRect(s.x*sx,s.y*sy,s.w*sx,s.h*sy);ctx.fillStyle='#f5e6c8';ctx.font='700 11px "Nunito Sans", sans-serif';ctx.textAlign='center';ctx.fillText(s.label,(s.x+s.w/2)*sx,(s.y+s.h/2)*sy+4)}marker(ctx,layout.entrance,'🚪','LỐI VÀO','#5dba4f',sx,sy);marker(ctx,layout.checkout,'🛒','THU NGÂN','#e05252',sx,sy);
  if(draft){ctx.strokeStyle='#ffca58';ctx.setLineDash([6,4]);ctx.lineWidth=2;if(tool==='wall'){ctx.beginPath();ctx.moveTo(draft.start.x*sx,draft.start.y*sy);ctx.lineTo(draft.end.x*sx,draft.end.y*sy);ctx.stroke()}else ctx.strokeRect(draft.start.x*sx,draft.start.y*sy,(draft.end.x-draft.start.x)*sx,(draft.end.y-draft.start.y)*sy);ctx.setLineDash([])}
  if(simulation&&!dirty){const visibleAgents=[];for(const agent of simulation.agents){if(agent.status==='WAITING'||agent.finished)continue;const shelfId=agent.currentShelf||agent.targetId,shelf=agent.status==='DWELL'&&shelfId?layout.shelves.find(item=>item.id===shelfId):null;visibleAgents.push(shelf?{...agent,facingDx:(shelf.x+shelf.w/2)-agent.x,facingDy:(shelf.y+shelf.h/2)-agent.y}:agent);if(agent.trail.length>1){ctx.strokeStyle=colors[agent.status]+'35';ctx.lineWidth=1;ctx.beginPath();agent.trail.forEach((p,i)=>i?ctx.lineTo(p.x*sx,p.y*sy):ctx.moveTo(p.x*sx,p.y*sy));ctx.stroke()}if(selected?.type==='npc'&&selected.id===agent.id&&agent.path.length){ctx.strokeStyle='#ffffff90';ctx.setLineDash([5,4]);ctx.beginPath();ctx.moveTo(agent.x*sx,agent.y*sy);for(let i=agent.pathIndex;i<agent.path.length;i++)ctx.lineTo(agent.path[i].x*sx,agent.path[i].y*sy);ctx.stroke();ctx.setLineDash([])}}npcRenderer.draw(ctx,visibleAgents,{runSeed:simulation.seed,animationTimeMs:performance.now(),running:playing,scaleX:sx,scaleY:sy,selectedId:selected?.type==='npc'?selected.id:null,fallbackColors:colors})}else npcRenderer.draw(ctx,[],{runSeed:lastRunSeed,animationTimeMs:performance.now(),running:false,scaleX:sx,scaleY:sy});
  ctx.restore();
  $('#clock').textContent=formatTime(simulation?.time||0);$('#timeline').value=simulation?simulation.time/durationSeconds()*1000:0;$('#active-count').textContent=`${simulation?.snapshot().active||0} khách đang trong cửa hàng`;ctx.textAlign='left'}
function marker(ctx,p,icon,label,color,sx,sy){if(!p)return;ctx.fillStyle='#120a04';ctx.strokeStyle=color;ctx.lineWidth=2;ctx.beginPath();ctx.arc(p.x*sx,p.y*sy,14,0,Math.PI*2);ctx.fill();ctx.stroke();ctx.fillStyle=color;ctx.font='bold 14px "Nunito Sans", sans-serif';ctx.textAlign='center';ctx.fillText(icon,p.x*sx,p.y*sy+5);ctx.font='bold 10px "Nunito Sans", sans-serif';ctx.fillText(label,p.x*sx,p.y*sy-20)}

function openManual(){const columns='npc_id,target_category,need_product,need_growth,need_explore,explore_growth,attractor,stability,dispersion,recovery,speed,dwell,steadiness',sample='test_001,beverage,0.8,0.02,0.25,0.01,0.3,0.65,0.4,0.15,1.3,9,0.75';$('#manual-editor').value=columns+'\n'+(manualRows.length?manualRows.map(row=>columns.split(',').map(k=>row[k]??'').join(',')).join('\n'):sample);$('#manual-error').textContent='Values are clamped to safe ranges.';$('#manual-dialog').showModal()}
function applyManual(){try{const lines=$('#manual-editor').value.trim().split(/\r?\n/),headers=lines.shift().split(',').map(x=>x.trim());manualRows=lines.filter(Boolean).map(line=>Object.fromEntries(headers.map((h,i)=>[h,line.split(',')[i]?.trim()??''])));manualPopulation(manualRows);if(!manualRows.length)throw new Error('Enter at least one NPC row.');$('#manual-count').textContent=manualRows.length;$('#population-mode').value='manual';$('#npc-count').disabled=true;$('#manual-dialog').close();markDirty(`${manualRows.length} manual NPC inputs applied.`)}catch(error){$('#manual-error').textContent=error.message;$('#manual-error').style.color='#e05252'}}
async function saveProject(){try{const result=await api('/api/project',{method:'POST',body:JSON.stringify({layout,catalog})});$('#save-state').textContent='● Saved';if(result.warnings?.length){const message=`Saved with warning: ${result.warnings.join(' ')}`;showSystemEvent(message);toast(`${result.warnings.length} layout warning${result.warnings.length>1?'s':''}`)}}catch(error){$('#save-state').textContent='● Unsaved';showSystemEvent(error.message);toast(error.message)}}
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
