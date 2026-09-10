const fs=require('fs');
const{chromium}=require('C:/Users/Admin/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
(async()=>{const browser=await chromium.launch({channel:'chrome',headless:true});try{
 const page=await browser.newPage({viewport:{width:1440,height:900}}),errors=[];page.on('pageerror',e=>{errors.push(e.message);console.error('PAGE',e.message);});page.on('console',m=>{if(m.type()==='error')console.error('CONSOLE',m.text());});
 await page.goto('http://127.0.0.1:5190/?view=world');await page.waitForFunction(()=>window.__relight?.world);
 const main=await page.evaluate(()=>fetch('/src/main.ts').then(r=>r.text()));
 const phaserURL=main.match(/from "([^"]*phaser\.js[^"]*)"/)[1];
 await page.route('**/scene-review',route=>route.fulfill({contentType:'text/html',body:'<html><body style="margin:0;background:#16282a"></body></html>'}));
 await page.goto('http://127.0.0.1:5190/scene-review');
 const data=JSON.parse(fs.readFileSync('packages/game/src/editor/RiverfrontCity.scene','utf8'));
 const result=await page.evaluate(async({phaserURL,data})=>{const{default:Phaser}=await import(phaserURL);return new Promise((resolve,reject)=>{
  const failures=[];window.reviewGame=new Phaser.Game({type:Phaser.AUTO,width:1440,height:900,audio:{noAudio:true},banner:false,scene:{
   preload(){this.load.on('loaderror',f=>failures.push(f.key));this.load.pack('buildings','/relight-asset-pack.json');this.load.pack('reference','/relight-editor-pack.json');},
   create(){window.reviewScene=this;for(const o of data.displayList)this.add.image(o.x??0,o.y??0,o.texture.key).setName(o.label).setOrigin(o.originX??.5,o.originY??.5).setScale(o.scaleX??1,o.scaleY??1);
    this.cameras.main.setZoom(.55).centerOn(72*32,374*32);const ref=this.textures.get('editor-riverfront-ground').getSourceImage();resolve({objects:this.children.list.length,reference:[ref.width,ref.height],failures});}
  }});setTimeout(()=>reject(Error('Scene load timeout')),20000);
 });},{phaserURL,data});
 if(result.objects!==288||result.failures.length||errors.length)throw Error(JSON.stringify({result,errors}));
 await page.screenshot({path:'docs/evidence/phaser-scene/scene-home.png'});
 await page.evaluate(()=>reviewScene.cameras.main.setZoom(.048).centerOn(864*16,576*16));
 await page.screenshot({path:'docs/evidence/phaser-scene/scene-city.png'});
 fs.writeFileSync('docs/evidence/phaser-scene/render.json',JSON.stringify({nativeEditorUI:false,renderer:'Phaser using the native scene object transforms',...result,errors},null,2));console.log(JSON.stringify(result));
}finally{await browser.close()}})().catch(e=>{console.error(e);process.exitCode=1});
