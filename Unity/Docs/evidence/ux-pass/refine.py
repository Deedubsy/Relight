from pathlib import Path
b=Path('Unity/Relight/Assets/Relight')
p=b/'UI/GameUI.uxml';s=p.read_text(encoding='utf-8-sig');s=s.replace('<ui:Template name="hud-doc"','<Style src="project://database/Assets/Relight/UI/Hud/Hud.uss" />\n    <ui:Template name="hud-doc"');p.write_text(s,encoding='utf-8')
p=b/'UI/Hud/Hud.uss';s=p.read_text(encoding='utf-8');s+='''
#mining { display: none; }
.hud-meter, .hud-meter .unity-progress-bar__container, .hud-meter .unity-progress-bar__background, .hud-meter .unity-progress-bar__progress { height: 6px; min-height: 6px; max-height: 6px; }
.hud-meter { flex-shrink: 0; margin-top: 8px; margin-bottom: 5px; }
.compact-ui #goal-card { width: 300px; padding: 12px; }
.compact-ui #goal-title { font-size: 18px; }
.compact-ui #goal-text { font-size: 14px; }
.compact-ui #threat { top: 72px; }
#goal-why { max-height: 340px; }
''';p.write_text(s,encoding='utf-8')
p=b/'UI/Inventory/InventoryPanel.uss';s=p.read_text(encoding='utf-8');s+='\n.slot-name { white-space: normal; max-height: 34px; font-size: 13px; }\n';p.write_text(s,encoding='utf-8')
p=b/'UI/Hud/GameplayDock.cs';s=p.read_text(encoding='utf-8');s=s.replace('readonly ProgressBar progress;', 'readonly ProgressBar progress;\n        float paintAt;');s=s.replace('var bar=WeaponQueries.Bar(sim.State);\n            for(int i=0;', 'PaintTarget(sim,panel);\n            if(Time.unscaledTime<paintAt)return;\n            paintAt=Time.unscaledTime+.1f;\n            var bar=WeaponQueries.Bar(sim.State);\n            for(int i=0;');s=s.replace('            PaintTarget(sim,panel);\n        }','        }');s=s.replace('                else return;\n            }\n            title.text=name;', '''                else if(WorldTargetQueries.Visible(ctx,st,x,y))
                {
                    if(ctx.Geometry.Solid(x,y)){name="Building exterior";verb="Find an entrance";info="This footprint blocks movement and placement.";blocked=true;}
                    else if(Ground.TileAt(ctx,st,x,y)==TileClass.River){name="Water";verb="Cannot walk or build here";blocked=true;}
                    else return;
                }
                else return;
            }
            title.text=name;''');p.write_text(s,encoding='utf-8')
p=b/'Sim/UI/Hud/HudViewModel.cs';s=p.read_text(encoding='utf-8-sig');s=s.replace('case MiningStoppedEvent m:\n                        if (string.IsNullOrEmpty(m.Reason)) break;', '''case MinedEvent gained:
                        Notices.Post("mined:"+Items.Key(gained.Item), "+1 "+Items.Key(gained.Item)+" to Backpack", HudNoticeKind.Info, now, NoteSeconds);
                        break;
                    case MiningStoppedEvent m:
                        if (string.IsNullOrEmpty(m.Reason)) { Notices.Post(MiningNoticeKey,"Mining stopped",HudNoticeKind.Info,now,NoteSeconds); break; }''');p.write_text(s,encoding='utf-8')
p=b/'Sim/Campaign/Opening/OpeningQueries.cs';s=p.read_text(encoding='utf-8');a=s.index('public readonly struct MaterialSource');z=s.index('public readonly struct ObjectiveView',a);part=s[a:z];part=part.replace('public bool Found { get; }','public bool Found { get; }\n        public bool HasLocation { get; }\n        public Vec2 Location { get; }');part=part.replace('string shortForm, string text)','string shortForm, string text, Vec2? location = null)');part=part.replace('Found = found;', 'HasLocation = location.HasValue; Location = location ?? Vec2.Zero;\n            Found = found;');s=s[:a]+part+s[z:];s=s.replace('here ? "" : compass, tiles, shortForm, text);','here ? "" : compass, tiles, shortForm, text, target);');s=s.replace('var sentence = "";\n            var changed = false;','var sentence = "";\n            var location = step.Location;\n            var hasLocation = step.HasLocation;\n            var changed = false;');s=s.replace('if (sentence.Length == 0) sentence = m.DisplayName + ": " + src.Text;', 'if (sentence.Length == 0) { sentence = m.DisplayName + ": " + src.Text; if(src.HasLocation){location=src.Location;hasLocation=true;} }');s=s.replace('step.Text, detail, step.HasLocation, step.Location, rows);','step.Text, detail, hasLocation, location, rows);');p.write_text(s,encoding='utf-8')
p=b/'UI/Guide/GoalCardViewModel.cs';s=p.read_text(encoding='utf-8-sig');s=s.replace('+ " " + material.DisplayName + ". " + material.Source', '+ " " + material.DisplayName + " — " + material.Source');p.write_text(s,encoding='utf-8')
p=b/'Sim/Actor/Mining/Mining.cs';s=p.read_text(encoding='utf-8-sig');s=s.replace('if (!TryTile(ctx, st, x, y, out var item, out _)) return EmptyText;', 'if (!TryTile(ctx, st, x, y, out var item, out _)) return EmptyText;\n            if(ctx.Geometry.Solid(x,y) || ProductionRules.MachineAt(st,x,y)!=null) return "Resource blocked by a structure";');p.write_text(s,encoding='utf-8')
# One direct, scroll-aware Workshop entry in the Backpack header.
p=b/'UI/Inventory/InventoryPanel.uxml';s=p.read_text(encoding='utf-8-sig');anchor='<ui:Button name="close-inventory"';s=s.replace(anchor,'<ui:Button name="jump-workshop" text="Workshop" class="ui-button ui-button-secondary" />\n            '+anchor);p.write_text(s,encoding='utf-8')
p=b/'UI/Inventory/InventoryPanelController.cs';s=p.read_text(encoding='utf-8-sig');anchor='BuildQuickbar();';s=s.replace(anchor,anchor+'''
            var workshopButton=_root.Q<Button>("jump-workshop");
            if(workshopButton!=null) workshopButton.clicked+=()=>{
                var foldout=_root.Q<Foldout>("workshop-foldout"); if(foldout==null)return;
                foldout.value=true;
                foldout.schedule.Execute(()=>_root.Q<ScrollView>("inventory-body")?.ScrollTo(foldout)).ExecuteLater(30);
            };''');p.write_text(s,encoding='utf-8')
