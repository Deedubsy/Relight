import {test} from 'node:test';
import assert from 'node:assert/strict';
import {parseUiPreferences,preferenceView,saveUiPreferences,saveQuickbar,DEFAULT_QUICKBAR} from '../../game/src/uiPreferences';
import {applyBindings,bound,bindingProblem,parseBindings,movementKeys,shortcut} from '../../game/src/controls';
import {createCampaign,makeSave,loadState,stateHash} from '../src/index';
test('UI-06 optional preferences preserve old quickbars and recover malformed individual fields',()=>{
 const quickbar=[...DEFAULT_QUICKBAR];quickbar[0]=null;const p=parseUiPreferences(JSON.stringify({version:1,quickbar,scale:150,motion:'reduce',bindings:{inspect:'o',build:'w',bogus:'x'},dismissedHints:['opening','achievement']}));assert.deepEqual(p.quickbar,quickbar);assert.equal(p.scale,150);assert.equal(p.motion,'reduce');assert.deepEqual(p.bindings,{inspect:'o'});assert.deepEqual(p.dismissedHints,['opening']);
 const old=parseUiPreferences(JSON.stringify({version:1,quickbar}));assert.equal(old.scale,100);assert.equal(old.motion,'system');assert.deepEqual(old.bindings,{});assert.deepEqual(parseUiPreferences('{').quickbar,DEFAULT_QUICKBAR);assert.equal(parseUiPreferences('{"version":9,"scale":150}').scale,100);assert.equal(parseUiPreferences('{"version":1,"scale":900}').scale,100);
});
test('UI-06 binding validation preserves modifier/blueprint contexts and prevents world collisions',()=>{
 assert.equal(bindingProblem('inspect','o',{}),'');assert.ok(bindingProblem('inspect','w',{}));assert.ok(bindingProblem('north','ArrowLeft',{}));assert.equal(bindingProblem('save','m',{}),'');assert.equal(bindingProblem('mirrorX','h',{}),'');assert.ok(bindingProblem('mirrorX','w',{}));assert.ok(bindingProblem('cancel','q',{}));assert.deepEqual(parseBindings(JSON.parse('{"__proto__":"o","constructor":"o"}')),{});
 applyBindings({inspect:'o',north:'n',save:'m'});assert.ok(bound('inspect','O'));assert.ok(!bound('inspect','f'));assert.ok(bound('north','ArrowUp'));assert.equal(movementKeys().W,78);assert.equal(shortcut('save'),'M');applyBindings({});assert.ok(bound('inspect','f'));assert.equal(movementKeys().W,87);
});
test('UI-06 settings and quickbar saves compose; denied storage retains the session value without changing gameplay',()=>{
 const original=Object.getOwnPropertyDescriptor(globalThis,'localStorage');let saved='';Object.defineProperty(globalThis,'localStorage',{configurable:true,value:{getItem:()=>saved||null,setItem:(_k:string,v:string)=>{saved=v;}}});
 const st=createCampaign(),before=stateHash(st);try{preferenceView.value=parseUiPreferences(null);assert.ok(saveUiPreferences({scale:125,motion:'reduce',bindings:{inspect:'o'}}));assert.ok(saveQuickbar([...DEFAULT_QUICKBAR].reverse()));const p=parseUiPreferences(saved);assert.equal(p.scale,125);assert.equal(p.motion,'reduce');assert.equal(p.bindings.inspect,'o');assert.deepEqual(p.quickbar,[...DEFAULT_QUICKBAR].reverse());
 Object.defineProperty(globalThis,'localStorage',{configurable:true,get:()=>{throw Error('denied');}});assert.equal(saveUiPreferences({scale:150}),false);assert.equal(preferenceView.value.scale,150);assert.equal(stateHash(st),before);assert.equal(stateHash(loadState(makeSave(st))),before);
 }finally{preferenceView.value=parseUiPreferences(null);if(original)Object.defineProperty(globalThis,'localStorage',original);else Reflect.deleteProperty(globalThis,'localStorage');}
});
