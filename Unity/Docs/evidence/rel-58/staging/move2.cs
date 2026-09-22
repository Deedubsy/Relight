// REL-58 look: stand the Engineer on a tile so the camera frames it. Assistant Play session only.
var host = UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();
var st = host.Simulation.State;
st.Engineer.Pos = new Relight.Sim.Vec2(88.5, 364.5);
return "eng=" + st.Engineer.Pos.X + "," + st.Engineer.Pos.Y;
