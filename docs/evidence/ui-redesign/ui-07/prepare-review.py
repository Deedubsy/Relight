from pathlib import Path
out=Path(__file__).resolve().parent;prior=out.parent/'ui-06'
for name in ['building','inspection','projects','regression','matrix','preferences']:
 s=(prior/(name+'.cjs')).read_text(encoding='utf-8')
 if name in ['building','inspection','projects']:s=s.replace('for(const width of [1366,900])','for(const width of [1280])')
 (out/(name+'.cjs')).write_text(s,encoding='utf-8',newline='\n')
s=(out/'review.cjs').read_text(encoding='utf-8').replace('before-','after-').replace("await page.waitForTimeout(400);rows.push", "await page.waitForTimeout(550);rows.push")
(out/'final-review.cjs').write_text(s,encoding='utf-8',newline='\n')
