from pathlib import Path
out=Path('packages/game/public/art/riverfront');out.mkdir(parents=True,exist_ok=True)
# Original scalable top-down sprites; all share north-west highlights and south-east shadows.
for kind in ['house','terrace','garage','workshop','warehouse','shop','civic','utility']:
 for variant in range(3):
  colors=['#795e47','#586968','#626c4b'];roof=colors[variant];s=['<svg xmlns="http://www.w3.org/2000/svg" width="320" height="256" viewBox="0 0 320 256">',f'<defs><pattern id="tile" width="20" height="12" patternUnits="userSpaceOnUse"><rect width="20" height="12" fill="{roof}"/><path d="M0 11H20M10 0V11" stroke="#182b2c" opacity=".38"/><path d="M0 1H20" stroke="#ded1ac" opacity=".18"/></pattern><pattern id="metal" width="16" height="16" patternUnits="userSpaceOnUse"><rect width="16" height="16" fill="{roof}"/><path d="M1 0V16M5 0V16" stroke="#b1b5a0" opacity=".26"/></pattern><linearGradient id="shade"><stop stop-color="#e0ce9b" stop-opacity=".18"/><stop offset="1" stop-color="#071d23" stop-opacity=".4"/></linearGradient></defs>','<path d="M13 14H305L318 35V248H26L13 231Z" fill="#0b191c" opacity=".5"/>','<rect x="8" y="8" width="304" height="229" rx="2" fill="#958f77" stroke="#172c2b" stroke-width="5"/>','<rect x="12" y="201" width="296" height="32" fill="#817a61"/>']
  if kind in ['house','terrace']:
   n=3 if kind=='terrace'else 1
   for i in range(n):
    x=12+i*296/n;w=296/n;s += [f'<path d="M{x} 201V25L{x+w/2} 8L{x+w} 25V201Z" fill="url(#tile)" stroke="#273331" stroke-width="3"/>',f'<path d="M{x+w/2} 13L{x+w} 28V197L{x+w/2} 170Z" fill="#071e25" opacity=".28"/>',f'<path d="M{x} 111H{x+w}" stroke="#c1ae82" stroke-width="3"/>',f'<rect x="{x+w*.66}" y="35" width="19" height="40" fill="#414443" stroke="#202c2a" stroke-width="3"/><rect x="{x+w*.66-3}" y="32" width="25" height="10" fill="#b4a48a"/><rect x="{x+w*.66+4}" y="34" width="12" height="5" fill="#172629"/>']
  elif kind=='civic':
   s+=['<rect x="20" y="20" width="280" height="180" fill="url(#tile)"/>','<path d="M19 192L160 151L301 192Z" fill="#c8bfa1" stroke="#475951" stroke-width="4"/>','<ellipse cx="160" cy="100" rx="73" ry="67" fill="#58877e" stroke="#293e3d" stroke-width="5"/>','<path d="M160 33Q120 100 160 168M160 33Q200 100 160 168M87 100H233" fill="none" stroke="#9ebaa0" stroke-width="3"/>','<ellipse cx="140" cy="76" rx="26" ry="30" fill="#b7c3a8" opacity=".2"/>']
   for x in [35,70,105,215,250,285]:s+=[f'<rect x="{x}" y="201" width="12" height="32" fill="#d6c7a3" stroke="#776f5a" stroke-width="2"/>']
  else:
   s+=['<rect x="13" y="14" width="294" height="183" fill="url(#metal)" stroke="#283d3d" stroke-width="3"/>']
   if kind in ['warehouse','workshop']: 
    for x in [44,139,234]:s +=[f'<path d="M{x} 32H{x+43}L{x+55} 58V160H{x}Z" fill="#253d43" stroke="#aec4be" stroke-width="2"/>',f'<rect x="{x+3}" y="36" width="36" height="110" fill="#6d9291" opacity=".7"/>',f'<path d="M{x} 73H{x+50}M{x} 112H{x+50}" stroke="#1c363a" stroke-width="3"/>']
   if kind=='garage':s+=['<rect x="40" y="193" width="240" height="41" fill="#52656a" stroke="#293c3d" stroke-width="4"/>']
   if kind=='utility':
    for x in [94,219]:s +=[f'<path d="M{x-41} 73V152Q{x} 179 {x+41} 152V73" fill="#8c9e97" stroke="#283f42" stroke-width="5"/>',f'<ellipse cx="{x}" cy="74" rx="42" ry="24" fill="#b7beb0" stroke="#3b5152" stroke-width="5"/>',f'<ellipse cx="{x}" cy="74" rx="27" ry="14" fill="#30464b"/>',f'<path d="M{x} 175V189H160V199" fill="none" stroke="#b2ab85" stroke-width="9"/>']
   for x,y in [(32,29),(270,160)]:s +=[f'<rect x="{x}" y="{y}" width="22" height="19" fill="#354b4c" stroke="#a3ac98" stroke-width="2"/><path d="M{x+4} {y+5}h14m-14 4h14m-14 4h14" stroke="#172d32" stroke-width="2"/>']
  if kind not in ['civic','garage']:
   for x in [25,60,235,270]:s +=[f'<rect x="{x}" y="209" width="23" height="15" fill="#1c393c" stroke="#b3a689" stroke-width="3"/><path d="M{x+11} 210V224M{x} 218H{x+23}" stroke="#5b7270" stroke-width="2"/>']
  if kind=='shop':
   s+=['<rect x="67" y="188" width="186" height="20" fill="#c4b998" stroke="#374a47" stroke-width="2"/>','<path d="M65 208H255L267 230H53Z" fill="#b9a77e" stroke="#37413c" stroke-width="2"/>']
   for x in range(60,252,24):s +=[f'<path d="M{x+6} 208h12l4 22h-20Z" fill="#865044"/>']
  # Gutter, a few weather streaks and chipped edges are consistent across the set.
  s+=['<path d="M10 201H310M308 27V235" fill="none" stroke="#283f3d" stroke-width="5"/>','<path d="M13 15H303" stroke="#d8c99e" stroke-width="3" opacity=".5"/>']
  for i in range(17):
   x=18+(i*67+variant*23)%281;y=20+(i*41)%166;s+=[f'<path d="M{x} {y}l7 3m-5 1l9 2" stroke="#152d28" opacity=".17" stroke-width="3"/>']
  s+=['</svg>'];(out/f'{kind}-roof-{variant}.svg').write_text(''.join(s))
 # Floor art has no misleading furniture; solid props are separate canonical objects.
 s=['<svg xmlns="http://www.w3.org/2000/svg" width="320" height="256" viewBox="0 0 320 256"><rect width="320" height="256" fill="#7a7764"/>']
 for y in range(0,256,16 if kind in ['house','terrace','shop']else 32):
  s+=[f'<path d="M0 {y}H320" stroke="#434e49" stroke-width="2" opacity=".6"/>']
 for x in range(16,320,64):s+=[f'<path d="M{x} 0V256" stroke="#aba48a" opacity=".16"/>']
 s+=['<path d="M19 38l6 31 21 9-3 36M288 177l-25 13 7 31" fill="none" stroke="#354b43" opacity=".4" stroke-width="3"/></svg>'];(out/f'{kind}-floor.svg').write_text(''.join(s))
print('32 coordinated building SVG layers authored.')

