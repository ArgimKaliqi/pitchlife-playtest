#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Pitchlife.P01.Editor
{
    // A small project-owned automation entry point, also usable from Unity's Tools menu.
    public static class P01ProjectSetup
    {
        private const string Folder = "Assets/_Pitchlife/Settings";
        private const string ScenePath = "Assets/_Pitchlife/Scenes/P01FootballSandbox.unity";

        [MenuItem("Tools/PITCHLIFE/Set Up P01 Sandbox")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before setup.");
            if (EditorSceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Save the current scene before setup.");
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Folder + "/PitchlifeURP.asset");
            if (pipeline == null)
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, Folder + "/PitchlifeRenderer.asset");
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.msaaSampleCount = 2;
                pipeline.supportsHDR = false;
                pipeline.shadowDistance = 35;
                AssetDatabase.CreateAsset(pipeline, Folder + "/PitchlifeURP.asset");
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            int currentQuality = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(currentQuality, false);
            PlayerSettings.companyName = "Pitchlife";
            PlayerSettings.productName = "Pitchlife P01";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.pitchlife.sandbox");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            // Use the controller's Input System backend, including on Android.
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            settings.FindProperty("activeInputHandler").intValue = 1;
            settings.ApplyModifiedPropertiesWithoutUndo();
            if (!File.Exists(ScenePath)) P01SceneCreator.Create();
            else EditorSceneManager.OpenScene(ScenePath);
            var tuning = AssetDatabase.LoadAssetAtPath<FootballTuning>(Folder + "/FootballTuning.asset");
            if (tuning == null)
            {
                tuning = ScriptableObject.CreateInstance<FootballTuning>();
                AssetDatabase.CreateAsset(tuning, Folder + "/FootballTuning.asset");
            }
            tuning.litShader = Shader.Find("Universal Render Pipeline/Lit");
            tuning.unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            EditorUtility.SetDirty(tuning);
            var sandbox = UnityEngine.Object.FindFirstObjectByType<P01FootballSandbox>();
            if (sandbox != null) { sandbox.tuning = tuning; EditorUtility.SetDirty(sandbox); }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("P01 setup complete: scene, rendering, input and Android settings are ready.");
        }

        [MenuItem("Tools/PITCHLIFE/Build Android Test APK")]
        public static void BuildAndroid()
        {
            Setup();
            Directory.CreateDirectory("Builds/Android");
            string output = "Builds/Android/Pitchlife-P01-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".apk";
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = output,
                target = BuildTarget.Android, options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Android build failed; errors=" + report.summary.totalErrors);
            Debug.Log("P01 Android build succeeded: " + output);
        }
    }
}
#endif
