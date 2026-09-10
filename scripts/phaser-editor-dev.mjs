/** Keep Phaser Editor's Play URL in sync with Vite, using this checkout's native dependencies. */
import {existsSync,readFileSync} from 'node:fs';
import {fileURLToPath} from 'node:url';
import {resolve} from 'node:path';
import {spawn} from 'node:child_process';

const root=fileURLToPath(new URL('../',import.meta.url));
const config=JSON.parse(readFileSync(resolve(root,'phasereditor2d.config.json'),'utf8'));
const url=new URL(config.playUrl);
let child;
if(process.platform==='win32'&&!existsSync(resolve(root,'node_modules/@esbuild/win32-x64/esbuild.exe'))){
  // This working tree was installed in Ubuntu. Do not reinstall over its shared node_modules.
  console.log('Starting the Phaser Editor preview using WSL (Ubuntu-24.04).');
  child=spawn('wsl.exe',['-d','Ubuntu-24.04','--cd',root,'--','bash','-lc',
    'if [ -s "$HOME/.nvm/nvm.sh" ]; then . "$HOME/.nvm/nvm.sh"; fi; node scripts/phaser-editor-dev.mjs'],
    {cwd:root,stdio:'inherit',windowsHide:true});
}else{
  console.log(`Phaser Editor Play: ${url.href}`);
  child=spawn(process.execPath,[resolve(root,'node_modules/vite/bin/vite.js'),resolve(root,'packages/game'),
    '--host',url.hostname,'--port',url.port,'--strictPort'],{cwd:root,stdio:'inherit',windowsHide:true,
    // Windows editor writes do not reliably emit Linux filesystem events on /mnt drives.
    env:{...process.env,...(/^\/mnt\/[a-z]\//.test(root)?{CHOKIDAR_USEPOLLING:'true',CHOKIDAR_INTERVAL:'500'}:{})}});
}
child.on('error',error=>{console.error('Could not start preview:',error.message);process.exitCode=1;});
child.on('exit',code=>{process.exitCode=code??0;});
process.on('SIGINT',()=>child.kill('SIGINT'));
process.on('SIGTERM',()=>child.kill('SIGTERM'));
