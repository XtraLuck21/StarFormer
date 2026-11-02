#!/usr/bin/env python3
# Imports images/metadata only. Never edits Sheet events or the runtime map manifest.
from pathlib import Path
import hashlib,json,re,shutil
import argparse
parser=argparse.ArgumentParser(description='Import Unity-rendered maps from exact release source checkouts.')
parser.add_argument('--release',action='append',nargs=4,required=True,metavar=('BUILD','SOURCE_COMMIT','PUBLISHING_COMMIT','EXPORT_PROJECT'))
args=parser.parse_args()
root=Path(__file__).resolve().parents[1]
releases=args.release
catalog=json.loads((root/'MapCatalog.html').read_text());bindings={};evidence=[]
prior=root/'Maps/releases.json'
if prior.exists():
 evidence=json.loads(prior.read_text())['releases']
 for release in evidence:
  bindings[release['build']]={m['level_name']:{'map_version':m['map_version'],'source_commit':release['source_commit']} for m in release['maps']}
updated={r[0] for r in releases}
evidence=[r for r in evidence if r['build'] not in updated]
for build,commit,publishing,folder in releases:
 assert re.fullmatch(r'[0-9a-f]{40}',commit) and re.fullmatch(r'[0-9a-f]{40}',publishing)
 project=Path(folder).resolve()
 manifest=project/'AnalyticsBackend/Maps/manifest.json'
 if not manifest.exists():raise RuntimeError('Export not complete: '+folder)
 maps=json.loads(manifest.read_text())['maps'];embedded=json.loads((project/'AnalyticsBackend/MapCatalog.html').read_text())['maps'];bindings[build]={}
 for m in maps:
  level=m['level_name'];scene=project/f'Assets/Scenes/{level}.unity'
  assert hashlib.sha256(scene.read_bytes()).hexdigest()==m['source_scene_hash']
  image=project/'AnalyticsBackend'/m['image_file'];assert hashlib.sha256(image.read_bytes()).hexdigest()[:12]==m['map_version'].split('-')[-1]
  dest=root/m['image_file'];dest.parent.mkdir(parents=True,exist_ok=True);shutil.copy(image,dest)
  full=next(v for v in embedded if v['map_version']==m['map_version'])
  catalog['maps']=[v for v in catalog['maps'] if not(v['level_name']==level and v['map_version']==m['map_version'])]+[full]
  bindings[build][level]={'map_version':m['map_version'],'source_commit':commit}
 included=[scene for enabled,scene in re.findall(r'- enabled: ([01])\s+path: Assets/Scenes/(\w+)\.unity',(project/'ProjectSettings/EditorBuildSettings.asset').read_text()) if enabled=='1' and scene!='Mainmenu']
 tracked=re.findall(r'^\s*-\s*(\w+)\s*$',(project/'Assets/Resources/AnalyticsSettings.asset').read_text().split('trackedScenes:',1)[1],re.M)
 evidence.append({'build':build,'source_commit':commit,'publishing_commit':publishing,'included_scenes':included,'tracked_scenes':tracked,'maps':maps})
(root/'MapCatalog.html').write_text(json.dumps(catalog,separators=(',',':')))
(root/'Maps/releases.json').write_text(json.dumps({'releases':evidence},indent=2)+'\n')
p=root/'DashboardData.gs';s=p.read_text();s=re.sub(r'  const bindings = .*; // RELEASE_MAP_BINDINGS',lambda _: '  const bindings = '+json.dumps(bindings,separators=(',',':'))+'; // RELEASE_MAP_BINDINGS',s);config={r['build']:{'included_scenes':r.get('included_scenes',[m['level_name'] for m in r['maps']]),'tracked_scenes':r.get('tracked_scenes',[])} for r in evidence}
s=re.sub(r'  const releases = .*; // RELEASE_BUILD_METADATA',lambda _: '  const releases = '+json.dumps(config,separators=(',',':'))+'; // RELEASE_BUILD_METADATA',s)
p.write_text(s)
print('Imported',len(bindings),'release bindings;',len(catalog['maps']),'total map assets')
