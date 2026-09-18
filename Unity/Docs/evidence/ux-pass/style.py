from pathlib import Path
b=Path('Unity/Relight/Assets/Relight')
def append(n,t):
 p=b/n;p.write_text(p.read_text(encoding='utf-8-sig')+'\n'+t,encoding='utf-8')
append('UI/Hud/Hud.uss','''/* Persistent tool dock: reserved by every gameplay drawer. */
#action-bar { left: 16px; right: 16px; bottom: 12px; flex-direction: column; align-items: stretch; background-color: var(--ui-surface); border-width: 1px; border-color: var(--ui-border); padding: 8px; }
#dock-header { flex-direction: row; align-items: center; margin-bottom: 6px; }
#current-action { flex-grow: 1; flex-shrink: 1; min-width: 0; font-size: 16px; color: var(--ui-accent); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
#dock-header .ui-button { margin-top: 0; margin-bottom: 0; min-height: 28px; }
#hud-slots { flex-direction: row; justify-content: center; }
.dock-slot { position: relative; flex-grow: 1; flex-shrink: 1; flex-basis: 0; height: 70px; min-width: 0; margin: 0 3px; padding: 3px; background-color: var(--ui-bg); border-width: 1px; border-color: var(--ui-border); border-radius: 4px; }
.dock-slot:hover { background-color: var(--ui-raised); border-color: var(--ui-text); }
.dock-slot.selected { border-width: 2px; border-color: var(--ui-accent); background-color: var(--ui-raised); }
.dock-slot.unavailable #dock-icon { opacity: 0.45; }
.dock-slot.unavailable #dock-count { color: var(--ui-danger); }
#slot-key { position: absolute; top: 2px; left: 5px; font-size: 13px; color: var(--ui-secondary); }
#dock-icon { width: 32px; height: 32px; align-self: center; margin-top: 4px; }
#dock-count { position: absolute; right: 5px; top: 4px; color: var(--ui-text); font-size: 14px; -unity-font-style: bold; }
#dock-name { font-size: 13px; color: var(--ui-text); -unity-text-align: middle-center; white-space: nowrap; text-overflow: ellipsis; overflow: hidden; margin-top: 4px; }
#engineer-block { bottom: 146px; width: 250px; }
#world-target { right: 16px; bottom: 146px; width: 310px; padding: 12px; }
.target-title { font-size: 18px; -unity-font-style: bold; color: var(--ui-text); white-space: normal; }
.target-action { font-size: 16px; color: var(--ui-accent); white-space: normal; margin-top: 5px; }
.target-detail { font-size: 14px; color: var(--ui-secondary); white-space: normal; margin-top: 6px; }
.target-blocked .target-action { color: var(--ui-danger); }
.target-active .target-action { color: var(--ui-working); }
.target-outline { position: absolute; border-width: 2px; border-color: var(--ui-accent); background-color: rgba(239,191,97,0.08); }
.target-outline.target-blocked { border-color: var(--ui-danger); }
.target-outline.target-active { border-color: var(--ui-working); border-width: 3px; }
#mining { bottom: 310px; width: 250px; }
#problems { bottom: 310px; width: 310px; }
#placement-toolbar { bottom: 150px; left: 280px; right: 340px; }
#placement-label, #placement-problem { white-space: normal; }
#hand-lock { bottom: 300px; left: 280px; right: 340px; }
#hand-lock-text { white-space: normal; }
.drawer-open #world-target, .drawer-open #target-outline, .drawer-open #goal-card, .drawer-open #problems, .drawer-open #placement-toolbar, .drawer-open #engineer-block { display: none; }
.compact-ui #dock-name { font-size: 11px; }
.compact-ui #dock-header .ui-button { padding-left: 7px; padding-right: 7px; font-size: 13px; }
.compact-ui #current-action { font-size: 13px; }
.compact-ui #engineer-block { width: 210px; }
.compact-ui #world-target { width: 270px; }
.compact-ui #placement-toolbar { left: 235px; right: 290px; }
.compact-ui #status-content { max-width: 94%; }
.compact-ui #core-block { width: 210px; }
''')
append('UI/Inventory/InventoryPanel.uss','''#inventory-panel { top: 80px; bottom: 144px; max-width: 96%; }
#quickbar, #quickbar-heading { display: none; }
.machine-progress .unity-progress-bar__title { color: var(--ui-text); }
''')
append('UI/Build/BuildPanel.uss','''#build-panel { top: 80px; bottom: 144px; }
''')
append('UI/Guide/OpeningHint.uss','''#opening-hint { top: 76px; bottom: auto; left: auto; right: 16px; width: 330px; max-width: 34%; }
''')
# Tooltip lifetime follows ancestor visibility and drag; visible values refresh from their provider.
p=b/'UI/Tooltips/TooltipController.cs';s=p.read_text(encoding='utf-8-sig');start=s.index('        private void Update()');end=s.index('        private void Reveal()',start);s=s[:start]+'''        private float _refreshAt;
        private static bool OnScreen(VisualElement e)
        {
            if (e == null || e.panel == null) return false;
            for(var p=e;p!=null;p=p.parent)
                if(p.resolvedStyle.display==DisplayStyle.None || p.resolvedStyle.visibility==Visibility.Hidden) return false;
            return e.worldBound.width>0 && e.worldBound.height>0;
        }
        private void Update()
        {
            if(UiDrag.Dragging || (_target!=null && !OnScreen(_target))) { HideNow(); return; }
            if(_shownFor==null || Time.unscaledTime<_refreshAt)return;
            _refreshAt=Time.unscaledTime+.15f;
            Reveal();
        }

'''+s[end:];s=s.replace('if (_tip == null || target == null || target.panel == null) return;', 'if (_tip == null || !OnScreen(target) || UiDrag.Dragging) return;');p.write_text(s,encoding='utf-8')
# Workshop cards: recognisable output and ingredients; no anonymous x/y chips.
p=b/'UI/Workshop/RecipeCard.uxml';s=p.read_text(encoding='utf-8-sig');s=s.replace('<ui:Label name="card-title"', '<ui:VisualElement name="card-output-icon" class="item-icon" picking-mode="Ignore"><ui:Label name="card-output-icon-code" class="item-icon-code" picking-mode="Ignore" /></ui:VisualElement>\n        <ui:Label name="card-title"');p.write_text(s,encoding='utf-8')
p=b/'UI/Workshop/WorkshopPanelController.cs';s=p.read_text(encoding='utf-8-sig');s=s.replace('var chip = new Label(WorkshopText.Chip(have, need));','var chip = new Label(Items.Key(inputs[i].item) + "  " + WorkshopText.Chip(have, need));');s=s.replace('private void Wire(Card c)\n        {','''private void Wire(Card c)
        {
            var iconKey = c.Recipe == null ? "core1" : c.Recipe.Key == RifleKey ? "rifle" : c.Recipe.Outputs.Count > 0 ? Items.Key(c.Recipe.Outputs[0].Item) : "";
            ItemIcons.Paint(c.Root.Q("card-output-icon"), c.Root.Q<Label>("card-output-icon-code"), iconKey);''');p.write_text(s,encoding='utf-8')
append('UI/Workshop/WorkshopPanel.uss','''#card-output-icon { width: 36px; height: 36px; margin-bottom: 6px; }
.card-chips { flex-wrap: wrap; }
.card-chip { white-space: normal; }
''')
# Hints stop once skills are demonstrated; current action paths, not non-existent Interact/Build paths.
p=b/'UI/Guide/OpeningHintController.cs';s=p.read_text(encoding='utf-8-sig');s=s.replace('_dismissed || covered','_dismissed || covered || Demonstrated()');s=s.replace('Key("World/Interact", "E")','Key("World/Equip", "E")').replace('Key("World/Build", "B")','Key("Global/ToggleBuild", "B")');p.write_text(s,encoding='utf-8')
# Show unavailable audio controls as unavailable, rather than accepting settings that drive no mixer.
p=b/'UI/Settings/SettingsController.cs';s=p.read_text(encoding='utf-8-sig');anchor='Label(_root, "heading-audio", FrontEndText.SectionAudio);';s=s.replace(anchor,anchor+'''
            if(mixer==null)
            {
                Label(_root,"heading-audio","Audio · unavailable in this build");
                foreach(var control in new VisualElement[]{_master,_ui,_world,_alerts,_mute}) control?.SetEnabled(false);
            }''');p.write_text(s,encoding='utf-8')
# Expose compact objectives, with details and live material guidance in the foldout.
p=b/'UI/Guide/GoalCard.uxml';s=p.read_text(encoding='utf-8-sig');a=s.index('        <ui:VisualElement name="goal-materials"');z=s.index('        <ui:Foldout name="goal-why"',a);materials=s[a:z];s=s[:a]+s[z:];s=s.replace('text="Why this next?"','text="Materials &amp; next steps"');s=s.replace('<ui:Label name="goal-detail"',materials+'            <ui:Label name="goal-detail"');p.write_text(s,encoding='utf-8')
# Preserve visible missing-material instruction on the compact card; full rows stay available on demand.
p=b/'UI/Guide/GoalCardViewModel.cs';s=p.read_text(encoding='utf-8-sig');anchor='Detail = next.Detail ?? "";';s=s.replace(anchor,anchor+'''
            foreach(var material in next.Materials)
                if(material.Available < material.Required && !string.IsNullOrEmpty(material.Source))
                {
                    Text = "Need " + (material.Required-material.Available) + " " + material.DisplayName + ". " + material.Source + ".";
                    break;
                }''');p.write_text(s,encoding='utf-8')
