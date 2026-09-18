var d=UnityEditor.AssetDatabase.LoadAssetAtPath<Relight.Data.GameDataRegistry>("Assets/Relight/Data/GameDataRegistry.asset").Build();
var rows=new System.Text.StringBuilder();rows.AppendLine("Play="+UnityEditor.EditorApplication.isPlaying);
foreach(var spec in d.Machines){var recipes=new System.Collections.Generic.List<Relight.Sim.Recipe>();Relight.Sim.ProductionRules.RecipesFor(d,new Relight.Sim.Machine{Kind=spec.Key},recipes);rows.AppendLine(spec.Key+" | "+spec.DisplayName+" | "+spec.RecipeStation+" | "+string.Join(", ",recipes.Select(r=>r.DisplayName+" -> "+string.Join(",",r.Outputs.Select(o=>d.Item(o.Item).DisplayName)))));}
return rows.ToString();
