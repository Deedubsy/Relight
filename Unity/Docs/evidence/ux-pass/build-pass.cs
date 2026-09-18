var host=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var sim=host.Simulation;
var keyboard=UnityEngine.InputSystem.Keyboard.current;
var mouse=UnityEngine.InputSystem.InputSystem.GetDevice<UnityEngine.InputSystem.Mouse>() ?? UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>();
UnityEngine.InputSystem.InputSystem.EnableDevice(mouse);
var root=UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>(UnityEngine.FindObjectsSortMode.None).First(d=>d.visualTreeAsset!=null && d.visualTreeAsset.name=="GameUI").rootVisualElement;
var shell=UnityEngine.Object.FindAnyObjectByType<Relight.UI.UiShell>();
string log="E:/Factorio2/Unity/Docs/evidence/ux-pass/onboarding-log.txt";
System.IO.File.AppendAllText(log,"Normal starting inventory: "+sim.State.Engineer.Inv[Relight.Sim.ItemId.Steel]+" steel, "+sim.State.Engineer.Inv[Relight.Sim.ItemId.Copper]+" copper\n");
void Note(string s){System.IO.File.AppendAllText(log,s+"\n");}
void Check(bool condition,string what){if(!condition)throw new System.Exception(what);Note("PASS "+what);}
void Key(UnityEngine.InputSystem.Key key){UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(key));}
void Release(){UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState{position=mouse.position.ReadValue()});}
UnityEngine.Vector2 At(int x,int y){var s=UnityEngine.Camera.main.WorldToScreenPoint(new UnityEngine.Vector3(x+.5f,-y-.5f,0));return new UnityEngine.Vector2(s.x,s.y);}
System.Collections.IEnumerator Tap(UnityEngine.InputSystem.Key key){Key(key);yield return null;yield return null;Release();yield return null;yield return null;}
System.Collections.IEnumerator Move(double x,double y){
var until=UnityEngine.Time.realtimeSinceStartup+12;
while(UnityEngine.Time.realtimeSinceStartup<until){
var p=sim.State.Engineer.Pos;double dx=x-p.X,dy=y-p.Y;
if(System.Math.Abs(dx)<.12 && System.Math.Abs(dy)<.12)break;
var keys=new System.Collections.Generic.List<UnityEngine.InputSystem.Key>();
if(System.Math.Abs(dx)>=.12)keys.Add(dx>0?UnityEngine.InputSystem.Key.D:UnityEngine.InputSystem.Key.A);
if(System.Math.Abs(dy)>=.12)keys.Add(dy>0?UnityEngine.InputSystem.Key.S:UnityEngine.InputSystem.Key.W);
UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(keys.ToArray()));yield return null;
}
Release();yield return null;
var end=sim.State.Engineer.Pos;if(System.Math.Abs(x-end.X)>.3 || System.Math.Abs(y-end.Y)>.3)throw new System.Exception("Walk blocked at "+end+" wanted "+x+","+y);
}
System.Collections.Generic.List<Relight.Sim.TilePoint> Path(Relight.Sim.ItemId item){
var ctx=sim.Context;var st=sim.State;int width=ctx.Geometry.Width;int sx=(int)st.Engineer.Pos.X,sy=(int)st.Engineer.Pos.Y;
var q=new System.Collections.Generic.Queue<int>();var from=new System.Collections.Generic.Dictionary<int,int>();int start=sy*width+sx;from[start]=-1;q.Enqueue(start);int found=-1;
while(q.Count>0 && from.Count<10000){int n=q.Dequeue(),x=n%width,y=n/width;
if(Relight.Sim.Mining.TryTile(ctx,st,x,y,out var got,out _) && got==item && !ctx.Geometry.Solid(x,y)){found=n;break;}
for(int i=0;i<4;i++){int xx=x+(i==0?1:i==1?-1:0),yy=y+(i==2?1:i==3?-1:0),nn=yy*width+xx;if(!Relight.Sim.Ground.InBounds(ctx,xx,yy)||from.ContainsKey(nn)||!Relight.Sim.Ground.CanStand(ctx,st,xx+.5,yy+.5))continue;from[nn]=n;q.Enqueue(nn);}}
if(found<0)throw new System.Exception("No reachable source for "+item);
var path=new System.Collections.Generic.List<Relight.Sim.TilePoint>();for(int n=found;n!=start;n=from[n])path.Add(new Relight.Sim.TilePoint(n%width,n/width));path.Reverse();return path;
}
System.Collections.IEnumerator Run(){
int before=sim.State.Machines.Count;
yield return Tap(UnityEngine.InputSystem.Key.Digit1);
var input=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.WorldInput>();
Check(input.Tool=="generator","Shortcut 1 selects Generator");
yield return Tap(UnityEngine.InputSystem.Key.R);Check(input.GhostDir==Relight.Sim.Dir.E,"R rotates the selected structure");
int tx=-1,ty=-1;var p=sim.State.Engineer.Pos;
for(int radius=0;radius<7 && tx<0;radius++)for(int y=(int)p.Y-radius;y<=(int)p.Y+radius && tx<0;y++)for(int x=(int)p.X-radius;x<=(int)p.X+radius && tx<0;x++)
if(Relight.Sim.Placement.Validity(sim.Context,sim.State,"generator",x,y,input.GhostDir).ok && !shell.PointerOverUi(At(x,y))){tx=x;ty=y;}
Check(tx>=0,"Reachable Generator placement");
var at=At(tx,ty);UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState{position=at});yield return null;yield return null;
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/ux-pass/placement-generator.png");yield return null;
UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState{position=at}.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left));yield return null;yield return null;Release();yield return new UnityEngine.WaitForSecondsRealtime(.3f);
Check(sim.State.Machines.Count==before+1,"LMB places Generator with mined materials");
Check(Relight.Sim.OpeningQueries.Objective(sim.Context,sim.State).Title.Contains("Excavator"),"Objective advances after placement");
yield return Tap(UnityEngine.InputSystem.Key.Escape);Check(input.Tool=="","Escape cancels placement");
var button=UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(root,"open-inventory");var rect=button.worldBound;float scale=UnityEngine.Screen.width/root.resolvedStyle.width;
var pos=new UnityEngine.Vector2(rect.center.x*scale,UnityEngine.Screen.height-rect.center.y*scale);
UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState{position=pos});yield return null;yield return null;
UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState{position=pos}.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left));yield return null;yield return null;Release();yield return new UnityEngine.WaitForSecondsRealtime(.25f);
Check(shell.Active=="inventory-panel","Mouse Inventory button opens Backpack");Check(sim.State.Machines.Count==before+1,"UI click does not build in the world");
yield return Tap(UnityEngine.InputSystem.Key.Tab);
System.IO.File.WriteAllBytes("E:/Factorio2/Unity/Docs/evidence/ux-pass/normal-opening.json",Relight.Sim.SaveSerializer.Write(sim.State,sim.Context,null,out _));
Note("BUILD DONE");
}
System.Collections.IEnumerator Guard(){var run=Run();var stack=new System.Collections.Generic.Stack<System.Collections.IEnumerator>();stack.Push(run);while(stack.Count>0){var e=stack.Peek();bool next=false;try{next=e.MoveNext();}catch(System.Exception ex){Note("FAIL "+ex.Message);Release();yield break;}if(!next){stack.Pop();continue;}if(e.Current is System.Collections.IEnumerator child){stack.Push(child);continue;}yield return e.Current;}Release();}
host.StartCoroutine(Guard());return "Integrated normal-stock onboarding pass started";

