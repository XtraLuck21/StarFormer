/* Run: node AnalyticsBackend/tests/backend.test.cjs. No network or real Sheet writes. */
const fs=require('node:fs'),path=require('node:path'),vm=require('node:vm'),crypto=require('node:crypto'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'..');
function runtime(){
  const store=new Map(),sheets={};
  const spreadsheet={getSheetByName:name=>sheets[name],insertSheet:name=>(sheets[name]=sheet(name,[]))};
  function sheet(name,data){
    return {name,data,maxColumns:40,backups:[],getName(){return this.name;},setName(name){this.name=name;return this;},
      getParent:()=>spreadsheet,getLastRow(){return this.data.length;},getLastColumn(){return Math.max(0,...this.data.map(row=>row.length));},getMaxColumns(){return this.maxColumns;},
      insertColumnsAfter(index,count){this.maxColumns+=count;},setFrozenRows(){},appendRow(row){this.data.push(row);},
      copyTo(){const backup=sheet(this.name+'_copy',JSON.parse(JSON.stringify(this.data)));this.backups.push(backup);return backup;},
      getDataRange(){return {getValues:()=>this.data};},getRange(r,c,n,m){return {
        getValues:()=>Array.from({length:n},(_,i)=>Array.from({length:m},(_,j)=>this.data[r+i-1]?.[c+j-1]??'')),
        setValues:values=>values.forEach((row,i)=>row.forEach((value,j)=>{this.data[r+i-1]??=[];this.data[r+i-1][c+j-1]=value;})),
        createTextFinder:text=>({matchEntireCell:()=>({findNext:()=>this.data.slice(r-1,r-1+n).some(row=>row[c-1]===text)?{}:null})})
      };}};
  }
  const context=vm.createContext({console,Date,Map,Set,JSON,Number,String,Array,Object,Math,
    CacheService:{getScriptCache:()=>({get:key=>store.get(key)||null,put:(key,value)=>store.set(key,value),getAll:keys=>Object.fromEntries(keys.filter(k=>store.has(k)).map(k=>[k,store.get(k)])),putAll:items=>Object.entries(items).forEach(([k,v])=>store.set(k,v))})},
    Utilities:{DigestAlgorithm:{SHA_256:'sha256'},Charset:{UTF_8:'utf8'},computeDigest:(_,value)=>crypto.createHash('sha256').update(value).digest(),
      base64EncodeWebSafe:value=>Buffer.from(value).toString('base64url'),base64DecodeWebSafe:value=>Buffer.from(value,'base64url'),
      newBlob:value=>({getDataAsString:()=>Buffer.from(value).toString('utf8')}),getUuid:()=>crypto.randomUUID()},
    HtmlService:{createHtmlOutputFromFile:name=>({getContent:()=>fs.readFileSync(path.join(root,name+'.html'),'utf8'),setTitle(){return this;}})},
    ContentService:{MimeType:{JSON:'json'},createTextOutput:value=>({setMimeType:()=>({body:JSON.parse(value)})})},
    LockService:{getScriptLock:()=>({waitLock(){},releaseLock(){}})},PropertiesService:{getScriptProperties:()=>({getProperty:()=>null})},
    SpreadsheetApp:{getActiveSpreadsheet:()=>spreadsheet}
  });
  vm.runInContext(fs.readFileSync(path.join(root,'Code.gs'),'utf8')+'\n'+fs.readFileSync(path.join(root,'DashboardData.gs'),'utf8')+'\nthis.columns=ANALYTICS_COLUMNS;this.levels=ANALYTICS_LEVELS;',context);
  return {context,sheets,store,sheet};
}
function event(overrides={}){return {schema_version:2,environment:'test',event_id:crypto.randomUUID(),event_type:'attempt_start',
  player_id:'private-player-a',session_id:'session',level_run_id:'run-a',attempt_id:'attempt',client_timestamp:'2026-11-01T10:00:00Z',
  level_name:'Level01',map_version:'map-a',entry_reason:'normal',build_version:'test-build',start_reason:'level_start',checkpoint_id:'START',
  has_position:true,position_x:0,position_y:0,...overrides};}
const rt=runtime(),api=rt.context;
// v1 migration appends two columns, backs up first, and leaves data intact.
const legacyHeader=Array.from(api.columns).slice(0,-2),legacyRow=legacyHeader.map(key=>event({schema_version:1})[key]??'');
const old=rt.sheet('RawEvents_Test',[legacyHeader,legacyRow]);rt.sheets.RawEvents_Test=old;
api.ensureHeader_(old);assert.equal(old.backups.length,1);assert.deepEqual(old.data[1],legacyRow);
assert.equal(old.data[0].length,29);api.ensureHeader_(old);assert.equal(old.backups.length,1);
const invalid=rt.sheet('bad',[['different']]);assert.throws(()=>api.ensureHeader_(invalid));assert.equal(invalid.backups.length,0);
for(const level of api.levels){const payload=event({level_name:level});assert.equal(api.validatePayload_(payload),'');
  assert.equal(api.doPost({postData:{contents:JSON.stringify(payload)}}).body.ok,true);
  assert.equal(api.doPost({postData:{contents:JSON.stringify(payload)}}).body.duplicate,true);}
assert.equal(api.validatePayload_(event({schema_version:1,map_version:undefined,entry_reason:undefined})),'');
assert.equal(api.validatePayload_(event({level_name:'Mainmenu'})),'invalid_level_name');
assert.equal(api.validatePayload_(event({event_type:'death',position_x:NaN})),'invalid_death_position');
assert.equal(api.validatePayload_(event({event_type:'death',has_position:false})),'invalid_death_position');
assert.equal(api.validatePayload_(event({duration_seconds:-1})),'invalid_duration_seconds');
// Each attempt has a unique ID; a successful interval may span many death respawns.
let serial=0;
function runEvents(run,player,retries,complete=true) {
  let active=0;const rows=[],base={level_run_id:run,player_id:player};
  const add=(type,extra={})=>rows.push(event({...base,event_type:type,client_timestamp:new Date(Date.UTC(2026,10,1,10,0,++serial)).toISOString(),level_active_seconds:active,attempt_id:run+'-'+serial,...extra}));
  add('level_start');add('attempt_start');active=10;
  add('checkpoint_reached',{checkpoint_id:'A',attempt_active_seconds:10});
  add('attempt_start',{checkpoint_id:'A',start_reason:'checkpoint_reached'});
  for(let i=0;i<retries;i++) {
    active+=7;add('death',{checkpoint_id:'A',attempt_active_seconds:7,controlled_form:'Box',death_cause:'Spike',position_x:999});
    add('attempt_start',{checkpoint_id:'A',start_reason:'death_respawn'});
  }
  add('form_duration',{controlled_form:'Box',duration_seconds:20});
  add('analytics_snapshot',{controlled_form:'Box',duration_seconds:20});
  if(complete) {
    active+=12;add('checkpoint_reached',{checkpoint_id:'B',attempt_active_seconds:12});
    add('attempt_start',{checkpoint_id:'B',start_reason:'checkpoint_reached'});active+=9;
    add('level_complete',{level_active_seconds:active});
    add('level_complete',{level_active_seconds:active}); // repeated completion callback counts once
  }
  return rows;
}
const rows=runEvents('run-1','private-player-a',3).concat(runEvents('run-2','private-player-b',0),runEvents('unfinished','private-player-c',4,false));
const view=api.buildDashboardView_(rows),ab=view.checkpoint_intervals.find(i=>i.from==='A'&&i.to==='B');
assert.equal(view.runs,3);assert.equal(view.completes,2);assert.equal(view.form_durations.Box,60);assert.equal(view.deaths,7);
assert.equal(ab.passed,2);assert.equal(ab.mean_seconds,22.5);assert.equal(ab.sample_count,2);assert.equal(ab.mean_respawns,undefined);
assert.equal(view.checkpoint_intervals.find(i=>i.from==='START').mean_seconds,10);
assert.equal(view.checkpoint_intervals.find(i=>i.to==='FINISH').mean_seconds,9);
assert.equal(view.unpassed_intervals.find(i=>i.from==='A').count,1);
assert.equal(view.completion.mean_seconds,41.5);assert.equal(view.completion.distribution[0].count,2);
assert.equal(view.death_points[0].x,999);assert.equal(view.death_times,undefined);
assert.equal(Object.keys(view.death_points[0]).sort().join(','),'x,y');
assert.ok(!JSON.stringify(view).includes('private-player'));assert.equal(view.completion_runs,undefined);
// Reversed network delivery and repeated event / attempt IDs do not change counts.
assert.equal(api.buildDashboardView_(rows.slice().reverse()).checkpoint_intervals.find(i=>i.from==='A'&&i.to==='B').mean_seconds,22.5);
const respawn=rows.find(r=>r.start_reason==='death_respawn');
assert.equal(api.buildDashboardView_(rows.concat({...respawn,event_id:crypto.randomUUID()})).checkpoint_intervals.find(i=>i.from==='A'&&i.to==='B').mean_seconds,22.5);
assert.equal(api.buildDashboardView_(rows.concat(rows)).deaths,7);
const orphan=api.buildDashboardView_([event({event_type:'level_complete'})]);assert.equal(orphan.completes,0);assert.equal(orphan.orphan_completions,1);assert.equal(orphan.completion.count,0);
const partial=api.buildDashboardView_([event({start_reason:'death_respawn',checkpoint_id:'A'}),event({event_type:'level_complete'})]);
assert.equal(partial.checkpoint_intervals[0].mean_seconds,null);assert.equal(partial.unverified_intervals,1);
const missing=api.buildDashboardView_(runEvents('missing','p',1).filter(r=>r.start_reason!=='death_respawn'));
assert.equal(missing.checkpoint_intervals.find(i=>i.from==='A').mean_seconds,19); // level clock includes the missing respawn's time
const tutorial=api.buildDashboardView_([event({event_type:'level_start',level_active_seconds:0}),event({start_reason:'death_respawn',level_active_seconds:15}),event({event_type:'level_complete',level_active_seconds:60})]);
assert.equal(tutorial.checkpoint_intervals[0].from,'START');assert.equal(tutorial.checkpoint_intervals[0].to,'FINISH');assert.equal(tutorial.checkpoint_intervals[0].mean_seconds,60);
const missingClock=runEvents('missing-clock','p',0).map(r=>r.checkpoint_id==='B'?{...r,level_active_seconds:''}:r);
assert.equal(api.buildDashboardView_(missingClock).checkpoint_intervals.find(i=>i.from==='A').mean_seconds,null);
const invalidTimes=api.buildDashboardView_([event({event_type:'death',has_position:false,attempt_active_seconds:''})]);
assert.equal(invalidTimes.invalid_death_positions,1);assert.equal(invalidTimes.death_points.length,0);
// Separate levels, maps, builds and environments; always ALL players and normal visits.
const data=rows.concat(event({map_version:'map-b'}),event({entry_reason:'return',level_run_id:'return-run'}));
rt.sheets.RawEvents_Production=rt.sheet('RawEvents_Production',[Array.from(api.columns),...data.map(row=>Array.from(api.columns).map(c=>row[c]??''))]);
const normal=api.getDashboardData({build:'test-build',level:'Level01',map_version:'map-a'});
assert.equal(normal.view.runs,3);assert.equal(normal.levels.length,8);assert.equal(normal.view.event_count,rows.length);
const attemptedFilters=api.getDashboardData({level:'Level01',map_version:'map-a',entry:'return',player:'Player 01'});
assert.equal(attemptedFilters.view.runs,3);assert.equal(attemptedFilters.players,undefined);assert.equal(attemptedFilters.selected_player,undefined);
assert.equal(api.getDashboardData({level:'Level01',map_version:'map-b'}).view.deaths,0);
assert.ok(!JSON.stringify(normal).includes('private-player'));
assert.equal(api.getDashboardData({level:'FanTutorial'}).view.event_count,0);
assert.equal(api.getDashboardData({environment:'test',level:'IntroSpider',map_version:'map-a'}).view.runs,1);
// Legacy Alpha labels reused with ADV checkpoint geometry must not use the old map.
const conflictRt=runtime(),capi=conflictRt.context;
const conflictRows=[event({build_version:'alpha-0.1.2',level_run_id:'advanced',map_version:'',checkpoint_id:'ADV_CP1'}),
  event({build_version:'alpha-0.1.2',level_run_id:'advanced',map_version:'',event_type:'death'}),
  event({build_version:'alpha-0.1.2',level_run_id:'released-alpha',map_version:''})];
conflictRt.sheets.RawEvents_Production=conflictRt.sheet('RawEvents_Production',[Array.from(capi.columns),...conflictRows.map(row=>Array.from(capi.columns).map(c=>row[c]??''))]);
const releasedView=capi.getDashboardData({build:'alpha-0.1.2',level:'Level01'});
assert.equal(releasedView.selected_map_version,'legacy');assert.equal(releasedView.view.deaths,0);
const conflictingView=capi.getDashboardData({build:'alpha-0.1.2',level:'Level01',map_version:'legacy-layout-conflict'});
assert.equal(conflictingView.view.deaths,1);assert.equal(capi.getDashboardMap('Level01','legacy-layout-conflict','alpha-0.1.2'),null);
assert.equal(conflictRt.sheets.RawEvents_Production.data[1][Array.from(capi.columns).indexOf('map_version')],'');
// A known released tutorial must show its map even when its event count is zero.
for(const scene of ['IntroBox','IntroBubble','IntroBouncyBall','IntroSpider','FanTutorial']) {
 const data=api.getDashboardData({build:'beta-0.1.0',level:scene});
 assert.equal(data.selected_level,scene);assert.equal(data.selected_map_version,'legacy');assert.equal(data.view.event_count,0);
 assert.equal(data.view.deaths,0);assert.equal(data.view.completion.count,0);assert.equal(data.release_collects_level,false);
 assert.ok(api.getDashboardMap(scene,data.selected_map_version,data.selected_build)?.image_data_url);
 assert.equal(data.levels.length,7);
}
const alphaTutorial=api.getDashboardData({build:'alpha-0.1.2',level:'IntroBox'});
assert.equal(alphaTutorial.view.event_count,0);assert.equal(alphaTutorial.levels.length,4);
assert.equal(alphaTutorial.levels.includes('FanTutorial'),false);assert.equal(alphaTutorial.levels.includes('IntroBouncyBall'),false);
assert.equal(api.getDashboardData({build:'beta-0.1.0',level:'Level01'}).release_collects_level,true);
// Shard large Unicode results safely and reject partially evicted values.
const large={text:'星星'.repeat(60000)};api.dashboardCachePut_('large',large);assert.equal(api.dashboardCacheGet_('large').text,large.text);
const shard=Array.from(rt.store.keys()).find(key=>key.includes('-0')&&key.startsWith(api.dashboardCacheKey_('large')));rt.store.delete(shard);assert.equal(api.dashboardCacheGet_('large'),null);
// All map images have matching runtime metadata and exactly preserve aspect.
const maps=JSON.parse(fs.readFileSync(path.join(root,'Maps/manifest.json'),'utf8')).maps;
for(const map of maps){assert.ok(fs.existsSync(path.join(root,map.image_file)));assert.ok(map.source_scene_hash);
  assert.ok(Math.abs((map.max_x-map.min_x)/(map.max_y-map.min_y)-map.width/map.height)<0.00001);
  const live=api.getDashboardMap(map.level_name,map.map_version);assert.ok(live?.image_data_url.startsWith('data:image/png;base64,'));}
const releases=JSON.parse(fs.readFileSync(path.join(root,'Maps/releases.json'),'utf8')).releases;
for(const release of releases)for(const map of release.maps) {
  const resolved=api.getDashboardMap(map.level_name,'legacy',release.build);
  assert.equal(resolved.map_version,map.map_version);assert.equal(resolved.source_commit,release.source_commit);
  assert.equal(resolved.historical_build,release.build);assert.ok(fs.existsSync(path.join(root,map.image_file)));
}
assert.notEqual(api.getDashboardMap('Level01','legacy','beta-0.1.0').map_version,api.getDashboardMap('Level01','legacy','alpha-0.1.2').map_version);
assert.equal(api.getDashboardMap('Level01','legacy','unknown-build'),null);assert.equal(api.getDashboardMap('Level01','unknown-map'),null);
const html=fs.readFileSync(path.join(root,'Dashboard.html'),'utf8');new Function(html.slice(html.indexOf('<script>')+8,html.lastIndexOf('</script>')));
assert.ok(!html.includes('id="player"'));assert.ok(!html.includes('id="entry"'));assert.ok(!html.includes('form-filter'));assert.ok(!html.includes('death-checkpoint'));assert.ok(!html.includes('retry-interval'));assert.ok(html.includes('Time between checkpoints'));assert.equal((html.match(/<section aria-labelledby=/g)||[]).length,4);
console.log('PASS: ingestion/migration, aggregate-only privacy, checkpoint travel durations across retries, unfinished/missing-time intervals, completion distribution, cache, map matching and four-metric frontend.');
module.exports={runtime,event};

// IntroFan is the current name; old published events and maps still use FanTutorial.
assert.ok(api.levels.includes('IntroFan'));assert.ok(api.levels.includes('FanTutorial'));
assert.equal(api.validatePayload_(event({level_name:'IntroFan'})),'');
const renamedFan=JSON.parse(fs.readFileSync(path.join(root,'Maps/manifest.json'),'utf8')).maps.find(m=>m.level_name==='IntroFan');
assert.ok(renamedFan);
const currentFanMap=api.getDashboardMap('IntroFan',renamedFan.map_version);
const legacyFanMap=api.getDashboardMap('FanTutorial',renamedFan.map_version);
assert.equal(currentFanMap.level_name,'IntroFan');assert.equal(legacyFanMap.level_name,'FanTutorial');
assert.equal(currentFanMap.image_data_url,legacyFanMap.image_data_url);
assert.equal(api.getDashboardMap('IntroFan','missing-map-version'),null);
console.log('PASS: IntroFan accepted alongside FanTutorial; exact-version map compatibility preserved.');
