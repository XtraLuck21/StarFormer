// Aggregate-only dashboard. Identity values and individual run records stay on the server.
function readDashboardRows_(environment) {
  const values = getAnalyticsSheet_(environment || 'production').getDataRange().getValues();
  if (values.length < 2) return [];
  const header = values[0].map(String);
  ANALYTICS_COLUMNS.slice(0,-2).forEach(column => {
    if (header.indexOf(column) === -1) throw new Error('Missing source column: ' + column);
  });
  const seen = new Set(), rows = [];
  values.slice(1).forEach(valuesRow => {
    const row = {};
    header.forEach((column,index) => { row[column] = valuesRow[index]; });
    if (!row.event_id || seen.has(String(row.event_id))) return;
    seen.add(String(row.event_id));
    row.map_version = row.map_version || 'legacy';
    row.entry_reason = row.entry_reason || 'normal';
    rows.push(row);
  });
  // Some pre-Beta tests reused an Alpha build label with the rebuilt level.
  // The released Alpha maps have no ADV checkpoints: isolate provably conflicting
  // runs in memory instead of plotting their deaths on an incorrect background.
  const conflicts=new Set(rows.filter(row=>row.map_version==='legacy'&&/^alpha-0\.1\.[012]$/.test(row.build_version)&&
    row.level_name==='Level01'&&/^ADV_CP\d+$/.test(row.checkpoint_id||'')).map(dashboardRunKey_));
  rows.forEach(row=>{if(row.map_version==='legacy'&&conflicts.has(dashboardRunKey_(row)))row.map_version='legacy-layout-conflict';});
  return rows;
}
function dashboardRunKey_(row) {return JSON.stringify([row.player_id||'',row.session_id||'',row.level_run_id||'',row.build_version||'',row.level_name||'']);}

function getDashboardData(request) {
  request = request && typeof request === 'object' ? request : {};
  const environment = request.environment === 'test' ? 'test' : 'production';
  const metadataKey = environment + '-metadata';
  const cachedMetadata = dashboardCacheGet_(metadataKey);
  let rows;
  const metadata = cachedMetadata || buildDashboardMetadata_(rows = readDashboardRows_(environment));
  if (!cachedMetadata) dashboardCachePut_(metadataKey,metadata);
  // Map availability is independent of events: a zero-event tutorial still has a layout.
  const releaseBuilds=dashboardReleaseBuilds_();
  const builds=metadata.builds.concat(Object.keys(releaseBuilds).filter(build=>metadata.builds.indexOf(build)===-1));
  const build=builds.indexOf(request.build)!==-1?request.build:builds[0]||'';
  const release=releaseBuilds[build];
  const levels=release?ANALYTICS_LEVELS.filter(level=>release.included_scenes.indexOf(level)!==-1):ANALYTICS_LEVELS;
  const level=levels.indexOf(request.level)!==-1?request.level:levels.indexOf('Level01')!==-1?'Level01':levels[0]||'';
  const versions=((metadata.versions[build]||{})[level]||[]).slice();
  if(!versions.length&&dashboardLegacyMapBinding_(build,level))versions.push('legacy');
  const mapVersion=versions.indexOf(request.map_version)!==-1?request.map_version:versions.find(version=>version!=='legacy-layout-conflict')||versions[0]||'';
  // Always all players, normal entries. Old/bookmarked player or visits parameters are ignored.
  const key = JSON.stringify({environment:environment,build:build,level:level,map_version:mapVersion});
  let result = dashboardCacheGet_(key);
  if (!result) {
    if (!rows) rows = readDashboardRows_(environment);
    const selected = rows.filter(row => row.build_version === build && row.level_name === level &&
      row.map_version === mapVersion && row.entry_reason === 'normal');
    result = {generated_at:new Date().toISOString(),view:buildDashboardView_(selected)};
    dashboardCachePut_(key,result);
  }
  return {
    ok:true,environment:environment,generated_at:result.generated_at,total_rows:metadata.total_rows,
    builds:builds,levels:levels,map_versions:versions,
    release_collects_level:release?release.tracked_scenes.indexOf(level)!==-1:null,
    selected_build:build,selected_level:level,selected_map_version:mapVersion,view:result.view
  };
}

function buildDashboardMetadata_(rows) {
  const latest = Object.create(null), versions = Object.create(null);
  rows.forEach(row => {
    if (!row.build_version || ANALYTICS_LEVELS.indexOf(row.level_name) === -1) return;
    const build = String(row.build_version), stamp = dashboardTimestamp_(row.server_timestamp || row.client_timestamp);
    latest[build] = !latest[build] || stamp > latest[build] ? stamp : latest[build];
    if (!versions[build]) versions[build] = Object.create(null);
    if (!versions[build][row.level_name]) versions[build][row.level_name] = [];
    const maps = versions[build][row.level_name], existing = maps.indexOf(row.map_version);
    if (existing !== -1) maps.splice(existing,1);
    maps.unshift(row.map_version);
  });
  return {total_rows:rows.length,versions:versions,
    builds:Object.keys(latest).sort((a,b) => latest[b].localeCompare(latest[a]))};
}

function dashboardDistribution_(values,limits,labels) {
  const counts = limits.map(() => 0);
  values.forEach(value => {const index=limits.findIndex(limit=>value<limit);if(index!==-1)counts[index]++;});
  return labels.map((label,index)=>({label:label,count:counts[index]}));
}
function dashboardMean_(values) {return values.length?values.reduce((sum,n)=>sum+n,0)/values.length:null;}
function dashboardDuration_(value) {
  if(value === '' || value === null || value === undefined)return null;
  const n=Number(value);return Number.isFinite(n)&&n>=0?n:null;
}
function buildDashboardView_(inputRows) {
  const seen=new Set(), rows=inputRows.filter(row=>{
    if(row.event_id&&seen.has(row.event_id))return false;
    if(row.event_id)seen.add(row.event_id);return true;
  });
  const runs=new Map(),formDurations=Object.create(null),deaths=[];
  let invalidDeaths=0;
  rows.forEach((row,index)=>{
    if(row.event_type==='form_duration') {
      const seconds=dashboardDuration_(row.duration_seconds),form=String(row.controlled_form||'Unknown');
      if(seconds!==null)formDurations[form]=(formDurations[form]||0)+seconds;
    }
    if(row.event_type==='death') {
      if(dashboardHasPosition_(row))deaths.push({x:Number(row.position_x),y:Number(row.position_y)});
      else invalidDeaths++;
    }
    if(!row.level_run_id)return;
    // Run UUIDs normally suffice; include identity/session defensively without exposing either.
    const key=dashboardRunKey_(row);
    if(!runs.has(key))runs.set(key,[]);
    runs.get(key).push({row:row,index:index});
  });
  const intervals=new Map(),unpassed=new Map(),completionTimes=[];
  let observedStarts=0,completes=0,orphanCompletions=0,invalidCompletionTimes=0,unverifiedIntervals=0;
  function interval(from,to) {
    const key=JSON.stringify([from,to]);
    if(!intervals.has(key))intervals.set(key,{from:from,to:to,durations:[],unverified:0});
    return intervals.get(key);
  }
  runs.forEach(events=>{
    // Use the monotonic level clock when complete. If an endpoint clock is
    // missing, keep that boundary in timestamp order instead of treating it as 0.
    const completeClock=events.every(e=>dashboardDuration_(e.row.level_active_seconds)!==null);
    events.sort((a,b)=>{
      const stamp=dashboardTimestamp_(a.row.client_timestamp).localeCompare(dashboardTimestamp_(b.row.client_timestamp));
      return (completeClock?Number(a.row.level_active_seconds)-Number(b.row.level_active_seconds):0)||stamp||a.index-b.index;
    });
    const start=events.find(e=>e.row.event_type==='level_start') ||
      events.find(e=>e.row.event_type==='attempt_start'&&e.row.start_reason==='level_start');
    if(start)observedStarts++;
    let segment=null,finished=false,recordedComplete=false;
    function pass(to,row) {
      if(!segment)return;
      const item=interval(segment.from,to),end=dashboardDuration_(row.level_active_seconds);
      // Keep the original checkpoint-arrival clock through every death/respawn.
      // Attempt time resets on respawn and would only measure the final attempt.
      if(segment.start_active!==null&&end!==null&&end>=segment.start_active)item.durations.push(end-segment.start_active);
      else {item.unverified++;unverifiedIntervals++;}
    }
    events.forEach(({row})=>{
      const type=row.event_type,cp=String(row.checkpoint_id||'START');
      if(finished)return;
      if(type==='level_start'||(type==='attempt_start'&&row.start_reason==='level_start')) {
        if(!segment)segment={from:cp,start_active:dashboardDuration_(row.level_active_seconds)};
      }
      if(type==='checkpoint_reached'||(type==='attempt_start'&&row.start_reason==='checkpoint_reached')) {
        // checkpoint_reached and its following attempt_start describe the same arrival.
        if(!segment||segment.from!==cp) {
          pass(cp,row);segment={from:cp,start_active:dashboardDuration_(row.level_active_seconds)};
        }
      }
      if(type==='attempt_start'&&row.start_reason==='death_respawn'&&(!segment||segment.from!==cp)) {
        // A respawn is not an observed arrival at the source checkpoint.
        if(segment)unpassed.set(segment.from,(unpassed.get(segment.from)||0)+1);
        segment={from:cp,start_active:null};
      }
      if(type==='level_complete'&&!recordedComplete) {
        recordedComplete=true;
        if(start) {
          completes++;const seconds=dashboardDuration_(row.level_active_seconds);
          if(seconds!==null)completionTimes.push(seconds);else invalidCompletionTimes++;
        } else orphanCompletions++;
        pass('FINISH',row);segment=null;finished=true;
      }
      if(type==='level_end')finished=true;
    });
    if(segment) {
      const count=unpassed.get(segment.from)||0;unpassed.set(segment.from,count+1);
    }
  });
  return {
    event_count:rows.length,runs:observedStarts,completes:completes,
    orphan_completions:orphanCompletions,invalid_completion_times:invalidCompletionTimes,
    deaths:deaths.length+invalidDeaths,invalid_death_positions:invalidDeaths,
    death_points:deaths,form_durations:formDurations,
    completion:{count:completionTimes.length,mean_seconds:dashboardMean_(completionTimes),median_seconds:dashboardMedian_(completionTimes),
      distribution:dashboardDistribution_(completionTimes,[120,240,360,600,900,Infinity],['0–2m','2–4m','4–6m','6–10m','10–15m','15m+'])},
    checkpoint_intervals:Array.from(intervals.values()).map(item=>({from:item.from,to:item.to,
      passed:item.durations.length+item.unverified,sample_count:item.durations.length,unverified:item.unverified,
      mean_seconds:dashboardMean_(item.durations)})),
    unpassed_intervals:Array.from(unpassed,([from,count])=>({from:from,count:count})),unverified_intervals:unverifiedIntervals
  };
}

// Only explicit, source-verified release bindings may provide backgrounds for v1 data.
// Filled by tests/import-release-maps.py; never use the current map as a fallback.
function dashboardReleaseBuilds_() {
  const releases = {"beta-0.1.0":{"included_scenes":["IntroBox","IntroBubble","IntroBouncyBall","Level01","IntroSpider","FanTutorial","Level02"],"tracked_scenes":["Level01","Level02"]},"alpha-0.1.2":{"included_scenes":["IntroBox","IntroBubble","Level01","Level02"],"tracked_scenes":["Level01","Level02"]},"alpha-0.1.1":{"included_scenes":["IntroBox","IntroBubble","Level01","Level02"],"tracked_scenes":["Level01","Level02"]},"alpha-0.1.0":{"included_scenes":["IntroBox","IntroBubble","Level01","Level02"],"tracked_scenes":["Level01","Level02"]}}; // RELEASE_BUILD_METADATA
  return releases;
}
function dashboardLegacyMapBinding_(build,level) {
  const bindings = {"beta-0.1.0":{"IntroBox":{"map_version":"map-33c2d4635eeb-d519bf3343b6","source_commit":"3a693ca067a4898c97935262ee731fe1ecf69d33"},"IntroBubble":{"map_version":"map-33d3f8f7fde4-13d9a8802c64","source_commit":"3a693ca067a4898c97935262ee731fe1ecf69d33"},"IntroBouncyBall":{"map_version":"map-1588010abde6-da8fbd61d819","source_commit":"3a693ca067a4898c97935262ee731fe1ecf69d33"},"Level01":{"map_version":"map-990d365e2ac4-86300a7d6f63","source_commit":"3a693ca067a4898c97935262ee731fe1ecf69d33"},"IntroSpider":{"map_version":"map-5624518b9147-7bf73f4ea763","source_commit":"3a693ca067a4898c97935262ee731fe1ecf69d33"},"FanTutorial":{"map_version":"map-6d73edb4b525-50d97a7e8708","source_commit":"3a693ca067a4898c97935262ee731fe1ecf69d33"},"Level02":{"map_version":"map-016d66aa0cbc-63f786c65786","source_commit":"3a693ca067a4898c97935262ee731fe1ecf69d33"}},"alpha-0.1.2":{"IntroBox":{"map_version":"map-2570abb231d0-d519bf3343b6","source_commit":"565025022cef23045ba512bd198603fdc5a573d6"},"IntroBubble":{"map_version":"map-6d3f9748cb44-4a7f9adee971","source_commit":"565025022cef23045ba512bd198603fdc5a573d6"},"Level01":{"map_version":"map-9302e1e3b52c-8f7632e0a1c6","source_commit":"565025022cef23045ba512bd198603fdc5a573d6"},"Level02":{"map_version":"map-263b146d82dc-63f786c65786","source_commit":"565025022cef23045ba512bd198603fdc5a573d6"}},"alpha-0.1.1":{"IntroBox":{"map_version":"map-04e326683230-d519bf3343b6","source_commit":"765a1ea67481a0800f7ce6e9a9b15fc78d3f686c"},"IntroBubble":{"map_version":"map-8508f5ac4b92-4a7f9adee971","source_commit":"765a1ea67481a0800f7ce6e9a9b15fc78d3f686c"},"Level01":{"map_version":"map-0e1a70c0a3a3-8f7632e0a1c6","source_commit":"765a1ea67481a0800f7ce6e9a9b15fc78d3f686c"},"Level02":{"map_version":"map-263b146d82dc-63f786c65786","source_commit":"765a1ea67481a0800f7ce6e9a9b15fc78d3f686c"}},"alpha-0.1.0":{"IntroBox":{"map_version":"map-04e326683230-d519bf3343b6","source_commit":"994a0146c0ee8979838d45e5f05bc6d9f8d8fa6f"},"IntroBubble":{"map_version":"map-8508f5ac4b92-4a7f9adee971","source_commit":"994a0146c0ee8979838d45e5f05bc6d9f8d8fa6f"},"Level01":{"map_version":"map-0e1a70c0a3a3-8f7632e0a1c6","source_commit":"994a0146c0ee8979838d45e5f05bc6d9f8d8fa6f"},"Level02":{"map_version":"map-0f40718dfbdf-63f786c65786","source_commit":"994a0146c0ee8979838d45e5f05bc6d9f8d8fa6f"}}}; // RELEASE_MAP_BINDINGS
  return (bindings[build]||{})[level]||null;
}
function getDashboardMap(level,mapVersion,build) {
  if (ANALYTICS_LEVELS.indexOf(level)===-1||typeof mapVersion!=='string'||mapVersion.length>128)return null;
  let binding=null;
  if(mapVersion==='legacy') {
    binding=dashboardLegacyMapBinding_(String(build||''),level);
    if(!binding)return null;
    mapVersion=binding.map_version;
  }
  if(!mapVersion||mapVersion==='unmapped')return null;
  try {
    const catalog=JSON.parse(HtmlService.createHtmlOutputFromFile('MapCatalog').getContent());
    const map=(catalog.maps||[]).find(map=>map.level_name===level&&map.map_version===mapVersion);
    return map?Object.assign({},map,binding?{historical_build:build,source_commit:binding.source_commit}:{}):null;
  } catch(error) {console.warn('Map catalog unavailable: '+error.message);return null;}
}
function dashboardHasPosition_(row) {
  return (row.has_position===true||row.has_position==='TRUE')&&row.position_x!==''&&row.position_y!==''&&
    Number.isFinite(Number(row.position_x))&&Number.isFinite(Number(row.position_y));
}
function dashboardNumber_(value) {const number=Number(value);return Number.isFinite(number)?number:0;}
function dashboardTimestamp_(value) {return value instanceof Date?value.toISOString():String(value||'');}
function dashboardMedian_(values) {
  if(!values.length)return null;
  const sorted=values.slice().sort((a,b)=>a-b),middle=Math.floor(sorted.length/2);
  return sorted.length%2?sorted[middle]:(sorted[middle-1]+sorted[middle])/2;
}

// Read-only cloud smoke check; logs summaries, never rows or identities.
function verifyAggregateDashboard() {
  const result={dashboard_schema:4,sources:{},historical_maps:[]};
  ['production','test'].forEach(environment=>{
    const data=getDashboardData({environment:environment});
    if(data.players!==undefined||data.view.completion_runs!==undefined)throw new Error('Individual analysis is still exposed');
    if(!data.view.completion||!data.view.checkpoint_intervals)throw new Error('Aggregate metrics missing');
    const map=getDashboardMap(data.selected_level,data.selected_map_version,data.selected_build);
    result.sources[environment]={build:data.selected_build,level:data.selected_level,events:data.view.event_count,runs:data.view.runs,
      completions:data.view.completes,deaths:data.view.deaths,intervals:data.view.checkpoint_intervals.length,checkpoint_times:data.view.checkpoint_intervals.map(item=>({from:item.from,to:item.to,mean_seconds:item.mean_seconds,samples:item.sample_count})),map_matched:!!map};
  });
  const releases=dashboardReleaseBuilds_();
  Object.keys(releases).forEach(build=>{
    releases[build].included_scenes.forEach(level=>{
      const data=getDashboardData({environment:'production',build:build,level:level});
      const map=getDashboardMap(level,'legacy',build);
      if(!map)throw new Error('Missing release map: '+build+' '+level);
      if(data.view.event_count===0&&data.selected_map_version!=='legacy')throw new Error('Zero-event map unavailable');
      result.historical_maps.push({build:build,level:level,map_version:map.map_version,events:data.view.event_count});
    });
  });
  console.log(JSON.stringify(result));return result;
}

// Base64 shards respect the byte limit even with non-ASCII labels. Evicted
// shards are a cache miss, never a partially returned dashboard.
function dashboardCacheKey_(key) {
  return 'dashboard-v4-durations-' + Utilities.base64EncodeWebSafe(Utilities.computeDigest(Utilities.DigestAlgorithm.SHA_256,key)).slice(0,40);
}
function dashboardCacheGet_(key) {
  const cache=CacheService.getScriptCache(), prefix=dashboardCacheKey_(key), index=cache.get(prefix);
  if (!index) return null;
  try {
    const info=JSON.parse(index), names=[];
    for(let i=0;i<info.count;i++) names.push(prefix+'-'+info.generation+'-'+i);
    const parts=cache.getAll(names);
    if (names.some(name=>!parts[name])) return null;
    const json=Utilities.newBlob(Utilities.base64DecodeWebSafe(names.map(name=>parts[name]).join(''))).getDataAsString('UTF-8');
    return JSON.parse(json);
  } catch(error) {return null;}
}
function dashboardCachePut_(key,value) {
  try {
    const cache=CacheService.getScriptCache(), prefix=dashboardCacheKey_(key);
    const encoded=Utilities.base64EncodeWebSafe(JSON.stringify(value),Utilities.Charset.UTF_8);
    const count=Math.ceil(encoded.length/60000), generation=Utilities.getUuid(), parts={};
    if(count>20) return;
    for(let i=0;i<count;i++) parts[prefix+'-'+generation+'-'+i]=encoded.slice(i*60000,(i+1)*60000);
    cache.putAll(parts,DASHBOARD_CACHE_SECONDS);
    cache.put(prefix,JSON.stringify({count:count,generation:generation}),DASHBOARD_CACHE_SECONDS);
  } catch(error) {console.warn('Dashboard cache skipped: '+error.message);}
}
