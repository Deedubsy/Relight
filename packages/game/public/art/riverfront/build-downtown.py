"""Original ground-floor / roof layers for the six city-centre archetypes.
All windows are unlit; runtime finite-power lighting supplies the illumination.
"""
from pathlib import Path
out=Path(__file__).resolve().parent
for kind in ['townhall','apartment','office','department','arcade','parking']:
 for variant,col in enumerate(['#746854','#526767','#697061']):
  s=['<svg xmlns="http://www.w3.org/2000/svg" width="640" height="560" viewBox="0 0 640 560">',f'<defs><pattern id="roof" width="28" height="20" patternUnits="userSpaceOnUse"><rect width="28" height="20" fill="{col}"/><path d="M0 19H28M14 0V19" stroke="#243536" opacity=".5"/></pattern><linearGradient id="glass"><stop stop-color="#95aaa4"/><stop offset=".45" stop-color="#506d72"/><stop offset="1" stop-color="#243d45"/></linearGradient></defs>', '<path d="M14 14H614L634 36V548H34L14 526Z" fill="#142d30" opacity=".6"/>','<rect x="10" y="12" width="612" height="526" rx="3" fill="#9b947d" stroke="#263b3d" stroke-width="8"/>','<rect x="20" y="26" width="592" height="390" fill="url(#roof)" stroke="#c0b596" stroke-width="6"/>']
  # Three restrained facade tiers read as height without extending beyond the parcel.
  for y in [425,461,497]:
   s += [f'<rect x="20" y="{y}" width="592" height="33" fill="{col}"/><path d="M20 {y}H612" stroke="#c5b794" stroke-width="5"/>']
   for x in range(36,608,42):s += [f'<rect x="{x}" y="{y+7}" width="23" height="19" fill="#263e43" stroke="#b3aa8d" stroke-width="3"/><path d="M{x+11} {y+7}v19" stroke="#71827a" stroke-width="2"/>']
  if kind=='townhall':
   s += ['<path d="M35 414L320 345L600 414Z" fill="#c0b89c" stroke="#435951" stroke-width="6"/>','<ellipse cx="320" cy="210" rx="131" ry="138" fill="#527b72" stroke="#b4b99c" stroke-width="7"/>','<path d="M320 72Q218 210 320 348M320 72Q422 210 320 348M189 210H451" fill="none" stroke="#9aaa90" stroke-width="6"/>','<ellipse cx="320" cy="83" rx="20" ry="12" fill="#b6b997"/>']
   for x in range(70,580,62):s += [f'<rect x="{x}" y="422" width="18" height="110" fill="#d5c8a5" stroke="#746f5e" stroke-width="3"/>']
  elif kind=='arcade':
   s += ['<path d="M170 398V55Q320 -18 470 55V398Z" fill="url(#glass)" stroke="#c2b69a" stroke-width="8"/>','<path d="M320 21V398M190 43V398M450 43V398" stroke="#2a3c40" stroke-width="5"/>']
   for y in range(60,400,34):s += [f'<path d="M170 {y}Q320 {y-33}470 {y}" fill="none" stroke="#a5b4a6" stroke-width="5"/>']
   s += ['<path d="M230 535V460Q320 388 410 460V535Z" fill="#273f42" stroke="#c1b89c" stroke-width="7"/>']
  elif kind=='parking':
   s += ['<rect x="30" y="38" width="570" height="365" fill="#929587"/>','<path d="M50 374H565V63H90V327H500" fill="none" stroke="#b9baa5" stroke-width="8"/>','<path d="M405 390L584 208V350L545 390Z" fill="#3d5050" stroke="#bac0a7" stroke-width="5"/>']
   for x in range(72,390,44):
    for y in [75,295]:s += [f'<path d="M{x} {y}v63h32v-63" fill="none" stroke="#c9c6a7" stroke-width="3"/>']
   for x,y in [(91,91),(225,311),(315,93)]:s += [f'<rect x="{x}" y="{y}" width="22" height="43" rx="5" fill="#455556"/><rect x="{x+3}" y="{y+12}" width="16" height="19" fill="#8c9d92"/>']
  elif kind=='department':
   s += ['<path d="M35 405V80L90 38H545L600 80V405Z" fill="url(#roof)" stroke="#b7ac8f" stroke-width="7"/>','<ellipse cx="530" cy="112" rx="60" ry="63" fill="#608578" stroke="#aeb698" stroke-width="5"/>','<rect x="50" y="392" width="505" height="34" fill="#344c48" stroke="#acac87" stroke-width="3"/>','<text x="305" y="416" text-anchor="middle" font-family="serif" font-size="22" fill="#cab990">HARPER &amp; CO</text>','<path d="M20 495H612L620 533H12Z" fill="#a5926e" stroke="#334442" stroke-width="4"/>']
   for x in range(28,608,38):s += [f'<path d="M{x} 496h17l4 35h-25Z" fill="#695f4b"/>']
  else:
   for x in [64,376]:
    s += [f'<rect x="{x}" y="66" width="184" height="256" fill="#354a49" stroke="#b2aa8c" stroke-width="6"/>',f'<rect x="{x+15}" y="80" width="154" height="228" fill="{col}"/>']
    if kind=='office':
     for xx in range(x+18,x+160,32):s += [f'<rect x="{xx}" y="105" width="22" height="153" fill="url(#glass)" stroke="#223d42" stroke-width="3"/>']
    else:
     for yy in [103,185,267]:s += [f'<rect x="{x+28}" y="{yy}" width="55" height="27" fill="#a0a394" stroke="#394e4d" stroke-width="5"/><path d="M{x+34} {yy+7}h43m-43 7h43" stroke="#526766" stroke-width="3"/>']
   s += ['<rect x="270" y="105" width="54" height="111" fill="#273d3f" stroke="#b4b196" stroke-width="4"/>']
  if kind not in ['parking','arcade','townhall']:
   for x,y in [(52,48),(542,350)]:s += [f'<rect x="{x}" y="{y}" width="35" height="30" fill="#344b4c" stroke="#abae98" stroke-width="4"/><path d="M{x+6} {y+8}h23m-23 7h23m-23 7h23" stroke="#18333a" stroke-width="3"/>']
  for i in range(26):
   x=28+(i*87)%566;y=36+(i*63)%353;s += [f'<path d="M{x} {y}l12 4m-8 3l14 2" stroke="#1d3b32" opacity=".2" stroke-width="4"/>']
  s += ['<path d="M14 417H619M617 36V534" fill="none" stroke="#293d3c" stroke-width="7"/>','</svg>'];(out/f'{kind}-roof-{variant}.svg').write_text(''.join(s),encoding='utf-8')
 (out/f'{kind}-floor.svg').write_text('<svg xmlns="http://www.w3.org/2000/svg" width="640" height="560"><defs><pattern id="p" width="40" height="40" patternUnits="userSpaceOnUse"><rect width="40" height="40" fill="#797c6b" stroke="#536159" stroke-width="2"/></pattern></defs><rect width="640" height="560" fill="url(#p)"/></svg>',encoding='utf-8')
print('24 downtown SVG layers authored.')
