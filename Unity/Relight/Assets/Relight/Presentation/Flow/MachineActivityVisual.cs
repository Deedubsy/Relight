using System.Collections.Generic;
using Relight.Sim;
using UnityEngine;

namespace Relight.Presentation
{
    /// <summary>Read-only, small mechanical/status overlays. The owner retains its id, not a sim machine.</summary>
    internal sealed class MachineActivityVisual
    {
        private static readonly Color Mint = new Color(.48f,.91f,.65f,1);
        private static readonly Color Amber = new Color(1f,.68f,.22f,1);
        private static readonly Color Red = new Color(1f,.37f,.30f,1);
        private static readonly Color Metal = new Color(.40f,.48f,.43f,1);
        private static readonly Color Dark = new Color(.055f,.08f,.065f,1);
        private readonly Transform _parent;
        private readonly Material _material;
        private readonly List<LineRenderer> _strokes = new List<LineRenderer>(16);
        private int _used;
        private float _clock, _motion;
        public MachineOperatingState State { get; private set; }
        public float Motion => _motion;
        public MachineActivityVisual(Transform parent,Material material) { _parent=parent;_material=material; }

        public static bool Supports(GameData d,Machine m) => FlowRules.IsConveyor(m.Kind) || FlowRules.IsInserter(m.Kind) ||
            PowerGrid.IsSource(d,m) || ProductionRules.IsProcessor(d,m) || ProductionRules.IsMiner(d,m) || TurretHopper.IsTurret(d,m);

        public void Draw(Simulation sim,Machine m,float dt)
        {
            _used=0;_clock+=dt;
            var ctx=sim.Context;var st=sim.State;var data=ctx.Data;
            var belt=FlowRules.IsConveyor(m.Kind);var arm=FlowRules.IsInserter(m.Kind);var source=PowerGrid.IsSource(data,m);
            State=ProductionQueries.OperatingState(ctx,st,m.Id);
            var throttle=PowerQueries.Throttle(ctx,st,m.Id);
            if(belt || arm)
            {
                var flow=FlowQueries.Status(ctx,st,m);
                State=arm&&throttle<=0 ? MachineOperatingState.Unpowered : flow==FlowStatus.Blocked ? MachineOperatingState.OutputFull :
                    flow==FlowStatus.Starved ? MachineOperatingState.NoInput : belt||flow==FlowStatus.Running ? MachineOperatingState.Running : MachineOperatingState.Idle;
            }
            if(arm&&State==MachineOperatingState.Running&&throttle<1)State=MachineOperatingState.Throttled;
            var running=State==MachineOperatingState.Running||State==MachineOperatingState.Throttled;
            if(running)_motion+=dt*(belt?(float)FlowRules.Speed(data,m):source?1f:(float)throttle);
            var colour=running ? State==MachineOperatingState.Throttled?Amber:Mint :
                State==MachineOperatingState.Unpowered||State==MachineOperatingState.OutOfFuel||State==MachineOperatingState.Disabled ? Red : Amber;
            var pulse=.68f+.32f*(.5f+.5f*Mathf.Sin(_clock*3.4f));
            var (w,h)=m.Dimensions;
            if(belt) DrawBelt(st,m,colour);
            else
            {
                if(source || ProductionRules.IsMiner(data,m) || ProductionRules.IsProcessor(data,m))
                {
                    var r=Mathf.Min(w,h)*.20f;var centre=new Vector2(-w*.10f,0);
                    Circle(centre,r+.055f,Metal,.055f);
                    Circle(centre,r,Dark,.08f);
                    for(var i=0;i<3;i++)
                    { var a=_motion*4.5f+i*Mathf.PI*2/3;Segment(centre,centre+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r, running?colour:Metal,.055f); }
                    Circle(centre,.045f,running?colour:Metal,.07f);
                    if(source && running)
                    {
                        // Small exhaust wisps, not lights or gameplay particles; absent at standby/off.
                        for(var i=0;i<3;i++)
                        { var t=Mathf.Repeat(_motion*.65f+i/3f,1);var smoke=new Color(.64f,.69f,.62f,(1-t)*.38f);
                          Circle(new Vector2(w*.20f+Mathf.Sin(t*3+i)*.06f,h*.25f+t*.42f),.045f+t*.055f,smoke,.04f); }
                    }
                }
                else if(arm)
                {
                    var fraction=(float)FlowQueries.SwingFraction(data,st,m.Id);var angle=((int)m.Dir*90+180-fraction*180)*Mathf.Deg2Rad;
                    var tip=new Vector2(Mathf.Sin(angle),Mathf.Cos(angle))*.40f;
                    Segment(Vector2.zero,tip,Metal,.10f);Circle(tip,.05f,colour,.045f);
                }
                // Top-right status badge: shape as well as colour distinguishes the stop reasons.
                var at=new Vector2(w*.5f-.28f,h*.5f-.28f);
                Circle(at,.19f,Dark,.13f);
                var c=colour;c.a=running?1:pulse;
                if(State==MachineOperatingState.Unpowered)
                {
                    var line=Stroke(4,.048f,c);Point(line,0,at+new Vector2(.055f,.14f));Point(line,1,at+new Vector2(-.06f,.015f));
                    Point(line,2,at+new Vector2(.065f,-.015f));Point(line,3,at+new Vector2(-.055f,-.14f));
                    Segment(at+new Vector2(-.13f,-.12f),at+new Vector2(.13f,.12f),Dark,.055f);
                }
                else if(State==MachineOperatingState.OutputFull)
                {
                    Segment(at+new Vector2(-.065f,-.12f),at+new Vector2(-.065f,.12f),c,.06f);
                    Segment(at+new Vector2(.065f,-.12f),at+new Vector2(.065f,.12f),c,.06f);
                }
                else if(State==MachineOperatingState.OutOfFuel || State==MachineOperatingState.Disabled)
                { Circle(at,.11f,c,.045f);Segment(at+new Vector2(0,.015f),at+new Vector2(0,.16f),c,.05f); }
                else if(State==MachineOperatingState.NoInput)
                { Circle(at,.105f,c,.04f);Segment(at,at+new Vector2(0,.075f),c,.03f);Segment(at,at+new Vector2(.06f,-.04f),c,.03f); }
                else Circle(at,.07f,running?c:Metal,.09f);
            }
            for(var i=_used;i<_strokes.Count;i++)_strokes[i].enabled=false;
        }
        private void DrawBelt(SimState st,Machine m,Color colour)
        {
            var (w,h)=m.Dimensions;var centre=new Vector2(m.X+w/2f,m.Y+h/2f);
            // Follow the same centreline and corner entry as carried items; exactly one arrow stays on MachineView.
            for(var i=0;i<5;i++)
            {
                var t=Mathf.Repeat(_motion+i/5f,1);var p=FlowQueries.Position(st,m,t);
                var dir=FlowRules.IsBelt(m.Kind)&&t<.5?FlowRules.EntryDir(st,m):m.Dir;
                var side=new Vector2(Dirs.DY[(int)dir],Dirs.DX[(int)dir])*.28f;
                var at=new Vector2((float)p.X-centre.x,centre.y-(float)p.Y);
                // Splitters buffer centrally, so their deck slats instead traverse the output direction.
                if(FlowRules.IsSplitter(m.Kind))at=new Vector2(Dirs.DX[(int)m.Dir],-Dirs.DY[(int)m.Dir])*(t-.5f);
                Segment(at-side,at+side,State==MachineOperatingState.OutputFull?new Color(.52f,.40f,.22f,1):Metal,.035f,DrawOrder.MachineCue);
            }
            if(State==MachineOperatingState.OutputFull || State==MachineOperatingState.NoInput)
            {
                var c=colour;c.a=.65f+.35f*(.5f+.5f*Mathf.Sin(_clock*3.4f));
                Segment(new Vector2(-.36f,-.35f),new Vector2(-.36f,-.19f),c,.055f);
                Segment(new Vector2(-.24f,-.35f),new Vector2(-.24f,-.19f),c,.055f);
            }
        }
        private LineRenderer Stroke(int points,float width,Color colour,int order=DrawOrder.MachineCue)
        {
            if(_used==_strokes.Count)
            {
                var go=new GameObject("Activity detail");go.transform.SetParent(_parent,false);
                var line=go.AddComponent<LineRenderer>();line.sharedMaterial=_material;line.useWorldSpace=false;line.numCapVertices=2;
                _strokes.Add(line);
            }
            var stroke=_strokes[_used++];stroke.enabled=true;stroke.positionCount=points;stroke.widthMultiplier=width;
            stroke.startColor=stroke.endColor=colour;stroke.sortingOrder=order;return stroke;
        }
        private static void Point(LineRenderer line,int i,Vector2 p)=>line.SetPosition(i,new Vector3(p.x,p.y,-.7f));
        private void Segment(Vector2 a,Vector2 b,Color c,float width,int order=DrawOrder.MachineCue)
        { var line=Stroke(2,width,c,order);Point(line,0,a);Point(line,1,b); }
        private void Circle(Vector2 at,float radius,Color c,float width)
        { var line=Stroke(17,width,c);for(var i=0;i<=16;i++){var a=i*Mathf.PI/8;Point(line,i,at+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius);} }
    }
}
