using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DonerMaster.EditorTools
{
    /// <summary>
    /// Command-line builds. Called by Tools/build-android.sh:
    ///   Unity -batchmode -quit -projectPath . -buildTarget Android
    ///         -executeMethod DonerMaster.EditorTools.BuildScript.BuildAndroidRelease
    /// Settings come from environment variables (see the shell script).
    /// </summary>
    public static class BuildScript
    {
        public const string ScenePath = "Assets/DonerMaster/Scenes/Main.unity";

        [MenuItem("Doner Master/Build Android Release")]
        public static void BuildAndroidRelease()
        {
            try
            {
                EnsureMainScene();
                ConfigureAndroid();

                bool aab = Env("DONER_AAB", "0") == "1";
                EditorUserBuildSettings.buildAppBundle = aab;
                string version = PlayerSettings.bundleVersion;
                string output = Env("DONER_OUTPUT", Path.Combine("Builds", "Android", "DonerMaster-" + version + (aab ? ".aab" : ".apk")));
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));

                var options = new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = output,
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = BuildOptions.None
                };

                var report = BuildPipeline.BuildPlayer(options);
                var s = report.summary;
                if (s.result != BuildResult.Succeeded)
                {
                    Debug.LogError("[DonerBuild] FAILED: " + s.result + " (" + s.totalErrors + " errors)");
                    Exit(1);
                    return;
                }
                Debug.Log("[DonerBuild] OK: " + Path.GetFullPath(output) + " (" + (s.totalSize / (1024 * 1024)) + " MB)");
                Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("[DonerBuild] " + e);
                Exit(1);
            }
        }

        static void ConfigureAndroid()
        {
            var android = NamedBuildTarget.Android;
            PlayerSettings.companyName = Env("DONER_COMPANY", "Hamurcu Games");
            PlayerSettings.productName = Env("DONER_PRODUCT", "Doner Master");
            PlayerSettings.SetApplicationIdentifier(android, Env("DONER_BUNDLE_ID", "com.hamurcuabi.donermaster"));
            PlayerSettings.bundleVersion = Env("DONER_VERSION", "0.1.0");
            PlayerSettings.Android.bundleVersionCode = int.Parse(Env("DONER_VERSION_CODE", "1"));

            // Google Play requirements: IL2CPP + 64-bit
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            // ARM64 only: Play needs 64-bit, and Unity's 32-bit libs have had 16 KB page-size issues.
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            // Google Play: new apps and updates must target Android 16 (API 36) since 31 Aug 2026.
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)int.Parse(Env("DONER_TARGET_SDK", "36"));
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            // Release signing (required for an installable release APK)
            string keystore = Env("DONER_KEYSTORE", "");
            if (string.IsNullOrEmpty(keystore))
                throw new Exception("DONER_KEYSTORE is not set. See Tools/build-android.sh for how to create a keystore.");
            if (!File.Exists(keystore))
                throw new Exception("Keystore not found: " + keystore);

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = Path.GetFullPath(keystore);
            PlayerSettings.Android.keystorePass = Env("DONER_KEYSTORE_PASS", "");
            PlayerSettings.Android.keyaliasName = Env("DONER_KEY_ALIAS", "doner");
            PlayerSettings.Android.keyaliasPass = Env("DONER_KEY_PASS", PlayerSettings.Android.keystorePass);
            EditorUserBuildSettings.development = false;
        }

        /// <summary>Creates the bootstrap scene and puts it in Build Settings if it is not there yet (no dialogs).</summary>
        public static void EnsureMainScene()
        {
            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("DonerMaster").AddComponent<GameBootstrap>();
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.Refresh();
            }
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            foreach (var s in EditorBuildSettings.scenes)
                if (s.path != ScenePath) scenes.Add(s);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static string Env(string key, string fallback)
        {
            var v = Environment.GetEnvironmentVariable(key);
            return string.IsNullOrEmpty(v) ? fallback : v;
        }

        static void Exit(int code)
        {
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }
    }
}
