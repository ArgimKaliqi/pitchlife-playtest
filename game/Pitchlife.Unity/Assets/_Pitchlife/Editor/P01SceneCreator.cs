#if UNITY_EDITOR
using Pitchlife.P01;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pitchlife.P01.Editor
{
    public static class P01SceneCreator
    {
        private const string Path = "Assets/_Pitchlife/Scenes/P01FootballSandbox.unity";

        [MenuItem("Tools/PITCHLIFE/Create P01 Football Sandbox Scene")]
        public static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!AssetDatabase.IsValidFolder("Assets/_Pitchlife/Scenes"))
                AssetDatabase.CreateFolder("Assets/_Pitchlife","Scenes");

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("P01 Sandbox");
            root.AddComponent<P01FootballSandbox>();
            EditorSceneManager.SaveScene(scene,Path);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Path,true) };
            Selection.activeGameObject = root;
            Debug.Log("P01 scene created. Press Play and swipe upward to kick.");
        }
    }
}
#endif
