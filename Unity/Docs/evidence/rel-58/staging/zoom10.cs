// REL-58 look: widen the view (CameraRig sets the size only in Awake). zoom10.cs puts it back.
UnityEngine.Camera.main.orthographicSize = 10f;
return "ortho=" + UnityEngine.Camera.main.orthographicSize;
