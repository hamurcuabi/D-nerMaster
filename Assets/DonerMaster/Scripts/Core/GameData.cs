using System.Collections.Generic;
using UnityEngine;

namespace DonerMaster
{
    public enum Product { None, Wrap, Sandwich }
    public enum Ingredient { Tomato, Onion, Lettuce, Pickle, Chili }
    public enum UpgradeId { Knife, Burner, HeatedTray, Decor }

    public class CustomerType
    {
        public string Id;
        public string Name;
        public float Patience;        // seconds
        public float TipMultiplier;   // tip = base price * this * quality^2
        public int UnlockDay;
        public float SpawnWeight;
        public Color Body;
        public Color Accent;
        public float Height = 1f;
        public bool IsRegular;        // always orders the same thing
        public bool IsCritic;         // demands quality >= CriticBar
        public bool IsPicky;          // special request (no onion, extra tomato)
    }

    public class UpgradeDef
    {
        public UpgradeId Id;
        public string Name;
        public string Description;
        public int[] Prices;          // one price per level
        public int MaxLevel => Prices.Length;
    }

    /// <summary>
    /// All balance numbers live here so tuning never means hunting through gameplay code.
    /// (Move to ScriptableObjects once a designer needs to tweak them in the editor.)
    /// </summary>
    public static class GameData
    {
        // ---------- Day ----------
        public const float DayLength = 180f;
        public const int TutorialDay = 1;

        public static int MaxCustomers(int day) => day <= 2 ? 2 : 3;
        public static float SpawnInterval(int day) => Mathf.Lerp(13f, 7.5f, Mathf.Clamp01((day - 1) / 8f));
        public static Vector2Int IngredientCount(int day) => day <= 2 ? new Vector2Int(1, 2) : day <= 5 ? new Vector2Int(2, 3) : new Vector2Int(3, 4);
        public static float AyranChance(int day) => day <= 1 ? 0.15f : 0.35f;
        public static float SandwichChance => 0.4f;

        // ---------- Menu ----------
        public const int AyranPrice = 4;
        public static int BasePrice(Product p) => p == Product.Wrap ? 12 : 10;
        public static int MeatPieces(Product p) => p == Product.Wrap ? 5 : 4;
        public static string ProductName(Product p) => p == Product.Wrap ? "WRAP" : p == Product.Sandwich ? "SANDWICH" : "-";

        public static readonly string[] IngredientNames = { "Tomato", "Onion", "Lettuce", "Pickles", "Chili" };
        public static readonly Color[] IngredientColors =
        {
            new Color(0.86f, 0.18f, 0.14f),   // tomato
            new Color(0.93f, 0.88f, 0.95f),   // onion
            new Color(0.40f, 0.75f, 0.25f),   // lettuce
            new Color(0.55f, 0.62f, 0.18f),   // pickles
            new Color(0.95f, 0.45f, 0.08f),   // chili
        };
        public static string IngredientHex(Ingredient i)
        {
            switch (i)
            {
                case Ingredient.Tomato: return "#E0433A";
                case Ingredient.Onion: return "#B58AD6";
                case Ingredient.Lettuce: return "#4FA83A";
                case Ingredient.Pickle: return "#8A9A1E";
                default: return "#F07A12";
            }
        }

        // ---------- Doner spit ----------
        public const int PiecesPerSpit = 60;           // roughly 12 wraps
        public const float BaseReloadTime = 4f;
        public static readonly float[] HeatRates = { 2.6f, 4.6f, 7.5f }; // cook points / second at full heater exposure
        public static readonly string[] HeatNames = { "LOW", "MED", "HIGH" };
        public const float FreshLayerCarry = 0.35f;    // how much of a zone's cook survives after a slice

        // ---------- Slicing (speed in screen-heights per second) ----------
        public const float IdealSpeedMin = 0.35f;
        public const float IdealSpeedMax = 1.25f;
        public const float SliceStep = 0.2f;           // world units of knife travel per piece

        // ---------- Scoring ----------
        public const int PerfectQuality = 90;
        public const int ComboBreakQuality = 70;
        public const int RefuseQuality = 40;
        public const int CriticBar = 85;
        public const float ComboStep = 0.1f;
        public const float ComboMax = 2f;
        public const float BaseHeatHold = 20f;         // seconds meat stays hot in the tray

        // ---------- Customers ----------
        public static readonly List<CustomerType> CustomerTypes = new List<CustomerType>
        {
            new CustomerType { Id = "student", Name = "Student", Patience = 40, TipMultiplier = 0.25f, UnlockDay = 1, SpawnWeight = 3f,
                Body = new Color(0.25f, 0.45f, 0.85f), Accent = new Color(0.95f, 0.75f, 0.2f), Height = 0.95f },
            new CustomerType { Id = "office", Name = "Office Worker", Patience = 22, TipMultiplier = 0.8f, UnlockDay = 1, SpawnWeight = 2f,
                Body = new Color(0.2f, 0.22f, 0.28f), Accent = new Color(0.8f, 0.1f, 0.15f), Height = 1.05f },
            new CustomerType { Id = "taxi", Name = "Taxi Driver", Patience = 35, TipMultiplier = 0.5f, UnlockDay = 2, SpawnWeight = 1.5f,
                Body = new Color(0.95f, 0.78f, 0.1f), Accent = new Color(0.15f, 0.15f, 0.15f), Height = 1f, IsRegular = true },
            new CustomerType { Id = "tourist", Name = "Tourist", Patience = 50, TipMultiplier = 1.2f, UnlockDay = 3, SpawnWeight = 1.5f,
                Body = new Color(0.95f, 0.45f, 0.55f), Accent = new Color(0.3f, 0.8f, 0.85f), Height = 1.02f },
            new CustomerType { Id = "auntie", Name = "Auntie", Patience = 45, TipMultiplier = 0.9f, UnlockDay = 5, SpawnWeight = 1.2f,
                Body = new Color(0.55f, 0.3f, 0.55f), Accent = new Color(0.95f, 0.9f, 0.8f), Height = 0.9f, IsPicky = true },
            new CustomerType { Id = "critic", Name = "Food Critic", Patience = 30, TipMultiplier = 1.6f, UnlockDay = 6, SpawnWeight = 0.7f,
                Body = new Color(0.12f, 0.12f, 0.14f), Accent = new Color(0.85f, 0.15f, 0.2f), Height = 1.08f, IsCritic = true },
        };

        // ---------- Upgrades ----------
        public static readonly List<UpgradeDef> Upgrades = new List<UpgradeDef>
        {
            new UpgradeDef { Id = UpgradeId.Knife, Name = "Sharp Knife", Description = "Wider sweet spot for thin slices", Prices = new[] { 120, 400, 1200 } },
            new UpgradeDef { Id = UpgradeId.Burner, Name = "Strong Burner", Description = "Meat cooks 20% faster per level", Prices = new[] { 150, 500, 1500 } },
            new UpgradeDef { Id = UpgradeId.HeatedTray, Name = "Heated Tray", Description = "Sliced meat stays hot longer", Prices = new[] { 250, 900 } },
            new UpgradeDef { Id = UpgradeId.Decor, Name = "Shop Decor", Description = "Customers wait 5% longer per level", Prices = new[] { 200, 450, 800 } },
        };

        public static UpgradeDef Upgrade(UpgradeId id) => Upgrades.Find(u => u.Id == id);

        // Upgrade effects
        public static float KnifeWiden(int level) => 1f + 0.3f * level;
        public static float CookSpeed(int level) => 1f + 0.2f * level;
        public static float HeatHold(int level) => level == 0 ? BaseHeatHold : level == 1 ? 32f : 45f;
        public static float PatienceBonus(int level) => 1f + 0.05f * level;

        public static CustomerType PickCustomerType(int day)
        {
            float total = 0f;
            foreach (var t in CustomerTypes) if (t.UnlockDay <= day) total += t.SpawnWeight;
            float r = Random.value * total;
            foreach (var t in CustomerTypes)
            {
                if (t.UnlockDay > day) continue;
                r -= t.SpawnWeight;
                if (r <= 0f) return t;
            }
            return CustomerTypes[0];
        }
    }
}
