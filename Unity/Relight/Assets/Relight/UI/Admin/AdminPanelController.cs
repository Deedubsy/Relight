using System;
using System.Collections.Generic;
using Relight.Sim;
using Relight.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Relight.UI
{
    [DisallowMultipleComponent]
    public sealed class AdminPanelController : MonoBehaviour
    {
        private SimHost _host;
        private UiShell _shell;
        private VisualElement _root, _panel;
        private Simulation _session;
        private readonly List<string> _itemKeys=new List<string>(), _weaponKeys=new List<string>(), _enemyKeys=new List<string>();
        private float _paintAt;
        private bool _bound;
        private T Q<T>(string name) where T:VisualElement => _root.Q<T>(name);
        private void Start() { _host=FindAnyObjectByType<SimHost>(); _shell=GetComponent<UiShell>(); Bind(); }
        private void Bind()
        {
            _root=GetComponent<UIDocument>()?.rootVisualElement;_panel=_root?.Q("admin-panel");
            if(_panel==null)return;
            Q<Button>("admin-launcher").clicked+=Toggle;
            Q<Button>("admin-close").clicked+=()=>_shell.CloseActive();
            foreach(var tab in new[]{"supplies","enemies","player","world"}){var page=tab;Q<Button>("admin-tab-"+tab).clicked+=()=>Page(page);}
            Q<TextField>("admin-search").RegisterValueChangedCallback(_=>FilterItems());
            Q<DropdownField>("admin-direction").choices=new List<string>{"North","East","South","West"};Q<DropdownField>("admin-direction").index=0;
            Button("admin-give",()=>new AdminCommand("grant",Selected(_itemKeys,"admin-item"),Q<IntegerField>("admin-amount").value));
            Button("admin-kit",()=>new AdminCommand("materials"));
            Button("admin-give-weapon",()=>new AdminCommand("weapon",Selected(_weaponKeys,"admin-weapon")));
            Button("admin-spawn",()=>new AdminCommand("spawn",Selected(_enemyKeys,"admin-enemy"),Q<IntegerField>("admin-enemy-count").value,Q<DropdownField>("admin-direction").index,Q<IntegerField>("admin-distance").value));
            Button("admin-raid",()=>new AdminCommand("raid",Amount:Q<IntegerField>("admin-enemy-count").value,Direction:Q<DropdownField>("admin-direction").index));
            foreach(var entry in new[]{("clear","clear-enemies"),("heal","heal"),("home","home"),("repair","repair"),("fuel","fuel"),("ammo","ammo"),("reset","reset-overrides")})
            {var action=entry.Item2;Button("admin-"+entry.Item1,()=>new AdminCommand(action));}
            Button("admin-day",()=>new AdminCommand("lighting",Amount:1));Button("admin-night",()=>new AdminCommand("lighting",Amount:0));Button("admin-natural",()=>new AdminCommand("lighting",Amount:-1));
            Q<Toggle>("admin-god").RegisterValueChangedCallback(e=>Run(new AdminCommand("invulnerable",Amount:e.newValue?1:0)));
            Q<Toggle>("admin-freeze").RegisterValueChangedCallback(e=>Run(new AdminCommand("freeze",Amount:e.newValue?1:0)));
            Q<Button>("admin-pause").clicked+=()=>{if(_host?.Simulation==null)return;_host.Paused=!_host.Paused;Say(_host.Paused?"Simulation paused. Admin actions remain available.":"Simulation running at normal speed.",false);Paint();};
            Page("supplies");_bound=true;
        }
        private void Button(string name,Func<AdminCommand> command)=>Q<Button>(name).clicked+=()=>Run(command());
        private string Selected(List<string> keys,string field){var i=Q<DropdownField>(field).index;return i>=0 && i<keys.Count?keys[i]:"";}
        public void Toggle()
        {
            if(_host?.Simulation==null || _shell==null)return;
            if (FindAnyObjectByType<PauseMenu.PauseMenuController>()?.AdminAvailable == false) return;
            FindAnyObjectByType<WorldInput>()?.ClearHand();
            _shell.Toggle("admin-panel");Paint();
        }
        private void Run(AdminCommand command)
        {
            if(_host?.Simulation==null)return;
            var result=_host.Simulation.Apply(command);
            if(result.Accepted && command.Action!="invulnerable" && command.Action!="freeze" && command.Action!="lighting" && command.Action!="reset-overrides")
                FindAnyObjectByType<FrontEnd.FrontEndBootstrap>()?.Dirty.MarkChanged();
            Say(result.Problem,!result.Accepted);Paint();
            GetComponent<InventoryPanelController>()?.Paint(true);
        }
        private void Say(string text,bool error){Q<Label>("admin-status").text=text;Q<Label>("admin-status").EnableInClassList("admin-error",error);}
        private void Page(string page)
        {
            foreach(var tab in new[]{"supplies","enemies","player","world"}){Q<VisualElement>("admin-page-"+tab).EnableInClassList("admin-hidden",tab!=page);Q<Button>("admin-tab-"+tab).EnableInClassList("selected",tab==page);}
            Q<ScrollView>("admin-body").scrollOffset=Vector2.zero;
        }
        private void FilterItems()
        {
            var sim=_host?.Simulation;if(sim==null)return;
            var selected=Selected(_itemKeys,"admin-item");if(selected.Length==0)selected="steel";_itemKeys.Clear();var labels=new List<string>();
            var search=Q<TextField>("admin-search").value?.Trim()??"";
            foreach(var item in sim.Context.Data.Items)if(Match(item.Key,item.DisplayName,search)){_itemKeys.Add(item.Key);labels.Add(item.DisplayName+" · resource");}
            foreach(var machine in sim.Context.Data.Machines)if(!_itemKeys.Contains(machine.Key)&&Match(machine.Key,machine.DisplayName,search)){_itemKeys.Add(machine.Key);labels.Add(machine.DisplayName+" · packed structure");}
            var field=Q<DropdownField>("admin-item");field.choices=labels;field.index=labels.Count==0?-1:Math.Max(0,_itemKeys.IndexOf(selected));
            Q<Button>("admin-give").SetEnabled(labels.Count>0);
        }
        private static bool Match(string key,string name,string search)=>key.IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0 || name.IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0;
        /// <summary>True in the editor and in a development build: the only places the Admin panel can be opened.</summary>
        public static bool Available=>Application.isEditor||Debug.isDebugBuild;
        private void Update()
        {
            if(!_bound){if(_host==null)_host=FindAnyObjectByType<SimHost>();if(_shell==null)_shell=GetComponent<UiShell>();Bind();if(!_bound)return;}
            var sim=_host?.Simulation;
            if(!ReferenceEquals(_session,sim))
            {
                _session=sim;if(_shell?.Active=="admin-panel")_shell.CloseActive();Q<TextField>("admin-search").SetValueWithoutNotify("");
                if(sim!=null){FilterItems();var weapons=new List<string>();_weaponKeys.Clear();foreach(var w in sim.Context.Data.Weapons){weapons.Add(w.DisplayName);_weaponKeys.Add(w.Key);}Q<DropdownField>("admin-weapon").choices=weapons;Q<DropdownField>("admin-weapon").index=weapons.Count>0?0:-1;
                var enemies=new List<string>();_enemyKeys.Clear();foreach(var e in sim.Context.Data.Enemies){enemies.Add(e.DisplayName);_enemyKeys.Add(e.Key);}Q<DropdownField>("admin-enemy").choices=enemies;Q<DropdownField>("admin-enemy").index=enemies.Count>0?0:-1;}
                Say("Choose a tool. F8 or Escape closes this menu.",false);
            }
            // GP-W6: a developer tool. It is offered in the editor and in development builds, and nowhere else.
            Q<Button>("admin-launcher").style.display=Available?DisplayStyle.Flex:DisplayStyle.None;
            if(!Available)return;
            Q<Button>("admin-launcher").SetEnabled(sim!=null);
            if(Keyboard.current?.f8Key.wasPressedThisFrame==true)Toggle();
            if(Time.unscaledTime>=_paintAt){Paint();_paintAt=Time.unscaledTime+.15f;}
        }
        private void Paint()
        {
            var sim=_host?.Simulation;if(sim==null)return;
            _panel.style.width=Mathf.Min(660,Mathf.Max(280,_root.resolvedStyle.width-36));
            var a=sim.State.Admin;
            Q<Toggle>("admin-god").SetValueWithoutNotify(a.Invulnerable);Q<Toggle>("admin-freeze").SetValueWithoutNotify(a.FreezeEnemies);
            Q<Button>("admin-pause").text=_host.Paused?"Resume simulation":"Pause simulation";
            var flags=new List<string>();if(a.Invulnerable)flags.Add("INVULNERABLE");if(a.FreezeEnemies)flags.Add("ENEMIES FROZEN");if(a.Lighting>=0)flags.Add(a.Lighting==1?"DAYLIGHT":"NIGHT");
            Q<Label>("admin-indicator").text=string.Join(" · ",flags);
            var p=sim.State.Engineer.Pos;var power=PowerQueries.Network(sim.Context,sim.State);
            Q<Label>("admin-readout").text=$"{(_host.Paused?"Paused":"Running")} · {sim.State.Enemies.Actors.Count} enemies · {power.SupplyKw:0}/{power.DemandKw:0} kW supply / demand\nPosition {p.X:0.0}, {p.Y:0.0} · Admin actions: {a.Actions}";
        }
    }
}
