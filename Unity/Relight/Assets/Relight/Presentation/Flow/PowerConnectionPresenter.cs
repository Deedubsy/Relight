using System.Collections.Generic;
using Relight.Sim;
using Relight.World;
using UnityEngine;

namespace Relight.Presentation
{
    /// <summary>Persistent cables and subtle travelling voltage cues from the real connected power network.</summary>
    public sealed class PowerConnectionPresenter : MonoBehaviour
    {
        private sealed class Cable
        {
            public LineRenderer Wire;
            public readonly LineRenderer[] Pulses=new LineRenderer[2];
            public readonly Vector3[] Points=new Vector3[9];
            public int From,To;
            public bool Reverse;
        }
        private SimHost _host;
        private Material _material;
        private readonly List<Cable> _cables=new List<Cable>();
        private readonly List<PowerLink> _links=new List<PowerLink>();
        private readonly HashSet<long> _seen=new HashSet<long>();
        private Simulation _session;
        private int _rev=-1;
        private float _clock;
        public int Drawn { get; private set; }
        public int Energized { get; private set; }
        public float AnimationTime => _clock;
        private void Awake()
        {
            _host=FindAnyObjectByType<SimHost>();
            _material=new Material(Shader.Find("Sprites/Default"));
        }
        private void LateUpdate()
        {
            var sim=_host==null?null:_host.Simulation;
            if(sim==null){HideFrom(0);Energized=0;return;}
            if(!ReferenceEquals(_session,sim)){_rev=-1;_clock=0;_session=sim;}
            if(_rev!=sim.State.Rev){Rebuild(sim);_rev=sim.State.Rev;}
            if(!_host.Paused)_clock+=Mathf.Min(Time.deltaTime,.1f);
            // Read the cached grid once; topology is independent of clock and fuel. Fuel changes still repaint while paused.
            var grid=PowerGrid.Of(sim.Context,sim.State);Energized=0;
            for(var i=0;i<Drawn;i++)
            {
                var cable=_cables[i];var c=grid.Of(cable.From);
                var from=sim.State.MachineById(cable.From);var to=sim.State.MachineById(cable.To);
                var live=c!=null&&c.Supply>0&&from!=null&&to!=null;
                if(live && ((PowerGrid.IsSource(sim.Context.Data,from)&&PowerGrid.FuelUnits(from)<=0) ||
                            (PowerGrid.IsSource(sim.Context.Data,to)&&PowerGrid.FuelUnits(to)<=0)))live=false;
                if(live)Energized++;
                cable.Wire.startColor=cable.Wire.endColor=live?new Color(.76f,.54f,.24f,.95f):new Color(.37f,.40f,.37f,.85f);
                for(var j=0;j<2;j++)
                {
                    var pulse=cable.Pulses[j];pulse.enabled=live;
                    if(!live)continue;
                    var t=Mathf.Repeat(_clock*.34f+j*.5f+i*.137f,1);
                    var tail=Mathf.Max(0,t-.10f);
                    var alpha=Mathf.Clamp01(t*12)*Mathf.Clamp01((1-t)*12);
                    pulse.startColor=new Color(1,.77f,.31f,alpha*.25f);pulse.endColor=new Color(1,.95f,.65f,alpha);
                    for(var k=0;k<5;k++)
                    {var p=Mathf.Lerp(tail,t,k/4f);pulse.SetPosition(k,Along(cable,cable.Reverse?1-p:p));}
                }
            }
        }
        private static Vector3 Along(Cable cable,float t)
        { var at=Mathf.Clamp(t,0,1)*8;var i=Mathf.Min(7,(int)at);return Vector3.Lerp(cable.Points[i],cable.Points[i+1],at-i); }
        private void Rebuild(Simulation sim)
        {
            var st=sim.State;var d=sim.Context.Data;_seen.Clear();var count=0;
            foreach(var m in st.Machines)
            {
                if(PowerGrid.ReachOf(d,m)<=0&&!PowerGrid.IsSource(d,m)&&PowerGrid.DemandKw(d,m)<=0)continue;
                PowerLinks.At(sim.Context,st,m.Kind,m.X,m.Y,m.Dir,m.Size,_links);
                foreach(var link in _links)
                {
                    var target=st.MachineById(link.MachineId);
                    if(target==null||target.Id==m.Id)continue;
                    if(PowerGrid.ReachOf(d,m)>0&&PowerGrid.ReachOf(d,target)<=0&&!PowerGrid.IsSource(d,target))continue;
                    var key=((long)System.Math.Min(m.Id,target.Id)<<32)|(uint)System.Math.Max(m.Id,target.Id);
                    if(!_seen.Add(key))continue;
                    var cable=Take(count++);cable.From=m.Id;cable.To=target.Id;
                    cable.Reverse=PowerGrid.IsSource(d,target)||(!PowerGrid.IsSource(d,m)&&PowerGrid.ReachOf(d,m)<=0);
                    for(var j=0;j<9;j++)
                    {
                        var t=j/8.0;var x=link.FromX+(link.ToX-link.FromX)*t;
                        var y=link.FromY+(link.ToY-link.FromY)*t+System.Math.Sin(t*System.Math.PI)*.18;
                        cable.Points[j]=WorldSpace.World(new Vec2(x,y),-1.8f);cable.Wire.SetPosition(j,cable.Points[j]);
                    }
                }
            }
            HideFrom(count);
        }
        private LineRenderer Line(string name,int points,int order,float width)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);
            var line=go.AddComponent<LineRenderer>();line.sharedMaterial=_material;line.useWorldSpace=true;line.positionCount=points;
            line.sortingOrder=order;line.numCapVertices=3;line.widthMultiplier=width;return line;
        }
        private Cable Take(int i)
        {
            if(i==_cables.Count)
            {var c=new Cable{Wire=Line("Power cable",9,9,.045f)};
             for(var j=0;j<2;j++)c.Pulses[j]=Line("Live current",5,10,.07f);_cables.Add(c);}
            _cables[i].Wire.enabled=true;return _cables[i];
        }
        private void HideFrom(int n)
        {
            for(var i=n;i<_cables.Count;i++){_cables[i].Wire.enabled=false;foreach(var pulse in _cables[i].Pulses)pulse.enabled=false;}
            Drawn=n;
        }
        private void OnDisable(){HideFrom(0);_rev=-1;Energized=0;}
        private void OnDestroy(){if(_material!=null)Destroy(_material);}
    }
}
