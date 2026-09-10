from pathlib import Path
out=Path(__file__).resolve().parent
s=(out.parent/'ui-06/snapshot.py').read_text(encoding='utf-8').replace("prior=out.parent/'ui-05'","prior=out.parent/'ui-06'").replace("fromfile='UI-05/'+n,tofile='UI-06/'+n","fromfile='UI-06/'+n,tofile='UI-07/'+n")
(out/'snapshot.py').write_text(s,encoding='utf-8',newline='\n')
