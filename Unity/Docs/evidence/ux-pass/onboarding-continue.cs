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
Release();yield return null;yield return null;yield return null;
var end=sim.State.Engineer.Pos;if(System.Math.Abs(x-end.X)>.3 || System.Math.Abs(y-end.Y)>.3)throw new System.Exception("Walk blocked at "+end+" wanted "+x+","+y);
}
System.Collections.Generic.List<Relight.Sim.TilePoint> Path(Relight.Sim.ItemId item){
var ctx=sim.Context;var st=sim.State;int width=ctx.Geometry.Width;int sx=(int)st.Engineer.Pos.X,sy=(int)st.Engineer.Pos.Y;
var q=new System.Collections.Generic.Queue<int>();var from=new System.Collections.Generic.Dictionary<int,int>();int start=sy*width+sx;from[start]=-1;q.Enqueue(start);int found=-1;
while(q.Count>0 && from.Count<10000){int n=q.Dequeue(),x=n%width,y=n/width;
if(Relight.Sim.Mining.TryTile(ctx,st,x,y,out var got,out _) && got==item && !ctx.Geometry.Solid(x,y)){found=n;break;}
for(int i=0;i<4;i++){int xx=x+(i==0?1:i==1?-1:0),yy=y+(i==2?1:i==3?-1:0),nn=yy*width+xx;if(!Relight.Sim.Ground.InBounds(ctx,xx,yy)||from.ContainsKey(nn)||!Relight.Sim.Ground.CanStand(ctx,st,xx+.5,yy+.5))continue;from[nn]=n;q.Enqueue(nn);}}
if(found<0)throw new System.Exception("No reachable source for "+item);
var path=new System.Collections.Generic.List<Relight.Sim.TilePoint>();for(int n=found;n!=start;n=from[n])path.Add(new Relight.Sim.TilePoint(n%width,n/width));path.Reverse();if(path.Count==0)path.Add(new Relight.Sim.TilePoint(sx,sy));return path;
}
System.Collections.IEnumerator Run(){
foreach(var item in new[]{Relight.Sim.ItemId.Steel,Relight.Sim.ItemId.Copper}){
var path=Path(item);Note("Walk to "+item+": "+string.Join(";",path.Select(p=>p.X+","+p.Y)));
foreach(var p in path)yield return Move(p.X+.5,p.Y+.5);
int x=path[path.Count-1].X,y=path[path.Count-1].Y;
var at=At(x,y);UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState{position=at});yield return null;yield return null;yield return null;
var target=UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Label>(root,"target-title");
Check(target.text.Contains(sim.Context.Data.Item(item).DisplayName),"Hover identifies "+item+": "+target.text);
UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState{position=at}.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left));
double goal=item==Relight.Sim.ItemId.Steel?30:10;float until=UnityEngine.Time.realtimeSinceStartup+(float)((goal-sim.State.Engineer.Inv[item]+2)/sim.Context.Data.Engineer.HandMinePerS)+8;bool captured=false;
while(sim.State.Engineer.Inv[item]<goal && UnityEngine.Time.realtimeSinceStartup<until){
if(!captured && sim.State.Engineer.Mining && sim.State.Engineer.MineProg>.2){UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/ux-pass/mining-"+item+".png");captured=true;Note("Live MineProg="+sim.State.Engineer.MineProg);}
yield return null;}
Release();yield return null;yield return null;
Check(captured,"Real mining progress "+item);Check(sim.State.Engineer.Inv[item]>=goal,"Mined into Backpack: "+item+"="+sim.State.Engineer.Inv[item]);
Check(!sim.State.Engineer.Mining,"Release interrupts mining");
}
Note("Opening materials gathered using only WASD and LMB.");
UnityEngine.ScreenCapture.CaptureScreenshot("E:/Factorio2/Unity/Docs/evidence/ux-pass/onboarding-materials.png");
Note("DONE");
}
System.Collections.IEnumerator Guard(){var run=Run();var stack=new System.Collections.Generic.Stack<System.Collections.IEnumerator>();stack.Push(run);while(stack.Count>0){var e=stack.Peek();bool next=false;try{next=e.MoveNext();}catch(System.Exception ex){Note("FAIL "+ex.Message);Release();yield break;}if(!next){stack.Pop();continue;}if(e.Current is System.Collections.IEnumerator child){stack.Push(child);continue;}yield return e.Current;}Release();}
host.StartCoroutine(Guard());return "Integrated normal-stock onboarding pass started";
