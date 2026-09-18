var h=UnityEngine.Object.FindAnyObjectByType<Relight.Presentation.SimHost>();var s=h.Simulation;
return new{t=s.State.T,pos=s.State.Engineer.Pos,rate=s.Context.Data.Engineer.HandMinePerS,progress=s.State.Engineer.MineProg,remaining=Relight.Sim.Ground.UnitsAt(s.Context,s.State,63,356),mining=s.State.Engineer.Mining,objective=Relight.Sim.OpeningQueries.Objective(s.Context,s.State).Text};
