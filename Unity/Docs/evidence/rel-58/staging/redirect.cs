var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "relight-rel58-watch");
System.IO.Directory.CreateDirectory(root);
UnityEditor.SessionState.SetString("Relight.SaveRootOverride", root);
return root + " scene=" + UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;
