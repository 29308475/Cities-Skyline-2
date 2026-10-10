// Run from the workspace: node CityStreetOverlay/Tests/ui-smoke.cjs
// Tests the real UI module with mocked game bindings, not the Unity/Gameface runtime.
const http=require('node:http'),fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'..');
// Check the installed game's actual export table, not just our mock interface.
const gameUI=fs.readFileSync(process.env.CS2_UI_BUNDLE||'D:/SteamLibrary/steamapps/common/Cities Skylines II/Cities2_Data/Content/Game/UI/index.js','utf8');
const exportObject=gameUI.match(/"cs2\/ui":\{value:(\w+)\}/)[1];
const exportTable=gameUI.match(new RegExp('n\\.d\\('+exportObject+',\\{([^}]+)\\}\\)'))[1];
const source=fs.readFileSync(path.join(root,'UI/CityStreetOverlay.mjs'),'utf8');
const publicNames=source.match(/const \{ ([^}]+) \} = window\["cs2\/ui"\]/)[1].split(',').map(s=>s.trim());
for(const name of publicNames)assert(exportTable.includes(name+':()=>'),'Missing public game export: '+name);
for(const [,module,name] of source.matchAll(/getModule\("([^"]+)", "([^"]+)"\)/g)){
 const at=gameUI.indexOf('Q.add("'+module+'",');assert(at>=0,'Missing registered game module: '+module);
 assert(gameUI.slice(at,gameUI.indexOf(');',at)).includes('get '+name+'()'),'Missing module export: '+name);
}
const dependencies=path.resolve(root,'../.tools/ui-preview/node_modules');
const {chromium}=require(path.join(dependencies,'playwright-core'));
const files={
 '/':path.join(__dirname,'ui-harness.html'),
 '/CityStreetOverlay.mjs':path.join(root,'UI/CityStreetOverlay.mjs'),
 '/CityStreetOverlay.css':path.join(root,'UI/CityStreetOverlay.css'),
 '/react.js':path.join(dependencies,'react/umd/react.development.js'),
 '/react-dom.js':path.join(dependencies,'react-dom/umd/react-dom.development.js')
};
const server=http.createServer((req,res)=>{
 if(req.url.includes('MapTile.svg')){res.writeHead(200,{'Content-Type':'image/svg+xml'});res.end('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32"><path fill="#81d1e5" d="M2 5l9-3 10 3 9-3v25l-9 3-10-3-9 3z"/><path stroke="#234554" stroke-width="2" d="M11 2v25M21 5v25"/></svg>');return;}
 const file=files[req.url];if(!file){res.writeHead(404);res.end();return;}
 res.setHeader('Content-Type',file.endsWith('.html')?'text/html; charset=utf-8':file.endsWith('.css')?'text/css':'text/javascript');res.end(fs.readFileSync(file));
});
(async()=>{
 await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
 const browser=await chromium.launch({executablePath:'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',headless:true});
 try{
 const page=await browser.newPage({viewport:{width:1440,height:1000}});const errors=[];page.on('pageerror',e=>errors.push(e.message));
 await page.goto('http://127.0.0.1:'+server.address().port);await page.locator('.cso-input-root').waitFor();
 await page.getByRole('button',{name:'Straßentypen & Breiten',exact:false}).click();
 const artifact=path.join(root,'dist/window-preview.png');fs.mkdirSync(path.dirname(artifact),{recursive:true});await page.screenshot({path:artifact});
 assert.equal(await page.locator('.cso-road').count(),4);
 const legend=await page.locator('.cso-legend').boundingBox(),footer=await page.locator('.cso-footer').boundingBox();assert(legend.y+legend.height<=footer.y,'Legend remains above footer');
 await page.locator('.cso-road-toggle').first().click();
 assert(await page.evaluate(()=>commands.some(c=>c.action==='filter'&&c.type===0&&c.value===false)));
 await page.locator('.cso-road-choice').first().click();
 await page.getByPlaceholder('Spielstraße suchen …').fill('Autobahn');
 assert((await page.locator('.cso-road-option').count())>0);
 // Native wheel handlers must not bubble from nested lists to the outer controls.
 await page.locator('.cso-road-list').scrollIntoViewIfNeeded();
 const outerBefore=await page.locator('.cso-controls').evaluate(el=>el.scrollTop);
 await page.locator('.cso-road-list').evaluate(el=>el.scrollTop=0);
 await page.locator('.cso-road-list').dispatchEvent('wheel',{deltaY:80,bubbles:true,cancelable:true});
 assert((await page.locator('.cso-road-list').evaluate(el=>el.scrollTop))>0,'Inner list scrolls');
 assert.equal(await page.locator('.cso-controls').evaluate(el=>el.scrollTop),outerBefore,'Outer panel stays still');
 for(const deltaY of [5000,100,-5000,-100]){
  await page.locator('.cso-road-list').dispatchEvent('wheel',{deltaY,bubbles:true,cancelable:true});
  assert.equal(await page.locator('.cso-controls').evaluate(el=>el.scrollTop),outerBefore,'No chaining at either list edge');
 }
 await page.locator('.cso-road-option').first().click();assert(await page.evaluate(()=>commands.some(c=>c.action==='selectRoad')));
 await page.getByRole('button',{name:/^Kreisverkehr:/}).click();
 assert(await page.getByText('Rechtsverkehr · gegen den Uhrzeigersinn',{exact:true}).isVisible());
 await page.locator('.cso-roundabout-controls .cso-road-option').first().click();
 assert(await page.evaluate(()=>commands.some(c=>c.action==='roundaboutRoad'&&c.id)),'Ring profile command is sent');
 await page.getByRole('button',{name:/^Kreisverkehr:/}).click();
 await page.getByRole('button',{name:/^Ein-\/Ausfahrten:/}).click();
 await page.locator('.cso-roundabout-controls .cso-road-option').first().click();
 assert(await page.evaluate(()=>commands.some(c=>c.action==='roundaboutApproachRoad'&&c.id)),'Approach profile command is sent');
 await page.getByRole('button',{name:'Platzierung & Darstellung',exact:false}).click();
 const angle=page.getByRole('textbox',{name:'Drehung in Grad'});
 assert.equal(await angle.inputValue(),'0,0','Default rotation remains zero');
 await angle.fill('-59,4');await angle.press('Enter');
 assert(await page.evaluate(()=>commands.some(c=>c.action==='placement'&&c.field==='rotation'&&c.value===-59.4)),'Comma decimal rotation commits');
 await angle.fill('12.3');await angle.press('Tab');assert.equal(await angle.inputValue(),'12,3');
 await angle.fill('invalid');await angle.press('Enter');assert.equal(await angle.inputValue(),'12,3','Invalid input restores prior angle');
 await angle.fill('200');await angle.press('Enter');assert.equal(await angle.inputValue(),'180,0','Rotation is clamped');
 const rotationRange=page.locator('.cso-range').filter({has:angle}).locator('input[type=range]');
 await rotationRange.fill('27.8');assert.equal(await angle.inputValue(),'28,0','Slider keeps whole-degree steps');
 await page.getByRole('button',{name:'Position fixieren',exact:true}).click();
 assert.equal(await page.getByRole('button',{name:'← 10 m',exact:true}).isDisabled(),true);
 assert.equal(await angle.isDisabled(),true,'Rotation text respects position lock');
 await page.getByRole('button',{name:'Position entsperren',exact:true}).click();
 await page.getByRole('button',{name:'← 10 m',exact:true}).click();assert(await page.evaluate(()=>commands.some(c=>c.action==='nudge')));
 assert(await page.locator('.cso-controls').evaluate(el=>el.scrollHeight<=el.clientHeight+1),'Placement fits without scrolling');
 await page.screenshot({path:path.join(root,'dist/placement-preview.png')});
 await page.getByRole('button',{name:'Straßentypen & Breiten',exact:false}).click();
 assert(await page.locator('.cso-controls').evaluate(el=>el.scrollHeight<=el.clientHeight+1),'Road overview fits without scrolling');
 await page.locator('.cso-road-choice').first().click();
 await page.locator('.cso-controls').evaluate(el=>el.scrollTop=0);
 const controls=await page.locator('.cso-controls').boundingBox();await page.mouse.move(controls.x+controls.width/2,controls.y+80);
 const scrollWorld=await page.evaluate(()=>worldWheels);await page.mouse.wheel(0,500);await page.waitForTimeout(150);
 assert((await page.locator('.cso-controls').evaluate(el=>el.scrollTop))>0,'Panel content scrolls');assert.equal(await page.evaluate(()=>worldWheels),scrollWorld,'Scroll does not reach world');
 await page.mouse.move(120,230);assert.equal(await page.evaluate(()=>barrier),true);
 const before=await page.evaluate(()=>worldWheels);await page.mouse.wheel(0,400);assert.equal(await page.evaluate(()=>worldWheels),before);
 await page.mouse.click(160,230);assert.equal(await page.evaluate(()=>worldClicks),0);
 await page.mouse.move(1350,920);assert.equal(await page.evaluate(()=>barrier),false);
 await page.mouse.click(1350,920);assert.equal(await page.evaluate(()=>worldClicks),1,'World remains interactive outside panel');
 const first=await page.locator('.cso-input-root').boundingBox();
 await page.mouse.move(first.x+200,first.y+30);await page.mouse.down();await page.mouse.move(first.x+290,first.y+65);await page.mouse.up();
 const moved=await page.locator('.cso-input-root').boundingBox();assert(moved.x>first.x+60);assert.equal(await page.locator('.cso-drag-shield').count(),0);
 assert.equal(await page.locator('.cso-resize-handle').count(),0);
 assert.equal(await page.locator('.cso-close').count(),1);
 const header=await page.locator('.cso-header').boundingBox();
 assert(header.width>=moved.width-4,'Whole title bar spans the window');
 await page.mouse.move(header.x+header.width-160,header.y+30);await page.mouse.down();await page.mouse.move(header.x+header.width-210,header.y+10);await page.mouse.up();
 const movedFromRight=await page.locator('.cso-input-root').boundingBox();assert(movedFromRight.x<moved.x-35,'Right side of header drags too');
 assert.equal(movedFromRight.width,first.width,'Dragging retains fixed width');
 assert.equal(await page.locator('.cso-section-toggle>.cso-chevron').count(),3);
 assert.equal(await page.locator('.cso-chevron').first().textContent(),'','Accordion arrow does not rely on font glyphs');
 assert.equal(await page.locator('#toolbar .cso-icon-road').count(),3,'Road network uses CSS geometry');
 const previewBox=await page.locator('.cso-preview-frame').boundingBox(),nameBox=await page.locator('.cso-preview-heading').boundingBox(),hintBox=await page.locator('.cso-preview-note').boundingBox();
 assert(Math.abs(previewBox.x-nameBox.x)<2&&Math.abs(hintBox.x-nameBox.x)<2,'Preview and hint align with heading');
 assert(Math.abs(previewBox.width-nameBox.width)<2,'No empty side gutters around preview');
 await page.setViewportSize({width:800,height:600});await page.waitForTimeout(100);
 await page.getByRole('button',{name:'Platzierung & Darstellung',exact:false}).click();
 await page.getByRole('button',{name:'Position fixieren',exact:true}).click();
 await page.screenshot({path:path.join(root,'dist/placement-compact.png')});
 assert(await page.locator('.cso-controls').evaluate(el=>el.scrollHeight<=el.clientHeight+1),'Locked placement fits at 800x600 without scrolling');
 const compact=await page.locator('.cso-input-root').boundingBox();assert(compact.x>=0&&compact.y>=0&&compact.x+compact.width<=800&&compact.y+compact.height<=600);
 const compactLegend=await page.locator('.cso-legend').boundingBox(),compactFooter=await page.locator('.cso-footer').boundingBox();assert(compactLegend.y+compactLegend.height<=compactFooter.y,'Compact legend remains above footer');
 const save=await page.getByRole('button',{name:'Speichern',exact:true}).boundingBox();assert(save.y+save.height<=600);
 await page.screenshot({path:path.join(root,'dist/window-compact.png')});
 await page.locator('.cso-close').click();assert.equal(await page.locator('.cso-input-root').count(),0);await page.waitForFunction(()=>!barrier);
 await page.locator('#toolbar button').click();await page.getByRole('button',{name:'Stadt & Ausschnitt',exact:false}).click();await page.getByPlaceholder('Stadt oder Ort suchen …').fill('Berlin');await page.getByRole('button',{name:'Suchen',exact:true}).click();assert(await page.evaluate(()=>commands.some(c=>c.action==='search'&&c.query==='Berlin')));
 await page.evaluate(()=>fixture({map:null,visible:false,busy:true}));assert.equal(await page.getByRole('button',{name:'Auf Karte anzeigen',exact:true}).isDisabled(),true);assert.equal(await page.getByRole('button',{name:'Offline laden',exact:true}).isDisabled(),true);
 const note=await page.locator('.cso-preview-note').boundingBox(),status=await page.locator('.cso-status-row').boundingBox();assert(note.y+note.height<=status.y,'Preview note has reserved space above actions');
 assert.deepEqual(errors,[]);
 // A broken game module must close only this window and release its input barrier.
 await page.mouse.move(120,230);
 await page.evaluate(()=>{window.failModule=true;fixture({busy:false});});
 await page.waitForFunction(()=>document.querySelector('.cso-input-root')===null&&!window.barrier);
 assert.equal(await page.locator('#toolbar button').count(),1,'Toolbar survives a render failure');
 const clicks=await page.evaluate(()=>worldClicks);await page.mouse.click(790,590);
 assert.equal(await page.evaluate(()=>worldClicks),clicks+1,'World survives a render failure');
 await page.evaluate(()=>window.failModule=false);
 await page.locator('#toolbar button').click();await page.locator('.cso-input-root').waitFor();
 await page.setViewportSize({width:1440,height:1000});await page.reload();await page.locator('.cso-input-root').waitFor();
 for(const count of [1,4,5]){
  await page.evaluate(n=>fixture({places:Array.from({length:n},(_,i)=>'Stadt '+i),selected:'Stadt 0'}),count);
  const results=page.locator('.cso-results');
  assert.equal(await results.locator('.cso-place').count(),count);
  if(count<=4){
   assert.equal(await results.evaluate(el=>el.classList.contains('mock-scroll')),false,'Up to four cities have no scroll container');
   const box=await results.boundingBox();assert(Math.abs(box.height-count*36)<2,'Result height follows city count');
  }else{
   assert(await results.evaluate(el=>el.scrollHeight>el.clientHeight),'Five cities scroll in capped list');
  }
  assert(await page.locator('.cso-controls').evaluate(el=>el.scrollHeight<=el.clientHeight+1),'City controls and all accordion headers fit');
 }
 await page.evaluate(()=>fixture({places:['Muenchen'],selected:'Muenchen'}));
 await page.screenshot({path:path.join(root,'dist/city-results-preview.png')});
 await page.setViewportSize({width:2560,height:1440});await page.reload();await page.locator('.cso-spacious').waitFor();
 const large=await page.locator('.cso-input-root').boundingBox();assert(1440-large.y-large.height>=180,'Large window leaves clearance above game toolbar');
 for(const title of ['Stadt & Ausschnitt','Strassentypen','Platzierung & Darstellung']){
  if(title==='Strassentypen')await page.locator('.cso-section-toggle').nth(1).click();
  else if(title!=='Stadt & Ausschnitt')await page.getByRole('button',{name:title,exact:false}).click();
  assert(await page.locator('.cso-controls').evaluate(el=>el.scrollHeight<=el.clientHeight+1),'Spacious sections fit');
 }
 const placementRows=await page.locator('.cso-placement-grid>.cso-range').all();
 let previousBottom=0;
 for(const row of placementRows){
  const box=await row.boundingBox();assert(box.y>=previousBottom,'Placement rows do not overlap');previousBottom=box.y+box.height;
  const label=await row.locator('.cso-label-row').boundingBox();assert(label.x+label.width<=box.x+box.width+1,'Labels stay inside row');
 }
 const largeLegend=await page.locator('.cso-legend').boundingBox(),largeFooter=await page.locator('.cso-footer').boundingBox();assert(largeFooter.y-largeLegend.y-largeLegend.height>=24,'Legend has breathing room');
 await page.screenshot({path:path.join(root,'dist/spacious-window-preview.png')});
 await page.evaluate(()=>fixture({rotation:90,canPan:true,tileCount:1}));
 const previewFrame=await page.locator('.cso-interactive-preview').boundingBox();
 const priorPan=await page.evaluate(()=>commands.filter(c=>c.action==='panPreview').length);
 await page.mouse.move(previewFrame.x+previewFrame.width/2,previewFrame.y+previewFrame.height/2);await page.mouse.down();
 await page.mouse.move(previewFrame.x+previewFrame.width/2+60,previewFrame.y+previewFrame.height/2);
 assert.equal(await page.evaluate(()=>commands.filter(c=>c.action==='panPreview').length),priorPan,'No download while dragging');
 await page.mouse.up();
 const pan=await page.evaluate(()=>commands.filter(c=>c.action==='panPreview').slice(-1)[0]);
 assert(Math.abs(pan.east)<0.001&&pan.north>0,'Drag respects rotated north');
 assert.equal(await page.locator('.cso-drag-shield').count(),0,'Preview drag releases input shield');
 await page.evaluate(()=>fixture({busy:true}));
 await page.mouse.move(previewFrame.x+70,previewFrame.y+70);await page.mouse.down();await page.mouse.move(previewFrame.x+100,previewFrame.y+70);await page.mouse.up();
 assert.equal(await page.evaluate(()=>commands.filter(c=>c.action==='panPreview').length),priorPan+1,'Busy preview cannot start another load');
 await page.evaluate(()=>fixture({busy:false}));
 await page.getByRole('button',{name:'Offline laden',exact:true}).click();
 await page.locator('.cso-library').waitFor();assert.equal(await page.locator('.cso-template-entry').count(),2);
 await page.getByRole('button',{name:'Vorlage löschen',exact:true}).click();
 assert.equal(await page.getByRole('button',{name:'Endgültig löschen',exact:true}).count(),1);
 await page.getByRole('button',{name:'Abbrechen',exact:true}).click();
 assert(!await page.evaluate(()=>commands.some(c=>c.action==='deleteTemplate')),'Cancel delete leaves saved files untouched');
 await page.getByRole('button',{name:'Vorlage löschen',exact:true}).click();
 await page.getByRole('button',{name:'Endgültig löschen',exact:true}).click();
 assert(await page.evaluate(()=>commands.some(c=>c.action==='deleteTemplate'&&c.id==='fixture-2')),'Deletion addresses selected version only');
 await page.locator('.cso-template-entry').filter({hasText:'Version 1'}).click();
 assert((await page.locator('.cso-library-detail h3').textContent()).includes('Version 1'));
 await page.screenshot({path:path.join(root,'dist/library-preview.png')});
 await page.getByRole('button',{name:'Diese Vorlage laden',exact:true}).click();
 await page.waitForFunction(()=>!document.querySelector('.cso-library'));
 assert(await page.evaluate(()=>commands.some(c=>c.action==='loadTemplate'&&c.id==='fixture-1')),'Loads explicitly selected version');
 await page.getByRole('button',{name:'Offline laden',exact:true}).click();await page.getByRole('button',{name:'Zur\u00fcck',exact:true}).click();
 assert.equal(await page.locator('.cso-library').count(),0);
 const minimizeBounds=await page.locator('.cso-minimize').boundingBox(),closeBounds=await page.locator('.cso-close').boundingBox();
 assert(Math.abs(minimizeBounds.y-closeBounds.y)<1&&Math.abs(minimizeBounds.height-closeBounds.height)<1,'Header controls aligned');
 await page.getByRole('button',{name:'Minimieren',exact:true}).click();
 await page.locator('.cso-mini-dock').waitFor();
 assert.equal(await page.locator('.cso-mini-card').count(),4);
 const viewportBounds=await page.locator('.cso-mini-scroll').boundingBox(),footerBounds=await page.locator('.cso-mini-footer').boundingBox();
 assert(viewportBounds.y+viewportBounds.height<=footerBounds.y+1,'Category viewport never overlaps footer');
 assert.equal(await page.locator('.cso-window').count(),0,'Main window is replaced');
 const mini=await page.locator('.cso-mini-dock').boundingBox();assert(mini.x<40,'Mini windows sit on left edge');
 const card=page.locator('.cso-mini-card').first();
 await card.locator('input[type=range]').fill('35');
 assert(await page.evaluate(()=>commands.some(c=>c.action==='categoryOpacity'&&c.type===0&&c.value===.35)),'Independent category opacity');
 await card.locator('.cso-road-toggle').click();
 await card.locator('.cso-road-choice').click();
 await card.locator('.cso-road-option').first().click();
 assert.equal(await card.locator('input[type=range]').inputValue(),'35','Road choice keeps opacity');
 assert.equal(await card.locator('.cso-stamp-button').isDisabled(),true,'Hidden category cannot be built');
 await page.evaluate(()=>fixture({visible:true,filter:15}));
 await card.locator('.cso-stamp-button').click();
 assert(await page.evaluate(()=>commands.some(c=>c.action==='stampRoad'&&c.type===0)),'Stamp command selects correct category');
 assert.equal(await page.locator('.cso-stamp-button:disabled').count(),4,'No duplicate requests while building');
 await page.getByRole('button',{name:'Bau abbrechen',exact:true}).click();
 assert(await page.evaluate(()=>commands.some(c=>c.action==='cancelStamp')),'Cancel command is available');
 await page.screenshot({path:path.join(root,'dist/mini-windows-preview.png')});
 await page.evaluate(()=>fixture({selecting:true,stampBusy:false}));
 await page.locator('.cso-selection-surface').waitFor();
 await page.mouse.move(700,400);
 assert.equal(await page.evaluate(()=>barrier),false,'Moving from mod panel onto selection map releases panel camera barrier');
 const navigation=await page.evaluate(()=>{
   const surface=document.querySelector('.cso-selection-surface');
   const wheel=new WheelEvent('wheel',{bubbles:true,cancelable:true,deltaY:100});surface.dispatchEvent(wheel);
   const key=new KeyboardEvent('keydown',{bubbles:true,cancelable:true,key:'w',code:'KeyW'});surface.dispatchEvent(key);
   return {wheel:wheel.defaultPrevented,key:key.defaultPrevented};
 });
 assert.deepEqual(navigation,{wheel:false,key:false},'Selection surface does not consume wheel zoom or WASD');
 assert.equal(await page.evaluate(()=>document.elementFromPoint(700,400).id),'world','World remains the mouse hit target during selection');
 await page.evaluate(()=>emit('selectionBox',JSON.stringify({x0:700/innerWidth,y0:400/innerHeight,x1:950/innerWidth,y1:650/innerHeight})));
 await page.locator('.cso-selection-box').waitFor();
 const nativeBox=await page.locator('.cso-selection-box').boundingBox();
 assert(Math.abs(nativeBox.x-700)<1&&Math.abs(nativeBox.y-400)<1&&Math.abs(nativeBox.width-250)<1,'Native drag binding displays correct rectangle');
 assert.equal(await page.evaluate(()=>document.elementFromPoint(800,450).id),'world','Visible rectangle does not intercept map navigation');
 const beforeWheel=await page.evaluate(()=>worldWheels);
 await page.mouse.move(800,450);await page.mouse.wheel(0,120);
 await page.waitForFunction(n=>worldWheels>n,beforeWheel);
 await page.evaluate(()=>emit('selectionBox','null'));
 assert.equal(await page.locator('.cso-selection-box').count(),0,'Native drag cancellation removes rectangle');
 await page.getByRole('button',{name:'Auswahl beenden',exact:true}).click();
 assert(await page.evaluate(()=>commands.some(c=>c.action==='selectionMode')),'Panel remains clickable above selection surface');
 await page.evaluate(()=>fixture({selecting:false}));
 await page.evaluate(()=>fixture({selectionCount:2,selectionOnly:true}));
 await page.getByRole('button',{name:'2 markiert · Aktionen',exact:true}).click();
 await page.getByRole('button',{name:'Als Untergruppe zuordnen',exact:true}).click();
 await page.getByPlaceholder('Name der Untergruppe').fill('Auffahrt');
 await page.locator('.cso-subgroup-options button').first().click();
 await page.getByRole('button',{name:'Untergruppe anlegen',exact:true}).click();
 assert(await page.evaluate(()=>commands.some(c=>c.action==='createSubgroup'&&c.name==='Auffahrt'&&c.type===0&&c.id)),'Selected roads assigned as subgroup with own road prefab');
 await page.evaluate(()=>fixture({subgroups:[{id:'ramp-test',name:'Auffahrt',category:0,count:2,roadName:'Teststraße',prefabId:'test',available:true}]}));
 assert.equal(await page.locator('.cso-tree-row').count(),0,'Subgroups start collapsed');
 await page.getByRole('button',{name:'Unterkategorien (1)',exact:true}).click();
 await page.locator('.cso-tree-row').hover();
 assert(await page.evaluate(()=>commands.some(c=>c.action==='hoverRoadGroup'&&c.group==='ramp-test')),'Hover sends subgroup identity');
 await page.locator('.cso-tree-row').click();
 const subgroupPicker=page.locator('.cso-subgroup-road-picker');
 await subgroupPicker.locator('.cso-road-choice').click();
 await subgroupPicker.getByRole('button',{name:'Alle Straßen und Wege',exact:true}).click();
 const chosenName=await subgroupPicker.locator('.cso-road-option').last().locator('span').first().textContent();
 const categoryChoice=await card.locator('.cso-road > .cso-road-choice').textContent();
 await subgroupPicker.locator('.cso-road-option').last().click();
 assert.equal(await subgroupPicker.locator('.cso-road-list').count(),0,'Subgroup choice applies immediately and closes dropdown');
 assert((await subgroupPicker.locator('.cso-road-choice').textContent()).includes(chosenName),'Backend state refresh displays selected subgroup road');
 assert.equal(await card.locator('.cso-road > .cso-road-choice').textContent(),categoryChoice,'Subgroup selection leaves parent road unchanged');
 assert(await page.evaluate(()=>commands.some(c=>c.action==='subgroupRoad'&&c.group==='ramp-test')),'Existing subgroup road can be changed');
 await page.getByRole('button',{name:'Zuordnung aufheben',exact:true}).click();
 assert(await page.evaluate(()=>commands.some(c=>c.action==='deleteSubgroup'&&c.group==='ramp-test')),'Subgroup assignment can be removed');
 assert.equal(await page.locator('.cso-stamp-button:disabled').count(),4,'Pending removal must be resolved before building');
 await page.getByRole('button',{name:'Markierte entfernen',exact:true}).click();
 assert(await page.evaluate(()=>commands.some(c=>c.action==='removeMarked')),'Remove marked command');
 await page.evaluate(()=>fixture({selectionCount:0,selectionOnly:false,excludedCount:2}));
 assert((await page.locator('.cso-stamp-button').first().textContent()).includes('Bauen'),'Build action targets remaining roads');
 await page.getByRole('button',{name:'Weitere Einstellungen',exact:true}).click();
 await page.getByRole('button',{name:'Entfernte wiederherstellen',exact:true}).click();
 assert(await page.evaluate(()=>commands.some(c=>c.action==='restoreExcluded')),'Restore exclusions command');
 await page.getByRole('button',{name:'Gro\u00dfes Fenster',exact:true}).click();
 await page.locator('.cso-window').waitFor();assert.equal(await page.locator('.cso-mini-dock').count(),0);
 await page.getByRole('button',{name:'Minimieren',exact:true}).click();
 assert.equal(await page.locator('.cso-mini-card').first().locator('input[type=range]').inputValue(),'35','State survives view switch');
 await page.evaluate(()=>fixture({focusGroup:'ramp-test',focusCategory:0,focusRevision:1}));
 await page.locator('.cso-tree-focused').waitFor();
 assert.equal(await page.locator('.cso-tree-focused').getAttribute('aria-expanded'),'true','Overlay focus expands matching subgroup');
 assert(await page.locator('.cso-subgroup-road-picker .cso-road-choice').isVisible(),'Focused subgroup can be edited inline');
 await page.getByRole('button',{name:'Detailfenster schlie\u00dfen',exact:true}).click();
 await page.waitForFunction(()=>!document.querySelector('.cso-input-root')&&!barrier);
 console.log('PASS: installed game export contract; UI rendering, filters, road choice, lock, full-header dragging, fixed size, CSS road icon, CSS arrows, viewport bounds, mouse barrier, click/wheel isolation, close cleanup, search, busy state and render-error isolation/recovery');
 }finally{await browser.close();server.close();}
})().catch(e=>{console.error(e);server.close();process.exitCode=1;});
