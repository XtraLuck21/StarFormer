// Run: node AnalyticsBackend/tests/integration-scope.test.cjs <pre-integration-main-sha>
// Verifies the integration boundary, not gameplay behavior. No network or Sheet writes.
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto'),assert=require('node:assert/strict');
const {execFileSync}=require('node:child_process');
const root=path.resolve(__dirname,'../..'),base=process.argv[2];
if(!base)throw new Error('Provide the fetched main revision used as the integration baseline.');
const git=args=>execFileSync('git',args,{cwd:root});
const before=file=>git(['show',base+':'+file]);
const after=file=>fs.readFileSync(path.join(root,file));
function replaceOnce(bytes,oldText,newText){
 const old=Buffer.from(oldText),index=bytes.indexOf(old);assert.ok(index>=0,'Expected analytics insertion anchor missing');
 assert.equal(bytes.indexOf(old,index+old.length),-1,'Ambiguous insertion anchor');
 return Buffer.concat([bytes.subarray(0,index),Buffer.from(newText),bytes.subarray(index+old.length)]);
}
const hooks={
 'Assets/Scripts/IntroSceneExit.cs':[
  '        isLoading = true;\n        pendingReturnScene',
  '        isLoading = true;\n        if (returnToPreviousExit)\n            AnalyticsManager.Instance?.TrackSceneReturn(nextSceneName);\n        else\n            AnalyticsManager.Instance?.TrackLevelComplete(controlledBody);\n        pendingReturnScene'],
 'Assets/Scripts/IntroStarGoal.cs':['        isLoading = true;\n','        isLoading = true;\n        AnalyticsManager.Instance?.TrackLevelComplete(playerBody);\n'],
 'Assets/Scripts/IntroBouncyBallGoal.cs':['        isLoading = true;','        isLoading = true;\n        AnalyticsManager.Instance?.TrackLevelComplete(body);'],
 'Assets/Scripts/LevelNavigationUI.cs':['AnalyticsManager.Instance?.TrackManualRestart();','AnalyticsManager.Instance?.TrackManualRestart(clearCheckpoint ? "restart_level" : "retry_checkpoint");'],
 'Assets/Scripts/PlayerPossessionController.cs':['AnalyticsManager.Instance?.TrackManualRestart();','AnalyticsManager.Instance?.TrackManualRestart(clearCheckpoint ? "restart_level" : "retry_checkpoint");']
};
for(const [file,[oldText,newText]] of Object.entries(hooks))assert.deepEqual(after(file),replaceOnce(before(file),oldText,newText),file+' has a non-analytics change');
const sceneNames=['IntroBox','IntroBubble','IntroBouncyBall','Level01','IntroSpider','FanTutorial','Level02'];
const settingsFile='Assets/Resources/AnalyticsSettings.asset';
assert.deepEqual(after(settingsFile),replaceOnce(before(settingsFile),'  trackedScenes:\n  - Level01\n  - Level02','  trackedScenes:\n'+sceneNames.map(name=>'  - '+name).join('\n')),'Only the tracked-scene list may change in the production settings');
const links='- [Analytics dashboard](Docs/AnalyticsDashboard.md) — four aggregate metrics, verified real maps and Apps Script deployment.\n- [All-level analytics and real maps](Docs/AnalyticsAllLevels.md) — all seven gameplay scenes, schema v2, map exports and validation.\n';
let readme=replaceOnce(before('README.md'),'- [Publish to GitHub Pages](Docs/publish_instructions.md)',links+'- [Publish to GitHub Pages](Docs/publish_instructions.md)');
readme=replaceOnce(readme,'**Known analytics follow-up:** the dashboard does not yet match this build to the rebuilt Level01 map and displays deaths on a coordinate grid. Data arrival is confirmed; map rendering, Level02-specific data and full metric validation remain separate checks.','**Analytics source update:** the current source adds schema-v2 collection for all seven gameplay scenes, including the five tutorials. The live Apps Script dashboard is on Version 9 with release-matched base maps and the four aggregate metrics. The already-hosted Beta retains its embedded Level01/Level02-only collector until a new game build is published; Level02-specific raw-row and broader playtest checks remain separate acceptance work.');
assert.deepEqual(after('README.md'),readme,'README non-analytics content changed');
let build=after('Assets/Editor/WebGLBuild.cs').toString('utf8');
build=build.replace('using System.IO;\n','').replace('using System.Security.Cryptography;\n','').replace('        ValidateAnalyticsMaps(scenes);\n\n','');
const start=build.indexOf('    private static void ValidateAnalyticsMaps('),end=build.indexOf('    private static string GetArgument(',start);
assert.ok(start>=0&&end>start);build=build.slice(0,start)+build.slice(end);
assert.equal(build,before('Assets/Editor/WebGLBuild.cs').toString('utf8'),'Existing WebGL build options changed');
const changed=new Set([...git(['diff','--name-only',base,'--']).toString().trim().split('\n'),...git(['ls-files','--others','--exclude-standard']).toString().trim().split('\n')].filter(Boolean));
const extra=new Set([...Object.keys(hooks),settingsFile,'Assets/Editor/WebGLBuild.cs','Assets/Resources/AnalyticsMapCatalog.json','Assets/Resources/AnalyticsMapCatalog.json.meta','README.md']);
for(const file of changed)assert.ok(extra.has(file)||/^AnalyticsBackend\//.test(file)||/^Assets\/Scripts\/Analytics\//.test(file)||/^Assets\/Editor\/Analytics\w+\.cs(?:\.meta)?$/.test(file)||/^Docs\/Analytics[^/]*\.md$/.test(file),'Out-of-scope file changed: '+file);
const enabled=Array.from(after('ProjectSettings/EditorBuildSettings.asset').toString().matchAll(/- enabled: 1\s+path: Assets\/Scenes\/(\w+)\.unity/g),m=>m[1]).filter(name=>name!=='Mainmenu');
assert.deepEqual(enabled,sceneNames);
const manifest=JSON.parse(after('AnalyticsBackend/Maps/manifest.json')).maps,runtime=JSON.parse(after('Assets/Resources/AnalyticsMapCatalog.json')).maps;
assert.deepEqual(manifest.map(m=>m.level_name),sceneNames);assert.deepEqual(runtime,manifest);
for(const map of manifest){
 const hash=crypto.createHash('sha256').update(after('Assets/Scenes/'+map.level_name+'.unity')).digest('hex');
 assert.equal(map.source_scene_hash,hash,'Stale map: '+map.level_name);
 assert.ok(after('Assets/Editor/AnalyticsMapExporter.cs').includes(Buffer.from('"'+map.level_name+'"')));
 assert.ok(after('Assets/Scripts/Analytics/AnalyticsSettings.cs').includes(Buffer.from('"'+map.level_name+'"')));
}
assert.deepEqual(after('Assets/Scripts/FanSet/FanPossessionHint.cs'),before('Assets/Scripts/FanSet/FanPossessionHint.cs'));
console.log('PASS: analytics-only integration; exact hooks; production identity preserved; 7 enabled/tracked/exported scenes; gameplay, art, scenes and build options unchanged.');
