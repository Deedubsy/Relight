"""Component-authored facings; roof highlights stay northwest (no sprite rotation)."""
from pathlib import Path
out=Path(__file__).parent
for facing in ['N','E','W']:
 for variant,color in enumerate(['#795e47','#586968','#626c4b']):
  s=['<svg xmlns="http://www.w3.org/2000/svg" width="320" height="256" viewBox="0 0 320 256">',f'<defs><pattern id="tile" width="20" height="12" patternUnits="userSpaceOnUse"><rect width="20" height="12" fill="{color}"/><path d="M0 11H20M10 0V11" stroke="#182b2c" opacity=".38"/><path d="M0 1H20" stroke="#ded1ac" opacity=".18"/></pattern></defs>','<rect x="6" y="7" width="308" height="243" fill="#958f77" stroke="#283c36" stroke-width="5"/>']
  # Inset roof leaves the actual entry wall visible. Hip, ridge and cross-gable variants.
  x,y,w,h={'N':(14,37,292,201),'E':(14,15,264,223),'W':(42,15,264,223)}[facing]
  if variant==0:
   s += [f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="url(#tile)" stroke="#293c38" stroke-width="4"/>',f'<path d="M{x} {y}L{x+w*.5} {y+25}L{x+w} {y}M{x+w*.5} {y+25}V{y+h-25}L{x} {y+h}M{x+w*.5} {y+h-25}L{x+w} {y+h}" stroke="#b4a486" stroke-width="3" fill="none"/>',f'<path d="M{x+w*.5} {y+25}L{x+w} {y}V{y+h}L{x+w*.5} {y+h-25}Z" fill="#10262b" opacity=".3"/>']
  else:
   s += [f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="url(#tile)" stroke="#293c38" stroke-width="4"/>',f'<path d="M{x} {y+h*.5}H{x+w}V{y+h}H{x}Z" fill="#10262b" opacity=".3"/>',f'<path d="M{x} {y+h*.5}H{x+w}" stroke="#c6b48b" stroke-width="4"/>']
   if variant==2:s += [f'<path d="M{x+40} {y+h}V{y+h*.4}L{x+85} {y+h*.25}L{x+130} {y+h*.4}V{y+h}Z" fill="url(#tile)" stroke="#b4a486" stroke-width="3"/>']
  s += [f'<path d="M{x} {y}H{x+w}" stroke="#d4c49a" stroke-width="3"/>',f'<rect x="{x+45}" y="{y+28}" width="18" height="35" fill="#48504a"/><rect x="{x+41}" y="{y+25}" width="26" height="9" fill="#beb294"/>']
  # Windows flank (never occupy) the renderer's canonical centre doorway.
  for a in [42,199]:
   wx,wy,ww,wh=(a,13,25,17) if facing=='N' else (286 if facing=='E' else 12,a,19,24)
   s += [f'<rect x="{wx}" y="{wy}" width="{ww}" height="{wh}" fill="#263f40" stroke="#b8ad8d" stroke-width="3"/>']
  s+=['</svg>'];(out/f'house-{facing}-roof-{variant}.svg').write_text(''.join(s),encoding='utf-8')
