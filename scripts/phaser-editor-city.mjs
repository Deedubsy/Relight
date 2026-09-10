/** Launch the map tool with this checkout's existing native esbuild installation. */
import {existsSync} from 'node:fs';
import {fileURLToPath} from 'node:url';
import {resolve} from 'node:path';
import {spawn} from 'node:child_process';
const root=fileURLToPath(new URL('../',import.meta.url)),mode=process.argv[2];
if(!['export','check','apply'].includes(mode))throw Error('Expected export, check or apply');
const wsl=process.platform==='win32'&&!existsSync(resolve(root,'node_modules/@esbuild/win32-x64/esbuild.exe'));
const child=wsl?spawn('wsl.exe',['-d','Ubuntu-24.04','--cd',root,'--','bash','-lc',`if [ -s "$HOME/.nvm/nvm.sh" ]; then . "$HOME/.nvm/nvm.sh"; fi; node --import tsx packages/tools/src/phaserCity.ts ${mode}`],{cwd:root,stdio:'inherit',windowsHide:true}):spawn(process.execPath,['--import','tsx','packages/tools/src/phaserCity.ts',mode],{cwd:root,stdio:'inherit',windowsHide:true});
child.on('error',e=>{console.error(e.message);process.exitCode=1;});child.on('exit',code=>{process.exitCode=code??1;});
