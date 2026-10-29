using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DonerMaster.EditorTools
{
    public static class DonerMenu
    {
        const string ScenePath = "Assets/DonerMaster/Scenes/Main.unity";

        [MenuItem("Doner Master/Create Main Scene (for builds)")]
        public static void CreateMainScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("DonerMaster").AddComponent<GameBootstrap>();
            System.IO.Directory.CreateDirectory("Assets/DonerMaster/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);

            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            // Portrait phone game
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            Debug.Log("Doner Master: Main scene created and added to Build Settings. Press Play!");
        }

        [MenuItem("Doner Master/Reset Save Data")]
        public static void ResetSave()
        {
            PlayerPrefs.DeleteKey("doner_master_save_v1");
            PlayerPrefs.Save();
            Debug.Log("Doner Master: save data cleared.");
        }

        [MenuItem("Doner Master/Give 1000 Coins (debug)")]
        public static void GiveCoins()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play mode first."); return; }
            SaveSystem.Data.Coins += 1000;
            SaveSystem.Save();
        }
    }
}
