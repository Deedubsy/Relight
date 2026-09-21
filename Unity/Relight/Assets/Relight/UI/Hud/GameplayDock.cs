using System;
using Relight.Presentation;
using Relight.Sim;
using Relight.Sim.UI;
using Relight.UI.Settings;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
namespace Relight.UI
{
    /// <summary>Persistent shortcuts and a world target inspector. All counts and progress come from the running sim.</summary>
    public sealed class GameplayDock
    {
        readonly VisualElement root, target, outline;
        readonly SimHost host;
        readonly UiShell shell;
        readonly WorldInput input;
        readonly InputRouter router;
        readonly BuildPanelController build;
        readonly Button[] slots = new Button[10];
        readonly Label title, action, detail, current;
        readonly ProgressBar progress;
        float paintAt;
        public GameplayDock(VisualElement root, SimHost host, UiShell shell, InventoryPanelController inventory)
        {
            this.root=root; this.host=host; this.shell=shell;
            input=UnityEngine.Object.FindAnyObjectByType<WorldInput>();
            router=UnityEngine.Object.FindAnyObjectByType<InputRouter>();
            build=UnityEngine.Object.FindAnyObjectByType<BuildPanelController>();
            target=root.Q("world-target"); outline=root.Q("target-outline");
            title=root.Q<Label>("target-title"); action=root.Q<Label>("target-action");
            detail=root.Q<Label>("target-detail"); current=root.Q<Label>("current-action");
            progress=root.Q<ProgressBar>("target-progress");
            var row=root.Q("hud-slots");
            if(row==null)return;
            row.Clear();
            for(int i=0;i<10;i++)
            {
                int n=i;
                var button=new Button { name="hud-slot-"+i }; button.AddToClassList("dock-slot");
                button.Add(new Label((i==9?0:i+1).ToString()){name="slot-key",pickingMode=PickingMode.Ignore});
                button.Add(ItemIcons.Make("dock-icon",out _));
                button.Add(new Label {name="dock-count",pickingMode=PickingMode.Ignore});
                button.Add(new Label {name="dock-name",pickingMode=PickingMode.Ignore});
                button.Add(new VisualElement { name="selection-rim", pickingMode=PickingMode.Ignore });
                button.clicked+=()=>{if(!UiDrag.ClickSuppressed && build?.TryAssignHudSlot(n)!=true)input?.SelectBarSlot(n);};
                button.RegisterCallback<PointerDownEvent>(e=>{
                    if(e.button==1 && host?.Simulation!=null){host.Submit(new AssignBarCommand(n,"")); e.StopPropagation();}
                });
                row.Add(button); slots[i]=button;
                inventory?.RegisterHudSlot(button,i);
                Tooltips.Attach(button,()=>SlotTip(n));
            }
            var hands=root.Q<Button>("clear-tool"); if(hands!=null)hands.clicked+=()=>input?.ClearHand();
            Tooltips.Attach(hands,()=>new TooltipContent{Title="Empty hands",Body="Put away the selected tool. Hold the primary action on visible salvage to collect resources into Backpack."});
            Tooltips.Attach(root.Q("open-inventory"),()=>new TooltipContent{Title="Backpack",Body="Carried items and equipment. Drag an owned weapon to the dock to assign its shortcut. The Home workshop opens when you interact with it at Home.",Footer="Toggle: "+Key("Global/TogglePanel","Tab")});
            Tooltips.Attach(root.Q("open-build"),()=>new TooltipContent{Title="Construction",Body="Browse structures, inspect material costs and select a placement tool. Use Add to action bar to assign a shortcut.",Footer="Toggle: "+Key("Global/ToggleBuild","B")});
        }
        string Key(string path,string fallback)
        {
            var a=router?.Actions?.FindAction(path,false);
            return a==null?fallback:a.GetBindingDisplayString();
        }
        string SlotKey(int n)
        {
            var a=router?.Slot;
            return a!=null && n<a.bindings.Count?a.GetBindingDisplayString(n):(n==9?0:n+1).ToString();
        }
        public void Paint()
        {
            var sim=host?.Simulation; if(sim==null || slots[0]==null)return;
            root.EnableInClassList("compact-ui",root.resolvedStyle.width<1100 || root.resolvedStyle.height<800);
            bool panel=!string.IsNullOrEmpty(shell?.Active);
            root.EnableInClassList("drawer-open",panel);
            PaintTarget(sim,panel);
            if(Time.unscaledTime<paintAt)return;
            paintAt=Time.unscaledTime+.1f;
            var bar=WeaponQueries.Bar(sim.State);
            for(int i=0;i<10;i++)
            {
                var key=bar[i]; var b=slots[i]; string name="Empty",count="—"; bool available=false;
                if(sim.Context.Data.TryMachine(key,out var spec))
                {
                    name=spec.DisplayName;
                    var view=BuildCatalogue.Card(sim.Context.Data,spec,sim.State.Engineer.Inv);
                    available=view.Affordable;
                    int builds=int.MaxValue;
                    foreach(var c in view.Cost) if(c.Required>0) builds=Math.Min(builds,c.Available/c.Required);
                    if(builds==int.MaxValue)builds=0;
                    count=(view.Carried+builds).ToString();
                }
                else if(key.Length>0)
                {
                    foreach(var w in WeaponQueries.Owned(sim.State))if(w.Id==key || w.Kind==key)
                    { name=sim.Context.Data.TryWeapon(w.Kind,out var ws)?ws.DisplayName:w.Kind; count=PackLayout.Num(w.Loaded); available=true; break; }
                    if(!available){name="Unowned";count="0";}
                }
                ItemIcons.Paint(b.Q("dock-icon"),b.Q<Label>("dock-icon-code"),key);
                b.Q<Label>("slot-key").text=SlotKey(i);
                b.Q<Label>("dock-name").text=name; b.Q<Label>("dock-count").text=count;
                b.EnableInClassList("selected",input!=null && input.Tool.Length>0 && input.Tool==key);
                b.EnableInClassList("unavailable",!available);
            }
            string tool=input?.Tool??"";
            string held=tool.Length==0?"Hands · Hold "+Key("World/Click","LMB")+" on salvage to mine":tool;
            if(input?.PlacementActive==true && sim.Context.Data.TryMachine(tool,out var chosen))held="Place "+chosen.DisplayName;
            else if(tool.Length>0)
            {
                var w=WeaponQueries.Equipped(sim.State,sim.Context.Data);
                held=w.Equipped?w.DisplayName+" · "+PackLayout.Num(w.Loaded)+" / "+PackLayout.Num(w.Capacity)+" · "+(w.Reloading?"Reloading":w.Loaded<1?"Empty · "+Key("Global/Reload","R")+" reload":"Hold "+Key("World/Click","LMB")+" to fire")
                    :"Weapon not equipped · open Backpack to equip";
            }
            if(current!=null)current.text=build?.Assigning==true?"Assign "+build.Selected+" · click a dock slot":held;
            root.Q<Button>("open-inventory").text="Inventory ["+Key("Global/TogglePanel","Tab")+"]";
            root.Q<Button>("open-build").text="Build ["+Key("Global/ToggleBuild","B")+"]";
        }
        TooltipContent SlotTip(int n)
        {
            var sim=host?.Simulation;if(sim==null)return null;
            var key=WeaponQueries.Bar(sim.State)[n];
            if(key.Length==0)return new TooltipContent{Title="Empty shortcut ["+SlotKey(n)+"]",Body="Assign a structure from Build → Add to action bar, or drag an owned weapon here from Backpack. Shortcuts consume no inventory space."};
            if(sim.Context.Data.TryMachine(key,out var spec))
            {
                var v=BuildCatalogue.Card(sim.Context.Data,spec,sim.State.Engineer.Inv);
                return new TooltipContent{Title=v.DisplayName+" ["+SlotKey(n)+"]",Body=v.Facts+"\n"+v.Availability,Footer=v.CostText+"\nCount = packed structures + builds affordable from Backpack. Right-click clears this shortcut."};
            }
            return new TooltipContent{Title=slots[n].Q<Label>("dock-name").text+" ["+SlotKey(n)+"]",Body="Select this owned weapon. Equipment and loaded bullets stay with the weapon instance.",Footer="Use Backpack to equip. Right-click clears this shortcut."};
        }
        void PaintTarget(Simulation sim,bool panel)
        {
            Show(target,false); Show(outline,false);
            if(panel || input==null || input.PlacementActive || !input.TryHoverTile(out int x,out int y))return;
            var st=sim.State;var ctx=sim.Context;int w=1,h=1;string name="",verb="",info="";bool active=false,blocked=false;
            var machine=ProductionRules.MachineAt(st,x,y);
            if(machine!=null)
            {
                x=machine.X;y=machine.Y;(w,h)=machine.Dimensions;
                if(!WorldTargetQueries.Visible(ctx,st,x,y,w,h))return;
                name=ctx.Data.TryMachine(machine.Kind,out var ms)?ms.DisplayName:machine.Kind;
                var status=ProductionQueries.Status(ctx,st,machine.Id);
                blocked=!Interaction.InReach(ctx,st,machine);
                verb=blocked?"Walk closer to interact":"["+Key("World/Equip","E")+"] Open / configure";
                info=ProductionQueries.Description(ctx,st,machine.Id);
                if(TurretHopper.IsTurret(ctx.Data,machine))info+=" · "+machine.Rounds+" rounds";
            }
            else if(DeathCache.OnTile(st,x,y) is DropCache pile)
            {
                // INT-01: the cargo dropped at a death. The marker draws over the darkness, so the card does too.
                name="Dropped cargo";
                blocked=!DeathCache.InReach(ctx,st,pile);
                verb=blocked?"Walk closer to collect":"["+Key("World/Equip","E")+"] Collect";
                info=DeathCache.Summary(ctx.Data,pile)+"\nWhat fits returns to Backpack. The rest stays here.";
            }
            else if(WorldTargetQueries.Resource(ctx,st,x,y,out var item,out var units))
            {
                name=ctx.Data.Item(item).DisplayName+" salvage";
                var p=MiningQueries.Progress(ctx,st);active=p.Active && p.X==x && p.Y==y;
                var problem=Mining.Problem(ctx,st,x,y);blocked=problem.Length>0 || input.Tool.Length>0;
                verb=active?"Mining · release to stop":input.Tool.Length>0?"Select Hands to mine":problem.Length>0?problem:"Hold ["+Key("World/Click","LMB")+"] to mine";
                info=PackLayout.Num(units)+" left here · +1 per cycle\nBackpack: "+PackLayout.Num(st.Engineer.Inv[item])+" · "+PackLayout.Num(ctx.Data.Engineer.HandMinePerS)+" / sec";
                if(progress!=null)progress.value=(float)p.Fraction;
            }
            else
            {
                var (hx,hy,hw,hh)=HomeQueries.CoreRect(st);
                if(hw>0 && x>=hx && y>=hy && x<hx+hw && y<hy+hh)
                {
                    x=hx;y=hy;w=hw;h=hh;
                    if(!WorldTargetQueries.Visible(ctx,st,x,y,w,h))return;
                    name="Home core";verb=HandCraft.NearDepot(ctx,st)?"["+Key("World/Equip","E")+"] Home workshop":"Approach the Home entrance";
                    info="Workshop uses your Backpack. Protect the core.";
                }
                else if(WorldTargetQueries.Visible(ctx,st,x,y))
                {
                    if(ctx.Geometry.Solid(x,y)){name="Building exterior";verb="Find an entrance";info="This footprint blocks movement and placement.";blocked=true;}
                    else if(Ground.TileAt(ctx,st,x,y)==TileClass.River){name="Water";verb="Cannot walk or build here";blocked=true;}
                    else return;
                }
                else return;
            }
            title.text=name; action.text=verb;detail.text=info;
            target.EnableInClassList("target-blocked",blocked);target.EnableInClassList("target-active",active);
            Show(progress,active);Show(target,true);
            var camera=Camera.main;if(camera==null || root.panel==null)return;
            var a=camera.WorldToScreenPoint(new Vector3(x,-y,0));var z=camera.WorldToScreenPoint(new Vector3(x+w,-y-h,0));
            var tl=RuntimePanelUtils.ScreenToPanel(root.panel,new Vector2(a.x,Screen.height-a.y));
            var br=RuntimePanelUtils.ScreenToPanel(root.panel,new Vector2(z.x,Screen.height-z.y));
            outline.style.left=tl.x;outline.style.top=tl.y;outline.style.width=br.x-tl.x;outline.style.height=br.y-tl.y;
            var bounds=root.worldBound;
            float labelWidth=target.resolvedStyle.width;
            float labelHeight=target.resolvedStyle.height;
            if(float.IsNaN(labelWidth) || labelWidth<1)labelWidth=260;
            if(float.IsNaN(labelHeight) || labelHeight<1)labelHeight=130;
            float left=br.x+12;
            if(left+labelWidth>bounds.width-12)left=tl.x-labelWidth-12;
            float top=tl.y-8;
            target.style.left=Mathf.Clamp(left,12,Mathf.Max(12,bounds.width-labelWidth-12));
            target.style.top=Mathf.Clamp(top,68,Mathf.Max(68,bounds.height-labelHeight-128));
            outline.EnableInClassList("target-active",active);outline.EnableInClassList("target-blocked",blocked);Show(outline,true);
        }
        static void Show(VisualElement e,bool on){e?.EnableInClassList("is-hidden",!on);}
    }
}
