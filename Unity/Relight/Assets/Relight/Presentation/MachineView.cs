using Relight.Data;
using Relight.Sim;
using Relight.Sim.UI;
using Relight.World;
using UnityEngine;

namespace Relight.Presentation
{
    /// <summary>
    /// B-13. One placed machine's visual. It holds the machine's <see cref="MachineDefinition"/> (the forward half
    /// of the data link in TECHNICAL_ARCHITECTURE.md §7.2) and a sim machine <b>id</b> — never a
    /// <c>Machine</c> reference (TA §4.5). Everything else it needs it asks the selectors for.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Relight/Machine View")]
    public sealed class MachineView : MonoBehaviour
    {
        [Tooltip("The data asset this prefab draws. Its Key is the sim machine kind, and it is what the Inspector edits.")]
        [SerializeField] private MachineDefinition definition;

        [Tooltip("Sprite depth; machines draw over the tilemap and under the engineer.")]
        [SerializeField] private float z = -1f;

        private SpriteRenderer _sprite;
        private LineRenderer _direction;
        private Material _directionMaterial;
        private MachineActivityVisual _activity;
        private BuildingConditionVisual _condition;
        private BuildingHealthVisual _health;
        private GateVisual _gate;
        public MachineOperatingState ActivityState => _activity?.State ?? MachineOperatingState.Idle;
        public float ActivityMotion => _activity?.Motion ?? 0;

        /// <summary>REL-133: true while this machine is drawing the repair mark. For the play tests.</summary>
        public bool RepairShowing => _condition != null && _condition.Showing;

        /// <summary>REL-134: true while this machine is wearing a health bar. For the play tests.</summary>
        public bool HealthShowing => _health != null && _health.Showing;

        /// <summary>REL-134: the thickness the health bar was last drawn at, in world units. For the zoom test.</summary>
        public float HealthThickness => _health == null ? 0 : _health.Thickness;

        /// <summary>REL-132: true while this machine is drawing a gate leaf. For the play tests.</summary>
        public bool GateShowing => _gate != null && _gate.Showing;

        /// <summary>REL-132: 0 shut, 1 wide open. For the play tests.</summary>
        public float GateOpenness => _gate == null ? 0 : _gate.Openness;

        public void Animate(Simulation sim,float dt,Material material,float halfView)
        {
            if (_sprite == null || !_sprite.isVisible) return;
            var m=sim.State.MachineById(MachineId);
            // REL-134: the health bar sits ABOVE the top edge — the space REL-133 left clear — so it and the repair
            // ring never overlap, whichever of them is showing. Shown only when the building has lost health: a base
            // at full strength must not sprout a bar over every wall and pole.
            var health=BuildingCondition.Health(sim.Context.Data,sim.State,m);
            if(health.Damaged)
            {
                if(_health==null)_health=new BuildingHealthVisual(transform,material);
                var (bw,bh)=m.Dimensions;
                _health.Draw(bw,bh,health.Fraction,halfView);
            }
            else _health?.Hide();
            // REL-132: the gate's leaf. A wreck is a hole rather than a door, so GateRules.Open turns it off and
            // the wreck's own drawing takes over. Nothing here is read back into the simulation.
            if(m!=null && GateRules.IsGate(m))
            {
                if(_gate==null)_gate=new GateVisual(transform,material);
                if(TurretRules.Wrecked(sim.Context.Data,sim.State,m))_gate.Hide();
                else _gate.Draw(GateRules.Horizontal(sim.Context,sim.State,m),
                    GateRules.Open(sim.Context,sim.State,m),dt,halfView);
            }
            else _gate?.Hide();
            // REL-133: the repair mark has its own gate and is drawn ABOVE the activity gate below, because the
            // things most often repaired — a Wall, a chest, a pole — are not things MachineActivityVisual supports.
            if(m!=null && BuildingCondition.RepairingMachine(sim.State,m.Id))
            {
                if(_condition==null)_condition=new BuildingConditionVisual(transform,material);
                var (cw,ch)=m.Dimensions;
                _condition.Draw(cw,ch,BuildingCondition.RepairProgress(sim.Context.Data,sim.State),dt);
            }
            else _condition?.Hide();
            // E-17: a wreck of any kind is supported while it is one, and put away once it is repaired.
            if(m==null || !MachineActivityVisual.Supports(sim.Context.Data,sim.State,m)){_activity?.Hide();return;}
            if(_activity==null)_activity=new MachineActivityVisual(transform,material);
            _activity.Draw(sim,m,dt);
            if(_direction!=null && FlowRules.IsConveyor(m.Kind))
                _direction.startColor=_direction.endColor=_activity.State==MachineOperatingState.OutputFull?new Color(1,.66f,.2f,.95f):new Color(.82f,.85f,.67f,.85f);
        }

        /// <summary>The sim id of the machine this view stands for; -1 before <see cref="Bind"/>.</summary>
        public int MachineId { get; private set; } = -1;

        /// <summary>The authored definition, for the presenter's kind check and for tests.</summary>
        public MachineDefinition Definition => definition;

        /// <summary>The sim machine kind this prefab is for, from the data asset.</summary>
        public string Kind => definition == null ? null : definition.Key;

        private void Awake() => _sprite = GetComponentInChildren<SpriteRenderer>();

        /// <summary>Place this view on a machine's footprint. Called by <see cref="MachinePresenter"/> only.</summary>
        public void Bind(in MachinePlacement m)
        {
            MachineId = m.Id;
            if (_sprite == null) _sprite = GetComponentInChildren<SpriteRenderer>();
            var centre = WorldSpace.RectCentre(m.X, m.Y, m.W, m.H);
            transform.position = new Vector3(centre.x, centre.y, z);
            // The placeholder sprite is one tile; scale it to the footprint the sim actually occupies.
            if (_sprite != null)
            {
                _sprite.transform.localScale = new Vector3(m.W, m.H, 1f);
                if (FlowRules.IsConveyor(m.Kind)) _sprite.color = new Color(.22f,.26f,.22f,1);
            }
            PaintDirection(m);
            name = $"{(definition != null ? definition.DisplayName : m.Kind)} #{m.Id}";
        }

        private void PaintDirection(in MachinePlacement m)
        {
            var miner = m.Kind == "excavator" || m.Kind == "pumpjack";
            if (!miner && !FlowRules.IsConveyor(m.Kind) && !FlowRules.IsInserter(m.Kind)) return;
            if (_direction == null)
            {
                var go = new GameObject("Travel direction"); go.transform.SetParent(transform, false);
                _direction = go.AddComponent<LineRenderer>();
                _directionMaterial = new Material(Shader.Find("Sprites/Default"));
                _direction.sharedMaterial = _directionMaterial;
                _direction.useWorldSpace = true; _direction.positionCount=3;
                _direction.widthMultiplier=.055f; _direction.sortingOrder=10;
                _direction.startColor=_direction.endColor=new Color(.96f,.74f,.32f,.95f);
            }
            var dx=Dirs.DX[(int)m.Dir]; var dy=Dirs.DY[(int)m.Dir];
            var cx=m.X+m.W/2.0; var cy=m.Y+m.H/2.0;
            var reach=miner ? (dx == 0 ? m.H : m.W)/2.0+.24 : .28;
            var tx=cx+dx*reach; var ty=cy+dy*reach;
            _direction.SetPosition(0,WorldSpace.World(new Vec2(tx-dx*.22-dy*.18,ty-dy*.22+dx*.18),-1.6f));
            _direction.SetPosition(1,WorldSpace.World(new Vec2(tx,ty),-1.6f));
            _direction.SetPosition(2,WorldSpace.World(new Vec2(tx-dx*.22+dy*.18,ty-dy*.22-dx*.18),-1.6f));
        }
        private void OnDestroy() { if (_directionMaterial != null) Destroy(_directionMaterial); }

        /// <summary>Used by the editor generator when it authors the prefab.</summary>
        public void SetDefinition(MachineDefinition d) => definition = d;
    }
}
