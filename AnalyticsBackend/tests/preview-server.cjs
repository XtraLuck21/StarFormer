// Local synthetic preview only: this server never calls the live receiver.
const http=require('node:http'),fs=require('node:fs'),path=require('node:path');
const {runtime,event}=require('./backend.test.cjs');
const root=path.resolve(__dirname,'..'),rt=runtime(),api=rt.context;
const maps=JSON.parse(fs.readFileSync(path.join(root,'Maps/manifest.json'),'utf8')).maps;
const rows=[];let order=0;
for(const map of maps){
  const level=map.level_name,version=map.map_version;
  const coordinates=level==='Level01'?[[23,-8],[27,-8],[13,9],[14,9],[30,9]]:
    Array.from({length:5},(_,i)=>[map.min_x+(map.max_x-map.min_x)*(i+1)/6,map.min_y+(map.max_y-map.min_y)*.4]);
  for(let player=1;player<=4;player++){
    const base={environment:'test',player_id:'synthetic-private-'+player,level_run_id:level+'-run-'+player,
      level_name:level,map_version:version,build_version:'SYNTHETIC PREVIEW — NOT PLAYTEST DATA'};
    let active=0;
    function add(type,extra={}){rows.push(event({...base,event_type:type,level_active_seconds:active,attempt_id:'attempt-'+(++order),client_timestamp:new Date(Date.UTC(2026,10,1,10,0,order)).toISOString(),...extra}));}
    add('level_start');add('attempt_start');active=12;
    add('checkpoint_reached',{checkpoint_id:'CP1',attempt_active_seconds:12});add('attempt_start',{checkpoint_id:'CP1',start_reason:'checkpoint_reached'});
    const retries=player===1?0:player;
    for(let i=0;i<retries;i++){const [x,y]=coordinates[i%coordinates.length];const duration=[3,12,25,70][i%4];active+=duration;
      add('death',{position_x:x,position_y:y,checkpoint_id:'CP1',attempt_active_seconds:duration});add('attempt_start',{start_reason:'death_respawn',checkpoint_id:'CP1'});
    }
    for(const [form,seconds] of [['Star',15],['Box',25],['Bubble',18],['BouncyBall',35],...(level==='Level02'?[['Spider',28],['Fan',20]]:[])])add('form_duration',{controlled_form:form,duration_seconds:seconds});
    if(player!==4){active+=45+player*20;add('checkpoint_reached',{checkpoint_id:'CP2'});add('attempt_start',{checkpoint_id:'CP2',start_reason:'checkpoint_reached'});active+=25;add('level_complete');}
  }
}
for(const environment of ['test','production'])rt.sheets[environment==='test'?'RawEvents_Test':'RawEvents_Production']=rt.sheet(environment,[Array.from(api.columns),...rows.map(row=>Array.from(api.columns).map(key=>row[key]??''))]);
const mock=`<script>
window.google={script:{get run(){let success=()=>{},failure=console.error;const runner={
withSuccessHandler(fn){success=fn;return runner},withFailureHandler(fn){failure=fn;return runner},
getDashboardData(request){fetch('/data',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(request)}).then(r=>r.json()).then(success).catch(failure)},
getDashboardMap(level,version,build){fetch('/map?level='+encodeURIComponent(level)+'&version='+encodeURIComponent(version)+'&build='+encodeURIComponent(build)).then(r=>r.json()).then(success).catch(failure)}
};return runner}}};</script>`;
const server=http.createServer(async(req,res)=>{
  try{
    const url=new URL(req.url,'http://127.0.0.1');
    if(url.pathname==='/data'){
      let body='';for await(const chunk of req){body+=chunk;if(body.length>10000)throw new Error('Request too large');}
      res.setHeader('Content-Type','application/json');res.end(JSON.stringify(api.getDashboardData(JSON.parse(body))));
    }else if(url.pathname==='/map'){
      res.setHeader('Content-Type','application/json');res.end(JSON.stringify(api.getDashboardMap(url.searchParams.get('level'),url.searchParams.get('version'),url.searchParams.get('build'))));
    }else if(url.pathname.startsWith('/game/')){
      const buildRoot=path.resolve(root,'../Builds/WebGL-Analytics-AllLevels-Test');
      const name=decodeURIComponent(url.pathname.slice('/game/'.length))||'index.html';
      const file=path.resolve(buildRoot,name);
      if(!file.startsWith(buildRoot+path.sep))throw new Error('Invalid game path');
      const types={'.html':'text/html','.js':'text/javascript','.css':'text/css','.wasm':'application/wasm','.png':'image/png'};
      res.setHeader('Content-Type',types[path.extname(file)]||'application/octet-stream');fs.createReadStream(file).on('error',()=>{res.statusCode=404;res.end('Not found');}).pipe(res);
    }else if(url.pathname==='/source'){
      const file=url.searchParams.get('file');
      if(!['Code.gs','DashboardData.gs','Dashboard.html','MapCatalog.html'].includes(file))throw new Error('Unknown source file');
      res.setHeader('Content-Type','text/plain;charset=utf-8');res.end(fs.readFileSync(path.join(root,file),'utf8'));
    }else if(url.pathname==='/'){
      const html=fs.readFileSync(path.join(root,'Dashboard.html'),'utf8').replace('<script>',mock+'<script>').replace('<main>','<main><div class="notice">LOCAL SYNTHETIC PREVIEW — no real playtest data and no Google Sheet writes.</div>');
      res.setHeader('Content-Type','text/html;charset=utf-8');res.end(html);
    }else{res.writeHead(404);res.end('Not found');}
  }catch(error){res.writeHead(500);res.end(JSON.stringify({error:error.message}));}
});
const port=Number(process.argv[2]||8765);
server.listen(port,'127.0.0.1',()=>console.log('Synthetic preview: http://127.0.0.1:'+port+'/'));
