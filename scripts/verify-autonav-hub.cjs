// Offline geometry, typography and pointer targets; not a native/gameplay test.
const {chromium}=require('playwright'),fs=require('node:fs'),path=require('node:path'),http=require('node:http');
const root=path.resolve(__dirname,'..'),packageDir=process.argv[2]?path.resolve(process.argv[2]):null,out=path.join(root,'.local/art-review/hub',packageDir?'package':'');
const assert=(ok,message)=>{if(!ok)throw Error(message)};
(async()=>{
 fs.mkdirSync(out,{recursive:true});
 const layout=JSON.parse(fs.readFileSync(path.join(root,'assets/phobos-autonav/hub-layout.json'),'utf8'));
 const body=layout.boxes.find(b=>b.id==='body');
 const zones=Object.fromEntries(layout.zones.map(zone=>[zone.id,zone]));
 const inside=(a,b)=>a.x>=b.x&&a.y>=b.y&&a.x+a.w<=b.x+b.w&&a.y+a.h<=b.y+b.h;
 const panels=layout.zones.filter(z=>z.id!=='title');
 for(let i=1;i<panels.length;i++)assert(panels[i].y>=panels[i-1].y+panels[i-1].h+4,'Frame gap missing: '+panels[i].id);
 for(const box of layout.boxes.filter(box=>!box.id.includes('.'))){assert(zones[box.zone]&&inside(box,zones[box.zone]),'Outside designated faceplate field: '+box.id);}
 const unique=new Set();for(const b of layout.boxes){assert(!unique.has(b.id),'Duplicate box '+b.id);unique.add(b.id);assert(b.x>=0&&b.y>=0&&b.w>0&&b.h>0,'Invalid '+b.id);const sub=b.id.includes('.');assert(b.x+b.w<=(sub?body.w:layout.width)&&b.y+b.h<=(sub?body.h:layout.height),'Outside '+b.id);}
 // Navigation settings and docking details intentionally occupy alternate views.
 const groups=[layout.boxes.filter(b=>!b.id.includes('.')),...['nav.','pursuit.','fire.','systems.'].map(prefix=>layout.boxes.filter(b=>b.id.startsWith(prefix))),layout.boxes.filter(b=>b.id.startsWith('dock.')||['nav.actions','nav.metrics'].includes(b.id))];
 for(const boxes of groups)for(let i=0;i<boxes.length;i++)for(let j=i+1;j<boxes.length;j++){const a=boxes[i],b=boxes[j];assert(!(a.x<b.x+b.w&&b.x<a.x+a.w&&a.y<b.y+b.h&&b.y<a.y+a.h),'Overlapping '+a.id+' / '+b.id);}
 const server=http.createServer((req,res)=>{let file=path.resolve(root,'.'+decodeURIComponent(new URL(req.url,'http://localhost').pathname));if(!file.startsWith(root+path.sep)){res.writeHead(403).end();return;}fs.readFile(file,(err,data)=>{if(err){res.writeHead(404).end();return;}res.setHeader('Content-Type',file.endsWith('.json')?'application/json':file.endsWith('.png')?'image/png':'text/html');res.end(data);});});
 await new Promise(r=>server.listen(0,'127.0.0.1',r));const browser=await chromium.launch({channel:'msedge',headless:true});
 const results=[];
 try{
  const page=await browser.newPage({viewport:{width:850,height:1250},deviceScaleFactor:1});
  await page.goto(packageDir?require('node:url').pathToFileURL(path.join(packageDir,'polaris-flight-hub-preview.html')).href:`http://127.0.0.1:${server.address().port}/assets/phobos-autonav/previews/flight-hub.html`);await page.evaluate(()=>window.ready);
  assert(await page.locator('.art').evaluate(e=>e.complete&&e.naturalWidth===1200&&e.naturalHeight===1920),'Production faceplate failed to load');
  for(const width of [300,400,600])for(const mode of ['navigation','pursuit','fire','systems','docking','departure','details','groups','handoff','warning'])for(const expanded of [false,true]){
   await page.selectOption('#size',String(width));await page.selectOption('#page',mode);await page.locator('#expanded').setChecked(expanded);
   const faults=await page.evaluate(()=>{
    const faults=[],panel=document.querySelector('#panel').getBoundingClientRect();
    for(const e of document.querySelectorAll('#panel [data-critical="true"],#panel [data-control="true"]')){const r=e.getBoundingClientRect();if(r.width<20||r.height<20)faults.push('small target '+e.textContent);if(e.dataset.critical==='true'&&(e.scrollHeight>e.clientHeight+1||e.scrollWidth>e.clientWidth+1))faults.push('text overflow '+e.textContent);if(!e.closest('.dialog')&&(r.left<panel.left||r.right>panel.right+1||r.bottom>panel.bottom+1))faults.push('outside '+e.textContent);}
    const viewport=document.querySelector('.dialog');
    if(viewport){const r=viewport.getBoundingClientRect(),frame=document.querySelector('[data-zone="body"]').getBoundingClientRect(),footer=document.querySelector('[data-box="actions"]').getBoundingClientRect();if(r.top<=frame.top||r.bottom>=frame.bottom||r.right>=frame.right||r.left<=frame.left||r.bottom>=footer.top)faults.push('Scroll viewport exceeds frame or footer');if(getComputedStyle(viewport).overflowY!=='scroll')faults.push('Missing visible scrollbar');const before=footer.top;viewport.scrollTop=viewport.scrollHeight;if(document.querySelector('[data-box="actions"]').getBoundingClientRect().top!==before)faults.push('Scrolling moved fixed actions');if(Math.abs(viewport.scrollHeight-viewport.clientHeight-viewport.scrollTop)>1)faults.push('Cannot reach end of content');viewport.scrollTop=0;}
    for(const e of document.querySelectorAll('#panel .label')){if(!e.classList.contains('clip')&&e.scrollHeight>e.clientHeight+1)faults.push('label overflow '+e.textContent);}
    for(const e of document.querySelectorAll('#panel .tab-face')){if(e.scrollHeight>e.clientHeight+1||e.scrollWidth>e.clientWidth+1)faults.push('tab face text overflow '+e.textContent);}
    for(const e of document.querySelectorAll('#panel [data-field="true"] .label')){const r=e.getBoundingClientRect(),field=e.parentElement.getBoundingClientRect();if(r.left<=field.left||r.right>=field.right||r.top<field.top||r.bottom>field.bottom)faults.push('readout crosses its bezel: '+e.textContent);}
    return faults;
   });
   results.push({width,height:width*1.6,page:mode,expanded,faults});
   await page.locator('#panel').screenshot({path:path.join(out,`${mode}-${width}${expanded?'-expanded':''}.png`)});
  }
  fs.writeFileSync(path.join(out,'layout-results.json'),JSON.stringify(results,null,2)+'\n');
  const errors=results.filter(r=>r.faults.length);assert(!errors.length,JSON.stringify(errors,null,2));
  await page.selectOption('#page','details');assert(await page.locator('.dialog').evaluate(e=>e.scrollHeight>e.clientHeight),'Long details should scroll');
  console.log(`PASS: ${results.length} page/size/translation cases. Live frame gaps, inset scroll bounds, reachable content and fixed actions checked. Compact 10 px secondary text at 300 × 480; native readability awaits owner testing. Offline only. ${out}`);
 }finally{await browser.close();await new Promise(r=>server.close(r));}
})().catch(e=>{console.error(e);process.exitCode=1});
