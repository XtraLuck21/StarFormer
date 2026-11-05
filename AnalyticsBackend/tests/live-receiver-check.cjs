// Explicit opt-in live integration check. Uploads ONLY the offline Unity
// verification events to RawEvents_Test under a clearly separate build label.
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto');
if(!process.argv.includes('--send-test-events'))throw new Error('Pass --send-test-events to write verification data to RawEvents_Test.');
const root=path.resolve(__dirname,'../..');
const endpoint=fs.readFileSync(path.join(root,'Assets/Resources/AnalyticsSettings.asset'),'utf8').match(/endpointUrl: (.+)/)[1].trim();
const log=fs.readFileSync(process.argv.find(arg=>arg.startsWith('--log='))?.slice(6)||'/tmp/starformer-analytics-verification.log','utf8');
const events=Array.from(log.matchAll(/^\[Analytics\] (\{.*\})$/gm),match=>JSON.parse(match[1]));
assert.ok(events.length>0,'Run AnalyticsVerification.RunAllBatch first.');
const expectedScenes=JSON.parse(fs.readFileSync(path.join(root,'Assets/Resources/AnalyticsMapCatalog.json'),'utf8')).maps.map(map=>map.level_name);
assert.deepEqual(Array.from(new Set(events.map(event=>event.level_name).filter(Boolean))).sort(),expectedScenes.slice().sort());
const player='verification-'+crypto.randomUUID(),session=crypto.randomUUID();
const build=process.argv.find(arg=>arg.startsWith('--build='))?.slice(8)||'integration-verification-'+new Date().toISOString().slice(0,10).replaceAll('-','');
async function send(event){
  const response=await fetch(endpoint,{method:'POST',headers:{'Content-Type':'text/plain;charset=utf-8'},body:JSON.stringify(event),signal:AbortSignal.timeout(45000)});
  const body=await response.json();assert.equal(body.ok,true,JSON.stringify(body));return body;
}
(async()=>{
  for(let index=0;index<events.length;index++){
    const event=events[index];event.environment='test';event.build_version=build;event.player_id=player;event.session_id=session;
    event.event_id=crypto.randomUUID();
    const result=await send(event);assert.equal(result.duplicate,false);
    console.log((index+1)+'/'+events.length+' accepted: '+event.level_name+' '+event.event_type);
  }
  const duplicate=await send(events[0]);assert.equal(duplicate.duplicate,true);
  console.log('PASS: '+events.length+' Unity-generated verification events accepted across '+expectedScenes.length+' scenes; duplicate rejected. Build: '+build+'. Test sheet only.');
})().catch(error=>{console.error(error);process.exitCode=1;});
