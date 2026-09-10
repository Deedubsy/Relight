from pathlib import Path
out=Path(__file__).resolve().parent
s=(out.parent/'ui-05/browser.cjs').read_text(encoding='utf-8')
s=s.replace('path.join(__dirname,`${name}.json`)','path.join(__dirname,`../ui-05/${name}.json`)')
s=s.replace('path:path.join(__dirname,','path:path.join(__dirname,\'projects-\'+').replace("path.join(__dirname,'browser-result.json')","path.join(__dirname,'projects-result.json')")
(out/'projects.cjs').write_text(s,encoding='utf-8',newline='\n')
s=(out.parent/'ui-03/browser.cjs').read_text(encoding='utf-8')
a=s.index("const steel=page.locator('[data-adapter=")
z=s.index("await page.getByRole('button',{name:'Close panel'",a)
s=s[:a]+"await page.locator('.transfer-card').filter({has:page.getByRole('heading',{name:'steel',exact:true})}).getByRole('button',{name:/^Take /}).click();"+s[z:]
s=s.replace('path:path.join(__dirname,','path:path.join(__dirname,\'building-\'+').replace("path.join(__dirname,'browser-result.json')","path.join(__dirname,'building-result.json')")
(out/'building.cjs').write_text(s,encoding='utf-8',newline='\n')
p=out/'accessibility.cjs';s=p.read_text(encoding='utf-8');s=s.replace("await page.waitForTimeout(400);await page.getByRole('button',{name:'Map (M)',exact:true}).click();","await page.waitForTimeout(400);await page.locator('#map > canvas').focus();await page.keyboard.press('m');")
p.write_text(s,encoding='utf-8',newline='\n')
