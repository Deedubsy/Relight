var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();var sim=host.Simulation;var shell=UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>();
var doc=UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>().First(d=>d.visualTreeAsset!=null&&d.visualTreeAsset.name=="GameUI");var root=doc.rootVisualElement;
var log="E:/Factorio2/Unity/Docs/evidence/admin-menu/runtime.txt";
T Q<T>(string n)where T:UnityEngine.UIElements.VisualElement=>UnityEngine.UIElements.UQueryExtensions.Q<T>(root,n);
void Check(bool ok,string msg){System.IO.File.AppendAllText(log,(ok?"PASS ":"FAIL ")+msg+"\n");if(!ok)throw new System.Exception(msg);}
System.Collections.IEnumerator Click(string name){var button=Q<UnityEngine.UIElements.Button>(name);Check(button!=null&&button.enabledInHierarchy,"Usable button: "+name);using(var e=UnityEngine.UIElements.NavigationSubmitEvent.GetPooled()){e.target=button;button.SendEvent(e);}yield return new UnityEngine.WaitForSecondsRealtime(.18f);}
System.Collections.IEnumerator Tap(UnityEngine.InputSystem.Key key){var kb=UnityEngine.InputSystem.Keyboard.current;UnityEngine.InputSystem.InputSystem.QueueStateEvent(kb,new UnityEngine.InputSystem.LowLevel.KeyboardState(key));yield return null;yield return null;UnityEngine.InputSystem.InputSystem.QueueStateEvent(kb,new UnityEngine.InputSystem.LowLevel.KeyboardState());yield return null;yield return null;}
System.Collections.IEnumerator Run(){
Check(shell.Active=="admin-panel","F8 opens the registered admin panel");
Q<UnityEngine.UIElements.TextField>("admin-search").value="steel";Q<UnityEngine.UIElements.IntegerField>("admin-amount").value=25;
var before=sim.State.Engineer.Inv[Relight.Sim.ItemId.Steel];yield return Click("admin-give");Check(sim.State.Engineer.Inv[Relight.Sim.ItemId.Steel]==before+25,"Resource button grants exact requested stock");
Q<UnityEngine.UIElements.IntegerField>("admin-amount").value=-1;yield return Click("admin-give");Check(Q<UnityEngine.UIElements.Label>("admin-status").ClassListContains("admin-error"),"Invalid quantity shows refusal");
Q<UnityEngine.UIElements.IntegerField>("admin-amount").value=10;
Q<UnityEngine.UIElements.IntegerField>("admin-amount").Focus();yield return null;yield return null;
Check(Relight.Presentation.WorldInput.UiTextInputFocused?.Invoke()==true,"Numeric input suppresses gameplay shortcuts");
yield return Tap(UnityEngine.InputSystem.Key.Tab);Check(shell.Active=="admin-panel","Tab navigates form without opening Backpack");
yield return Click("admin-give-weapon");Check(sim.State.Weapons.Owned.Count>0,"Weapon grant creates an owned weapon instance");
yield return Click("admin-tab-world");yield return Click("admin-pause");Check(host.Paused&&shell.Active=="admin-panel","Admin remains open while paused");
var pause=UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>().FirstOrDefault(d=>d.visualTreeAsset!=null&&d.visualTreeAsset.name=="PauseMenu");
if(pause!=null)Check(UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(pause.rootVisualElement,"pause-menu").ClassListContains("hidden"),"Pause overlay does not cover admin controls");
yield return Click("admin-night");Check(Relight.Sim.LightQueries.Daylight(sim.Context,sim.State).Daylight==0,"Night override applies while paused");
yield return Click("admin-tab-player");Q<UnityEngine.UIElements.Toggle>("admin-god").value=true;Check(sim.State.Admin.Invulnerable,"Invulnerability toggle changes simulation rule");
yield return Click("admin-heal");yield return Click("admin-ammo");Check(sim.State.Weapons.Owned[0].Loaded>0,"Refill action loads granted weapon");
yield return Click("admin-tab-enemies");Q<UnityEngine.UIElements.Toggle>("admin-freeze").value=true;Q<UnityEngine.UIElements.DropdownField>("admin-direction").index=2;Q<UnityEngine.UIElements.IntegerField>("admin-enemy-count").value=3;
yield return Click("admin-spawn");Check(sim.State.Enemies.Actors.Count>=3,"Enemy button spawns real actors while paused");
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/admin-menu/enemies-1080.png");yield return null;yield return null;
yield return Click("admin-clear");Check(sim.State.Enemies.Actors.Count==0,"Clear button removes enemies");
yield return Click("admin-raid");Check(sim.State.Director.Minor!=null,"Raid button stages a real raid group");
yield return Click("admin-clear");
yield return Click("admin-tab-world");yield return Click("admin-reset");Check(!sim.State.Admin.Invulnerable&&!sim.State.Admin.FreezeEnemies&&sim.State.Admin.Lighting==-1,"Reset turns off all temporary overrides");
var game=UnityEditor.EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));game.GetType().GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).SetValue(game,9);doc.panelSettings.scale=1.25f;yield return new UnityEngine.WaitForSecondsRealtime(.4f);
yield return Click("admin-tab-supplies");Q<UnityEngine.UIElements.ScrollView>("admin-body").ScrollTo(Q<UnityEngine.UIElements.Button>("admin-give-weapon"));yield return new UnityEngine.WaitForSecondsRealtime(.2f);
Check(Q<UnityEngine.UIElements.ScrollView>("admin-body").contentViewport.worldBound.Contains(Q<UnityEngine.UIElements.Button>("admin-give-weapon").worldBound.center),"Last Supplies action scrolls into view at 720p / 125%");
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/admin-menu/supplies-720-scale125.png");yield return null;yield return null;
Check(Q<UnityEngine.UIElements.Label>("admin-status").worldBound.yMax<Q<UnityEngine.UIElements.VisualElement>("action-bar").worldBound.yMin,"Admin result stays above the dock");
doc.panelSettings.scale=1;game.GetType().GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic).SetValue(game,3);
yield return Tap(UnityEngine.InputSystem.Key.Escape);Check(shell.Active!="admin-panel","Escape closes admin before another layer");
Check(Relight.Sim.Ledger.Conservation(sim.State,sim.Context.Data).Problems.Count==0,"Admin stock and ammo grants remain conserved");
System.IO.File.AppendAllText(log,"DONE\n");
}
host.StartCoroutine(Run());return "Admin UI checks started";
