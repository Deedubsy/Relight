import {cancelUiDrag} from './uiDrag';
import { bound, BINDINGS, shortcut,type Binding } from './controls';
import { hudInset } from './view';

/** DOM presentation only. Domain panels keep their queries and command handlers. */
export function el<K extends keyof HTMLElementTagNameMap>(tag: K, cls?: string, text?: string): HTMLElementTagNameMap[K] {
  const node = document.createElement(tag);
  if (cls) node.className = cls;
  if (text !== undefined) node.textContent = text;
  return node;
}
export const uiInput = { blocked: false, modal: false };
export function uiPalette() {
  const css = getComputedStyle(document.documentElement);
  return { color: css.getPropertyValue('--ui-secondary').trim(), backgroundColor: css.getPropertyValue('--ui-bg').trim(), fontFamily: css.fontFamily, fontSize: '16px' };
}
type Adapter = { body: HTMLElement; title: string; enter?: () => void; leave?: () => void };
export interface ShellHooks { releaseInput(): void; cancelSelection(): boolean; speed(): number; setSpeed(speed: number): void }

export function createUiShell(root: HTMLElement, hooks: ShellHooks) {
  document.body.classList.add('ui-shell');
  root.setAttribute('aria-label', 'Game panels');
  const nav = el('nav', 'ui-navigation'); nav.setAttribute('aria-label', 'Game controls');
  const brand = el('span', 'ui-brand', 'RELIGHT'); nav.append(brand);
  const drawer = el('section', 'ui-drawer'); drawer.hidden = true;
  const heading = el('header', 'ui-drawer-heading'), title = el('h2'); title.id = 'ui-drawer-title';
  const close = el('button', undefined, 'Close'); close.setAttribute('aria-label', 'Close panel');
  heading.append(title, close); drawer.append(heading); drawer.setAttribute('aria-labelledby', title.id);
  const modal = el('section', 'ui-modal'); modal.hidden = true; modal.tabIndex = -1;
  modal.setAttribute('role', 'dialog'); modal.setAttribute('aria-modal', 'true'); modal.setAttribute('aria-labelledby', 'pause-title');
  const pauseTitle = el('h1', undefined, 'Paused'); pauseTitle.id = 'pause-title';
  const pauseActions = el('div', 'ui-pause-actions'), child = el('div', 'ui-modal-child'); child.hidden = true;
  const resume = el('button', 'primary', 'Resume'); pauseActions.append(resume); modal.append(pauseTitle, pauseActions, child);
  root.append(drawer, modal); document.getElementById('app')!.append(nav);
  const adapters = new Map<string, Adapter>();
  let active: string | null = null, childId: string | null = null, previousSpeed = 1;

  const held = new Set<string>(), suppressed = new Set<string>();
  const canvas = () => document.querySelector<HTMLCanvasElement>('#map > canvas');
  const inUi = (target: EventTarget | null) => target instanceof Element && !!target.closest('#panel, .ui-navigation, #threat-controls, .ui-hud');
  function release() { for (const key of held) suppressed.add(key); hooks.releaseInput(); document.getElementById('tooltip')!.hidden = true; }
  function sync() {
    document.body.dataset.panel=active??'';if(active!=='inventory'&&active!=='inspection')drawer.classList.remove('storage-window');
    uiInput.modal = !modal.hidden; uiInput.blocked = !modal.hidden || active !== null || !!document.activeElement?.closest('input, textarea, select, [contenteditable=true]');
    // Reserve overlay layout space for HUD text; the world camera ignores these insets.
    hudInset.right = active && modal.hidden ? window.innerWidth - drawer.getBoundingClientRect().left + 12 : 0;
    hudInset.bottom = nav.offsetHeight + 24;
    document.documentElement.style.setProperty('--ui-nav-height', nav.offsetHeight+'px');
    document.documentElement.style.setProperty('--ui-drawer-inset',hudInset.right+'px');
    nav.querySelectorAll<HTMLButtonElement>('[data-drawer]').forEach(b => b.setAttribute('aria-expanded', String(b.dataset.drawer === active)));
  }
  function restore(target: HTMLElement | null) {
    const valid = target?.isConnected && target.getClientRects().length && !target.closest('[hidden]');
    (valid ? target : canvas())?.focus({ preventScroll: true }); sync();
  }
  function closeMenus(){nav.querySelectorAll<HTMLDetailsElement>('details[open]').forEach(d=>{d.open=false;});}
  function closeDrawer(focus = true) {
    cancelUiDrag();closeMenus();
    if (!active) return;
    const adapter = adapters.get(active)!; adapter.body.hidden = true; adapter.leave?.(); active = null; drawer.hidden = true;
    release(); if (focus) restore(canvas()); sync();
  }
  function open(id: string) {
    const adapter = adapters.get(id); if (!adapter || !modal.hidden) return;
    if (active !== id) {
      closeDrawer(false); active = id;
      adapter.body.hidden = false; drawer.hidden = false; title.textContent = adapter.title;
    }
    release(); adapter.enter?.(); close.focus({ preventScroll: true }); sync();
  }
  function closeChild() {
    if (!childId) return;
    const adapter = adapters.get(childId)!; adapter.body.hidden = true; drawer.append(adapter.body); adapter.leave?.();
    childId = null; child.replaceChildren(); child.hidden = true; pauseActions.hidden = false; resume.focus(); sync();
  }
  function pause() {
    if (!modal.hidden) return;
    if (hooks.speed() > 0) previousSpeed = 1;
    release(); closeDrawer(false); hooks.setSpeed(0); modal.hidden = false; nav.inert = true;
    resume.focus({ preventScroll: true }); sync();
  }
  function unpause() {
    closeChild(); modal.hidden = true; nav.inert = false; release(); hooks.setSpeed(previousSpeed); restore(canvas()); sync();
  }
  function pauseChild(id: string) {
    const adapter = adapters.get(id); if (!adapter) return;
    closeChild(); childId = id; pauseActions.hidden = true; child.hidden = false;
    const back = el('button', undefined, 'Back to Pause'); back.onclick = closeChild;
    child.append(back, el('h2', undefined, adapter.title), adapter.body); adapter.body.hidden = false; adapter.enter?.(); back.focus(); sync();
  }
  function action(label: string, run: () => void, destination: HTMLElement = nav) {
    const b = el('button', undefined, label); b.type = 'button'; b.onclick = run; destination.append(b);
    const name=label.split(' (')[0],binding=({Build:'build',Backpack:'pockets',Inventory:'pockets',Map:'map',Save:'save',Load:'load'} as Record<string,Binding>)[name];
    if(binding&&label.includes('(')){const refresh=()=>{b.textContent=`${name} (${name==='Save'||name==='Load'?'Ctrl+':''}${shortcut(binding)})`;};window.addEventListener('relight:preferences',refresh);refresh();}return b;
  }
  function register(id: string, label: string, nodes: HTMLElement[], enter?: () => void, leave?: () => void) {
    if (adapters.has(id)) throw Error(`Duplicate UI adapter: ${id}`);
    const body = el('div', 'ui-drawer-body'); body.dataset.adapter = id; body.hidden = true; body.append(...nodes); drawer.append(body);
    adapters.set(id, { body, title: label, enter, leave });
  }
  function toggle(id: string) { if (active === id) closeDrawer(); else open(id); }
  function drawerButton(id: string, label: string) { const b = action(label, () => toggle(id)); b.dataset.drawer = id;const binding=({build:'build',inventory:'pockets',projects:'projects'} as Record<string,Binding>)[id];if(binding){const refresh=()=>{b.textContent=`${label.replace(/ \([^)]+\)$/,'')} (${shortcut(binding)})`;};window.addEventListener('relight:preferences',refresh);refresh();} b.setAttribute('aria-expanded', 'false'); return b; }
  close.onclick = () => closeDrawer(); resume.onclick = unpause;
  // Capture before Phaser's window handlers; native form editing remains local to the UI.
  window.addEventListener('keydown', event => {
    const key = event.code || event.key; if(!event.repeat)suppressed.delete(key); held.add(key);
    // Inventory owns plain Tab even when a slot or quantity field has focus.
    if ((active === 'inventory'||active === 'inspection'&&drawer.classList.contains('storage-window')) && modal.hidden && event.key === 'Tab' && !event.shiftKey && !event.ctrlKey && !event.metaKey && !event.altKey) {
      event.preventDefault(); event.stopImmediatePropagation();
      if (!event.repeat) { closeDrawer(false); restore(canvas()); }
      return;
    }
    if (suppressed.has(key)) { event.stopImmediatePropagation(); if (!inUi(event.target)) event.preventDefault(); return; }
    if (bound('cancel', event.key)) {
      event.preventDefault(); event.stopImmediatePropagation(); if (event.repeat) return;
      if(cancelUiDrag())return;
      const slotMenu=nav.querySelector<HTMLElement>('.slot-menu:not([hidden])');if(slotMenu){slotMenu.hidden=true;restore(canvas());return;}
      const menu=nav.querySelector<HTMLDetailsElement>('details[open]');if(menu){menu.open=false;restore(canvas());return;}
      if (childId) closeChild(); else if (!modal.hidden) unpause(); else if (active) closeDrawer(); else if (!hooks.cancelSelection()) pause();
      return;
    }
    if (!modal.hidden && event.key === 'Tab') {
      const nodes = Array.from(modal.querySelectorAll<HTMLElement>('button:not(:disabled), a[href], input:not(:disabled), select:not(:disabled), textarea:not(:disabled), [tabindex="0"]')).filter(n => n.getClientRects().length > 0 && !n.closest('[hidden]'));
      const index = nodes.indexOf(document.activeElement as HTMLElement), next = (index + (event.shiftKey ? -1 : 1) + nodes.length) % nodes.length;
      event.preventDefault(); event.stopImmediatePropagation(); (nodes[next] ?? modal).focus(); return;
    }
    const textFocus = event.target instanceof Element && !!event.target.closest('input, textarea, select, [contenteditable=true]');
    if(!textFocus && (event.ctrlKey||event.metaKey) && (bound('save',event.key)||bound('load',event.key)))return;
    // Native UI handlers (including key capture) must receive the event before the world is blocked.
    if ((!modal.hidden || active) && !inUi(event.target)) event.stopImmediatePropagation();
  }, true);
  window.addEventListener('keyup', event => { held.delete(event.code || event.key); suppressed.delete(event.code || event.key); }, true);
  window.addEventListener('pointerdown', event => {
    if (inUi(event.target)) { release(); return; }
    if (!modal.hidden || active) { event.preventDefault(); event.stopImmediatePropagation(); if (modal.hidden) closeDrawer(); return; }
    if(event.target === canvas()){canvas()?.focus({preventScroll:true});sync();}
  }, true);
  window.addEventListener('pointerup', event => { if (inUi(event.target) || event.target !== canvas()) release(); }, true);
  window.addEventListener('focusin', event => { if (inUi(event.target)) release(); if (!modal.hidden && !modal.contains(event.target as Node)) resume.focus(); sync(); });
  window.addEventListener('blur', () => { release(); held.clear(); });
  document.addEventListener('visibilitychange', () => { if (document.hidden) { release(); held.clear(); } });
  const stopKeys=(event:KeyboardEvent)=>{const text=event.target instanceof Element&&!!event.target.closest('input,textarea,select,[contenteditable=true]');if(!text&&(event.ctrlKey||event.metaKey)&&(bound('save',event.key)||bound('load',event.key)))return;if(text||active||!modal.hidden||['Enter',' ','Tab','ArrowUp','ArrowDown','ArrowLeft','ArrowRight'].includes(event.key))event.stopPropagation();};
  function protect(surface:HTMLElement){surface.addEventListener('keydown',stopKeys);for (const name of ['pointerdown', 'pointerup', 'click', 'contextmenu', 'wheel']) surface.addEventListener(name,event=>event.stopPropagation());}
  for (const surface of [root, nav, document.getElementById('threat-controls')].filter(Boolean) as HTMLElement[]) {
    surface.addEventListener('keydown',stopKeys);
    for (const name of ['pointerdown', 'pointerup', 'click', 'contextmenu', 'wheel']) surface.addEventListener(name, event => event.stopPropagation());
  }
  window.addEventListener('relight:preferences',()=>{release();sync();});
  new ResizeObserver(sync).observe(nav); new ResizeObserver(sync).observe(drawer);
  return { register, open, toggle, close: closeDrawer, action, drawerButton, pause, pauseChild,
    pauseAction: (label: string, run: () => void) => action(label, run, pauseActions),
    active: () => active, paused: () => !modal.hidden, sync, protect,
  };
}
export type UiShell = ReturnType<typeof createUiShell>;

export function controlsHelp(campaign=false) {
 const body=el('section');const render=()=>{body.replaceChildren(el('h2',undefined,'Controls'),el('p','hint','Panels stay live: enemies and machines keep moving. Escape closes the panel; press Escape again to Pause. Closing a panel returns world controls.'));
 const list=el('dl','ui-controls');for(const [name,keys] of Object.entries(BINDINGS))if(keys.length&&!(campaign&&['track','tram','tramstop'].includes(name))){const modifier=['copy','paste','save','load','undo','redo'].includes(name)?'Ctrl / Cmd + ':'';list.append(el('dt',undefined,name),el('dd',undefined,keys.some(k=>/^[0-9]$/.test(k))?'Quickbar slot (see current assignments)':modifier+keys.map(k=>k===' '?'Space':k).join(' / ')));}
 body.append(list,el('p','hint',`${shortcut('mirrorX')} / ${shortcut('mirrorY')} mirror while pasting a blueprint. Rebind controls and scale the interface in Pause → Settings.`));};render();window.addEventListener('relight:preferences',render);return body;
}
