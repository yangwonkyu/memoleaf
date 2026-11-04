using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Portfolio.FoldGrid;

public static class CreateFoldGridScene
{
    [MenuItem("Portfolio/Create Fold Grid Demo")]
    public static void Create()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Camera").AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.10f, 0.14f);
        new GameObject("Fold Grid Demo").AddComponent<FoldGridDemo>();
        EditorSceneManager.SaveScene(scene, "Assets/FoldGridDemo.unity");
    }
}
