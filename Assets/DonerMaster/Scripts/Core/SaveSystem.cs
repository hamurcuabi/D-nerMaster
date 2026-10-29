using System;
using System.Collections.Generic;
using UnityEngine;

namespace DonerMaster
{
    [Serializable]
    public class SaveData
    {
        public int Day = 1;
        public int Coins;
        public int TotalServed;
        public int BestDayEarnings;
        public bool TutorialDone;
        public bool Muted;
        public bool HapticsOff;
        public List<int> UpgradeLevels = new List<int>();
        // The taxi driver's usual (regulars always order the same thing)
        public int RegularProduct = (int)Product.Wrap;
        public List<int> RegularIngredients = new List<int> { (int)Ingredient.Tomato, (int)Ingredient.Onion };
        public bool RegularAyran = true;

        public int Level(UpgradeId id)
        {
            int i = (int)id;
            return i < UpgradeLevels.Count ? UpgradeLevels[i] : 0;
        }

        public void SetLevel(UpgradeId id, int level)
        {
            int i = (int)id;
            while (UpgradeLevels.Count <= i) UpgradeLevels.Add(0);
            UpgradeLevels[i] = level;
        }
    }

    /// <summary>JSON in PlayerPrefs — enough for the MVP; swap for a file/cloud save later.</summary>
    public static class SaveSystem
    {
        const string Key = "doner_master_save_v1";
        public static SaveData Data { get; private set; } = new SaveData();

        public static void Load()
        {
            try
            {
                var json = PlayerPrefs.GetString(Key, "");
                Data = string.IsNullOrEmpty(json) ? new SaveData() : JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
            }
            catch (Exception)
            {
                Data = new SaveData();
            }
        }

        public static void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }

        public static void ResetAll()
        {
            Data = new SaveData();
            Save();
        }
    }
}
