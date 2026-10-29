using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DonerMaster
{
    public class Order
    {
        public Product Product;
        public readonly HashSet<Ingredient> Ingredients = new HashSet<Ingredient>();
        public bool Ayran;
        public string Note;   // special request shown in the bubble

        public int MeatPieces => GameData.MeatPieces(Product);
        public int Price => GameData.BasePrice(Product) + (Ayran ? GameData.AyranPrice : 0);

        /// <summary>Rich-text order bubble, e.g. "WRAP / Tomato Onion / + Ayran".</summary>
        public string Describe()
        {
            var sb = new StringBuilder();
            sb.Append("<b>").Append(GameData.ProductName(Product)).Append("</b>");
            if (Ingredients.Count > 0)
            {
                sb.Append('\n');
                bool first = true;
                foreach (var i in Ingredients.OrderBy(x => (int)x))
                {
                    if (!first) sb.Append("  ");
                    sb.Append("<color=").Append(GameData.IngredientHex(i)).Append('>').Append(GameData.IngredientNames[(int)i]).Append("</color>");
                    first = false;
                }
            }
            else sb.Append("\n<i>just meat</i>");
            if (Ayran) sb.Append("\n<color=#3C8DD6>+ Ayran</color>");
            if (!string.IsNullOrEmpty(Note)) sb.Append("\n<i>").Append(Note).Append("</i>");
            return sb.ToString();
        }

        public static Order Create(int day, CustomerType type)
        {
            var o = new Order();
            if (type.IsRegular)
            {
                var d = SaveSystem.Data;
                o.Product = (Product)d.RegularProduct;
                foreach (var i in d.RegularIngredients) o.Ingredients.Add((Ingredient)i);
                o.Ayran = d.RegularAyran;
                o.Note = "\"The usual, chef!\"";
                return o;
            }

            o.Product = Random.value < GameData.SandwichChance ? Product.Sandwich : Product.Wrap;
            var range = GameData.IngredientCount(day);
            int count = Random.Range(range.x, range.y + 1);
            var pool = new List<Ingredient> { Ingredient.Tomato, Ingredient.Onion, Ingredient.Lettuce, Ingredient.Pickle, Ingredient.Chili };

            if (type.IsPicky)
            {
                // Auntie: always extra tomato, never onion
                o.Ingredients.Add(Ingredient.Tomato);
                pool.Remove(Ingredient.Tomato);
                pool.Remove(Ingredient.Onion);
                o.Note = "\"No onion, dear!\"";
            }
            while (o.Ingredients.Count < count && pool.Count > 0)
            {
                int k = Random.Range(0, pool.Count);
                o.Ingredients.Add(pool[k]);
                pool.RemoveAt(k);
            }
            o.Ayran = Random.value < GameData.AyranChance(day);
            if (type.IsCritic) o.Note = "\"Impress me.\"";
            return o;
        }
    }

    public struct ScoreResult
    {
        public int Quality;          // 0..100
        public float Ingredients, Cook, Thickness, Temperature, Speed; // each 0..1
        public string Feedback;      // what went most wrong (or praise)
        public bool WrongProduct;
    }

    public static class ScoreCalculator
    {
        public static float CookScore(float c)
        {
            if (c < 40f) return Mathf.Lerp(0.05f, 0.35f, c / 40f);
            if (c < 70f) return Mathf.Lerp(0.55f, 0.95f, (c - 40f) / 30f);
            if (c <= 90f) return 1f;
            if (c <= 92f) return 0.85f;
            return 0.15f;
        }

        public static ScoreResult Evaluate(Order order, PrepBoard board, float patienceLeft01)
        {
            var r = new ScoreResult();
            var meat = board.Meat;

            // Ingredients (incl. meat amount and ayran)
            int required = order.Ingredients.Count + 1 + (order.Ayran ? 1 : 0);
            float hits = 0f, penalty = 0f;
            foreach (var i in order.Ingredients) if (board.Ingredients.Contains(i)) hits += 1f;
            int extras = 0;
            foreach (var i in board.Ingredients) if (!order.Ingredients.Contains(i)) extras++;
            penalty += extras * 0.6f;
            float meatFrac = Mathf.Clamp01(meat.Count / (float)order.MeatPieces);
            hits += meatFrac;
            if (meat.Count > order.MeatPieces + 3) penalty += 0.25f;
            if (order.Ayran && board.Ayran) hits += 1f;
            if (!order.Ayran && board.Ayran) penalty += 0.3f;
            r.Ingredients = Mathf.Clamp01((hits - penalty) / required);

            // Meat quality
            if (meat.Count > 0)
            {
                float cook = 0f, thick = 0f, warm = 0f;
                foreach (var m in meat)
                {
                    cook += CookScore(m.Cook);
                    thick += m.Thickness;
                    warm += m.Warmth;
                }
                r.Cook = cook / meat.Count;
                r.Thickness = thick / meat.Count;
                r.Temperature = warm / meat.Count;
            }

            r.Speed = Mathf.Clamp01(patienceLeft01 * 1.5f);

            float q = 35f * r.Ingredients + 25f * r.Cook + 15f * r.Thickness + 10f * r.Temperature + 15f * r.Speed;
            r.WrongProduct = board.Product != order.Product;
            if (r.WrongProduct) q = Mathf.Min(q, 20f);
            r.Quality = Mathf.Clamp(Mathf.RoundToInt(q), 0, 100);
            r.Feedback = Feedback(r, order, board, extras);
            return r;
        }

        static string Feedback(ScoreResult r, Order order, PrepBoard board, int extras)
        {
            if (r.WrongProduct) return order.Product == Product.Wrap ? "I wanted a WRAP!" : "I wanted BREAD!";
            if (r.Quality >= GameData.PerfectQuality) return "Perfect!";

            // Largest weighted loss decides the complaint
            float lossIng = 35f * (1 - r.Ingredients), lossCook = 25f * (1 - r.Cook), lossThick = 15f * (1 - r.Thickness);
            float lossTemp = 10f * (1 - r.Temperature), lossSpeed = 15f * (1 - r.Speed);
            float max = Mathf.Max(lossIng, Mathf.Max(lossCook, Mathf.Max(lossThick, Mathf.Max(lossTemp, lossSpeed))));

            if (max == lossIng)
            {
                if (board.Meat.Count < order.MeatPieces) return "More meat!";
                foreach (var i in order.Ingredients)
                    if (!board.Ingredients.Contains(i)) return "No " + GameData.IngredientNames[(int)i].ToLower() + "?";
                if (order.Ayran && !board.Ayran) return "Where's my ayran?";
                if (extras > 0) return "I didn't ask for that!";
                return "Hmm...";
            }
            if (max == lossCook)
            {
                float avg = 0f;
                foreach (var m in board.Meat) avg += m.Cook;
                avg /= Mathf.Max(1, board.Meat.Count);
                return avg > 90f ? "Burnt!" : "Too raw!";
            }
            if (max == lossThick) return "Slices too thick!";
            if (max == lossTemp) return "It's cold...";
            return "So slow...";
        }
    }
}
